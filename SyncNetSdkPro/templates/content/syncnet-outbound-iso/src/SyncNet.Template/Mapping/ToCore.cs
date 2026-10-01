using SyncNetPro.Contracts;
using SyncNetPro.Iso8583;

namespace SyncNet.Template.Mapping;

/// <summary>ISO 8583 biller → CoreResponse.</summary>
public sealed class ToCore
{
    public CoreResponse From(IsoMessage response, CoreRequest request)
    {
        // TODO(3): petakan response code biller ke response code Core bila berbeda.
        string rc = response[39] ?? "06";
        CoreResponse result = request.ToResponse(rc, rc == "00" ? "Approved" : "Declined by biller");

        // Data tambahan dari biller (mis. nama pelanggan, rincian tagihan) dikirim ke Core lewat additional_data.
        if (response[48] is { } billInfo) result.SetAdditionalData("bill_info", billInfo.Trim());
        if (response[38] is { } approvalCode) result.SetAdditionalData("approval_code", approvalCode);
        return result;
    }
}
