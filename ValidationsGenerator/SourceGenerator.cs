using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Roslyn.Reflection;
using ValidationsGenerator;

[Generator]
public class SourceGenerator : IIncrementalGenerator
{
    private static readonly string[] KnownMethods =
    {
        "MapGet",
        "MapPost",
        "MapPut",
        "MapDelete",
        "MapPatch",
        "Map"
    };

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all `MapAction` invocations in the application that also have a
        // `WithValidation` invocation as an opt-in into the validation experience.
        var mapActionsWithValidation = context.SyntaxProvider
            .CreateSyntaxProvider<IInvocationOperation>(
                                                        (node, _) => node is InvocationExpressionSyntax
                                                                     {
                                                                         Expression: MemberAccessExpressionSyntax
                                                                         {
                                                                             Name: IdentifierNameSyntax
                                                                             {
                                                                                 Identifier: {ValueText: var method}
                                                                             }
                                                                         },
                                                                         ArgumentList: {Arguments: {Count: 2} args}
                                                                     } mapActionCall && KnownMethods.Contains(method) &&
                                                                     HasWithValidationCall(mapActionCall),
                                                        (syntaxContext, _) =>
                                                        {
                                                            var node = (InvocationExpressionSyntax) syntaxContext.Node;
                                                            var operation =
                                                                syntaxContext.SemanticModel.GetOperation(node);
                                                            return (IInvocationOperation) operation;
                                                        })
            .Where(static m => m is not null);

        var parametersWithSpanInfo = mapActionsWithValidation.Select((operation, _) =>
        {
            Debug.Assert(operation.SemanticModel is not null);
            var metadataLoadContext = new MetadataLoadContext(operation.SemanticModel!.Compilation);
            var delegateArgument = operation.Arguments[2];
            var method = ResolveMethodFromOperation(delegateArgument);
            var methodInfo = method.AsMethodInfo(metadataLoadContext);
            var parameters = methodInfo.GetParameters();
            var filePath = operation.Syntax.SyntaxTree.FilePath;
            var span = operation.Syntax.SyntaxTree.GetLineSpan(operation.Syntax.Parent.Span);
            var lineNumber = span.EndLinePosition.Line + 1;
            return (filePath, lineNumber, parameters);
        });

        // Pluck out a collection of `ParameterType`s for all the `ParameterInfo`s
        // in an application
        var parameterTypes = parametersWithSpanInfo.Select((parametersWithSpanInfo, _) =>
        {
            var (_, _, parameters) = parametersWithSpanInfo;
            return parameters.Select(parameter => parameter.ParameterType);
        });

        // Get the parameters that are validatable because they are types that
        // have validatable properties like (Todo todo) => ..
        var validatableTypes = parameterTypes.Select((parameters, _) =>
        {
            return parameters.Select(type => GetTypesWithValidatableProperties(type))
                .Where(type => type.ValidatableProperties.Count > 0);
        }).Collect().Select((input, _) =>
        {
            var total = new List<ValidatableType>();
            foreach (var types in input)
            {
                foreach (var type in types)
                {
                    if (!total.Contains(type))
                    {
                        total.Add(type);
                    }
                }
            }

            return total.Distinct();
        });

        // Given a list of types, extract all the attributes on properties in those types and emit
        // static instances for them.
        var validationAttributeInstances = validatableTypes.Select((types, _) =>
        {
            var code = new StringBuilder();
            var codeWriter = new CodeWriter(code);
            codeWriter.Indent();
            foreach (var type in types)
            {
                foreach (var property in type.ValidatableProperties)
                {
                    var attributes = property.Attributes;
                    foreach (var attribute in attributes)
                    {
                        var validatorName = $"{type.ElementType}_{property.Property.Name}_{attribute.AttributeClass.Name}";
                        // If the attribute was configured with construct args, use those.
                        var constructorArguments = attribute.ConstructorArguments.Any()
                            ? $"({string.Join(",", attribute.ConstructorArguments.Select(arg => arg.Value))})"
                            : "()";
                        codeWriter
                            .WriteLine($"private static readonly ValidationAttribute {validatorName} = new {attribute.AttributeClass.Name}{constructorArguments};");
                    }
                }
            }

            return code.ToString();
        });

