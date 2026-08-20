using UnityEngine;

public class NetworkEgg : MonoBehaviour
{
    [HideInInspector] public Egg serverState;
    public string eggId;

    private void Update()
    {
        if (serverState == null) return;

        // If the server says someone is carrying this egg
        if (!string.IsNullOrEmpty(serverState.carrierId))
        {
            // Get the player carrying it
            GameObject carrier = NetworkManager.Instance.GetSpawnedPlayer(serverState.carrierId);
            if (carrier != null)
            {
                // Smoothly snap the egg above the player's head
                Vector3 targetPosition = carrier.transform.position + (Vector3.up * 2.5f);
                transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 15f);
            }
        }
        else
        {
            // If dropped/unclaimed, stay at the server's floor coordinates
            Vector3 targetPosition = new Vector3(serverState.x, serverState.y, serverState.z);
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 10f);
        }
    }
}