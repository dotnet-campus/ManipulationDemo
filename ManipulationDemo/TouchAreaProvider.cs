using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Media3D;
using System.Windows.Media;
using System.Threading.Tasks;

namespace ManipulationDemo;

class TouchAreaProvider
{
    public TouchAreaProvider(Grid rootGrid)
    {
        RootGrid = rootGrid;
    }

    public Grid RootGrid { get; }
    private readonly Dictionary<int /*Id*/, PointInfo> _dictionary = [];

    public void Down(int id, Point position, Size size)
    {
        var (width, height) = (size.Width, size.Height);
        Border border = new Border
        {
            Background = new SolidColorBrush(Colors.Gray)
            {
                Opacity = 0.5
            },
            Width = Math.Max(5, width),
            Height = Math.Max(5, height),
            IsHitTestVisible = false,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            RenderTransform = new TranslateTransform(position.X, position.Y),
        };
        RootGrid.Children.Add(border);

        var textBlock = new TextBlock()
        {
            IsHitTestVisible = false,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            TextWrapping = TextWrapping.Wrap,
            RenderTransform = new TranslateTransform(position.X, position.Y),
        };
        RootGrid.Children.Add(textBlock);

        _dictionary[id] = new PointInfo(position.X, position.Y, width, height, border, textBlock);
    }

    public void Move(int id, Point position, Size size, string? errorMessage)
    {
        var (width, height) = (size.Width, size.Height);
        if (_dictionary.TryGetValue(id, out var info))
        {
#nullable enable
            var viewWidth = width;
            var viewHeight = height;

            if (width <= 0.01)
            {
                width = info.Width;
                viewWidth = width;
            }

            if (height <= 0.01)
            {
                height = info.Height;
                viewHeight = height;
            }

            viewWidth = Math.Max(5, viewWidth);
            viewHeight = Math.Max(5, viewHeight);

            var borderRenderTransform = (TranslateTransform)info.Border.RenderTransform!;
            borderRenderTransform.X = position.X - viewWidth / 2;
            borderRenderTransform.Y = position.Y - viewHeight / 2;

            var textBlock = info.TextBlock;
            textBlock.Text =
                $"Id:{id}\r\nX: {position.X}, Y: {position.Y}\r\nWidth: {width:F2}, Height: {height:F2}\r\n{errorMessage}";

            var textBlockRenderTransform = (TranslateTransform)textBlock.RenderTransform!;
            textBlockRenderTransform.X = position.X;
            textBlockRenderTransform.Y = position.Y;

            _dictionary[id] = info with
            {
                X = position.X,
                Y = position.Y,

                Width = width,
                Height = height
            };
#nullable restore
        }
    }

    public async void Up(int id)
    {
        if (_dictionary.TryGetValue(id, out var info))
        {
            RootGrid.Children.Remove(info.Border);

            info.TextBlock.Text = "IsUp=true\r\n" + info.TextBlock.Text;
            info.TextBlock.Opacity = 0.9;

            await Task.Delay(TimeSpan.FromSeconds(5));
            info.TextBlock.Foreground = Brushes.Black;
            info.TextBlock.Opacity = 0.5;
            for (int i = 0; i < 5; i++)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                info.TextBlock.Opacity = (5 - i) * 0.1;
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
            RootGrid.Children.Remove(info.TextBlock);
        }
    }
}

struct PointInfo
{
    public PointInfo(double x, double y, double width, double height, Border border, TextBlock textBlock)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
        Border = border;
        TextBlock = textBlock;
    }

    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public Border Border { get; }
    public TextBlock TextBlock { get; }
}