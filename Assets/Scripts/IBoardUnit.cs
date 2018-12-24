interface IBoardUnit {
    bool IsEnemyPlayer { get; }
    bool IsFrontRow { get; }
    bool IsTempSpaceholder { get; }
}