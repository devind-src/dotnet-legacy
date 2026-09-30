namespace SyncNetPro.Sdk.Nodes;

/// <summary>Kategori node (<c>sw_nodes.category</c>) — menentukan kanal Core yang dibuka.</summary>
public enum NodeCategory
{
    /// <summary><c>"0"</c>: kanal inbound dan outbound.</summary>
    Both = 0,

    /// <summary><c>"1"</c> merchant/acquirer: hanya kanal inbound (Core SourceNode, <c>port_in</c>).</summary>
    Merchant = 1,

    /// <summary><c>"2"</c> biller/issuer: hanya kanal outbound (Core SinkNode, <c>port_out</c>).</summary>
    BillerIssuer = 2,
}

/// <summary>Node interface (baris <c>sw_nodes</c>). Read-only; berubah hanya lewat RESYNC.</summary>
public sealed record NodeInfo
{
    /// <summary><c>node_id</c>.</summary>
    public int Id { get; init; }

    /// <summary><c>node_name</c>.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary><c>category</c>.</summary>
    public NodeCategory Category { get; init; }

    /// <summary><c>inst_id</c>.</summary>
    public string? InstitutionId { get; init; }

    /// <summary><c>port_in</c>: port Core untuk kanal inbound (interface → Core).</summary>
    public int PortIn { get; init; }

    /// <summary><c>port_out</c>: port Core untuk kanal outbound (Core → interface).</summary>
    public int PortOut { get; init; }

    /// <summary><c>request_timeout</c> dalam detik.</summary>
    public int RequestTimeoutSeconds { get; init; } = 30;

    /// <summary><c>advice_timeout</c> dalam detik (untuk ADV/REV).</summary>
    public int AdviceTimeoutSeconds { get; init; } = 30;

    /// <summary><c>auto_signon</c> = "1".</summary>
    public bool AutoSignOn { get; init; }

    /// <summary><c>auto_reversal</c> (mode timeout Core).</summary>
    public string? AutoReversal { get; init; }

    /// <summary><c>echo_timer</c> dalam menit (0 = nonaktif).</summary>
    public int EchoTimerMinutes { get; init; }

    /// <summary><c>keychange_timer</c> dalam menit (0 = nonaktif).</summary>
    public int KeyChangeTimerMinutes { get; init; }

    /// <summary><c>pin_translate</c> = "1".</summary>
    public bool PinTranslate { get; init; }

    /// <summary><c>sensitive_data</c> = 1: PAN/track2 dimasking di trace.</summary>
    public bool ProtectSensitiveData { get; init; }

    /// <summary><c>saf_limit</c>.</summary>
    public int SafLimit { get; init; }

    /// <summary><c>parameter</c> (bebas, milik interface).</summary>
    public string? Parameter { get; init; }

    /// <summary>Node memiliki kanal inbound.</summary>
    public bool HasInbound => Category != NodeCategory.BillerIssuer;

    /// <summary>Node memiliki kanal outbound.</summary>
    public bool HasOutbound => Category != NodeCategory.Merchant;
}
