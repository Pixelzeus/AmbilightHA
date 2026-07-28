using System;
using System.Collections.Generic;
using AmbilightHA.Core.Models;

namespace AmbilightHA.Core.ColorAnalysis;

public sealed class ColorProcessor
{
    /// <summary>
    /// Calcule la couleur moyenne ou d'accentuation pour chaque zone spécifiée.
    /// </summary>
    public unsafe Dictionary<ZoneType, RgbColor> ProcessZones(
        nint dataPointer,
        int width,
        int height,
        int rowPitch,
        IReadOnlyList<ScreenZone> zones,
        int step = 16,
        float saturation = 1.3f,
        float brightness = 1.0f,
        float gamma = 1.0f,
        byte minBrightness = 0,
        ColorAnalysisMode mode = ColorAnalysisMode.VibrantAccent,
        float accentWeightExponent = 2.0f)
    {
        var results = new Dictionary<ZoneType, RgbColor>(zones.Count);
        byte* basePtr = (byte*)dataPointer;

        int sampleStep = Math.Max(1, step);

        foreach (var zone in zones)
        {
            int startX = (int)(zone.XRel * width);
            int startY = (int)(zone.YRel * height);
            int zoneW = (int)(zone.WidthRel * width);
            int zoneH = (int)(zone.HeightRel * height);

            int endX = Math.Min(startX + zoneW, width);
            int endY = Math.Min(startY + zoneH, height);

            var (sumR, sumG, sumB, totalWeight) = SimdColorCalculator.CalculateZoneSumPonderated(
                basePtr, startX, startY, endX, endY, rowPitch, sampleStep, mode, accentWeightExponent);

            if (totalWeight > 0.0001)
            {
                byte avgR = (byte)Math.Clamp(sumR / totalWeight, 0, 255);
                byte avgG = (byte)Math.Clamp(sumG / totalWeight, 0, 255);
                byte avgB = (byte)Math.Clamp(sumB / totalWeight, 0, 255);

                var rawColor = new RgbColor(avgR, avgG, avgB);
                results[zone.Type] = rawColor.Adjust(saturation, brightness, gamma, minBrightness);
            }
            else
            {
                results[zone.Type] = RgbColor.Black;
            }
        }

        return results;
    }
}
