using Microsoft.Extensions.Options;
using SyncNet.Template.Iso;
using SyncNet.Template.Mapping;
using SyncNetPro.Contracts;
using SyncNetPro.Iso8583;

namespace SyncNet.Template.Tests;

public class MappingTests
{
    private static readonly ToRemote ToRemote = new(Options.Create(new BillerOptions { AcquirerId = "008" }));

    private static CoreRequest Request(string tranType) => new()
    {
        MessageType = "0200",
        TranType = tranType,
        Amount = 125000,
        TraceNumber = "000123",
        TransactionDateTime = "0930101530",
        TerminalId = "TERM01",
        MerchantId = "MERCHANT01",
        ReferenceNumber = "260930000123",
        ToAccountNumber = "532110000001",
    };

    [Fact]
    public void Inquiry_is_mapped_to_iso_0200()
    {
        IsoMessage iso = ToRemote.Inquiry(Request(TranType.Inquiry));

        Assert.Equal("0200", iso.Mti);
        Assert.Equal("380000", iso[3]);
        Assert.Equal("000000125000", iso[4]);
        Assert.Equal("TERM01  ", iso[41]);
        Assert.Equal("532110000001", iso[48]);
        Assert.Equal("000123|TERM01  ", BillerIsoSpec.CorrelationKey(iso));
    }

    [Fact]
    public void Reversal_carries_original_data()
    {
        CoreRequest request = Request(TranType.Reversal);
        request.OriginalData = "PAY0930101500000122";

        IsoMessage iso = ToRemote.Reversal(request);

        Assert.Equal("0400", iso.Mti);
        Assert.EndsWith("0200PAY0930101500000122", iso[90], StringComparison.Ordinal);
    }

    [Fact]
    public void Biller_response_is_mapped_to_core_response()
    {
        CoreRequest request = Request(TranType.Inquiry);
        IsoMessage reply = ToRemote.Inquiry(request).CreateResponse("00").Set(48, "BUDI SANTOSO");

        CoreResponse response = new ToCore().From(reply, request);

        Assert.Equal("00", response.ResponseCode);
        Assert.Equal("0210", response.MessageType);
        Assert.Equal("BUDI SANTOSO", response.AdditionalData!["bill_info"]);
    }
}
