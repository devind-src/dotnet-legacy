using System.Buffers;

namespace SyncNetPro.Sdk.Transport;

/// <summary>Pemecah stream TCP menjadi pesan (frame) dan sebaliknya.</summary>
public interface ITcpFrameCodec
{
    /// <summary>Membungkus payload menjadi frame siap kirim.</summary>
    byte[] Encode(ReadOnlySpan<byte> payload);

    /// <summary>
    /// Mencoba mengambil satu frame lengkap dari awal <paramref name="buffer"/>. Bila berhasil,
    /// <paramref name="buffer"/> dimajukan melewati frame tersebut.
    /// </summary>
    /// <exception cref="InvalidDataException">Header tidak valid; koneksi sebaiknya ditutup.</exception>
    bool TryDecode(ref ReadOnlySequence<byte> buffer, out byte[] payload);
}
