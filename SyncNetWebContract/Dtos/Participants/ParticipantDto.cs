namespace SyncNetApi.Dtos.Participants
{
    public record ParticipantDto(
        string ParticipantId,
        string? InstId,
        string? Name,
        string? Address,
        string? City,
        string? Zipcode,
        string? Person,
        string? Phone,
        string? Fax,
        string? Email,
        string? VirtualAccount,
        string? AccNumber,
        bool Active,
        string? VaName);
}
