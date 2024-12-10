using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RoomKeypadManager; // MainForm の名前空間



public class TelnetClientHelper
{
    private TcpClient tcpClient;
    private NetworkStream networkStream;
    private StreamReader reader;
    private StreamWriter writer;

    private StringBuilder responseBuffer = new StringBuilder();

    // structuredDataを管理
    private Dictionary<string, List<DeviceData>> structuredData;

    // additionalDataを管理
    private Dictionary<string, Dictionary<string, string>> additionalData;

    private bool isListening = false; // リスニング状態の管理

    //public TelnetClientHelper(Dictionary<string, List<DeviceData>> structuredData)
    //{
    //    this.structuredData = structuredData;
    //}

    private MainForm mainFormInstance;

    public TelnetClientHelper(MainForm mainForm, Dictionary<string, List<DeviceData>> structuredData, Dictionary<string, Dictionary<string, string>> additionalData)
    {
        this.mainFormInstance = mainForm;
        this.structuredData = structuredData;
        this.additionalData = additionalData;
    }



    public void InitializeStream(NetworkStream stream)
    {
        networkStream = stream;
        reader = new StreamReader(networkStream, Encoding.ASCII);
        writer = new StreamWriter(networkStream, Encoding.ASCII) { AutoFlush = true };
    }

    public async Task<bool> LoginAsync(string username, string password,string nwk,bool nwk_check)
    {
        if (reader == null || writer == null)
        {
            throw new InvalidOperationException("Telnet接続が確立されていません。");
        }

        if(nwk_check == true) {
            try
            {
                await ReadToBufferAsync();
                await WriteCommandAsync(nwk);

                await ReadToBufferAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ログインエラー: {ex.Message}");
                return false;
            }
        }

        else
        {
            try
            {
                await ReadToBufferAsync();
                await WriteCommandAsync(username);

                await ReadToBufferAsync();
                await WriteCommandAsync(password);

                await ReadToBufferAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ログインエラー: {ex.Message}");
                return false;
            }
        }
        
    }

    public async Task<string> SendCommandAsync(string command)
    {
        if (writer == null || reader == null)
        {
            throw new InvalidOperationException("Telnet接続が確立されていません。");
        }

        try
        {
            await WriteCommandAsync(command);
            return string.Empty; // レスポンスを取得しない
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Telnetコマンド送信エラー: {ex.Message}");
            throw;
        }
    }

    public void ProcessResponseBuffer(Action<string> logAction)
    {
        lock (responseBuffer)
        {
            while (true)
            {
                string bufferContent = responseBuffer.ToString();
                int messageEndIndex = bufferContent.IndexOf("\r\n");
                if (messageEndIndex == -1)
                {
                    break; // メッセージの終端が見つからない場合は、処理を中断
                }

                string message = bufferContent.Substring(0, messageEndIndex).Trim();
                responseBuffer.Remove(0, messageEndIndex + 2);

                message = message.Replace("QNET>", "").Trim();

                if (!string.IsNullOrWhiteSpace(message))
                {
                    //logAction?.Invoke($"[レスポンス電文] {message}");
                    HandleResponseMessage(message, logAction);
                }
            }
        }
    }


