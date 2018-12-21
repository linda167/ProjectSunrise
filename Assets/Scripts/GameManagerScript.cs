using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManagerScript : MonoBehaviour {
	[SerializeField] 
	private GameObject cardPrefab;
	[SerializeField] 
	private GameObject playerHandCardPrefab;

	private const float distanceBetweenShipsX = -0.1f;
	private float cardWidth;

	GameObject attackingCard = null;
	GameObject defendingCard = null;
	CameraShakeScript camShakeScript;
	
	// Currently always player's turn
	private bool isPlayersTurn = true;

	private int playerFrontCards = 0;
	private int playerBackCards = 0;
	private int enemyFrontCards = 0;
	private int enemyBackCards = 0;

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
		if (this.defendingCard != null) {
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
				// Check that we're not attacking back row while enemy has a front line
				if (!cardClickedScript.isFrontRow && this.enemyFrontCards > 0) {
					return;
				}

				// Attack enemy
				selecedPlayerCardScript.MoveTowardsCard(cardClickedScript);
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
		if (!cardScript.isEnemyPlayer && cardScript.isFrontRow) {
			this.playerFrontCards--;
		} else if (!cardScript.isEnemyPlayer && !cardScript.isFrontRow) {
			this.playerBackCards--;
		} else if (cardScript.isEnemyPlayer && cardScript.isFrontRow) {
			this.enemyFrontCards--;
		} else if (cardScript.isEnemyPlayer && !cardScript.isFrontRow) {
			this.enemyBackCards--;
		}
	}

	private void InitializeBoard() {
		this.InitializePlayerHand();
		this.InitializeBoardShips();
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
		return playerHandCard;
	}

	private void InitializeBoardShips() {
		// Randomly generate ships for now
		float friendlyRow1PosY = -1.0f;
		float friendlyRow2PosY = -2.6f;
		float enemyRow1PosY = 1.0f;
		float enemyRow2PosY = 2.6f;

		this.AddRowOfShips(friendlyRow1PosY, false /* isEnemyPlayer */, true /* isFrontRow */);
		this.AddRowOfShips(friendlyRow2PosY, false /* isEnemyPlayer */, false /* isFrontRow */);
		this.AddRowOfShips(enemyRow1PosY, true /* isEnemyPlayer */, true /* isFrontRow */);
		this.AddRowOfShips(enemyRow2PosY, true /* isEnemyPlayer */, false /* isFrontRow */);
	}

	private void AddRowOfShips(float yPos, bool isEnemyPlayer, bool isFrontRow) {
		// Generate 1-7 ships
		int shipCount = UnityEngine.Random.Range(1,8);

		float totalWidth = shipCount * this.cardWidth + (shipCount - 1) * GameManagerScript.distanceBetweenShipsX;
		float shipPositionX = (float)(-1 * (totalWidth / 2.0) + 0.5 * this.cardWidth);
		
		for (int i = 0; i < shipCount; i++) {
			InstantiateCardPrefab(shipPositionX, yPos, isEnemyPlayer, isFrontRow);
			shipPositionX += this.cardWidth + GameManagerScript.distanceBetweenShipsX;
		}
	}

	private GameObject InstantiateCardPrefab(float xPos, float yPos, bool isEnemyPlayer, bool isFrontRow) {
		GameObject card = Instantiate(this.cardPrefab, new Vector2(xPos,yPos), Quaternion.identity);
		CardScript cardScript = card.GetComponent<CardScript>();
		cardScript.gameManager = this;
		cardScript.isEnemyPlayer = isEnemyPlayer;
		cardScript.isFrontRow = isFrontRow;

		if (!isEnemyPlayer && isFrontRow) {
			this.playerFrontCards++;
		} else if (!isEnemyPlayer && !isFrontRow) {
			this.playerBackCards++;
		} else if (isEnemyPlayer && isFrontRow) {
			this.enemyFrontCards++;
		} else if (isEnemyPlayer && !isFrontRow) {
			this.enemyBackCards++;
		}

		return card;
	}

	private void SelectPlayerCard(GameObject playerCard) {
		playerCard.GetComponent<CardScript>().SelectCard();
		this.attackingCard = playerCard;
	}

	private void UnselectPlayerCard(GameObject playerCard) {
		playerCard.GetComponent<CardScript>().UnselectCard();
	}
}
