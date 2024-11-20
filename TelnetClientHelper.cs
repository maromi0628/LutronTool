using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

public class TelnetClientHelper
{
    private TcpClient telnetClient; // Telnet用のクライアント
    private NetworkStream networkStream; // ネットワークストリーム
    private StreamReader reader; // Telnetからの読み取り
    private StreamWriter writer; // Telnetへの書き込み

    public async Task<bool> ConnectAsync(string ipAddress, int port)
    {
        try
        {
            telnetClient = new TcpClient();
            await telnetClient.ConnectAsync(ipAddress, port); // 接続を確立
            networkStream = telnetClient.GetStream(); // ストリームを取得
            reader = new StreamReader(networkStream, Encoding.ASCII); // 読み取りストリーム
            writer = new StreamWriter(networkStream, Encoding.ASCII) { AutoFlush = true }; // 書き込みストリーム
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"接続エラー: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> LoginAsync(string username, string password, string loginPrompt = "login: ", string passwordPrompt = "password: ")
    {
        try
        {
            // "login: "プロンプトを待機してユーザー名を送信
            await ReadUntilAsync(loginPrompt);
            await WriteLineAsync(username);

            // "password: "プロンプトを待機してパスワードを送信
            await ReadUntilAsync(passwordPrompt);
            await WriteLineAsync(password);

            // ログイン成功の確認はここでは実装しない（必要なら応答を確認可能）
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ログインエラー: {ex.Message}");
            return false;
        }
    }

    public async Task<string> SendCommandAsync(string command, string waitForPrompt = "QNET> ")
    {
        try
        {
            // プロンプトを待機
            await ReadUntilAsync(waitForPrompt);

            // コマンドを送信
            await WriteLineAsync(command);

            // コマンド実行後の結果を取得
            return await ReadUntilAsync(waitForPrompt);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"コマンド送信エラー: {ex.Message}");
            return string.Empty;
        }
    }

    private async Task<string> ReadUntilAsync(string expected)
    {
        StringBuilder response = new StringBuilder();
        char[] buffer = new char[1];

        while (true)
        {
            int bytesRead = await reader.ReadAsync(buffer, 0, 1); // 1文字を読み込む
            if (bytesRead == 0)
            {
                throw new IOException("接続が切断されました。");
            }

            // ヌル文字をスキップ
            if (buffer[0] != '\0')
            {
                response.Append(buffer[0]); // 読み取った文字をバッファに追加
            }

            // 指定した文字列が出現したら終了
            if (response.ToString().EndsWith(expected))
            {
                return response.ToString();
            }
        }
    }

    private async Task WriteLineAsync(string command)
    {
        await writer.WriteLineAsync(command); // コマンドを書き込み
    }

    public void Close()
    {
        reader?.Dispose();
        writer?.Dispose();
        networkStream?.Close();
        telnetClient?.Close();
    }
}
