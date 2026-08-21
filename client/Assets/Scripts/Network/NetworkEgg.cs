using UnityEngine;

public class NetworkEgg : MonoBehaviour
{
    [HideInInspector] public Egg serverState;
    public string eggId;
    public float holdHeightOffset = 4.5f; 

    private void Update()
    {
        if (serverState == null) return;

        // state 1 = Carried
        if (serverState.state == 1 && !string.IsNullOrEmpty(serverState.carrierId))
        {
            GameObject carrier = NetworkManager.Instance.GetSpawnedPlayer(serverState.carrierId);
            if (carrier != null)
            {
                Vector3 targetPosition = carrier.transform.position + (Vector3.up * holdHeightOffset);
                transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 15f);
            }
        }
        else // state 0 (Ground) or state 2 (Hatching)
        {
            Vector3 targetPosition = new Vector3(serverState.x, serverState.y, serverState.z);
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 10f);
        }
    }
}