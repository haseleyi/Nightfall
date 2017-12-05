using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls opening and closing of doors, spawning in houses, 
/// and other house-related behavior
/// </summary>
public class House : MonoBehaviour {

	private Animator anim;
	private GameObject player;

    public int Address;

	// Positions of house corners
	Vector3 c0, c1, c2, c3;
	public GameObject[] corners;
	public AudioClip[] doorSounds;
	AudioSource audioSource;

	public bool doorOpen = false;
	public bool doorLocked = false; 
    public bool hasOpened = false;
	bool playerNear = false; // Reflects if the player in the door collider


	void Start () {
		anim = GetComponentInChildren<Animator> ();
		player = GameObject.FindGameObjectWithTag ("Player");
		audioSource = GetComponent<AudioSource>();

        // Get positions for each corner of the house's two rectangles
		c0 = corners [0].transform.position;
		c1 = corners [1].transform.position;
		c2 = corners [2].transform.position;
		c3 = corners [3].transform.position;
	}


    /// <summary>
    /// Determines whether the provided position is located inside this house
    /// </summary>
    /// <returns><c>true</c>, if position is inside, <c>false</c> otherwise.</returns>
    /// <param name="p">P.</param>
	public bool PositionInsideHouse(Vector3 p) {
		if ((((c0.x - 1 < p.x && p.x < c1.x + 1) || (c1.x - 1 < p.x && p.x < c0.x + 1)) && 
             ((c0.z - 1 < p.z && p.z < c1.z + 1) || (c1.z - 1 < p.z && p.z < c0.z + 1))) || 
            (((c2.x < p.x && p.x < c3.x) || (c3.x < p.x && p.x < c2.x)) && 
             ((c2.z < p.z && p.z < c3.z) || (c3.z < p.z && p.z < c2.z)))) {
			return true;
		}
		return false;
	}


	/// <summary>
	/// Helper method for playing singing within a house
	/// </summary>
	public void PlaySinging() {
		StartCoroutine (PlaySingingCoroutine ());
	}


	/// <summary>
	/// Loops singing within house
	/// </summary>
	/// <returns>The singing coroutine.</returns>
	IEnumerator PlaySingingCoroutine() {
		while (true) {
			GameObject songObject = this.transform.Find ("SongObject").gameObject;
			AudioSource player = songObject.GetComponent<AudioSource> ();
			player.PlayOneShot(player.clip, 3);
			yield return new WaitForSeconds (86);
		}
	}


	/// <summary>
	/// Turns the off singing in this house
	/// </summary>
	void TurnOffSinging() {
		GameObject songObject = this.transform.Find ("SongObject").gameObject;
		AudioSource player = songObject.GetComponent<AudioSource> ();
		player.Stop ();
		StopCoroutine (PlaySingingCoroutine ());
	}


	/// <summary>
	/// Open the door, update player's canvas, notify PuzzleManager
	/// </summary>
	public void OpenDoor() {
		
        // If the door is locked, reset the sequence
        if (doorLocked) {
            
			// Play locked door audio
            audioSource.clip = doorSounds[2];
            audioSource.PlayOneShot(audioSource.clip, 3);

            // Close all the doors and 
            PuzzleManager.instance.CloseAllDoors();
            doorLocked = false;

			FirstPersonController.instance.NotifyInteract ("Incorrect.", null);
            return;
        }

		audioSource.clip = doorSounds[0];
		audioSource.PlayOneShot(audioSource.clip, 3);
		anim.ResetTrigger ("CloseDoorTrigger");
		anim.SetTrigger ("OpenDoorTrigger");
		doorOpen = true;

		TurnOffSinging ();

		// If the player is still in the proximity, 
        // update their interact function to close the door
		if (playerNear) {
			player.GetComponent<FirstPersonController> ()
                  .NotifyInteract ("Click to close door.", this.CloseDoor);
		}

		// If this door has never been opened, we need to load the interior of the house
		if (!hasOpened && PuzzleManager.instance && this.tag != "FinalHouse") {
			hasOpened = true;
			// If we're in stage two, let the PM know the door of this house opened
			if (PuzzleManager.instance.getState () == PuzzleManager.States.USECOMBO) {
				PuzzleManager.instance.DoorOpened (this);
			} else {
				PuzzleManager.instance.LoadInterior (gameObject);
			}
		}
	}


	/// <summary>
	/// Close the doore, update player's canvas, notify PuzzleManager
	/// </summary>
	public void CloseDoor() {
		audioSource.clip = doorSounds [1];
		audioSource.PlayDelayed (.8f);
		anim.ResetTrigger ("OpenDoorTrigger");
		anim.SetTrigger ("CloseDoorTrigger");
		doorOpen = false;

		// If the player is still in the proximity, 
		// update their interact function to open the door
		if (playerNear) {
			player.GetComponent<FirstPersonController> ()
                  .NotifyInteract ("Click to open door.", this.OpenDoor);
		}
	}


    public void LockDoor() {
        doorLocked = true;
    }


    public void UnlockDoor() {
        doorLocked = false;
    }


	/// <summary>
	/// Removes an interior from this house
	/// </summary>
	public void RemoveInterior() {
		foreach (Transform child in this.transform) {
			if (child.CompareTag ("Interior")) {
				Destroy (child.gameObject);
			} 
		}
	}


	/// <summary>
	/// When the player enters the collider (positioned on either side of the door), 
	/// display the appropriate InteractUI depending on the door's state (open, locked, etc.)
	/// </summary>
	/// <param name="other">What the house collides with</param>
	void OnTriggerEnter(Collider other) {
		if (other.CompareTag ("Player")) {
			playerNear = true;

			FirstPersonController FPC = player.GetComponent<FirstPersonController> ();

			if (doorOpen) {
				FPC.NotifyInteract ("Click to close door.", this.CloseDoor);
            } else if (doorLocked) {
				FPC.NotifyInteract ("This door is locked. Click to Reset.", PuzzleManager.instance.CloseAllDoors);
			} else {
				FPC.NotifyInteract ("Click to open door.", this.OpenDoor);
			}
		}
	}


    /// <summary>
	/// When the player exits the collider, remove the InteractUI
    /// </summary>
    /// <param name="other">Other.</param>
	void OnTriggerExit(Collider other) {
		if (other.CompareTag ("Player")) {
			playerNear = false;
			other.gameObject.GetComponent<FirstPersonController> ()
                 .NotifyInteract ("", null);
			if (this.tag == "FinalHouse" && PuzzleManager.instance) {
				PuzzleManager.instance.InitiateUSECOMBO ();
			}
		}
	}
}
