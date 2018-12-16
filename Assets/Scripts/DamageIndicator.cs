using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DamageIndicator : MonoBehaviour {
	[SerializeField]
	private GameObject damageBackground;

	[SerializeField]
	private Text damageText;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {		
	}

	public void ShowDamage(int damage) {
		this.damageBackground.GetComponent<Renderer>().enabled = true;
		this.damageText.text = "-" + damage;
	}

	public void HideDamage() {
		// Make damage fade out over time
		StartCoroutine(this.FadeOutDamageBackground(0, CardScript.FadeOutTime));
	}

	private IEnumerator FadeOutDamageBackground(float targetAlphaValue, float fadeOutTime) {
		Transform backgroundTransform = this.damageBackground.transform;
		Color originalTextColor = this.damageText.color;
		Color originalBackgroundColor = transform.GetComponent<SpriteRenderer>().material.color;

		// Wait a bit before fading
		yield return new WaitForSeconds(CardScript.DelayBeforeFadeTime);

        float originalBackgroundAlpha = transform.GetComponent<SpriteRenderer>().material.color.a;
        for (float t = 0.0f; t < 1.0f; t += Time.deltaTime / fadeOutTime) {
			Color newColor = new Color(1, 1, 1, Mathf.Lerp(originalBackgroundAlpha, targetAlphaValue, t));
			backgroundTransform.GetComponent<SpriteRenderer>().material.color = newColor;
			this.damageText.color = newColor;
			yield return null;
        }

		// Animation done, hide renderer entirely
		this.damageBackground.GetComponent<Renderer>().enabled = false;
		
		// Restore original color
		backgroundTransform.GetComponent<SpriteRenderer>().material.color = originalBackgroundColor;
		this.damageText.color = originalTextColor;

		// Remove text display
		this.damageText.text = "";
     }
}
