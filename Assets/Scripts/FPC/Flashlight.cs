using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controller for the player's flashlight
/// </summary>
public class Flashlight : MonoBehaviour {

	public static Flashlight instance;
	public EnemyController enemyForTeleporting;

	Light fl;
	bool turningOn, turningOff;

	void Start () {
		instance = this;
		fl = GetComponent<Light> ();
	}


	/// <summary>
	/// Returns the status of the flashlight
	/// </summary>
	public bool Status () {
		return fl.enabled;
	}


	/// <summary>
	/// Toggle's the flashlight's status
	/// </summary>
	public void Toggle () {
		if (!fl.enabled && !turningOn) {
			StartCoroutine (FlashlightOnCoroutine());
		} else if (fl.enabled && !turningOff) {
			StartCoroutine (FlashlightOffCoroutine());
		}
	}

	/// <summary>
	/// Turns the flashlight on
	/// 6% chance to teleport the enemy directly ahead
	/// </summary>
	/// <returns>The coroutine.</returns>
	IEnumerator FlashlightOnCoroutine() {
		turningOn = true;
		SoundManager.instance.flashlightOn.Play ();
		yield return new WaitForSeconds (.35f);
		// Keep the player on their toes and prevent flashlight flashing as a strategy
		if (!FirstPersonController.instance.dying && Random.value > .93f 
			&& enemyForTeleporting && !LocationManager.instance.PlayerIsSafe()) {
			enemyForTeleporting.TeleportAhead ();
		}
		fl.enabled = !fl.enabled;
		turningOn = false;
	}


	/// <summary>
	/// Turns off the flashlight
	/// </summary>
	/// <returns>The coroutine.</returns>
	IEnumerator FlashlightOffCoroutine() {
		turningOff = true;
		SoundManager.instance.flashlightOff.Play ();
		yield return new WaitForSeconds (.35f);
		fl.enabled = !fl.enabled;
		turningOff = false;
	}
}
