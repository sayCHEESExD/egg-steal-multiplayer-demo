using UnityEngine;

public class NetworkPet : MonoBehaviour
{
    [HideInInspector] public Pet serverState;
    
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip ambientClip;
    
    [Header("Animation")]
    public Animator animator;

    private float nextSoundTime;
    private bool uiInitialized = false;

    private void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        
        if (animator == null) animator = GetComponent<Animator>();

        nextSoundTime = Time.time + Random.Range(3f, 10f);
    }

    private void Update()
    {
        if (serverState == null) return;

        // 1. Initialize UI using the new PetUI script once the server state is available
        if (!uiInitialized)
        {
            PetUI ui = GetComponentInChildren<PetUI>();
            if (ui != null)
            {
                ui.SetupUI((int)serverState.biomeIndex);
            }
            uiInitialized = true;
        }

        // 2. Movement Calculations
        Vector3 targetPosition = new Vector3(serverState.x, serverState.y, serverState.z);
        float distanceToTarget = Vector3.Distance(targetPosition, transform.position);

        // 3. Animation Logic
        if (animator != null)
        {
            float moveSpeed = distanceToTarget > 0.05f ? 1f : 0f;
            animator.SetFloat("Speed", moveSpeed);
        }

        // 4. Movement and Rotation Interpolation
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 5f);

        Quaternion targetRotation = Quaternion.Euler(0, serverState.rotY, 0);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);

        // 5. Audio Logic
        if (ambientClip != null && Time.time > nextSoundTime)
        {
            if (audioSource != null && !audioSource.isPlaying)
            {
                audioSource.PlayOneShot(ambientClip);
            }
            nextSoundTime = Time.time + Random.Range(5f, 15f); 
        }
    }
}