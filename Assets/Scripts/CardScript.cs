using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;

public enum CardState {
	None,
	AttackingTowardsEnemy,
	ReturningFromAttack,
	ShiftingHorizontally,
	BeingDestroyed,
	IsDestroyed
}

public class CardScript : MonoBehaviour, IBoardUnit {
	// Time it takes to fade out the card in seconds
	public const float FadeOutTime = 0.75f;

	// Time to wait before starting fadeout in seconds
	public const float DelayBeforeFadeTime = 0.75f;
	private const float shakeOriginalDecay = 0.002f;
   	private const float shakeOriginalIntensity = 0.08f;

	// Time to hover before showing full card
	private const int hoverTimeBeforeShowingFullCardMS = 500;

	// Time to animate showing full card
	private const int showFullCardAnimationTimeMS = 150;

	private const float cardAttackTime = 0.7f;
	private CardState cardState = CardState.None;

	// Initial scale to set the full card for animation
	private Vector3 showFullCardAnimationInitialScale = new Vector3(0.75f, 0.75f, 0.75f);

	[SerializeField]
	public GameManagerScript gameManager;
	[SerializeField]
	public GameObject cardSprite;

	[SerializeField]
	public bool isEnemyPlayer;
	[SerializeField]
	public bool isFrontRow;

	[SerializeField]
	private DamageIndicator damageIndicatorScript;

	[SerializeField]
	private Transform selectionBorder;

	[SerializeField]
	private Text attackValueDisplay;

	[SerializeField]
	private Text healthValueDisplay;

	[SerializeField]
	private Text damageValueDisplay;

	[SerializeField]
	private Canvas canvas;

	[SerializeField]
	private GameObject fullCard;

	[SerializeField]
	private GameObject damageIndicator;

	// Current attack and health value
	private int attackValue;
	private int healthValue;

	// Whether this card is selected
	private bool isSelected = false;

	// Time when the mouse entered the card, null if mouse is not currently over card
	private DateTime mouseEnterTime;

	private Vector2 originalPosition;
	
	// Time when we started showing the full card
	private DateTime fullCardShowStartTime;

	private Coroutine currentMoveToCoroutine = null;

	private Vector2 queuedNextMovePosition = default(Vector2);

	private Vector2 currentMoveToPosition = default(Vector2);

	public bool IsSelected {
		get { return this.isSelected; }
	}

	public bool IsEnemyPlayer {
		get { return this.isEnemyPlayer; }
	}

	public bool IsFrontRow {
		get { return this.isFrontRow; }
	}

	public bool IsTempSpaceholder {
		get { return false; }
	}

	public int AttackValue {
		get { return this.attackValue; }
	}

	public int HealthValue {
		get { return this.healthValue; }
	}

	private bool IsFullCardShown {
		get { return this.fullCard.GetComponent<Renderer>().enabled; }
	}

	// Use this for initialization
	void Start () {
		// Initialize random attack / health values
		this.attackValue = UnityEngine.Random.Range(1,6);
		this.healthValue = UnityEngine.Random.Range(2,10);

		// Hide selection border to start
		selectionBorder.GetComponent<Renderer>().enabled = false;

		// Set full card to be the highest sorting layer
		this.fullCard.GetComponent<Renderer>().sortingOrder = 100;

		if (this.isEnemyPlayer) {
			// Enemy cards don't move for now
			Destroy(GetComponent<Rigidbody2D>());
		}

		this.originalPosition = this.cardSprite.transform.position;
	}
	
	// Update is called once per frame
	void Update () {
		// Update whether to show full card
		this.UpdateFullCardDisplay();

		// Make sure rotation is straight
		this.transform.rotation = Quaternion.identity;

		// Update attack and health values
		attackValueDisplay.text = attackValue.ToString();
		healthValueDisplay.text = healthValue.ToString();
	}

