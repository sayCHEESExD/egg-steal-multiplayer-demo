using UnityEngine;
using Colyseus;
using Colyseus.Schema;
using System.Threading.Tasks;
using System.Collections.Generic;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }
    
    public Client client;
    public Room<MyRoomState> room;

    [Header("Spawning")]
    public GameObject playerPrefab;
    public GameObject eggPrefab;
    public GameObject[] guardPrefabs;
    public GameObject petPrefab;
    
    public Dictionary<string, GameObject> spawnedPlayers { get; private set; } = new Dictionary<string, GameObject>();
    public Dictionary<string, GameObject> spawnedEggs { get; private set; } = new Dictionary<string, GameObject>();
    public Dictionary<string, GameObject> spawnedGuards { get; private set; } = new Dictionary<string, GameObject>();
    public Dictionary<string, GameObject> spawnedPets { get; private set; } = new Dictionary<string, GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private async void Start()
    {
        client = new Client("ws://localhost:2567");
        await ConnectToServer();
    }

    private async Task ConnectToServer()
    {
        try
        {
            room = await client.JoinOrCreate<MyRoomState>("my_room");
            RegisterStateListeners();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to connect to server: {e.Message}");
        }
    }

    private void RegisterStateListeners()
    {
        var callbacks = Colyseus.Schema.Callbacks.Get(room);

        // --- PLAYERS ---
        callbacks.OnAdd(state => state.players, (string sessionId, Player player) => SpawnPlayer(sessionId, player));
        callbacks.OnRemove(state => state.players, (string sessionId, Player player) =>
        {
            if (spawnedPlayers.TryGetValue(sessionId, out GameObject playerObject))
            {
                Destroy(playerObject);
                spawnedPlayers.Remove(sessionId);
            }
        });

        // --- EGGS ---
        callbacks.OnAdd(state => state.eggs, (string eggId, Egg egg) => SpawnEgg(eggId, egg));
        callbacks.OnRemove(state => state.eggs, (string eggId, Egg egg) =>
        {
            if (spawnedEggs.TryGetValue(eggId, out GameObject eggObject))
            {
                Destroy(eggObject);
                spawnedEggs.Remove(eggId);
            }
        });

        // --- GUARDS ---
        callbacks.OnAdd(state => state.guards, (string guardId, Guard guard) => SpawnGuard(guardId, guard));
        callbacks.OnRemove(state => state.guards, (string guardId, Guard guard) =>
        {
            if (spawnedGuards.TryGetValue(guardId, out GameObject guardObject))
            {
                Destroy(guardObject);
                spawnedGuards.Remove(guardId);
            }
        });

        // --- PETS (NEW) ---
        callbacks.OnAdd(state => state.pets, (string petId, Pet pet) => SpawnPet(petId, pet));
        callbacks.OnRemove(state => state.pets, (string petId, Pet pet) =>
        {
            if (spawnedPets.TryGetValue(petId, out GameObject petObject))
            {
                Destroy(petObject);
                spawnedPets.Remove(petId);
            }
        });

        // --- INITIAL STATE ---
        room.OnStateChange += (state, isFirstState) =>
        {
            if (isFirstState)
            {
                state.players.ForEach((sessionId, player) => SpawnPlayer(sessionId, player));
                state.eggs.ForEach((eggId, egg) => SpawnEgg(eggId, egg));
                state.guards.ForEach((guardId, guard) => SpawnGuard(guardId, guard));
                state.pets.ForEach((petId, pet) => SpawnPet(petId, pet));
            }
        };
    }

    private void SpawnPlayer(string sessionId, Player player)
    {
        if (spawnedPlayers.ContainsKey(sessionId)) return;

        Vector3 spawnPosition = new Vector3(player.x, player.y, player.z);
        if (playerPrefab != null)
        {
            GameObject newPlayer = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
            NetworkPlayer netPlayer = newPlayer.GetComponent<NetworkPlayer>();
            if (netPlayer != null)
            {
                netPlayer.isLocalPlayer = (sessionId == room.SessionId);
                netPlayer.serverState = player;
            }
            spawnedPlayers.Add(sessionId, newPlayer);
        }
    }

    private void SpawnEgg(string eggId, Egg egg)
    {
        if (spawnedEggs.ContainsKey(eggId)) return;

        Vector3 spawnPosition = new Vector3(egg.x, egg.y, egg.z);
        if (eggPrefab != null)
        {
            GameObject newEgg = Instantiate(eggPrefab, spawnPosition, Quaternion.identity);
            NetworkEgg netEgg = newEgg.AddComponent<NetworkEgg>();
            netEgg.serverState = egg;
            netEgg.eggId = eggId;
            spawnedEggs.Add(eggId, newEgg);
        }
    }

    private void SpawnGuard(string guardId, Guard guard)
    {
        if (spawnedGuards.ContainsKey(guardId)) return;

        Vector3 spawnPosition = new Vector3(guard.x, guard.y, guard.z);
        int bIndex = (int)guard.biomeIndex;

        // Select correct prefab, fallback to index 0 if missing
        GameObject prefabToUse = null;
        if (guardPrefabs != null && guardPrefabs.Length > 0)
        {
            prefabToUse = (bIndex >= 0 && bIndex < guardPrefabs.Length) 
                ? guardPrefabs[bIndex] 
                : guardPrefabs[0];
        }

        if (prefabToUse != null)
        {
            GameObject newGuard = Instantiate(prefabToUse, spawnPosition, Quaternion.identity);
            NetworkGuard netGuard = newGuard.GetComponent<NetworkGuard>();
            if (netGuard == null) netGuard = newGuard.AddComponent<NetworkGuard>();
            netGuard.serverState = guard;
            spawnedGuards.Add(guardId, newGuard);
        }
    }

    // --- SPPAWN PET LOGIC ---
    private void SpawnPet(string petId, Pet pet)
    {
        if (spawnedPets.ContainsKey(petId)) return;
        
        Debug.Log($"Client received Pet: {petId} at {pet.x}, {pet.y}, {pet.z}");

        Vector3 spawnPosition = new Vector3(pet.x, pet.y, pet.z);
        if (petPrefab != null)
        {
            GameObject newPet = Instantiate(petPrefab, spawnPosition, Quaternion.identity);
            NetworkPet netPet = newPet.AddComponent<NetworkPet>();
            netPet.serverState = pet;
            spawnedPets.Add(petId, newPet);
        }
    }

    private void Update()
    {
        if (room != null && room.State != null && room.State.pets != null)
        {
            // Forcefully catch any pets that the event listeners missed
            room.State.pets.ForEach((petId, pet) => 
            {
                if (!spawnedPets.ContainsKey(petId))
                {
                    Debug.LogWarning($"Callback missed! Force spawning pet: {petId}");
                    SpawnPet(petId, pet);
                }
            });
        }
    }

    public GameObject GetSpawnedPlayer(string sessionId)
    {
        if (spawnedPlayers.TryGetValue(sessionId, out GameObject playerObj))
        {
            return playerObj;
        }
        return null;
    }
}