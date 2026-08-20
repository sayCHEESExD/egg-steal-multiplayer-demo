import { Room, Client } from "@colyseus/core";
import { MyRoomState, Player } from "./schema/MyRoomState";

export class MyRoom extends Room<MyRoomState> {
  maxClients = 10;

  onCreate (options: any) {
    this.setState(new MyRoomState());

    // We will listen for movement inputs from Unity later
    this.onMessage("move", (client, data) => {
        const player = this.state.players.get(client.sessionId);
        if (player) {
            player.x = data.x;
            player.y = data.y;
            player.z = data.z;
        }
    });
  }

  onJoin (client: Client, options: any) {
    console.log(client.sessionId, "joined!");
    
    // Create a new player and add it to the state
    const player = new Player();
    this.state.players.set(client.sessionId, player);
  }

  onLeave (client: Client, consented: boolean) {
    console.log(client.sessionId, "left!");
    
    // Remove the player from the state
    this.state.players.delete(client.sessionId);
  }
}