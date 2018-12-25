using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHandFullCard : MonoBehaviour {
	[SerializeField]
	public GameManagerScript gameManager;
	[SerializeField]
	private PlayerHandMiniCard miniCardScript;
	[SerializeField]
	public GameObject playerHandCard;
	private const float animateUpTotalTimeSec = 1.25f;
	private const float animateUpTotalDistance = 0.1f;
	private Coroutine animateShiftUpCoroutine = null;
	private Vector3 originalPosition;
	private bool isHovered = false;
	private bool isDraggingCard = false;
	private DateTime lastShownTime;

	// Use this for initialization
	void Start () {
		this.originalPosition = this.GetComponent<Renderer>().transform.position;
	}
	
	// Update is called once per frame
	void Update () {
		if (this.isDraggingCard) {
			// Move full card with mouse cursor
			Vector2 cursorPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			this.transform.position = cursorPosition;
			this.gameManager.OnDropOverCard(this, cursorPosition);
		}
		else if (DateTime.Now.Subtract(this.lastShownTime).TotalMilliseconds > 100 &&
			!this.isHovered &&
			this.GetComponent<Renderer>().enabled) {
			// Hide full card if no longer hovered
			Debug.Log("Hiding full card because no longer hovered");
			this.HideFullCardShowMiniCard();
		}
	}

	void OnMouseEnter() {
		this.isHovered = true;
    }

	void OnMouseExit() {
		if (!isDraggingCard) {
			Debug.Log("PlayerHandFullCard.OnMouseExitCalled");
			this.isHovered = false;
			this.HideFullCardShowMiniCard();
		}
    }

	void OnMouseDown() {
		Debug.Log("PlayerHandFullCard OnMouseDown");
		this.isDraggingCard = true;
		this.gameManager.IsCardFromHandBeingDragged = true;

		if (this.animateShiftUpCoroutine != null) {
			// Stop any animation
			StopCoroutine(this.animateShiftUpCoroutine);
		}
	}

	void OnMouseUp() {
		Debug.Log("PlayerHandFullCard OnMouseUp");
		this.isDraggingCard = false;
		this.gameManager.IsCardFromHandBeingDragged = false;

		Vector2 cursorPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		this.gameManager.OnDropCard(this, cursorPosition);
	}

	public void Show() {
		this.lastShownTime = DateTime.Now;

		// Make sure card is straight
		this.transform.rotation = Quaternion.identity;
		this.GetComponent<Renderer>().enabled = true;
		this.GetComponent<BoxCollider2D>().enabled = true;

		// Put full card at the front most z so it can receive mouse events
		Transform fullCardTransform = this.GetComponent<Renderer>().transform;
		fullCardTransform.position = new Vector3(fullCardTransform.position.x, fullCardTransform.position.y, -2);

		// Animate moving card up
		Vector3 shiftUpPosition = new Vector3(
			fullCardTransform.position.x,
			fullCardTransform.position.y + PlayerHandFullCard.animateUpTotalDistance,
			fullCardTransform.position.z);
		this.animateShiftUpCoroutine = StartCoroutine(
			CommonUtils.MoveTowardsTargetOverTime(
				this.transform,
				PlayerHandFullCard.animateUpTotalTimeSec,
				shiftUpPosition));
	}

	public void HideFullCardShowMiniCard() {
		this.Hide();
		this.miniCardScript.Show();
	}

	public void RemovePlayerHandCard() {
		// Remove the entire player hand card
		Destroy(this.playerHandCard);
	}

	private void Hide() {
		Debug.Log("PlayerHandFullCard.Hide called");
		this.GetComponent<Renderer>().enabled = false;
		this.GetComponent<BoxCollider2D>().enabled = false;

		if (this.animateShiftUpCoroutine != null) {
			// Stop any animation, and restore original position
			StopCoroutine(this.animateShiftUpCoroutine);
		}

		// Restore original position
		Transform fullCardTransform = this.GetComponent<Renderer>().transform;
		fullCardTransform.position = this.originalPosition;
	}
}
