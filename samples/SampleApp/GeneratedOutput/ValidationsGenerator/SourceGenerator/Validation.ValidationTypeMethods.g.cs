using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
public static partial class Validations
{
    public static void Validate(Todo value, ref List<ValidationResult> results)
    {
        if (value.GetType() != typeof(Todo))
        {
            throw new Exception($"Expected to validate Todo but got {value.GetType()}");
        }
        var validationContext = new ValidationContext(value);
        validationContext.MemberName = "Todo.Id";
        validationContext.DisplayName = "Todo.Id";
        results.Add(Todo_Id_RequiredAttribute.GetValidationResult(value.Id, validationContext));
        validationContext.MemberName = "Todo.Id";
        validationContext.DisplayName = "Todo.Id";
        results.Add(Todo_Id_RangeAttribute.GetValidationResult(value.Id, validationContext));
        validationContext.MemberName = "Todo.Title";
        validationContext.DisplayName = "Todo.Title";
        results.Add(Todo_Title_RequiredAttribute.GetValidationResult(value.Title, validationContext));
        validationContext.MemberName = "Todo.Title";
        validationContext.DisplayName = "Todo.Title";
        results.Add(Todo_Title_MinLengthAttribute.GetValidationResult(value.Title, validationContext));
    }
    public static void Validate(IEnumerable<Todo> values, ref List<ValidationResult> results)
    {
        var validationContext = new ValidationContext(values);
        var index = 0;
        foreach (var value in values)
        {
            validationContext.MemberName = $"Todo[{index}].Id";
            validationContext.DisplayName = $"Todo[{index}].Id";
            results.Add(Todo_Id_RequiredAttribute.GetValidationResult(value.Id, validationContext));
            validationContext.MemberName = $"Todo[{index}].Id";
            validationContext.DisplayName = $"Todo[{index}].Id";
            results.Add(Todo_Id_RangeAttribute.GetValidationResult(value.Id, validationContext));
            validationContext.MemberName = $"Todo[{index}].Title";
            validationContext.DisplayName = $"Todo[{index}].Title";
            results.Add(Todo_Title_RequiredAttribute.GetValidationResult(value.Title, validationContext));
            validationContext.MemberName = $"Todo[{index}].Title";
            validationContext.DisplayName = $"Todo[{index}].Title";
            results.Add(Todo_Title_MinLengthAttribute.GetValidationResult(value.Title, validationContext));
            index++;
        }
    }
    public static void Validate(TodoWithProject value, ref List<ValidationResult> results)
    {
        if (value.GetType() != typeof(TodoWithProject))
        {
            throw new Exception($"Expected to validate TodoWithProject but got {value.GetType()}");
        }
        var validationContext = new ValidationContext(value);
        validationContext.MemberName = "TodoWithProject.Project";
        validationContext.DisplayName = "TodoWithProject.Project";
        results.Add(TodoWithProject_Project_RequiredAttribute.GetValidationResult(value.Project, validationContext));
        validationContext.MemberName = "TodoWithProject.Project";
        validationContext.DisplayName = "TodoWithProject.Project";
        results.Add(TodoWithProject_Project_MinLengthAttribute.GetValidationResult(value.Project, validationContext));
        validationContext.MemberName = "TodoWithProject.Id";
        validationContext.DisplayName = "TodoWithProject.Id";
        results.Add(TodoWithProject_Id_RequiredAttribute.GetValidationResult(value.Id, validationContext));
        validationContext.MemberName = "TodoWithProject.Id";
        validationContext.DisplayName = "TodoWithProject.Id";
        results.Add(TodoWithProject_Id_RangeAttribute.GetValidationResult(value.Id, validationContext));
        validationContext.MemberName = "TodoWithProject.Title";
        validationContext.DisplayName = "TodoWithProject.Title";
        results.Add(TodoWithProject_Title_RequiredAttribute.GetValidationResult(value.Title, validationContext));
        validationContext.MemberName = "TodoWithProject.Title";
        validationContext.DisplayName = "TodoWithProject.Title";
        results.Add(TodoWithProject_Title_MinLengthAttribute.GetValidationResult(value.Title, validationContext));
    }
    public static void Validate(IEnumerable<TodoWithProject> values, ref List<ValidationResult> results)
    {
        var validationContext = new ValidationContext(values);
        var index = 0;
        foreach (var value in values)
        {
            validationContext.MemberName = $"TodoWithProject[{index}].Project";
            validationContext.DisplayName = $"TodoWithProject[{index}].Project";
            results.Add(TodoWithProject_Project_RequiredAttribute.GetValidationResult(value.Project, validationContext));
            validationContext.MemberName = $"TodoWithProject[{index}].Project";
            validationContext.DisplayName = $"TodoWithProject[{index}].Project";
            results.Add(TodoWithProject_Project_MinLengthAttribute.GetValidationResult(value.Project, validationContext));
            validationContext.MemberName = $"TodoWithProject[{index}].Id";
            validationContext.DisplayName = $"TodoWithProject[{index}].Id";
            results.Add(TodoWithProject_Id_RequiredAttribute.GetValidationResult(value.Id, validationContext));
            validationContext.MemberName = $"TodoWithProject[{index}].Id";
            validationContext.DisplayName = $"TodoWithProject[{index}].Id";
            results.Add(TodoWithProject_Id_RangeAttribute.GetValidationResult(value.Id, validationContext));
            validationContext.MemberName = $"TodoWithProject[{index}].Title";
            validationContext.DisplayName = $"TodoWithProject[{index}].Title";
            results.Add(TodoWithProject_Title_RequiredAttribute.GetValidationResult(value.Title, validationContext));
            validationContext.MemberName = $"TodoWithProject[{index}].Title";
            validationContext.DisplayName = $"TodoWithProject[{index}].Title";
            results.Add(TodoWithProject_Title_MinLengthAttribute.GetValidationResult(value.Title, validationContext));
            index++;
        }
    }
    public static void Validate(RecursiveTodo value, ref List<ValidationResult> results)
    {
        if (value.GetType() != typeof(RecursiveTodo))
        {
            throw new Exception($"Expected to validate RecursiveTodo but got {value.GetType()}");
        }
        
        var validationContext = new ValidationContext(value);
        validationContext.MemberName = "RecursiveTodo.Id";
        validationContext.DisplayName = "RecursiveTodo.Id";
        results.Add(RecursiveTodo_Id_RequiredAttribute.GetValidationResult(value.Id, validationContext));
        validationContext.MemberName = "RecursiveTodo.Id";
        validationContext.DisplayName = "RecursiveTodo.Id";
        results.Add(RecursiveTodo_Id_RangeAttribute.GetValidationResult(value.Id, validationContext));
        validationContext.MemberName = "RecursiveTodo.Title";
        validationContext.DisplayName = "RecursiveTodo.Title";
        results.Add(RecursiveTodo_Title_RequiredAttribute.GetValidationResult(value.Title, validationContext));
        validationContext.MemberName = "RecursiveTodo.Title";
        validationContext.DisplayName = "RecursiveTodo.Title";
        results.Add(RecursiveTodo_Title_MinLengthAttribute.GetValidationResult(value.Title, validationContext));
    }
    public static void Validate(IEnumerable<RecursiveTodo> values, ref List<ValidationResult> results)
    {
        var validationContext = new ValidationContext(values);
        var index = 0;
        foreach (var value in values)
        {
            validationContext.MemberName = $"RecursiveTodo[{index}].Id";
            validationContext.DisplayName = $"RecursiveTodo[{index}].Id";
            results.Add(RecursiveTodo_Id_RequiredAttribute.GetValidationResult(value.Id, validationContext));
            validationContext.MemberName = $"RecursiveTodo[{index}].Id";
            validationContext.DisplayName = $"RecursiveTodo[{index}].Id";
            results.Add(RecursiveTodo_Id_RangeAttribute.GetValidationResult(value.Id, validationContext));
            validationContext.MemberName = $"RecursiveTodo[{index}].Title";
            validationContext.DisplayName = $"RecursiveTodo[{index}].Title";
            results.Add(RecursiveTodo_Title_RequiredAttribute.GetValidationResult(value.Title, validationContext));
            validationContext.MemberName = $"RecursiveTodo[{index}].Title";
            validationContext.DisplayName = $"RecursiveTodo[{index}].Title";
            results.Add(RecursiveTodo_Title_MinLengthAttribute.GetValidationResult(value.Title, validationContext));
            index++;
        }
    }
}
