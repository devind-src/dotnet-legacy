using System.Threading.Tasks;

namespace SyncNet.Routing
{
    public interface IRoutingStrategy
    {
        Task Initialize();

        bool IsApplicable(string productId);

        Task<string> ResolveSupplierAsync(string productId, long denom);
    }
}
