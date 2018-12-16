using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;

public class CardScript : MonoBehaviour {
	// Time it takes to fade out the card in seconds
	public const float FadeOutTime = 0.75f;

	// Time to wait before starting fadeout in seconds
	public const float DelayBeforeFadeTime = 0.75f;
	private const float shakeOriginalDecay = 0.002f;
   	private const float shakeOriginalIntensity = 0.08f;

	// Time to hover before showing full card
	private const int hoverTimeBeforeShowingFullCardMS = 700;

	// Time to animate showing full card
	private const int showFullCardAnimationTimeMS = 150;

	
	private const float cardMoveSpeed = 11f;

	// Initial scale to set the full card for animation
	private Vector3 showFullCardAnimationInitialScale = new Vector3(0.75f, 0.75f, 0.75f);

	[SerializeField]
	private GameManagerScript gameManager;

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

	[SerializeField]
	private bool isEnemyPlayer;

	// Current attack and health value
	private int attackValue;
	private int healthValue;

	// Whether this card is selected
	private bool isSelected = false;

	// Time when the mouse entered the card, null if mouse is not currently over card
	private DateTime mouseEnterTime;

	private Vector2 originalPosition;

	private Vector2 targetPosition = Vector2.zero;
	
	// Time when we started showing the full card
	private DateTime fullCardShowStartTime;

	public bool IsSelected {
		get { return this.isSelected; }
	}

	public bool IsEnemyPlayer {
		get { return this.isEnemyPlayer; }
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
		this.fullCard.GetComponent<Renderer>().sortingOrder = 10;

		if (this.isEnemyPlayer) {
			// Enemy cards don't move for now
			Destroy(GetComponent<Rigidbody2D>());
		}

		this.originalPosition = this.transform.position;
	}
	
	// Update is called once per frame
	void Update () {
		// Update movement if we're moving towards target
		this.UpdateMoveTowardsTarget();

		// Update whether to show full card
		this.UpdateFullCardDisplay();

		// Make sure rotation is straight
		this.transform.rotation = Quaternion.identity;

		// Update attack and health values
		attackValueDisplay.text = attackValue.ToString();
		healthValueDisplay.text = healthValue.ToString();
	}

	private void UpdateMoveTowardsTarget() {
		// We're in the process of moving towards a target position
		if (this.targetPosition != Vector2.zero) {
			float step = cardMoveSpeed * Time.deltaTime;
			this.transform.position = Vector2.MoveTowards(this.transform.position, this.targetPosition, step);

			// We've reached our destination
			if (this.transform.position.Equals(this.targetPosition)) {
				this.targetPosition = Vector2.zero;

				// If we've returned to original position, reset layer to default
				if (this.transform.position.Equals(this.originalPosition))  {
					this.gameManager.onAttackFinished();
				}
			}
		}
	}

	private void UpdateFullCardDisplay() {
		// If attack in progress, close full card
		if (this.gameManager.IsPlayersTurn &&
			this.gameManager.IsAttackInProgress) {
			this.closeFullCard();
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
			this.startShowFullCard();
		}
	}

	void OnMouseExit() {
		this.mouseEnterTime = default(DateTime);
		this.closeFullCard();
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

	public void MoveTowardsCard(CardScript cardToAttack) {
		this.targetPosition = cardToAttack.transform.position;
	}

	public void MoveBackToOriginalPosition() {
		this.targetPosition = originalPosition;
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

	private IEnumerator ShakeAndRemoveCardObject() {
		yield return new WaitForSeconds(CardScript.DelayBeforeFadeTime);

		Vector2 originalPosition = transform.position;
		float targetAlphaValue = 0f;
		float originalAlpha = transform.GetComponent<SpriteRenderer>().material.color.a;

		
		float shakeDecay = CardScript.shakeOriginalDecay;
   		float shakeIntensity = CardScript.shakeOriginalIntensity;


		for (float t = 0.0f; t < 1.0f; t += Time.deltaTime / CardScript.FadeOutTime) {
			// Fade out the card
			Color newColor = new Color(1, 1, 1, Mathf.Lerp(originalAlpha, targetAlphaValue, t));
			transform.GetComponent<SpriteRenderer>().material.color = newColor;

			// Shake the card
			if (shakeIntensity > 0f) {
				Vector2 newPosition = originalPosition;
				Vector3 delta = UnityEngine.Random.insideUnitSphere * shakeIntensity;
				newPosition.x += delta.x;
				newPosition.y += delta.y;
				transform.position = newPosition;
				shakeIntensity -= shakeDecay;
			}
			yield return null;
        }

		// TODO: lindach: Add shaking and fading
		Destroy(this.gameObject);
	}

	private void PutInAttackLayer() {
		this.gameObject.layer = 1;
		this.gameObject.GetComponent<Renderer>().sortingOrder = 1;
		this.damageIndicator.GetComponent<Renderer>().sortingOrder = 2;
		this.canvas.sortingOrder = 3;
	}

	private void RemoveFromAttackLayer() {
		this.gameObject.layer = 0;
		this.gameObject.GetComponent<Renderer>().sortingOrder = 0;
		this.damageIndicator.GetComponent<Renderer>().sortingOrder = 1;
		this.canvas.sortingOrder = 2;
	}

	private void closeFullCard() {
		Debug.Log("Closing fulll card");
		this.fullCard.GetComponent<Renderer>().enabled = false;
		this.fullCardShowStartTime = default(DateTime);
	}

	private void startShowFullCard() {
		Debug.Log("Start showing full card");
		this.fullCard.GetComponent<Renderer>().enabled = true;
		this.fullCard.transform.localScale = this.showFullCardAnimationInitialScale;
		this.fullCardShowStartTime = DateTime.Now;
	}
}
