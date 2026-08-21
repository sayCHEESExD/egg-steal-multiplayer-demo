import { Room, Client } from "@colyseus/core";
import { MyRoomState, Player, Egg, Guard } from "./schema/MyRoomState";

export class MyRoom extends Room<MyRoomState> {
  maxClients = 4;
  availableBases = [0, 1, 2, 3];
  
  // Adjusted base positions for a Safe Zone centered at 0,0
  basePositions = [
      { x: -15, z: -40 },
      { x: -5, z: -40 },
      { x: 5, z: -40 },
      { x: 15, z: -40 }
  ];

  // Biome Z-centers (100 to 800)
  biomeCenters = [100, 200, 300, 400, 500, 600, 700, 800];

  onCreate (options: any) {
    this.setState(new MyRoomState());

    // Populate Biomes
    let eggCounter = 0;
    this.biomeCenters.forEach((centerZ, index) => {
        // 1. Spawn 3 eggs per biome
        for (let i = 0; i < 3; i++) {
            const egg = new Egg();
            egg.id = "egg_" + eggCounter++;
            egg.x = (Math.random() * 40) - 20; // Spread across X axis
            egg.y = 0.5;
            egg.z = centerZ + ((Math.random() * 40) - 20); // Spread within biome Z
            this.state.eggs.set(egg.id, egg);
        }

        // 2. Spawn 1 guard per biome
        const guard = new Guard();
        guard.x = 0;
        guard.y = 0.5;
        guard.z = centerZ;
        guard.baseZ = centerZ;
        guard.speed = 3.0 + (index * 1.5); // Speed increases per biome
        this.state.guards.set("guard_" + index, guard);
    });

    this.onMessage("move", (client, data) => {
        const player = this.state.players.get(client.sessionId);
        if (player) {
            player.x = data.x;
            player.y = data.y;
            player.z = data.z;
            if (data.rotY !== undefined) player.rotY = data.rotY;
        }
    });

    this.onMessage("pickup_egg", (client, data) => {
        const egg = this.state.eggs.get(data.eggId);
        const player = this.state.players.get(client.sessionId);

        let isAlreadyCarrying = false;
        this.state.eggs.forEach((e) => {
            if (e.carrierId === client.sessionId) isAlreadyCarrying = true;
        });

        if (egg && player && egg.state === 0 && !isAlreadyCarrying) {
            const dx = egg.x - player.x;
            const dz = egg.z - player.z;
            if (Math.sqrt(dx * dx + dz * dz) < 3.0) {
                egg.state = 1; 
                egg.carrierId = client.sessionId;
            }
        }
    });

    this.onMessage("deliver_egg", (client, data) => {
        const player = this.state.players.get(client.sessionId);
        if (!player) return;

        let carriedEgg: Egg = null;
        this.state.eggs.forEach((e) => {
            if (e.carrierId === client.sessionId) carriedEgg = e;
        });

        if (carriedEgg) {
            const myBase = this.basePositions[player.baseIndex];
            const dx = player.x - myBase.x;
            const dz = player.z - myBase.z;

            if (Math.sqrt(dx * dx + dz * dz) < 8.0) { // Increased delivery radius slightly for enclosures
                carriedEgg.state = 2; 
                carriedEgg.carrierId = "";
                carriedEgg.ownerId = client.sessionId;
                carriedEgg.hatchProgress = 5000;
                
                // Place inside enclosure
                carriedEgg.x = myBase.x;
                carriedEgg.z = myBase.z;
            }
        }
    });

    this.setSimulationInterval((deltaTime) => {
        this.updateGame(deltaTime);
    }, 1000 / 30);
  }

  updateGame(deltaTime: number) {
      // 1. Process Hatching Eggs
      this.state.eggs.forEach(egg => {
          if (egg.state === 2) {
              egg.hatchProgress -= deltaTime;
              if (egg.hatchProgress <= 0) {
                  const player = this.state.players.get(egg.ownerId);
                  if (player) player.score += 1;

                  // Find original biome center based on ID (simplified logic)
                  const idNum = parseInt(egg.id.split('_')[1]);
                  const biomeIndex = Math.floor(idNum / 3);
                  const centerZ = this.biomeCenters[biomeIndex];

                  egg.state = 0;
                  egg.ownerId = "";
                  egg.x = (Math.random() * 40) - 20;
                  egg.z = centerZ + ((Math.random() * 40) - 20);
              }
          }
      });

      // 2. Guard AI Logic (Per Biome)
      this.state.guards.forEach((guard, key) => {
          let currentTarget = "";
          let minTargetDist = 9999;

          // Find egg thieves in THIS guard's territory (+/- 50 Z of baseZ)
          this.state.eggs.forEach(egg => {
              if (egg.state === 1 && egg.carrierId !== "") {
                  const target = this.state.players.get(egg.carrierId);
                  if (target && Math.abs(target.z - guard.baseZ) <= 50) {
                      const dist = Math.sqrt(Math.pow(target.x - guard.x, 2) + Math.pow(target.z - guard.z, 2));
                      if (dist < minTargetDist) {
                          minTargetDist = dist;
                          currentTarget = egg.carrierId;
                      }
                  }
              }
          });

          guard.targetId = currentTarget;

          if (currentTarget !== "") {
              const target = this.state.players.get(currentTarget);
              if (target) {
                  const dx = target.x - guard.x;
                  const dz = target.z - guard.z;
                  const dist = Math.sqrt(dx * dx + dz * dz);

                  if (dist > 1.5) {
                      const moveAmt = guard.speed * (deltaTime / 1000);
                      guard.x += (dx / dist) * moveAmt;
                      guard.z += (dz / dist) * moveAmt;
                      guard.rotY = Math.atan2(dx, dz) * (180 / Math.PI);
                  } else {
                      // Caught player
                      this.state.eggs.forEach(egg => {
                          if (egg.carrierId === currentTarget) {
                              egg.state = 0;
                              egg.carrierId = "";
                              egg.x = target.x;
                              egg.z = target.z;
                          }
                      });
                  }
              }
          } else {
              // Return to biome center
              const dx = 0 - guard.x;
              const dz = guard.baseZ - guard.z;
              const dist = Math.sqrt(dx * dx + dz * dz);
              
              if (dist > 0.1) {
                  const moveAmt = (guard.speed * 0.5) * (deltaTime / 1000); // Walk back slower
                  guard.x += (dx / dist) * moveAmt;
                  guard.z += (dz / dist) * moveAmt;
                  guard.rotY = Math.atan2(dx, dz) * (180 / Math.PI);
              }
          }
      });
  }

  onJoin (client: Client, options: any) {
    const player = new Player();
    player.baseIndex = this.availableBases.shift() ?? 0; 
    
    const spawnPos = this.basePositions[player.baseIndex];
    player.x = spawnPos.x;
    player.z = spawnPos.z;

    this.state.players.set(client.sessionId, player);
  }

  onLeave (client: Client, consented: boolean) {
    const player = this.state.players.get(client.sessionId);
    if (player) this.availableBases.push(player.baseIndex);
    
    this.state.eggs.forEach((e) => {
        if (e.carrierId === client.sessionId) {
            e.state = 0;
            e.carrierId = "";
        }
    });
    this.state.players.delete(client.sessionId);
  }
}