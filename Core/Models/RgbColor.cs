using System;

namespace AmbilightHA.Core.Models;

/// <summary>
/// Structure immutable ultra-légère représentant une couleur RVB (0-255)
/// avec ajustements optimisés de Saturation, Luminosité et Gamma.
/// </summary>
public readonly struct RgbColor
{
    public byte R { get; }
    public byte G { get; }
    public byte B { get; }

    public RgbColor(byte r, byte g, byte b)
    {
        R = r;
        G = g;
        B = b;
    }

    public static RgbColor Black => new(0, 0, 0);

    /// <summary>
    /// Calcule le niveau de luminosité matérielle (1-255) à transmettre à l'attribut 'brightness' d'une ampoule Home Assistant.
    /// </summary>
    public int CalculateBrightnessLevel(float brightnessFactor, float gamma = 1.0f, byte minBrightness = 0)
    {
        float maxChannel = Math.Max(R, Math.Max(G, B)) / 255.0f;

        if (maxChannel <= 0.001f)
        {
            return Math.Max(1, (int)minBrightness);
        }

        if (Math.Abs(gamma - 1.0f) > 0.01f)
        {
            maxChannel = MathF.Pow(maxChannel, gamma);
        }

        maxChannel *= brightnessFactor;
        int level = (int)(maxChannel * 255.0f);

        return Math.Clamp(Math.Max(minBrightness, level), 1, 255);
    }

    /// <summary>
    /// Applique la saturation (HSL) et renvoie la couleur saturée équilibrée.
    /// </summary>
    public RgbColor Adjust(float saturationFactor, float brightnessFactor = 1.0f, float gamma = 1.0f, byte minBrightness = 0)
    {
        float rf = R / 255.0f;
        float gf = G / 255.0f;
        float bf = B / 255.0f;

        // Amplification de Saturation via espace HSL
        if (Math.Abs(saturationFactor - 1.0f) > 0.01f)
        {
            float max = Math.Max(rf, Math.Max(gf, bf));
            float min = Math.Min(rf, Math.Min(gf, bf));
            float delta = max - min;

            float h = 0f;
            float s = 0f;
            float l = (max + min) / 2.0f;

            if (delta > 0.00001f)
            {
                s = l > 0.5f ? delta / (2.0f - max - min) : delta / (max + min);

                if (Math.Abs(max - rf) < 0.00001f)
                    h = (gf - bf) / delta + (gf < bf ? 6f : 0f);
                else if (Math.Abs(max - gf) < 0.00001f)
                    h = (bf - rf) / delta + 2f;
                else
                    h = (rf - gf) / delta + 4f;

                h /= 6f;
            }

            s = Math.Clamp(s * saturationFactor, 0.0f, 1.0f);

            var satRgb = FromHsl(h, s, l);
            rf = satRgb.R / 255.0f;
            gf = satRgb.G / 255.0f;
            bf = satRgb.B / 255.0f;
        }

        byte rFinal = (byte)Math.Clamp((int)(rf * 255.0f), 0, 255);
        byte gFinal = (byte)Math.Clamp((int)(gf * 255.0f), 0, 255);
        byte bFinal = (byte)Math.Clamp((int)(bf * 255.0f), 0, 255);

        return new RgbColor(rFinal, gFinal, bFinal);
    }

    private static RgbColor FromHsl(float h, float s, float l)
    {
        if (s <= 0.00001f)
        {
            byte val = (byte)Math.Clamp(l * 255.0f, 0, 255);
            return new RgbColor(val, val, val);
        }

        float q = l < 0.5f ? l * (1.0f + s) : l + s - l * s;
        float p = 2.0f * l - q;

        float r = HueToRgb(p, q, h + 1.0f / 3.0f);
        float g = HueToRgb(p, q, h);
        float b = HueToRgb(p, q, h - 1.0f / 3.0f);

        return new RgbColor(
            (byte)Math.Clamp(r * 255.0f, 0, 255),
            (byte)Math.Clamp(g * 255.0f, 0, 255),
            (byte)Math.Clamp(b * 255.0f, 0, 255)
        );
    }

    private static float HueToRgb(float p, float q, float t)
    {
        if (t < 0f) t += 1f;
        if (t > 1f) t -= 1f;
        if (t < 1f / 6f) return p + (q - p) * 6f * t;
        if (t < 1f / 2f) return q;
        if (t < 2f / 3f) return p + (q - p) * (2f / 3f - t) * 6f;
        return p;
    }

    public override string ToString() => $"RGB({R}, {G}, {B})";
}
