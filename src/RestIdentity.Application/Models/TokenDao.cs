using System.ComponentModel.DataAnnotations;

namespace RestIdentity.DataAccess.Models;

public class TokenDao
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(450)]
    public string UserId { get; set; }

    [Required]
    [MaxLength(64)]
    public string ClientId { get; set; }

    [Required]
    public string Value { get; set; }

    [Required]
    public Guid EncryptionKeyRefreshToken { get; set; }

    [Required]
    public Guid EncryptionKeyJwt { get; set; }

    [Required]
    public DateTime ExpiryDate { get; set; }

    public DateTime? LastModifiedDate { get; set; }

    [Required]
    public DateTime DateCreated { get; set; }

    public virtual UserDao User { get; set; }
}
