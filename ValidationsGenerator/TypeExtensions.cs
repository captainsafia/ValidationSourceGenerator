using System.Reflection;

namespace ValidationsGenerator;

public static class TypeExtensions
{
    public static bool TryGetTopLevelValidations(this ParameterInfo parameter, out IEnumerable<CustomAttributeData> attributes)
    {
        attributes =
            parameter.CustomAttributes.Where(attr =>
                attr.AttributeType.BaseType.Name
                    .Contains("ValidationAttribute"));
        return attributes.Any();
    }
}