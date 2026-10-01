namespace SyncNetPro.Iso8583;

/// <summary>Masking data sensitif pada trace (<see cref="IsoMessage.Format(IsoFormatOptions?)"/>).</summary>
public sealed record IsoFormatOptions
{
    /// <summary>Default: PIN block (52), track 1/2 (45/35), ICC (55) disamarkan penuh; PAN (2) 6 digit awal + 4 akhir.</summary>
    public static IsoFormatOptions Default { get; } = new();

    /// <summary>Tanpa masking (hanya untuk pengembangan lokal).</summary>
    public static IsoFormatOptions None { get; } = new() { SensitiveFields = new HashSet<int>(), PanFields = new HashSet<int>() };

    /// <summary>Field yang disamarkan penuh.</summary>
    public IReadOnlySet<int> SensitiveFields { get; init; } = new HashSet<int> { 35, 45, 52, 55 };

    /// <summary>Field PAN: tampil 6 awal + 4 akhir.</summary>
    public IReadOnlySet<int> PanFields { get; init; } = new HashSet<int> { 2 };

    internal string Mask(int field, string value)
    {
        if (SensitiveFields.Contains(field)) return new string('*', value.Length);
        if (PanFields.Contains(field) && value.Length > 10) return string.Concat(value.AsSpan(0, 6), new string('*', value.Length - 10), value.AsSpan(value.Length - 4));
        if (PanFields.Contains(field)) return new string('*', value.Length);
        return value;
    }

    internal bool IsMasked(int field) => SensitiveFields.Contains(field) || PanFields.Contains(field);
}