        context.RegisterSourceOutput(validationAttributeInstances, (context, sources) =>
        {
            var code = new StringBuilder();
            var codeWriter = new CodeWriter(code);
            codeWriter.WriteLine("using System.ComponentModel.DataAnnotations;");
            codeWriter.WriteLine("public static partial class Validations");
            codeWriter.WriteLine("{");
            codeWriter.WriteLine(sources);
            codeWriter.Unindent();
            codeWriter.WriteLine("}");
            
            context.AddSource("Validation.ValidationAttributeInstances.g.cs", code.ToString());
        });

        var validatableTypeMethods = validatableTypes.Select((validatableTypes,_) =>
        {
            var code = new StringBuilder();
            var codeWriter = new CodeWriter(code);
            foreach (var validatableType in validatableTypes)
            {
                var elementType = validatableType.ElementType;
                codeWriter.Indent();
                codeWriter.WriteLine($"public static void Validate({elementType} value, ref List<ValidationResult> results)");
                codeWriter.WriteLine("{");
                codeWriter.Indent();
                codeWriter.WriteLine($"if (value.GetType() != typeof({elementType}))");
                codeWriter.WriteLine("{");
                codeWriter.Indent();
                codeWriter.WriteLine($@"throw new Exception($""Expected to validate {elementType} but got {{value.GetType()}}"");");
                codeWriter.Unindent();
                codeWriter.WriteLine("}");
                codeWriter.WriteLine("var validationContext = new ValidationContext(value);");
                foreach (var validatableProperty in validatableType.ValidatableProperties)
                {
                    var property = validatableProperty.Property;
                    var attributes = validatableProperty.Attributes;
                    foreach (var attribute in attributes)
                    {
                        var validatorName = $"{elementType}_{property.Name}_{attribute.AttributeClass.Name}";
                        codeWriter.WriteLine(
                            $@"validationContext.MemberName = ""{elementType.Name}.{property.Name}"";");
                        codeWriter.WriteLine(
                            $@"validationContext.DisplayName = ""{elementType.Name}.{property.Name}"";");
                        codeWriter
                            .WriteLine(
                                $@"results.Add({validatorName}.GetValidationResult(value.{property.Name}, validationContext));");
                    }

                    if (validatableProperty.IsOtherValidatableType)
                    {
                        codeWriter.WriteLine($"Validations.Validate(value.{property.Name}, ref results);");
                    }
                }

                codeWriter.Unindent();
                codeWriter.WriteLine("}");
                
                codeWriter.WriteLine($"public static void Validate(IEnumerable<{elementType}> values, ref List<ValidationResult> results)");
                codeWriter.WriteLine("{");
                codeWriter.Indent();
                codeWriter.WriteLine("var validationContext = new ValidationContext(values);");
                codeWriter.WriteLine("var index = 0;");
                codeWriter.WriteLine("foreach (var value in values)");
                codeWriter.WriteLine("{");
                codeWriter.Indent();
                foreach (var validatableProperty in validatableType.ValidatableProperties)
                {
                    var property = validatableProperty.Property;
                    var attributes = validatableProperty.Attributes;
                    foreach (var attribute in attributes)
                    {
                        var validatorName = $"{elementType}_{property.Name}_{attribute.AttributeClass.Name}";
                        codeWriter.WriteLine(
                            $@"validationContext.MemberName = $""{elementType.Name}[{{index}}].{property.Name}"";");
                        codeWriter.WriteLine(
                            $@"validationContext.DisplayName = $""{elementType.Name}[{{index}}].{property.Name}"";");
                        codeWriter
                            .WriteLine(
                                $@"results.Add({validatorName}.GetValidationResult(value.{property.Name}, validationContext));");
                    }

                    if (validatableProperty.IsOtherValidatableType)
                    {
                        codeWriter.WriteLine($"Validations.Validate(value.{property.Name}, ref results);");
                    }
                }

                codeWriter.WriteLine("index++;");
                codeWriter.Unindent();
                codeWriter.WriteLine("}");
                codeWriter.Unindent();
                codeWriter.WriteLine("}");
                codeWriter.Unindent();
            }

            return code.ToString();
        });
        
