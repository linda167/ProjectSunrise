using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHandMiniCard : MonoBehaviour {

	[SerializeField]
	private PlayerHandScript playerHandCardScript;
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
		this.GetComponent<Renderer>().enabled = false;
		this.playerHandCardScript.ShowFullCard();
    }

	public void Show() {
		this.GetComponent<Renderer>().enabled = true;

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
}
