using UnityEngine;
using FMODUnity;

public class Footsteps : MonoBehaviour
{
    [Header("FMOD Footstep Events")]
    public EventReference stoneFootstepsEvent;
    public EventReference woodFootstepsEvent;
    public EventReference stairsFootstepsEvent;

    [Header("Default Movement Events")]
    public EventReference jumpEvent;
    public EventReference landEvent;
    public EventReference breathingEvent;

    private FMOD.Studio.EventInstance breathingSoundInstance;
    private bool isBreathingPlaying = false;

    private float lastFootstepTime = 0f;
    private float distToGround;
    private float currentRunTimer = 0f;

    // Bezpieczniki i stabilizatory lotu
    private float nextJumpAllowedTime = 0f;
    private float airTime = 0f;
    private bool wasGroundedLastFrame = true;

    private bool isMoving;
    private bool isRunning;
    private bool jumpRequested;
    private bool isGrounded;
    private bool isJumping = false;

    void Start()
    {
        distToGround = GetComponent<Collider>().bounds.extents.y;
    }

    void Update()
    {
        isMoving = (Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0);
        isRunning = Input.GetKey(KeyCode.LeftShift);

        // Zarejestruj skok TYLKO jeśli wciśnięto spację i minął cooldown (pół sekundy)
        if (Input.GetKeyDown(KeyCode.Space) && Time.time >= nextJumpAllowedTime)
        {
            jumpRequested = true;
            nextJumpAllowedTime = Time.time + 0.5f;
        }

        HandleBreathingLogic();
    }

    void FixedUpdate()
    {
        isGrounded = CheckIfGrounded();

        // Liczymy czas spędzony w powietrzu
        if (!isGrounded)
        {
            airTime += Time.fixedDeltaTime;
        }

        // Obsługa wybijania się
        if (jumpRequested)
        {
            if (isGrounded)
            {
                PlayJump();
            }
            jumpRequested = false;
        }

        // Obsługa lądowania
        if (isGrounded && !wasGroundedLastFrame)
        {
            if (airTime > 0.2f && isJumping)
            {
                PlayLanding();
            }

            isJumping = false;
            airTime = 0f;
        }

        HandleFootsteps();

        wasGroundedLastFrame = isGrounded;
    }

    private void HandleBreathingLogic()
    {
        bool isCurrentlyRunning = isMoving && isRunning && isGrounded;

        if (isCurrentlyRunning)
        {
            currentRunTimer += Time.deltaTime;
            if (currentRunTimer >= 5f && !isBreathingPlaying)
            {
                breathingSoundInstance = RuntimeManager.CreateInstance(breathingEvent);
                RuntimeManager.AttachInstanceToGameObject(breathingSoundInstance, transform, GetComponent<Rigidbody>());
                breathingSoundInstance.start();
                isBreathingPlaying = true;
            }
        }
        else
        {
            if (isBreathingPlaying)
            {
                breathingSoundInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                breathingSoundInstance.release();
                isBreathingPlaying = false;
            }
            currentRunTimer = 0f;
        }
    }

    private void HandleFootsteps()
    {
        if (isMoving && isGrounded && !isJumping)
        {
            float footstepInterval = isRunning ? 0.25f : 0.5f;

            if (Time.time - lastFootstepTime > footstepInterval)
            {
                lastFootstepTime = Time.time;
                PlayFootstepBySurface();
            }
        }
    }

    private void PlayJump()
    {
        isJumping = true;

        if (!jumpEvent.IsNull)
        {
            RuntimeManager.PlayOneShotAttached(jumpEvent, gameObject);
        }
    }

    private void PlayLanding()
    {
        if (!landEvent.IsNull)
        {
            RuntimeManager.PlayOneShotAttached(landEvent, gameObject);
        }

        // TWARDY RESET KROKÓW po lądowaniu, żeby uniknąć nakładania się dźwięku
        lastFootstepTime = Time.time;
    }

    private bool GetSurfaceTag(out string surfaceTag)
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, distToGround + 0.6f))
        {
            // Brak .ToLower() - tagi czytane są z uwzględnieniem wielkości liter
            surfaceTag = hit.collider.tag;
            return true;
        }
        surfaceTag = "Default";
        return false;
    }

    private bool CheckIfGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, distToGround + 0.4f);
    }

    private void PlayFootstepBySurface()
    {
        string surfaceTag;
        if (!GetSurfaceTag(out surfaceTag)) return;

        EventReference eventToPlay;

        switch (surfaceTag)
        {
            case "Stone":
            case "Inside_stone":
            case "Outside":
                eventToPlay = stoneFootstepsEvent;
                break;
            case "Wood":
            case "Inside_wood":
                eventToPlay = woodFootstepsEvent;
                break;
            case "Stairs":
                eventToPlay = stairsFootstepsEvent;
                break;
            default:
                eventToPlay = stoneFootstepsEvent;
                break;
        }

        if (!eventToPlay.IsNull)
        {
            RuntimeManager.PlayOneShotAttached(eventToPlay, gameObject);
        }
    }
}