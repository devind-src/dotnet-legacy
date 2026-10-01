using SyncNet.Template.Models;
using SyncNetPro.Contracts;

namespace SyncNet.Template.Mapping;

/// <summary>Response HTTP biller → CoreResponse.</summary>
public sealed class ToCore
{
    // TODO(3): petakan response code biller ke response code Core bila berbeda.
    public CoreResponse From(BillerResponse response, CoreRequest request)
    {
        string rc = string.IsNullOrEmpty(response.ResponseCode) ? "06" : response.ResponseCode;
        CoreResponse result = request.ToResponse(rc, response.Message);
        if (response.CustomerName is not null) result.SetAdditionalData("customer_name", response.CustomerName);
        if (response.BillInfo is not null) result.SetAdditionalData("bill_info", response.BillInfo);
        if (response.Amount is decimal amount && request.TranType == TranType.Inquiry) result.Amount = amount;
        return result;
    }

    /// <summary>HTTP non-2xx atau body bukan JSON: Core mendapat 96 (system malfunction) dari interface.</summary>
    public CoreResponse SystemError(CoreRequest request, string reason) =>
        request.ToResponse("96", reason, AuthorizedBy.Internal);
}
