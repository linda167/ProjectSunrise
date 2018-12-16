using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManagerScript : MonoBehaviour {
	[SerializeField] 
	private GameObject cardPrefab;

	private const float distanceBetweenShipsX = 0.6f;
	private float cardWidth;

	GameObject attackingCard = null;
	GameObject defendingCard = null;
	CameraShakeScript camShakeScript;
	
	// Currently always player's turn
	private bool isPlayersTurn = true;

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
				// We're clicking on enemy card, attack
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

	private void InitializeBoard() {
		// Randomly generate ships for now
		float friendlyRow1PosY = -2.65f;
		float enemyRow1PosY = 3.5f;

		this.AddRowOfShips(friendlyRow1PosY, false /* isEnemyPlayer */);
		this.AddRowOfShips(enemyRow1PosY, true /* isEnemyPlayer */);
	}

	private void AddRowOfShips(float yPos, bool isEnemyPlayer) {
		// Generate 1-7 ships
		int shipCount = UnityEngine.Random.Range(1,8);

		float totalWidth = shipCount * this.cardWidth + (shipCount - 1) * GameManagerScript.distanceBetweenShipsX;
		float shipPositionX = (float)(-1 * (totalWidth / 2.0) + 0.5 * this.cardWidth);
		
		for (int i = 0; i < shipCount; i++) {
			GameObject card = InstantiateCardPrefab(shipPositionX, yPos, isEnemyPlayer);
			shipPositionX += this.cardWidth + GameManagerScript.distanceBetweenShipsX;
		}
	}

	private GameObject InstantiateCardPrefab(float xPos, float yPos, bool isEnemyPlayer) {
		GameObject card = Instantiate(cardPrefab, new Vector2(xPos,yPos), Quaternion.identity);
		CardScript cardScript = card.GetComponent<CardScript>();
		cardScript.gameManager = this;
		cardScript.isEnemyPlayer = isEnemyPlayer;
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
