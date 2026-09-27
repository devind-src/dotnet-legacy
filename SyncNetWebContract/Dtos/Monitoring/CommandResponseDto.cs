namespace SyncNetApi.Dtos.Monitoring
{
    /// <summary>Empty Response means the probe timed out or the socket never connected —
    /// mirrors legacy NbCommand.Send, which returns an empty string in both cases. The
    /// frontend shows "No Response" for an empty string, same wording as legacy.</summary>
    public record CommandResponseDto(string Response);
}
