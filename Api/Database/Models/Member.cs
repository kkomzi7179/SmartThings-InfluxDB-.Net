using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

using Microsoft.EntityFrameworkCore;

namespace Api.Database.Models;

[Index(nameof(UserEmail), IsUnique = true)]
public class Member {
	public const int IDDefaut = 0;
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public virtual long ID { get; set; } = IDDefaut;
	[EmailAddress]
	public string UserEmail { get; set; }
	public string? AccessToken { get; private set; }
	public string? RefreshToken { get; private set; }
	public DateTime ExpiryTime { get; private set; } = DateTime.MinValue;

	[JsonIgnore]
	[NotMapped]
	public bool IsExpired => DateTime.UtcNow >= ExpiryTime;

	public void UpdateTokens(string accessToken, string refreshToken, DateTime expiryTime) {
		AccessToken = accessToken;
		RefreshToken = refreshToken;
		ExpiryTime = expiryTime;
	}

	public override string ToString() {
		return $"[{ExpiryTime:yyyy-MM-dd HH:mm:ss.fff zzz}] {AccessToken} / {RefreshToken}";
	}
}