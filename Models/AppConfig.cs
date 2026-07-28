using System;
using System.Collections.Generic;
using AmbilightHA.Core.Models;

namespace AmbilightHA.Models;

public class AppConfig
{
    public string HaUrl { get; set; } = "http://192.168.1.50:8123";
    public string HaToken { get; set; } = "";
    public int DisplayIndex { get; set; } = 0;
    public ColorAnalysisMode Mode { get; set; } = ColorAnalysisMode.VibrantAccent;
    public float AccentWeightExponent { get; set; } = 2.0f;
    public float Saturation { get; set; } = 1.3f;
    public float Brightness { get; set; } = 1.0f;
    public float Gamma { get; set; } = 1.0f;
    public byte MinBrightness { get; set; } = 0;
    public float TransitionSeconds { get; set; } = 0.2f;
    public int TargetFps { get; set; } = 30;
    public int RateLimitPerBulb { get; set; } = 8;
    public bool RestoreLightsOnStop { get; set; } = true;

    public List<ZoneMappingConfig> Mappings { get; set; } = new()
    {
        new ZoneMappingConfig { ZoneType = ZoneType.Global, EntityId = "light.salon_ambilight" },
        new ZoneMappingConfig { ZoneType = ZoneType.Left, EntityId = "light.ampoule_gauche" },
        new ZoneMappingConfig { ZoneType = ZoneType.Right, EntityId = "light.ampoule_droite" },
        new ZoneMappingConfig { ZoneType = ZoneType.Top, EntityId = "light.bandeau_haut" }
    };
}

public class ZoneMappingConfig
{
    public ZoneType ZoneType { get; set; }
    public string EntityId { get; set; } = "";
}
