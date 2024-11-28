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

    private bool isListening = false; // リスニング状態の管理

    public TelnetClientHelper(Dictionary<string, List<DeviceData>> structuredData)
    {
        this.structuredData = structuredData;
    }

    private MainForm mainFormInstance;

    public TelnetClientHelper(MainForm mainForm, Dictionary<string, List<DeviceData>> structuredData)
    {
        this.mainFormInstance = mainForm;
        this.structuredData = structuredData;
    }



    public void InitializeStream(NetworkStream stream)
    {
        networkStream = stream;
        reader = new StreamReader(networkStream, Encoding.ASCII);
        writer = new StreamWriter(networkStream, Encoding.ASCII) { AutoFlush = true };
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        if (reader == null || writer == null)
        {
            throw new InvalidOperationException("Telnet接続が確立されていません。");
        }

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
                    break;
                }

                string message = bufferContent.Substring(0, messageEndIndex).Trim();
                responseBuffer.Remove(0, messageEndIndex + 2);

                message = message.Replace("QNET>", "").Trim();

                if (!string.IsNullOrWhiteSpace(message))
                {
                    logAction?.Invoke($"[レスポンス電文] {message}");
                    HandleResponseMessage(message, logAction); // 電文処理
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

                    logAction?.Invoke($"[DEBUG] ID: {id} の現在の照度を更新しました。");
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
                    logAction?.Invoke($"[INFO] ID: {id}, ボタン: {buttonIndex}, 状態: {(isActive ? "アクティブ" : "インアクティブ")}");
                    return;
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


}
