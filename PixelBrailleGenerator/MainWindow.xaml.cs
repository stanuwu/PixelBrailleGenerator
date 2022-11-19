using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using FontFamily = System.Windows.Media.FontFamily;

namespace PixelBrailleGenerator;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private const int Thresh = 127;
    private const int Cw = 2;
    private const int Ch = 3;

    private readonly Dictionary<int, char> _charMap = new()
    {
        { 000000, '⠀' },
        { 011101, '⠮' },
        { 000010, '⠐' },
        { 001111, '⠼' },
        { 110101, '⠫' },
        { 100101, '⠩' },
        { 111101, '⠯' },
        { 001000, '⠄' },
        { 111011, '⠷' },
        { 011111, '⠾' },
        { 100001, '⠡' },
        { 001101, '⠬' },
        { 000001, '⠠' },
        { 001001, '⠤' },
        { 000101, '⠨' },
        { 001100, '⠌' },
        { 001011, '⠴' },
        { 010000, '⠂' },
        { 011000, '⠆' },
        { 010010, '⠒' },
        { 010011, '⠲' },
        { 010001, '⠢' },
        { 011010, '⠖' },
        { 011011, '⠶' },
        { 011001, '⠦' },
        { 001010, '⠔' },
        { 100011, '⠱' },
        { 000011, '⠰' },
        { 110001, '⠣' },
        { 111111, '⠿' },
        { 001110, '⠜' },
        { 100111, '⠹' },
        { 000100, '⠈' },
        { 100000, '⠁' },
        { 110000, '⠃' },
        { 100100, '⠉' },
        { 100110, '⠙' },
        { 100010, '⠑' },
        { 110100, '⠋' },
        { 110110, '⠛' },
        { 110010, '⠓' },
        { 010100, '⠊' },
        { 010110, '⠚' },
        { 101000, '⠅' },
        { 111000, '⠇' },
        { 101100, '⠍' },
        { 101110, '⠝' },
        { 101010, '⠕' },
        { 111100, '⠏' },
        { 111110, '⠟' },
        { 111010, '⠗' },
        { 011100, '⠎' },
        { 011110, '⠞' },
        { 101001, '⠥' },
        { 111001, '⠧' },
        { 010111, '⠺' },
        { 101101, '⠭' },
        { 101111, '⠽' },
        { 101011, '⠵' },
        { 010101, '⠪' },
        { 110011, '⠳' },
        { 110111, '⠻' },
        { 000110, '⠘' },
        { 000111, '⠸' }
    };

    private Bitmap? _image;

    public MainWindow()
    {
        InitializeComponent();
        OutTextBlock.FontFamily = new FontFamily(System.Drawing.FontFamily.GenericMonospace.GetName(0));
    }

    private int GetSize()
    {
        try
        {
            return Convert.ToInt32(SizeTextBox.Text);
        }
        catch (FormatException)
        {
            MessageBox.Show("Error: Invalid Size", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            throw;
        }
    }

    private BitmapSource BitmapToImage(Bitmap bitmap)
    {
        return Imaging.CreateBitmapSourceFromHBitmap(bitmap.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
    }

    private Bitmap ScaleImage(Image bitmap)
    {
        double scale = Math.Max(bitmap.Width / (float)Cw, bitmap.Height / (float)Ch) / GetSize();
        var image = new Bitmap((int)Math.Ceiling(bitmap.Width / scale), (int)Math.Ceiling(bitmap.Height / scale));
        var graphics = Graphics.FromImage(image);
        graphics.InterpolationMode = InterpolationMode.High;
        graphics.CompositingQuality = CompositingQuality.HighQuality;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.DrawImage(bitmap, new RectangleF(0, 0, image.Width, image.Height));
        return image;
    }

    private Bitmap FilterImage(Bitmap bitmap)
    {
        var filtered = new Bitmap(bitmap.Width, bitmap.Height);
        for (var y = 0; y < filtered.Height; y++)
        for (var x = 0; x < filtered.Width; x++)
        {
            var c = bitmap.GetPixel(x, y);
            var gs = (int)(c.R * 0.3 + c.G * 0.59 + c.B * 0.11);
            filtered.SetPixel(x, y, gs < Thresh ? Color.Black : Color.White);
        }

        return filtered;
    }

    private void UIElement_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var fileDialog = new OpenFileDialog
        {
            Multiselect = false,
            Title = "Select Image",
            Filter = "Image files (*.png;*.jpeg;*.jpg;*.gif;*.bmp)|*.png;*.jpeg;*.jpg;*.gif;*.bmp|All files (*.*)|*.*"
        };
        if (fileDialog.ShowDialog() != true) return;
        try
        {
            _image = new Bitmap(fileDialog.FileName);
            PreviewImage.Source = BitmapToImage(_image);
        }
        catch (Exception)
        {
            // ignored
        }
    }

    private void ConvertButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_image == null)
        {
            MessageBox.Show("Error: No Image Loaded", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var final = FilterImage(ScaleImage(_image));
        var outString = new StringBuilder();
        for (var y = 0; y < final.Height; y += Ch)
        {
            for (var x = 0; x < final.Width; x += Cw)
            {
                var colorCode = 0;
                var value = 100000;
                for (var lx = 0; lx < Cw; lx++)
                for (var ly = 0; ly < Ch; ly++)
                    try
                    {
                        if (final.GetPixel(x + lx, y + ly).ToArgb() == Color.White.ToArgb()) colorCode += value;
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        // ignored
                    }
                    finally
                    {
                        value /= 10;
                    }

                outString.Append(_charMap[colorCode]);
            }

            outString.Append('\n');
        }

        OutTextBlock.Text = outString.ToString();
    }

    private void UIElement_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        Clipboard.SetText(OutTextBlock.Text);
    }
}