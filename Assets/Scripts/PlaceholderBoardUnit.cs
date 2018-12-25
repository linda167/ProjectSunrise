public class PlaceholderBoardUnit : IBoardUnit {
    public bool IsEnemyPlayer {
		get { return false; }
	}

	public bool IsFrontRow {
		get;
        set;
	}

	public bool IsTempSpaceholder {
		get { return true; }
	}

    public bool ShouldReplaceWithRealUnit {
		get;
        set;
	}
}