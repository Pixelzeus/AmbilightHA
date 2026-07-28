using System.Text.Json.Serialization;

namespace AmbilightHA.Core.HomeAssistant;

public record HaAuthMessage(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("access_token")] string AccessToken
);

public record HaServiceTarget(
    [property: JsonPropertyName("entity_id")] string EntityId
);

public record HaLightServiceData(
    [property: JsonPropertyName("rgb_color")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    int[]? RgbColor = null,

    [property: JsonPropertyName("brightness")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    int? Brightness = null,

    [property: JsonPropertyName("color_temp")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    int? ColorTemp = null,

    [property: JsonPropertyName("transition")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    float? Transition = null
);

public record HaCallServiceMessage(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("domain")] string Domain,
    [property: JsonPropertyName("service")] string Service,
    [property: JsonPropertyName("target")] HaServiceTarget Target,
    [property: JsonPropertyName("service_data")] HaLightServiceData ServiceData
);

public record HaGetStatesMessage(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("type")] string Type = "get_states"
);

public record InitialLightState(
    string EntityId,
    bool IsOn,
    int[]? RgbColor,
    int? Brightness,
    int? ColorTemp
);
