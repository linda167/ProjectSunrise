using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardSpriteScript : MonoBehaviour {
	[SerializeField]
	public CardScript cardScript;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	void OnMouseDown() {
		this.cardScript.OnMouseDown();
    }

	void OnMouseEnter() {
		this.cardScript.OnMouseEnter();
	}

	void OnMouseOver() {
		this.cardScript.OnMouseOver();
	}

	void OnMouseExit() {
		this.cardScript.OnMouseExit();
    }
}
