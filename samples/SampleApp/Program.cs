using System.ComponentModel.DataAnnotations;

var app = WebApplication.Create();
var todos = new List<Todo>();

// Validating a single complex parameter
app.MapPost("/todo", (Todo todo) => todos.Add(todo))
.WithValidation((
    "/Users/captainsafia/github.com/captainsafia/ValidationSourceGenerator/samples/SampleApp/Program.cs", 7));

// Validate a single simple parameter
// app.MapPost("/todo/{id}", ([Required] [Range(1, int.MaxValue)] int id) => todos.SingleOrDefault(todo => todo.Id == id))
// .WithValidation(("/Users/captainsafia/github.com/captainsafia/ValidationSourceGenerator/samples/SampleApp/Program.cs", 12));
//
// // Validate two parameters, one simple and one complex
// app.MapPut("/todo/{id}", ([Required] [Range(1, int.MaxValue)] int id, Todo todo) =>
// {
//     var index = todos.FindIndex(todo => todo.Id == id);
//     todos[index] = todo;
// })
// .WithValidation(("/Users/captainsafia/github.com/captainsafia/ValidationSourceGenerator/samples/SampleApp/Program.cs", 16));

// Validate with IEnumerable types
app.MapPost("/todos", (List<Todo> todosIn) => todos.AddRange(todosIn))
.WithValidation(("/Users/captainsafia/github.com/captainsafia/ValidationSourceGenerator/samples/SampleApp/Program.cs", 24));

// Validate with polymorphic types
app.MapPost("/todos-with-project", (TodoWithProject todosIn) => todos.Add(todosIn))
    .WithValidation(("/Users/captainsafia/github.com/captainsafia/ValidationSourceGenerator/samples/SampleApp/Program.cs", 28));



// Don't allocate a new result list for each validate invocation
// Move validations to static class with static instance attribute newups


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