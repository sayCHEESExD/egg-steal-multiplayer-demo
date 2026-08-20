using Colyseus.Schema;

public partial class MyRoomState : Schema 
{
    [Type(0, "map", typeof(MapSchema<Player>))]
    public MapSchema<Player> players = new MapSchema<Player>();
}