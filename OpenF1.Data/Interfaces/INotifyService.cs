namespace OpenF1.Data;

public interface INotifyService
{
    /// <summary>
    /// Sends a notification to any registered handlers.
    /// This should only be used to alert a user to some important action, and not to transmit any data.
    /// </summary>
    public void SendNotification();

    /// <summary>
    /// Registers the provided <paramref name="action"/> as a handler for any notification that is sent.
    /// Only a single handler can be registered at any one time.
    /// Repeat calls will result in the previously registered handler being overridden.
    /// </summary>
    /// <param name="action">The action to call when a notification has been sent.</param>
    public void RegisterNotificationHandler(Action action);
}
