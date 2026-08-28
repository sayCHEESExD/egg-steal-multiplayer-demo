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
        "Plains  <sprite=0>",
        "Desert  <sprite=1>",
        "Forest  <sprite=2>",
        "Snow  <sprite=3>",
        "Volcano  <sprite=4>",
        "Abyss Ocean  <sprite=5>",
        "Prehistoric  <sprite=6>",
        "Cosmic  <sprite=7>"
    };
    
    private void Start()
    {
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
        if (Input.GetKeyDown(KeyCode.T))
        {
            NetworkManager.Instance.room.Send("upgrade_speed");
        }
    }

    private void HandleLocalMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        // 1. Check if local player is on a treadmill
        bool isOnTreadmill = false;
        if (NetworkManager.Instance != null && NetworkManager.Instance.room != null)
        {
            foreach (var kvp in NetworkManager.Instance.spawnedTreadmills)
            {
                NetworkTreadmill tm = kvp.Value.GetComponent<NetworkTreadmill>();
                if (tm != null && tm.serverState != null && tm.serverState.occupantId == NetworkManager.Instance.room.SessionId)
                {
                    isOnTreadmill = true;
                    break;
                }
            }
        }

        // --- TREADMILL INTERACTION ---
        if (Input.GetKeyDown(KeyCode.T))
        {
            TryInteractTreadmill();
        }

        // If on treadmill, snap to server position/rotation, play animation, and STOP physical movement
        if (isOnTreadmill)
        {
            if (animator != null)
            {
                animator.SetFloat("Speed", 1f);
                animator.speed = 3f; 
            }
            
            if (serverState != null)
            {
                // Force rotation to face forward and snap position to the treadmill
                transform.position = new Vector3(serverState.x, serverState.y, serverState.z);
                transform.rotation = Quaternion.Euler(0f, serverState.rotY, 0f);
            }
            return; // Skip normal movement
        }

        // --- NEW SPEED COMPRESSION MATH ---
        float massiveStat = (serverState != null) ? serverState.moveSpeed : 10f;
        float actualUnitySpeed = 10f + (Mathf.Log10(massiveStat + 1) * 5f);
        // ----------------------------------

        // 2. Normal local inputs
        if (Input.GetKeyDown(KeyCode.Space))
        {
            NetworkManager.Instance.room.Send("jump");
        }

        // 3. Egg Interaction
        if (Input.GetKeyDown(KeyCode.E))
        {
            bool isCarrying = false;
            foreach (var kvp in NetworkManager.Instance.spawnedEggs)
            {
                NetworkEgg e = kvp.Value.GetComponent<NetworkEgg>();
                // Added e.serverState != null to prevent Null Reference Exceptions which silently break the E key
                if (e != null && e.serverState != null && e.serverState.carrierId == NetworkManager.Instance.room.SessionId)
                {
                    isCarrying = true;
                    break;
                }
            }

            if (StealHUD.IsCarryingEgg)
            {
                NetworkManager.Instance.room.Send("deliver_egg");
            }
            else
            {
                TryPickupNearestEgg();
            }
        }

        if (animator != null)
        {
            float inputMagnitude = new Vector2(horizontal, vertical).magnitude > 0.1f ? 1f : 0f;
            animator.SetFloat("Speed", inputMagnitude);
            animator.speed = Mathf.Clamp(actualUnitySpeed / 10f, 1f, 4f); 
        }
        
        if (serverState != null)
        {
            Vector3 pos = transform.position;
            pos.y = Mathf.Lerp(pos.y, serverState.y, Time.deltaTime * 15f);
            transform.position = pos;
        }

        // 4. Camera-Relative Movement
        if (horizontal != 0 || vertical != 0)
        {
            Vector3 camForward = Camera.main.transform.forward;
            Vector3 camRight = Camera.main.transform.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 moveDir = (camForward * vertical + camRight * horizontal).normalized;
            
            transform.position += moveDir * actualUnitySpeed * Time.deltaTime;

            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);

            NetworkManager.Instance.room.Send("move", new { 
                x = transform.position.x, 
                y = transform.position.y, 
                z = transform.position.z,
                rotY = transform.eulerAngles.y
            });
        }
    }

    private void TryInteractTreadmill()
    {
        float closestDistance = 4f; 
        string closestTmId = "";

        foreach (var kvp in NetworkManager.Instance.spawnedTreadmills)
        {
            float distance = Vector3.Distance(transform.position, kvp.Value.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTmId = kvp.Key;
            }
        }

        if (!string.IsNullOrEmpty(closestTmId))
        {
            NetworkManager.Instance.room.Send("interact_treadmill", new { id = closestTmId });
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
        
        bool isRemoteOnTreadmill = false;
        if (NetworkManager.Instance != null)
        {
            foreach (var kvp in NetworkManager.Instance.spawnedTreadmills)
            {
                NetworkTreadmill tm = kvp.Value.GetComponent<NetworkTreadmill>();
                if (tm != null && tm.serverState != null && !string.IsNullOrEmpty(tm.serverState.occupantId))
                {
                    if (NetworkManager.Instance.GetSpawnedPlayer(tm.serverState.occupantId) == this.gameObject)
                    {
                        isRemoteOnTreadmill = true;
                        break;
                    }
                }
            }
        }

        if (animator != null)
        {
            if (isRemoteOnTreadmill)
            {
                animator.SetFloat("Speed", 1f);
                animator.speed = 3f;
            }
            else
            {
                float distanceToTarget = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(targetPosition.x, targetPosition.z));
                float remoteSpeed = distanceToTarget > 0.1f ? 1f : 0f;
                animator.SetFloat("Speed", remoteSpeed);

                float currentSpeed = serverState.moveSpeed > 0 ? serverState.moveSpeed : 10f;
                animator.speed = currentSpeed / 10f;
            }
        }

        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 10f);

        Quaternion targetRotation = Quaternion.Euler(0, serverState.rotY, 0);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
    }

    private void CheckBiomePosition()
    {
        // Tell the AudioManager if we are outside the safe zone (Z >= 50)
        bool isInBiome = transform.position.z >= 50f;
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ToggleBiomeMusic(isInBiome);
        }

        int currentBiome = Mathf.FloorToInt((transform.position.z - 50f) / 100f);

        if (currentBiome != lastBiomeIndex)
        {
            lastBiomeIndex = currentBiome;
            
            if (currentBiome >= 0 && currentBiome < biomeNames.Length)
            {
                if (UIManager.Instance != null && !StealHUD.IsCarryingEgg)
                {
                    UIManager.Instance.ShowBiomeText(biomeNames[currentBiome]);
                }
            }
        }
    }
}