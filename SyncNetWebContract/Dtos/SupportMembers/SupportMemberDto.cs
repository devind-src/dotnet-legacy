namespace SyncNetApi.Dtos.SupportMembers
{
    public record SupportMemberDto(int MemberId, int TeamId, string? TeamName, string? Name, string? Email, string? Phone, bool Active);
}
