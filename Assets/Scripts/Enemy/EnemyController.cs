using UnityEngine;
using System.Collections;
using UnityEngine.AI;

/// <summary>
/// Controls enemy movement and behavior, coordinates with EnemyAnimationController for visuals
/// </summary>
public class EnemyController : MonoBehaviour {

	// Track velocity and make public so that animator can adjust animation speed appropriately
	public float velocity;

	public AudioClip[] m_FootstepSounds;

	bool flickerCoroutineRunning, footstepCoroutineRunning, isKilling;
	enum Chasing {NOTHING, LIGHT, PLAYER};
	EnemyAnimationController animationController;
	NavMeshAgent agent;
	Rigidbody myRigidbody;
	Chasing chasing;
	AudioSource m_AudioSource;

	void Start() {
		GameObject songObject = this.transform.Find ("SongObject").gameObject;
		AudioSource player = songObject.GetComponent<AudioSource> ();
		player.Play ();
		player.loop = true;
		agent = GetComponent<NavMeshAgent> ();
		myRigidbody = GetComponent<Rigidbody> ();
		animationController = GetComponentInChildren<EnemyAnimationController> ();
		chasing = Chasing.NOTHING;
		flickerCoroutineRunning = false;
		m_AudioSource = GetComponent<AudioSource>();
//		StartCoroutine (DebugCoroutine ());
	}

	void Update() {
		velocity = agent.velocity.magnitude;
		if (LocationManager.instance && LocationManager.instance.PlayerIsSafe()) {
			agent.isStopped = true;
			myRigidbody.constraints = RigidbodyConstraints.FreezeAll;
			animationController.Idle ();
			chasing = Chasing.NOTHING;
			return;
		}
		AdjustSpeed ();
		PlayFootsteps ();
		ChaseIfLightOn ();
		StopAtGoal ();
		KeepUpWithPlayer ();
		ChaseIfLookingDownAndClose ();
	}

	/// <summary>
	/// All-purpose helper method for debugging enemy behavior
	/// </summary>
	/// <returns>The coroutine.</returns>
	IEnumerator DebugCoroutine() {
		while (true) {
			print ("===== STATUS =====");
			print ("My pos: " + transform.position.ToString ());
			print ("My dest: " + agent.destination.ToString ());
			print ("Distance between: " + Vector3.Distance (transform.position, agent.destination).ToString());
			print ("Chasing: " + chasing.ToString());
			yield return new WaitForSeconds (.1f);
		}
	}

	/// <summary>
	/// Checks flashlight status
	/// </summary>
	/// <returns><c>true</c>, if <<<it's lit>>>, <c>false</c> otherwise.</returns>
	bool LightOn() {
		return FirstPersonController.instance.m_Flashlight.Status ();
	}

	/// <summary>
	/// Plays footsteps if we're on the move
	/// </summary>
	void PlayFootsteps() {
		if (!footstepCoroutineRunning && chasing != Chasing.NOTHING && (1 / agent.velocity.magnitude) < .5f) {
			StartCoroutine (FootstepCoroutine ());
			footstepCoroutineRunning = true;
		}
	}

	/// <summary>
	/// Regulates speed at which we play footsteps based on agent's velocity
	/// </summary>
	/// <returns>The coroutine.</returns>
	IEnumerator FootstepCoroutine() {
		while (true) {
			if (!isKilling) {
				// Pick & play a random footstep sound from the array
				// Excludes sound at index 0
				// Code credit: Unity standard assets first person controller
				int n = Random.Range(1, m_FootstepSounds.Length);
				m_AudioSource.clip = m_FootstepSounds[n];
				m_AudioSource.PlayOneShot(m_AudioSource.clip, 3);
				// Move picked sound to index 0 so it's not picked next time
				m_FootstepSounds[n] = m_FootstepSounds[0];
				m_FootstepSounds[0] = m_AudioSource.clip;
			}
			if (agent.velocity.magnitude == 0) {
				footstepCoroutineRunning = false;
				break;
			}
			float waitTime = 1f / agent.velocity.magnitude;
			yield return new WaitForSeconds (waitTime);
		}
	}

