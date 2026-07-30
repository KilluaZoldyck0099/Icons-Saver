using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IconsSaver.Core.Storage;

/// <summary>
/// The single serializer configuration every document in the app is written with.
/// </summary>
public static class JsonSettings
{
    /// <summary>Options shared by layouts, settings and exports.</summary>
    /// <remarks>
    /// <para>
    /// <see cref="JavaScriptEncoder.UnsafeRelaxedJsonEscaping"/> is deliberate. These files are
    /// never embedded in HTML, and the default encoder would turn every non-ASCII file name on the
    /// desktop into a run of <c>\uXXXX</c> escapes, which makes a layout unreadable for anyone whose
    /// desktop is not in English.
    /// </para>
    /// <para>
    /// Reading is lenient (comments and trailing commas are tolerated) because users do hand-edit
    /// exported layouts.
    /// </para>
    /// </remarks>
    public static readonly JsonSerializerOptions Default = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };
}
