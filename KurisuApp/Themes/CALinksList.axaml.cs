using Avalonia;
using Avalonia.Controls.Primitives;
using System.Collections;
using System.Windows.Input;

namespace KurisuApp.Themes;

public class CALinksList : TemplatedControl
{
    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty
        = AvaloniaProperty.Register<CALinksList, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<ICommand?> RemoveCommandProperty
        = AvaloniaProperty.Register<CALinksList, ICommand?>(nameof(RemoveCommand));

    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public ICommand? RemoveCommand
    {
        get => GetValue(RemoveCommandProperty);
        set => SetValue(RemoveCommandProperty, value);
    }
}