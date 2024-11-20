using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RoomKeypadManager
{
    public class CsvProcessor
    {
        public static Dictionary<string, List<DeviceData>> ProcessCsvFile(string filePath)
        {
            var structuredData = new Dictionary<string, List<DeviceData>>();

            using (var reader = new StreamReader(filePath))
            {
                for (int i = 0; i < 6; i++) // 最初の6行をスキップ
                {
                    reader.ReadLine();
                }

                string line;
                string lastDeviceName = null;

                while ((line = reader.ReadLine()) != null)
                {
                    string[] columns = line.Split(',');

                    if (columns.Length < 6)
                    {
                        continue; // 必須列がない行をスキップ
                    }

                    string fullPath = columns[0];
                    string model = columns[1];
                    string id = columns[2];
                    string dColumn = columns[3];
                    string fColumn = columns[5];

                    if (string.IsNullOrWhiteSpace(fullPath) && !string.IsNullOrWhiteSpace(lastDeviceName))
                    {
                        fullPath = lastDeviceName;
                    }

                    if (!string.IsNullOrWhiteSpace(fullPath))
                    {
                        lastDeviceName = fullPath;
                    }

                    if (string.IsNullOrWhiteSpace(fullPath) || !fullPath.Contains("\\"))
                    {
                        continue;
                    }

                    string[] parts = fullPath.Split('\\');
                    string roomKey = string.Join("\\", parts.Take(parts.Length - 1));
                    string deviceName = parts.Last();

                    List<string> buttons = new List<string>();
                    if (dColumn.Contains("Button"))
                    {
                        buttons.Add(fColumn);
                    }

                    if (!structuredData.ContainsKey(roomKey))
                    {
                        structuredData[roomKey] = new List<DeviceData>();
                    }

                    var existingDevice = structuredData[roomKey].FirstOrDefault(d => d.DeviceName == deviceName);

                    if (existingDevice != null)
                    {
                        existingDevice.Buttons.AddRange(buttons);
                    }
                    else
                    {
                        structuredData[roomKey].Add(new DeviceData
                        {
                            DeviceName = deviceName,
                            Model = model,
                            ID = id,
                            Buttons = buttons
                        });
                    }
                }
            }

            return structuredData;
        }
    }
}
