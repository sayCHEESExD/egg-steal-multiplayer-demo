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
    public GameObject guardPrefab;
    
    public Dictionary<string, GameObject> spawnedPlayers { get; private set; } = new Dictionary<string, GameObject>();
    public Dictionary<string, GameObject> spawnedEggs { get; private set; } = new Dictionary<string, GameObject>();
    public Dictionary<string, GameObject> spawnedGuards { get; private set; } = new Dictionary<string, GameObject>();

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

        // --- INITIAL STATE ---
        room.OnStateChange += (state, isFirstState) =>
        {
            if (isFirstState)
            {
                state.players.ForEach((sessionId, player) => SpawnPlayer(sessionId, player));
                state.eggs.ForEach((eggId, egg) => SpawnEgg(eggId, egg));
                state.guards.ForEach((guardId, guard) => SpawnGuard(guardId, guard));
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
        if (guardPrefab != null)
        {
            GameObject newGuard = Instantiate(guardPrefab, spawnPosition, Quaternion.identity);
            NetworkGuard netGuard = newGuard.GetComponent<NetworkGuard>();
            
            if (netGuard == null) netGuard = newGuard.AddComponent<NetworkGuard>();
            
            netGuard.serverState = guard;
            spawnedGuards.Add(guardId, newGuard);
        }
    }

    // --- MISSING HELPER RE-ADDED ---
    public GameObject GetSpawnedPlayer(string sessionId)
    {
        if (spawnedPlayers.TryGetValue(sessionId, out GameObject playerObj))
        {
            return playerObj;
        }
        return null;
    }
}