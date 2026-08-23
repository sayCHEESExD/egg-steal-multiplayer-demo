using UnityEngine;
using Colyseus.Schema;

public class NetworkPlayer : MonoBehaviour
{
    [Header("Settings")]
    public Animator animator;
    public float rotationSpeed = 15f;
    
    [HideInInspector] public bool isLocalPlayer = false;
    [HideInInspector] public Player serverState;

    private int lastBiomeIndex = -1;
    
    private readonly string[] biomeNames = {
        "Plains",
        "Desert",
        "Forest",
        "Snow",
        "Volcano",
        "Abyss Ocean",
        "Prehistoric",
        "Cosmic"
    };
    
    private void Start()
    {
        // Automatically fetch the Animator if the slot is empty
        if (animator == null) animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

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
            
            if (serverState != null && UIManager.Instance != null)
            {
                UIManager.Instance.UpdateStats(serverState.coins, serverState.moveSpeed);
            }

            CheckBiomePosition(); 
        }
        else
        {
            HandleRemoteMovement();
        }
    }

    private void HandleUpgrades()
    {
        if (Input.GetKeyDown(KeyCode.U))
        {
            NetworkManager.Instance.room.Send("upgrade_speed");
        }
    }

    private void HandleLocalMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        if (Input.GetKeyDown(KeyCode.Space))
        {
            NetworkManager.Instance.room.Send("jump");
        }

        // Local Animation
        if (animator != null)
        {
            float inputMagnitude = new Vector2(horizontal, vertical).magnitude > 0.1f ? 1f : 0f;
            animator.SetFloat("Speed", inputMagnitude);

            float currentSpeed = serverState != null ? serverState.moveSpeed : 10f;
            animator.speed = currentSpeed / 10f;
        }
        
        if (serverState != null)
        {
            Vector3 pos = transform.position;
            pos.y = Mathf.Lerp(pos.y, serverState.y, Time.deltaTime * 15f);
            transform.position = pos;
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

        if (Input.GetKeyDown(KeyCode.E))
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
        
        // Remote Animation: Calculate distance to target to determine if they are walking
        if (animator != null)
        {
            // Ignore Y axis so jumping doesn't trigger the walk animation
            float distanceToTarget = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(targetPosition.x, targetPosition.z));
            float remoteSpeed = distanceToTarget > 0.1f ? 1f : 0f;
            animator.SetFloat("Speed", remoteSpeed);

            float currentSpeed = serverState.moveSpeed > 0 ? serverState.moveSpeed : 10f;
            animator.speed = currentSpeed / 10f;
        }

        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 10f);

        Quaternion targetRotation = Quaternion.Euler(0, serverState.rotY, 0);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
    }

    private void CheckBiomePosition()
    {
        int currentBiome = Mathf.FloorToInt((transform.position.z - 50f) / 100f);

        if (currentBiome != lastBiomeIndex)
        {
            lastBiomeIndex = currentBiome;
            
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