// Unity's standard-asset first person controller, with modifications

using System;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Utility;
using Random = UnityEngine.Random;
using UnityStandardAssets.Characters.FirstPerson;

[RequireComponent(typeof (CharacterController))]
[RequireComponent(typeof (AudioSource))]
public class FirstPersonController : MonoBehaviour
{
	[SerializeField] public Flashlight m_Flashlight;
	[SerializeField] public LayerMask m_LightRaycast;
    [SerializeField] private bool m_IsWalking;
	[SerializeField] public float m_WalkSpeed;
	[SerializeField] public float m_RunSpeed;
	[SerializeField] [Range(0f, 1f)] public float m_RunstepLenghten;
	[SerializeField] public float m_StickToGroundForce;
	[SerializeField] public float m_GravityMultiplier;
	[SerializeField] public MouseLook m_MouseLook;
	[SerializeField] public bool m_UseFovKick;
    [SerializeField] private FOVKick m_FovKick = new FOVKick();
	[SerializeField] public bool m_UseHeadBob;
    [SerializeField] private CurveControlledBob m_HeadBob = new CurveControlledBob();
    [SerializeField] private LerpControlledBob m_JumpBob = new LerpControlledBob();
	[SerializeField] public float m_StepInterval;
	[SerializeField] public AudioClip[] m_FootstepSounds;    // an array of footstep sounds that will be randomly selected from.

    private Camera m_Camera;
    private bool m_Jump;
    private float m_YRotation;
    private Vector2 m_Input;
    private Vector3 m_MoveDir = Vector3.zero;
    private CharacterController m_CharacterController;
    private CollisionFlags m_CollisionFlags;
    private bool m_PreviouslyGrounded;
    private Vector3 m_OriginalCameraPosition;
    private Vector3 m_OriginalFlashlightPosition;
    private float m_StepCycle;
    private float m_NextStep;
    private AudioSource m_AudioSource;
	private MenuManager m_menuManager;

	public static FirstPersonController instance;
	public bool dying;
    public bool hasKey = false;

	// Delegate definitions, used to pass methods for interaction
	public delegate void MyDelegate();
	public MyDelegate clickHandler;

	// Interaction ui, bottom left displaying "Click to <Interaction>"
	private UnityEngine.UI.Text InteractUI;


    private void Start()
    {
        m_CharacterController = GetComponent<CharacterController>();
        m_Camera = Camera.main;
		Camera.main.enabled = true;
        m_OriginalCameraPosition = m_Camera.transform.localPosition;
        m_OriginalFlashlightPosition = m_Flashlight.transform.localPosition;
        m_FovKick.Setup(m_Camera);
        m_HeadBob.Setup(m_Camera, m_StepInterval);
        m_StepCycle = 0f;
        m_NextStep = m_StepCycle/2f;
        m_AudioSource = GetComponent<AudioSource>();
		m_MouseLook.Init(transform , m_Camera.transform, m_Flashlight.transform);

		instance = this;
		dying = false;

		clickHandler = null;
		InteractUI = GameObject.FindGameObjectWithTag("InteractUI").GetComponent<UnityEngine.UI.Text>();
    }


    private void Update()
    {
		if (dying || !MenuManager.instance || !MenuManager.instance.running) {
			return;
		}
		LightRaycasts ();
		RotateView();
		// The jump state needs to read here to make sure it is not missed
		if (!m_Jump)
		{
			m_Jump = CrossPlatformInputManager.GetButtonDown("Jump");
		}
		if (CrossPlatformInputManager.GetButtonDown ("Jump") || Input.GetKeyDown (KeyCode.Mouse1)) {
			m_Flashlight.GetComponent<Flashlight>().Toggle ();
		}

		// If the player clicked and there's a function in clickHandler, run it and reset the delegate
		if (Input.GetMouseButtonDown(0) && clickHandler != null) {
			clickHandler ();
		}
    }


