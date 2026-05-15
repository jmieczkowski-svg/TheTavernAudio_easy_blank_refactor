using UnityEngine;
using FMODUnity;

/// <summary>
/// Zarządza odtwarzaniem dźwięków kroków, skoków i lądowania w zależności od powierzchni.
/// </summary>
public class Footsteps : MonoBehaviour
{
    // FMOD - Instancje zdarzeń.
    private FMOD.Studio.EventInstance footstepsSoundInstance;
    private FMOD.Studio.EventInstance jumpSoundInstance;
    private FMOD.Studio.EventInstance landSoundInstance;

    // Publiczne referencje do zdarzeń FMOD.
    public EventReference footstepsEvent;
    public EventReference jumpEvent;
    public EventReference landEvent;
    public EventReference breathingEvent; // <-- DODANE: Referencja do dźwięku sapania

    private float lastFootstepTime = 0f;
    private float distToGround;

    // Zmienne do obsługi sapania
    private float currentRunTimer = 0f;      // <-- DODANE
    private bool wasRunningLastFrame = false; // <-- DODANE

    [SerializeField]
    private bool isGrounded = true;
    [SerializeField]
    private bool isJumping = false;

    void Start()
    {
        distToGround = GetComponent<Collider>().bounds.extents.y;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            PlayJump();
        }
    }

    void FixedUpdate()
    {
        HandleFootsteps();
    }

    private void HandleFootsteps()
    {
        bool isMoving = (Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0);
        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        bool grounded = IsGrounded();

        // LOGIKA SAPANIA (DODANE)
        bool isCurrentlyRunning = isMoving && isRunning && grounded;
        if (isCurrentlyRunning)
        {
            currentRunTimer += Time.fixedDeltaTime;
        }
        else
        {
            if (wasRunningLastFrame && currentRunTimer >= 3f)
            {
                RuntimeManager.PlayOneShotAttached(breathingEvent, gameObject);
            }
            currentRunTimer = 0f;
        }
        wasRunningLastFrame = isCurrentlyRunning;

        // LOGIKA KROKÓW
        if (isMoving && grounded)
        {
            float footstepInterval = isRunning ? 0.25f : 0.5f;

            if (Time.time - lastFootstepTime > footstepInterval)
            {
                lastFootstepTime = Time.time;
                PlayFootsteps();
            }
        }
    }

    private void PlayFootsteps()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, distToGround + 0.5f))
        {
            string surfaceTag = hit.collider.tag;
            PlaySurfaceSound(footstepsSoundInstance, footstepsEvent, surfaceTag);
        }
    }

    private void PlayJump()
    {
        if (IsGrounded())
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, Vector3.down, out hit, distToGround + 0.5f))
            {
                string surfaceTag = hit.collider.tag;
                PlaySurfaceSound(jumpSoundInstance, jumpEvent, surfaceTag);
            }
            isGrounded = false;
            isJumping = true;
        }
    }

    private void OnCollisionEnter(Collision col)
    {
        if (!isGrounded && isJumping)
        {
            PlayLanding();
        }
    }

    private void PlayLanding()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, distToGround + 0.5f))
        {
            string surfaceTag = hit.collider.tag;
            PlaySurfaceSound(landSoundInstance, landEvent, surfaceTag);
        }
        isGrounded = true;
        isJumping = false;
    }

    private void PlaySurfaceSound(FMOD.Studio.EventInstance soundInstance, EventReference eventRef, string surfaceTag)
    {
        string surfaceParameter = null;

        switch (surfaceTag)
        {
            case "Stone":
            case "Inside_stone":
            case "Outside":
                surfaceParameter = "Stone";
                break;

            case "Wood":
            case "Inside_wood":
                surfaceParameter = "Wood";
                break;

            case "Bed":
                surfaceParameter = "Bed";
                break;
        }

        if (surfaceParameter != null)
        {
            soundInstance = RuntimeManager.CreateInstance(eventRef);
            soundInstance.set3DAttributes(RuntimeUtils.To3DAttributes(gameObject.transform));
            soundInstance.setParameterByNameWithLabel("Footsteps_surface", surfaceParameter);
            soundInstance.start();
            soundInstance.release();
        }
    }

    bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, distToGround + 0.5f);
    }
}