namespace SyncNetPro.Sdk.Tests.Infrastructure;

internal static class Wait
{
    public static async Task UntilAsync(Func<bool> condition, string because, int timeoutMs = 5000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (!condition())
        {
            if (DateTime.UtcNow > deadline) throw new TimeoutException($"Menunggu terlalu lama: {because}");
            await Task.Delay(20);
        }
    }

    /// <summary>Membaca file yang mungkin sedang ditulis (Windows menolak <c>File.ReadAllText</c> saat itu).</summary>
    public static string ReadShared(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public static async Task<T> ForAsync<T>(Task<T> task, int timeoutMs = 5000) =>
        await task.WaitAsync(TimeSpan.FromMilliseconds(timeoutMs));
}
