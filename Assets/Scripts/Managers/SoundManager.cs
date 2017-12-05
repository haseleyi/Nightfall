using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton that provides access to all sounds for other scripts
/// </summary>
public class SoundManager : MonoBehaviour {

	public AudioSource doorOpen, doorClose, flashlightOn, flashlightOff,
		death1, death2, death3, scream, deathFade, ambiance, endAmbiance;

	public static SoundManager instance;

	void Awake () {
		instance = this;
		AudioSource[] sounds = GetComponents<AudioSource> ();
		doorOpen = sounds [0];
		doorClose = sounds [1];
		flashlightOn = sounds [2];
		flashlightOff = sounds [3];
		death1 = sounds [4];
		death2 = sounds [5];
		death3 = sounds [6];
		scream = sounds [7];
		deathFade = sounds [8];
		ambiance = sounds [9];
		endAmbiance = sounds [10];
	}


	/// <summary>
	/// Plays a random death sound
	/// </summary>
	public void DeathSounds() {
		float r = Random.value;
		if (r < .33) {
			death1.Play ();
		} else if (r < .66) {
			death2.Play ();
		} else {
			death3.Play ();
		}	
	}
}