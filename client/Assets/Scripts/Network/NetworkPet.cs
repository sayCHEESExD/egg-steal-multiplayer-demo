using UnityEngine;
using TMPro;

public class NetworkPet : MonoBehaviour
{
    [HideInInspector] public Pet serverState;
    
    [Header("UI")]
    public TextMeshProUGUI incomeText; 
    
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
        
        // Automatically try to assign the Animator if it isn't set in the Inspector
        if (animator == null) animator = GetComponent<Animator>();

        nextSoundTime = Time.time + Random.Range(3f, 10f);
    }

    private void Update()
    {
        if (serverState == null) return;

        // 1. UI Logic (Unchanged)
        if (!uiInitialized && incomeText != null)
        {
            float income = 1 + (serverState.biomeIndex * 2);
            incomeText.text = "+" + income + "/s";
            uiInitialized = true;
        }

        if (incomeText != null && Camera.main != null)
        {
            incomeText.transform.parent.rotation = Camera.main.transform.rotation;
        }

        // 2. Movement Calculations
        Vector3 targetPosition = new Vector3(serverState.x, serverState.y, serverState.z);
        float distanceToTarget = Vector3.Distance(targetPosition, transform.position);

        // 3. Animation Logic (Safely handles models with no Animator)
        if (animator != null)
        {
            // If the pet is far enough from the server target to visibly move, set Speed to 1 (Walk)
            float moveSpeed = distanceToTarget > 0.05f ? 1f : 0f;
            animator.SetFloat("Speed", moveSpeed);
        }

        // 4. Movement and Rotation Interpolation (Unchanged)
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 5f);

        Quaternion targetRotation = Quaternion.Euler(0, serverState.rotY, 0);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);

        // 5. Audio Logic (Unchanged)
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