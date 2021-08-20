using System.ComponentModel.DataAnnotations;

namespace RestIdentity.Shared.Models;

public sealed record EmailAddress([EmailAddress] string Email);
