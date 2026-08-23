using UnityEngine;
using Colyseus.Schema; // Added this

public class NetworkTreadmill : MonoBehaviour
{
    public string treadmillId;
    [HideInInspector] public Treadmill serverState;

    private void Update()
    {
        if (serverState == null) return;
        transform.position = new Vector3(serverState.x, serverState.y, serverState.z);
    }
}