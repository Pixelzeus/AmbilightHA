namespace AmbilightHA.Core.Models;

public enum ZoneType
{
    Global,
    Top,
    Bottom,
    Left,
    Right,
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight
}

/// <summary>
/// Définit une zone de découpage de l'écran en pourcentage (0.0 à 1.0).
/// </summary>
public record ScreenZone(
    ZoneType Type,
    string Name,
    float XRel,
    float YRel,
    float WidthRel,
    float HeightRel
)
{
    /// <summary>
    /// Crée un ensemble standard de zones d'analyse.
    /// </summary>
    public static List<ScreenZone> CreateDefaultZones()
    {
        return new List<ScreenZone>
        {
            new(ZoneType.Global, "Écran Global", 0.0f, 0.0f, 1.0f, 1.0f),
            new(ZoneType.Top, "Haut", 0.2f, 0.0f, 0.6f, 0.25f),
            new(ZoneType.Bottom, "Bas", 0.2f, 0.75f, 0.6f, 0.25f),
            new(ZoneType.Left, "Gauche", 0.0f, 0.2f, 0.25f, 0.6f),
            new(ZoneType.Right, "Droite", 0.75f, 0.2f, 0.25f, 0.6f),
            new(ZoneType.TopLeft, "Coin Haut-Gauche", 0.0f, 0.0f, 0.3f, 0.3f),
            new(ZoneType.TopRight, "Coin Haut-Droit", 0.7f, 0.0f, 0.3f, 0.3f),
            new(ZoneType.BottomLeft, "Coin Bas-Gauche", 0.0f, 0.7f, 0.3f, 0.3f),
            new(ZoneType.BottomRight, "Coin Bas-Droit", 0.7f, 0.7f, 0.3f, 0.3f)
        };
    }
}
