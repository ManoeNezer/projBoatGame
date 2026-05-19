# projBoatGame

Prototype Unity 6 URP pour un systeme maritime jouable avec vagues Gerstner, shader vertex displacement synchronise, flottabilite Rigidbody, joueur FPS sur bateau mobile, gouvernail, voile et vent.
Le prototype inclut aussi un monde maritime procedural streamable avec iles, ports, rochers, epaves, bouees, oiseaux et silhouettes lointaines.

## Scene jouable

Ouvrir `Assets/Scenes/WaterPrototype.unity`, puis lancer Play.

Controles bateau:
- Deplacement FPS: `ZQSD` ou `WASD`, souris, `Space` saut, `Left Shift` sprint leger.
- Interaction: viser un poste et appuyer sur `E`.
- Gouvernail: en mode poste, `Q` / `D` tournent le gouvernail, `E` ou `Escape` quitte le poste.
- Voile: en mode poste, `Z` / `S` montent ou descendent la voile, `Q` / `D` orientent la voile.

La scene contient deja:
- `WaterManager`
- `WindManager`
- `Ocean`
- `PrototypeBoat`
- `FPSPlayer`
- postes `HelmStation` et `SailStation`
- deux caisses flottantes
- deux tonneaux flottants
- lumiere, skybox, brouillard leger
- `WaterDebugProbe`
- `WorldManager`
- streaming de chunks autour du bateau
- iles/ports/POI generes proceduralement

Si les assets doivent etre regeneres, utiliser le menu Unity:
`Tools/Ocean Prototype/Rebuild Complete Water Prototype`.

## Architecture

Scripts:
- `Assets/Scripts/Water/WaterManager.cs`
- `Assets/Scripts/Physics/FloatingObject.cs`
- `Assets/Scripts/Boat/BoatHelmController.cs`
- `Assets/Scripts/Boat/HelmStation.cs`
- `Assets/Scripts/Boat/SailStation.cs`
- `Assets/Scripts/Player/FpsPlayerController.cs`
- `Assets/Scripts/Interaction/IInteractable.cs`
- `Assets/Scripts/Interaction/InteractableBase.cs`
- `Assets/Scripts/Interaction/InteractionSystem.cs`
- `Assets/Scripts/Interaction/InteractionPromptUI.cs`
- `Assets/Scripts/Environment/WindManager.cs`
- `Assets/Scripts/Debug/WaterDebugProbe.cs`
- `Assets/Scripts/World/WorldManager.cs`
- `Assets/Scripts/World/ChunkStreamer.cs`
- `Assets/Scripts/World/WorldChunk.cs`
- `Assets/Scripts/World/WorldRules.cs`
- `Assets/Scripts/World/WorldGenerationSettings.cs`
- `Assets/Scripts/World/WorldMaterialSet.cs`
- `Assets/Scripts/World/OceanSurfaceFollower.cs`
- `Assets/Scripts/World/SimpleBirdFlock.cs`

Shader:
- `Assets/Shaders/OceanGerstnerURP.shader`

Assets generes:
- `Assets/Prefabs/PrototypeBoat.prefab`
- `Assets/Prefabs/FPSPlayer.prefab`
- `Assets/Prefabs/FloatingCrate.prefab`
- `Assets/Prefabs/FloatingBarrel.prefab`
- `Assets/Materials/OceanWater.mat`
- `Assets/Materials/WorldIslandGrass.mat`
- `Assets/Materials/WorldBeachSand.mat`
- `Assets/Materials/WorldCliffRock.mat`
- `Assets/Materials/WorldDockWood.mat`
- `Assets/Materials/WorldBuildingWall.mat`
- `Assets/Materials/WorldBuildingRoof.mat`
- `Assets/Materials/WorldBuoyRed.mat`
- `Assets/Materials/WorldBuoyWhite.mat`
- `Assets/Materials/WorldWreckWood.mat`
- `Assets/Materials/WorldSilhouette.mat`
- `Assets/Meshes/OceanGrid.asset`

## Monde procedural maritime

Le monde est pilote par `WorldManager`. Il garde la seed, les regles de generation, les materiaux et la cible de streaming. Dans la scene prototype, la cible est le transform du bateau, ce qui permet de charger le monde autour du navire plutot qu'autour de la camera.

`ChunkStreamer` maintient les chunks actifs autour du bateau:
- `fullChunkRadius`: chunks proches avec mesh, collisions et details.
- `silhouetteChunkRadius`: chunks lointains legers avec silhouettes d'iles et de ports.
- `updateInterval`: frequence de mise a jour pour eviter les allocations et les recalculs inutiles.

`WorldChunk` construit le contenu deterministe du chunk:
- iles petites et grandes avec heightmap noise, plage, falaises et `MeshCollider`.
- ports forces sur la route maritime avec docks, batiments, lumiere, bouees et zone sure.
- rochers emergents, epaves, debris flottants et zones dangereuses.
- oiseaux simples et ambiance oceanique.

`WorldRules` controle le rythme:
- rayon de securite autour du spawn.
- route maritime implicite.
- distance minimale entre ports.
- ports reguliers sur la trajectoire du joueur.
- densite separee entre route et haute mer.

