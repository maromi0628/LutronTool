using System.Collections.Generic;
using System.IO;

namespace RoomKeypadManager
{
    public class CsvProcessor
    {
        public static (Dictionary<string, List<DeviceData>>, Dictionary<string, Dictionary<string, string>>) ProcessCsvFile(string filePath)
        {
            var structuredData = new Dictionary<string, List<DeviceData>>();
            var additionalData = new Dictionary<string, Dictionary<string, string>>();

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

                // --- 1週目: 既存のデバイスデータ処理 ---
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
                                    existingDevice.Buttons.Add(fColumn); // ボタン名を追加
                                    existingDevice.DColumns.Add(dColumn); // D列の情報を追加
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
                    List<string> dColumns = new List<string>();

                    if (dColumn.Contains("Button") && !string.IsNullOrWhiteSpace(fColumn))
                    {
                        buttons.Add(fColumn); // F列のボタン名
                        dColumns.Add(dColumn); // D列の情報
                    }

                    if (!structuredData.ContainsKey(roomKey))
                    {
                        structuredData[roomKey] = new List<DeviceData>();
                    }

                    // 新しいデバイスを追加
                    structuredData[roomKey].Add(new DeviceData
                    {
                        DeviceName = deviceName,
                        Model = model,
                        ID = id,
                        Buttons = buttons,
                        DColumns = dColumns // D列の情報を追加
                    });

                    // 現在のデバイス情報を保持
                    lastRoomKey = roomKey;
                    lastDeviceName = deviceName;
                    lastDeviceID = id;
                }

                // --- 2週目: 新しいデータを取得 ---
                reader.BaseStream.Seek(0, SeekOrigin.Begin); // ファイルの先頭に戻る
                reader.DiscardBufferedData();

                // 最初の6行をスキップ
                for (int i = 0; i < 6; i++)
                {
                    reader.ReadLine();
                }

                string currentSection = null; // 現在のセクション ("Zone Name", "HVAC Zone Name", etc.)
                bool terminateAfterThermostatMode = false; // 処理を終了するフラグ

                while ((line = reader.ReadLine()) != null)
                {
                    string[] columns = line.Split(',');

                    // A列の内容を確認
                    string aColumn = columns.Length > 0 ? columns[0] : null;
                    string bColumn = columns.Length > 1 ? columns[1] : null;

                    if (terminateAfterThermostatMode && string.IsNullOrWhiteSpace(aColumn))
                    {
                        break; // 処理終了
                    }

                    // セクション開始を判定
                    if (aColumn.Equals("Zone Name"))
                    {
                        currentSection = "Zone Name";
                        continue;
                    }
                    else if (aColumn.Equals("HVAC Zone Name"))
                    {
                        currentSection = "HVAC Zone Name";
                        continue;
                    }
                    else if (aColumn.Equals("Variable Name"))
                    {
                        currentSection = "Variable Name";
                        continue;
                    }
                    else if (aColumn.Equals("Thermostat Mode"))
                    {
                        currentSection = "Thermostat Mode";
                        terminateAfterThermostatMode = true; // 次に空白が来たら終了
                        continue;
                    }

                    // 現在のセクションに基づいてデータを記録
                    if (!string.IsNullOrWhiteSpace(currentSection))
                    {
                        if (!additionalData.ContainsKey(currentSection))
                        {
                            additionalData[currentSection] = new Dictionary<string, string>();
                        }

                        if (!string.IsNullOrWhiteSpace(aColumn) && !string.IsNullOrWhiteSpace(bColumn))
                        {
                            additionalData[currentSection][aColumn] = bColumn;
                        }
                    }
                }
            }

            // structuredData に additionalData を追加して戻すか、別途返却可能
            return (structuredData, additionalData);
        }


    }
}