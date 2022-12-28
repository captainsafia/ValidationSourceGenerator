using System.ComponentModel.DataAnnotations;

var app = WebApplication.Create();
var todos = new List<Todo>();

// Validating a single complex parameter
app.MapPost("/todo", (Todo todo) => todos.Add(todo))
    .WithValidation();

// Validate a single simple parameter
app.MapPost("/todo/{id}", ([Required] [Range(1, int.MaxValue)] int id) => todos.SingleOrDefault(todo => todo.Id == id))
    .WithValidation();

// // Validate two parameters, one simple and one complex
app.MapPut("/todo/{id}", ([Required] [Range(1, int.MaxValue)] int id, Todo todo) =>
    {
        var index = todos.FindIndex(todo => todo.Id == id);
        todos[index] = todo;
    })
    .WithValidation();

// Validate with IEnumerable types
// For each validatable type, we produce a `Validate` overload that takes `IEnumerable<T>`
app.MapPost("/todos", (List<Todo> todosIn) => todos.AddRange(todosIn))
    .WithValidation();

// Validate with polymorphic types
// Under the hood, we produce two `Validate` calls. One that takes a `TodoWithProject` and another
// that takes `Todo`.
app.MapPost("/todos-with-project", (TodoWithProject todosIn) => todos.Add(todosIn))
    .WithValidation();

// Validate with recursive types
// In MVC, when MaxValidationDepth is lower than the amount of recursion in the stack, then
// an exception will be thrown. In this implementation, we present a warning to the user.
app.MapPost("/recursive-todos", (RecursiveTodo todo) => Results.Ok("Valid!"));

// Detecting validate on a group
// var group = app.MapGroup("/todos/{id}").WithValidation();

app.Run();

public class Todo
{
    [Required] [Range(1, int.MaxValue)] public int Id { get; set; }

    [Required] [MinLength(3)] public string Title { get; set; } = string.Empty;

    public bool IsCompleted { get; set; }
}

public class TodoWithProject : Todo
{
    [Required]
    [MinLength(6)]
    public string Project { get; set; } = string.Empty;
}

public class RecursiveTodo
{
    [Required, Range(1, int.MaxValue)]
    public int Id { get; set; }
    public bool IsCompleted { get; set; }
    [Required, MinLength(3)]
    public string Title { get; set; } = string.Empty;
    public RecursiveTodo ReferencedTodo { get; set; }
}