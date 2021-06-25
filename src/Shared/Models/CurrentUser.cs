using System.Collections.Generic;

namespace RestIdentity.Shared.Models
{
    public sealed class CurrentUser
    {
        public bool IsAuthenticated { get; set; }
        public string Email { get; set; }
        public Dictionary<string, string> Claims { get; set; }

        public CurrentUser()
        {
            Claims = new Dictionary<string, string>();
        }
    }
}