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
    
    private Dictionary<string, GameObject> spawnedPlayers = new Dictionary<string, GameObject>();

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
            Debug.Log($"Successfully joined room: {room.Name} with Session ID: {room.SessionId}");

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

        // Listen for new players joining mid-game
        callbacks.OnAdd(state => state.players, (string sessionId, Player player) =>
        {
            SpawnPlayer(sessionId, player);
        });

        // Listen for players leaving
        callbacks.OnRemove(state => state.players, (string sessionId, Player player) =>
        {
            Debug.Log($"Player left: {sessionId}");
            if (spawnedPlayers.TryGetValue(sessionId, out GameObject playerObject))
            {
                Destroy(playerObject);
                spawnedPlayers.Remove(sessionId);
            }
        });

        // THE FIX: Wait for the first actual state payload from the server
        room.OnStateChange += (state, isFirstState) =>
        {
            if (isFirstState)
            {
                state.players.ForEach((sessionId, player) =>
                {
                    SpawnPlayer(sessionId, player);
                });
            }
        };
    }

    private void SpawnPlayer(string sessionId, Player player)
    {
        if (spawnedPlayers.ContainsKey(sessionId)) return;

        Debug.Log($"Spawning player: {sessionId}");
        Vector3 spawnPosition = new Vector3(player.x, player.y, player.z);
        
        if (playerPrefab != null)
        {
            GameObject newPlayer = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
            spawnedPlayers.Add(sessionId, newPlayer);
        }
        else
        {
            Debug.LogError("Player Prefab is missing in the NetworkManager Inspector!");
        }
    }
}