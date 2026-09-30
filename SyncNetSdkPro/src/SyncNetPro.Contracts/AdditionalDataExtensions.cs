using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Newtonsoft.Json.Linq;

namespace SyncNetPro.Contracts;

/// <summary>
/// Helper untuk <c>additional_data</c> — satu-satunya tempat informasi tambahan dari interface
/// (keputusan Q1: skema JSON ke Core tidak boleh bertambah).
/// </summary>
public static class AdditionalDataExtensions
{
    /// <summary>Mengisi (atau menimpa) nilai <paramref name="key"/> pada <c>additional_data</c> request.</summary>
    public static CoreRequest SetAdditionalData(this CoreRequest request, string key, object? value)
    {
        ArgumentNullException.ThrowIfNull(request);
        (request.AdditionalData ??= [])[key] = value;
        return request;
    }

    /// <summary>Mengisi (atau menimpa) nilai <paramref name="key"/> pada <c>additional_data</c> response.</summary>
    public static CoreResponse SetAdditionalData(this CoreResponse response, string key, object? value)
    {
        ArgumentNullException.ThrowIfNull(response);
        (response.AdditionalData ??= [])[key] = value;
        return response;
    }

    /// <summary>Membaca nilai bertipe dari <c>additional_data</c> request.</summary>
    public static bool TryGetAdditionalData<T>(this CoreRequest request, string key, [MaybeNullWhen(false)] out T value)
    {
        ArgumentNullException.ThrowIfNull(request);
        return TryGet(request.AdditionalData, key, out value);
    }

    /// <summary>Membaca nilai bertipe dari <c>additional_data</c> response.</summary>
    public static bool TryGetAdditionalData<T>(this CoreResponse response, string key, [MaybeNullWhen(false)] out T value)
    {
        ArgumentNullException.ThrowIfNull(response);
        return TryGet(response.AdditionalData, key, out value);
    }

    /// <summary>
    /// Membaca nilai bertipe dari dictionary <c>additional_data</c>. Menangani nilai hasil
    /// deserialisasi (<see cref="JToken"/>) maupun nilai yang diisi langsung.
    /// </summary>
    public static bool TryGet<T>(IReadOnlyDictionary<string, object?>? data, string key, [MaybeNullWhen(false)] out T value)
    {
        value = default;
        if (data is null || !data.TryGetValue(key, out object? raw) || raw is null) return false;

        try
        {
            switch (raw)
            {
                case T typed:
                    value = typed;
                    return true;
                case JToken token:
                    T? converted = token.ToObject<T>();
                    if (converted is null) return false;
                    value = converted;
                    return true;
                case IConvertible when typeof(IConvertible).IsAssignableFrom(Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T)):
                    value = (T)Convert.ChangeType(raw, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T), CultureInfo.InvariantCulture);
                    return true;
                default:
                    return false;
            }
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException or ArgumentException or Newtonsoft.Json.JsonException)
        {
            value = default;
            return false;
        }
    }
}
