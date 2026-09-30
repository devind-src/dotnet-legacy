using SyncNetPro.Sdk.Nodes;
using SyncNetPro.Sdk.Transport;

namespace SyncNetPro.Sdk.Remote;

/// <summary>
/// Pemetaan kolom <c>sw_connections</c> (<c>protocol</c>, <c>tcp_header_format</c>, <c>tcp_hi_lo</c>) ke codec —
/// identik dengan <c>XTcpClientSdk/XTcpListenerSdk.SetProtocol()</c> SDK lama untuk protokol 0–4 agar
/// koneksi yang sudah berjalan di produksi tidak berubah:
/// <list type="table">
/// <item><term>2 byte + ASCII</term><description>biner 2 byte</description></item>
/// <item><term>2 byte + BCD</term><description>BCD 2 byte</description></item>
/// <item><term>4 byte + ASCII</term><description>4 digit ASCII</description></item>
/// <item><term>4 byte + BCD</term><description>biner 4 byte</description></item>
/// </list>
/// Protokol 5 (tanpa header) memakai <see cref="NoHeaderCodec"/> (perbaikan B2).
/// </summary>
public static class RemoteCodecs
{
    /// <summary>Codec untuk koneksi TCP.</summary>
    /// <exception cref="NotSupportedException">Protokol bukan TCP.</exception>
    public static ITcpFrameCodec ForConnection(RemoteConnectionInfo connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        (int length, TcpLengthMode mode) = connection.Protocol switch
        {
            ConnectionProtocol.Tcp2ByteExcludeHeader => (2, TcpLengthMode.Exclude),
            ConnectionProtocol.Tcp2ByteIncludeHeader => (2, TcpLengthMode.Include),
            ConnectionProtocol.Tcp4ByteExcludeHeader => (4, TcpLengthMode.Exclude),
            ConnectionProtocol.Tcp4ByteIncludeHeader => (4, TcpLengthMode.Include),
            // SDK lama: Custom = header 2 byte dengan mode default (exclude). Codec kustom sebenarnya
            // disediakan interface lewat SyncNetInterface.CreateTcpCodec.
            ConnectionProtocol.TcpHeaderCustom => (2, TcpLengthMode.Exclude),
            ConnectionProtocol.TcpHeaderNone => (0, TcpLengthMode.Exclude),
            _ => throw new NotSupportedException($"Koneksi {connection.Name}: protokol {connection.Protocol} bukan TCP."),
        };

        if (length == 0) return NoHeaderCodec.Instance;

        TcpHeaderType type = (length, connection.HeaderFormat) switch
        {
            (4, TcpHeaderFormat.Ascii) => TcpHeaderType.Ascii4Digit,
            (4, TcpHeaderFormat.Bcd) => TcpHeaderType.Binary4Byte,
            (2, TcpHeaderFormat.Bcd) => TcpHeaderType.Bcd2Byte,
            _ => TcpHeaderType.Binary2Byte,
        };

        TcpEndianMode endian = connection.HeaderHighLow ? TcpEndianMode.BigEndian : TcpEndianMode.LittleEndian;
        return new LengthPrefixCodec(new TcpHeaderOptions(type, mode, endian));
    }

    /// <summary><c>true</c> bila protokol koneksi adalah TCP (0–5).</summary>
    public static bool IsTcp(ConnectionProtocol protocol) => protocol <= ConnectionProtocol.TcpHeaderNone;
}
