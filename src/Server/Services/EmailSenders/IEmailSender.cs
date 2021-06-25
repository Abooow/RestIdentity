using System.Threading.Tasks;

namespace RestIdentity.Server.Services.EmailSenders
{
    public interface IEmailSender
    {
        Task SendAsync(string to, string subject, string message);
    }
}
