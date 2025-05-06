using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Win32.UI.WindowsAndMessaging;
using Windows.Win32.Foundation;
using static Windows.Win32.PInvoke;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using System.Runtime.Versioning;
using TouchSizeAvalonia.PointerConverters;
using Point = Avalonia.Point;
using Windows.Win32.UI.Input.Pointer;

namespace TouchSizeAvalonia.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        PointerPressed += MainView_PointerPressed;
        PointerMoved += MainView_PointerMoved;
        PointerReleased += MainView_PointerReleased;

        Loaded += MainView_Loaded;
    }


    readonly record struct PointInfo
    (
        double X,
        double Y,
        double Width,
        double Height,
        Border Border,
        TextBlock TextBlock
    );

    private readonly Dictionary<int /*Id*/, PointInfo> _dictionary = [];

    private void MainView_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (SwitchRawPointerToggleButton.IsChecked is true)
        {
            if (_rawPointerTextBlock is not null && e.Pointer.Type != PointerType.Mouse)
            {
                var (x, y) = e.GetPosition(null);
                _rawPointerTextBlock.Text += $"\r\n[Avalonia PointerPressed] Id={e.Pointer.Id} XY={x:0.00},{y:0.00}";
            }

            return;
        }

        var currentPoint = e.GetCurrentPoint(null);
        var position = currentPoint.Position;
        (bool success, double width, double height) = GetSize(currentPoint);
        //if (!success)
        //{
        //    width = 5;
        //    height = 5;
        //}

        Border border = new Border
        {
            Background = new SolidColorBrush(Colors.Gray, 0.5),
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

        _dictionary[e.Pointer.Id] = new PointInfo(position.X, position.Y, width, height, border, textBlock);
    }

    private void MainView_PointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        if (SwitchRawPointerToggleButton.IsChecked is true)
        {
            if (_rawPointerTextBlock is not null && e.Pointer.Type != PointerType.Mouse)
            {
                var (x, y) = e.GetPosition(null);
                _rawPointerTextBlock.Text += $"\r\n[Avalonia PointerMoved] Id={e.Pointer.Id} XY={x:0.00},{y:0.00}";
            }

            return;
        }

        if (_dictionary.TryGetValue(e.Pointer.Id, out var info))
        {
            var currentPoint = e.GetCurrentPoint(null);
            var position = currentPoint.Position;
            var (success, width, height) = GetSize(currentPoint);

            string? errorMessage = null;
            if (double.IsNaN(width))
            {
                errorMessage += "Origin width is NaN\r\n";
            }

            if (double.IsNaN(height))
            {
                errorMessage += "Origin height is NaN\r\n";
            }

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

            var borderRenderTransform = (TranslateTransform) info.Border.RenderTransform!;
            borderRenderTransform.X = position.X - viewWidth / 2;
            borderRenderTransform.Y = position.Y - viewHeight / 2;

            var textBlock = info.TextBlock;
            textBlock.Text =
                $"Id:{e.Pointer.Id}\r\nX: {position.X}, Y: {position.Y}\r\nWidth: {width:F2}, Height: {height:F2}\r\n{errorMessage}";

            var textBlockRenderTransform = (TranslateTransform) textBlock.RenderTransform!;
            textBlockRenderTransform.X = position.X;
            textBlockRenderTransform.Y = position.Y;

            _dictionary[e.Pointer.Id] = info with
            {
                X = position.X,
                Y = position.Y,

                Width = width,
                Height = height
            };
        }
    }

    private async void MainView_PointerReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
    {
        if (_dictionary.TryGetValue(e.Pointer.Id, out var info))
        {
            RootGrid.Children.Remove(info.Border);

            //var animation = new Animation()
            //{
            //    Duration = TimeSpan.FromSeconds(2),
            //    IterationCount = new IterationCount(1),
            //    Children =
            //    {
            //        new KeyFrame()
            //        {
            //            Setters =
            //            {
            //                new Setter(OpacityProperty,value:1),
            //            },
            //            KeyTime = TimeSpan.FromSeconds(0)
            //        },
            //        new KeyFrame()
            //        {
            //            Setters =
            //            {
            //                new Setter(OpacityProperty,value:0),
            //            },
            //            KeyTime = TimeSpan.FromSeconds(10)
            //        }
            //    }
            //};

            //animation.RunAsync(info.TextBlock).ContinueWith(_ =>
            //{
            //    RootGrid.Children.Remove(info.TextBlock);
            //});

            //info.TextBlock.Foreground = Brushes.Gray;
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

    private (bool Success, double Width, double Height) GetSize(PointerPoint currentPoint)
    {
        try
        {
            var d = currentPoint.Properties;
            Rect contactRect = d.ContactRect;

            return (Success: true, contactRect.Width, contactRect.Height);
        }
        catch
        {
            return (Success: false, 0, 0);
        }
    }

    private void SwitchRawPointerToggleButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (SwitchRawPointerToggleButton.IsChecked is true)
        {
            SwitchRawPointerToggleButton.Content = "Use Avalonia Pointer";

            if (_rawPointerBorder is null)
            {
                _rawPointerTextBlock = new TextBlock
                {
                    IsHitTestVisible = false,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                };

                RootGrid.Children.Add(_rawPointerTextBlock);
            }

            if (_rawPointerBorder is null)
            {
                _rawPointerBorder = new Border()
                {
                    IsHitTestVisible = false,
                    BorderThickness = new Thickness(2),
                    BorderBrush = Brushes.Gray,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    IsVisible = false,
                    RenderTransform = new TranslateTransform()
                };

                RootGrid.Children.Add(_rawPointerBorder);
            }
        }
        else
        {
            SwitchRawPointerToggleButton.Content = "Use Raw WM Pointer";

            if (_rawPointerTextBlock != null)
            {
                _rawPointerTextBlock.IsVisible = false;
            }

            if (_rawPointerBorder != null)
            {
                _rawPointerBorder.IsVisible = false;
            }
        }
    }

    private TextBlock? _rawPointerTextBlock;
    private Border? _rawPointerBorder;

    private void MainView_Loaded(object? sender, RoutedEventArgs e)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0))
        {
            return;
        }

        if (TopLevel.GetTopLevel(this)?.TryGetPlatformHandle() is { } handle)
        {
            // 一般来说，用 SetWindowsHookEx 是给全局的，自己应用内可以更加简单
            //SetWindowsHookEx()
            Debug.Assert(Environment.Is64BitProcess);

            // 这里用 SetWindowLongPtrW 的原因是，64位的程序调用 32位的 SetWindowLongW 会导致异常，第三位参数不匹配方法指针，详细请看
            // [实战经验：SetWindowLongPtr在开发64位程序的使用方法 | 官方博客 | 拓扑梅尔智慧办公平台 | TopomelBox 官方站点](https://www.topomel.com/archives/245.html )

            _newWndProc = Hook;
            var functionPointer = Marshal.GetFunctionPointerForDelegate(_newWndProc);
            _oldWndProc = SetWindowLongPtrW(handle.Handle, (int) WINDOW_LONG_PTR_INDEX.GWLP_WNDPROC, functionPointer);
        }
    }

    /*
 * LONG_PTR SetWindowLongPtrW
   (
     [in] HWND     hWnd,
     [in] int      nIndex,
     [in] LONG_PTR dwNewLong
   );
 */
    [LibraryImport("User32.dll")]
    private static partial IntPtr SetWindowLongPtrW(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    // cswin32 生成的是 [MarshalAs(UnmanagedType.FunctionPtr)] winmdroot.UI.WindowsAndMessaging.WNDPROC lpPrevWndFunc 的参数。咱这里已经拿到了函数指针，所以不能使用 WNDPROC 委托
    [DllImport("USER32.dll", ExactSpelling = true, EntryPoint = "CallWindowProcW"),
     DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [SupportedOSPlatform("windows5.0")]
    private static extern LRESULT CallWindowProc(nint lpPrevWndFunc, HWND hWnd, uint msg, WPARAM wParam, LPARAM lParam);

    private WNDPROC? _newWndProc;
    private IntPtr _oldWndProc;

    [SupportedOSPlatform("windows5.0")]
    private unsafe LRESULT Hook(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        if (msg == WM_POINTERUPDATE /*Pointer Update*/)
        {
            Debug.Assert(OperatingSystem.IsWindowsVersionAtLeast(10, 0), "能够收到 WM_Pointer 消息，必定系统版本号不会低");

            var pointerId = (uint) (ToInt32(wParam) & 0xFFFF);
            var rawPointerPoint = PointerConverter.ToRawPointerPoint(pointerId);



            // 转换为 Avalonia 坐标系
            global::Windows.Win32.Foundation.RECT pointerDeviceRect = default;
            global::Windows.Win32.Foundation.RECT displayRect = default;

            GetPointerTouchInfo(pointerId, out POINTER_TOUCH_INFO info);
            GetPointerDeviceRects(info.pointerInfo.sourceDevice, &pointerDeviceRect, &displayRect);

            var scale = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1;
            var originPointToScreen = this.PointToScreen(new Point(0, 0));

            var xAvalonia = (rawPointerPoint.X + displayRect.left - originPointToScreen.X) / scale;
            var yAvalonia = (rawPointerPoint.Y + displayRect.top - originPointToScreen.Y) / scale;
            var widthAvalonia = rawPointerPoint.PixelWidth / scale;
            var heightAvalonia = rawPointerPoint.PixelHeight / scale;

            rawPointerPoint = rawPointerPoint with
            {
                Info = rawPointerPoint.Info +
                       $"\r\nAvaloniaX:{xAvalonia:0.00}, AvaloniaY:{yAvalonia:0.00}, AvaloniaWidth:{widthAvalonia:0.00}, AvaloniaHeight:{heightAvalonia:0.00}"
            };

            if (double.IsRealNumber(xAvalonia) && double.IsRealNumber(yAvalonia) &&
                double.IsRealNumber(widthAvalonia) && double.IsRealNumber(heightAvalonia) && _rawPointerBorder != null)
            {
                _rawPointerBorder.IsVisible = true;
                if (_rawPointerBorder.RenderTransform is TranslateTransform translateTransform)
                {
                    translateTransform.X = xAvalonia - widthAvalonia / 2;
                    translateTransform.Y = yAvalonia - heightAvalonia / 2;
                }

                _rawPointerBorder.Width = widthAvalonia;
                _rawPointerBorder.Height = heightAvalonia;
            }

            if (_rawPointerTextBlock != null)
            {
                _rawPointerTextBlock.IsVisible = true;
                _rawPointerTextBlock.Text = rawPointerPoint.Info;
            }
        }
        return CallWindowProc(_oldWndProc, hwnd, msg, wParam, lParam);
    }

    private static int ToInt32(WPARAM wParam) => ToInt32((IntPtr) wParam.Value);
    private static int ToInt32(IntPtr ptr) => IntPtr.Size == 4 ? ptr.ToInt32() : (int) (ptr.ToInt64() & 0xffffffff);
}