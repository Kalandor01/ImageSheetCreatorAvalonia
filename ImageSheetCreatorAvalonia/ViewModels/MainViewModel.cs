using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using DynamicData;
using ImageSheetCreatorAvalonia.Views;
using Splat;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Image = System.Drawing.Image;

namespace ImageSheetCreatorAvalonia.ViewModels;

public class MainViewModel : ViewModelBase
{
    #region Private fields
    private string _imagesInColumnString;
    private string _imagesInRowString;
    private string _aspectRatioString;
    private string _imageLimitString;
    private MainWindow _window;

    private int imagesInColumn;
    private int imagesInRow;
    private double aspectRatio;
    private int imageLimit;
    #endregion

    #region Public properies
    public string ImagesInColumn
    {
        get
        {
            return _imagesInColumnString;
        }
        set
        {
            _imagesInColumnString = int.TryParse(value, out int parsedValue) && parsedValue > 0 ? parsedValue.ToString() : _imagesInColumnString;
            imagesInColumn = int.Parse(_imagesInColumnString);
        }
    }

    public string ImagesInRow
    {
        get
        {
            return _imagesInRowString;
        }
        set
        {
            _imagesInRowString = int.TryParse(value, out int parsedValue) && parsedValue > 0 ? parsedValue.ToString() : _imagesInRowString;
            imagesInRow = int.Parse(_imagesInRowString);
        }
    }

    public string AspectRatio
    {
        get
        {
            return _aspectRatioString;
        }
        set
        {
            _aspectRatioString = double.TryParse(value, out double parsedValue) && parsedValue > 0 ? parsedValue.ToString() : _aspectRatioString;
            aspectRatio = double.Parse(_aspectRatioString);
        }
    }

    public bool FlipAspectRatio { get; set; }

    public string ImageLimit
    {
        get
        {
            return _imageLimitString;
        }
        set
        {
            _imageLimitString = string.IsNullOrWhiteSpace(value) || (int.TryParse(value, out int parsedValue) && parsedValue > 0) ? value : _imageLimitString;
            imageLimit = string.IsNullOrWhiteSpace(_imageLimitString) ? -1 : int.Parse(_imageLimitString);
        }
    }

    public string[] ImagePathsArray { get; set; }

    public string ImagePaths
    {
        get
        {
            return string.Join('\n', ImagePathsArray);
        }
        set
        {
            var imageList = value.Split('\n');
            try
            {
                foreach (var path in imageList)
                {
                    Image.FromFile(path);
                }
            }
            catch (Exception)
            {
                return;
            }
            ImagePathsArray = imageList;
        }
    }

    public ObservableCollection<ImageData> Images { get; set; }
    public bool IsEnabledAddImageButton { get => !string.IsNullOrEmpty(ImagePaths); }
    public bool IsEnabledRemoveImageButton { get => _window.ImageList.SelectedIndex != -1; }
    public bool IsEnabledCreateImageSheetButton { get => Images.Any(); }
    #endregion

    public MainViewModel()
    {
        ImagesInColumn = "1";
        ImagesInRow = "1";
        AspectRatio = (297 / 210.0).ToString();
        ImageLimit = "";
        ImagePaths = "";
        Images = [];
    }

    public void Setup(MainWindow window)
    {
        _window = window;
        _window.selectImageButton.Click += SelectImageCommand;
        _window.addImageButton.Click += AddImageCommand;
        _window.removeImageButton.Click += RemoveImageCommand;
        _window.createImageSheetButton.Click += CreateImageSheetCommand;
        _window.ImageList.SelectionChanged += ImageListSelectionChanged;
    }

    #region Commands
    private void SelectImageCommand(object? sender, RoutedEventArgs e)
    {
        SelectImage();
    }

    private async Task SelectImage()
    {
        var storageProvider = _window.StorageProvider;
        var startingFolder = await storageProvider.TryGetFolderFromPathAsync(Directory.GetCurrentDirectory());
        var fpOptions = new FilePickerOpenOptions
        {
            SuggestedStartLocation = startingFolder,
            AllowMultiple = true,
        };

        var files = await _window.StorageProvider.OpenFilePickerAsync(fpOptions);

        if (files.Count == 0)
        {
            return;
        }

        var paths = files.Select(f => f.Path.LocalPath).ToList();
        ImagePaths = string.Join("\n", paths);
        _window.ImagePathTextBox.Text = ImagePaths;
        _window.addImageButton.IsEnabled = IsEnabledAddImageButton;
    }

