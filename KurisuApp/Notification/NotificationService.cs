using Avalonia.Threading;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace KurisuApp.Notification
{
    public sealed class NotificationService
    {
        public ObservableCollection<Notification> Notifications { get; } = new();

        public void ShowSuccess(string message)
            => Show(message, NotificationType.Success);

        public void ShowFail(string message)
            => Show(message, NotificationType.Fail);

        public void ShowWarning(string message)
            => Show(message, NotificationType.Warning);

        public void ShowInfo(string message)
            => Show(message, NotificationType.Info);

        private async void Show(string message, NotificationType type)
        {
            Notification notification = new(message, type);

            Dispatcher.UIThread.Post(() =>
            {
                Notifications.Add(notification);
            });

            await Task.Delay(1);

            notification.Opacity = 1;

            await Task.Delay(4000);

            Dispatcher.UIThread.Post(() => notification.Opacity = 0);

            await Task.Delay(300);

            Dispatcher.UIThread.Post(() => Notifications.Remove(notification));
        }
    }
}
