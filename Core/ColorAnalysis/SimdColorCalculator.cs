using System;
using AmbilightHA.Core.Models;

namespace AmbilightHA.Core.ColorAnalysis;

public static class SimdColorCalculator
{
    /// <summary>
    /// Calcule la somme ponderée ou la somme directe des composantes RVB.
    /// En mode VibrantAccent, chaque pixel est pondéré par sa saturation (S^exponent)
    /// afin de filtrer le gris/asphalte/murs sombres et isoler les couleurs vives d'accent.
    /// </summary>
    public static unsafe (double sumR, double sumG, double sumB, double totalWeight) CalculateZoneSumPonderated(
        byte* basePtr,
        int startX,
        int startY,
        int endX,
        int endY,
        int rowPitch,
        int sampleStep,
        ColorAnalysisMode mode,
        float accentWeightExponent = 2.0f)
    {
        double sumR = 0;
        double sumG = 0;
        double sumB = 0;
        double totalWeight = 0;

        for (int y = startY; y < endY; y += sampleStep)
        {
            byte* rowPtr = basePtr + (y * rowPitch);

            for (int x = startX; x < endX; x += sampleStep)
            {
                byte* pixelPtr = rowPtr + (x * 4); // BGRA

                byte b = pixelPtr[0];
                byte g = pixelPtr[1];
                byte r = pixelPtr[2];

                if (mode == ColorAnalysisMode.StandardAverage)
                {
                    sumR += r;
                    sumG += g;
                    sumB += b;
                    totalWeight += 1.0;
                }
                else // VibrantAccent Mode
                {
                    // Calcul de la saturation du pixel S = (max - min) / max
                    float max = Math.Max(r, Math.Max(g, b));
                    float min = Math.Min(r, Math.Min(g, b));
                    float delta = max - min;

                    float saturation = max > 0.001f ? (delta / max) : 0f;

                    // Pondération exponentielle par la saturation
                    // Un pixel gris (S ~ 0) reçoit un poids proche de 0.01
                    // Un pixel très vivement coloré (S ~ 1) reçoit un poids élevé (1.0)
                    double weight = Math.Pow(saturation, accentWeightExponent) + 0.02;

                    sumR += r * weight;
                    sumG += g * weight;
                    sumB += b * weight;
                    totalWeight += weight;
                }
            }
        }

        return (sumR, sumG, sumB, totalWeight);
    }
}
