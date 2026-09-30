using SyncNetPro.Contracts;

namespace SyncNetPro.Sdk.Core;

/// <summary>Kunci korelasi request ↔ response pada kanal inbound.</summary>
public interface ICoreCorrelationKeyProvider
{
    /// <summary>Kunci dari request yang dikirim ke Core.</summary>
    string GetKey(CoreRequest request);

    /// <summary>Kunci dari response Core.</summary>
    string GetKey(CoreResponse response);
}

/// <summary>
/// Default: <c>UPPER(tran_type + datetime_tran + trace_number + terminal_id)</c> — sama dengan kunci
/// switch Core (dok. 02 §3).
/// </summary>
public sealed class CoreSwitchKeyProvider : ICoreCorrelationKeyProvider
{
    /// <inheritdoc />
    public string GetKey(CoreRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Key(request.TranType, request.TransactionDateTime, request.TraceNumber, request.TerminalId);
    }

    /// <inheritdoc />
    public string GetKey(CoreResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return Key(response.TranType, response.TransactionDateTime, response.TraceNumber, response.TerminalId);
    }

    private static string Key(string? tranType, string? dateTime, string? trace, string? terminal) =>
        string.Concat(tranType, dateTime, trace, terminal).ToUpperInvariant();
}
