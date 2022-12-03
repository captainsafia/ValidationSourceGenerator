namespace SourceGeneratorTemplate.Tests;

[UsesVerify]
public class SourceGeneratorTests
{
    [Fact]
    public Task BasicScenarioWorks()
    {
        var source = """
using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;

var app = WebApplication.Create();
var todos = new List<Todo>();

app.MapPost("/todo", (Todo todo) => todos.Add(todo))
    .WithValidation();

app.Run();

class Todo
{
    [Required, Range(1, int.MaxValue)]
    public int Id { get; set; }
    [Required, MinLength(3)]
    public string Title { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
}
""";
        var validationBuilderExtensions = """
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

public static class ValidationBuilderExtensions
{
    public static TBuilder WithValidation<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
    {
        var @delegate = ValidationLookups.Get("ValidateTodo");
        if (@delegate is not null)
            builder.AddEndpointFilter(@delegate);
        return builder;
    }
}
""";
        var validationLookups = """
#nullable enable
using System;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

public static partial class ValidationLookups
{
    public static Func<EndpointFilterInvocationContext, EndpointFilterDelegate, ValueTask<object?>>? Get(string key)
    {
        return null;
    }
}
""";
        return TestHelper.Verify(source, validationBuilderExtensions, validationLookups);
    }
}