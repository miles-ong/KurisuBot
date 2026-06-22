using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using KurisuApp.ViewModels;

namespace KurisuApp.Views;

public partial class ImageCropWindowView : Window
{
    private bool _isDragging;
    private bool _isResizing;
    private Point _lastPos;

    public Bitmap? Result => _vm.Result;
    private ImageCropWindowViewModel _vm => (ImageCropWindowViewModel)DataContext!;

    public ImageCropWindowView(Bitmap source)
    {
        InitializeComponent();

        ImageCropWindowViewModel vm = new(source)
        {
            RequestClose = Close
        };

        DataContext = vm;
    }

    private void OnFramePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _isDragging = true;
        _lastPos = e.GetPosition(CropCanvas);
        e.Pointer.Capture(CropFrame);
    }

    private void OnFramePointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isDragging) return;

        Point curr = e.GetPosition(CropCanvas);
        _vm.MoveFrame(curr.X - _lastPos.X, curr.Y - _lastPos.Y);
        _lastPos = curr;
    }

    private void OnFramePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isDragging = false;
        e.Pointer.Capture(null);
    }

    private void OnHandlePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _isResizing = true;
        _lastPos = e.GetPosition(CropCanvas);
        e.Pointer.Capture(ResizeHandle);
        e.Handled = true;
    }

    private void OnHandlePointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isResizing) return;

        Point curr = e.GetPosition(CropCanvas);
        _vm.ResizeFrame(curr.X - _lastPos.X);
        _lastPos = curr;
    }

    private void OnHandlePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isResizing = false;
        e.Pointer.Capture(null);
    }
}