        context.RegisterSourceOutput(validatableTypeMethods, (context, sources) =>
        {

            var code = new StringBuilder();
            var codeWriter = new CodeWriter(code);
            codeWriter.WriteLine("using System;");
            codeWriter.WriteLine("using System.ComponentModel.DataAnnotations;");
            codeWriter.WriteLine("using System.Collections.Generic;");
            codeWriter.WriteLine("public static partial class Validations");
            codeWriter.WriteLine("{");
            codeWriter.WriteLine(sources);
            codeWriter.WriteLine("}");
            
            context.AddSource("Validation.ValidationTypeMethods.g.cs", code.ToString());
        });

        // For each validatable parameter, generate a syntax tree that represents the invocations to the underlying
        // validate calls.
        var methodsWithValidation = parametersWithSpanInfo.Select((input, _) =>
        {
            var (filePath, lineNumber, parameters) = input;
            var code = new StringBuilder();
            var codeWriter = new CodeWriter(code);
            var filterFactoryCode = new StringBuilder();
            var filterFactoryCodeWriter = new CodeWriter(filterFactoryCode);
            filterFactoryCodeWriter.Indent();
            filterFactoryCodeWriter.Indent();
            filterFactoryCodeWriter.Indent();
            var filterDelegateCode = new StringBuilder();
            var filterDelegateCodeWriter = new CodeWriter(filterDelegateCode);
            filterDelegateCodeWriter.Indent();
            filterDelegateCodeWriter.Indent();
            filterDelegateCodeWriter.Indent();
            filterDelegateCodeWriter.WriteLine("return async (context) =>");
            filterDelegateCodeWriter.WriteLine("{");
            filterDelegateCodeWriter.Indent();
            filterDelegateCodeWriter.WriteLine("var results = new List<ValidationResult>();");
            var parameterIndex = 0;
            foreach (var parameter in parameters)
            {
                filterDelegateCodeWriter.WriteLine($"var {parameter.Name} = context.GetArgument<{parameter.ParameterType}>({parameterIndex});");
                if (parameter.TryGetTopLevelValidations(out var topLevelAttributes))
                {
                    filterDelegateCodeWriter.WriteLine($"var validationContext = new ValidationContext({parameter.Name});");
                    filterDelegateCodeWriter.WriteLine(
                        $@"validationContext.MemberName = ""{parameter.Name}"";");
                    filterDelegateCodeWriter.WriteLine(
                        $@"validationContext.DisplayName = ""{parameter.Name}"";");
                    foreach (var attribute in topLevelAttributes)
                    {
                        var validatorName = $"{parameter.Name}_{attribute.AttributeType.Name}";
                        var constructorArgs = attribute.ConstructorArguments.Count > 0
                            ? $"({string.Join(",", attribute.ConstructorArguments.Select(arg => arg.Value))})"
                            : "()";
                        filterFactoryCodeWriter.WriteLine($"var {validatorName} = new {attribute.AttributeType.FullName}{constructorArgs};");
                        filterDelegateCodeWriter.WriteLine($"results.Add({validatorName}.GetValidationResult({parameter.Name}, validationContext));");
                    }
                }
                else
                {
                    filterDelegateCodeWriter.WriteLine($"Validations.Validate({parameter.Name}, ref results);");
                }
                parameterIndex++;
            }
            
            filterDelegateCodeWriter.WriteLine("var errors = new Dictionary<string, string[]>();");
            filterDelegateCodeWriter.WriteLine("foreach (var result in results)");
            filterDelegateCodeWriter.WriteLine("{");
            filterDelegateCodeWriter.Indent();
            filterDelegateCodeWriter.WriteLine("if (result != ValidationResult.Success)");
            filterDelegateCodeWriter.WriteLine("{");
            filterDelegateCodeWriter.Indent();
            filterDelegateCodeWriter.WriteLine("errors.Add(result.MemberNames.SingleOrDefault(), new[] { result.ErrorMessage });");
            filterDelegateCodeWriter.Unindent();
            filterDelegateCodeWriter.WriteLine("}");
            filterDelegateCodeWriter.Unindent();
            filterDelegateCodeWriter.WriteLine("}");
            filterDelegateCodeWriter.WriteLine("if (errors.Count > 0)");
            filterDelegateCodeWriter.WriteLine("{");
            filterDelegateCodeWriter.Indent();
            filterDelegateCodeWriter.WriteLine("return Results.ValidationProblem(errors);");
            filterDelegateCodeWriter.Unindent();
            filterDelegateCodeWriter.WriteLine("}");
            filterDelegateCodeWriter.WriteLine("return await next(context);");
            filterDelegateCodeWriter.Unindent();
            filterDelegateCodeWriter.WriteLine("};");
            
            codeWriter.WriteLine($@"[(""{filePath}"", {lineNumber})] = (EndpointFilterFactoryContext factoryContext, EndpointFilterDelegate next) =>");
            codeWriter.Indent();
            codeWriter.Indent();
            codeWriter.WriteLine("{");
            codeWriter.Unindent();
            codeWriter.Unindent();
            codeWriter.WriteLine(filterFactoryCodeWriter.ToString());
            codeWriter.WriteLine(filterDelegateCodeWriter.ToString());
            codeWriter.Indent();
            codeWriter.Indent();
            codeWriter.WriteLine("},");
            codeWriter.Unindent();
            codeWriter.Unindent();
            return code.ToString();
        });

