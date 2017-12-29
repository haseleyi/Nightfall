using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Controlls when the house manager should open and close doors
/// Writes things on walls
/// Spawns objects in houses
/// Manages game progression
/// </summary>
public class PuzzleManager: MonoBehaviour {

	public static PuzzleManager instance;
	public GameObject[] Houses;
	public enum States {TUTORIAL, FINDCOMBO, USECOMBO};

	States gameState; 
    int numHouses = 4;
    int numHousesOpened = 0;

    // These are addresses of the houses
	int[] DoorCombo = {13, 11, 21, 33};

	// Were the doors opened in the correct order?
	bool correctOrder = true;

	void Start () {
		instance = this;
		gameState = States.TUTORIAL;
        Houses = GameObject.FindGameObjectsWithTag("House");
		FadeController.instance.StartFadeIn (5);
	}

    
    /******************
     * Public methods *
     ******************/

	public States getState() {
		return gameState;
	}


	/// <summary>
	/// Given the number of doors (therefore of house interiors loaded), 
	/// load the appropriate interior prefab as a child of the house
	/// The prefab should contain GameObjects positioned in order
	/// to fit inside a house.
	/// </summary>
	/// <param name="house">A GameObject (preferable a house) to become 
	/// the parent of the interior</param>
    public void LoadInterior(GameObject house) {
		
		// If we're in the second stage and we just opened the last door, 
		if (gameState == States.USECOMBO && numHousesOpened == 3) {

			// Fade out
			FadeController.instance.StartFadeOut (5f);

			// Load a whole new scene
			SceneManager.LoadScene ("FinalScene");
			return;
		}

		// Path to prefab for the Nth interior, where N = numHousesOpened
		string inPath = "Prefabs/Interiors/interior" + numHousesOpened;

        // Load the prefab into a gameobject variable
        GameObject interiorPrefab = Resources.Load(inPath, typeof(GameObject)) 
                                    as GameObject;

        // Instantiate the prefab as a child of the house param, 
        // with relative positioning
        Instantiate(interiorPrefab, house.transform, false);

        numHousesOpened++;

		if (numHousesOpened == 3 && correctOrder) {
			// Play singing in the fourth unopened house
			for (int i = 0; i < numHouses; i++) {
				House h = Houses [i].GetComponent<House> ();
				if (!h.hasOpened) {
					h.PlaySinging();
				}
			}

		} else if (numHousesOpened == 4 && gameState == States.TUTORIAL) {
            InitiateFINDCOMBO ();
        }

		GameObject[] enemyObjects = GameObject.FindGameObjectsWithTag ("Enemy");
		if (enemyObjects.Length > 0) {
			Flashlight.instance.enemyForTeleporting = enemyObjects [0].GetComponent<EnemyController> ();
		}
    }


	/// <summary>
	/// Notify the manager that a door opened, so that it keeps track
	/// Note: This is only called in stage two, FINDCOMBO.
	/// </summary>
	/// <param name="h">The house.</param>
	public void DoorOpened(House h) {
        
		// If the house is the last one, close all the doors and delete interiors
		if (DoorCombo[numHousesOpened] == h.Address) {
            
			// Load <numHousesOpened>th interior
			LoadInterior(h.gameObject);

        } else {
            correctOrder = false;
			numHousesOpened++;
        }

        // Once we've opened 3 of the 4 houses, check the order.  
        if (numHousesOpened == 3) {
			for (int i = 0; i < numHouses; i++) {
				House house = Houses [i].GetComponent<House> ();
				if (!house.hasOpened) {
					if (correctOrder) {

						// Unlock fourth door, get ready to load interiorFinal
						house.UnlockDoor (); 

					} else {
						
						// Lock the fourth door
						house.LockDoor ();
					}
				}
			}
        }
    }


	/// <summary>
	/// Iterates through the houses array and closes each house's door
	/// This is called in the findcombo state if the player attempts
	/// to open the locked fourth door when they get the wrong combo
	/// </summary>
    public void CloseAllDoors(){
        for (int i = 0; i < numHouses; i++) {
			House h = Houses [i].GetComponent<House> ();
			h.hasOpened = false;
			h.doorLocked = false;
			h.CloseDoor();
			h.RemoveInterior ();
		}
		if (GameObject.FindGameObjectsWithTag ("Enemy").Length < 1) {
			// load the enemy prefab
			GameObject e = Resources.Load("Prefabs/Enemy", typeof(GameObject)) 
				as GameObject;
			// create the enemy
			Instantiate(e, new Vector3(119, 1, 121), Quaternion.identity);
			// tell the flashlight about the enemy
			Flashlight.instance.enemyForTeleporting = e.GetComponent<EnemyController> ();
		}
        // reset order information
		numHousesOpened = 0;
        correctOrder = true;
    }


	/// <summary>
	/// Allows player to proceed beyond tutorial zone
	/// </summary>
    public void InitiateFINDCOMBO() {
		reshuffle (DoorCombo);
		gameState = States.FINDCOMBO;
		GameObject[] tutfences = GameObject.FindGameObjectsWithTag("TutFence");
		for (int i = 0; i < tutfences.Length; i++) {
			Destroy (tutfences [i]);
		}
    }


	/// <summary>
	/// Initalizes the state USECOMBO
	/// </summary>
	public void InitiateUSECOMBO(){
		CloseAllDoors ();
		gameState = States.USECOMBO;
	}


	/*******************
     * private methods *
     *******************/

	/// <summary>
	/// Generates a random combination of house addreses
	/// Code credit: https://forum.unity.com/threads/randomize-array-in-c.86871/
	/// User: harvesteR
	/// </summary>
	/// <param name="DoorCombo">Our array of door combinations</param>
	private void reshuffle(int[] DoorCombo)
	{
		for (int t = 0; t < DoorCombo.Length; t++ )
		{
			int tmp = DoorCombo[t];
			int r = Random.Range(t, DoorCombo.Length);
			DoorCombo[t] = DoorCombo[r];
			DoorCombo[r] = tmp;
		}

		Sprite[] numSprites = Resources.LoadAll<Sprite> ("Sprites/3 2 1");
			
		GameObject[] displays = GameObject.FindGameObjectsWithTag ("OrderNums");
		for (int i = 0; i < displays.Length; i++) {

			// Get the 9th char of the gameobject's name
			int x = displays [i].name [9] - '0' - 1;

			// Use that to calculate which number of doorcombo should be displayed
			// We need to get the right address, and then 
			// the number char within that using string indexing
			int n = 3 - (DoorCombo [x / 2].ToString () [x % 2]) + '0';

			// Set this one to display this number -> string(DoorCombo[j / 2])[j % 2]
			// The filename will be that number - 1
			SpriteRenderer sr = displays[i].GetComponent<SpriteRenderer>();

			// Then get the correct sprite and display it
			sr.sprite = numSprites [n];
		}
	}
}
