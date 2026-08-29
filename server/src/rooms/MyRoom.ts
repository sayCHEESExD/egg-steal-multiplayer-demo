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
        const guardStartX = isEven ? 24 : -24; 
        const guardStartZ = centerZ + 35;

        // 1. Spawn 1 guard per biome
        const guard = new Guard();
        guard.x = guardStartX;
        guard.y = 0.5;
        guard.z = guardStartZ;
        guard.baseX = guardStartX; 
        guard.baseZ = guardStartZ; 
        guard.speed = 15.0 + (index * 5.0); 
        guard.biomeIndex = index;
        this.state.guards.set("guard_" + index, guard);

        // 2. Spawn 3 eggs tightly nested next to the guard
        for (let i = 0; i < 3; i++) {
            const egg = new Egg();
            egg.id = "egg_" + eggCounter++;
            
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
        tm.x = basePos.x;
        tm.y = -0.8; 
        tm.z = basePos.z + 30;
        tm.ownerId = ""; 
        tm.level = 1;
        tm.upgradeCost = 50;
        this.state.treadmills.set(tm.id, tm);
    });

    this.onMessage("mount_treadmill", (client, data) => {
        const player = this.state.players.get(client.sessionId);
        const tm = this.state.treadmills.get(data.id);
        
        if (player && tm && tm.ownerId === client.sessionId && tm.occupantId === "") {
            const dx = player.x - tm.x;
            const dz = player.z - tm.z;
            
            // Tight distance check
            if (Math.sqrt(dx * dx + dz * dz) < 1.2) {
                tm.occupantId = client.sessionId;
                player.x = tm.x; 
                player.z = tm.z;
                player.rotY = 0; 
            }
        }
    });

    // MISSING UNMOUNT HANDLER TO LET PLAYERS JUMP OFF
    this.onMessage("unmount_treadmill", (client, data) => {
        const player = this.state.players.get(client.sessionId);
        let activeTm: Treadmill | null = null;
        
        this.state.treadmills.forEach(tm => {
            if (tm.occupantId === client.sessionId) activeTm = tm;
        });

        if (player && activeTm) {
            activeTm.occupantId = "";
            player.velocityY = 15; // Makes the player pop off the treadmill
        }
    });

    this.onMessage("upgrade_treadmill", (client, data) => {
        const player = this.state.players.get(client.sessionId);
        const tm = this.state.treadmills.get(data.id);

        if (player && tm && tm.ownerId === client.sessionId) {
            if (player.coins >= tm.upgradeCost) {
                player.coins -= tm.upgradeCost;
                tm.level += 1;
                tm.upgradeCost = Math.floor(tm.upgradeCost * 2); 
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

        if (egg && player && !isAlreadyCarrying && (egg.carrierId === "" || egg.state === 2)) {
            const dx = egg.x - player.x;
            const dz = egg.z - player.z;
            
            if (Math.sqrt(dx * dx + dz * dz) < 3.5) {
                egg.carrierId = client.sessionId;
                egg.state = 1; 
                egg.ownerId = ""; 
            }
        }
    });

    this.onMessage("drop_egg", (client, data) => {
        let carriedEgg: Egg = null;
        this.state.eggs.forEach((e) => {
            if (e.carrierId === client.sessionId) carriedEgg = e;
        });

        if (carriedEgg) {
            const player = this.state.players.get(client.sessionId);
            carriedEgg.carrierId = "";
            carriedEgg.state = 0; 
            
            if (player) {
                carriedEgg.x = player.x;
                carriedEgg.z = player.z;
                carriedEgg.y = 0.5;
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
            const dist = Math.sqrt(dx * dx + dz * dz);
            
            if (dist < 20.0) { 
                // --- NEW CAPACITY CHECK ---
                let currentPetsAndEggs = 0;
                this.state.pets.forEach(p => { if (p.ownerId === client.sessionId) currentPetsAndEggs++; });
                this.state.eggs.forEach(e => { if (e.ownerId === client.sessionId && e.state === 2) currentPetsAndEggs++; });

                if (currentPetsAndEggs >= player.petCapacity) {
                    // Enclosure is full, deny delivery
                    return; 
                }
                // --------------------------

                carriedEgg.state = 2; 
                carriedEgg.carrierId = ""; 
                carriedEgg.ownerId = client.sessionId; 
                carriedEgg.hatchProgress = 5000; 
                
                carriedEgg.x = player.x;
                carriedEgg.z = player.z;
                carriedEgg.y = 0; 
            }
        }
    });

    this.onMessage("upgrade_enclosure", (client) => {
        const player = this.state.players.get(client.sessionId);
        if (player && player.coins >= player.enclosureUpgradeCost) {
            player.coins -= player.enclosureUpgradeCost;
            player.enclosureLevel += 1;
            player.petCapacity += 2; // Adds 2 more pet slots per level
            player.enclosureUpgradeCost = Math.floor(player.enclosureUpgradeCost * 2);
        }
    });

    this.onMessage("sell_pet", (client, data) => {
        const pet = this.state.pets.get(data.petId);
        const player = this.state.players.get(client.sessionId);
        
        if (pet && player && pet.ownerId === client.sessionId) {
            // Refund coins based on the pet's biome rarity
            player.coins += 50 * (pet.biomeIndex + 1);
            this.state.pets.delete(data.petId); // Remove the pet from the server
        }
    });

    this.onMessage("jump", (client, message) => {
        const player = this.state.players.get(client.sessionId);
        if (player && player.y <= 0.1) { 
            player.velocityY = 15; 
        }
    });

    this.setSimulationInterval((deltaTime) => {
        this.updateGame(deltaTime);
    }, 1000 / 30);
  }

  updateGame(deltaTime: number) {

    this.state.eggTimer -= (deltaTime / 1000);
        if (this.state.eggTimer <= 0) {
            this.state.eggTimer = 300; // Reset to 5 minutes
            
            this.state.eggs.forEach(egg => {
                // Only reset eggs that have been consumed (state 3) or are sitting uncarried (state 0)
                if (egg.state === 3 || egg.state === 0) {
                    egg.state = 0;
                    egg.carrierId = "";
                    egg.ownerId = "";
                    egg.x = egg.baseX;
                    egg.y = 0.5;
                    egg.z = egg.baseZ;
                    egg.hatchProgress = 0;
                }
            });
        } // <-- MISSING BRACE WAS HERE
        
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
                  const player = this.state.players.get(egg.ownerId);
                  
                  if (player) {
                      player.score += 1;
                      
                      const pet = new Pet();
                      pet.id = "pet_" + Date.now() + "_" + Math.floor(Math.random() * 1000);
                      pet.ownerId = egg.ownerId;
                      pet.biomeIndex = egg.biomeIndex; 
                      
                      const myBase = this.basePositions[player.baseIndex];
                      pet.x = myBase.x + (Math.random() * 20 - 10); 
                      pet.y = 1.0; 
                      pet.z = myBase.z + (Math.random() * 20 - 10);
                      
                      this.state.pets.set(pet.id, pet);
                  }

                  try {
                      egg.state = 3; // 3 = Consumed / Waiting for 5-min timer
                      egg.ownerId = "";
                      egg.x = egg.baseX;
                      egg.y = -50; // Hide underground
                      egg.z = egg.baseZ;
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

          this.state.eggs.forEach((egg) => {
              if (egg.biomeIndex === guard.biomeIndex && egg.state === 1 && egg.carrierId !== "") {
                  const p = this.state.players.get(egg.carrierId);
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
                  // --- NEW: SERVER KNOCKBACK LOGIC ---
                  targetPlayer.velocityY = 15 + (guard.speed * 0.2);

                  const magnitude = Math.sqrt(dx * dx + dz * dz);
                  const normX = magnitude > 0 ? dx / magnitude : 1;
                  const normZ = magnitude > 0 ? dz / magnitude : 0;
                  const force = guard.speed * 2.5; 

                  // Find the client BEFORE clearing the carrierId
                  const targetClient = this.clients.find(c => c.sessionId === stolenEgg.carrierId);
                  if (targetClient) {
                      targetClient.send("knockback", { x: normX * force, z: normZ * force });
                  }

                  // Reset the egg
                  stolenEgg.carrierId = "";
                  stolenEgg.state = 0; 
                  stolenEgg.x = stolenEgg.baseX;
                  stolenEgg.y = 0.5;
                  stolenEgg.z = stolenEgg.baseZ;
                  // -----------------------------------
                  
              } else if (dist > 0.1) {
                  const moveAmt = guard.speed * (deltaTime / 1000);
                  guard.x += (dx / dist) * moveAmt;
                  guard.z += (dz / dist) * moveAmt;
                  guard.rotY = Math.atan2(dx, dz) * (180 / Math.PI);
              }
          } else {
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
      this.coinTimer += deltaTime;
      const giveCoins = this.coinTimer >= 1000;
      if (giveCoins) this.coinTimer -= 1000;

      this.state.pets.forEach(pet => {
          const owner = this.state.players.get(pet.ownerId);
          if (owner) {
              if (giveCoins) {
                  const income = 1 + (pet.biomeIndex * 2); 
                  owner.coins += income;
              }

              pet.idleTimer -= deltaTime;
              if (pet.idleTimer <= 0) {
                  const myBase = this.basePositions[owner.baseIndex];
                  pet.targetX = myBase.x + (Math.random() * 38 - 19);
                  pet.targetZ = myBase.z + (Math.random() * 38 - 19);
                  pet.idleTimer = 2000 + Math.random() * 4000;
              }

              const dx = pet.targetX - pet.x;
              const dz = pet.targetZ - pet.z;
              const dist = Math.sqrt(dx * dx + dz * dz);
              
              if (dist > 0.1) {
                  const moveAmt = 1.5 * (deltaTime / 1000); 
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

      // 4. Treadmill Logic
      this.state.treadmills.forEach(tm => {
          if (tm.occupantId !== "" && giveCoins) {
              const p = this.state.players.get(tm.occupantId);
              if (p) {
                  // Safety check to prevent NaN serialization crashes
                  if (p.moveSpeed === undefined || isNaN(p.moveSpeed)) {
                      p.moveSpeed = 10; 
                  }
                  if (tm.level === undefined || isNaN(tm.level)) {
                      tm.level = 1;
                  }

                  // Safely apply the massive stat boost
                  p.moveSpeed += 50 + (tm.level * 50); 
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

    const tm = this.state.treadmills.get("treadmill_" + player.baseIndex);
    if (tm) tm.ownerId = client.sessionId;

    this.state.players.set(client.sessionId, player);
  }

  onLeave (client: Client, code?: number) {
    const player = this.state.players.get(client.sessionId);
    if (player) {
        this.availableBases.push(player.baseIndex);
        
        const tm = this.state.treadmills.get("treadmill_" + player.baseIndex);
        if (tm) {
            tm.ownerId = "";
            tm.occupantId = "";
            tm.level = 1;
            tm.upgradeCost = 50;
        }
    }
    
    this.state.eggs.forEach((e) => {
        if (e.carrierId === client.sessionId) {
            e.state = 0;
            e.carrierId = "";
        }
    });
    this.state.players.delete(client.sessionId);
  }
}