using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Basic.Reference.Assemblies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;


namespace ValidationsGenerator.Tests;

public class TestBase
{
    internal ILoggerFactory TestLoggerFactory;

    public async Task<Compilation> Verify(params string[] sources)
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
            MetadataReference.CreateFromFile(typeof(Microsoft.AspNetCore.Routing.RouteData).Assembly.Location),
        };
        references.AddRange(Net70.References.All);

        // Create a Roslyn compilation for the syntax tree.
        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: Guid.NewGuid().ToString(),
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

        return updatedCompilation;
    }

    public Endpoint GetEndpoint(Compilation compilation)
    {
        var symbolsName = compilation.AssemblyName;
        var output = new MemoryStream();
        var pdb = new MemoryStream();

        var emitOptions = new EmitOptions(
                                          debugInformationFormat: DebugInformationFormat.PortablePdb,
                                          pdbFilePath: symbolsName);
        
        var embeddedTexts = new List<EmbeddedText>();

        // Make sure we embed the sources in pdb for easy debugging
        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            var text = syntaxTree.GetText();
            var encoding = text.Encoding ?? Encoding.UTF8;
            var buffer = encoding.GetBytes(text.ToString());
            var sourceText = SourceText.From(buffer, buffer.Length, encoding, canBeEmbedded: true);

            var syntaxRootNode = (CSharpSyntaxNode)syntaxTree.GetRoot();
            var newSyntaxTree = CSharpSyntaxTree.Create(syntaxRootNode, options: null, encoding: encoding, path: syntaxTree.FilePath);

            compilation = compilation.ReplaceSyntaxTree(syntaxTree, newSyntaxTree);

            embeddedTexts.Add(EmbeddedText.FromSource(syntaxTree.FilePath, sourceText));
        }

        var _= compilation.Emit(output, pdb, options: emitOptions, embeddedTexts: embeddedTexts);
        
        output.Position = 0;
        pdb.Position = 0;

        var assembly = AssemblyLoadContext.Default.LoadFromStream(output, pdb);
        var handler = assembly?.GetType("EndpointTest")
            ?.GetMethod("MapTestEndpoints", BindingFlags.Public | BindingFlags.Static)
            ?.CreateDelegate<Func<IEndpointRouteBuilder, IEndpointRouteBuilder>>();
        
        Assert.NotNull(handler);
        
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<ILoggerFactory>(TestLoggerFactory);
        var serviceProvider = serviceCollection.BuildServiceProvider();
        
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(serviceProvider));
        var __ = handler(builder);

        var dataSource = Assert.Single(builder.DataSources);
        // Trigger Endpoint build by calling getter.
        var endpoint = Assert.Single(dataSource.Endpoints);

        return endpoint;
    }

    public async Task AssertEndpointBehavior(
        Endpoint endpoint,
        string expectedResponse,
        int expectedStatusCode,
        RouteValueDictionary? routes = null,
        QueryString? query = null,
        string? requestBody = null)
    {
        var httpContext = new DefaultHttpContext();

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<ILoggerFactory>(TestLoggerFactory);
        httpContext.RequestServices = serviceCollection.BuildServiceProvider();
        
        var outStream = new MemoryStream();
        httpContext.Response.Body = outStream;

        if (query is { } q)
        {
            httpContext.Request.QueryString = q;
        }

        if (routes is not null)
        {
            httpContext.Request.RouteValues = routes;
        }

        if (requestBody is not null)
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));
            httpContext.Request.Body = stream;

            httpContext.Request.Headers["Content-Type"] = "application/json";
            httpContext.Request.Headers["Content-Length"] = stream.Length.ToString(CultureInfo.InvariantCulture);
            httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));
        }

        Assert.NotNull(endpoint.RequestDelegate);
        await endpoint.RequestDelegate(httpContext);

        var httpResponse = httpContext.Response;
        httpResponse.Body.Seek(0, SeekOrigin.Begin);
        var streamReader = new StreamReader(httpResponse.Body);
        var body = await streamReader.ReadToEndAsync();
        Assert.Equal(expectedStatusCode, httpContext.Response.StatusCode);
        Assert.Equal(expectedResponse, body);
    }
}

internal class DefaultEndpointRouteBuilder : IEndpointRouteBuilder
{
    public DefaultEndpointRouteBuilder(IApplicationBuilder applicationBuilder)
    {
        ApplicationBuilder = applicationBuilder ?? throw new ArgumentNullException(nameof(applicationBuilder));
        DataSources = new List<EndpointDataSource>();
    }

    public IApplicationBuilder ApplicationBuilder { get; }

    public IApplicationBuilder CreateApplicationBuilder() => ApplicationBuilder.New();

    public ICollection<EndpointDataSource> DataSources { get; }

    public IServiceProvider ServiceProvider => ApplicationBuilder.ApplicationServices;
}

internal class EmptyServiceProvider : IServiceScope, IServiceProvider, IServiceScopeFactory
{
    public IServiceProvider ServiceProvider => this;

    public RouteHandlerOptions RouteHandlerOptions { get; set; } = new RouteHandlerOptions();

    public IServiceScope CreateScope()
    {
        return this;
    }

    public void Dispose() { }

    public object? GetService(Type serviceType)
    {
        return null;
    }
}

public class XunitLoggerProvider : ILoggerProvider
{
    private readonly ITestOutputHelper _testOutputHelper;

    public XunitLoggerProvider(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    public ILogger CreateLogger(string categoryName)
        => new XunitLogger(_testOutputHelper, categoryName);

    public void Dispose()
    { }
}

public class XunitLogger : ILogger
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly string _categoryName;

    public XunitLogger(ITestOutputHelper testOutputHelper, string categoryName)
    {
        _testOutputHelper = testOutputHelper;
        _categoryName = categoryName;
    }

    public IDisposable BeginScope<TState>(TState state)
        => NoopDisposable.Instance;

    public bool IsEnabled(LogLevel logLevel)
        => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        _testOutputHelper.WriteLine($"{_categoryName} [{eventId}] {formatter(state, exception)}");
        if (exception != null)
            _testOutputHelper.WriteLine(exception.ToString());
    }

    private class NoopDisposable : IDisposable
    {
        public static readonly NoopDisposable Instance = new NoopDisposable();
        public void Dispose()
        { }
    }
}
internal class RequestBodyDetectionFeature : IHttpRequestBodyDetectionFeature
{
    public RequestBodyDetectionFeature(bool canHaveBody)
    {
        CanHaveBody = canHaveBody;
    }

    public bool CanHaveBody { get; }
}