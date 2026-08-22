using UnityEngine;
using Colyseus.Schema;

public class NetworkPlayer : MonoBehaviour
{
    [Header("Settings")]
    public Animator animator;
    public float rotationSpeed = 15f;
    // Removed moveSpeed - it is now controlled by the server!
    
    [HideInInspector] public bool isLocalPlayer = false;
    [HideInInspector] public Player serverState;

    private int lastBiomeIndex = -1;
    
    private readonly string[] biomeNames = {
        "Plains",
        "Desert",
        "Forest",
        "Snow",
        "Abyss Ocean",
        "Prehistoric",
        "Cosmic",
        "Volcano" 
    };
    
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
            HandleUpgrades();
            
            // Sync UI with server state
            if (serverState != null && UIManager.Instance != null)
            {
                UIManager.Instance.UpdateStats(serverState.coins, serverState.moveSpeed);
            }

            CheckBiomePosition(); // <-- NEW
        }
        else
        {
            HandleRemoteMovement();
        }
    }

    private void HandleUpgrades()
    {
        // Press 'U' to buy a speed upgrade
        if (Input.GetKeyDown(KeyCode.U))
        {
            NetworkManager.Instance.room.Send("upgrade_speed");
        }
    }

    private void HandleLocalMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        if (animator != null)
        {
            float inputMagnitude = new Vector2(horizontal, vertical).magnitude;
            animator.SetFloat("Speed", inputMagnitude);
        }
        
        if (horizontal != 0 || vertical != 0)
        {
            Vector3 camForward = Camera.main.transform.forward;
            Vector3 camRight = Camera.main.transform.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 moveDir = (camForward * vertical + camRight * horizontal).normalized;
            
            // Use serverState.moveSpeed for local movement calculations
            float currentSpeed = serverState != null ? serverState.moveSpeed : 5f;
            transform.position += moveDir * currentSpeed * Time.deltaTime;

            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);

            NetworkManager.Instance.room.Send("move", new { 
                x = transform.position.x, 
                y = transform.position.y, 
                z = transform.position.z,
                rotY = transform.eulerAngles.y
            });
        }

        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space))
        {
            bool isCarrying = false;
            foreach (var kvp in NetworkManager.Instance.spawnedEggs)
            {
                NetworkEgg e = kvp.Value.GetComponent<NetworkEgg>();
                if (e != null && e.serverState.carrierId == NetworkManager.Instance.room.SessionId)
                {
                    isCarrying = true;
                    break;
                }
            }

            if (isCarrying)
            {
                NetworkManager.Instance.room.Send("deliver_egg");
            }
            else
            {
                TryPickupNearestEgg();
            }
        }
    }

    private void TryPickupNearestEgg()
    {
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

        Vector3 targetPosition = new Vector3(serverState.x, serverState.y, serverState.z);
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 10f);

        Quaternion targetRotation = Quaternion.Euler(0, serverState.rotY, 0);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
    }

    private void CheckBiomePosition()
    {
        // Biome centers are at Z = 100, 200, 300... 
        // We shift by 50 so crossing Z=50 puts you in Biome 0, Z=150 puts you in Biome 1, etc.
        int currentBiome = Mathf.FloorToInt((transform.position.z - 50f) / 100f);

        if (currentBiome != lastBiomeIndex)
        {
            lastBiomeIndex = currentBiome;
            
            // Only show text if within valid biome ranges (0 to 7)
            if (currentBiome >= 0 && currentBiome < biomeNames.Length)
            {
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowBiomeText(biomeNames[currentBiome]);
                }
            }
        }
    }
}