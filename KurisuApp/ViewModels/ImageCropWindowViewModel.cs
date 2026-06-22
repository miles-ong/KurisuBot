using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;

namespace KurisuApp.ViewModels
{
    public partial class ImageCropWindowViewModel : ViewModelBase
    {
        public const int OutputWidth = 400;
        public const int OutputHeight = 600;
        public const double Aspect = (double)OutputWidth / OutputHeight;

        private readonly Bitmap _source;
        private double _displayScale;

        public Bitmap SourceBitmap => _source;
        public Bitmap? Result { get; private set; }
        public Action? RequestClose { get; set; }

        [ObservableProperty]
        private double _canvasWidth;

        [ObservableProperty]
        private double _canvasHeight;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HandleX))]
        private double _frameX;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HandleY))]
        private double _frameY;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HandleX))]
        private double _frameWidth;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HandleY))]
        private double _frameHeight;

        public double HandleX => FrameX + FrameWidth - 8;
        public double HandleY => FrameY + FrameHeight - 8;

        public ImageCropWindowViewModel(Bitmap source)
        {
            _source = source;
            SetupCanvas();
        }

        public void MoveFrame(double x, double y)
        {
            FrameX = Math.Clamp(FrameX + x, 0, CanvasWidth - FrameWidth);
            FrameY = Math.Clamp(FrameY + y, 0, CanvasHeight - FrameHeight);
        }

        public void ResizeFrame(double w)
        {
            double minW = OutputWidth * _displayScale;
            double maxWByCanvas = CanvasWidth - FrameX;
            double maxWByHeight = (CanvasHeight - FrameY) * Aspect;
            double maxAllowedW = Math.Min(maxWByCanvas, maxWByHeight);
            double newW = Math.Clamp(FrameWidth + w, minW, maxAllowedW);

            FrameWidth = newW;
            FrameHeight = newW / Aspect;
        }

        private void SetupCanvas()
        {
            PixelSize size = _source.PixelSize;
            const double maxDisplayWidth = 1920;
            const double maxDisplayHeight = 1080;

            double scaleX = maxDisplayWidth / size.Width;
            double scaleY = maxDisplayHeight / size.Height;
            _displayScale = Math.Min(1.0, Math.Min(scaleX, scaleY));

            CanvasWidth = size.Width * _displayScale;
            CanvasHeight = size.Height * _displayScale;

            double frameHeight = CanvasHeight;
            double frameWidth = frameHeight * Aspect;

            if (frameWidth > CanvasWidth)
            {
                frameWidth = CanvasWidth;
                frameHeight = frameWidth / Aspect;
            }

            double minFrameWidth = OutputWidth * _displayScale;
            double minFrameHeight = OutputHeight * _displayScale;
            frameWidth = Math.Max(frameWidth, minFrameWidth);
            frameHeight = Math.Max(frameHeight, minFrameHeight);

            FrameWidth = frameWidth;
            FrameHeight = frameHeight;
            FrameX = (CanvasWidth - frameWidth) / 2;
            FrameY = (CanvasHeight - frameHeight) / 2;
        }
        
        [RelayCommand]
        private void Crop()
        {
            Dispatcher.UIThread.Post(() =>
            {
                Rect sourceRect = new(
                    FrameX / _displayScale,
                    FrameY / _displayScale,
                    FrameWidth / _displayScale,
                    FrameHeight / _displayScale);

                PixelSize dimensions = new PixelSize(OutputWidth, OutputHeight);
                int dpi = 96;
                RenderTargetBitmap renderTarget = new(dimensions, new Vector(dpi, dpi));

                using (DrawingContext ctx = renderTarget.CreateDrawingContext()) ctx.DrawImage(_source, sourceRect, new Rect(0, 0, OutputWidth, OutputHeight));

                Result = renderTarget;
                RequestClose?.Invoke();
            }, DispatcherPriority.Background);
        }

        [RelayCommand]
        private void Cancel()
        {
            Result = null;
            RequestClose?.Invoke();
        }
    }
}
