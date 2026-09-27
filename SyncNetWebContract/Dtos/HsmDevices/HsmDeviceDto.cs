namespace SyncNetApi.Dtos.HsmDevices
{
    public record HsmDeviceDto(
        string HsmName,
        short? Priority,
        string? Protocol,
        bool UseScheme,
        short? MessageHeader,
        string? RemoteIp,
        string? RemotePort);
}
