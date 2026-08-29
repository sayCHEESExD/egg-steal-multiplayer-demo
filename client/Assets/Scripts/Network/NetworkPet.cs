using UnityEngine;
using TMPro; 

public class NetworkPet : MonoBehaviour
{
    [HideInInspector] public Pet serverState;
    
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip ambientClip;
    
    [Header("Animation")]
    public Animator animator;

    [Header("UI Sign")]
    public TMP_Text sellSignText; 

    private float nextSoundTime;
    private bool uiInitialized = false;
    private Transform localPlayerTransform; 
    private Camera mainCamera; // Cache camera for performance

    private void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (animator == null) animator = GetComponent<Animator>();
        
        mainCamera = Camera.main;
        nextSoundTime = Time.time + Random.Range(3f, 10f);
    }

    private void Update()
    {
        if (serverState == null) return;

        if (!uiInitialized)
        {
            PetUI ui = GetComponentInChildren<PetUI>();
            if (ui != null)
            {
                ui.SetupUI((int)serverState.biomeIndex);
            }
            uiInitialized = true;
        }

        if (localPlayerTransform == null)
        {
            NetworkPlayer[] players = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);
            foreach (var p in players)
            {
                if (p.isLocalPlayer)
                {
                    localPlayerTransform = p.transform;
                    break;
                }
            }
        }

        // --- UI PROMPT LOGIC ---
        if (sellSignText != null && NetworkManager.Instance != null && NetworkManager.Instance.room != null)
        {
            // Force the text to always face the camera (Billboard effect)
            if (mainCamera != null)
            {
                sellSignText.transform.rotation = mainCamera.transform.rotation;
            }

            if (serverState.ownerId == NetworkManager.Instance.room.SessionId && localPlayerTransform != null)
            {
                float distance = Vector2.Distance(
                    new Vector2(transform.position.x, transform.position.z), 
                    new Vector2(localPlayerTransform.position.x, localPlayerTransform.position.z)
                );

                if (distance < 4f)
                {
                    float sellValue = 50 * (serverState.biomeIndex + 1);
                    sellSignText.text = $"<color=yellow>+{sellValue} Coins</color>\n[X] Sell";
                }
                else
                {
                    sellSignText.text = ""; 
                }
            }
            else
            {
                sellSignText.text = ""; 
            }
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