	/// <summary>
	/// Determines whether the enemy is hit by the player's flashlight
	/// </summary>
	/// <returns><c>true</c> if this instance is visible to the main camera; otherwise, <c>false</c>.</returns>
	public void LightRaycasts() {
		if (!m_Flashlight.Status ()) {
			return;
		}
		Vector3 firstRayInRow = Camera.main.transform.forward.normalized;
		firstRayInRow = Quaternion.AngleAxis (-30, Vector3.up) * firstRayInRow;
		firstRayInRow = Quaternion.AngleAxis (-30, Vector3.right) * firstRayInRow;
		// From top row to bottom row
		for (int angle1 = -90; angle1 <= 90; angle1 += 5) {
			Vector3 direction = firstRayInRow;
			// From far left column to far right column
			for (int angle2 = -20; angle2 <= 20; angle2 += 5) {
				RaycastHit hit;
				// To move the point of origin ahead of player, modify the constant in the product here
				Ray ray = new Ray (Camera.main.transform.position + Camera.main.transform.forward * 1, direction);
				if (Physics.Raycast (ray, out hit, 30, m_LightRaycast.value)) {
					if (hit.collider.gameObject.CompareTag("Enemy")) {
						hit.collider.gameObject.GetComponent<EnemyController> ().HitByLight ();
						return;
					}
				}
				// Rotate ray 5 degrees right
				direction = Quaternion.AngleAxis (5, Vector3.up) * direction;
			}
			// Rotate firstRayInRow 5 degrees down
			firstRayInRow = Quaternion.AngleAxis(5, Vector3.right) * firstRayInRow;
		}
	}
		

	/// <summary>
	/// Gets the light location
	/// This is used by the enemies to pathfind to a location within the light beam
	/// </summary>
	/// <returns>The location nearest to where the light hits the environment</returns>
	public Vector3 GetLightLocation() {
		// Raycast in direction of flashlight for 10 meters, 
		// Get location of first collision or maximum distance
		Ray ray = new Ray (m_Flashlight.transform.position, m_Flashlight.transform.forward);
		RaycastHit hit;
		if (!Physics.Raycast (ray, out hit, 10, m_LightRaycast.value)) {
			Vector3 upVector = Camera.main.transform.position + 10 * Camera.main.transform.forward.normalized;
			return upVector;
		}
		return hit.point;
	}


    private void FixedUpdate()
    {
		if (!dying) {
			float speed;
			GetInput(out speed);
			// Always move along the camera forward as it is the direction that it being aimed at
			Vector3 desiredMove = transform.forward*m_Input.y + transform.right*m_Input.x;

			// Get a normal for the surface that is being touched to move along it
			RaycastHit hitInfo;
			Physics.SphereCast(transform.position, m_CharacterController.radius, Vector3.down, out hitInfo,
				m_CharacterController.height/2f, Physics.AllLayers, QueryTriggerInteraction.Ignore);
			desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;

			m_MoveDir.x = desiredMove.x*speed;
			m_MoveDir.z = desiredMove.z*speed;


			if (m_CharacterController.isGrounded)
			{
				m_MoveDir.y = -m_StickToGroundForce;
			}
			else
			{
				m_MoveDir += Physics.gravity*m_GravityMultiplier*Time.fixedDeltaTime;
			}
			m_CollisionFlags = m_CharacterController.Move(m_MoveDir*Time.fixedDeltaTime);

			ProgressStepCycle(speed);
			UpdateCameraPosition(speed);
			UpdateFlashlightPosition(speed);

			m_MouseLook.UpdateCursorLock();
		} 
    }


	/// <summary>
	/// Regulates player's step cycle
	/// </summary>
	/// <param name="speed">Speed.</param>
    private void ProgressStepCycle(float speed)
    {
        if (m_CharacterController.velocity.sqrMagnitude > 0 && (m_Input.x != 0 || m_Input.y != 0))
        {
            m_StepCycle += (m_CharacterController.velocity.magnitude + (3*speed*(m_IsWalking ? 1f : m_RunstepLenghten)))*
                         Time.fixedDeltaTime;
        }

        if (!(m_StepCycle > m_NextStep))
        {
            return;
        }

        m_NextStep = m_StepCycle + m_StepInterval;

        PlayFootStepAudio();
    }


	/// <summary>
	/// Regulates player's footstep audio
	/// </summary>
    private void PlayFootStepAudio()
    {
        if (!m_CharacterController.isGrounded)
        {
            return;
        }
        // Pick & play a random footstep sound from the array,
        // Excluding sound at index 0
        int n = Random.Range(1, m_FootstepSounds.Length);
        m_AudioSource.clip = m_FootstepSounds[n];
        m_AudioSource.PlayOneShot(m_AudioSource.clip, 1.5f);
        // Move picked sound to index 0 so it's not picked next time
        m_FootstepSounds[n] = m_FootstepSounds[0];
        m_FootstepSounds[0] = m_AudioSource.clip;
    }


