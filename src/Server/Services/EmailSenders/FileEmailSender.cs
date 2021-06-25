using System;
using System.IO;
using System.Threading.Tasks;

namespace RestIdentity.Server.Services.EmailSenders
{
    public class FileEmailSender : IEmailSender
    {
        public Task SendAsync(string to, string subject, string message)
        {
            using (StreamWriter writer = new StreamWriter("Emails.txt", true))
            {
                writer.WriteLine($"Time: {DateTime.Now}");
                writer.WriteLine($"    To: {to}, Subject: {subject}, Message: {message}");
                writer.WriteLine();
                writer.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
                writer.WriteLine();
            }

            return Task.CompletedTask;
        }
    }
}
