using Grimoire.Core.Interfaces;

namespace Grimoire.App.Services;

/// <summary>
/// In-memory notification queue. Displays toast-style messages
/// for expedition returns, crafting completions, narrative beats.
/// </summary>
public sealed class NotificationService : INotificationService
{
    public event Action<NotificationEvent>? OnNotificationQueued;

    public void Show(string title, string message, NotificationType type = NotificationType.Info, float durationSeconds = 3f)
    {
        OnNotificationQueued?.Invoke(new NotificationEvent
        {
            Title = title,
            Message = message,
            Type = type,
            DurationSeconds = durationSeconds
        });
    }

    public void ShowExpeditionComplete(string familiarName, int manaCrystals, int itemCount)
    {
        Show(
            "Expedition Complete",
            $"{familiarName} returned with {manaCrystals} Mana Crystals and {itemCount} item(s).",
            NotificationType.Success,
            4f);
    }

    public void ShowCraftComplete(string itemName)
    {
        Show(
            "Crafting Complete",
            $"Created: {itemName}",
            NotificationType.Success,
            3f);
    }

    public void ShowHatchComplete(string familiarName)
    {
        Show(
            "Egg Hatched!",
            $"A new familiar has been born — name it!",
            NotificationType.Narrative,
            5f);
    }
}