	/// <summary>
	/// Updates the camera position
	/// </summary>
	/// <param name="speed">Speed.</param>
    private void UpdateCameraPosition(float speed)
    {
        Vector3 newCameraPosition;
        if (!m_UseHeadBob)
        {
            return;
        }
        if (m_CharacterController.velocity.magnitude > 0 && m_CharacterController.isGrounded)
        {
            m_Camera.transform.localPosition =
                m_HeadBob.DoHeadBob(m_CharacterController.velocity.magnitude +
                                  (speed*(m_IsWalking ? 1f : m_RunstepLenghten)));
            newCameraPosition = m_Camera.transform.localPosition;
            newCameraPosition.y = m_Camera.transform.localPosition.y - m_JumpBob.Offset();
        }
        else
        {
            newCameraPosition = m_Camera.transform.localPosition;
            newCameraPosition.y = m_OriginalCameraPosition.y - m_JumpBob.Offset();
        }
        m_Camera.transform.localPosition = newCameraPosition;
    }


	/// <summary>
	/// Updates the flashlight position
	/// </summary>
	/// <param name="speed">Speed.</param>
    private void UpdateFlashlightPosition(float speed)
    {
        Vector3 newFlashlightPosition;
        if (!m_UseHeadBob)
        {
            return;
        }
        if (m_CharacterController.velocity.magnitude > 0 && m_CharacterController.isGrounded)
        {
            m_Flashlight.transform.localPosition =
                m_HeadBob.DoHeadBob(m_CharacterController.velocity.magnitude +
                                  (speed*(m_IsWalking ? 1f : m_RunstepLenghten)));
            newFlashlightPosition = m_Flashlight.transform.localPosition;
            newFlashlightPosition.y = m_Flashlight.transform.localPosition.y - m_JumpBob.Offset();
        }
        else
        {
            newFlashlightPosition = m_Flashlight.transform.localPosition;
            newFlashlightPosition.y = m_OriginalFlashlightPosition.y - m_JumpBob.Offset();
        }
        m_Flashlight.transform.localPosition = newFlashlightPosition;
    }


	// Thanks Unity
    private void GetInput(out float speed)
    {
        // Read input
        float horizontal = CrossPlatformInputManager.GetAxis("Horizontal");
        float vertical = CrossPlatformInputManager.GetAxis("Vertical");

        bool waswalking = m_IsWalking;

#if !MOBILE_INPUT
        // On standalone builds, walk/run speed is modified by a key press
        // Keep track of whether or not the character is walking or running
        m_IsWalking = !Input.GetKey(KeyCode.LeftShift);
#endif
        // Set the desired speed to be walking or running
		speed = m_IsWalking ? m_WalkSpeed : m_WalkSpeed;
        m_Input = new Vector2(horizontal, vertical);

        // Normalize input if it exceeds 1 in combined length:
        if (m_Input.sqrMagnitude > 1)
        {
            m_Input.Normalize();
        }

        // Handle speed change to give an fov kick
        // Only if the player is going to a run, is running and the fovkick is to be used
        if (m_IsWalking != waswalking && m_UseFovKick && m_CharacterController.velocity.sqrMagnitude > 0)
        {
            StopAllCoroutines();
            StartCoroutine(!m_IsWalking ? m_FovKick.FOVKickUp() : m_FovKick.FOVKickDown());
        }
    }


	/// <summary>
	/// Rotates the view
	/// </summary>
    private void RotateView()
    {
        m_MouseLook.LookRotation (transform, m_Camera.transform, m_Flashlight.transform);
    }


	// Thanks Unity
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody body = hit.collider.attachedRigidbody;
        // Don't move the rigidbody if the character is on top of it
        if (m_CollisionFlags == CollisionFlags.Below)
        {
            return;
        }

        if (body == null || body.isKinematic)
        {
            return;
        }
        body.AddForceAtPosition(m_CharacterController.velocity*0.1f, hit.point, ForceMode.Impulse);
    }

    /******************
     * Public Methods *
     ******************/


	/// <summary>
	/// Updates the canvas text with instructions for player
	/// </summary>
	/// <param name="uitext">Uitext.</param>
	/// <param name="interactMethod">Interact method.</param>
	public void NotifyInteract(string uitext, MyDelegate interactMethod) {
		// On click, run delegate method
		clickHandler = interactMethod;

		// Display uitext
        InteractUI.text = uitext;
	}
}
