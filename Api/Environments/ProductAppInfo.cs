namespace Api.Environments;

public static class ProductAppInfo {
	public const string AppName = "SmartThingsData";
	public const string DeveloperName = "Jaeyong Park";

	public const string AdminEmail = "admin@domain.com";
	public const string AdminPassword = "password";

	internal const string SwaggerRoute = "/Swagger";

	internal static readonly TimeSpan DelaySmartThingsTokenRefresh = TimeSpan.FromHours(1);
	internal static readonly TimeSpan DelaySmartThingsDataCollect = TimeSpan.FromMinutes(1);
}
