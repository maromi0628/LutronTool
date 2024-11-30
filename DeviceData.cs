using System;
using System.Collections.Generic;

public class DeviceData
{
    public string DeviceName { get; set; } // デバイス名
    public string Model { get; set; } // モデル情報
    public string ID { get; set; } // デバイスID
    public List<string> Buttons { get; set; } = new List<string>(); // ボタン名リスト
    public List<string> DColumns { get; set; } = new List<string>(); // D列の情報

    private Dictionary<int, bool> buttonStates = new Dictionary<int, bool>(); // ボタン状態の管理
    private int activeBrightness = -1; // アクティブ状態のバックライト照度
    private int inactiveBrightness = -1; // インアクティブ状態のバックライト照度

    // データ変更イベント
    public event Action<string> DataChanged; // IDを渡して通知
    public event Action<string, int> ButtonStateChanged; // IDとボタン番号を渡して通知

    /// <summary>
    /// アクティブ状態のバックライト照度を取得または設定
    /// </summary>
    public int ActiveBrightness
    {
        get => activeBrightness;
        set
        {
            if (activeBrightness != value)
            {
                activeBrightness = value;
                OnDataChanged();
            }
        }
    }

    /// <summary>
    /// インアクティブ状態のバックライト照度を取得または設定
    /// </summary>
    public int InactiveBrightness
    {
        get => inactiveBrightness;
        set
        {
            if (inactiveBrightness != value)
            {
                inactiveBrightness = value;
                OnDataChanged();
            }
        }
    }

    /// <summary>
    /// ボタンの状態を設定
    /// </summary>
    /// <param name="buttonIndex">ボタン番号 (1から始まる)</param>
    /// <param name="isActive">アクティブ状態なら true、インアクティブなら false</param>
    public void SetButtonState(int buttonIndex, bool isActive)
    {
        if (!buttonStates.ContainsKey(buttonIndex) || buttonStates[buttonIndex] != isActive)
        {
            buttonStates[buttonIndex] = isActive;
            OnButtonStateChanged(buttonIndex); // ボタン変更イベントを発火
        }
    }

    /// <summary>
    /// ボタンの状態を取得
    /// </summary>
    /// <param name="buttonIndex">ボタン番号 (1から始まる)</param>
    /// <returns>アクティブ状態なら true、インアクティブなら false、未設定なら null</returns>
    public bool? GetButtonState(int buttonIndex)
    {
        return buttonStates.TryGetValue(buttonIndex, out var state) ? (bool?)state : null;
    }

    /// <summary>
    /// 全ボタンの状態を取得
    /// </summary>
    /// <returns>ボタン番号とその状態の辞書</returns>
    public Dictionary<int, bool> GetAllButtonStates()
    {
        return new Dictionary<int, bool>(buttonStates);
    }

    /// <summary>
    /// データ変更時のイベントをトリガー
    /// </summary>
    private void OnDataChanged()
    {
        DataChanged?.Invoke(ID); // ID を渡してイベントを通知
    }

    /// <summary>
    /// ボタン変更イベントをトリガー
    /// </summary>
    /// <param name="buttonIndex">変更されたボタン番号</param>
    private void OnButtonStateChanged(int buttonIndex)
    {
        ButtonStateChanged?.Invoke(ID, buttonIndex); // ID とボタン番号を通知
    }

    /// <summary>
    /// デバッグ用: すべてのボタン状態を文字列で取得
    /// </summary>
    /// <returns>全ボタンの状態を表す文字列</returns>
    public string GetButtonStatesAsString()
    {
        var stateStrings = new List<string>();
        foreach (var kvp in buttonStates)
        {
            stateStrings.Add($"Button {kvp.Key}: {(kvp.Value ? "Active" : "Inactive")}");
        }
        return string.Join(", ", stateStrings);
    }
}
