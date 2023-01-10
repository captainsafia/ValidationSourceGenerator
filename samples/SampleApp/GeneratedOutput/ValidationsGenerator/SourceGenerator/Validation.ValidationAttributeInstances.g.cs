using System.ComponentModel.DataAnnotations;
public static partial class Validations
{
    private static readonly ValidationAttribute Todo_Id_RequiredAttribute = new RequiredAttribute();
    private static readonly ValidationAttribute Todo_Id_RangeAttribute = new RangeAttribute(1,2147483647);
    private static readonly ValidationAttribute Todo_Title_RequiredAttribute = new RequiredAttribute();
    private static readonly ValidationAttribute Todo_Title_MinLengthAttribute = new MinLengthAttribute(3);
    private static readonly ValidationAttribute TodoWithProject_Project_RequiredAttribute = new RequiredAttribute();
    private static readonly ValidationAttribute TodoWithProject_Project_MinLengthAttribute = new MinLengthAttribute(6);
    private static readonly ValidationAttribute TodoWithProject_Id_RequiredAttribute = new RequiredAttribute();
    private static readonly ValidationAttribute TodoWithProject_Id_RangeAttribute = new RangeAttribute(1,2147483647);
    private static readonly ValidationAttribute TodoWithProject_Title_RequiredAttribute = new RequiredAttribute();
    private static readonly ValidationAttribute TodoWithProject_Title_MinLengthAttribute = new MinLengthAttribute(3);
    private static readonly ValidationAttribute RecursiveTodo_Id_RequiredAttribute = new RequiredAttribute();
    private static readonly ValidationAttribute RecursiveTodo_Id_RangeAttribute = new RangeAttribute(1,2147483647);
    private static readonly ValidationAttribute RecursiveTodo_Title_RequiredAttribute = new RequiredAttribute();
    private static readonly ValidationAttribute RecursiveTodo_Title_MinLengthAttribute = new MinLengthAttribute(3);

}
