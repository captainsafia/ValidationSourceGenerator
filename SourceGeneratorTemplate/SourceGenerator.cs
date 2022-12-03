using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Roslyn.Reflection;

[Generator]
public class SourceGenerator : IIncrementalGenerator
{
    private static readonly string[] KnownMethods = new[]
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
                predicate: (node, _) => node is InvocationExpressionSyntax
                {
                    Expression: MemberAccessExpressionSyntax
                    {
                        Name: IdentifierNameSyntax
                        {
                            Identifier: { ValueText: var method }
                        }
                    },
                    ArgumentList: { Arguments: { Count: 2 } args }
                } mapActionCall && KnownMethods.Contains(method) && HasWithValidationCall(mapActionCall),
                transform: (syntaxContext, _) =>
                {
                    var node = (InvocationExpressionSyntax) syntaxContext.Node;
                    var operation = syntaxContext.SemanticModel.GetOperation(node);
                    return (IInvocationOperation) operation;
                })
            .Where(static m => m is not null);
        
        // Find the validatable parameter types in the target invocation.
        var validatableParameters = mapActionsWithValidation.Select((operation, _) =>
        {
            Debug.Assert(operation.SemanticModel is not null);
            var metadataLoadContext = new MetadataLoadContext(operation.SemanticModel!.Compilation);
            var delegateArgument = operation.Arguments[2];
            var method = ResolveMethodFromOperation(delegateArgument);
            var methodInfo = method.AsMethodInfo(metadataLoadContext);
            var parameters = methodInfo.GetParameters();
            return parameters.Where(parameter => HasPropertiesWithValidations(parameter, out var _));
        });
            
        // For each validatable parameter, generate a syntax tree that represents the invocations to the underlying
        // validate calls.
        var generateValidateCall = validatableParameters.Select((parameters, _) =>
        {
            System.Diagnostics.Debugger.Launch();
            var validateMethod =
                "public Validate(EndpointFilterInvocation context, EndpointFilterDelegate next)";
            validateMethod += "{";
            var attributeConstructions = "";
            var attributeValidateInvocations = "";
            var extractParameters = "";
            for (int i = 0; i < parameters.Count(); i++)
            {
                var parameter = parameters.ElementAt(i);
                extractParameters +=
                    $"var {parameter.Name} = context.GetArgument<{parameter.ParameterType}>({i});";
                
                if (HasPropertiesWithValidations(parameter, out var attributedProperties))
                {
                    foreach (var pair in attributedProperties)
                    {
                        var property = pair.Key;
                        var attributes = pair.Value;
                        foreach (var attribute in attributes)
                        {
                            // Give [Range(1, 3)] we want to emit something like
                            // var Id_RangeAttribute = new RangeAttribute(1, 3);
                            var validatorName = $"{property.Name}_{attribute.AttributeClass.Name}";
                            var parens = attribute.ToString().EndsWith(")") ? string.Empty : "()";
                            attributeConstructions += $"var {validatorName} = new {attribute}{parens};\n";
                            var validateInvocation = $"{validatorName}.IsValid({parameter.Name}.{property.Name})";
                            attributeValidateInvocations += $"if ({validateInvocation})\n";
                            attributeValidateInvocations += "{";
                            attributeValidateInvocations +=
                                $@"return Results.Problem(""Validation problem detected in {validatorName}"");";
                            attributeValidateInvocations += "}";
                        }
                    }
                }
            }

            return $@"
public static async ValueTask<object?> Validate(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
{{
    {extractParameters}
    {attributeConstructions}
    {attributeValidateInvocations}
    return next(context);
}}";
        });
        
        context.RegisterSourceOutput(generateValidateCall.Collect(),
            static (context, generatedSources) =>
            {
                if (generatedSources.IsEmpty)
                    return;
                StringBuilder source = new();
                source.AppendLine("// <auto-generated/>");
                source.AppendLine("using Microsoft.AspNetCore.Http;");
                source.AppendLine("public static partial class ValidationLookups");
                source.AppendLine("{");
                foreach (var generated in generatedSources)
                {
                    source.Append(generated);
                }
                source.AppendLine("}");
                context.AddSource("ValidationGenerated.g.cs", source.ToString());
            });
    }

    private static bool HasWithValidationCall(InvocationExpressionSyntax invocationExpressionSyntax)
    {
        // TODO: Figure out how to discover `WithValidation` call.
        return true;
    }

    private static bool HasPropertiesWithValidations(ParameterInfo parameter, out IDictionary<RoslynPropertyInfo, List<AttributeData>> attributedProperties)
    {
        var type = parameter.ParameterType;
        var properties = type.GetProperties();
        attributedProperties = new Dictionary<RoslynPropertyInfo, List<AttributeData>>();
        var hasValidation = false;
        foreach (var property in properties)
        {
            // Get attributes on each property and check if property implements
            // IValidate experience
            if (property is RoslynPropertyInfo roslynPropertyInfo)
            {
                var attributes = roslynPropertyInfo.PropertySymbol.GetAttributes();
                var validationAttributes = attributes.Where(attribute =>
                {
                    var attributeClass = attribute.AttributeClass;
                    var baseTypes = attributeClass.BaseTypes();
                    return baseTypes.Any(baseType => baseType.Name.Contains("ValidationAttribute"));
                });
                hasValidation |= validationAttributes.Any();
                if (validationAttributes.Any())
                {
                    attributedProperties.Add(roslynPropertyInfo, validationAttributes.ToList());
                }
            }
        }
        return hasValidation;
    }
    
    private static IMethodSymbol ResolveMethodFromOperation(IOperation delegateArgumentOperation) => delegateArgumentOperation switch
    {
        IArgumentOperation argument => ResolveMethodFromOperation(argument.Value),
        IConversionOperation conv => ResolveMethodFromOperation(conv.Operand),
        IDelegateCreationOperation del => ResolveMethodFromOperation(del.Target),
        IFieldReferenceOperation f when f.Field.IsReadOnly && ResolveDeclarationOperation(f.Field, f.SemanticModel!) is IOperation op => ResolveMethodFromOperation(op),
        IAnonymousFunctionOperation anon => anon.Symbol,
        ILocalFunctionOperation local => local.Symbol,
        IMethodReferenceOperation method => method.Method,
        _ => null
    };
    
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
