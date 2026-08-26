// MyRoom.ts
import { Room, Client } from "colyseus";
import { MyRoomState, Player, Egg, Guard, Pet, Treadmill } from "./schema/MyRoomState.js";

export class MyRoom extends Room<{ state: MyRoomState }> {
  maxClients = 4;
  availableBases = [0, 1, 2, 3];
  
  // Adjusted base positions for a Safe Zone centered at 0,0
  basePositions = [
      { x: -70, z: -20 }, // Player 1
      { x: -23, z: -20 }, // Player 2
      { x: 23, z: -20 },  // Player 3
      { x: 70, z: -20 }   // Player 4
  ];

  // Biome Z-centers (100 to 800)
  biomeCenters = [100, 200, 300, 400, 500, 600, 700, 800];

  coinTimer: number = 0;

  onCreate (options: any) {
    this.state = new MyRoomState();

    // Populate Biomes
    let eggCounter = 0;
    this.biomeCenters.forEach((centerZ, index) => {
        const isEven = index % 2 === 0;
        const guardStartX = isEven ? 100 : -100; 
        const guardStartZ = centerZ + 1;// Stationed near the top wall of the biome

        // 1. Spawn 1 guard per biome
        const guard = new Guard();
        guard.x = guardStartX;
        guard.y = 0.5;
        guard.z = guardStartZ;
        guard.baseX = guardStartX; // Save X for AI return
        guard.baseZ = guardStartZ; // Save Z for AI return
        guard.speed = 3.0 + (index * 1.5)*2; 
        guard.biomeIndex = index;
        this.state.guards.set("guard_" + index, guard);

        // 2. Spawn 3 eggs tightly nested next to the guard

        for (let i = 0; i < 3; i++) {
            const egg = new Egg();
            egg.id = "egg_" + eggCounter++;
            
            // Create a small triangular nest shape slightly to the left/right of the guard
            let xOffset = 0;
            let zOffset = 0;
            if (i === 0) { xOffset = isEven ? -8 : 8; zOffset = 0; }
            if (i === 1) { xOffset = isEven ? -11 : 11; zOffset = 4; }
            if (i === 2) { xOffset = isEven ? -11 : 11; zOffset = -4; }

            egg.baseX = guardStartX + xOffset;
            egg.baseZ = guardStartZ + zOffset;
            
            egg.x = egg.baseX;
            egg.y = 0.5;
            egg.z = egg.baseZ;
            egg.biomeIndex = index;
            this.state.eggs.set(egg.id, egg);
        }
    });

    // Spawn 1 Treadmill per base
    this.basePositions.forEach((basePos, index) => {
        const tm = new Treadmill();
        tm.id = "treadmill_" + index;
        tm.x = basePos.x; // Offset slightly from base center
        tm.y = -0.8; 
        tm.z = basePos.z + 30;
        this.state.treadmills.set(tm.id, tm);
    });

    this.onMessage("interact_treadmill", (client, data) => {
        const player = this.state.players.get(client.sessionId);
        const tm = this.state.treadmills.get(data.id);
        
        if (player && tm) {
            if (tm.occupantId === "") {
                // Get ON if close enough
                const dx = player.x - tm.x;
                const dz = player.z - tm.z;
                if (Math.sqrt(dx * dx + dz * dz) < 4.0) {
                    tm.occupantId = client.sessionId;
                    player.x = tm.x; // Snap player to treadmill
                    player.z = tm.z;
                    player.rotY = 0; // Face forward
                }
            } else if (tm.occupantId === client.sessionId) {
                // Get OFF
                tm.occupantId = "";
            }
        }
    });

    this.onMessage("move", (client, data) => {
        const player = this.state.players.get(client.sessionId);
        if (player) {
            player.x = data.x;
            
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
        console.log(`[DELIVERY] Received request from ${client.sessionId}`);
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
            const dist = Math.sqrt(dx * dx + dz * dz);
            
            console.log(`[DELIVERY] Player distance to base center: ${dist.toFixed(2)} meters`);

            if (dist < 20.0) { 
                console.log(`[DELIVERY] ACCEPTED! Egg ${carriedEgg.id} is now hatching.`);
                
                carriedEgg.state = 2; // Hatching state
                carriedEgg.carrierId = ""; // Dropped on the ground
                carriedEgg.ownerId = client.sessionId; // Claimed by the base owner
                carriedEgg.hatchProgress = 5000; // Reset timer
                
                // Drop the egg at the player's exact feet instead of base center so they don't stack
                carriedEgg.x = player.x;
                carriedEgg.z = player.z;
                carriedEgg.y = 0; 
            } else {
                console.log(`[DELIVERY] DENIED! Player is too far from base (${dist.toFixed(2)} > 20.0)`);
            }
        } else {
            console.log(`[DELIVERY] DENIED! Player is not carrying an egg.`);
        }
    });

    this.onMessage("pickup_egg", (client, data) => {
        const egg = this.state.eggs.get(data.eggId);
        
        // Allow pickup if it is completely free OR if it is currently hatching (state 2)
        if (egg && (egg.carrierId === "" || egg.state === 2)) {
            egg.carrierId = client.sessionId;
            egg.state = 1; // Set back to carried state
            egg.ownerId = ""; // Clear the owner so it is successfully stolen
        }
    });

    this.onMessage("jump", (client, message) => {
        const player = this.state.players.get(client.sessionId);
        
        // Simple ground check (assuming Y = 1.0 is your floor based on the enclosure setup)
        if (player && player.y <= 0.1) { 
            // Give the player upward velocity (adjust this number for jump height)
            player.velocityY = 15; 
        }
    });

    this.setSimulationInterval((deltaTime) => {
        this.updateGame(deltaTime);
    }, 1000 / 30);

    this.onMessage("upgrade_speed", (client, data) => {
        const player = this.state.players.get(client.sessionId);
        if (player) {
            // Base cost is 10, doubles for each upgrade level past 5.0 speed
            const currentLevel = player.moveSpeed - 10;
            const cost = 10 * Math.pow(2, currentLevel); 

            if (player.coins >= cost) {
                player.coins -= cost;
                player.moveSpeed += 2; // Increase speed by 1
                console.log(`${client.sessionId} upgraded speed to ${player.moveSpeed}`);
            }
        }
    });
  }

