using System.Collections.Generic;

namespace RoomKeypadManager
{
    public class DeviceData
    {
        public string DeviceName { get; set; }
        public string Model { get; set; }
        public string ID { get; set; }
        public List<string> Buttons { get; set; } = new List<string>();
    }
}
