using UnityEngine;

public class StealHUD : MonoBehaviour
{
    [Header("UI References")]
    public GameObject runBanner;
    public GameObject dropPrompt;
    
    [Header("Input")]
    public KeyCode dropKey = KeyCode.Backspace;

    // Set this from your NetworkPlayer/NetworkManager when a pickup or drop happens
    public static bool IsCarryingEgg = false; 

    void Start()
    {
        runBanner.SetActive(false);
        dropPrompt.SetActive(false);
    }

    void Update()
    {
        // Toggle visibility based on carrying state
        if (runBanner.activeSelf != IsCarryingEgg)
        {
            runBanner.SetActive(IsCarryingEgg);
            dropPrompt.SetActive(IsCarryingEgg);
        }

        // Handle Drop Input
        if (IsCarryingEgg && Input.GetKeyDown(dropKey))
        {
            // IMPORTANT: Replace this line with your actual Colyseus Room reference
            // e.g., NetworkManager.Instance.room.Send("drop_egg");
            
            IsCarryingEgg = false; // Instantly hide UI locally
        }
    }
}