	private void UpdateFullCardDisplay() {
		// If attack in progress, close full card
		if (this.gameManager.IsPlayersTurn &&
			this.gameManager.IsAttackInProgress) {
			this.CloseFullCard();
		}

		// Animate showing full card
		if (!this.fullCardShowStartTime.Equals(default(DateTime))) {
			double timeSinceStartAnimation = DateTime.Now.Subtract(this.fullCardShowStartTime).TotalMilliseconds;
			if (timeSinceStartAnimation >= CardScript.showFullCardAnimationTimeMS) {
				// Animation is done
				this.fullCard.transform.localScale = new Vector3(1, 1, 1);
				this.fullCardShowStartTime = default(DateTime);
			} else {
				float initialScale = this.showFullCardAnimationInitialScale.x;
				float newScale = (float)(initialScale + 
					(timeSinceStartAnimation / (float) CardScript.showFullCardAnimationTimeMS) *
					(1 - initialScale));
				this.fullCard.transform.localScale = new Vector3(newScale, newScale, newScale);
			}
		}
	}

	void OnMouseDown() {
		if (this.cardState == CardState.BeingDestroyed) {
			return;
		}

		this.gameManager.OnCardClicked(this.gameObject);
    }

	void OnMouseEnter() {
		this.mouseEnterTime = DateTime.Now;
	}

	void OnMouseOver() {
		if (!this.IsFullCardShown &&
			!(this.gameManager.IsPlayersTurn &&
			this.gameManager.IsAttackInProgress) &&
			!this.mouseEnterTime.Equals(default(DateTime)) &&
			DateTime.Now.Subtract(this.mouseEnterTime).TotalMilliseconds > hoverTimeBeforeShowingFullCardMS) {
			
			// Show full card on long hover
			this.StartShowFullCard();
		}
	}

	void OnMouseExit() {
		this.mouseEnterTime = default(DateTime);
		this.CloseFullCard();
    }

	void OnCollisionEnter2D(Collision2D other) {
		if (this.isEnemyPlayer) {
			// Enemies don't move
			return;
		}

		if (other.gameObject.tag == "Card") {
			CardScript cardBeingHitScript = other.gameObject.GetComponent<CardScript>();

			// Enemy card being hit should be the only one on this layer
			Assert.IsTrue(cardBeingHitScript.IsEnemyPlayer);

			// We've hit the enemy
			this.gameManager.OnCardsCollide();
		}
	}

	public void SelectCard() {
		this.isSelected = true;
	
		// Show border
        selectionBorder.GetComponent<Renderer>().enabled = true;
	}

	public void UnselectCard() {
		this.isSelected = false;
	
		// Hide border
        selectionBorder.GetComponent<Renderer>().enabled = false;
	}

	public void MoveHorizontally(Vector2 position) {		
		if (this.cardState == CardState.ShiftingHorizontally && position.Equals(this.currentMoveToPosition)) {
			// We're already moving to the same place, noop
			return;
		}

		if (this.cardState == CardState.None && this.transform.position.Equals(position)) {
			// We're not moving right now and we want to move to where we're already at, noop
			return;
		}

		if (this.cardState == CardState.ShiftingHorizontally) {
			// Cancel current horizontal move
			StopCoroutine(this.currentMoveToCoroutine);
		}

		this.ChangeStateTo(CardState.ShiftingHorizontally);
		this.MoveToPositionOverTime(
			position,
			CardScript.cardAttackTime,
			delegate() {
				this.ChangeStateTo(CardState.None);

				// Move to queued position
				if (!this.queuedNextMovePosition.Equals(default(Vector2))) {
					this.MoveHorizontally(this.queuedNextMovePosition);
					this.queuedNextMovePosition = default(Vector2);
				}
			});
		
		// Save position as new original position
		this.originalPosition = position;
	}

	public void MoveToAttackTarget(Vector2 position) {
		this.ChangeStateTo(CardState.AttackingTowardsEnemy);
		this.MoveToPositionOverTime(
			position,
			CardScript.cardAttackTime,
			delegate() {
				this.ChangeStateTo(CardState.ReturningFromAttack);
			});
	}

	public void MoveBackToOriginalPosition() {
		this.ChangeStateTo(CardState.ReturningFromAttack);

		if (this.currentMoveToCoroutine != null) {
			StopCoroutine(this.currentMoveToCoroutine);
		}

		this.MoveToPositionOverTime(
			originalPosition,
			CardScript.cardAttackTime,
			delegate() {
				this.ChangeStateTo(CardState.None);
				this.gameManager.onAttackFinished();
			});
	}

	public void LoseHealth(int value) {
		this.healthValue -= value;
		this.damageIndicatorScript.ShowDamage(value);
	}

