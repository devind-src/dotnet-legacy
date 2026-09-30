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

    public static async Task<T> ForAsync<T>(Task<T> task, int timeoutMs = 5000) =>
        await task.WaitAsync(TimeSpan.FromMilliseconds(timeoutMs));
}
