//HintName: Validation.ValidationTypeMethods.g.cs
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
}public static void Validate(IEnumerable<Todo> values, ref List<ValidationResult> results)
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
}
