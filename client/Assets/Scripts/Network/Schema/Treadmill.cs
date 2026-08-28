using Colyseus.Schema;

public partial class Treadmill : Schema
{
    [Type(0, "string")] public string id = default(string);
    [Type(1, "number")] public float x = default(float);
    [Type(2, "number")] public float y = default(float);
    [Type(3, "number")] public float z = default(float);
    [Type(4, "string")] public string occupantId = default(string);
    [Type(5, "string")] public string ownerId = default(string);
    [Type(6, "number")] public float level = default(float);
    [Type(7, "number")] public float upgradeCost = default(float);
}