using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Source:http://wiki.unity3d.com/index.php?title=FadeInOut
/// This link contains all code below, which is used to fade in and out for transitions
/// </summary>
public class FadeController : MonoBehaviour {

	public static FadeController instance;

	private GUIStyle m_BackgroundStyle = new GUIStyle();		// Style for background tiling
	private Texture2D m_FadeTexture;				// 1x1 pixel texture used for fading
	private Color m_CurrentScreenOverlayColor = new Color(0,0,0,0);	// Default starting color: black and fully transparrent
	private Color m_TargetScreenOverlayColor = new Color(0,0,0,0);	// Default target color: black and fully transparrent
	private Color m_DeltaColor = new Color(0,0,0,0);		// The delta-color is basically the "speed / second" at which the current color should change
	private int m_FadeGUIDepth = -1000;				// Make sure this texture is drawn on top of everything


	// Initialize the texture, background-style and initial color
	private void Awake()
	{		
		instance = this;
		m_FadeTexture = new Texture2D(1, 1);        
		m_BackgroundStyle.normal.background = m_FadeTexture;
		SetScreenOverlayColor(m_CurrentScreenOverlayColor);

		// TEMP:
		// usage: use "SetScreenOverlayColor" to set the initial color, then use "StartFade" to set the desired color & fade duration and start the fade
		// StartFadeIn(10);
	}


	// Draw the texture and perform the fade:
	private void OnGUI()
	{   
		// If the current color of the screen is not equal to the desired color: keep fading!
		if (m_CurrentScreenOverlayColor != m_TargetScreenOverlayColor)
		{			
			// If the difference between the current alpha and the desired alpha is smaller than delta-alpha * deltaTime, then we're pretty much done fading:
			if (Mathf.Abs(m_CurrentScreenOverlayColor.a - m_TargetScreenOverlayColor.a) < Mathf.Abs(m_DeltaColor.a) * Time.deltaTime)
			{
				m_CurrentScreenOverlayColor = m_TargetScreenOverlayColor;
				SetScreenOverlayColor(m_CurrentScreenOverlayColor);
				m_DeltaColor = new Color(0,0,0,0);
			}
			else
			{
				// Fade!
				SetScreenOverlayColor(m_CurrentScreenOverlayColor + m_DeltaColor * Time.deltaTime);
			}
		}

		// Only draw the texture when the alpha value is greater than 0:
		if (m_CurrentScreenOverlayColor.a > 0)
		{			
			GUI.depth = m_FadeGUIDepth;
			GUI.Label(new Rect(-10, -10, Screen.width + 10, Screen.height + 10), m_FadeTexture, m_BackgroundStyle);
		}
	}


	// Instantly set the current color of the screen-texture to "newScreenOverlayColor"
	// Can be usefull if you want to start a scene fully black and then fade to opague
	public void SetScreenOverlayColor(Color newScreenOverlayColor)
	{
		m_CurrentScreenOverlayColor = newScreenOverlayColor;
		m_FadeTexture.SetPixel(0, 0, m_CurrentScreenOverlayColor);
		m_FadeTexture.Apply();
	}


	// Initiate a fade from the current screen color (set using "SetScreenOverlayColor") towards "newScreenOverlayColor" taking "fadeDuration" seconds
	public void StartFadeOut(float fadeDuration)
	{
		Color newScreenOverlayColor = new Color (0, 0, 0, 1);

		if (fadeDuration <= 0.0f)		// Can't have a fade last -2455.05 seconds!
		{
			SetScreenOverlayColor(newScreenOverlayColor);
		}
		else					// Initiate the fade: set the target-color and the delta-color
		{
			m_TargetScreenOverlayColor = newScreenOverlayColor;
			m_DeltaColor = (m_TargetScreenOverlayColor - m_CurrentScreenOverlayColor) / fadeDuration;
		}
	}

	public void StartFadeIn(float fadeDuration)
	{
		Color newScreenOverlayColor = new Color (0, 0, 0, 0);
		SetScreenOverlayColor(new Color(0,0,0,1));

		if (fadeDuration <= 0.0f)		// Can't have a fade last -2455.05 seconds!
		{
			SetScreenOverlayColor(newScreenOverlayColor);
		}
		else					// Initiate the fade: set the target-color and the delta-color
		{
			m_TargetScreenOverlayColor = newScreenOverlayColor;
			m_DeltaColor = (m_TargetScreenOverlayColor - m_CurrentScreenOverlayColor) / fadeDuration;
		}
	}
}
