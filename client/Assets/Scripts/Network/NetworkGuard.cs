using UnityEngine;

public class NetworkGuard : MonoBehaviour
{
    [HideInInspector] public Guard serverState;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip growlClip;
    
    [Header("Animation")]
    public Animator animator;
    
    private float nextGrowlTime;

    private void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        
        // Automatically try to assign the Animator if it isn't set in the Inspector
        if (animator == null) animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (serverState == null) return;

        Vector3 targetPosition = new Vector3(serverState.x, serverState.y, serverState.z);
        
        float distanceToTarget = Vector3.Distance(targetPosition, transform.position);
        
        // 1. Audio Logic
        if (distanceToTarget > 0.1f)
        {
            if (growlClip != null && Time.time > nextGrowlTime)
            {
                if (audioSource != null)
                {
                    audioSource.PlayOneShot(growlClip);
                }
                nextGrowlTime = Time.time + Random.Range(2f, 5f);
            }
        }

        // 2. Animation Logic (Ignores Guard 6 gracefully)
        if (animator != null)
        {
            // If the guard is far enough from the server target to visibly move, set Speed to 1 (Walk)
            // Otherwise, set to 0 (Idle)
            float moveSpeed = distanceToTarget > 0.05f ? 1f : 0f;
            animator.SetFloat("Speed", moveSpeed);
        }

        // 3. Movement and Rotation Interpolation (Unchanged)
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 10f);
        
        Quaternion targetRotation = Quaternion.Euler(0, serverState.rotY, 0);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
    }
}