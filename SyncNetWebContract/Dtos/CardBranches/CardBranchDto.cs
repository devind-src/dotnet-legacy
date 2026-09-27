namespace SyncNetApi.Dtos.CardBranches
{
    public record CardBranchDto(
        int Id,
        string Issuer,
        string? IdBranch,
        string? Branch,
        string? Address,
        string? City,
        string? Phone,
        string? Fax,
        string? Email,
        string? Contact);
}
