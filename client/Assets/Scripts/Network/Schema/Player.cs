using Colyseus.Schema;

public partial class Player : Schema 
{
    [Type(0, "number")] public float x = 0;
    [Type(1, "number")] public float y = 0;
    [Type(2, "number")] public float z = 0;
    [Type(3, "number")] public float rotY = 0;
    [Type(4, "number")] public float baseIndex = 0;
    [Type(5, "number")] public float score = 0;
    [Type(6, "number")] public float coins = 0;     
    [Type(7, "number")] public float moveSpeed = 5f;
}