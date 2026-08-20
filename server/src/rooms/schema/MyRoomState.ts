import { Schema, type, MapSchema } from "@colyseus/schema";

export class Player extends Schema {
    @type("number") x: number = 0;
    @type("number") y: number = 0;
    @type("number") z: number = 0;
    @type("number") rotY: number = 0; // <-- NEW
}

export class Egg extends Schema {
    @type("string") id: string = "";
    @type("number") x: number = 0;
    @type("number") y: number = 0;
    @type("number") z: number = 0;
    @type("string") carrierId: string = "";
}

export class MyRoomState extends Schema {
    @type({ map: Player }) players = new MapSchema<Player>();
    @type({ map: Egg }) eggs = new MapSchema<Egg>();
}