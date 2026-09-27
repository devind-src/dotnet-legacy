using System.Threading;
using System.Threading.Tasks;

namespace SyncNetApi.Services.Notifications
{
    public interface IEmailSender
    {
        Task SendPasswordResetLinkAsync(string toEmail, string userName, string resetLink, CancellationToken ct = default);
    }
}