	public void OnAttackStarted() {
		this.PutInAttackLayer();
	}

	public void CleanupCardOnAttackFinished() {
		this.RemoveFromAttackLayer();
		this.damageIndicatorScript.HideDamage();

		if (this.healthValue <= 0) {
			StartCoroutine(ShakeAndRemoveCardObject());
		}
	}

	private void MoveToPositionOverTime(Vector2 targetPosition, float totalTime, System.Action callback) {
		this.currentMoveToPosition = targetPosition;
		this.currentMoveToCoroutine = StartCoroutine(CommonUtils.MoveTowardsTargetOverTime(
			this.transform,
			totalTime,
			targetPosition,
			delegate() {
				this.currentMoveToPosition = default(Vector2);
				callback();
			}));
	}

	private IEnumerator ShakeAndRemoveCardObject() {
		this.ChangeStateTo(CardState.BeingDestroyed);
	
		yield return new WaitForSeconds(CardScript.DelayBeforeFadeTime);

		Vector2 originalPosition = this.cardSprite.transform.position;
		float targetAlphaValue = 0f;
		float originalAlpha = this.cardSprite.GetComponent<SpriteRenderer>().material.color.a;

		
		float shakeDecay = CardScript.shakeOriginalDecay;
   		float shakeIntensity = CardScript.shakeOriginalIntensity;


		for (float t = 0.0f; t < 1.0f; t += Time.deltaTime / CardScript.FadeOutTime) {
			// Fade out the card
			Color newColor = new Color(1, 1, 1, Mathf.Lerp(originalAlpha, targetAlphaValue, t));
			this.cardSprite.GetComponent<SpriteRenderer>().material.color = newColor;
			this.attackValueDisplay.color = newColor;
			this.healthValueDisplay.color = newColor;

			// Shake the card
			if (shakeIntensity > 0f) {
				Vector2 newPosition = originalPosition;
				Vector3 delta = UnityEngine.Random.insideUnitSphere * shakeIntensity;
				newPosition.x += delta.x;
				newPosition.y += delta.y;
				this.cardSprite.transform.position = newPosition;
				shakeIntensity -= shakeDecay;
			}
			yield return null;
        }

		// Destroy object after shaking / fading is done
		Destroy(this.gameObject);
		this.ChangeStateTo(CardState.IsDestroyed);
		this.gameManager.onCardDestroyed(this);
	}

	private void PutInAttackLayer() {
		this.gameObject.layer = 1;
		this.cardSprite.GetComponent<Renderer>().sortingOrder = 1;
		this.damageIndicator.GetComponent<Renderer>().sortingOrder = 2;
		this.canvas.sortingOrder = 3;
	}

	private void RemoveFromAttackLayer() {
		this.gameObject.layer = 0;
		this.cardSprite.GetComponent<Renderer>().sortingOrder = 0;
		this.damageIndicator.GetComponent<Renderer>().sortingOrder = 1;
		this.canvas.sortingOrder = 2;
	}

	private void CloseFullCard() {
		Debug.Log("Closing fulll card");
		this.fullCard.GetComponent<Renderer>().enabled = false;
		this.fullCardShowStartTime = default(DateTime);
	}

	private void StartShowFullCard() {
		Debug.Log("Start showing full card");
		this.fullCard.GetComponent<Renderer>().enabled = true;
		this.fullCard.transform.localScale = this.showFullCardAnimationInitialScale;
		this.fullCardShowStartTime = DateTime.Now;
	}

	private void ChangeStateTo(CardState newState) {
		if (newState == this.cardState) {
			return;
		}

		// Handle destruction states
		if (newState == CardState.BeingDestroyed) {
			this.gameManager.AddCardToMovedOrDestroyed(this);
		} else if (newState == CardState.IsDestroyed) {
			this.gameManager.RemoveCardFromMovedOrDestroyed(this);
		}

		// Handle horizontal move states
		if (newState == CardState.ShiftingHorizontally) {
			this.gameManager.AddCardToMovedOrDestroyed(this);
		} else if (newState == CardState.None && this.cardState == CardState.ShiftingHorizontally) {
			this.gameManager.RemoveCardFromMovedOrDestroyed(this);
		}

		this.cardState = newState;
	}
}
