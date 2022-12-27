//HintName: Validation.ValidationAttributeInstances.g.cs
using System.ComponentModel.DataAnnotations;
public static partial class Validations
{
        private static readonly ValidationAttribute Todo_Id_RequiredAttribute = new RequiredAttribute();
    private static readonly ValidationAttribute Todo_Id_RangeAttribute = new RangeAttribute(1,2147483647);
    private static readonly ValidationAttribute Todo_Title_RequiredAttribute = new RequiredAttribute();
    private static readonly ValidationAttribute Todo_Title_MinLengthAttribute = new MinLengthAttribute(3);

}
