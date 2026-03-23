using System.Text.Json.Serialization;

namespace NMSE.Core;

/// <summary>
/// Source-generated JSON serializer context for Native AOT compatibility.
/// When the application is published with <c>PublishAot=true</c> and trimming,
/// the default reflection-based <see cref="System.Text.Json.JsonSerializer"/> is
/// stripped.  This context provides compile-time metadata for the small number of
/// types that are serialized / deserialized via <c>JsonSerializer</c> in the main
/// project.
///
/// <para>
/// <b>Scope:</b> Only <see cref="System.Text.Json.JsonSerializer"/> calls are
/// affected.  The majority of JSON loading in this application uses
/// <see cref="System.Text.Json.JsonDocument"/> (a low-level DOM reader) or the
/// custom <see cref="Models.JsonObject"/> parser — neither relies on reflection
/// and neither is touched by this context.
/// </para>
/// </summary>
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(ExportConfig))]
internal partial class AppJsonContext : JsonSerializerContext
{
}
