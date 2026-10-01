using Microsoft.Extensions.Options;
using SyncNet.Template.Mapping;
using SyncNet.Template.Models;
using SyncNetPro.Contracts;

namespace SyncNet.Template.Tests;

public class MappingTests
{
    private static readonly ToCore ToCore = new(Options.Create(new ChannelOptions { AcquirerId = "008" }), TimeProvider.System);

    private static ChannelRequest Request() => new()
    {
        Reference = "260930000123",
        TraceNumber = "000123",
        TerminalId = "TERM0001",
        MerchantId = "MERCHANT01",
        ProductCode = "PLNPOST",
        CustomerId = "532110000001",
        Amount = 125000,
    };

    [Theory]
    [InlineData("/inquiry", "INQ")]
    [InlineData("/payment/", "PAY")]
    [InlineData("/ADVICE", "ADV")]
    [InlineData("/reversal", "REV")]
    [InlineData("/other", null)]
    public void Path_is_mapped_to_tran_type(string path, string? tranType) => Assert.Equal(tranType, ToCore.TranTypeFor(path));

    [Fact]
    public void Channel_request_is_mapped_to_core_request()
    {
        CoreRequest core = ToCore.Map(TranType.Reversal, Request());

        Assert.Equal("0400", core.MessageType);
        Assert.Equal("PLNPOST", core.ReceivingInstitutionId);
        Assert.Equal("008", core.AcquirerInstitutionId);
        Assert.Equal(10, core.TransactionDateTime!.Length);
    }

    [Fact]
    public void Core_response_is_mapped_to_channel_response()
    {
        CoreResponse response = ToCore.Map(TranType.Payment, Request()).ToResponse("00", "Approved");
        response.SetAdditionalData("receipt", "R1");

        ChannelResponse reply = new ToChannel().From(response);

        Assert.Equal("00", reply.ResponseCode);
        Assert.Equal("R1", reply.Data!["receipt"]);
        Assert.Null(reply.Fee);
    }
}
