using System.Collections.Generic;
using System.IO;

namespace RoomKeypadManager
{
    public class CsvProcessor
    {
        public static Dictionary<string, List<DeviceData>> ProcessCsvFile(string filePath)
        {
            var structuredData = new Dictionary<string, List<DeviceData>>();
            var deviceNameCounter = new Dictionary<string, int>(); // デバイス名のカウンタ
            string lastRoomKey = null; // 直前の部屋キーを保持
            string lastDeviceName = null; // 直前のデバイス名を保持
            string lastDeviceID = null;   // 直前のデバイスIDを保持

            using (var reader = new StreamReader(filePath))
            {
                // 最初の6行をスキップ
                for (int i = 0; i < 6; i++)
                {
                    reader.ReadLine();
                }

                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] columns = line.Split(',');

                    // 必須列がない行をスキップ
                    if (columns.Length < 6)
                    {
                        continue;
                    }

                    string fullPath = columns[0]; // A列: フルパス
                    string model = columns[1];    // B列: モデル
                    string id = columns[2];       // C列: ID
                    string dColumn = columns[3];  // D列: 判定用
                    string fColumn = columns[5];  // F列: ボタン名

                    // C列が空白の場合はスキップ
                    if (string.IsNullOrWhiteSpace(id))
                    {
                        // ボタン情報として処理
                        if (!string.IsNullOrWhiteSpace(dColumn) && dColumn.Contains("Button") && !string.IsNullOrWhiteSpace(fColumn))
                        {
                            // 最後に登録された部屋キーとデバイスにボタンを追加
                            if (!string.IsNullOrWhiteSpace(lastRoomKey) && !string.IsNullOrWhiteSpace(lastDeviceName) && structuredData.ContainsKey(lastRoomKey))
                            {
                                var existingDevice = structuredData[lastRoomKey]
                                    .FirstOrDefault(device => device.DeviceName == lastDeviceName && device.ID == lastDeviceID);

                                if (existingDevice != null)
                                {
                                    existingDevice.Buttons.Add(fColumn);
                                }
                            }
                        }
                        continue;
                    }

                    // A列が空白の場合、直前の部屋キーとデバイス名を使用
                    if (string.IsNullOrWhiteSpace(fullPath) && !string.IsNullOrWhiteSpace(lastRoomKey) && !string.IsNullOrWhiteSpace(lastDeviceName))
                    {
                        fullPath = $"{lastRoomKey}\\{lastDeviceName}";
                    }

                    if (string.IsNullOrWhiteSpace(fullPath) || !fullPath.Contains("\\"))
                    {
                        continue; // 無効なデータをスキップ
                    }

                    // 部屋キーとデバイス名を分割
                    string[] parts = fullPath.Split('\\');
                    string roomKey = string.Join("\\", parts.Take(parts.Length - 1)); // 部屋キー
                    string deviceName = parts.Last(); // デバイス名

                    // ボタン情報を取得
                    List<string> buttons = new List<string>();
                    if (dColumn.Contains("Button"))
                    {
                        buttons.Add(fColumn);
                    }

                    if (!structuredData.ContainsKey(roomKey))
                    {
                        structuredData[roomKey] = new List<DeviceData>();
                    }

                    // デバイス名に番号を付ける
                    string uniqueDeviceName = deviceName;
                    if (!deviceNameCounter.ContainsKey(deviceName))
                    {
                        deviceNameCounter[deviceName] = 1;
                    }
                    else
                    {
                        deviceNameCounter[deviceName]++;
                        uniqueDeviceName = $"{deviceName}({deviceNameCounter[deviceName]})";
                    }

                    // 新しいデバイスを追加
                    structuredData[roomKey].Add(new DeviceData
                    {
                        DeviceName = uniqueDeviceName,
                        Model = model,
                        ID = id,
                        Buttons = buttons
                    });

                    // 現在のデバイス情報を保持
                    lastRoomKey = roomKey;
                    lastDeviceName = uniqueDeviceName;
                    lastDeviceID = id;
                }
            }

            return structuredData;
        }
    }
}