	/// <summary>
	/// Enemy chases faster if chasing player
	/// </summary>
	void AdjustSpeed() {
		if (chasing == Chasing.PLAYER) {
			agent.speed = 4;
		} else {
			agent.speed = 3;
		}
	}

	/// <summary>
	/// If the light is on, chase the light
	/// </summary>
	void ChaseIfLightOn() {
		if (LightOn() && chasing != Chasing.PLAYER) {
			chasing = Chasing.LIGHT;
			agent.isStopped = false;
			myRigidbody.constraints = RigidbodyConstraints.None;
			animationController.Run();
			Vector3 newGoal = FirstPersonController.instance.GetLightLocation ();
			if (newGoal != new Vector3 (0, 0, 0)) {
				NavMeshHit hit;
				NavMesh.SamplePosition (newGoal, out hit, 10, 1);
				agent.destination = hit.position;
			}
		}
	}

	/// <summary>
	/// If enemy reaches a goal, stop chasing and start idling
	/// </summary>
	void StopAtGoal() {
		// Through trial and error, 1.1m seems a good threshold for a goal state if multiple agents are crowding around
		if ((!LightOn() && Vector3.Distance(transform.position, agent.destination) < 1.1f)) {
			agent.isStopped = true;
			myRigidbody.constraints = RigidbodyConstraints.FreezeAll;
			animationController.Idle ();
			chasing = Chasing.NOTHING;
		}
	}

	/// <summary>
	/// Warps enemy into player's vicinity
	/// This runs if distance between player and enemy gets too large
	/// e.g. because player trapped enemy in a room and it's been a while
	/// </summary>
	void KeepUpWithPlayer() {
		if (Vector3.Distance (transform.position, FirstPersonController.instance.gameObject.transform.position) > 30
			// We don't want this to run if the player somehow falls off the map, because SamplePosition will crash Unity
			&& FirstPersonController.instance.transform.position.y > -5) {
			Vector3 spawnDirection = Camera.main.transform.forward.normalized;
			spawnDirection = Quaternion.AngleAxis(Random.Range(95, 265), Vector3.up) * spawnDirection;
			NavMeshHit hit;
			NavMesh.SamplePosition (FirstPersonController.instance.transform.position + 10 * spawnDirection, out hit, 10, 1);
			agent.Warp (hit.position);
		}
	}

	/// <summary>
	/// Teleports immediately ahead of player 
	/// This is called every so often when the light is activated to keep you on your toes
	/// </summary>
	public void TeleportAhead() {
		Vector3 newLocation = FirstPersonController.instance.GetLightLocation ();
		NavMeshHit hit;
		NavMesh.SamplePosition (newLocation, out hit, 10, 1);
		if (agent) {
			agent.Warp (hit.position);
			StartCoroutine (ATtaC ());
		}
	}
		
	/// <summary>
	/// FPC uses this to alert enemies that they're hit by its raycasts
	/// </summary>
	public void HitByLight() {
		if (!LocationManager.instance.PlayerIsSafe()) {
			StartCoroutine (ATtaC ());
		} else {
			// Stop and stare. One Republic.
			agent.isStopped = true;
			myRigidbody.constraints = RigidbodyConstraints.FreezeAll;
			animationController.Idle ();
			chasing = Chasing.NOTHING;
		}
	}

	/// <summary>
	/// If player is looking down at the ground with their flashlight,
	/// spare them of their boring gameplay
	/// </summary>
	void ChaseIfLookingDownAndClose() {
		if (!LightOn() || isKilling) {
			return;
		}
		RaycastHit hit;
		Ray ray = new Ray (Camera.main.transform.position, Camera.main.transform.forward);
		Debug.DrawRay (Camera.main.transform.position, Camera.main.transform.forward * 2);
		if (Physics.Raycast(ray, out hit, 2, FirstPersonController.instance.m_LightRaycast)) {
			if (Vector3.Distance(transform.position, hit.point) < 1.5f) {
				StartCoroutine (ATtaC ());
			}
		}
	}

