namespace SyncNetPro.Contracts.Tests;

public class CoreResponseFromTests
{
    private static CoreRequest Request() => new()
    {
        MessageType = "0200",
        TranType = TranType.Payment,
        FromAccountType = "10",
        ToAccountType = "20",
        PosEntryMode = "021",
        TraceNumber = "000123",
        TransactionDateTime = "0930101530",
        TerminalId = "TERM0001",
        TempData = "temp",
        EchoData = "echo",
    };

    [Fact]
    public void Fills_response_mti_and_copies_pos_entry_mode()
    {
        CoreResponse response = CoreResponse.From(Request());

        Assert.Equal("0210", response.MessageType);
        Assert.Equal("021", response.PosEntryMode);
    }

    [Fact]
    public void Keeps_core_correlation_fields()
    {
        CoreRequest request = Request();

        CoreResponse response = CoreResponse.From(request);

        Assert.Equal(request.TranType, response.TranType);
        Assert.Equal(request.TransactionDateTime, response.TransactionDateTime);
        Assert.Equal(request.TraceNumber, response.TraceNumber);
        Assert.Equal(request.TerminalId, response.TerminalId);
    }

    [Fact]
    public void Does_not_replicate_core_to_acc_type_bug()
    {
        // Core: to_acc_type = req.from_acc_type (bug). SDK baru menyalin to_acc_type apa adanya.
        CoreResponse response = CoreResponse.From(Request());

        Assert.Equal("10", response.FromAccountType);
        Assert.Equal("20", response.ToAccountType);
    }

    [Fact]
    public void Does_not_copy_temp_data_and_defaults_to_external()
    {
        CoreResponse response = CoreResponse.From(Request());

        Assert.Null(response.TempData);
        Assert.Equal("echo", response.EchoData);
        Assert.Equal(AuthorizedBy.External, response.AuthorizedBy);
    }

    [Fact]
    public void Modifying_response_does_not_modify_request()
    {
        CoreRequest request = Request().SetAdditionalData("a", 1);
        request.Fees!.TotalFee = 1000m;
        request.PrivateData!.ConnectionName = "CONN";

        CoreResponse response = CoreResponse.From(request);
        response.SetAdditionalData("b", 2);
        response.Fees!.TotalFee = 0m;
        response.PrivateData!.ConnectionName = "OTHER";

        Assert.False(request.AdditionalData!.ContainsKey("b"));
        Assert.Equal(1000m, request.Fees.TotalFee);
        Assert.Equal("CONN", request.PrivateData.ConnectionName);
        Assert.Equal("CONN", CoreResponse.From(request).PrivateData!.ConnectionName);
    }

    [Fact]
    public void ToResponse_sets_code_message_and_authorizer()
    {
        CoreResponse response = Request().ToResponse("A1", "Transaction is not supported", AuthorizedBy.Internal);

        Assert.Equal("A1", response.ResponseCode);
        Assert.Equal("Transaction is not supported", response.ResponseMessage);
        Assert.Equal(AuthorizedBy.Internal, response.AuthorizedBy);
        Assert.Equal("0210", response.MessageType);
    }

    [Fact]
    public void Null_request_throws() => Assert.Throws<ArgumentNullException>(() => CoreResponse.From(null!));
}
