using Colyseus.Schema;

public partial class MyRoomState : Schema 
{
    [Type(0, "map", typeof(MapSchema<Player>))]
    public MapSchema<Player> players = new MapSchema<Player>();

    [Type(1, "map", typeof(MapSchema<Egg>))]
    public MapSchema<Egg> eggs = new MapSchema<Egg>();
}