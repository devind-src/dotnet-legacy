using SyncNet.Template.Models;
using SyncNetPro.Contracts;

namespace SyncNet.Template.Mapping;

/// <summary>CoreResponse → response channel.</summary>
public sealed class ToChannel
{
    // TODO(4): field yang dikembalikan ke channel.
    public ChannelResponse From(CoreResponse response) => new()
    {
        ResponseCode = response.ResponseCode ?? "06",
        Message = response.ResponseMessage,
        Reference = response.ReferenceNumber,
        Amount = response.Amount,
        Fee = response.Fees?.TotalFee is > 0 ? response.Fees.TotalFee : null,
        Data = response.AdditionalData is { Count: > 0 } data ? data : null,
    };

    /// <summary>Respons yang dibuat interface sendiri (validasi, timeout, dsb.).</summary>
    public static ChannelResponse Status(string code, string message) => new() { ResponseCode = code, Message = message };
}
