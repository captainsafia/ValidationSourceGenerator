using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Basic.Reference.Assemblies;


namespace ValidationsGenerator.Tests;

public static class TestHelper
{
    public static Task Verify(params string[] sources)
    {
        // Parse the provided string into a C# syntax tree
        var syntaxTrees = sources.Select(source => CSharpSyntaxTree.ParseText(source, path: $"{Guid.NewGuid()}.cs"));
        var references = new List<PortableExecutableReference>
        {
            MetadataReference.CreateFromFile(typeof(Microsoft.AspNetCore.Builder.IEndpointConventionBuilder).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Microsoft.AspNetCore.Builder.WebApplication).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Microsoft.AspNetCore.Http.EndpointFilterExtensions).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Microsoft.AspNetCore.Http.EndpointFilterDelegate).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Microsoft.Extensions.Hosting.IHost).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Microsoft.AspNetCore.Http.Results).Assembly.Location),
        };
        references.AddRange(Net70.References.All);

        // Create a Roslyn compilation for the syntax tree.
        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: "Tests",
            syntaxTrees: syntaxTrees,
            references: references,
            options: new CSharpCompilationOptions(OutputKind.ConsoleApplication));

        var generator = new SourceGenerator();

        // The GeneratorDriver is used to run our generator against a compilation
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        // Run the source generator
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation,
            out var outputDiagnostics);
        var diagnostics = updatedCompilation.GetDiagnostics();
        Assert.Empty(diagnostics.Where(d => d.Severity > DiagnosticSeverity.Warning));
        Assert.True(outputDiagnostics.IsEmpty);

        // Use verify to snapshot test the source generator output!
        return Verifier.Verify(driver).UseDirectory(Path.Combine("Snapshots"));
    }
}