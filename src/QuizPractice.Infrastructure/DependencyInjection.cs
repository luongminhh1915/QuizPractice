using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuizPractice.Application.Abstractions;
using QuizPractice.Infrastructure.Persistence;
using QuizPractice.Infrastructure.Security;
using QuizPractice.Infrastructure.Storage;

namespace QuizPractice.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString, string webRootPath)
    {
        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddSingleton<IFileStorage>(new LocalFileStorage(webRootPath));
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        // Every XxxRepository is registered against its IXxxRepository, so adding a
        // repository needs no line here (and no merge conflict on this file).
        foreach (var type in typeof(DependencyInjection).Assembly.GetTypes()
                     .Where(t => t is { IsClass: true, IsAbstract: false } && t.Name.EndsWith("Repository")))
        {
            var contract = type.GetInterfaces().SingleOrDefault(i => i.Name == "I" + type.Name)
                ?? throw new InvalidOperationException($"{type.Name} does not implement I{type.Name}.");
            services.AddScoped(contract, type);
        }

        return services;
    }
}
