using Colyseus.Schema;

public partial class Pet : Schema 
{
    [Type(0, "string")] public string id = "";
    [Type(1, "string")] public string ownerId = "";
    [Type(2, "number")] public float x = 0;
    [Type(3, "number")] public float y = 0;
    [Type(4, "number")] public float z = 0;
    [Type(5, "number")] public float rotY = 0;
    [Type(6, "number")] public float biomeIndex = 0;
}