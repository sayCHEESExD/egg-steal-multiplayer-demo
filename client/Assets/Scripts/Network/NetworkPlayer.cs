using UnityEngine;
using Colyseus.Schema;

public class NetworkPlayer : MonoBehaviour
{
    [Header("Settings")]
    public float moveSpeed = 5f;
    
    // Hidden variables managed by NetworkManager
    [HideInInspector] public bool isLocalPlayer = false;
    [HideInInspector] public Player serverState;

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
        // 1. Get WASD Input
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        if (horizontal != 0 || vertical != 0)
        {
            // 2. Calculate new position
            Vector3 movement = new Vector3(horizontal, 0, vertical).normalized * moveSpeed * Time.deltaTime;
            transform.position += movement;

            // 3. Send new position to the server
            // We use an anonymous object to match the server's expected data format
            NetworkManager.Instance.room.Send("move", new { x = transform.position.x, y = transform.position.y, z = transform.position.z });
        }
    }

    private void HandleRemoteMovement()
    {
        if (serverState == null) return;

        // Smoothly interpolate other players to their server positions
        Vector3 targetPosition = new Vector3(serverState.x, serverState.y, serverState.z);
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 10f);
    }
}