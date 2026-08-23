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
    public GameObject treadmillPrefab;
    public GameObject[] eggPrefabs;
    public GameObject[] guardPrefabs;
    public GameObject[] petPrefabs;
    
    public Dictionary<string, GameObject> spawnedPlayers { get; private set; } = new Dictionary<string, GameObject>();
    public Dictionary<string, GameObject> spawnedTreadmills { get; private set; } = new Dictionary<string, GameObject>();
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
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to connect to server: {e.Message}");
        }
    }

    private void Update()
    {
        if (room == null || room.State == null) return;

        SyncMap(room.State.players, spawnedPlayers, SpawnPlayer);
        SyncMap(room.State.treadmills, spawnedTreadmills, SpawnTreadmill);
        SyncMap(room.State.eggs, spawnedEggs, SpawnEgg);
        SyncMap(room.State.guards, spawnedGuards, SpawnGuard);
        SyncMap(room.State.pets, spawnedPets, SpawnPet);
    }

    private void SyncMap<T>(MapSchema<T> serverMap, Dictionary<string, GameObject> localMap, System.Action<string, T> spawnMethod)
    {
        if (serverMap == null) return;

        HashSet<string> serverKeys = new HashSet<string>();
        
        serverMap.ForEach((key, item) => 
        {
            serverKeys.Add(key);
            if (!localMap.ContainsKey(key))
            {
                spawnMethod(key, item);
            }
        });

        List<string> keysToRemove = new List<string>();
        foreach (var localKey in localMap.Keys)
        {
            if (!serverKeys.Contains(localKey))
            {
                keysToRemove.Add(localKey);
            }
        }

        foreach (var key in keysToRemove)
        {
            if (localMap.TryGetValue(key, out GameObject obj))
            {
                Destroy(obj);
                localMap.Remove(key);
            }
        }
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

    private void SpawnTreadmill(string treadmillId, Treadmill treadmill)
    {
        if (spawnedTreadmills.ContainsKey(treadmillId)) return;

        Vector3 spawnPosition = new Vector3(treadmill.x, treadmill.y, treadmill.z);
        if (treadmillPrefab != null)
        {
            // <-- CHANGED: Now respects your Prefab's rotation instead of forcing 0,0,0
            GameObject newTreadmill = Instantiate(treadmillPrefab, spawnPosition, treadmillPrefab.transform.rotation); 
            
            NetworkTreadmill netTm = newTreadmill.GetComponent<NetworkTreadmill>();
            if (netTm == null) netTm = newTreadmill.AddComponent<NetworkTreadmill>();
            netTm.serverState = treadmill;
            netTm.treadmillId = treadmillId;
            spawnedTreadmills.Add(treadmillId, newTreadmill);
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
            
            spawnedPets.Add(petId, newPet);

            NetworkPet netPet = newPet.GetComponent<NetworkPet>();
            if (netPet == null) 
            {
                netPet = newPet.AddComponent<NetworkPet>();
            }
            
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

    public GameObject GetSpawnedPlayer(string sessionId)
    {
        if (spawnedPlayers.TryGetValue(sessionId, out GameObject playerObj))
        {
            return playerObj;
        }
        return null;
    }
}