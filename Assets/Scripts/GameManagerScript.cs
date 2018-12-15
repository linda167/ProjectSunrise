using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManagerScript : MonoBehaviour {

	// Time in seconds to wait before cleaning up card after attack
	private const float cardCleanupDelaySec = 0.75f;

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
				selecedPlayerCardScript.PutInAttackLayer();
				cardClickedScript.PutInAttackLayer();
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
		// Put cards back to default layer
		CardScript defendingCardScript = this.defendingCard.GetComponent<CardScript>();
		defendingCardScript.RemoveFromAttackLayer();
		
		CardScript attackCardScript = this.attackingCard.GetComponent<CardScript>();
		attackCardScript.RemoveFromAttackLayer();

		// Do clean up of attack on delay
		StartCoroutine(CleanUpAttackOnDelay(attackCardScript, defendingCardScript));

		// Attack has finished
		this.defendingCard = null;
		this.attackingCard = null;
	}

	private IEnumerator CleanUpAttackOnDelay(CardScript attackCardScript, CardScript defendingCardScript) {
		yield return new WaitForSeconds(GameManagerScript.cardCleanupDelaySec);

		// Hide damage taken on delay
		defendingCardScript.HideDamageBeingTaken();
		attackCardScript.HideDamageBeingTaken();

		// TODO: lindach: Animate death
		// Check if cards have been destroyed
		if (attackCardScript.HealthValue <= 0) {
			Destroy(attackCardScript.gameObject);
		}

		if (defendingCardScript.HealthValue <= 0) {
			Destroy(defendingCardScript.gameObject);
		}

		
	}

	private void SelectPlayerCard(GameObject playerCard) {
		playerCard.GetComponent<CardScript>().SelectCard();
		this.attackingCard = playerCard;
	}

	private void UnselectPlayerCard(GameObject playerCard) {
		playerCard.GetComponent<CardScript>().UnselectCard();
	}
}
