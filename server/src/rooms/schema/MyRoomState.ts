import { Schema, type, MapSchema } from "@colyseus/schema";

// Define what data makes up a Player
export class Player extends Schema {
    @type("number") x: number = 0;
    @type("number") y: number = 0;
    @type("number") z: number = 0;
}

// Define the overall room state
export class MyRoomState extends Schema {
    // A map (dictionary) of all players currently in the room
    @type({ map: Player }) players = new MapSchema<Player>();
}