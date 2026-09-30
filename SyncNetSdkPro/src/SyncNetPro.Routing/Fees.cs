using SyncNetPro.Contracts;

namespace SyncNetPro.Routing;

/// <summary>
/// Harga supplier dan harga jual merchant untuk topup (<c>PriceRepository</c> SDK lama). Data diganti utuh saat
/// RESYNC (SDK lama mengosongkan dictionary di tempat sehingga request yang berjalan bisa membaca data kosong/rusak).
/// </summary>
public sealed class PriceBook(IRoutingDataStore store)
{
    private volatile Snapshot _data = Snapshot.Empty;

    /// <summary>Muat harga.</summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<SupplierPrice> suppliers = await store.GetSupplierPricesAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlyList<MerchantPrice> merchants = await store.GetMerchantPricesAsync(cancellationToken).ConfigureAwait(false);

        var price = new Dictionary<(string, string, int), SupplierPrice>();
        var groups = new Dictionary<(string, int), List<SupplierPrice>>();
        foreach (SupplierPrice p in suppliers.Where(p => p.IsActive))
        {
            price.TryAdd((p.SupplierId, p.ProductId, p.Denom), p);
            if (!groups.TryGetValue((p.ProductId, p.Denom), out List<SupplierPrice>? list)) groups[(p.ProductId, p.Denom)] = list = [];
            list.Add(p);
        }

        // Rank 1 = margin terbesar; tie-break harga beli termurah lalu supplier id (deterministik).
        var ranked = groups.ToDictionary(
            g => g.Key,
            g => (IReadOnlyList<SupplierPrice>)[.. g.Value.OrderByDescending(p => p.Margin).ThenBy(p => p.PurchasePrice).ThenBy(p => p.SupplierId, StringComparer.Ordinal)]);

        var merchant = new Dictionary<(string, string, int), int>();
        foreach (MerchantPrice m in merchants) merchant.TryAdd((m.MerchantId, m.ProductId, m.Denom), m.SellingPrice);

        _data = new Snapshot(ranked, price, merchant);
    }

    /// <summary>Supplier aktif untuk produk + denom, urut margin terbesar.</summary>
    public bool TryGetRankedSuppliers(string productId, long denom, out IReadOnlyList<SupplierPrice> suppliers)
    {
        if (denom is >= int.MinValue and <= int.MaxValue && _data.Ranked.TryGetValue((productId, (int)denom), out IReadOnlyList<SupplierPrice>? list))
        {
            suppliers = list;
            return true;
        }

        suppliers = [];
        return false;
    }

    /// <summary>Harga supplier.</summary>
    public bool TryGetSupplierPrice(string? supplierId, string productId, long denom, out SupplierPrice? price)
    {
        price = null;
        return supplierId is not null && denom is >= int.MinValue and <= int.MaxValue && _data.Supplier.TryGetValue((supplierId, productId, (int)denom), out price);
    }

    /// <summary>Harga jual khusus merchant.</summary>
    public bool TryGetMerchantSellingPrice(string? merchantId, string productId, long denom, out int sellingPrice)
    {
        sellingPrice = 0;
        return merchantId is not null && denom is >= int.MinValue and <= int.MaxValue && _data.Merchant.TryGetValue((merchantId, productId, (int)denom), out sellingPrice);
    }

    private sealed record Snapshot(
        Dictionary<(string, int), IReadOnlyList<SupplierPrice>> Ranked,
        Dictionary<(string, string, int), SupplierPrice> Supplier,
        Dictionary<(string, string, int), int> Merchant)
    {
        public static readonly Snapshot Empty = new([], [], []);
    }
}

/// <summary>Fee topup = margin supplier siklus ini (<c>MarginCalculator</c> SDK lama).</summary>
public sealed class MarginCalculator(PriceBook prices)
{
    /// <summary>
    /// <c>total_fee = switch_fee = harga jual (merchant bila ada) − harga beli supplier</c>; supplier tanpa harga = fee 0.
    /// </summary>
    public Fees Calculate(string? merchantId, string productId, long denom, string? supplierId)
    {
        var result = new Fees();
        if (!prices.TryGetSupplierPrice(supplierId, productId, denom, out SupplierPrice? price)) return result;

        int sellingPrice = prices.TryGetMerchantSellingPrice(merchantId, productId, denom, out int merchantPrice) ? merchantPrice : price!.SellingPrice;
        int margin = sellingPrice - price!.PurchasePrice;
        result.TotalFee = margin;
        result.SwitchFee = margin;
        return result;
    }
}

