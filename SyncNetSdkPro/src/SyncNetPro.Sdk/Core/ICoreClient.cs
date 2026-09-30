using SyncNetPro.Contracts;
using SyncNetPro.Sdk.Nodes;

namespace SyncNetPro.Sdk.Core;

/// <summary>Opsi pengiriman request ke Core.</summary>
public sealed record CoreSendOptions
{
    /// <summary>Nama koneksi eksternal asal → <c>private_data.connection_name</c>.</summary>
    public string? ConnectionName { get; init; }

    /// <summary>IP klien eksternal → <c>private_data.ip_external</c>.</summary>
    public string? RemoteAddress { get; init; }

    /// <summary>Batas waktu; default <c>request_timeout</c>/<c>advice_timeout</c> node + margin.</summary>
    public TimeSpan? Timeout { get; init; }
}

/// <summary>Mengirim request ke Core (kanal inbound) dan menunggu respons terkorelasi.</summary>
public interface ICoreClient
{
    /// <summary>Kirim request dan tunggu respons.</summary>
    /// <exception cref="CoreUnavailableException">Node tidak ada/tidak punya kanal inbound/terputus.</exception>
    /// <exception cref="DuplicateCoreRequestException">Kunci korelasi yang sama masih menunggu.</exception>
    /// <exception cref="TimeoutException">Core tidak membalas dalam batas waktu.</exception>
    Task<CoreResponse> SendAsync(string nodeName, CoreRequest request, CoreSendOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>Status kanal Core untuk node.</summary>
    bool IsConnected(string nodeName, CoreChannelDirection direction);
}
