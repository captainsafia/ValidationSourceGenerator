using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace ValidationsGenerator.Tests;

[UsesVerify]
public class SourceGeneratorTests : TestBase
{
    public SourceGeneratorTests(ITestOutputHelper testOutputHelper)
    { 
        TestLoggerFactory = LoggerFactory.Create(l => { l.AddProvider(new XunitLoggerProvider(testOutputHelper)); });
    }
    
    [Theory]
    [InlineData(@"{ ""id"": 0, ""title"": ""A valid title"", ""isCompleted"": false }", 400, """{"type":"https://tools.ietf.org/html/rfc7231#section-6.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"Todo.Id":["The field Todo.Id must be between 1 and 2147483647."]}}""")]
    [InlineData(@"{ ""id"": 1, ""title"": ""A"", ""isCompleted"": false }", 400, """{"type":"https://tools.ietf.org/html/rfc7231#section-6.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"Todo.Title":["The field Todo.Title must be a string or array type with a minimum length of '3'."]}}""")]
    [InlineData(@"{ ""id"": 1, ""title"": ""A valid title"", ""isCompleted"": false }", 200, "")]
    public async Task BasicComplexType_Works(string requestBody, int expectedStatusCode, string expectedResponse)
    {
        var source = GetCompilationSource("""
app.MapPost("/todo", (Todo todo) => todos.Add(todo))
    .WithValidation();
""");
        
        var compilation = await Verify(source);
        var endpoint = GetEndpoint(compilation);
        
        await AssertEndpointBehavior(endpoint: endpoint,
                                    requestBody: requestBody,
                                    expectedResponse: expectedResponse,
                                    expectedStatusCode: expectedStatusCode);
    }
    
    [Theory]
    [InlineData("0", 400, """{"type":"https://tools.ietf.org/html/rfc7231#section-6.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"id":["The field id must be between 1 and 2147483647."]}}""")]
    [InlineData("1", 200, "")]
    public async Task BasicSimpleType_Works(string id, int expectedStatusCode, string expectedResponse)
    {
        var source = GetCompilationSource("""
app.MapGet("/todo/{id}", ([Required] [Range(1, int.MaxValue)] int id) =>
     {
         var index = todos.FindIndex(todo => todo.Id == id);
         return string.Empty;
     })
     .WithValidation();
""");
        
        var compilation = await Verify(source);
        var endpoint = GetEndpoint(compilation);
        
        await AssertEndpointBehavior(endpoint: endpoint,
                                     routes: new RouteValueDictionary { { "id", id } },
                                     expectedResponse: expectedResponse,
                                     expectedStatusCode: expectedStatusCode);
    }
    

    [Theory]
    [InlineData(@"[{ ""id"": 1, ""title"": ""A valid title"", ""isCompleted"": false }, { ""id"": 0, ""title"": ""A valid title"", ""isCompleted"": false }]", 400, """{"type":"https://tools.ietf.org/html/rfc7231#section-6.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"Todo[1].Id":["The field Todo[1].Id must be between 1 and 2147483647."]}}""")]
    [InlineData(@"[{ ""id"": 1, ""title"": ""A valid title"", ""isCompleted"": false }, { ""id"": 1, ""title"": ""A"", ""isCompleted"": false }]", 400, """{"type":"https://tools.ietf.org/html/rfc7231#section-6.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"Todo[1].Title":["The field Todo[1].Title must be a string or array type with a minimum length of '3'."]}}""")]
    [InlineData(@"[{ ""id"": 1, ""title"": ""A valid title"", ""isCompleted"": false }]", 200, "")]

    public async Task EnumerableType_Works(string requestBody, int expectedStatusCode, string expectedResponse)
    {
        var source = GetCompilationSource("""
app.MapPost("/todos", (List<Todo> todosIn) => todos.AddRange(todosIn))
    .WithValidation();
""");
        
        var compilation = await Verify(source);
        var endpoint = GetEndpoint(compilation);
        
        await AssertEndpointBehavior(
            endpoint: endpoint,
            requestBody: requestBody,
            expectedResponse: expectedResponse,
            expectedStatusCode: expectedStatusCode);
    }

    [Theory]
    [InlineData(@"{ ""id"": 0, ""title"": ""A valid title"", ""isCompleted"": false, ""project"": ""inva"" }", 400, """{"type":"https://tools.ietf.org/html/rfc7231#section-6.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"TodoWithProject.Project":["The field TodoWithProject.Project must be a string or array type with a minimum length of '6'."],"TodoWithProject.Id":["The field TodoWithProject.Id must be between 1 and 2147483647."]}}""")]
    [InlineData(@"{ ""id"": 1, ""title"": ""A"", ""isCompleted"": false, ""project"": ""Valid Project Title"" }", 400, """{"type":"https://tools.ietf.org/html/rfc7231#section-6.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"TodoWithProject.Title":["The field TodoWithProject.Title must be a string or array type with a minimum length of '3'."]}}""")]
    [InlineData(@"{ ""id"": 1, ""title"": ""A valid title"", ""isCompleted"": false, ""project"": ""Valid Project Title"" }", 200, "")]
    public async Task PolymorphicType_Works(string requestBody, int expectedStatusCode, string expectedResponse)
    {
        var source = GetCompilationSource("""
app.MapPost("/todos-with-project", (TodoWithProject todosIn) => todos.Add(todosIn))
    .WithValidation();
""");
        
        var compilation = await Verify(source);
        var endpoint = GetEndpoint(compilation);
        
        await AssertEndpointBehavior(
            endpoint: endpoint,
            requestBody: requestBody,
            expectedResponse: expectedResponse,
            expectedStatusCode: expectedStatusCode);
    }

    private static string GetCompilationSource(string innerSource)
    {
        return $$"""
using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

var app = WebApplication.Create();

EndpointTest.MapTestEndpoints(app);

app.Run();

public static class EndpointTest
{
    public static IEndpointRouteBuilder MapTestEndpoints(IEndpointRouteBuilder app)
    {
        var todos = new List<Todo>();
        {{innerSource}}
        return app;
    }
}

public class Todo
{
    [Required, Range(1, int.MaxValue)]
    public int Id { get; set; }
    [Required, MinLength(3)]
    public string Title { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
}

public class TodoWithProject : Todo
{
    [Required]
    [MinLength(6)]
    public string Project { get; set; } = string.Empty;
}

public partial class Program {}
""";
    }
}