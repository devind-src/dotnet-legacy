using SyncNet.Template.Iso;
using SyncNet.Template.Mapping;
using SyncNetPro.Contracts;
using SyncNetPro.Iso8583;

namespace SyncNet.Template.Tests;

public class MappingTests
{
    private static IsoMessage Iso(string mti, string processingCode) => new IsoMessage(AcquirerIsoSpec.Instance, mti)
        .Set(3, processingCode)
        .Set(4, "000000125000")
        .Set(7, "0930101530")
        .Set(11, "000123")
        .Set(37, "260930000123")
        .Set(41, "TERM0001")
        .Set(42, "MERCHANT01     ")
        .Set(48, "532110000001");

    [Fact]
    public void Payment_is_mapped_to_core_request()
    {
        CoreRequest request = new ToCore().Map(Iso("0200", "500000"));

        Assert.Equal(TranType.Payment, request.TranType);
        Assert.Equal(125000, request.Amount);
        Assert.Equal("MERCHANT01", request.MerchantId);
        Assert.Equal("000123", request.TraceNumber);
    }

    [Fact]
    public void Reversal_original_data_comes_from_field_90()
    {
        IsoMessage iso = Iso("0400", "500000").Set(90, "0200" + "000122" + "0930101500" + new string('0', 22));

        CoreRequest request = new ToCore().Map(iso);

        Assert.Equal(TranType.Reversal, request.TranType);
        Assert.Equal("PAY0930101500000122", request.OriginalData);
    }

    [Fact]
    public void Core_response_is_mapped_to_iso_response()
    {
        IsoMessage iso = Iso("0200", "380000");
        CoreResponse response = new ToCore().Map(iso).ToResponse("00", "Approved");
        response.SetAdditionalData("bill_info", "BUDI");

        IsoMessage reply = new ToIso().From(iso, response);

        Assert.Equal("0210", reply.Mti);
        Assert.Equal("00", reply[39]);
        Assert.Equal("BUDI", reply[48]);
    }
}
