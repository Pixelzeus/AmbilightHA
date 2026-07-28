using System;
using System.Collections.Concurrent;
using AmbilightHA.Core.Models;

namespace AmbilightHA.Core.Throttling;

public sealed class ColorSmoother
{
    private readonly ConcurrentDictionary<string, RgbColor> _lastColors = new();

    /// <summary>
    /// Applique un lissage exponentiel (EMA / LERP) sur la couleur reçue pour éviter les clignotements violents.
    /// </summary>
    /// <param name="entityId">Identifiant de l'ampoule HA</param>
    /// <param name="targetColor">Nouvelle couleur capturée</param>
    /// <param name="alpha">Facteur de lissage (ex: 0.3 = 30% nouvelle couleur, 70% ancienne couleur)</param>
    public RgbColor Smooth(string entityId, RgbColor targetColor, float alpha = 0.35f)
    {
        if (!_lastColors.TryGetValue(entityId, out var prevColor))
        {
            _lastColors[entityId] = targetColor;
            return targetColor;
        }

        byte r = (byte)Math.Clamp(prevColor.R + (targetColor.R - prevColor.R) * alpha, 0, 255);
        byte g = (byte)Math.Clamp(prevColor.G + (targetColor.G - prevColor.G) * alpha, 0, 255);
        byte b = (byte)Math.Clamp(prevColor.B + (targetColor.B - prevColor.B) * alpha, 0, 255);

        var smoothed = new RgbColor(r, g, b);
        _lastColors[entityId] = smoothed;
        return smoothed;
    }

    public void Reset()
    {
        _lastColors.Clear();
    }
}
