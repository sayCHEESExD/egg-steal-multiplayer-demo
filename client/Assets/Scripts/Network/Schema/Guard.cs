using Colyseus.Schema;

public partial class Guard : Schema 
{
    [Type(0, "number")] public float x = 0;
    [Type(1, "number")] public float y = 0;
    [Type(2, "number")] public float z = 0;
    [Type(3, "number")] public float rotY = 0;
    [Type(4, "string")] public string targetId = "";
    [Type(5, "number")] public float baseZ = 0;
    [Type(6, "number")] public float speed = 0;
}