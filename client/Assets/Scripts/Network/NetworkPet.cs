using UnityEngine;

public class NetworkPet : MonoBehaviour
{
    [HideInInspector] public Pet serverState;

    private void Update()
    {
        if (serverState == null) return;

        Vector3 targetPosition = new Vector3(serverState.x, serverState.y, serverState.z);
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 5f);

        Quaternion targetRotation = Quaternion.Euler(0, serverState.rotY, 0);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
    }
}