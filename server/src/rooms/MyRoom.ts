import { Room, Client } from "@colyseus/core";
import { MyRoomState, Player, Egg } from "./schema/MyRoomState";

export class MyRoom extends Room<MyRoomState> {
  maxClients = 10;

  onCreate (options: any) {
    this.setState(new MyRoomState());

    // --- NEW: Spawn 3 Eggs in the center ---
    for (let i = 0; i < 3; i++) {
        const egg = new Egg();
        egg.id = "egg_" + i;
        // Spread them out slightly in the center
        egg.x = (i - 1) * 3; 
        egg.y = 0.5; // Slightly above ground
        egg.z = 0;
        egg.carrierId = "";
        
        this.state.eggs.set(egg.id, egg);
    }
    // ---------------------------------------

    this.onMessage("move", (client, data) => {
        const player = this.state.players.get(client.sessionId);
        if (player) {
            player.x = data.x;
            player.y = data.y;
            player.z = data.z;
            if (data.rotY !== undefined) player.rotY = data.rotY; // <-- NEW
        }
    });
    this.onMessage("pickup_egg", (client, data) => {
        const egg = this.state.eggs.get(data.eggId);
        const player = this.state.players.get(client.sessionId);

        // Check if this player is ALREADY carrying any egg
        let isAlreadyCarrying = false;
        this.state.eggs.forEach((e) => {
            if (e.carrierId === client.sessionId) {
                isAlreadyCarrying = true;
            }
        });

        // Only allow pickup if they don't have one
        if (egg && player && egg.carrierId === "" && !isAlreadyCarrying) {
            const dx = egg.x - player.x;
            const dz = egg.z - player.z;
            const distance = Math.sqrt(dx * dx + dz * dz);

            if (distance < 3.0) {
                egg.carrierId = client.sessionId;
                console.log(`${client.sessionId} picked up ${data.eggId}`);
            }
        }
    });
  }

  onJoin (client: Client, options: any) {
    console.log(client.sessionId, "joined!");
    const player = new Player();
    this.state.players.set(client.sessionId, player);
  }

  onLeave (client: Client, consented: boolean) {
    console.log(client.sessionId, "left!");
    this.state.players.delete(client.sessionId);
  }
}