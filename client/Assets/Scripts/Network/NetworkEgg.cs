using UnityEngine;

public class NetworkEgg : MonoBehaviour
{
    [HideInInspector] public Egg serverState;
    public string eggId;
    public float holdHeightOffset = 4.5f; 
    
    // Tracks if this specific egg was being carried by the local player last frame
    private bool wasCarriedByMe = false;

    private void Update()
    {
        if (serverState == null) return;

        bool isVisible = serverState.state != 3;
        foreach (Renderer r in GetComponentsInChildren<Renderer>()) 
        {
            r.enabled = isVisible;
        }
        if (TryGetComponent(out Collider c)) c.enabled = isVisible;

        if (!isVisible) return;

        // 1. Toggle HUD Logic
        if (NetworkManager.Instance != null && NetworkManager.Instance.room != null)
        {
            string mySessionId = NetworkManager.Instance.room.SessionId;
            bool isCarriedByMe = (serverState.state == 1 && serverState.carrierId == mySessionId);

            if (isCarriedByMe && !wasCarriedByMe)
            {
                StealHUD.IsCarryingEgg = true;
                wasCarriedByMe = true;
            }
            else if (!isCarriedByMe && wasCarriedByMe)
            {
                StealHUD.IsCarryingEgg = false;
                wasCarriedByMe = false;
            }
        }

        // 2. Movement Logic
        // state 1 = Carried
        if (serverState.state == 1 && !string.IsNullOrEmpty(serverState.carrierId))
        {
            GameObject carrier = NetworkManager.Instance.GetSpawnedPlayer(serverState.carrierId);
            if (carrier != null)
            {
                Vector3 targetPosition = carrier.transform.position + (Vector3.up * holdHeightOffset);
                
                // Instantly snap to the local player to hide network delay
                if (serverState.carrierId == NetworkManager.Instance.room.SessionId)
                {
                    transform.position = targetPosition;
                }
                else 
                {
                    // Smoothly follow other players
                    transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 15f);
                }
            }
        }
        else // state 0 (Ground) or state 2 (Hatching)
        {
            Vector3 targetPosition = new Vector3(serverState.x, serverState.y, serverState.z);
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 10f);
        }
    }
}