    private void AddImageCommand(object? sender, RoutedEventArgs e)
    {
        try
        {
            foreach (var imagePath in ImagePathsArray)
            {
                Image.FromFile(imagePath);
            }
        }
        catch (Exception)
        {
            return;
        }

        Images.AddRange(ImagePathsArray.Select(ip => new ImageData(ip, imageLimit)));
        _window.ImageList.ItemsSource = Images;
        _window.createImageSheetButton.IsEnabled = IsEnabledCreateImageSheetButton;
    }

    private void RemoveImageCommand(object? sender, RoutedEventArgs e)
    {
        if (_window.ImageList.SelectedIndex == -1)
        {
            return;
        }

        Images.RemoveAt(_window.ImageList.SelectedIndex);
        _window.createImageSheetButton.IsEnabled = IsEnabledCreateImageSheetButton;
    }

    private void CreateImageSheetCommand(object? sender, RoutedEventArgs e)
    {
        if (!Images.Any())
        {
            return;
        }

        if (FlipAspectRatio)
        {
            aspectRatio = 1 / aspectRatio;
        }

        // correct image aspect ratios
        (int width, int height) biggestSize = (0, 0);

        foreach (var targetImage in Images)
        {
            var image = targetImage.Image;

            var fullWidth = image.Width * imagesInRow;
            var fullHeight = image.Height * imagesInColumn;


            var rawAspectRatio = fullHeight / (fullWidth * 1.0);

            (int width, int height) correctedSize = (0, 0);

            if (rawAspectRatio != aspectRatio)
            {
                if (rawAspectRatio > aspectRatio)
                {
                    correctedSize = (image.Width, (int)Math.Round(image.Height / (rawAspectRatio / aspectRatio)));
                }
                else
                {
                    correctedSize = ((int)Math.Round(image.Width / (aspectRatio / rawAspectRatio)), image.Height);
                }
            }

            if (correctedSize.width > biggestSize.width)
            {
                biggestSize = correctedSize;
            }
        }

        (int width, int height) = (biggestSize.width * imagesInRow, biggestSize.height * imagesInColumn);

        // make img
        var imageIndex = 0;
        var currentImage = Images[imageIndex];

        var destImage = new Bitmap(width, height);

        using (var graphics = Graphics.FromImage(destImage))
        {
            graphics.CompositingMode = CompositingMode.SourceCopy;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            var wrapMode = new ImageAttributes();
            wrapMode.SetWrapMode(WrapMode.TileFlipXY);

            var imageNum = 0;

            for (int y = 0; y < imagesInColumn; y++)
            {
                for (int x = 0; x < imagesInRow; x++)
                {
                    var destRect = new Rectangle(x * biggestSize.width, y * biggestSize.height, biggestSize.width, biggestSize.height);
                    graphics.DrawImage(currentImage.Image, destRect, 0, 0, currentImage.Image.Width, currentImage.Image.Height, GraphicsUnit.Pixel, wrapMode);
                    if (currentImage.Limit > 0)
                    {
                        imageNum++;
                        if (imageNum >= currentImage.Limit)
                        {
                            imageIndex++;
                            if (imageIndex >= Images.Count)
                            {
                                break;
                            }
                            else
                            {
                                imageNum = 0;
                                currentImage = Images[imageIndex];
                            }
                        }
                    }
                }
                if (imageIndex >= Images.Count)
                {
                    break;
                }
            }
        }

        var imageName = "címke kép.png";
        destImage.Save(imageName, ImageFormat.Png);
        var savePath = Path.Join(Directory.GetCurrentDirectory(), imageName);
        var h = new MessageBoxWindow($"A kép elmentve ide: \"{savePath}\"");
        h.Show();
    }
    
    private void ImageListSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        _window.removeImageButton.IsEnabled = IsEnabledRemoveImageButton;
    }
    #endregion
}
