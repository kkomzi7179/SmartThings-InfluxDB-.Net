namespace Api.Entities.Options;
public class SmartThingsOption {	
	public string ClientId { get; set; }
	public string ClientSecret { get; set; }
	public string RedirectUri { get; set; }
	public string Scope { get; set; }
	public TargetInfo[] Targets { get; set; }
}
public class TargetInfo {
	public string DeviceId { get; set; }
	public string DeviceName { get; set; }
	public AttributeInfo[] Attributes { get; set; }
}
public class AttributeInfo {
	public string Capability { get; set; }
	public string Attribute { get; set; }
}