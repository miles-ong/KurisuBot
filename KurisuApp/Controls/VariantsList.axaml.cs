using Avalonia;
using Avalonia.Controls.Primitives;
using System.Collections;
using System.Windows.Input;

namespace KurisuApp.Controls;

public class VariantsList : TemplatedControl
{
    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty
        = AvaloniaProperty.Register<VariantsList, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<ICommand?> EditCommandProperty
        = AvaloniaProperty.Register<VariantsList, ICommand?>(nameof(EditCommand));

    public static readonly StyledProperty<ICommand?> RemoveCommandProperty
        = AvaloniaProperty.Register<VariantsList, ICommand?>(nameof(RemoveCommand));

    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public ICommand? EditCommand
    {
        get => GetValue(EditCommandProperty);
        set => SetValue(EditCommandProperty, value);
    }

    public ICommand? RemoveCommand
    {
        get => GetValue(RemoveCommandProperty);
        set => SetValue(RemoveCommandProperty, value);
    }
}