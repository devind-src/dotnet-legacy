namespace SyncNetPro.Contracts.Tests;

public class AdditionalDataTests
{
    private static CoreRequest RoundTrip(CoreRequest request)
    {
        var serializer = NewtonsoftCoreMessageSerializer.Instance;
        return serializer.Deserialize<CoreRequest>(serializer.Serialize(request));
    }

    [Fact]
    public void Reads_typed_values_after_round_trip()
    {
        CoreRequest request = RoundTrip(new CoreRequest()
            .SetAdditionalData("text", "abc")
            .SetAdditionalData("count", 3)
            .SetAdditionalData("amount", 12.5m)
            .SetAdditionalData("flag", true)
            .SetAdditionalData("items", new[] { "a", "b" }));

        Assert.True(request.TryGetAdditionalData("text", out string? text));
        Assert.Equal("abc", text);
        Assert.True(request.TryGetAdditionalData("count", out int count));
        Assert.Equal(3, count);
        Assert.True(request.TryGetAdditionalData("amount", out decimal amount));
        Assert.Equal(12.5m, amount);
        Assert.True(request.TryGetAdditionalData("flag", out bool flag));
        Assert.True(flag);
        Assert.True(request.TryGetAdditionalData("items", out string[]? items));
        Assert.Equal(["a", "b"], items!);
    }

    [Fact]
    public void Reads_values_set_in_memory()
    {
        var response = new CoreResponse().SetAdditionalData("count", 3L);

        Assert.True(response.TryGetAdditionalData("count", out int count));
        Assert.Equal(3, count);
    }

    [Fact]
    public void Returns_false_for_missing_null_or_incompatible_value()
    {
        CoreRequest request = RoundTrip(new CoreRequest()
            .SetAdditionalData("null", null)
            .SetAdditionalData("text", "abc"));

        Assert.False(request.TryGetAdditionalData("missing", out string? _));
        Assert.False(request.TryGetAdditionalData("null", out string? _));
        Assert.False(request.TryGetAdditionalData("text", out int _));
    }

    [Fact]
    public void Creates_dictionary_when_null()
    {
        var request = new CoreRequest { AdditionalData = null };

        request.SetAdditionalData("k", "v");

        Assert.Equal("v", request.AdditionalData!["k"]);
    }
}
