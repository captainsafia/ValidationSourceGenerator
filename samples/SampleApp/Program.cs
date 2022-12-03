using System.ComponentModel.DataAnnotations;

var app = WebApplication.Create();
var todos = new List<Todo>();

// Validating a single complex parameter
app.MapPost("/todo", (Todo todo) => todos.Add(todo))
    .WithValidation();

// Validate a single simple parameter
app.MapPost("/todo/{id}", ([Required, Range(1, int.MaxValue)] int id) => todos.SingleOrDefault(todo => todo.Id == id));

// Validate two parameters, one simple and one complex
app.MapPut("/todo/{id}", ([Required, Range(1, int.MaxValue)] int id, Todo todo) =>
{
    int index = todos.FindIndex(todo => todo.Id == id);
    todos[index] = todo;
});

// Validate with IValidatableObjects

// Detecting validate on a group
var group = app.MapGroup("/todos/{id}").WithValidation();

app.Run();

class Todo
{
    [Required, Range(1, int.MaxValue)]
    public int Id { get; set; }
    [Required, MinLength(3)]
    public string Title { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
}