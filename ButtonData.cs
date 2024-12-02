public class ButtonData
{
    public string Name { get; set; } // ボタン名
    public string DColumn { get; set; } // D列の情報
    public bool? State { get; set; } // 状態（アクティブ/インアクティブ）

    public override string ToString()
    {
        return $"{Name} (State: {(State.HasValue ? (State.Value ? "Active" : "Inactive") : "Unknown")})";
    }
}
