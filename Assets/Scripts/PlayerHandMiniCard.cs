using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHandMiniCard : MonoBehaviour {

	[SerializeField]
	private PlayerHandScript playerHandCardScript;

	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	void OnMouseEnter() {
		Debug.Log("PlayerHandMiniScard.OnMouseEnter called");
		this.GetComponent<Renderer>().enabled = false;
		this.playerHandCardScript.ShowFullCard();
    }
}
