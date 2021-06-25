using System.ComponentModel.DataAnnotations;

namespace RestIdentity.Shared.Models
{
    public sealed class EmailAddress
    {
        [EmailAddress]
        public string Email { get; set; }
    }
}
