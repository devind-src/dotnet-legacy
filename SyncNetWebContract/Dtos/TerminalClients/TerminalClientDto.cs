namespace SyncNetApi.Dtos.TerminalClients
{
    public record TerminalClientDto(
        string ClientId,
        string ClientName,
        string? Username,
        string? Password,
        string? SecretId,
        string? SecretKey,
        string? MasterKey,
        string? SessionKey,
        string? GroupName,
        string? SubgroupName,
        string? CallbackUrl,
        string? CallbackClientId,
        string? CallbackSecretKey);
}
