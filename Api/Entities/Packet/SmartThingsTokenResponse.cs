using System.Text.Json.Serialization;

namespace Api.Entities.Packet;
public class SmartThingsTokenResponse {
	[JsonPropertyName("access_token")]
	public string AccessToken { get; set; }

	[JsonPropertyName("refresh_token")]
	public string RefreshToken { get; set; }

	[JsonPropertyName("expires_in")]
	public int ExpiresIn { get; set; }

	[JsonPropertyName("token_type")]
	public string TokenType { get; set; }

	[JsonPropertyName("scope")]
	public string Scope { get; set; }
}