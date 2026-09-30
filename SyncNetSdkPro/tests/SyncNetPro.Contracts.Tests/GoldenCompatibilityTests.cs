using Newtonsoft.Json;

namespace SyncNetPro.Contracts.Tests;

/// <summary>
/// Kompatibilitas byte-per-byte dengan SDK lama (dok. 02, dok. 08 §2.1).
/// Golden file dihasilkan oleh serializer SDK lama, bukan diketik manual.
/// </summary>
public class GoldenCompatibilityTests
{
    private static readonly CoreMessageCodec Codec = CoreMessageCodec.Default;

    public static TheoryData<string> RequestCases => GoldenFiles.Cases("request-");
    public static TheoryData<string> ResponseCases => GoldenFiles.Cases("response-");
    public static TheoryData<string> FromCases => GoldenFiles.FromCases();

    [Fact]
    public void Golden_files_are_present()
    {
        Assert.True(RequestCases.Count >= 8, "Golden request hilang — jalankan tools/SyncNetPro.GoldenGenerator");
        Assert.True(ResponseCases.Count >= 3);
        Assert.True(FromCases.Count >= 8);
    }

    [Theory]
    [MemberData(nameof(RequestCases))]
    public void Request_round_trip_is_identical_to_legacy_json_and_frame(string name)
    {
        string legacyJson = GoldenFiles.ReadText(name + ".json");

        CoreRequest request = Codec.Serializer.Deserialize<CoreRequest>(legacyJson);

        Assert.Equal(legacyJson, Codec.Serializer.Serialize(request));
        Assert.Equal(GoldenFiles.ReadBytes(name + ".frame.bin"), Codec.EncodeFrame(request));
    }

    [Theory]
    [MemberData(nameof(ResponseCases))]
    public void Response_round_trip_is_identical_to_legacy_json_and_frame(string name)
    {
        string legacyJson = GoldenFiles.ReadText(name + ".json");

        CoreResponse response = Codec.Serializer.Deserialize<CoreResponse>(legacyJson);

        Assert.Equal(legacyJson, Codec.Serializer.Serialize(response));
        Assert.Equal(GoldenFiles.ReadBytes(name + ".frame.bin"), Codec.EncodeFrame(response));
    }

    [Theory]
    [MemberData(nameof(FromCases))]
    public void CoreResponse_From_matches_legacy_response_with_decision_Q2(string name)
    {
        CoreRequest request = Codec.Serializer.Deserialize<CoreRequest>(GoldenFiles.ReadText(name + ".request.json"));

        CoreResponse response = CoreResponse.From(request);

        Assert.Equal(GoldenFiles.ReadText(name + ".expected.json"), Codec.Serializer.Serialize(response));
    }

    [Fact]
    public void New_objects_serialize_like_legacy_defaults()
    {
        Assert.Equal(GoldenFiles.ReadText("request-empty.json"), Codec.Serializer.Serialize(new CoreRequest()));
        Assert.Equal(GoldenFiles.ReadText("response-empty.json"), Codec.Serializer.Serialize(new CoreResponse()));
    }

    [Fact]
    public void MessageTypes_ToResponse_matches_legacy_GetMsgTypeResp()
    {
        var legacy = JsonConvert.DeserializeObject<Dictionary<string, string>>(GoldenFiles.ReadText("mti.json"))!;

        Assert.NotEmpty(legacy);
        foreach ((string request, string expected) in legacy)
        {
            Assert.Equal(expected, MessageTypes.ToResponse(request));
        }

        Assert.Equal(string.Empty, MessageTypes.ToResponse(null));
    }
}
