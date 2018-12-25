using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHandMiniCard : MonoBehaviour {
	[SerializeField]
	private PlayerHandFullCard fullCardScript;
	[SerializeField]
	public GameManagerScript gameManager;
	private const float animateDownTotalTimeSec = 1.7f;
	private const float animateDownTotalDistance = 0.2f;
	private Vector3 originalPosition;
	private Coroutine animateShiftDownCoroutine = null;

	// Use this for initialization
	void Start () {
		this.originalPosition = this.GetComponent<Renderer>().transform.position;
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	void OnMouseEnter() {
		Debug.Log("PlayerHandMiniScard.OnMouseEnter called");

		if (!this.gameManager.IsCardFromHandBeingDragged) {
			this.Hide();
			this.fullCardScript.Show();
		}
    }

	public void Show() {
		this.GetComponent<Renderer>().enabled = true;
		this.GetComponent<BoxCollider2D>().enabled = true;

		Vector3 shiftedtUpPosition = new Vector3(
			originalPosition.x,
			originalPosition.y + PlayerHandMiniCard.animateDownTotalDistance,
			originalPosition.z);
		this.transform.position = shiftedtUpPosition;

		this.animateShiftDownCoroutine = StartCoroutine(
			CommonUtils.MoveTowardsTargetOverTime(
				this.transform,
				PlayerHandMiniCard.animateDownTotalTimeSec,
				originalPosition));
	}

	public void Hide() {
		this.GetComponent<Renderer>().enabled = false;
		this.GetComponent<BoxCollider2D>().enabled = false;
		this.transform.position = this.originalPosition;
		if (this.animateShiftDownCoroutine != null) {
			StopCoroutine(this.animateShiftDownCoroutine);
		}
	}
}
