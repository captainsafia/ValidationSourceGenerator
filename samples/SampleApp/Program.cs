using System.ComponentModel.DataAnnotations;

var app = WebApplication.Create();
var todos = new List<Todo>();
var projects = new List<Project>();

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
app.MapPost("/todos", (List<Todo> todosIn) => todos.AddRange(todosIn))
    .WithValidation();

// Validate with polymorphic types
app.MapPost("/todos-with-project", (TodoWithProject todosIn) => todos.Add(todosIn))
    .WithValidation();

// Validate with recursive types
app.MapPost("/projects", (Project project) =>
{
    projects.Add(project);
    return Results.Ok(project);
});

// Validate with IValidatableObjects


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

public class TodoList
{
    public string ListName { get; set; }
    [EmailAddress]
    public string OwnerEmail { get; set; }
    public List<Todo> Tasks { get; set; }
}

public class Board
{
    public string BoardName { get; set; }
    [Phone]
    public string BoardContact { get; set; }
    public List<TodoList> TodoLists { get; set; }
}

public class Project
{
    public string ProjectName { get; set; }
    [Url]
    public string ProjectUrl { get; set; }
    public List<Board> Boards { get; set; }
}