    private void HandleResponseMessage(string message, Action<string> logAction)
    {
        Regex regexBacklight = new Regex(@"~DEVICE,(\d+),89,(36|37),(\d+)");
        Match matchBacklight = regexBacklight.Match(message);
        if (matchBacklight.Success)
        {
            string id = matchBacklight.Groups[1].Value;
            string type = matchBacklight.Groups[2].Value;
            int brightness = int.Parse(matchBacklight.Groups[3].Value);

            foreach (var roomKey in structuredData.Keys)
            {
                var device = structuredData[roomKey].FirstOrDefault(d => d.ID == id);
                if (device != null)
                {
                    if (type == "36")
                    {
                        device.ActiveBrightness = brightness;
                    }
                    else if (type == "37")
                    {
                        device.InactiveBrightness = brightness;
                    }

                    //logAction?.Invoke($"[DEBUG] ID: {id} の現在の照度を更新しました。");
                    return;
                }
            }
        }

        Regex regexButtonState = new Regex(@"~DEVICE,(\d+),8(\d),9,(0|1)");
        Match matchButtonState = regexButtonState.Match(message);
        if (matchButtonState.Success)
        {
            string id = matchButtonState.Groups[1].Value;
            int buttonIndex = int.Parse(matchButtonState.Groups[2].Value);
            bool isActive = matchButtonState.Groups[3].Value == "1";

            foreach (var roomKey in structuredData.Keys)
            {
                var device = structuredData[roomKey].FirstOrDefault(d => d.ID == id);
                if (device != null)
                {
                    device.SetButtonState(buttonIndex, isActive);
                    //logAction?.Invoke($"[INFO] ID: {id}, ボタン: {buttonIndex}, 状態: {(isActive ? "アクティブ" : "インアクティブ")}");
                    return;
                }
            }
        }

        // Null チェック
        if (additionalData == null)
        {
            //logAction?.Invoke("[ERROR] additionalData が初期化されていません。");
            return;
        }

        if (mainFormInstance == null)
        {
            //logAction?.Invoke("[ERROR] mainFormInstance が初期化されていません。");
            return;
        }

        // 新規照度電文処理（~OUTPUT,ID,1,Brightness）
        Regex regexOutputBrightness = new Regex(@"~OUTPUT,(\d+),1,(\d{1,3}\.\d{1,2})");
        Match matchOutputBrightness = regexOutputBrightness.Match(message);
        if (matchOutputBrightness.Success)
        {
            string id = matchOutputBrightness.Groups[1].Value;
            float brightness = float.Parse(matchOutputBrightness.Groups[2].Value);

            //logAction?.Invoke($"[DEBUG] 照度更新電文を受信: ID={id}, Brightness={brightness}%");

            // additionalData を参照して更新を試みる
            foreach (var section in additionalData.Keys)
            {
                //logAction?.Invoke($"[DEBUG] セクション: {section}");

                // セクション内のデータを走査
                foreach (var kvp in additionalData[section]) // kvp は KeyValuePair<string, string>
                {
                    // KeyValuePairのKeyとIDを比較
                    if (kvp.Value == id)
                    {
                        // MainForm のインスタンスを使って2個目のタブを更新
                        try
                        {
                            mainFormInstance?.UpdateLightingStatusTabBrightness(id, brightness);
                            //logAction?.Invoke($"[INFO] ID: {id} の照度を {brightness}% に更新しました。");
                        }
                        catch (Exception ex)
                        {
                            logAction?.Invoke($"[ERROR] タブ更新中にエラーが発生: {ex.Message}");
                        }

                        return; // 更新が完了したので終了
                    }
                }
            }
        }

        // 新規電文処理（~SYSVAR,ID,1,x）
        Regex regexSysVar = new Regex(@"~SYSVAR,(\d+),1,(\d+)");
        Match matchSysVar = regexSysVar.Match(message);
        if (matchSysVar.Success)
        {
            string id = matchSysVar.Groups[1].Value;
            float brightness = float.Parse(matchSysVar.Groups[2].Value);

            //logAction?.Invoke($"[DEBUG] SYSVAR電文を受信: ID={id}, Brightness={brightness}%");

            // additionalData を参照して更新を試みる
            foreach (var section in additionalData.Keys)
            {
                //logAction?.Invoke($"[DEBUG] セクション: {section}");

                // セクション内のデータを走査
                foreach (var kvp in additionalData[section]) // kvp は KeyValuePair<string, string>
                {
                    if (kvp.Value == id)
                    {
                        // MainForm のインスタンスを使って2個目のタブを更新
                        try
                        {
                            mainFormInstance?.UpdateLightingStatusTabBrightness(id, brightness);
                            //logAction?.Invoke($"[INFO] SYSVAR: ID: {id} の照度を {brightness}% に更新しました。");
                        }
                        catch (Exception ex)
                        {
                            logAction?.Invoke($"[ERROR] SYSVAR処理中にエラーが発生: {ex.Message}");
                        }

                        return; // 更新が完了したので終了
                    }
                }
            }
        }
    }



