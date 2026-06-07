using System.Text.Json;
using System.Text.Json.Serialization;

namespace BHelper.App.Power;

internal sealed class PowerModeKindJsonConverter : JsonConverter<PowerModeKind>
{
    public override PowerModeKind Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String
            && Enum.TryParse<PowerModeKind>(reader.GetString(), ignoreCase: true, out var kind))
            return kind;

        return PerformanceProfile.DefaultKind;
    }

    public override void Write(Utf8JsonWriter writer, PowerModeKind value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString());
}
