using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManagerScript : MonoBehaviour {
	[SerializeField] 
	private GameObject cardPrefab;
	[SerializeField] 
	private GameObject playerHandCardPrefab;

	private const float distanceBetweenShipsX = -0.1f;
	private const float row1YPos = 1.0f;
	private const float row2YPos = 2.6f;
	private float cardWidth;

	GameObject attackingCard = null;
	GameObject defendingCard = null;
	CameraShakeScript camShakeScript;
	
	// Currently always player's turn
	private bool isPlayersTurn = true;

	private List<IBoardUnit> boardUnits = new List<IBoardUnit>();
	private List<CardScript> cardsBeingMovedOrDestroyed = new List<CardScript>();

	private PlaceholderBoardUnit tempDropOverCard = new PlaceholderBoardUnit();

	public bool IsPlayersTurn {
		get { return this.isPlayersTurn; }
	}

	public bool IsAttackInProgress {
		get { return this.defendingCard != null; }
	}

	// Use this for initialization
	void Start () {
		this.camShakeScript = GetComponent<CameraShakeScript>();
		this.cardWidth = this.cardPrefab.transform.GetComponent<Renderer>().bounds.size.x;;

		this.InitializeBoard();
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public void OnCardClicked(GameObject cardClicked) {
		if (this.IsAttackInProgress) {
			// Current attack going on
			return;
		}
		
		CardScript selecedPlayerCardScript = null;
		CardScript cardClickedScript = cardClicked.GetComponent<CardScript>();
		if (this.attackingCard != null) {
			selecedPlayerCardScript = this.attackingCard.GetComponent<CardScript>();
		}

		// No current selection
		if (this.attackingCard == null) {
			if (cardClickedScript.IsEnemyPlayer) {
				// Selecting enemy card not allowed
				return;
			}

			// Selecting player card
			this.SelectPlayerCard(cardClicked);
		} else {
			// We have existing selection on player card

			if (cardClicked == this.attackingCard) {
				// Selecting same card, unselect
				this.UnselectPlayerCard(this.attackingCard);
				this.attackingCard = null;
			} else if (!cardClickedScript.IsEnemyPlayer) {
				// We're clicking on another player card
				// Unselect existing player card
				this.UnselectPlayerCard(this.attackingCard);
				this.SelectPlayerCard(cardClicked);
			} else {
				// We're clicking on enemy card

				// Do not allow attack if cards are still being destroyed or moved from previous attack
				if (this.cardsBeingMovedOrDestroyed.Count > 0) {
					return;
				}

				// Check that we're not attacking back row while enemy has a front line
				int enemyFrontCards = this.GetShipCountForRow(true /* isEnemyPlayer */, true /* isFrontRow */);
				if (!cardClickedScript.isFrontRow && enemyFrontCards > 0) {
					return;
				}

				// Attack enemy
				selecedPlayerCardScript.MoveToAttackTarget(cardClickedScript.cardSprite.transform.position);
				this.defendingCard = cardClicked;

				// Move cards clashing to attack layer
				selecedPlayerCardScript.OnAttackStarted();
				cardClickedScript.OnAttackStarted();
			}
		}
	}

	public void OnCardsCollide() {

		// Move attacking card back to original position
		CardScript attackCardScript = this.attackingCard.GetComponent<CardScript>();
		attackCardScript.MoveBackToOriginalPosition();

		// Unselect attacking card
		this.UnselectPlayerCard(this.attackingCard);

		// Update health values
		CardScript defendingCardScript = this.defendingCard.GetComponent<CardScript>();
		attackCardScript.LoseHealth(defendingCardScript.AttackValue);
		defendingCardScript.LoseHealth(attackCardScript.AttackValue);

		// Shake camera
		camShakeScript.Shake();
	}

	public void onAttackFinished() {
		// Clean up cards on attack finish
		CardScript defendingCardScript = this.defendingCard.GetComponent<CardScript>();
		defendingCardScript.CleanupCardOnAttackFinished();
		
		CardScript attackCardScript = this.attackingCard.GetComponent<CardScript>();
		attackCardScript.CleanupCardOnAttackFinished();

		// Attack has finished
		this.defendingCard = null;
		this.attackingCard = null;
	}

	public void onCardDestroyed(CardScript cardScript) {
		this.boardUnits.Remove(cardScript);

		// Move rest of ships to account for removed space
		this.RerenderRow(cardScript.isEnemyPlayer, cardScript.isFrontRow);
	}

	public void AddCardToMovedOrDestroyed(CardScript cardScript) {
		this.cardsBeingMovedOrDestroyed.Add(cardScript);
	}

	public void RemoveCardFromMovedOrDestroyed(CardScript cardScript) {
		this.cardsBeingMovedOrDestroyed.Remove(cardScript);
	}

	public void OnDropOverCard(PlayerHandFullCard fullCard, Vector2 dropPosition) {
		int dropRow = this.GetRowPositionFromDropPosition(dropPosition);

		// Remove existing placeHolder
		this.boardUnits.Remove(this.tempDropOverCard);
		Debug.Log("Removing existing placeholder, count=: " + this.boardUnits.Count);

		if (dropRow > 0) {
			this.tempDropOverCard.IsFrontRow = dropRow == 1;
			int colPosition = this.GetColPositionFromDropPosition(dropPosition, false /* isEnemyPlayer */, this.tempDropOverCard.IsFrontRow);
			this.InsertBoardUnitAtIndex(colPosition, this.tempDropOverCard);
		}

		// Move units accordingly
		this.RerenderPlayerRows();
	}

	public void OnDropCard(PlayerHandFullCard fullCard, Vector2 dropPosition) {
		this.boardUnits.Remove(this.tempDropOverCard);
		int dropRow = this.GetRowPositionFromDropPosition(dropPosition);
		this.RerenderPlayerRows();
	}

	private void RerenderPlayerRows() {
		this.RerenderRow(false /* isEnemyPlayer */, true /* isFrontRow */);
		this.RerenderRow(false /* isEnemyPlayer */, false /* isFrontRow */);
	}

	private void InitializeBoard() {
		this.InitializePlayerHand();
		this.InitializeBoardShips();
	}

	private void RerenderRow(bool isEnemyPlayer, bool isFrontRow) {
		// Move rest of ships to account for removed space
		List<IBoardUnit> unitsList = this.GetBoardUnitsForRow(isEnemyPlayer, isFrontRow);
		List<Vector2> unitPositions = this.GetUnitPositionsForRow(isEnemyPlayer, isFrontRow, unitsList.Count);	
		for (int i = 0; i < unitsList.Count; i++) {
			IBoardUnit unit = unitsList[i];
			if (!unit.IsTempSpaceholder) {
				((CardScript)unit).MoveHorizontally(unitPositions[i]);
			}
		}
	}

	private void InitializePlayerHand() {
		// Generate 1-7 cards
		int cardCount = UnityEngine.Random.Range(1,8);

		// Calculate left most card x position
		const float xDelta = 1f;
		float xPos = (float)(-1 * xDelta * (cardCount - 1) / 2.0);

		// Calculate left most card y position
		const float yBaseLine = -4.64f;
		const float yDelta = 0.05f;
		float yPos = (float)(yBaseLine - (cardCount - 1) / 2.0 * yDelta);

		// Calculate left most rotation
		const float zRotationDelta = -0.035f;
		float rotationZ = (float)((cardCount - 1) / 2.0 * -1 * zRotationDelta);

		for (int i = 0; i < cardCount; i++) {
			// Adjust y position
			float distanceFromCenter = Mathf.Ceil(Mathf.Abs((float)((cardCount - 1) / 2.0 - i)));
			float yAdjust = 0;
			if (distanceFromCenter == 1) {
				if (cardCount >= 7) {
					yAdjust = 0.02f;
				}
				else if (cardCount >= 5) {
					yAdjust = 0.01f;
				}
			} else if (distanceFromCenter == 2) {
				if (cardCount == 4) {
					yAdjust = -0.02f;
				}
				else if (cardCount == 5) {
					yAdjust = -0.06f;				
				}
				else if (cardCount == 6) {
					yAdjust = -0.03f;				
				}
				else if (cardCount == 7) {
					yAdjust = -0.03f;				
				}
			} else if (distanceFromCenter == 3) {
				if (cardCount >= 7) {
					yAdjust = -0.16f;
				} else {
					yAdjust = -0.14f;
				}
			}

			Vector3 position = new Vector3(xPos, yPos + yAdjust, 0);
			Quaternion rotation = new Quaternion(0, 0, rotationZ, 1);
			GameObject playerHandCard = InstantiatePlayerHandCardPrefab(position, rotation, i /* sortingOrder */);

			// Calculate new positions
			xPos += xDelta;
			rotationZ += zRotationDelta;
			if (i + 1< cardCount / 2.0) {
				yPos += yDelta;
			} else if (i + 1> cardCount / 2.0) {
				yPos -= yDelta;
			}
		}
	}

	private GameObject InstantiatePlayerHandCardPrefab(Vector3 position, Quaternion rotation, int sortingOrder) {
		GameObject playerHandCard = Instantiate(this.playerHandCardPrefab, position, rotation);
		GameObject miniCard = playerHandCard.transform.GetChild(1).gameObject;
		miniCard.GetComponent<Renderer>().sortingOrder = sortingOrder;

		GameObject fullCard = playerHandCard.transform.GetChild(0).gameObject;
		PlayerHandFullCard fullCardScript = fullCard.GetComponent<PlayerHandFullCard>();
		fullCardScript.gameManager = this;

		return playerHandCard;
	}

	private void InitializeBoardShips() {
		// Randomly generate ships for now
		this.AddRowOfShips(false /* isEnemyPlayer */, true /* isFrontRow */);
		this.AddRowOfShips(false /* isEnemyPlayer */, false /* isFrontRow */);
		this.AddRowOfShips(true /* isEnemyPlayer */, true /* isFrontRow */);
		this.AddRowOfShips(true /* isEnemyPlayer */, false /* isFrontRow */);
	}

	private void AddRowOfShips(bool isEnemyPlayer, bool isFrontRow) {
		// Generate 1-7 ships
		int shipCount = UnityEngine.Random.Range(1,8);

		List<Vector2> shipPositions = this.GetUnitPositionsForRow(isEnemyPlayer, isFrontRow, shipCount);		
		for (int i = 0; i < shipCount; i++) {
			InstantiateCardPrefab(shipPositions[i].x, shipPositions[i].y, isEnemyPlayer, isFrontRow);
		}
	}

	private List<Vector2> GetUnitPositionsForRow(bool isEnemyPlayer, bool isFrontRow, int shipCount) {
		List<Vector2> positionList = new List<Vector2>();
		float yPos = this.GetShipYPosition(isEnemyPlayer, isFrontRow);
		float totalWidth = shipCount * this.cardWidth + (shipCount - 1) * GameManagerScript.distanceBetweenShipsX;
		float shipPositionX = (float)(-1 * (totalWidth / 2.0) + 0.5 * this.cardWidth);
		for (int i = 0; i < shipCount; i++) {
			positionList.Add(new Vector2(shipPositionX, yPos));
			shipPositionX += this.cardWidth + GameManagerScript.distanceBetweenShipsX;
		}

		return positionList;
	}

	private float GetShipYPosition(bool isEnemyPlayer, bool isFrontRow) {
		if (isEnemyPlayer && isFrontRow) {
			return GameManagerScript.row1YPos;
		} else if (isEnemyPlayer && !isFrontRow) {
			return GameManagerScript.row2YPos;
		} else if (!isEnemyPlayer && isFrontRow) {
			return -GameManagerScript.row1YPos;
		} else if (!isEnemyPlayer && !isFrontRow) {
			return -GameManagerScript.row2YPos;
		}

		return 0;
	}

	/**
	 * Returns:
	 *   1 for front row
     *   2 for backrow
	 *   0 to indicate invalid drop zone
	 */
	private int GetRowPositionFromDropPosition(Vector2 dropPosition) {
		const float bufferYAmountRow2 = 1.1f;
		const float bufferYAmountRow1 = 1.3f;
		if (dropPosition.y < -GameManagerScript.row2YPos - bufferYAmountRow2) {
			// Invalid drop zone. This is close to player's hand
			return 0;
		} else if (dropPosition.y < -GameManagerScript.row1YPos - bufferYAmountRow1) {
			// Backrow
			return 2;
		} else {
			// Front row
			return 1;
		}
	}

	private int GetColPositionFromDropPosition(Vector2 dropPosition, bool isEnemyPlayer, bool isFrontRow) {
		List<IBoardUnit> unitsList = this.GetBoardUnitsForRow(isEnemyPlayer, isFrontRow);
		List<Vector2> unitPositions = this.GetUnitPositionsForRow(isEnemyPlayer, isFrontRow, unitsList.Count);
		for (int i = 0; i < unitsList.Count; i++) {
			IBoardUnit unit = unitsList[i];
			if (unit.IsTempSpaceholder) {
				continue;
			}

			Vector2 unitPosition = unitPositions[i];
			if (dropPosition.x < unitPosition.x) {
				return i;
			}
		}

		return unitsList.Count;
	}


	private void InsertBoardUnitAtIndex(int targetIndex, IBoardUnit boardUnit) {
		if (targetIndex == 0) {
			this.boardUnits.Insert(0, boardUnit);
			return;
		}

		int seenCount = 0;
		for (int i = 0; i< this.boardUnits.Count; i++) {
			IBoardUnit unit = this.boardUnits[i];
			if (unit.IsEnemyPlayer == boardUnit.IsEnemyPlayer && unit.IsFrontRow == boardUnit.IsFrontRow) {
				seenCount++;
			}

			if (seenCount == targetIndex) {
				this.boardUnits.Insert(i+1, boardUnit);
				break;
			}
		}
	}

	private GameObject InstantiateCardPrefab(float xPos, float yPos, bool isEnemyPlayer, bool isFrontRow) {
		GameObject card = Instantiate(this.cardPrefab, new Vector2(xPos,yPos), Quaternion.identity);
		CardScript cardScript = card.GetComponent<CardScript>();
		cardScript.gameManager = this;
		cardScript.isEnemyPlayer = isEnemyPlayer;
		cardScript.isFrontRow = isFrontRow;
		this.boardUnits.Add(cardScript);

		return card;
	}

	private void SelectPlayerCard(GameObject playerCard) {
		playerCard.GetComponent<CardScript>().SelectCard();
		this.attackingCard = playerCard;
	}

	private void UnselectPlayerCard(GameObject playerCard) {
		playerCard.GetComponent<CardScript>().UnselectCard();
	}

	private int GetShipCountForRow(bool isEnemyPlayer, bool isFrontRow) {
		return this.GetBoardUnitsForRow(isEnemyPlayer, isFrontRow).Count;
	}

	private List<IBoardUnit> GetBoardUnitsForRow(bool isEnemyPlayer, bool isFrontRow) {
		List<IBoardUnit> unitsList = new List<IBoardUnit>();
		for (int i=0; i<this.boardUnits.Count; i++) {
			IBoardUnit unit = this.boardUnits[i];

			if (unit.IsEnemyPlayer == isEnemyPlayer &&
				unit.IsFrontRow == isFrontRow) {
				// Add units in specified row
				unitsList.Add(unit);
			}
		}
		return unitsList;
	}
}
