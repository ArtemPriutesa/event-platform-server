using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Reviews.Application.Abstractions;
using Reviews.Infrastructure.Mongo;
using Reviews.Infrastructure.Repositories;
using Reviews.Infrastructure.Seeding;

namespace Reviews.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MongoDbOptions>(options =>
        {
            configuration.GetSection(MongoDbOptions.SectionName).Bind(options);
            options.ConnectionString = configuration.GetConnectionString("Reviews")
                ?? options.ConnectionString;
            if (!string.IsNullOrWhiteSpace(options.ConnectionString))
            {
                var url = MongoDB.Driver.MongoUrl.Create(options.ConnectionString);
                if (!string.IsNullOrWhiteSpace(url.DatabaseName)) options.DatabaseName = url.DatabaseName;
            }
        });
        services.AddSingleton<MongoDbContext>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IReviewQueries, ReviewQueries>();
        services.AddSingleton<ReviewDataSeeder>();
        services.AddHostedService<ReviewsStartupService>();
        return services;
    }
}