/// <summary>
/// Fee bill payment &amp; purchase dari Product Fees (<c>BillPaymentFeeCalculator</c> SDK lama). Pencarian baris:
/// produk + CA + sub CA → produk + CA → default produk.
/// </summary>
public sealed class ProductFeeCalculator(IRoutingDataStore store)
{
    private volatile Dictionary<string, FeeRule> _rules = [];

    /// <summary>Muat baris fee (baris pertama menang bila duplikat).</summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var rules = new Dictionary<string, FeeRule>();
        foreach (FeeRule rule in await store.GetFeeRulesAsync(cancellationToken).ConfigureAwait(false))
        {
            rules.TryAdd(Key(rule.ProductId, rule.MerchantId, rule.SubMerchantId), rule);
        }

        _rules = rules;
    }

    /// <summary>Baris fee yang berlaku.</summary>
    public FeeRule? FindRule(string productId, string? merchantId, string? subMerchantId = null)
    {
        Dictionary<string, FeeRule> rules = _rules;
        if (!string.IsNullOrEmpty(merchantId))
        {
            if (!string.IsNullOrEmpty(subMerchantId) && rules.TryGetValue(Key(productId, merchantId, subMerchantId), out FeeRule? sub)) return sub;
            if (rules.TryGetValue(Key(productId, merchantId, null), out FeeRule? mer)) return mer;
        }

        return rules.GetValueOrDefault(Key(productId, null, null));
    }

    /// <summary>Routing mode CA: dari baris CA bila diisi, selain itu baris default produk; tanpa baris = STATIC.</summary>
    public (string Mode, int? StaticNodeId) GetRoutingMode(string productId, string? merchantId, string? subMerchantId = null)
    {
        FeeRule? rule = FindRule(productId, merchantId, subMerchantId);
        if (!string.IsNullOrEmpty(rule?.RoutingMode)) return (RoutingModes.Normalize(rule.RoutingMode), rule.StaticNodeId);
        if (_rules.TryGetValue(Key(productId, null, null), out FeeRule? def) && !string.IsNullOrEmpty(def.RoutingMode))
        {
            return (RoutingModes.Normalize(def.RoutingMode), def.StaticNodeId);
        }

        return (RoutingModes.Static, null);
    }

    /// <summary>
    /// Hitung fee. <paramref name="sharingFee"/> (mode dynamic) = sharing fee biller siklus ini: biller menahan sisa admin
    /// fee, perusahaan mendapat sisa sharing fee. <c>null</c> = fee biller/switch yang diinput (STATIC).
    /// </summary>
    public Fees Calculate(string? merchantId, string productId, decimal amount, int? sharingFee = null, string? subMerchantId = null)
    {
        var result = new Fees();
        FeeRule? f = FindRule(productId, merchantId, subMerchantId);
        if (f is null) return result;

        if (f.IsFixedFee)
        {
            result.TotalFee = f.FixedFeeTotal;
            result.AcquirerFee = f.FixedFeeAcquirer;
            result.MerchantFee = f.FixedFeeMerchant;
            result.IssuerFee = f.FixedFeeIssuer;
            if (sharingFee is int sharing)
            {
                result.BillerFee = f.FixedFeeTotal - sharing;
                result.SwitchFee = sharing - f.FixedFeeAcquirer - f.FixedFeeMerchant - f.FixedFeeIssuer;
            }
            else
            {
                result.BillerFee = f.FixedFeeBiller;
                result.SwitchFee = f.FixedFeeSwitch;
            }
        }
        else
        {
            // Persen hanya untuk STATIC (mis. MDR): komponen dihitung dari total fee.
            result.TotalFee = f.PercentFeeTotal * amount / 100;
            result.AcquirerFee = f.PercentFeeAcquirer * result.TotalFee / 100;
            result.MerchantFee = f.PercentFeeMerchant * result.TotalFee / 100;
            result.IssuerFee = f.PercentFeeIssuer * result.TotalFee / 100;
            result.BillerFee = f.PercentFeeBiller * result.TotalFee / 100;
            result.SwitchFee = f.PercentFeeSwitch * result.TotalFee / 100;
        }

        return result;
    }

    private static string Key(string productId, string? merchantId, string? subMerchantId) =>
        $"{productId}|{(string.IsNullOrEmpty(merchantId) ? null : merchantId)}|{(string.IsNullOrEmpty(subMerchantId) ? null : subMerchantId)}";
}
