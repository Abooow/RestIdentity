using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestIdentity.Server.Models;
public class TokenModel
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(450)]
    public string UserId { get; set; }

    [Required]
    [MaxLength(450)]
    public string ClientId { get; set; }

    [Required]
    public string Value { get; set; }

    [Required]
    public string EncryptionKeyRt { get; set; }

    [Required]
    public string EncryptionKeyJwt { get; set; }

    [Required]
    public DateTime ExpiryDate { get; set; }

    [Required]
    public DateTime LastModifiedDate { get; set; }

    [Required]
    public DateTime DateCreated { get; set; }

    [ForeignKey("userId")]
    public virtual ApplicationUser User { get; set; }
}
