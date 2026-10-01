using Microsoft.Extensions.Options;
using SyncNet.Template.Mapping;
using SyncNet.Template.Models;
using SyncNetPro.Contracts;

namespace SyncNet.Template.Tests;

public class MappingTests
{
    private static readonly ToRemote ToRemote = new(Options.Create(new BillerOptions { PartnerId = "P01" }));

    private static CoreRequest Request(string tranType) => new()
    {
        MessageType = "0200",
        TranType = tranType,
        TranTypeExt = "PLNPOST",
        Amount = 125000,
        TraceNumber = "000123",
        TransactionDateTime = "0930101530",
        TerminalId = "TERM01",
        MerchantId = "MERCHANT01",
        ReferenceNumber = "260930000123",
        ToAccountNumber = "532110000001",
    };

    [Fact]
    public void Inquiry_is_mapped_to_inquiry_endpoint()
    {
        (string path, BillerRequest body) = ToRemote.Map(Request(TranType.Inquiry))!.Value;

        Assert.Equal("/bill/inquiry", path);
        Assert.Equal("P01", body.PartnerId);
        Assert.Equal("532110000001", body.CustomerId);
        Assert.Equal(125000, body.Amount);
    }

    [Fact]
    public void Unsupported_transaction_is_not_mapped()
    {
        Assert.Null(ToRemote.Map(Request("XYZ")));
        Assert.Equal("k", ToRemote.Headers("k")["X-Api-Key"]);
    }

    [Fact]
    public void Biller_response_is_mapped_to_core_response()
    {
        CoreRequest request = Request(TranType.Inquiry);

        CoreResponse response = new ToCore().From(new BillerResponse { ResponseCode = "00", CustomerName = "BUDI SANTOSO", Amount = 130000 }, request);

        Assert.Equal("00", response.ResponseCode);
        Assert.Equal("0210", response.MessageType);
        Assert.Equal(130000, response.Amount);
        Assert.Equal("BUDI SANTOSO", response.AdditionalData!["customer_name"]);
    }
}
