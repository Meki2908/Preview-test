using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Footstep procedural theo quãng đường di chuyển. Hoạt động với CharacterController (Soldier)
/// hoặc NavMeshAgent (zombie). Animation events có thể gọi PlayStepFromAnimationEvent().
/// </summary>
[DisallowMultipleComponent]
public class FootstepController : MonoBehaviour
{
    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    [Header("Clips")]
    [SerializeField] private AudioClip[] footstepClips;

    [Header("Procedural")]
    [SerializeField] private bool useProceduralSteps = true;
    [SerializeField] private float walkStepDistance = 0.55f;
    [SerializeField] private float runStepDistance = 0.4f;
    [SerializeField] private float minMoveSpeed = 0.12f;
    [SerializeField] private float runSpeedThreshold = 4f;

    [Header("Audio")]
    [SerializeField] [Range(0f, 1f)] private float volume = 0.35f;
    [SerializeField] [Range(0f, 1f)] private float spatialBlend = 1f;
    [SerializeField] private Vector3 footWorldOffset = new Vector3(0f, 0.05f, 0f);
    [SerializeField] private float pitchMin = 0.92f;
    [SerializeField] private float pitchMax = 1.08f;

    private AudioSource audioSource;
    private CharacterController characterController;
    private NavMeshAgent agent;
    private Animator animator;
    private Health health;
    private SoldierController soldierController;

    private Vector3 lastPosition;
    private float distanceAccum;
    private int lastClipIndex = -1;

    public AudioClip[] FootstepClips => footstepClips;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        health = GetComponent<Health>();
        soldierController = GetComponent<SoldierController>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = spatialBlend;
        audioSource.volume = volume;

        lastPosition = transform.position;
    }

    private void Update()
    {
        if (!useProceduralSteps || !CanPlay())
            return;

        float speed = GetHorizontalSpeed();
        if (speed < minMoveSpeed)
        {
            distanceAccum = 0f;
            lastPosition = transform.position;
            return;
        }

        Vector3 delta = transform.position - lastPosition;
        delta.y = 0f;
        distanceAccum += delta.magnitude;
        lastPosition = transform.position;

        float stepDistance = speed >= runSpeedThreshold ? runStepDistance : walkStepDistance;
        while (distanceAccum >= stepDistance)
        {
            distanceAccum -= stepDistance;
            PlayStep();
        }
    }

    public void PlayStepFromAnimationEvent()
    {
        if (CanPlay())
            PlayStep();
    }

    public void SetFootstepClips(AudioClip[] clips)
    {
        footstepClips = clips;
    }

    private bool CanPlay()
    {
        if (footstepClips == null || footstepClips.Length == 0)
            return false;

        if (health != null && health.IsDead)
            return false;

        if (agent != null && (!agent.enabled || agent.isStopped))
            return false;

        return true;
    }

    private float GetHorizontalSpeed()
    {
        if (agent != null && agent.enabled)
            return agent.velocity.magnitude;

        if (characterController != null && characterController.enabled)
        {
            Vector3 delta = transform.position - lastPosition;
            delta.y = 0f;
            float deltaSpeed = Time.deltaTime > 0f ? delta.magnitude / Time.deltaTime : 0f;

            if (animator != null)
            {
                float animSpeed = animator.GetFloat(SpeedHash);
                float estimatedSpeed = animSpeed * GetReferenceMoveSpeed();
                return Mathf.Max(deltaSpeed, estimatedSpeed);
            }

            return deltaSpeed;
        }

        Vector3 frameDelta = transform.position - lastPosition;
        frameDelta.y = 0f;
        return Time.deltaTime > 0f ? frameDelta.magnitude / Time.deltaTime : 0f;
    }

    private float GetReferenceMoveSpeed()
    {
        if (soldierController != null)
            return soldierController.moveSpeed;

        if (agent != null)
            return Mathf.Max(agent.speed, walkStepDistance);

        return 5f;
    }

    private void PlayStep()
    {
        AudioClip clip = PickClip();
        if (clip == null)
            return;

        Vector3 position = transform.TransformPoint(footWorldOffset);
        audioSource.pitch = Random.Range(pitchMin, pitchMax);
        audioSource.PlayOneShot(clip, volume);
    }

    private AudioClip PickClip()
    {
        if (footstepClips.Length == 1)
            return footstepClips[0];

        int index = Random.Range(0, footstepClips.Length);
        if (footstepClips.Length > 1 && index == lastClipIndex)
            index = (index + 1) % footstepClips.Length;

        lastClipIndex = index;
        return footstepClips[index];
    }
}
