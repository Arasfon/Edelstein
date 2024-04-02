using System.Text.Json.Serialization;

namespace Edelstein.Models.Protocol;

public class Stamina
{
    [JsonPropertyName("stamina")]
    public int StaminaValue { get; set; }

    public long LastUpdatedTime { get; set; }
}
