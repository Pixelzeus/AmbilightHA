namespace AmbilightHA.Core.Models;

public enum ColorAnalysisMode
{
    /// <summary>
    /// Extraction prioritaire des couleurs vives et d'accentuation (ignore les teintes grises/neutres).
    /// </summary>
    VibrantAccent,

    /// <summary>
    /// Calcul classique de la couleur moyenne globale (moyenne arithmétique).
    /// </summary>
    StandardAverage
}