Reglages conseilles dans `WorldManager`:
- Chunk Size: `420`
- Full Chunk Radius: `2`
- Silhouette Chunk Radius: `4`
- Port Spacing Chunks: `3`
- Route Poi Chance: `0.76`
- Open Sea Poi Chance: `0.38`
- Island Mesh Resolution: `28`
- Distant Mesh Resolution: `10`

Pour tester le voyage, lance Play, monte la voile, prends le gouvernail et navigue dans la direction generale des silhouettes. Un port important apparait regulierement sur la route, et les chunks lointains donnent toujours un objectif visuel avant que les collisions/detail meshes soient charges.

## Installation manuelle eau/flottabilite

1. Creer un GameObject `WaterManager` et ajouter `WaterManager.cs`.
2. Regler `Water Level` et les vagues dans la liste `Waves`.
3. Creer un mesh ocean dense et lui assigner un material utilisant `BoatGame/Ocean Gerstner URP`.
4. Garder `Push Shader Globals` actif pour que le shader utilise exactement les memes vagues que la physique.
5. Ajouter `FloatingObject.cs` sur tout objet flottant avec un `Rigidbody`.
6. Placer plusieurs float points sous la ligne de flottaison et les assigner dans `FloatingObject`.

## Installation manuelle FPS et bateau

1. Placer `FPSPlayer.prefab` sur le pont, pieds juste au-dessus du collider de pont.
2. Le joueur doit avoir:
   - `Rigidbody` mass `75`, `Freeze Rotation`, `Interpolate`, `Continuous Dynamic`
   - `CapsuleCollider` height `1.8`, radius `0.3`, center `(0, 0.9, 0)`
   - `FpsPlayerController`
   - `InteractionSystem`
3. Le bateau doit rester un `Rigidbody` dynamique avec `BoatHelmController`.
4. Les postes interactifs sont des triggers sur le layer `Interactable`.
5. Le pont et les rails sont des colliders enfants du Rigidbody bateau, layer `Boat`.
6. Ajouter un `WindManager` dans la scene pour alimenter la propulsion.

Layers crees par le builder:
- `Player`
- `Boat`
- `Interactable`
- `World`

Collisions:
- Le joueur collide avec le bateau et les objets flottants.
- Les triggers interactifs sont exclus du ground check FPS.
- Le raycast d'interaction ignore le layer `Player`.
- Les iles/ports/rochers generes sont sur le layer `World`.
- Les colliders d'iles et de rochers sont statiques par chunk et decharges avec le chunk.

## Reglages Rigidbody conseilles

Bateau prototype:
- Mass: `2400`
- Linear Damping: `0.08`
- Angular Damping: `0.72`
- Interpolate: `Interpolate`
- Collision Detection: `Continuous Dynamic`
- Center of Mass local: `(0, -0.65, -0.12)`
- Float points: 6 points repartis avant, milieu, arriere.
- Gouvernail max: `34 deg`
- Voile initiale: ouverture `0.72`, angle `8 deg`
- Vent scene: direction `42 deg`, force `8.5`

Joueur FPS:
- Mass: `75`
- Freeze Rotation actif
- Walk speed: `3.2`
- Sprint speed: `4.6`
- Jump velocity: `5.1`
- Le moteur injecte la vitesse du Rigidbody sous les pieds pour suivre le bateau sans parentage.

Caisse:
- Mass: `85`
- 4 float points bas aux coins.

Tonneau:
- Mass: `110`
- CapsuleCollider couche horizontalement.
- 4 float points bas pour provoquer du roulis naturel.

## Points importants

- Aucun `transform.position` force pour la flottabilite.
- Le bateau n'est pas parent de l'eau.
- Le joueur n'est pas parent du bateau en marche libre.
- Les grosses vagues Gerstner alimentent la physique et le vertex displacement.
- Les petites vagues visuelles sont uniquement dans les normals/fragment shader et ne touchent pas la physique.
- La voile et le gouvernail appliquent des forces/torques Rigidbody, jamais de rotation bateau forcee.
- Les gizmos montrent les float points, hauteurs d'eau et normales.
- Les gizmos du bateau montrent propulsion et force de gouvernail; ceux du vent montrent direction et intensite.
- Les gizmos du monde montrent chunks, POI, routes implicites et distances de streaming.

## Workflow prefab et generation

Les elements du monde sont generes a l'execution pour rester modulaires. Pour remplacer les placeholders plus tard, garder la meme architecture:
- conserver `WorldRules` pour decider quoi spawn.
- etendre `WorldChunk` avec des builders de contenu par type de POI.
- remplacer les primitives par des prefabs instancies via un set de references dedie.
- garder les collisions et LOD separes entre chunk proche et silhouette lointaine.

Optimisations deja presentes:
- generation deterministe par chunk, sans etat global fragile.
- pooling simple des `WorldChunk`.
- refresh cadence par `updateInterval`, pas a chaque frame.
- silhouettes lointaines sans colliders lourds.
- meshes et colliders seulement dans le rayon proche.
- objets flottants d'ambiance limites et synchronises a l'eau via `OceanSurfaceFollower`.
