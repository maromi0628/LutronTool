﻿using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RoomKeypadManager
{
    public class CsvProcessor
    {
        public static (Dictionary<string, List<DeviceData>>, Dictionary<string, Dictionary<string, string>>) ProcessCsvFile(string filePath)
        {
            var structuredData = new Dictionary<string, List<DeviceData>>();
            var additionalData = new Dictionary<string, Dictionary<string, string>>();
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
                int lastButtonNumber = 1; // 初期値は0（ボタンが見つからない場合のため）

                // --- 1週目: 既存のデバイスデータ処理 ---
                while ((line = reader.ReadLine()) != null)
                {
                    // 制御文字を除去
                    line = line.Replace("\u200E", "");

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

                    // 不要な末尾の文字列を削除
                    if (!string.IsNullOrWhiteSpace(fullPath) && (fullPath.EndsWith("CSD 001") || fullPath.EndsWith("Device 1")))
                    {
                        fullPath = fullPath.Substring(0, fullPath.LastIndexOf("\\"));
                    }

                    // C列 (ID) の処理
                    if (!string.IsNullOrWhiteSpace(id) && id.Contains("/"))
                    {
                        // 最後のバックスラッシュ以降の文字列を取得
                        id = id.Substring(id.LastIndexOf("/") + 1);
                    }

                    int maxButtonNumber = 0;
                    //int lastButtonNumber = 0; // 初期値は0（ボタンが見つからない場合のため）
                    int ButtonNumber = 0; // 初期値は0（ボタンが見つからない場合のため）

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
                                    //string txt;
                                    //txt = fColumn + " " + dColumn.Replace("Button ", "");
                                    //existingDevice.Buttons.Add(txt); // ボタン名を追加
                                    //existingDevice.Buttons.Add(fColumn); // ボタン名を追加
                                    //existingDevice.DColumns.Add(dColumn); // D列の情報を追加
                                    ButtonNumber = dColumn[dColumn.Length - 1] - 48; // 最大ボタン番号を更新
                                    if (ButtonNumber <= 4)
                                    {
                                        maxButtonNumber = 4;
                                    }
                                    else if (ButtonNumber <= 8)
                                    {
                                        maxButtonNumber = 8;
                                    }
                                    if (maxButtonNumber+1 > ButtonNumber)
                                    {
                                        while (ButtonNumber - lastButtonNumber !=1)
                                        {
                                            int num;
                                            num = lastButtonNumber + 1;
                                            existingDevice.Buttons.Add($"Dummy Button{num}"); // ボタン名を追加
                                            //existingDevice.DColumns.Add(dColumn); // D列の情報を追加
                                            lastButtonNumber += 1;
                                        }
                                    }
                                    existingDevice.Buttons.Add(fColumn); // ボタン名を追加
                                    existingDevice.DColumns.Add(dColumn); // D列の情報を追加
                                    lastButtonNumber = ButtonNumber;
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

                    // --- ボタン情報の最大値を特定し、ダミー登録を含めた処理 ---
                    List<string> buttons = new List<string>();
                    List<string> dColumns = new List<string>();
                    //int maxButtonNumber = 0; // 初期値は0（ボタンが見つからない場合のため）

                    // 正規表現でD列に「Button」とボタン番号があるかチェック
                    if (dColumn.Contains("Button") && !string.IsNullOrWhiteSpace(fColumn))
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(dColumn, @"Button (\d+)");
                        if (match.Success)
                        {
                            int buttonNumber = int.Parse(match.Groups[1].Value);
                            //maxButtonNumber = dColumn[dColumn.Length-1]-48; // 最大ボタン番号を更新
                            buttons.Add(fColumn); // F列のボタン名を追加
                            dColumns.Add(dColumn); // D列の情報を追加
                        }
                    }

                    //// 最大ボタン番号を特定する処理
                    //if (maxButtonNumber > 1)
                    //{
                    //    // Button 1 から最大ボタン数までのループ
                    //    for (int i = 1; i <= maxButtonNumber; i++)
                    //    {
                    //        string buttonName = $"Button {i}";
                    //        if (!dColumns.Contains(buttonName)) // D列にボタンがない場合
                    //        {
                    //            dColumns.Add(buttonName); // D列にダミーを追加
                    //            buttons.Add($"ダミー {buttonName}"); // ボタン名リストにダミーを追加
                    //        }
                    //    }
                    //}
                    //else
                    //{
                    //    // ボタンが見つからない場合のエラーハンドリング
                    //    Console.WriteLine($"[WARN] ボタン情報が見つかりませんでした: D列={dColumn}");
                    //}


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
                    lastButtonNumber = 1;
                }

                // --- 2週目: 追加データの処理 ---
                reader.BaseStream.Seek(0, SeekOrigin.Begin); // ファイルの先頭に戻る
                reader.DiscardBufferedData();
                bool space_flag = false;
                bool zone_name_end = false;

                for (int i = 0; i < 6; i++)
                {
                    reader.ReadLine();
                }

                string currentSection = null;

                while ((line = reader.ReadLine()) != null)
                {
                    string[] columns = line.Split(',');

                    string aColumn = columns.Length > 0 ? columns[0] : null;
                    string bColumn = columns.Length > 1 ? columns[1] : null;

                    if(aColumn.Contains("Lutron Electronics"))
                    {
                        break;
                    }

                    // セクションの切り替え
                    if (aColumn == "Zone Name")
                    {
                        currentSection = "Zone Name";
                        zone_name_end = true;
                        continue;
                    }

                    if (!zone_name_end)
                    {
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(aColumn))
                    {
                        space_flag = true;
                        continue;
                    }

                    if (space_flag)
                    {
                        space_flag = false;
                        currentSection = aColumn;
                        continue;
                    }


                    if (!string.IsNullOrWhiteSpace(currentSection) && !string.IsNullOrWhiteSpace(aColumn) && !string.IsNullOrWhiteSpace(bColumn))
                    {
                        if (!additionalData.ContainsKey(currentSection))
                        {
                            additionalData[currentSection] = new Dictionary<string, string>();
                        }

                        string id = bColumn;
                        if (!string.IsNullOrWhiteSpace(id) && id.Contains("/"))
                        {
                            // 最後のバックスラッシュ以降の文字列を取得
                            id = id.Substring(id.LastIndexOf("/") + 1);
                        }
                        //additionalData[currentSection][aColumn] = bColumn;
                        additionalData[currentSection][aColumn] = id;
                    }
                }
            }

            return (structuredData, additionalData);
        }
    }
}