        context.RegisterSourceOutput(methodsWithValidation.Collect(),
         static (context, generatedSources) =>
         {
             var code = new StringBuilder();
             var codeWriter = new CodeWriter(code);
             codeWriter.WriteLine("// <auto-generated/>");
             codeWriter.WriteLine("#nullable enable");
             codeWriter.WriteLine("using System;");
             codeWriter.WriteLine("using System.Linq;");
             codeWriter.WriteLine("using System.Threading.Tasks;");
             codeWriter.WriteLine("using Microsoft.AspNetCore.Http;");
             codeWriter.WriteLine("using Microsoft.AspNetCore.Builder;");
             codeWriter.WriteLine("using System.Collections.Generic;");
             codeWriter.WriteLine("using System.ComponentModel.DataAnnotations;");
             codeWriter.WriteLine("using System.Runtime.CompilerServices;");
             codeWriter.WriteLine("public static partial class Validations");
             codeWriter.WriteLine("{");
             codeWriter.Indent();
             codeWriter.WriteLine(@"public static TBuilder WithValidation<TBuilder>(this TBuilder builder, [CallerFilePath] string filePath = """", [CallerLineNumber] int lineNumber = 0) where TBuilder : IEndpointConventionBuilder");
             codeWriter.WriteLine("{");
             codeWriter.Indent();
             codeWriter.WriteLine("builder.AddEndpointFilterFactory(Validations.Map[(filePath, lineNumber)]);");
             codeWriter.WriteLine("return builder;");
             codeWriter.Unindent();
             codeWriter.WriteLine("}");
             codeWriter.WriteLine("public static readonly System.Collections.Generic.Dictionary<(string, int), Func<EndpointFilterFactoryContext, EndpointFilterDelegate, EndpointFilterDelegate>> Map = new()");
             codeWriter.WriteLine("{");
             codeWriter.Indent();
             foreach (var generated in generatedSources)
             {
                 codeWriter.WriteLine(generated);
             }
             codeWriter.Unindent();
             codeWriter.WriteLine("};");
             codeWriter.Unindent();
             codeWriter.WriteLine("}");
             context.AddSource("Validations.ValidationFilters.g.cs", code.ToString());
         });
    }

    private static bool HasWithValidationCall(InvocationExpressionSyntax invocationExpressionSyntax)
    {
        // TODO: Figure out how to discover `WithValidation` call.
        return true;
    }

    // Discovers validations on simple parameters like ([MinLength(3)] string routeParam)
    private static bool IsParameterWithTopLevelValidations(ParameterInfo parameter)
    {
        var hasTopLevelAttributes =
            parameter.CustomAttributes.Any(attr => attr.AttributeType.BaseType.Name.Contains("ValidationAttribute"));
        if (hasTopLevelAttributes)
        {
            return hasTopLevelAttributes;
        }

        return false;
    }

    private ValidatableType GetTypesWithValidatableProperties(Type type, bool isBaseType = false)
    {
        var validatableType = new ValidatableType();
        
        var isListType = type.GetTypeInfo().ImplementedInterfaces.Contains(typeof(IEnumerable));
        if (isListType)
        {
            type = type.GetGenericArguments().SingleOrDefault();
        }
        
        validatableType.ElementType = type;
        
        if (type.BaseType is RoslynType baseType)
        {
            validatableType.BaseTypes = new List<Type> { baseType };
        }

        var properties = type.GetProperties();
        foreach (var property in properties)
        {
            // Get attributes on each property and check if property implements
            // IValidate interface
            if (property is RoslynPropertyInfo roslynPropertyInfo)
            {
                var attributes = roslynPropertyInfo.PropertySymbol.GetAttributes();
                var validationAttributes = attributes.Where(attribute =>
                {
                    var attributeClass = attribute.AttributeClass;
                    var baseTypes = attributeClass.BaseTypes();
                    return baseTypes.Any(baseType => baseType.Name.Contains("ValidationAttribute"));
                });
                if (validationAttributes.Any())
                {
                    validatableType.ValidatableProperties.Add(new ValidatableProperty()
                    {
                        Property = roslynPropertyInfo,
                        Attributes = validationAttributes.ToList(),
                    });
                }

                if (HasValidatableProperties(roslynPropertyInfo.PropertyType))
                {
                    validatableType.ValidatableProperties.Add(new ValidatableProperty()
                    {
                        Property = roslynPropertyInfo,
                        Attributes = new(),
                        IsOtherValidatableType = true
                    });
                }
            }
        }

        return validatableType;
    }

    public bool HasValidatableProperties(Type type)
    {
        var properties = type.GetProperties();
        foreach (var property in properties)
        {
            // Get attributes on each property and check if property implements
            // IValidate interface
            if (property is RoslynPropertyInfo roslynPropertyInfo)
            {
                var attributes = roslynPropertyInfo.PropertySymbol.GetAttributes();
                var validationAttributes = attributes.Where(attribute =>
                {
                    var attributeClass = attribute.AttributeClass;
                    var baseTypes = attributeClass.BaseTypes();
                    return baseTypes.Any(baseType => baseType.Name.Contains("ValidationAttribute"));
                });
                if (validationAttributes.Any())
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static IMethodSymbol ResolveMethodFromOperation(IOperation delegateArgumentOperation)
    {
        return delegateArgumentOperation switch
        {
            IArgumentOperation argument => ResolveMethodFromOperation(argument.Value),
            IConversionOperation conv => ResolveMethodFromOperation(conv.Operand),
            IDelegateCreationOperation del => ResolveMethodFromOperation(del.Target),
            IFieldReferenceOperation { Field.IsReadOnly: true } f when ResolveDeclarationOperation(f.Field, f.SemanticModel!) is IOperation op =>
                ResolveMethodFromOperation(op),
            IAnonymousFunctionOperation anon => anon.Symbol,
            ILocalFunctionOperation local => local.Symbol,
            IMethodReferenceOperation method => method.Method,
            _ => null
        };
    }

    private static IOperation ResolveDeclarationOperation(ISymbol symbol, SemanticModel semanticModel)
    {
        foreach (var syntaxReference in symbol.DeclaringSyntaxReferences)
        {
            var syn = syntaxReference.GetSyntax();

            if (syn is VariableDeclaratorSyntax
                {
                    Initializer:
                    {
                        Value: var expr
                    }
                })
            {
                // Use the correct semantic model based on the syntax tree
                var operation = semanticModel.GetOperation(expr);

                if (operation is not null)
                {
                    return operation;
                }
            }
        }

        return null;
    }
}

public class ValidatableType
{
    public Type ElementType { get; set; }

    public List<Type> BaseTypes { get; set; }

    public List<ValidatableProperty> ValidatableProperties { get; set; } = new();
    
    public override bool Equals(object o)
    {
        return o is ValidatableType otherTypeAnnotation &&
               otherTypeAnnotation.ElementType == ElementType;
    }
}

public class ValidatableProperty
{
    public PropertyInfo Property { get; set; }
    public List<AttributeData> Attributes { get; set; }
    public bool IsOtherValidatableType { get; set; }
}