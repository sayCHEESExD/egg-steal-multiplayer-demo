// MyRoomState.ts
import { Schema, type, MapSchema } from "@colyseus/schema";

export class Player extends Schema {
    @type("number") x: number = 0;
    @type("number") y: number = 0;
    @type("number") z: number = 0;
    @type("number") rotY: number = 0;
    @type("number") baseIndex: number = 0;
    @type("number") score: number = 0;
    @type("number") coins: number = 0;
    @type("number") moveSpeed: number = 10;
    @type("number") enclosureLevel: number = 1;
    @type("number") petCapacity: number = 3; 
    @type("number") enclosureUpgradeCost: number = 100;

    velocityY: number = 0;
}

export class Egg extends Schema {
    @type("string") id: string = "";
    @type("number") x: number = 0;
    @type("number") y: number = 0;
    @type("number") z: number = 0;
    @type("string") carrierId: string = "";
    @type("number") state: number = 0;
    @type("number") hatchProgress: number = 0;
    @type("string") ownerId: string = "";
    @type("number") biomeIndex: number = 0;

    // Unsynced properties to remember the exact nest position
    baseX: number = 0; 
    baseZ: number = 0; 
}

export class Guard extends Schema {
    @type("number") x: number = 0;
    @type("number") y: number = 0;
    @type("number") z: number = 0;
    @type("number") rotY: number = 0;
    @type("string") targetId: string = "";
    @type("number") baseZ: number = 0;   // The center of this guard's biome
    @type("number") speed: number = 0;   // Speed increases in further biomes
    @type("number") biomeIndex: number = 0;

    // Unsynced property to remember wall position
    baseX: number = 0; 
}

export class Pet extends Schema {
    @type("string") id: string = "";
    @type("string") ownerId: string = "";
    @type("number") x: number = 0;
    @type("number") y: number = 0;
    @type("number") z: number = 0;
    @type("number") rotY: number = 0;
    @type("number") biomeIndex: number = 0;
    
    // Non-synced properties for server-side AI logic
    targetX: number = 0;
    targetZ: number = 0;
    idleTimer: number = 0;
}

export class Treadmill extends Schema {
    @type("string") id: string = "";
    @type("number") x: number = 0;
    @type("number") y: number = 0;
    @type("number") z: number = 0;
    @type("string") occupantId: string = "";
    @type("string") ownerId: string = ""; 
    @type("number") level: number = 1; 
    @type("number") upgradeCost: number = 50;
}

export class MyRoomState extends Schema {
    @type({ map: Player }) players = new MapSchema<Player>();
    @type({ map: Egg }) eggs = new MapSchema<Egg>();
    @type({ map: Guard }) guards = new MapSchema<Guard>();
    @type({ map: Pet }) pets = new MapSchema<Pet>(); 
    @type({ map: Treadmill }) treadmills = new MapSchema<Treadmill>();
    @type("number") eggTimer: number = 300;
}