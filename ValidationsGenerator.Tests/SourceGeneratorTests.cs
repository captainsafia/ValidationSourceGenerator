namespace ValidationsGenerator.Tests;

[UsesVerify]
public class SourceGeneratorTests
{
    [Fact]
    public Task BasicComplexType_Works()
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

app.MapPost("/todo-2", (Todo todo) => todos.Add(todo))
    .WithValidation();

app.MapPost("/todo", (TodoWithProject todo) => todos.Add(todo))
    .WithValidation();

app.Run();

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
""";
        return TestHelper.Verify(source);
    }
    
    [Fact]
    public Task SimpleType_Works()
    {
        var source = """
using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;

var app = WebApplication.Create();

app.MapPost("/todo", ([Required, Range(1, int.MaxValue)] int id) => id)
    .WithValidation();

app.Run();
""";
        
        return TestHelper.Verify(source);
    }
}