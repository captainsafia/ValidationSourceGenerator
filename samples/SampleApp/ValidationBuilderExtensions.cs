using Microsoft.AspNetCore.Builder;

public static class ValidationBuilderExtensions
{
    public static TBuilder WithValidation<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
    {
        builder.AddEndpointFilter(ValidationLookups.Validate);
        return builder;
    }
}
