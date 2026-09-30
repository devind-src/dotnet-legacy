using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using SyncNetPro.Sdk.Tests.Infrastructure;
using SyncNetPro.Sdk.Tracing;

namespace SyncNetPro.Sdk.Tests;

public class TraceTests
{
    private static string Golden(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Golden", file));

    [Fact]
    public void Trace_record_json_matches_legacy_LogModel()
    {
        var record = new TraceRecord
        {
            Datetime = new DateTime(2026, 9, 30, 10, 15, 30, 123, DateTimeKind.Unspecified),
            AppName = "API Biller",
            FileName = "BILLER_ABC",
            LogType = TraceRecord.TransactionType,
            Title = "<0200> Message to BILLER_ABC 10.0.0.1:5000",
            Detail = "MTI 0200\nDE 11 000123",
        };

        Assert.Equal(Golden("logmodel.json"), TraceDispatcher.Serialize(record));
    }

    private static TraceWriter Writer(bool enabled = true, int capacity = 100, string? overflowDirectory = null)
    {
        var options = new SyncNetOptions { AppName = "API Biller" };
        options.Trace.Enabled = enabled;
        options.Trace.QueueCapacity = capacity;
        return new TraceWriter(Options.Create(options), new FakeTimeProvider(new DateTimeOffset(2026, 9, 30, 10, 0, 0, TimeSpan.Zero)),
            overflowDirectory ?? Path.Combine(Path.GetTempPath(), "trace-" + Guid.NewGuid().ToString("N")));
    }

    [Fact]
    public void Message_uses_legacy_title_format()
    {
        TraceWriter writer = Writer();

        writer.Message("BILLER_ABC", TraceDirection.Outgoing, "0200", "detail", "10.0.0.1:5000");
        writer.Message("BILLER_ABC", TraceDirection.Incoming, "0210", "detail", "10.0.0.1:5000");

        Assert.True(writer.Reader.TryRead(out TraceRecord? outgoing));
        Assert.Equal("<0200> Message to BILLER_ABC 10.0.0.1:5000", outgoing.Title);
        Assert.Equal("transaction", outgoing.LogType);
        Assert.Equal("BILLER_ABC", outgoing.FileName);
        Assert.Equal("API Biller", outgoing.AppName);
        Assert.True(writer.Reader.TryRead(out TraceRecord? incoming));
        Assert.Equal("<0210> Message from BILLER_ABC 10.0.0.1:5000", incoming.Title);
    }

    [Fact]
    public void Nothing_is_queued_when_trace_is_off()
    {
        TraceWriter writer = Writer(enabled: false);

        writer.Info("NODE", "title");
        writer.SetEnabled(true);
        writer.Info("", "title2");

        Assert.True(writer.Reader.TryRead(out TraceRecord? record));
        Assert.Equal("title2", record.Title);
        Assert.Equal("API Biller", record.FileName);
        Assert.Equal("info", record.LogType);
        Assert.False(writer.Reader.TryRead(out _));
    }

    [Fact]
    public void Full_queue_writes_overflow_to_fallback_file_instead_of_dropping()
    {
        // Keputusan tim: trace transaksi adalah jejak audit — tidak boleh ada yang hilang.
        using var dir = new TempDirectory();
        TraceWriter writer = Writer(capacity: 100, overflowDirectory: dir.Path);

        for (int i = 0; i < 150; i++) writer.Message("BILLER", TraceDirection.Outgoing, "0200", $"isi-{i}", "remote");

        Assert.Equal(50, writer.OverflowCount);
        Assert.Equal(0, writer.LostCount);
        string[] files = Directory.GetFiles(dir.Combine("api-biller"), "biller_*.log");
        string text = File.ReadAllText(Assert.Single(files));
        for (int i = 100; i < 150; i++) Assert.Contains($"isi-{i}\n", text, StringComparison.Ordinal);
        int queued = 0;
        while (writer.Reader.TryRead(out _)) queued++;
        Assert.Equal(100, queued);
        Assert.Null(typeof(ITraceWriter).GetMethod("Clear"));
    }

    [Fact]
    public async Task File_sink_writes_legacy_format_with_normalized_names()
    {
        using var dir = new TempDirectory();
        await using var sink = new FileTraceSink(dir.Path);
        var record = new TraceRecord
        {
            Datetime = new DateTime(2026, 9, 30, 10, 15, 30, 123),
            AppName = "API Biller",
            FileName = "BILLER ABC",
            Title = "<0200> Message to BILLER ABC",
            Detail = "isi",
        };

        Assert.True(await sink.TrySendAsync(record, "{}", CancellationToken.None));

        string file = dir.Combine("api-biller", "biller-abc_20260930_10.log");
        Assert.Equal("[30 Sep 2026 10:15:30.123] <0200> Message to BILLER ABC\nisi\n\n", File.ReadAllText(file));
    }

    [Fact]
    public void Masking_replaces_every_character()
    {
        Assert.Equal("****************", SensitiveData.Mask("6019001234567890"));
        Assert.Null(SensitiveData.Mask(null));
        Assert.Equal(string.Empty, SensitiveData.Mask(string.Empty));
    }
}
