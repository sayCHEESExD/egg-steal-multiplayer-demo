using UnityEngine;
using Colyseus.Schema;
using TMPro;

public class NetworkPlayer : MonoBehaviour
{
    [Header("Settings")]
    public Animator animator;
    public float rotationSpeed = 15f;

    [Header("UI Popups")]
    public TMP_Text speedPopupText;
    private float lastMoveSpeed = -1f;
    private float popupTimer = 0f;
    private Vector3 popupStartLocalPos = new Vector3(0, 4.0f, 0);
    
    [HideInInspector] public bool isLocalPlayer = false;
    [HideInInspector] public Player serverState;

    private int lastBiomeIndex = -1;
    private float treadmillCooldown = 0f;

    private float networkSendRate = 0.05f; // 1 / 20 = 20 times per second
    private float nextSendTime = 0f;

    private Vector3 knockbackVelocity = Vector3.zero;
    private bool knockbackRegistered = false;

    private CharacterController characterController;
    
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
        characterController = GetComponent<CharacterController>();
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
            // 1. Knockback Listener
            if (!knockbackRegistered && NetworkManager.Instance != null && NetworkManager.Instance.room != null)
            {
                NetworkManager.Instance.room.OnMessage<KnockbackMessage>("knockback", (msg) => {
                    knockbackVelocity = new Vector3(msg.x, 0, msg.z);
                });
                knockbackRegistered = true;
            }

            // 2. RESTORE YOUR MISSING UI CODE HERE
            if (serverState != null)
            {
               UIManager.Instance.UpdateStats(serverState.coins, serverState.moveSpeed);
            }

