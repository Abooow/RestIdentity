namespace RestIdentity.Server.Services;

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string message);
}
