using UnityEngine;

public class StealHUD : MonoBehaviour
{
    [Header("UI References")]
    public GameObject runBanner;
    public GameObject dropPrompt;
    public GameObject hatchPrompt;
    
    [Header("Input")]
    public KeyCode dropKey = KeyCode.Q;

    public static bool IsCarryingEgg = false; 
    private Transform localPlayer;

    void Start()
    {
        runBanner.SetActive(false);
        dropPrompt.SetActive(false);
        if (hatchPrompt != null) hatchPrompt.SetActive(false);
    }

    void Update()
    {
        if (localPlayer == null && NetworkManager.Instance != null && NetworkManager.Instance.room != null)
        {
            GameObject pObj = NetworkManager.Instance.GetSpawnedPlayer(NetworkManager.Instance.room.SessionId);
            if (pObj != null) localPlayer = pObj.transform;
        }

        if (!IsCarryingEgg)
        {
            runBanner.SetActive(false);
            dropPrompt.SetActive(false);
            if (hatchPrompt != null) hatchPrompt.SetActive(false);
            return;
        }

        bool inSafeZone = localPlayer != null && localPlayer.position.z < 50f;

        if (inSafeZone)
        {
            runBanner.SetActive(false);
            dropPrompt.SetActive(false);
            if (hatchPrompt != null) hatchPrompt.SetActive(true);
        }
        else
        {
            runBanner.SetActive(true);
            dropPrompt.SetActive(true);
            if (hatchPrompt != null) hatchPrompt.SetActive(false);

            if (Input.GetKeyDown(dropKey))
            {
                NetworkManager.Instance.room.Send("drop_egg");
            }
        }
    }
}