  updateGame(deltaTime: number) {
      // 0. Process Player Gravity and Jumping Physics
      this.state.players.forEach(player => {
          if (player.velocityY === undefined) {
              player.velocityY = 0;
          }

          player.velocityY -= 40 * (deltaTime / 1000); 
          player.y += player.velocityY * (deltaTime / 1000);

          if (player.y < 0.0) {
              player.y = 0.0;
              player.velocityY = 0;
          }
      });

      // 1. Process Hatching Eggs
      this.state.eggs.forEach(egg => {
          if (egg.state === 2) {
              egg.hatchProgress -= deltaTime;
              
              if (egg.hatchProgress <= 0) {
                  console.log(`[HATCH] Timer finished for egg: ${egg.id}`);
                  
                  const player = this.state.players.get(egg.ownerId);
                  
                  if (player) {
                      player.score += 1;
                      
                      const pet = new Pet();
                      pet.id = "pet_" + Date.now() + "_" + Math.floor(Math.random() * 1000);
                      pet.ownerId = egg.ownerId;
                      pet.biomeIndex = egg.biomeIndex; 
                      
                      const myBase = this.basePositions[player.baseIndex];
                      
                      // Spawn somewhere randomly within the new enclosure
                      pet.x = myBase.x + (Math.random() * 20 - 10); 
                      pet.y = 1.0; 
                      pet.z = myBase.z + (Math.random() * 20 - 10);
                      
                      this.state.pets.set(pet.id, pet);
                      console.log(`[HATCH] SUCCESS! Created Pet ${pet.id} at X:${pet.x}, Z:${pet.z}`);
                  } else {
                      console.log(`[HATCH ERROR] Could not find player with ID: ${egg.ownerId}`);
                  }

                  // Reset the egg back to its EXACT fixed nest for the next cycle
                  try {
                      egg.state = 0;
                      egg.ownerId = "";
                      egg.x = egg.baseX;
                      egg.z = egg.baseZ;
                      console.log(`[HATCH] Egg ${egg.id} reset to fixed nest.`);
                  } catch (e) {
                      console.log(`[HATCH ERROR] Failed to reset egg: ${e instanceof Error ? e.message : String(e)}`);
                  }
              }
          }
      });

      // 2. Guard AI
      this.state.guards.forEach(guard => {
          let targetPlayer: Player | null = null;
          let stolenEgg: Egg | null = null;

          // 1. Find if an egg from this guard's biome is being carried
          this.state.eggs.forEach((egg) => {
              if (egg.biomeIndex === guard.biomeIndex && egg.state === 1 && egg.carrierId !== "") {
                  const p = this.state.players.get(egg.carrierId);
                  // Safe zone check: Players are safe if Z < 50
                  if (p && p.z >= 50) {
                      targetPlayer = p;
                      stolenEgg = egg;
                  }
              }
          });

          if (targetPlayer && stolenEgg) {
              const dx = targetPlayer.x - guard.x;
              const dz = targetPlayer.z - guard.z;
              const dist = Math.sqrt(dx * dx + dz * dz);
              
              if (dist < 2.5) {
                  // Guard catches the player! Reset egg to its fixed nest.
                  stolenEgg.carrierId = "";
                  stolenEgg.state = 0; 
                  stolenEgg.x = stolenEgg.baseX;
                  stolenEgg.y = 0.5;
                  stolenEgg.z = stolenEgg.baseZ;
              } else if (dist > 0.1) {
                  // Chase target
                  const moveAmt = guard.speed * (deltaTime / 1000);
                  guard.x += (dx / dist) * moveAmt;
                  guard.z += (dz / dist) * moveAmt;
                  guard.rotY = Math.atan2(dx, dz) * (180 / Math.PI);
              }
          } else {
              // No target, return to exact wall position instead of X=0
              const dx = guard.baseX - guard.x; 
              const dz = guard.baseZ - guard.z;
              const dist = Math.sqrt(dx * dx + dz * dz);
              
              if (dist > 0.1) {
                  const moveAmt = guard.speed * (deltaTime / 1000);
                  if (moveAmt > dist) {
                      guard.x = guard.baseX;
                      guard.z = guard.baseZ;
                  } else {
                      guard.x += (dx / dist) * moveAmt;
                      guard.z += (dz / dist) * moveAmt;
                      guard.rotY = Math.atan2(dx, dz) * (180 / Math.PI);
                  }
              }
          }
      });

      // 3. Pet AI & Passive Income
      // Give coins every 1000ms (1 second)
      this.coinTimer += deltaTime;
      const giveCoins = this.coinTimer >= 1000;
      if (giveCoins) this.coinTimer -= 1000;

      this.state.pets.forEach(pet => {
          const owner = this.state.players.get(pet.ownerId);
          if (owner) {
              // Income generation based on rarity (biomeIndex)
              if (giveCoins) {
                  const income = 1 + (pet.biomeIndex * 2); 
                  owner.coins += income;
              }

              // Wandering AI within the 40x40 pen
              pet.idleTimer -= deltaTime;
              if (pet.idleTimer <= 0) {
                  const myBase = this.basePositions[owner.baseIndex];
                  
                  // Pick a random spot inside a 38x38 area (leaves a 1 unit margin)
                  pet.targetX = myBase.x + (Math.random() * 38 - 19);
                  pet.targetZ = myBase.z + (Math.random() * 38 - 19);
                  
                  pet.idleTimer = 2000 + Math.random() * 4000;
              }

              // Move towards target
              const dx = pet.targetX - pet.x;
              const dz = pet.targetZ - pet.z;
              const dist = Math.sqrt(dx * dx + dz * dz);
              
              if (dist > 0.1) {
                  const moveAmt = 1.5 * (deltaTime / 1000); // Walk speed
                  if (moveAmt > dist) {
                      pet.x = pet.targetX;
                      pet.z = pet.targetZ;
                  } else {
                      pet.x += (dx / dist) * moveAmt;
                      pet.z += (dz / dist) * moveAmt;
                      pet.rotY = Math.atan2(dx, dz) * (180 / Math.PI);
                  }
              }
          }
      });

      // 4. Treadmill Logic (Safe farming: 2 coins per second)
      this.state.treadmills.forEach(tm => {
          if (tm.occupantId !== "" && giveCoins) {
              const p = this.state.players.get(tm.occupantId);
              if (p) p.coins += 2; 
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

  onLeave (client: Client, code?: number) {
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