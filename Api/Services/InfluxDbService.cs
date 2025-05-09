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
				// TODO: 관리형 상태(관리형 개체)를 삭제합니다.
				_client.Dispose();
			}

			// TODO: 비관리형 리소스(비관리형 개체)를 해제하고 종료자를 재정의합니다.
			// TODO: 큰 필드를 null로 설정합니다.
			disposedValue = true;
		}
	}

	// // TODO: 비관리형 리소스를 해제하는 코드가 'Dispose(bool disposing)'에 포함된 경우에만 종료자를 재정의합니다.
	// ~InfluxDbService()
	// {
	//     // 이 코드를 변경하지 마세요. 'Dispose(bool disposing)' 메서드에 정리 코드를 입력합니다.
	//     Dispose(disposing: false);
	// }

	public void Dispose() {
		// 이 코드를 변경하지 마세요. 'Dispose(bool disposing)' 메서드에 정리 코드를 입력합니다.
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}