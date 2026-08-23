using Colyseus.Schema;

public partial class MyRoomState : Schema 
{
    [Type(0, "map", typeof(MapSchema<Player>))] public MapSchema<Player> players = new MapSchema<Player>();
    [Type(1, "map", typeof(MapSchema<Egg>))] public MapSchema<Egg> eggs = new MapSchema<Egg>();
    [Type(2, "map", typeof(MapSchema<Guard>))] public MapSchema<Guard> guards = new MapSchema<Guard>();
    [Type(3, "map", typeof(MapSchema<Pet>))] public MapSchema<Pet> pets = new MapSchema<Pet>(); // <-- NEW
    [Type(4, "map", typeof(MapSchema<Treadmill>))] public MapSchema<Treadmill> treadmills = new MapSchema<Treadmill>();
}