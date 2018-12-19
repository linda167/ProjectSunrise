using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHandScript : MonoBehaviour {

	[SerializeField]
	private GameObject fullCard;

	[SerializeField]
	private GameObject miniCard;

	private PlayerHandMiniCard miniCardScript;
	private PlayerHandFullCard fullCardScript;

	// Use this for initialization
	void Start () {
		this.miniCardScript = this.miniCard.GetComponent<PlayerHandMiniCard>();
		this.fullCardScript = this.fullCard.GetComponent<PlayerHandFullCard>();
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public void ShowFullCard() {
		// Show full card, make sure it's straight
		this.fullCard.transform.rotation = Quaternion.identity;
		this.fullCard.GetComponent<Renderer>().enabled = true;

		// Put full card at the front most z so it can receive mouse events
		Transform fullCardTransform = this.fullCard.GetComponent<Renderer>().transform;
		fullCardTransform.position = new Vector3(fullCardTransform.position.x, fullCardTransform.position.y, -2);
    }

	public void HideFullCard() {
		this.fullCard.GetComponent<Renderer>().enabled = false;

		// Restore original z position
		Transform fullCardTransform = this.fullCard.GetComponent<Renderer>().transform;
		fullCardTransform.position = new Vector3(fullCardTransform.position.x, fullCardTransform.position.y, 0);
	}

	public void ShowMiniCard() {
		this.miniCard.GetComponent<Renderer>().enabled = true;
    }
}
