namespace Grimoire.Core.Interfaces;

/// <summary>
/// Manages user-facing notifications (toast-style messages).
/// Non-blocking overlays for expedition returns, achievements, etc.
/// </summary>
public interface INotificationService
{
    void Show(string title, string message, NotificationType type = NotificationType.Info, float durationSeconds = 3f);
    void ShowExpeditionComplete(string familiarName, int manaCrystals, int itemCount);
    void ShowCraftComplete(string itemName);
    void ShowHatchComplete(string familiarName);
    event Action<NotificationEvent>? OnNotificationQueued;
}

public enum NotificationType
{
    Info,
    Success,
    Warning,
    Narrative
}

public sealed class NotificationEvent
{
    public required string Title { get; init; }
    public required string Message { get; init; }
    public NotificationType Type { get; init; }
    public float DurationSeconds { get; init; } = 3f;
}
