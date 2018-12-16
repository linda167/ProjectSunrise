using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DamageIndicator : MonoBehaviour {
	[SerializeField]
	private GameObject damageBackground;

	[SerializeField]
	private Text damageText;

	private Coroutine currentFadeOutCoroutine = null;

	private Color originalTextColor;
	private Color originalBackgroundColor;

	private bool IsShowingDamage {
		get { return this.damageText.text != ""; }
	}

	// Use this for initialization
	void Start () {
		this.originalTextColor = this.damageText.color;
		this.originalBackgroundColor = transform.GetComponent<SpriteRenderer>().material.color;
	}
	
	// Update is called once per frame
	void Update () {		
	}

	public void ShowDamage(int damage) {
		// Cancel current fade out if one is going on
		if (this.currentFadeOutCoroutine != null) {
			StopCoroutine(this.currentFadeOutCoroutine);
			this.ResetDamageIndicatorRender();
		}

		this.damageBackground.GetComponent<Renderer>().enabled = true;
		this.damageText.text = "-" + damage;
	}

	public void HideDamage() {
		// Make damage fade out over time
		this.currentFadeOutCoroutine = StartCoroutine(this.FadeOutDamageBackground(0, CardScript.FadeOutTime));
	}

	private IEnumerator FadeOutDamageBackground(float targetAlphaValue, float fadeOutTime) {
		Transform backgroundTransform = this.damageBackground.transform;

		// Wait a bit before fading
		yield return new WaitForSeconds(CardScript.DelayBeforeFadeTime);

        float originalBackgroundAlpha = transform.GetComponent<SpriteRenderer>().material.color.a;
        for (float t = 0.0f; t < 1.0f; t += Time.deltaTime / fadeOutTime) {
			Color newColor = new Color(1, 1, 1, Mathf.Lerp(originalBackgroundAlpha, targetAlphaValue, t));
			backgroundTransform.GetComponent<SpriteRenderer>().material.color = newColor;
			this.damageText.color = newColor;
			yield return null;
        }

		// Animation done, reset render
		this.ResetDamageIndicatorRender();
     }

	 private void ResetDamageIndicatorRender() {
		 // Renderer hidden by default
		this.damageBackground.GetComponent<Renderer>().enabled = false;
		
		// Restore original color
		this.damageBackground.transform.GetComponent<SpriteRenderer>().material.color = originalBackgroundColor;
		this.damageText.color = originalTextColor;

		// Remove text display
		this.damageText.text = "";
	 }
}
