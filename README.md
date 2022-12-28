
# Validations Source Generator

This repo contains code for an incremental source generator that generates code that validates parameters on a minimal endpoint and produces `ProblemDetails` if validations are not passed.


## Usage/Examples

To opt-in to code generation for validations, invoke `WithValidation` on an
endpoint that requires validation.

```csharp
// Validating a single complex parameter
app.MapPost("/todo", (Todo todo) => todos.Add(todo))
    .WithValidation();
```


## Running Tests

To run tests, run the following command inside the `ValidationsGenerator.Tests` directory:

```shell
dotnet test
```

To run the sample app for this generator, execute the following command in the `samples/SampleApp` directory:

```shell
dotnet run
```

## License

[MIT](https://choosealicense.com/licenses/mit/)

