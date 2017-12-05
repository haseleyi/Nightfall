using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FinalSceneManager : MonoBehaviour {

	GameObject finalHouse;
	House houseScript;

	void Start () {
		StartCoroutine(Fadings ());
	}


	/// <summary>
	/// Controls fade in and out for final scene
	/// </summary>
	IEnumerator	Fadings() {
		FadeController.instance.StartFadeIn (10); 
		yield return new WaitForSeconds (25);
		FadeController.instance.StartFadeOut (10);
		yield return new WaitForSeconds (5);
		SceneManager.LoadScene ("MainMenu");
	}
}
