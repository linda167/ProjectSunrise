using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHandFullCard : MonoBehaviour {
	[SerializeField]
	private PlayerHandScript playerHandCardScript;

	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	void OnMouseExit() {
		Debug.Log("PlayerHandFullCard.OnMouseExit called");
		this.GetComponent<Renderer>().enabled = false;
		this.playerHandCardScript.HideFullCard();
		this.playerHandCardScript.ShowMiniCard();
    }
}
