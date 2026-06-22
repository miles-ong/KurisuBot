using CommunityToolkit.Mvvm.ComponentModel;

namespace KurisuApp.Notification
{
    public partial class Notification : ObservableObject
    {
        public string Message { get; }
        public NotificationType Type { get; }

        [ObservableProperty]
        private double _opacity = 0;

        public bool IsSuccess => Type == NotificationType.Success;
        public bool IsFail => Type == NotificationType.Fail;
        public bool IsWarning => Type == NotificationType.Warning;
        public bool IsInfo => Type == NotificationType.Info;
        
        
        public Notification(string message, NotificationType type)
        {
            Message = message;
            Type = type;
        }
    }
}
