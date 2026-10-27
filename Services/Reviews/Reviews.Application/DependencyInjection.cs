using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Reviews.Application.Behaviors;
using Reviews.Application.Mapping;

namespace Reviews.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, string? autoMapperLicenseKey = null)
    {
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            configuration.AddOpenBehavior(typeof(ExceptionHandlingBehavior<,>));
            configuration.AddOpenBehavior(typeof(LoggingBehavior<,>));
            configuration.AddOpenBehavior(typeof(PerformanceBehavior<,>));
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddAutoMapper(configuration =>
        {
            configuration.LicenseKey = string.IsNullOrWhiteSpace(autoMapperLicenseKey)
                ? Environment.GetEnvironmentVariable("AUTOMAPPER_LICENSE_KEY")
                : autoMapperLicenseKey;
            configuration.AddProfile<ReviewsMappingProfile>();
        });
        return services;
    }
}
