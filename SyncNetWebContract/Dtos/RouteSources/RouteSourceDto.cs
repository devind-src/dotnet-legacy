namespace SyncNetApi.Dtos.RouteSources
{
    public record RouteSourceDto(int Id, int NodeIdIn, int NodeIdOut, string? InstId, string? Notes);
}
