using UnityEngine;
using Colyseus.Schema;

public class NetworkPlayer : MonoBehaviour
{
    [Header("Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 15f;
    
    [HideInInspector] public bool isLocalPlayer = false;
    [HideInInspector] public Player serverState;

    private void Start()
    {
        if (isLocalPlayer)
        {
            Camera.main.GetComponent<CameraFollow>().target = this.transform;
        }
    }

    private void Update()
    {
        if (isLocalPlayer)
        {
            HandleLocalMovement();
        }
        else
        {
            HandleRemoteMovement();
        }
    }

    private void HandleLocalMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        if (horizontal != 0 || vertical != 0)
        {
            // 1. Calculate movement relative to camera
            Vector3 camForward = Camera.main.transform.forward;
            Vector3 camRight = Camera.main.transform.right;
            camForward.y = 0f; // Keep movement flat
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 moveDir = (camForward * vertical + camRight * horizontal).normalized;
            transform.position += moveDir * moveSpeed * Time.deltaTime;

            // 2. Rotate the player model to face the movement direction
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);

            // 3. Send position AND rotation to server
            NetworkManager.Instance.room.Send("move", new { 
                x = transform.position.x, 
                y = transform.position.y, 
                z = transform.position.z,
                rotY = transform.eulerAngles.y
            });
        }

        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space))
        {
            TryPickupNearestEgg();
        }
    }

    private void TryPickupNearestEgg()
    {
        foreach (var kvp in NetworkManager.Instance.spawnedEggs)
        {
            NetworkEgg e = kvp.Value.GetComponent<NetworkEgg>();
            if (e != null && e.serverState.carrierId == NetworkManager.Instance.room.SessionId) return;
        }

        float closestDistance = 3f; 
        string closestEggId = "";

        foreach (var kvp in NetworkManager.Instance.spawnedEggs)
        {
            GameObject eggObj = kvp.Value;
            NetworkEgg eggScript = eggObj.GetComponent<NetworkEgg>();

            if (eggScript != null && string.IsNullOrEmpty(eggScript.serverState.carrierId))
            {
                float distance = Vector3.Distance(transform.position, eggObj.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEggId = eggScript.eggId;
                }
            }
        }

        if (!string.IsNullOrEmpty(closestEggId))
        {
            NetworkManager.Instance.room.Send("pickup_egg", new { eggId = closestEggId });
        }
    }

    private void HandleRemoteMovement()
    {
        if (serverState == null) return;

        // Interpolate position
        Vector3 targetPosition = new Vector3(serverState.x, serverState.y, serverState.z);
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 10f);

        // Interpolate rotation
        Quaternion targetRotation = Quaternion.Euler(0, serverState.rotY, 0);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
    }
}