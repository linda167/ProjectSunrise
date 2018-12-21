using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHandFullCard : MonoBehaviour {
	[SerializeField]
	private PlayerHandMiniCard miniCardScript;
	private const float animateUpTotalTimeSec = 1.25f;
	private const float animateUpTotalDistance = 0.1f;
	private Coroutine animateShiftUpCoroutine = null;
	private Vector3 originalPosition;
	private bool isHovered = false;
	private DateTime lastShownTime;

	// Use this for initialization
	void Start () {
		this.originalPosition = this.GetComponent<Renderer>().transform.position;
	}
	
	// Update is called once per frame
	void Update () {
		if (DateTime.Now.Subtract(this.lastShownTime).TotalMilliseconds > 100 &&
			!this.isHovered &&
			this.GetComponent<Renderer>().enabled) {
			// Hide full card if no longer hovered
			this.HideFullCardShowMiniCard();
		}		
	}

	void OnMouseEnter() {
		this.isHovered = true;
    }

	void OnMouseExit() {
		Debug.Log("PlayerHandFullCard.OnMouseExitCalled");
		this.isHovered = false;
		this.HideFullCardShowMiniCard();
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

	private void HideFullCardShowMiniCard() {
		this.Hide();
		this.miniCardScript.Show();
	}

	private void Hide() {
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
