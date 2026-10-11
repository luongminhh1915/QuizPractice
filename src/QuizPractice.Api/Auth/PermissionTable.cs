using QuizPractice.Application.Abstractions;
using QuizPractice.Domain.Constants;

namespace QuizPractice.Api.Auth;

/// <summary>
/// The "Permissions" section of appsettings.json: System Menu item → roles. Each item
/// becomes an authorization policy of the same name, so a controller is guarded with
/// [Authorize(Policy = SettingValues.MenuItems.Posts)] and typing the URL of a screen
/// is refused exactly when its menu item is hidden (BR-03).
/// </summary>
public sealed class PermissionTable : IPermissionTable
{
    public const string Section = "Permissions";

    private readonly IReadOnlyDictionary<string, string[]> _rolesByItem;

    public PermissionTable(IReadOnlyDictionary<string, string[]> rolesByItem) => _rolesByItem = rolesByItem;

    public bool Allows(string role, string menuItem) =>
        _rolesByItem.TryGetValue(menuItem, out var roles) && roles.Contains(role);
}

public static class PermissionTableSetup
{
    /// <summary>
    /// Reads the table and registers one policy per item. A missing item, an unknown
    /// item or an unknown role stops the app at start-up: a typo must not silently
    /// open or close a screen.
    /// </summary>
    public static IServiceCollection AddPermissionTable(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(PermissionTable.Section);
        var table = section.GetChildren().ToDictionary(
            item => item.Key,
            item => item.GetChildren().Select(r => r.Value ?? string.Empty).ToArray());

        var problems = new List<string>();
        problems.AddRange(SettingValues.MenuItems.All.Where(i => !table.ContainsKey(i)).Select(i => $"item '{i}' is missing"));
        problems.AddRange(table.Keys.Where(i => !SettingValues.MenuItems.All.Contains(i)).Select(i => $"'{i}' is not a System Menu item"));
        problems.AddRange(table.SelectMany(kv => kv.Value.Where(r => !Roles.All.Contains(r)).Select(r => $"'{r}' of '{kv.Key}' is not a role")));
        problems.AddRange(table.Where(kv => kv.Value.Length == 0).Select(kv => $"'{kv.Key}' has no role"));
        if (problems.Count > 0)
        {
            throw new InvalidOperationException(
                $"The {PermissionTable.Section} section of appsettings.json is wrong: {string.Join("; ", problems)}.");
        }

        services.AddSingleton<IPermissionTable>(new PermissionTable(table));
        services.AddAuthorization(options =>
        {
            foreach (var (item, roles) in table)
            {
                options.AddPolicy(item, policy => policy.RequireRole(roles));
            }
        });
        return services;
    }
}
