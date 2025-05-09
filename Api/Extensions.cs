using System.Text.Json;
using Api.Entities.Options;

namespace Api;

public static partial class Extensions {
	public static void AddConfig(this IServiceCollection services, IConfiguration config) {
		services.Configure<SmartThingsOption>(config.GetSection(nameof(SmartThingsOption)));
		services.Configure<InfluxDBOption>(config.GetSection(nameof(InfluxDBOption)));
	}
	public static string ExBeautify(this string jsonString) {
		if(string.IsNullOrWhiteSpace(jsonString))
			return "";

		var jDoc = JsonDocument.Parse(jsonString);
		return JsonSerializer.Serialize(jDoc, new JsonSerializerOptions { WriteIndented = true });
	}
}