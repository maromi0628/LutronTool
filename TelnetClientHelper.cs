using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public class TelnetClientHelper
{
    private TcpClient tcpClient;
    private NetworkStream networkStream;
    private StreamReader reader;
    private StreamWriter writer;

    // レスポンスバッファ
    private StringBuilder responseBuffer = new StringBuilder();

    // 既存の接続からストリームを初期化する
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

    // Telnetからデータを読み取り、バッファに格納する
    private async Task<string> ReadToBufferAsync()
    {
        char[] buffer = new char[1024]; // 一度に読み取るバッファサイズ
        while (true)
        {
            // データを読み取りバッファに追加
            int bytesRead = await reader.ReadAsync(buffer, 0, buffer.Length);
            if (bytesRead == 0)
            {
                throw new IOException("接続が切断されました。");
            }

            responseBuffer.Append(buffer, 0, bytesRead);

            // 電文を分割して処理
            var messages = ExtractMessages();
            if (messages.Count > 0)
            {
                string firstMessage = messages[0];

                // バッファから処理済み部分を削除
                responseBuffer.Remove(0, responseBuffer.ToString().IndexOf(firstMessage) + firstMessage.Length);

                return firstMessage;
            }
        }
    }

    // レスポンスバッファからメッセージを抽出する
    private List<string> ExtractMessages()
    {
        List<string> messages = new List<string>();
        string fullResponse = responseBuffer.ToString();

        // パターン: "QNET>" または "~" の場合は先頭の識別子を除外して格納
        string pattern = @"(?:QNET>\s*|~)(.*?)(\r\n|$)|^(login: |password: )$";
        MatchCollection matches = Regex.Matches(fullResponse, pattern);

        foreach (Match match in matches)
        {
            if (match.Groups[1].Success) // QNET> または ~ の場合
            {
                string message = match.Groups[1].Value.Trim(); // メッセージ部分のみを取得
                if (!string.IsNullOrWhiteSpace(message))
                {
                    messages.Add(message);
                }
            }
            else if (match.Groups[3].Success) // login: または password: の場合
            {
                messages.Add(match.Groups[3].Value);
            }
        }

        return messages;
    }

    // Telnetにコマンドを送信する
    private async Task WriteCommandAsync(string command)
    {
        await writer.WriteLineAsync(command);
    }

    // バックライト照度を取得する
    public async Task<int> GetBacklightBrightnessAsync(string deviceId, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            throw new ArgumentException("デバイスIDが無効です。");
        }

        string command = isActive
            ? $"?DEVICE,{deviceId},89,36" // アクティブ状態
            : $"?DEVICE,{deviceId},89,37"; // インアクティブ状態

        string response = await SendCommandAsync(command);
        return ParseBacklightResponse(response);
    }

    // バックライト照度レスポンスを解析
    private int ParseBacklightResponse(string response)
    {
        string[] parts = response.Split(',');
        if (parts.Length >= 5 && int.TryParse(parts[^1].TrimEnd(), out int brightness))
        {
            return brightness;
        }

        Console.WriteLine($"レスポンスの解析に失敗しました: {response}");
        return -1; // エラー値
    }

    // すべてのキーパッドのバックライト照度を取得
    public async Task<Dictionary<string, (int Active, int Inactive)>> GetBacklightBrightnessForKeypads(Dictionary<string, List<DeviceData>> structuredData)
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