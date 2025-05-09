using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

using Api.Database.Models;

using Api.Entities.Options;
using Api.Environments;
using Api.Services.Database;

using Microsoft.Extensions.Options;

namespace Api.Services;
public interface ISmartThingsService {
	/// <summary>
	/// 토큰 정보 갱신 여부
	/// </summary>
	bool Initialized { get; }
	/// <summary>
	/// 토큰 정보가 갱신되고 유효기간 이내인 경우
	/// </summary>
	bool Authorized { get; }
	Member CurrentMember { get; }

	Task<string> GetAuthorizationUrlAsync();
	Task<(bool, string)> GetTokenWithCodeAsync(string code);
	Task Initialize();
	Task<(bool, string?)> RefreshTokenAsync(string? newRefreshToken = null);
	Task UpdateTargetDataAsync();
	Task<string> ViewTokenAsync();
}
/// <inheritdoc/>
public class SmartThingsService(IOptions<SmartThingsOption> smartThingsOptions, IHttpClientFactory httpClientFactory, IDbMemberService dbMemberService, IInfluxDbService influxDbService, ILogger<SmartThingsService> logger) : ISmartThingsService {
	public bool Initialized { get; private set; }
	public bool Authorized => Initialized && CurrentMember?.IsExpired == false;
	public Member CurrentMember { get; private set; }

	string clientId;
	string clientSecret;
	string redirectUri;
	string scope;
	TargetInfo[] targets = Array.Empty<TargetInfo>();
	public async Task Initialize() {
		#region Validation

		SmartThingsOption smartThingsOption = smartThingsOptions.Value;
		clientId = smartThingsOption.ClientId;
		if(string.IsNullOrWhiteSpace(clientId)) {
			throw new Exception($"{nameof(clientId)} is not set in appsettings");
		}
		clientSecret = smartThingsOption.ClientSecret;
		if(string.IsNullOrWhiteSpace(clientSecret)) {
			throw new Exception($"{nameof(clientSecret)} is not set in appsettings");
		}
		redirectUri = smartThingsOption.RedirectUri;
		if(string.IsNullOrWhiteSpace(redirectUri)) {
			throw new Exception($"{nameof(redirectUri)} is not set in appsettings");
		}
		scope = smartThingsOption.Scope;
		if(string.IsNullOrWhiteSpace(scope)) {
			throw new Exception($"{nameof(scope)} is not set in appsettings");
		}
		targets = smartThingsOption.Targets ?? Array.Empty<TargetInfo>();
		if(targets.Length == 0) {
			throw new Exception($"{nameof(targets)} is not set in appsettings");
		}
		
		#endregion

		// DB에서 멤버 정보 가져오기
		CurrentMember = await dbMemberService.GetByEmail(ProductAppInfo.AdminEmail);
		if(CurrentMember == null) {
			throw new Exception("Member not found in database.");
		}
	}

	public async Task<string> GetAuthorizationUrlAsync() {
		var responseType = "code";

		var authUrl = $"https://api.smartthings.com/oauth/authorize?client_id={clientId}&response_type={responseType}&redirect_uri={redirectUri}&scope={scope}";

		logger.LogInformation("Generated authorization URL: {AuthUrl}", authUrl);
		return await Task.FromResult(authUrl);
	}

	public async Task<(bool, string)> GetTokenWithCodeAsync(string code) {
		var tokenEndpoint = "https://api.smartthings.com/oauth/token";
		var grant_type = "authorization_code";

		using var client = new HttpClient();

		// Basic 인증 헤더 설정
		var credentials = $"{clientId}:{clientSecret}";
		var base64Credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64Credentials);

		// 토큰 요청 파라미터
		var postData = new FormUrlEncodedContent(new[]
		{
			new KeyValuePair<string, string>("grant_type", grant_type),
			new KeyValuePair<string, string>("client_id", clientId),
			new KeyValuePair<string, string>("code", code),
			new KeyValuePair<string, string>("redirect_uri", redirectUri)
		});

		// 토큰 요청
		var response = await client.PostAsync(tokenEndpoint, postData);

		var responseContent = await response.Content.ReadAsStringAsync();

