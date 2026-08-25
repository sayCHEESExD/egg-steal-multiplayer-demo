using UnityEngine;

public class EggPrompt : MonoBehaviour
{
    public GameObject promptUI;
    public float interactDistance = 3.5f;
    
    private NetworkEgg eggScript;
    private Transform localPlayerTransform;

    void Start()
    {
        eggScript = GetComponentInParent<NetworkEgg>();
    }

    void LateUpdate()
    {
        if (Camera.main != null)
        {
            transform.rotation = Camera.main.transform.rotation;
        }

        if (localPlayerTransform == null)
        {
            NetworkPlayer[] players = FindObjectsOfType<NetworkPlayer>();
            foreach (var p in players)
            {
                if (p.isLocalPlayer)
                {
                    localPlayerTransform = p.transform;
                    break;
                }
            }
            return;
        }

        bool isFree = eggScript != null && eggScript.serverState != null && string.IsNullOrEmpty(eggScript.serverState.carrierId);
        float dist = Vector3.Distance(transform.position, localPlayerTransform.position);
        
        promptUI.SetActive(isFree && dist <= interactDistance);
    }
}