public class PlaceholderBoardUnit : IBoardUnit {
    private bool isFrontRow;
    public bool IsEnemyPlayer {
		get { return false; }
	}

	public bool IsFrontRow {
		get { return this.isFrontRow; }
        set { this.isFrontRow = value; }
	}

	public bool IsTempSpaceholder {
		get { return true; }
	}
}