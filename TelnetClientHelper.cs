using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RoomKeypadManager; // DeviceData クラスが移動した場合の名前空間

public class TelnetClientHelper
{
    private TcpClient tcpClient;
    private NetworkStream networkStream;
    private StreamReader reader;
    private StreamWriter writer;

    // レスポンスバッファ
    private StringBuilder responseBuffer = new StringBuilder();

    public void InitializeStream(NetworkStream stream)
    {
        networkStream = stream;
        reader = new StreamReader(networkStream, Encoding.ASCII);
        writer = new StreamWriter(networkStream, Encoding.ASCII) { AutoFlush = true };
    }

    // ログイン処理
    public async Task<bool> LoginAsync(string username, string password)
    {
        if (reader == null || writer == null)
        {
            throw new InvalidOperationException("Telnet接続が確立されていません。");
        }

        try
        {
            // "login:" プロンプト処理
            await ReadToBufferAsync();
            await WriteCommandAsync(username);

            // "password:" プロンプト処理
            await ReadToBufferAsync();
            await WriteCommandAsync(password);

            // ログイン完了後のプロンプトを処理
            await ReadToBufferAsync();

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ログインエラー: {ex.Message}");
            return false;
        }
    }

    // コマンドを送信してレスポンスを取得する
    public async Task<string> SendCommandAsync(string command)
    {
        if (writer == null || reader == null)
        {
            throw new InvalidOperationException("Telnet接続が確立されていません。");
        }

        try
        {
            // コマンドを送信
            await WriteCommandAsync(command);

            // レスポンスを取得
            return await ReadToBufferAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Telnetコマンド送信エラー: {ex.Message}");
            throw;
        }
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
                Console.WriteLine($"[DEBUG] バッファ追加内容: {new string(buffer, 0, bytesRead)}");
                Console.WriteLine($"[DEBUG] 現在のレスポンスバッファ: {responseBuffer}");
            }

            return responseBuffer.ToString();
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
                }
            }
        }
    }

    private async Task WriteCommandAsync(string command)
    {
        await writer.WriteLineAsync(command);
    }

    private bool isListening = false; // リスニング状態の管理

    public void StartListening()
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            throw new ArgumentException("デバイスIDが無効です。");
        }

        string command = isActive
            ? $"?DEVICE,{deviceId},89,36" // アクティブ状態
            : $"?DEVICE,{deviceId},89,37"; // インアクティブ状態

        Task.Run(async () =>
        {
            try
            {
                char[] buffer = new char[1024]; // 一度に読み取るバッファサイズ

                while (isListening)
                {
                    int bytesRead = await reader.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        lock (responseBuffer) // スレッドセーフに操作
                        {
                            responseBuffer.Append(buffer, 0, bytesRead);
                            Console.WriteLine($"[DEBUG] 受信データ: {new string(buffer, 0, bytesRead)}");
                            Console.WriteLine($"[DEBUG] 現在のレスポンスバッファ: {responseBuffer}");
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

        Console.WriteLine($"レスポンスの解析に失敗しました: {response}");
        return -1; // エラー値
    }


    public async Task SendBacklightBrightnessCommandsForKeypads(
        Dictionary<string, List<DeviceData>> structuredData, Action<string> logAction)
    {
        var result = new Dictionary<string, (int Active, int Inactive)>();

        foreach (var roomKey in structuredData.Keys)
        {
            foreach (var device in structuredData[roomKey])
            {
                if (!string.IsNullOrWhiteSpace(device.ID) &&
                    (device.Model.StartsWith("MWP-U") || device.Model.StartsWith("MWP-B")))
                {
                    int activeBrightness = await GetBacklightBrightnessAsync(device.ID, true);
                    int inactiveBrightness = await GetBacklightBrightnessAsync(device.ID, false);
                    result[device.DeviceName] = (activeBrightness, inactiveBrightness);
                }
            }
        }

        return result;
    }

    // 接続を閉じる
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


}
