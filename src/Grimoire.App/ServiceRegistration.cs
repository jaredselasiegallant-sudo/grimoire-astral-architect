using Microsoft.Extensions.DependencyInjection;
using Grimoire.Core.Interfaces;
using Grimoire.Core.Narrative;
using Grimoire.Core.Tutorial;
using Grimoire.Data.Repositories;
using Grimoire.App.Services;
using Grimoire.App.ViewModels;

namespace Grimoire.App;

/// <summary>
/// Registers all services into the DI container.
/// Called once at application startup.
/// </summary>
public static class ServiceRegistration
{
    public static IServiceProvider Configure(string dbPath)
    {
        var services = new ServiceCollection();

        // Data layer
        services.AddSingleton<IGameRepository>(new GameRepository(dbPath));

        // Core services
        services.AddSingleton<IGameStateService, GameStateService>();
        services.AddSingleton<NarrativeService>();
        services.AddSingleton<TutorialService>();
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<ISettingsService, SettingsService>();

        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<InventoryViewModel>();
        services.AddSingleton<FamiliarManagementViewModel>();
        services.AddSingleton<CraftingCauldronViewModel>();

        return services.BuildServiceProvider();
    }
}
