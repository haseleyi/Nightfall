// <copyright file="GameManager.cs" company="DIS Copenhagen">
// Copyright (c) 2017 All Rights Reserved
// </copyright>
// <author>Benno Lueders</author>
// <date>14/08/2017</date>

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// In control of the game flow, pauses/unpauses the game.
/// </summary>
public class MenuManager : MonoBehaviour {

	public static MenuManager instance;
	public static GameState gameState;
	public bool running;

	public enum GameState
	{
		MainMenu,
		Running,
		Paused
	}

	UnityEngine.UI.Text pauseText;


	void Start() {
		instance = this;
		if (SceneManager.GetActiveScene().buildIndex == 0) {
			gameState = GameState.MainMenu;
			running = false;
		} else {
			Time.timeScale = 1;
			gameState = GameState.Running;
			running = true;
			pauseText = GameObject.Find("PauseText").GetComponent<UnityEngine.UI.Text>();
			if (SceneManager.GetActiveScene().buildIndex == 1) {
				SoundManager.instance.ambiance.Play ();
				SoundManager.instance.ambiance.loop = true;
			} else if (SceneManager.GetActiveScene().buildIndex == 2) {
				SoundManager.instance.endAmbiance.Play ();
				SoundManager.instance.endAmbiance.loop = true;
			}
		}
	}


	void Update() {
		if ((Input.GetKeyDown (KeyCode.Escape) || Input.GetKeyDown (KeyCode.P))
			&& gameState == GameState.Running && !FirstPersonController.instance.dying) {
			Pause ();
		} else if ((Input.GetKeyDown (KeyCode.Escape) || Input.GetKeyDown (KeyCode.P))
		    && gameState == GameState.Paused) {
			Unpause ();
		} else if (Input.GetKey(KeyCode.M) && gameState == GameState.Paused) {
			Time.timeScale = 1;
			gameState = GameState.MainMenu;
			SceneManager.LoadScene (0);
		}
		if (gameState == GameState.MainMenu) {
			Cursor.visible = true;
		} else {
			Cursor.visible = false;
		}
	}


	/// <summary>
	/// Pauses the game
	/// </summary>
	public void Pause() {
		pauseText.enabled = true;
		running = false;
		Time.timeScale = 0;
		gameState = GameState.Paused;
	}


	/// <summary>
	/// Unpauses the game
	/// </summary>
	public void Unpause() {
		pauseText.enabled = false;
		Time.timeScale = 1;
		running = true;
		gameState = GameState.Running;
	}


	/// <summary>
	/// Loads the game, called when player presses Start
	/// </summary>
	public void StartGame() {
		Time.timeScale = 1;
		gameState = GameState.Running;
		SceneManager.LoadScene (1);
	}
}
