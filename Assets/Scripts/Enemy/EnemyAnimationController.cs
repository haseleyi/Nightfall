using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Accompanies enemy's animation state machine
/// </summary>
public class EnemyAnimationController : MonoBehaviour {

	EnemyController controller;
	Animator animator;
	Camera offCam, mainCam;
	bool running;

	void Start () {
		controller = GetComponentInParent<EnemyController> ();
		animator = GetComponent<Animator> ();
		GameObject deathCamera = GameObject.Find ("DeathCamera");
		offCam = deathCamera.gameObject.GetComponent<Camera> ();
		mainCam = Camera.main;
		running = false;
	}

	void FixedUpdate() {
		if (running) {
			animator.SetFloat ("runSpeed", controller.velocity);
		}
	}

	/// <summary>
	/// Idles the enemy
	/// </summary>
	public void Idle() {
		animator.SetBool ("isRunning", false);
		running = false;
	}

	/// <summary>
	/// Enemy runs
	/// </summary>
	public void Run() {
		animator.SetBool ("isRunning", true);
		running = true;
	}

	/// <summary>
	/// Enemy kills player
	/// </summary>
	public void Kill() {
		StartCoroutine (KillCoroutine ());
	}

	/// <summary>
	/// Trigger killing animation, sounds, blackout
	/// </summary>
	/// <returns>The coroutine.</returns>
	IEnumerator KillCoroutine() {
		mainCam.enabled = true;
		offCam.enabled = false;
		animator.SetBool ("isKilling", true);
		yield return new WaitForSeconds (1.25f);
		SoundManager.instance.deathFade.Play ();
		offCam.enabled = true;
		mainCam.enabled = false;
		yield return new WaitForSeconds (2.7f);
		SceneManager.LoadScene (SceneManager.GetActiveScene().buildIndex);
	}
}