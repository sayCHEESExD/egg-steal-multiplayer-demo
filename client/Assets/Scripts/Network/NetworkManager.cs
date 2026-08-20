using UnityEngine;
using Colyseus;
using Colyseus.Schema; // <-- Added to access the Schema base class
using System.Threading.Tasks;

// 1. We create a temporary empty state class to satisfy the compiler constraint.
public class MyRoomState : Schema 
{
}

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }
    
    public Client client;
    
    // 2. Use MyRoomState instead of dynamic
    public Room<MyRoomState> room;

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
            Debug.Log("Connecting to Colyseus Server...");
            
            // 3. Use MyRoomState here as well
            room = await client.JoinOrCreate<MyRoomState>("my_room");
            
            Debug.Log($"Successfully joined room: {room.Name} with Session ID: {room.SessionId}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to connect to server: {e.Message}");
        }
    }
}