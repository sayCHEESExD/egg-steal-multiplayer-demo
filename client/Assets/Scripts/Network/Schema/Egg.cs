using Colyseus.Schema;

public partial class Egg : Schema 
{
    [Type(0, "string")]
    public string id = "";

    [Type(1, "number")]
    public float x = 0;

    [Type(2, "number")]
    public float y = 0;

    [Type(3, "number")]
    public float z = 0;

    [Type(4, "string")]
    public string carrierId = "";
}