namespace SyncNetApi.Dtos.HsmServices
{
    public record HsmServiceDto(
        string HsmDesc,
        int? HsmPort,
        int? RequestTimeout,
        short? MessageHeader);
}
