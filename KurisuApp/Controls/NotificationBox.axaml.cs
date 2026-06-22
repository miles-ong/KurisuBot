using Avalonia;
using Avalonia.Controls.Primitives;
using System.Collections;

namespace KurisuApp.Controls;

public class NotificationBox : TemplatedControl
{
    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty
        = AvaloniaProperty.Register<NotificationBox, IEnumerable?>(nameof(ItemsSource));

    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }
}