            // 3. Movement and Popups
            HandleLocalMovement();
            HandleSpeedPopup();
        }
        else
        {
            HandleRemoteMovement();
        }
    }

    private void HandleUpgrades()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            TryUpgradeTreadmill();
        }
    }

    private void TryUpgradeTreadmill()
    {
        float closestDistance = 5f; 
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
            NetworkManager.Instance.room.Send("upgrade_treadmill", new { id = closestTmId });
        }
    }

    private void HandleLocalMovement()
    {
        if (treadmillCooldown > 0f) treadmillCooldown -= Time.deltaTime;

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        // 1. Check Treadmill states and find the player's owned treadmill
        bool isOnTreadmill = false;
        string ownedTreadmillId = "";
        Vector3 ownedTreadmillPos = Vector3.zero;

        if (NetworkManager.Instance != null && NetworkManager.Instance.room != null)
        {
            foreach (var kvp in NetworkManager.Instance.spawnedTreadmills)
            {
                NetworkTreadmill tm = kvp.Value.GetComponent<NetworkTreadmill>();
                if (tm != null && tm.serverState != null)
                {
                    if (tm.serverState.ownerId == NetworkManager.Instance.room.SessionId)
                    {
                        ownedTreadmillId = kvp.Key;
                        ownedTreadmillPos = kvp.Value.transform.position;
                        
                        if (tm.serverState.occupantId == NetworkManager.Instance.room.SessionId)
                        {
                            isOnTreadmill = true;
                        }
                    }
                }
            }
        }

        // --- TREADMILL AUTO-MOUNT ---
        if (!isOnTreadmill && !string.IsNullOrEmpty(ownedTreadmillId))
        {
            Vector2 playerFlatPos = new Vector2(transform.position.x, transform.position.z);
            Vector2 treadmillFlatPos = new Vector2(ownedTreadmillPos.x, ownedTreadmillPos.z);
            float dist = Vector2.Distance(playerFlatPos, treadmillFlatPos);
            
            // Expanded distance to 1.5f and relaxed Y constraint to 1.5f
            if (dist < 1.5f && treadmillCooldown <= 0f && transform.position.y < 1.5f) 
            {
                // Use a strict Dictionary to prevent MsgPack serialization drops
                var payload = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "id", ownedTreadmillId }
                };
                NetworkManager.Instance.room.Send("mount_treadmill", payload);
                
                treadmillCooldown = 1.0f; 
            }
        }

        // If on treadmill, wait for SPACE to jump off and STOP physical movement
        if (isOnTreadmill)
        {
            if (animator != null)
            {
                animator.SetFloat("Speed", 1f);
                animator.speed = 3f; 
            }
            
            if (serverState != null)
            {
                Vector3 snapPos = new Vector3(serverState.x, serverState.y, serverState.z);
                
                // Temporarily disable Character Controller so it doesn't fight the snap
                if (characterController != null)
                {
                    characterController.enabled = false;
                    transform.position = snapPos;
                    characterController.enabled = true;
                }
                else
                {
                    transform.position = snapPos;
                }
                
                // Read the exact rotation from the server instead of forcing 0f
                transform.rotation = Quaternion.Euler(0f, serverState.rotY, 0f);
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                NetworkManager.Instance.room.Send("unmount_treadmill");
                
                // Give the player 1.5 seconds to fall and walk away before auto-mounting triggers again
                treadmillCooldown = 1.5f; 
            }
            return; 
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
            if (StealHUD.IsCarryingEgg)
            {
                NetworkManager.Instance.room.Send("deliver_egg");
            }
            else
            {
                TryPickupNearestEgg();
            }
        }

        if (transform.position.z < 50f && Input.GetKeyDown(KeyCode.G))
        {
            NetworkManager.Instance.room.Send("upgrade_enclosure");
        }

        // Sell Pet (Press X while standing near one of your pets)
        if (Input.GetKeyDown(KeyCode.X))
        {
            TrySellNearestPet();
        }

        if (animator != null)
        {
            float inputMagnitude = new Vector2(horizontal, vertical).magnitude > 0.1f ? 1f : 0f;
            animator.SetFloat("Speed", inputMagnitude);
            animator.speed = Mathf.Clamp(actualUnitySpeed / 10f, 1f, 4f); 
        }

        // 4. Camera-Relative Movement
        // --- NEW COMBINED MOVEMENT BLOCK ---
        Vector3 finalMove = Vector3.zero;

        // 1. Calculate Horizontal Movement (Player Input)
        if (horizontal != 0 || vertical != 0)
        {
            Vector3 camForward = Camera.main.transform.forward;
            Vector3 camRight = Camera.main.transform.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 moveDir = (camForward * vertical + camRight * horizontal).normalized;
            finalMove += moveDir * actualUnitySpeed * Time.deltaTime;

            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }

        // 2. Calculate Vertical Movement (Server Gravity / Jump)
        if (serverState != null)
        {
            float targetY = serverState.y;
            float currentY = transform.position.y;
            float newY = Mathf.Lerp(currentY, targetY, Time.deltaTime * 15f);
            
            finalMove.y = newY - currentY; 
        }

        // --- NEW: ADD KNOCKBACK DECAY ---
        if (knockbackVelocity.magnitude > 0.1f)
        {
            finalMove += knockbackVelocity * Time.deltaTime;
            
            // Rapidly slow down the knockback force over time
            knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, Time.deltaTime * 4f);
        }
        // --------------------------------

        // 3. Apply ALL movement simultaneously
        if (characterController != null)
        {
            characterController.Move(finalMove);
        }
        else
        {
            transform.position += finalMove;
        }

        // 4. Rate-Limited Network Sending
        if ((horizontal != 0 || vertical != 0) && Time.time >= nextSendTime)
        {
            // Server only cares about X and Z for movement anyway
            NetworkManager.Instance.room.Send("move", new { 
                x = transform.position.x, 
                z = transform.position.z,
                rotY = transform.eulerAngles.y
            });
            
            nextSendTime = Time.time + networkSendRate;
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

    private void TrySellNearestPet()
    {
        float closestDistance = 4f; 
        string closestPetId = "";

        // Assuming you track pets in NetworkManager and have a NetworkPet.cs script attached to them
        if (NetworkManager.Instance.spawnedPets != null)
        {
            foreach (var kvp in NetworkManager.Instance.spawnedPets)
            {
                NetworkPet pet = kvp.Value.GetComponent<NetworkPet>();
                if (pet != null && pet.serverState != null && pet.serverState.ownerId == NetworkManager.Instance.room.SessionId)
                {
                    float distance = Vector2.Distance(
                        new Vector2(transform.position.x, transform.position.z), 
                        new Vector2(kvp.Value.transform.position.x, kvp.Value.transform.position.z)
                    );
                    
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestPetId = kvp.Key;
                    }
                }
            }
        }

        if (!string.IsNullOrEmpty(closestPetId))
        {
            NetworkManager.Instance.room.Send("sell_pet", new { petId = closestPetId });
        }
    }
    private void HandleSpeedPopup()
    {
        if (serverState == null || speedPopupText == null) return;

        // Initialize tracking
        if (lastMoveSpeed < 0) lastMoveSpeed = serverState.moveSpeed;

        // Detect if the server increased our speed
        if (serverState.moveSpeed > lastMoveSpeed)
        {
            float gained = serverState.moveSpeed - lastMoveSpeed;
            
            // Use TMP rich text for the sprite. Change '0' to the index of your speed icon.
            speedPopupText.text = $"<sprite=0> +{gained}"; 
            
            speedPopupText.color = new Color(0.2f, 0.8f, 1f, 1f); // Cyan
            speedPopupText.transform.localPosition = popupStartLocalPos;
            popupTimer = 1f; // Display for 1 second
            
            lastMoveSpeed = serverState.moveSpeed;
        }
        else if (serverState.moveSpeed < lastMoveSpeed)
        {
            lastMoveSpeed = serverState.moveSpeed; // Reset if stats drop
        }

        // Animate the popup (Float up and fade out)
        if (popupTimer > 0)
        {
            popupTimer -= Time.deltaTime;
            speedPopupText.transform.localPosition += Vector3.up * Time.deltaTime * 1.5f;
            
            Color c = speedPopupText.color;
            c.a = popupTimer; // Fades alpha from 1 to 0
            speedPopupText.color = c;

            // Billboard text to always face the camera
            if (Camera.main != null)
            {
                speedPopupText.transform.rotation = Camera.main.transform.rotation;
            }
        }
        else
        {
            speedPopupText.text = ""; // Hide completely when timer is done
        }
    }
}
public class KnockbackMessage 
{
    public float x;
    public float z;
}