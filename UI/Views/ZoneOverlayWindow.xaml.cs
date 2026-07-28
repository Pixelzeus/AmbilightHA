using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using MediaColor = System.Windows.Media.Color;
using MediaBrushes = System.Windows.Media.Brushes;
using MediaBrush = System.Windows.Media.SolidColorBrush;
using WpfRectangle = System.Windows.Shapes.Rectangle;
using AmbilightHA.Core.Models;

namespace AmbilightHA.UI.Views;

public partial class ZoneOverlayWindow : Window
{
    public ZoneOverlayWindow()
    {
        InitializeComponent();
    }

    public void RenderZones(IReadOnlyList<ScreenZone> zones)
    {
        OverlayCanvas.Children.Clear();

        double screenWidth = SystemParameters.PrimaryScreenWidth;
        double screenHeight = SystemParameters.PrimaryScreenHeight;

        foreach (var zone in zones)
        {
            if (zone.Type == ZoneType.Global) continue;

            double left = zone.XRel * screenWidth;
            double top = zone.YRel * screenHeight;
            double width = zone.WidthRel * screenWidth;
            double height = zone.HeightRel * screenHeight;

            // Rectangle de bordure WPF
            var rect = new WpfRectangle
            {
                Width = width,
                Height = height,
                Stroke = new MediaBrush(MediaColor.FromArgb(200, 108, 92, 231)),
                StrokeThickness = 3,
                StrokeDashArray = new System.Windows.Media.DoubleCollection { 4, 2 },
                Fill = new MediaBrush(MediaColor.FromArgb(40, 108, 92, 231))
            };

            Canvas.SetLeft(rect, left);
            Canvas.SetTop(rect, top);
            OverlayCanvas.Children.Add(rect);

            // Étiquette du nom de zone
            var border = new Border
            {
                Background = new MediaBrush(MediaColor.FromArgb(220, 20, 20, 30)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2, 6, 2),
                Margin = new Thickness(4)
            };

            var text = new TextBlock
            {
                Text = $"📍 {zone.Name}",
                Foreground = MediaBrushes.Cyan,
                FontSize = 11,
                FontWeight = FontWeights.Bold
            };

            border.Child = text;
            Canvas.SetLeft(border, left + 4);
            Canvas.SetTop(border, top + 4);
            OverlayCanvas.Children.Add(border);
        }
    }
}
