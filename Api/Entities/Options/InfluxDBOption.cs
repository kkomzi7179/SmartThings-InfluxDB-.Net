namespace Api.Entities.Options;
public class InfluxDBOption {
	public string Url { get; set; }
	public string Token { get; set; }
	public string Bucket { get; set; }
	public string Org { get; set; }
}