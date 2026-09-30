using System.Text;

namespace SyncNetPro.Sdk.Logging;

/// <summary>
/// Menambah teks ke file log/trace dengan <see cref="FileShare.ReadWrite"/> | <see cref="FileShare.Delete"/>:
/// di Windows file tetap dapat dibaca (tail, audit, collector) atau diarsipkan selama interface menulis.
/// </summary>
internal static class SharedFile
{
    private const FileShare Share = FileShare.ReadWrite | FileShare.Delete;

    public static void Append(string path, string text)
    {
        using var stream = new FileStream(path, FileMode.Append, FileAccess.Write, Share);
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        stream.Write(bytes);
    }

    public static async Task AppendAsync(string path, string text, CancellationToken cancellationToken = default)
    {
        var stream = new FileStream(path, FileMode.Append, FileAccess.Write, Share, 4096, FileOptions.Asynchronous);
        await using (stream.ConfigureAwait(false))
        {
            await stream.WriteAsync(Encoding.UTF8.GetBytes(text), cancellationToken).ConfigureAwait(false);
        }
    }
}
