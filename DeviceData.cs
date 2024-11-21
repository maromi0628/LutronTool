public class DeviceData
{
    public string DeviceName { get; set; }
    public string Model { get; set; }
    public string ID { get; set; }
    public List<string> Buttons { get; set; } = new List<string>();
    public List<string> DColumns { get; set; } = new List<string>(); // D列の情報
}