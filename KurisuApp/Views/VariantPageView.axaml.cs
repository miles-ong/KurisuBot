using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using KurisuApp.ViewModels;
using SkiaSharp;
using System.IO;

namespace KurisuApp.Views;

public partial class VariantPageView : UserControl
{
    public VariantPageView()
    {
        InitializeComponent();

        AttachedToVisualTree += (_, _) =>
        {
            if (DataContext is VariantPageViewModel vm)
            {
                CharacterSearchBox.AsyncPopulator = vm.SearchCharactersAsync;
            }
        };
    }

    private async void OnUploadImageClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);

        if (topLevel is not Window owner) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select variant image",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Images")
                {
                    Patterns = ["*.png", "*.jpg", "*.jpeg", "*.webp"]
                }
            ]
        });

        if (files.Count == 0) return;

        var file = files[0];
        Bitmap source;

        using (Stream read = await file.OpenReadAsync())
        {
            source = new Bitmap(read);
        }

        PixelSize size = source.PixelSize;
        const int minWidth = 400;
        const int minHeight = 600;

        if (size.Width < minWidth || size.Height < minHeight)
        {
            if (DataContext is VariantPageViewModel model)
            {
                model.NotifyImageError($"Image must be at least {minWidth}x{minHeight}. Selected image is {size.Width}x{size.Height}.");
            }
            return;
        }

        Bitmap final;

        if (size.Width == minWidth && size.Height == minHeight)
        {
            final = source;
        }
        else
        {
            ImageCropWindowView cropWindow = new(source);
            await cropWindow.ShowDialog(owner);

            if (cropWindow.Result is null) return;

            final = cropWindow.Result;
        }

        using MemoryStream pngStream = new();
        final.Save(pngStream);
        pngStream.Position = 0;

        using SKBitmap bitmap = SKBitmap.Decode(pngStream);
        using SKImage image = SKImage.FromBitmap(bitmap);
        using SKData data = image.Encode(SKEncodedImageFormat.Webp, 90);

        using MemoryStream webpStream = new();
        data.SaveTo(webpStream);
        webpStream.Position = 0;

        if (DataContext is VariantPageViewModel vm) await vm.UploadVariantImageAsync(webpStream, "image/webp");
    }
}