	/// <summary>
	/// Send enemy at player, called when enemy is caught in light
	/// </summary>
	/// <returns>The coroutine.</returns>
	IEnumerator ATtaC() {

		// Deer in headlights, stare at player for a second or so
		if (chasing != Chasing.PLAYER) {
			animationController.Idle ();
			myRigidbody.constraints = RigidbodyConstraints.FreezeAll;
			agent.velocity = new Vector3(0, 0, 0);
			agent.destination = transform.position;
			chasing = Chasing.PLAYER;
		}
		float time = 0f;
		while (time < Random.value + .5f) {
			transform.LookAt (FirstPersonController.instance.gameObject.transform);
			time += Time.deltaTime;
			yield return null;
		}

		// Chase player, track position while light is on
		animationController.Run();
		agent.isStopped = false;
		myRigidbody.constraints = RigidbodyConstraints.None;
		agent.destination = FirstPersonController.instance.transform.position;
		chasing = Chasing.PLAYER;
		while (LightOn()) {
			agent.destination = FirstPersonController.instance.transform.position;
			yield return null;
		}
	}

	/// <summary>
	/// If we're chasing the player and we collide with them, kill the player
	/// </summary>
	/// <param name="other">The other object we collide with</param>
	void OnCollisionEnter(Collision other) {
		if (chasing == Chasing.PLAYER && other.gameObject.CompareTag("Player")) {
			Kill ();
		} else if (other.gameObject.CompareTag("Player")) {
			// If the enemy is pathfinding and hits the player, it gets super confused
			// As a workaround, we'll respawn the enemy somewhere behind the player
			Vector3 spawnDirection = Camera.main.transform.forward.normalized;
			spawnDirection = Quaternion.AngleAxis(Random.Range(95, 265), Vector3.up) * spawnDirection;
			NavMeshHit hit;
			NavMesh.SamplePosition (FirstPersonController.instance.transform.position + 10 * spawnDirection, out hit, 10, 1);
			agent.Warp (hit.position);
		}
	}

	/// <summary>
	/// Coordinates player death
	/// </summary>
	void Kill() {

		isKilling = true;

		GameObject songObject = this.transform.Find ("SongObject").gameObject;
		AudioSource player = songObject.GetComponent<AudioSource> ();
		player.Stop ();

		if (!flickerCoroutineRunning) {

			FirstPersonController.instance.NotifyInteract ("I don't think it likes that flashlight...", null);

			StartCoroutine (PleaseGodStopMovingSomethingHasToWork ());
			animationController.Kill ();
			SoundManager.instance.DeathSounds ();

			// Freeze player, turn toward enemy
			FirstPersonController.instance.GetComponent<BoxCollider> ().enabled = false;
			FirstPersonController.instance.dying = true;
			Camera.main.transform.LookAt (transform.position + new Vector3(0, .2f, 0));
			Flashlight.instance.transform.LookAt (transform.position);
			StartCoroutine (FlickerCoroutine ());
		}
	}

	/// <summary>
	/// Freezes enemy for death animation
	/// </summary>
	/// <returns>The coroutine.</returns>
	IEnumerator PleaseGodStopMovingSomethingHasToWork() {
		while (true) {
			agent.isStopped = true;
			agent.velocity = new Vector3(0, 0, 0);
			myRigidbody.constraints = RigidbodyConstraints.FreezeAll;
			agent.destination = transform.position;
			GetComponent<CapsuleCollider> ().enabled = false;
			myRigidbody.isKinematic = true;
			transform.LookAt (FirstPersonController.instance.transform.position);
			yield return null;
		}
	}

	/// <summary>
	/// Flickers flashlight for cooler death
	/// </summary>
	/// <returns>The coroutine.</returns>
	IEnumerator FlickerCoroutine() {
		if (!flickerCoroutineRunning) {
			flickerCoroutineRunning = true;
			if (!LightOn()) {
				Flashlight.instance.Toggle ();
			}
			yield return new WaitForSeconds (.45f);
			Flashlight.instance.Toggle ();
			yield return new WaitForSeconds (.25f);
			Flashlight.instance.Toggle ();
			yield return new WaitForSeconds (.05f);
			Flashlight.instance.Toggle ();
			yield return new WaitForSeconds (.05f);
			Flashlight.instance.Toggle ();
		}
	}
}