using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Allows enemy to determine whether player is in a safe zone
/// </summary>
public class LocationManager : MonoBehaviour {

	public static LocationManager instance;

	void Start() {
		instance = this;
	}

	/// <summary>
	/// Determines whether player is alone in a shack with the door closed
	/// This gets called on two occassions:
	/// 1. When hit by light and determining whether to chase player
	/// 2. When flashlight wants to teleport an enemy in front of player
	/// </summary>
	/// <returns><c>true</c>, if is safe was playered, <c>false</c> otherwise.</returns>
	public bool PlayerIsSafe() {
		GameObject[] houseObjects = PuzzleManager.instance.Houses;
		foreach (GameObject houseObject in houseObjects) {
			House house = houseObject.GetComponent<House> ();
			if (house.PositionInsideHouse(FirstPersonController.instance.transform.position) && !house.doorOpen) {
				GameObject[] enemyObjects = GameObject.FindGameObjectsWithTag ("Enemy");
				foreach (GameObject enemyObject in enemyObjects) {
					EnemyController enemyController;
					try {
						enemyController = enemyObject.GetComponent<EnemyController>();
						if (house.PositionInsideHouse(enemyController.transform.position)) {
							return false;
						}
					} catch {
						continue;
					}
				}
				return true;
			}
		}
		return false;
	}
}
