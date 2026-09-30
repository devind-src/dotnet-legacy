using SyncNetPro.Contracts;

namespace SyncNetPro.Sdk.Testing;

/// <summary>Menentukan balasan SimCore untuk request dari interface (kanal source).</summary>
internal static class SourceResponder
{
    public static (CoreResponse? Response, int DelayMs) Respond(SourceResponderOptions options, CoreRequest request)
    {
        if (options.Mode == SourceResponseMode.None) return (null, 0);

        ResponseRule? rule = options.Mode == SourceResponseMode.Rules ? options.Rules.FirstOrDefault(r => Matches(r, request)) : null;
        CoreResponse response = request.ToResponse(rule?.ResponseCode ?? options.ResponseCode, rule?.ResponseMessage ?? options.ResponseMessage);
        if (rule?.AdditionalData is not null)
        {
            foreach ((string key, object? value) in rule.AdditionalData) response.SetAdditionalData(key, value);
        }

        return (response, rule?.DelayMs ?? options.DelayMs);
    }

    private static bool Matches(ResponseRule rule, CoreRequest request) =>
        (rule.TranType is null || string.Equals(rule.TranType, request.TranType, StringComparison.OrdinalIgnoreCase))
        && (rule.TranTypeExt is null || string.Equals(rule.TranTypeExt, request.TranTypeExt, StringComparison.OrdinalIgnoreCase))
        && (rule.MinAmount is null || request.Amount >= rule.MinAmount)
        && (rule.MaxAmount is null || request.Amount <= rule.MaxAmount);
}
