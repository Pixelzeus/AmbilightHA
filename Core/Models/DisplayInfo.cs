using System.Drawing;

namespace AmbilightHA.Core.Models;

public class DisplayInfo
{
    public int Index { get; }
    public string Name { get; }
    public Rectangle Bounds { get; }
    public bool IsPrimary { get; }

    public int Width => Bounds.Width;
    public int Height => Bounds.Height;

    public DisplayInfo(int index, string deviceName, Rectangle bounds, bool isPrimary)
    {
        Index = index;
        Bounds = bounds;
        IsPrimary = isPrimary;

        string primaryBadge = isPrimary ? " (Principal)" : "";
        Name = $"🖥️ Écran {index}{primaryBadge} - {bounds.Width}x{bounds.Height}";
    }

    public override string ToString() => Name;
}
