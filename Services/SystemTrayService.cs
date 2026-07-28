using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using AmbilightHA.UI.ViewModels;

namespace AmbilightHA.Services;

public sealed class SystemTrayService : IDisposable
{
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool DestroyIcon(IntPtr handle);

    private readonly NotifyIcon _notifyIcon;
    private readonly Icon _customIcon;
    private readonly IntPtr _hIconHandle = IntPtr.Zero;

    public SystemTrayService(MainViewModel viewModel, Action showWindowAction, Action exitAction)
    {
        (_customIcon, _hIconHandle) = CreateGlowingRgbIcon();

        _notifyIcon = new NotifyIcon
        {
            Icon = _customIcon,
            Text = "Ambilight Home Assistant",
            Visible = true
        };

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("🖥️ Ouvrir l'Interface", null, (s, e) => showWindowAction());
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("▶ Démarrer Ambilight", null, (s, e) => viewModel.StartCommand.Execute(null));
        contextMenu.Items.Add("⏹ Arrêter Ambilight", null, (s, e) => viewModel.StopCommand.Execute(null));
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("❌ Quitter", null, (s, e) => exitAction());

        _notifyIcon.ContextMenuStrip = contextMenu;
        _notifyIcon.DoubleClick += (s, e) => showWindowAction();
    }

    private static (Icon icon, IntPtr handle) CreateGlowingRgbIcon()
    {
        try
        {
            using var bmp = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;

                using (var path = new GraphicsPath())
                {
                    path.AddEllipse(1, 1, 30, 30);
                    using (var brush = new LinearGradientBrush(new Point(0, 0), new Point(32, 32), Color.FromArgb(108, 92, 231), Color.FromArgb(0, 255, 204)))
                    {
                        using (var pen = new Pen(brush, 3))
                        {
                            g.DrawEllipse(pen, 2, 2, 28, 28);
                        }
                    }
                }

                using (var fillBrush = new SolidBrush(Color.FromArgb(24, 24, 34)))
                {
                    g.FillEllipse(fillBrush, 5, 5, 22, 22);
                }

                using (var bulbBrush = new SolidBrush(Color.FromArgb(0, 255, 204)))
                {
                    g.FillEllipse(bulbBrush, 11, 8, 10, 10);
                }

                using (var baseBrush = new SolidBrush(Color.FromArgb(108, 92, 231)))
                {
                    g.FillRectangle(baseBrush, 13, 17, 6, 5);
                }
            }

            IntPtr hIcon = bmp.GetHicon();
            Icon icon = Icon.FromHandle(hIcon);
            return (icon, hIcon);
        }
        catch
        {
            return (SystemIcons.Application, IntPtr.Zero);
        }
    }

    public void ShowNotification(string title, string message)
    {
        try
        {
            _notifyIcon.ShowBalloonTip(3000, title, message, ToolTipIcon.Info);
        }
        catch { }
    }

    public void Dispose()
    {
        try
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _customIcon.Dispose();

            if (_hIconHandle != IntPtr.Zero)
            {
                DestroyIcon(_hIconHandle);
            }
        }
        catch { }
    }
}
