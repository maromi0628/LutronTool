using System.Collections.Generic;

public class DeviceData
{
    public string DeviceName { get; set; }
    public string Model { get; set; }
    public string ID { get; set; }
    public List<string> Buttons { get; set; } = new List<string>();
    public List<string> DColumns { get; set; } = new List<string>(); // D列の情報
    private Dictionary<int, bool> ButtonStates { get; set; } = new Dictionary<int, bool>(); // ボタン番号と状態

    public int ActiveBrightness { get; set; } = -1;
    public int InactiveBrightness { get; set; } = -1;

    /// <summary>
    /// ボタンの状態を設定
    /// </summary>
    /// <param name="buttonIndex">ボタン番号 (1から始まる)</param>
    /// <param name="isActive">アクティブ状態なら true、インアクティブなら false</param>
    public void SetButtonState(int buttonIndex, bool isActive)
    {
        if (ButtonStates == null)
        {
            ButtonStates = new Dictionary<int, bool>();
        }
        ButtonStates[buttonIndex] = isActive;
    }

    /// <summary>
    /// ボタンの状態を取得
    /// </summary>
    /// <param name="buttonIndex">ボタン番号 (1から始まる)</param>
    /// <returns>アクティブ状態なら true、インアクティブなら false、未設定なら null</returns>
    public bool? GetButtonState(int buttonIndex)
    {
        return ButtonStates.ContainsKey(buttonIndex) ? ButtonStates[buttonIndex] : (bool?)null;
    }

    /// <summary>
    /// 全ボタンの状態を取得
    /// </summary>
    /// <returns>全ボタンの状態を格納した辞書</returns>
    public Dictionary<int, bool> GetAllButtonStates()
    {
        return new Dictionary<int, bool>(ButtonStates);
    }

    /// <summary>
    /// 指定されたボタンがアクティブ状態か確認する
    /// </summary>
    /// <param name="buttonIndex">ボタン番号 (1から始まる)</param>
    /// <returns>アクティブ状態なら true、インアクティブなら false、未設定なら null</returns>
    public bool? IsButtonActive(int buttonIndex)
    {
        return GetButtonState(buttonIndex);
    }

    /// <summary>
    /// 全てのボタンがアクティブ状態か確認する
    /// </summary>
    /// <returns>すべてのボタンがアクティブ状態なら true、それ以外なら false</returns>
    public bool AreAllButtonsActive()
    {
        foreach (var state in ButtonStates.Values)
        {
            if (!state) // 一つでもインアクティブ状態があれば false を返す
            {
                return false;
            }
        }
        return ButtonStates.Count > 0; // ボタンが1つ以上あり、すべてがアクティブの場合 true
    }

    /// <summary>
    /// アクティブなボタンのリストを取得する
    /// </summary>
    /// <returns>アクティブ状態のボタン番号リスト</returns>
    public List<int> GetActiveButtons()
    {
        var activeButtons = new List<int>();
        foreach (var entry in ButtonStates)
        {
            if (entry.Value) // アクティブなボタン
            {
                activeButtons.Add(entry.Key);
            }
        }
        return activeButtons;
    }

    /// <summary>
    /// アクティブ状態のバックライト照度を取得
    /// </summary>
    /// <returns>アクティブ状態の照度 (0～100)</returns>
    public int GetActiveBrightness()
    {
        return ActiveBrightness; // 既存プロパティを返す
    }

    /// <summary>
    /// インアクティブ状態のバックライト照度を取得
    /// </summary>
    /// <returns>インアクティブ状態の照度 (0～100)</returns>
    public int GetInactiveBrightness()
    {
        return InactiveBrightness; // 既存プロパティを返す
    }
}
