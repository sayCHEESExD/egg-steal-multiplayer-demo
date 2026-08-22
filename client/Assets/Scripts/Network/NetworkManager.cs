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
    public GameObject[] eggPrefabs;
    public GameObject[] guardPrefabs;
    public GameObject[] petPrefabs;
    
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

        // --- PETS ---
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
        int bIndex = (int)egg.biomeIndex;

        GameObject prefabToUse = null;
        if (eggPrefabs != null && eggPrefabs.Length > 0)
        {
            prefabToUse = (bIndex >= 0 && bIndex < eggPrefabs.Length) ? eggPrefabs[bIndex] : eggPrefabs[0];
        }

        if (prefabToUse != null)
        {
            GameObject newEgg = Instantiate(prefabToUse, spawnPosition, Quaternion.identity);
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

    private void SpawnPet(string petId, Pet pet)
    {
        if (spawnedPets.ContainsKey(petId)) return;

        Vector3 spawnPosition = new Vector3(pet.x, pet.y, pet.z);
        int bIndex = (int)pet.biomeIndex;

        GameObject prefabToUse = null;
        if (petPrefabs != null && petPrefabs.Length > 0)
        {
            prefabToUse = (bIndex >= 0 && bIndex < petPrefabs.Length) ? petPrefabs[bIndex] : petPrefabs[0];
        }

        if (prefabToUse != null)
        {
            GameObject newPet = Instantiate(prefabToUse, spawnPosition, Quaternion.identity);
            
            // 1. ADD TO DICTIONARY IMMEDIATELY
            // This guarantees we never duplicate the model, even if the script below fails!
            spawnedPets.Add(petId, newPet);

            // 2. Safely get or add the component
            NetworkPet netPet = newPet.GetComponent<NetworkPet>();
            if (netPet == null) 
            {
                netPet = newPet.AddComponent<NetworkPet>();
            }
            
            // 3. Only apply server data if the component successfully attached
            if (netPet != null)
            {
                netPet.serverState = pet;
            }
            else
            {
                Debug.LogError($"[Fix] Failed to attach NetworkPet script to {prefabToUse.name}!");
            }
        }
    }

    private void Update()
    {
        if (room != null && room.State != null && room.State.pets != null)
        {
            room.State.pets.ForEach((petId, pet) => 
            {
                if (!spawnedPets.ContainsKey(petId))
                {
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