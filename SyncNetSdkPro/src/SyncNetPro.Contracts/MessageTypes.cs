namespace SyncNetPro.Contracts;

/// <summary>Utilitas message type indicator (MTI).</summary>
public static class MessageTypes
{
    /// <summary>
    /// MTI response dari MTI request — aturan identik dengan <c>NbMessage.GetMsgTypeResp</c> SDK lama:
    /// digit ke-3 <c>0</c>→<c>1</c> (request) atau <c>2</c>→<c>3</c> (advice), lalu digit terakhir
    /// <c>1</c> (repeat) → <c>0</c>. Contoh <c>0200</c>→<c>0210</c>, <c>0221</c>→<c>0230</c>.
    /// Selain itu dikembalikan apa adanya; <c>null</c>/kosong → <c>""</c>.
    /// </summary>
    public static string ToResponse(string? requestMessageType)
    {
        if (string.IsNullOrEmpty(requestMessageType)) return string.Empty;
        if (requestMessageType.Length != 4) return requestMessageType;

        char function = requestMessageType[2] switch
        {
            '0' => '1',
            '2' => '3',
            _ => '\0',
        };
        if (function == '\0') return requestMessageType;

        char origin = requestMessageType[3] == '1' ? '0' : requestMessageType[3];
        return string.Concat(requestMessageType.AsSpan(0, 2), [function, origin]);
    }
}
