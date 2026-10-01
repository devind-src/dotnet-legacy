using SyncNetPro.Contracts;
using SyncNetPro.Routing;

namespace SyncNet.Template.Routing;

/// <summary>
/// Routing biller &amp; fee untuk bill payment/purchase (padanan <c>ApplyFeesAndRouting</c> ApiChannel lama).
/// Produk = <c>receiving_inst_id</c>. Supplier terpilih dikirim ke Core lewat <c>private_data.sink_node</c>; tanpa
/// <c>sink_node</c> Core memakai routing statis (primary).
/// </summary>
public sealed class RoutingStep(RoutingResolver resolver, ProductFeeCalculator fees)
{
    // TODO(6): produk topup memakai margin routing (resolver.IsMarginRouting / ResolveMarginAsync + MarginCalculator).

    /// <returns><c>false</c> = ditolak Jadwal Routing (tidak ada biller buka).</returns>
    public async Task<bool> ApplyAsync(CoreRequest request, CancellationToken cancellationToken)
    {
        string? productId = request.ReceivingInstitutionId;
        if (string.IsNullOrEmpty(productId)) return true;

        (string mode, int? staticNodeId) = fees.GetRoutingMode(productId, request.MerchantId);
        string? nodeName = null;
        if (resolver.HasProductRoute(productId))
        {
            RoutingDecision decision = await resolver.ResolveProductAsync(RoutingContext.From(request), mode, staticNodeId, cancellationToken);
            if (decision.Rejected) return false;

            if (decision.Apply && !string.IsNullOrEmpty(decision.NodeName))
            {
                request.PrivateData ??= new PrivateData();
                request.PrivateData.SinkNode = decision.NodeName;
                nodeName = decision.NodeName;
            }
            else
            {
                nodeName = resolver.GetProductPrimary(productId);
            }
        }

        // Fee: Product > Fees per CA, lalu default produk; mode dinamis memakai sharing fee biller siklus ini.
        int? sharingFee = RoutingModes.IsDynamic(mode) && !string.IsNullOrEmpty(nodeName) ? resolver.GetProductSharingFee(productId, nodeName) : null;
        request.Fees = fees.Calculate(request.MerchantId, productId, request.Amount, sharingFee);
        return true;
    }

    /// <summary>Hasil supplier: volume (Volume &amp; Tiering) dan health check (failover).</summary>
    public async Task RecordResultAsync(CoreRequest request, CoreResponse response, int latencyMs, CancellationToken cancellationToken)
    {
        string? productId = request.ReceivingInstitutionId;
        if (string.IsNullOrEmpty(productId) || !resolver.HasProductRoute(productId)) return;

        string? switchKey = SwitchKeys.Build(request.TranType, request.TransactionDateTime, request.TraceNumber, request.TerminalId);
        string? originalKey = request.TranType is TranType.Advice or TranType.Reversal ? SwitchKeys.BuildOriginal(request.OriginalData, request.TerminalId) : null;
        DateTime? date = SwitchKeys.ParseTransactionDate(request.TransactionDateTime);

        string? supplier = request.PrivateData?.SinkNode;
        if (string.IsNullOrEmpty(supplier))
        {
            // Dirutekan statis oleh Core: volume dicatat ke primary, tanpa health check.
            resolver.RecordVolume(resolver.GetProductPrimary(productId), RoutingTypes.Product, request.TranType, response.ResponseCode, productId, request.Amount,
                switchKey, originalKey, date);
            return;
        }

        await resolver.RecordResultAsync(supplier, RoutingTypes.Product, request.TranType, response.ResponseCode, productId, (long)request.Amount,
            request.TraceNumber, latencyMs, request.Amount, switchKey, originalKey, date, cancellationToken);
    }
}