    public void StartListening()
    {
        if (isListening)
        {
            Console.WriteLine("[DEBUG] すでにリスニング中です。");
            return;
        }

        isListening = true;

        Task.Run(async () =>
        {
            try
            {
                char[] buffer = new char[1024];

                while (isListening)
                {
                    int bytesRead = await reader.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        lock (responseBuffer)
                        {
                            responseBuffer.Append(buffer, 0, bytesRead);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] リスニング中にエラーが発生しました: {ex.Message}");
                isListening = false;
            }
        });
    }

    public void StopListening()
    {
        isListening = false;
        Console.WriteLine("[DEBUG] リスニングを停止しました。");
    }

    public void Close()
    {
        try
        {
            reader?.Close();
            writer?.Close();
            networkStream?.Close();
            tcpClient?.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Telnet切断エラー: {ex.Message}");
        }
    }

    private async Task WriteCommandAsync(string command)
    {
        await writer.WriteLineAsync(command);
        //await Task.Delay(50); // 50ms の遅延を追加（必要に応じて調整）
    }

    private async Task<string> ReadToBufferAsync()
    {
        char[] buffer = new char[1024];

        while (true)
        {
            int bytesRead = await reader.ReadAsync(buffer, 0, buffer.Length);
            if (bytesRead == 0)
            {
                throw new IOException("接続が切断されました。");
            }

            lock (responseBuffer)
            {
                responseBuffer.Append(buffer, 0, bytesRead);
            }

            return responseBuffer.ToString();
        }
    }

    public async Task SendBacklightBrightnessCommandsForKeypads(
    Dictionary<string, List<DeviceData>> structuredData, Action<string> logAction)
    {
        if (structuredData == null || structuredData.Count == 0)
        {
            logAction?.Invoke("[WARN] structuredData が空です。コマンドをスキップします。");
            return;
        }

        foreach (var roomKey in structuredData.Keys)
        {
            foreach (var device in structuredData[roomKey])
            {
                if (!string.IsNullOrWhiteSpace(device.ID) &&
                    (device.Model.StartsWith("MWP-U") || device.Model.StartsWith("MWP-B")))
                {
                    string activeCommand = $"?DEVICE,{device.ID},89,36";
                    string inactiveCommand = $"?DEVICE,{device.ID},89,37";

                    logAction?.Invoke($"[送信コマンド] {activeCommand}");
                    await SendCommandAsync(activeCommand);

                    logAction?.Invoke($"[送信コマンド] {inactiveCommand}");
                    await SendCommandAsync(inactiveCommand);

                    // ボタン状態確認コマンドの送信
                    int buttonCount = device.Buttons.Count; // ボタンの数を取得
                    await SendButtonStateCheckCommands(device.ID, buttonCount, logAction);

                    // 小さな待機を入れる（必要に応じて調整）
                    //await Task.Delay(50); // 50ms の遅延を追加
                }
            }
        }
    }


    public async Task SendButtonStateCheckCommands(string deviceID, int buttonCount, Action<string> logAction)
    {
        for (int i = 1; i <= buttonCount; i++)
        {
            string command = $"?DEVICE,{deviceID},8{i},9";
            logAction?.Invoke($"[送信コマンド] {command}");
            await SendCommandAsync(command);
        }
    }

    public async Task SendGetLightStatus(List<string> IdList)
    {
        foreach (string id in IdList)
        {
            string command = $"?OUTPUT,{id},1";
            await SendCommandAsync(command);
        }
    }



}
