using SyncNetPro.Contracts;

namespace SyncNetPro.Sdk.Testing;

/// <summary>Kanal Core.</summary>
public enum SimChannel
{
    /// <summary>Core → interface (SinkNode, <c>port_out</c>).</summary>
    Sink,

    /// <summary>Interface → Core (SourceNode, <c>port_in</c>).</summary>
    Source,
}

/// <summary>Arah pesan dilihat dari SimCore.</summary>
public enum SimDirection
{
    /// <summary>SimCore mengirim ke interface.</summary>
    ToInterface,

    /// <summary>SimCore menerima dari interface.</summary>
    FromInterface,
}

/// <summary>Hasil pengiriman request ke interface.</summary>
public enum SimOutcome
{
    /// <summary>Interface membalas.</summary>
    Responded,

    /// <summary>Interface tidak membalas dalam batas waktu.</summary>
    Timeout,

    /// <summary>Kanal sink tidak terkoneksi — Core membalas sendiri <c>91 Link down</c>.</summary>
    LinkDown,

    /// <summary>Kunci sama masih menunggu — Core membalas sendiri <c>94 Duplicate transaction</c>.</summary>
    Duplicate,
}

/// <summary>Hasil <see cref="SimCore.SendAsync"/>.</summary>
/// <param name="Outcome">Hasil.</param>
/// <param name="Response">Balasan interface, atau balasan internal Core (91/94).</param>
/// <param name="Elapsed">Waktu tunggu.</param>
/// <param name="Warnings">Pelanggaran kontrak pada balasan.</param>
public sealed record SimResult(SimOutcome Outcome, CoreResponse? Response, TimeSpan Elapsed, IReadOnlyList<string> Warnings);

/// <summary>Satu pesan yang tercatat SimCore.</summary>
/// <param name="Id">Nomor urut.</param>
/// <param name="Time">Waktu.</param>
/// <param name="Node">Node.</param>
/// <param name="Channel">Kanal.</param>
/// <param name="Direction">Arah.</param>
/// <param name="Json">Isi pesan (JSON).</param>
/// <param name="Warnings">Pelanggaran kontrak.</param>
/// <param name="Note">Keterangan (mis. <c>late</c>, <c>unmatched</c>, <c>auto-reversal</c>).</param>
public sealed record SimMessage(long Id, DateTimeOffset Time, string Node, SimChannel Channel, SimDirection Direction, string Json,
    IReadOnlyList<string> Warnings, string? Note = null);

/// <summary>Trace yang diterima Log Services tiruan.</summary>
/// <param name="Id">Nomor urut.</param>
/// <param name="Time">Waktu diterima.</param>
/// <param name="Json">Isi <c>LogModel</c>.</param>
public sealed record SimTrace(long Id, DateTimeOffset Time, string Json);
