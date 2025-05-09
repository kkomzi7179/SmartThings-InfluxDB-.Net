namespace Api.Services;

using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;

using Microsoft.Extensions.Options;

using Api.Entities.Options;
public interface IInfluxDbService {
	void Dispose();
	Task WriteSensorDataAsync(DateTime timestamp, string deviceId, string deviceName, string attribute, double value);
	Task WriteSensorDataAsync(string deviceId, string deviceName, double? temperature, double? humidity);
}

public class InfluxDbService : IDisposable, IInfluxDbService {
	private bool disposedValue;

	readonly string Mesurement = "DeviceData";
	readonly string TagDeviceId = "DeviceId";
	readonly string TagDeviceName = "DeviceName";

	private readonly InfluxDBClient _client;
	private readonly string _bucket;
	private readonly string _org;
	readonly InfluxDBOption _influxDBOption;
	public InfluxDbService(IOptions<InfluxDBOption> influxDBOption) {
		_influxDBOption = influxDBOption.Value;

		var url = _influxDBOption.Url;
		var token = _influxDBOption.Token;
		_bucket = _influxDBOption.Bucket;
		_org = _influxDBOption.Org;

		_client = new InfluxDBClient(url, token);
	}

	public async Task WriteSensorDataAsync(DateTime timestamp, string deviceId, string deviceName, string attribute, double value) {
		var writeApi = _client.GetWriteApiAsync();

		var point = PointData.Measurement(Mesurement).Tag(TagDeviceId, deviceId).Tag(TagDeviceName, deviceName).Field(attribute, value).Timestamp(timestamp, WritePrecision.Ns);

		await writeApi.WritePointAsync(point, _bucket, _org);
	}

	public async Task WriteSensorDataAsync(string deviceId, string deviceName, double? temperature, double? humidity) {
		var writeApi = _client.GetWriteApiAsync();

		var point = PointData.Measurement(Mesurement).Tag(TagDeviceId, deviceId).Tag(TagDeviceName, deviceName);
		if(temperature.HasValue) {
			point = point.Field("temperature", temperature);
		}
		if(humidity.HasValue) {
			point = point.Field("humidity", humidity);
		}
		point = point.Timestamp(DateTime.UtcNow, WritePrecision.Ns);

		await writeApi.WritePointAsync(point, _bucket, _org);
	}

	protected virtual void Dispose(bool disposing) {
		if(!disposedValue) {
			if(disposing) {
				// TODO: ������ ����(������ ��ü)�� �����մϴ�.
				_client.Dispose();
			}

			// TODO: ������� ���ҽ�(������� ��ü)�� �����ϰ� �����ڸ� �������մϴ�.
			// TODO: ū �ʵ带 null�� �����մϴ�.
			disposedValue = true;
		}
	}

	// // TODO: ������� ���ҽ��� �����ϴ� �ڵ尡 'Dispose(bool disposing)'�� ���Ե� ��쿡�� �����ڸ� �������մϴ�.
	// ~InfluxDbService()
	// {
	//     // �� �ڵ带 �������� ������. 'Dispose(bool disposing)' �޼��忡 ���� �ڵ带 �Է��մϴ�.
	//     Dispose(disposing: false);
	// }

	public void Dispose() {
		// �� �ڵ带 �������� ������. 'Dispose(bool disposing)' �޼��忡 ���� �ڵ带 �Է��մϴ�.
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}