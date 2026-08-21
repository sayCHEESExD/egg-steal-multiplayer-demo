using UnityEngine;
using TMPro; // Added TMPro namespace

public class NetworkPet : MonoBehaviour
{
    [HideInInspector] public Pet serverState;
    
    [Header("UI")]
    public TextMeshProUGUI incomeText; // Changed to TextMeshProUGUI
    
    private bool uiInitialized = false;

    private void Update()
    {
        if (serverState == null) return;

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

        Vector3 targetPosition = new Vector3(serverState.x, serverState.y, serverState.z);
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 5f);

        Quaternion targetRotation = Quaternion.Euler(0, serverState.rotY, 0);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
    }
}