		if(response.IsSuccessStatusCode) {
			var json = JsonDocument.Parse(responseContent);
			var accessToken = json.RootElement.GetProperty("access_token").GetString()!;
			var refreshToken = json.RootElement.GetProperty("refresh_token").GetString()!;
			var expiresIn = json.RootElement.GetProperty("expires_in").GetInt32();
			var expiryTime = DateTime.UtcNow.AddSeconds(expiresIn);

			// DB에 토큰 저장
			CurrentMember.UpdateTokens(accessToken, refreshToken, expiryTime);
			await dbMemberService.Update(CurrentMember);

			logger.LogInformation("Token successfully acquired and stored.");
			Initialized = true;
			return (true, responseContent);
		} else {
			logger.LogError($"Failed to refresh token: {response.StatusCode}, Response: {responseContent}{Environment.NewLine}{CurrentMember.ToString()}");
			Initialized = false;
			return (false, responseContent);
		}
	}

	public async Task<(bool, string?)> RefreshTokenAsync(string? newRefreshToken = null) {
		logger.LogInformation("Starting token refresh...");

		var refreshToken = newRefreshToken is not null ? newRefreshToken : CurrentMember.RefreshToken;
		var tokenEndpoint = "https://api.smartthings.com/oauth/token";
		var grant_type = "refresh_token";

		using var client = httpClientFactory.CreateClient();

		logger.LogDebug("Sending refresh request with RefreshToken : {0}", refreshToken);

		// 기본 인증 헤더 설정
		var credentials = $"{clientId}:{clientSecret}";
		var base64Credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64Credentials);

		// POST 파라미터 구성
		var postData = new FormUrlEncodedContent(new[]
		{
			new KeyValuePair<string, string>("grant_type", grant_type),
			new KeyValuePair<string, string>("client_id", clientId),
			new KeyValuePair<string, string>("client_secret", clientSecret),
			new KeyValuePair<string, string>("refresh_token", refreshToken)
		});

		// 요청 전송
		var response = await client.PostAsync(tokenEndpoint, postData);
		// 응답 처리
		var responseContent = await response.Content.ReadAsStringAsync();											

		if(response.IsSuccessStatusCode) {
			var json = JsonDocument.Parse(responseContent);

			var accessToken = json.RootElement.GetProperty("access_token").GetString()!;
			var refreshTokenNew = json.RootElement.GetProperty("refresh_token").GetString()!;
			var expiresIn = json.RootElement.GetProperty("expires_in").GetInt32();
			var expiryTime = DateTime.UtcNow.AddSeconds(expiresIn);

			logger.LogDebug("Received new {Member}", CurrentMember.ToString());

			// DB에 토큰 저장
			CurrentMember.UpdateTokens(accessToken, refreshTokenNew, expiryTime);
			await dbMemberService.Update(CurrentMember);

			logger.LogInformation("Access token refreshed successfully.");
			Initialized = true;
			return (true, responseContent);

		} else {
			logger.LogError($"Failed to refresh token: {response.StatusCode}, Response: {responseContent}{Environment.NewLine}{CurrentMember.ToString()}");
			Initialized = false;
			return (false, $"{response.StatusCode}{Environment.NewLine}{responseContent}");
		}
	}

	public async Task UpdateTargetDataAsync() {
		var timestamp = DateTime.UtcNow;
		logger.LogInformation("Loaded {Count} device IDs from config.", targets.Length);

		foreach(var target in targets) {
			try {
				logger.LogInformation("Collecting data for device: {DeviceName}", target.DeviceName);

				var statusJson = await GetDeviceStatusAsync(target.DeviceId);
				//logger.LogDebug("Device {DeviceName} status JSON: {StatusJson}", target.DeviceName, statusJson);
				var json = JsonDocument.Parse(statusJson);

				var components = json.RootElement.GetProperty("components");
				var main = components.GetProperty("main");

				foreach(var attribeteInfo in target.Attributes) {
					if(main.TryGetProperty(attribeteInfo.Capability, out var property)) {
						var value = property.GetProperty(attribeteInfo.Attribute).GetProperty("value").GetDouble();
						logger.LogInformation("Device {DeviceName}: {Attribute} = {Temp}", target.DeviceName, attribeteInfo.Attribute, value);
						await influxDbService.WriteSensorDataAsync(timestamp, target.DeviceId, target.DeviceName, attribeteInfo.Attribute, value);
					} else {
						logger.LogWarning("Device {DeviceName} does not contain {Attribute}", target.DeviceName, attribeteInfo.Attribute);
					}
				}
			} catch(Exception ex) {
				logger.LogError(ex, "Error collecting or writing data for device {DeviceName}", target.DeviceName);
			}
		}
	}

	private async Task<string> GetDeviceStatusAsync(string deviceId) {
		using var client = httpClientFactory.CreateClient();

		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CurrentMember.AccessToken);

		var response = await client.GetAsync($"https://api.smartthings.com/v1/devices/{deviceId}/status");

		response.EnsureSuccessStatusCode();

		return await response.Content.ReadAsStringAsync();
	}
	public async Task<string> ViewTokenAsync() => await Task.FromResult(CurrentMember.ToString());

}
