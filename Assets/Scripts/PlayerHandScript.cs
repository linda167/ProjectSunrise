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
	// TODO: lindach: This class isn't necessary
	void Start () {
		this.miniCardScript = this.miniCard.GetComponent<PlayerHandMiniCard>();
		this.fullCardScript = this.fullCard.GetComponent<PlayerHandFullCard>();
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public void ShowFullCard() {
		// Show full card
		this.fullCardScript.Show();
    }

	public void ShowMiniCard() {
		this.miniCardScript.Show();
    }
}
