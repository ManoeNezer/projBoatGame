# projBoatGame

Prototype Unity 6 URP pour un systeme maritime jouable avec vagues Gerstner, shader vertex displacement synchronise, flottabilite Rigidbody, joueur FPS sur bateau mobile, gouvernail, voile et vent.
Le prototype inclut aussi un monde maritime procedural streamable avec iles, ports, rochers, epaves, bouees, oiseaux et silhouettes lointaines.
La boucle de voyage contient maintenant contrats de port, rumeurs de marins, decouvertes, objectifs suivis et recompenses connectees a l'economie.

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
- `WeatherManager`
- `MaritimeEventManager`
- systeme de degats/reparation bateau
- courants, tempetes, recifs et evenements de navigation
- economie joueur, port interactif, amarrage et ameliorations bateau
- contrats, rumeurs, decouvertes, objectif actif et recompenses

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
- `Assets/Scripts/World/CurrentZone.cs`
- `Assets/Scripts/Weather/WeatherManager.cs`
- `Assets/Scripts/Weather/StormZone.cs`
- `Assets/Scripts/Weather/WeatherCameraFeedback.cs`
- `Assets/Scripts/Damage/BoatDamageSystem.cs`
- `Assets/Scripts/Damage/RepairablePart.cs`
- `Assets/Scripts/Damage/RepairTool.cs`
- `Assets/Scripts/Damage/RepairResource.cs`
- `Assets/Scripts/Events/MaritimeEventManager.cs`
- `Assets/Scripts/Events/StormGustEvent.cs`
- `Assets/Scripts/Events/DangerousWaveEvent.cs`
- `Assets/Scripts/Events/FogBankEvent.cs`
- `Assets/Scripts/Events/CurrentZoneEvent.cs`
- `Assets/Scripts/Economy/PlayerCurrency.cs`
- `Assets/Scripts/Economy/ResourceInventory.cs`
- `Assets/Scripts/Economy/TradeItem.cs`
- `Assets/Scripts/Economy/TradeDatabase.cs`
- `Assets/Scripts/Port/PortManager.cs`
- `Assets/Scripts/Port/PortZone.cs`
- `Assets/Scripts/Port/DockingZone.cs`
- `Assets/Scripts/Port/PortServicePoint.cs`
- `Assets/Scripts/Port/ResourceMerchant.cs`
- `Assets/Scripts/Port/ShipUpgradeMerchant.cs`
- `Assets/Scripts/Port/RepairMerchant.cs`
- `Assets/Scripts/Port/PortUIController.cs`
- `Assets/Scripts/Upgrades/BoatUpgradeSystem.cs`
- `Assets/Scripts/Upgrades/BoatUpgradeDefinition.cs`
- `Assets/Scripts/Quests/QuestManager.cs`
- `Assets/Scripts/Quests/Quest.cs`
- `Assets/Scripts/Quests/QuestStep.cs`
- `Assets/Scripts/Quests/QuestObjective.cs`
- `Assets/Scripts/Quests/QuestReward.cs`
- `Assets/Scripts/Quests/QuestDatabase.cs`
- `Assets/Scripts/Quests/ContractBoard.cs`
- `Assets/Scripts/Quests/ContractBoardUI.cs`
- `Assets/Scripts/Quests/ObjectiveTrackerUI.cs`
- `Assets/Scripts/Rumors/RumorManager.cs`
- `Assets/Scripts/Rumors/Rumor.cs`
- `Assets/Scripts/Rumors/RumorSource.cs`
- `Assets/Scripts/Rumors/RumorUI.cs`
- `Assets/Scripts/Discovery/DiscoveryManager.cs`
- `Assets/Scripts/Discovery/DiscoverableLocation.cs`
- `Assets/Scripts/Discovery/DiscoveryNotificationUI.cs`

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

## Dangers, meteo et reparations

`WeatherManager` controle la meteo globale et locale:
- vent calme, vent fort, pluie, brouillard, tempete et mer dangereuse.
- force/direction du vent via `WindManager`.
- hauteur, frequence et vitesse des vagues via `WaterManager.ReplaceWaves`.
- brouillard Unity, pluie particules placeholder et audio procedural.
- maniement de voile/gouvernail via multiplicateurs non destructifs sur `BoatHelmController`.

`StormZone` cree une zone de tempete physique:
- entree/sortie progressives selon la distance au centre.
- hausse danger, pluie, brouillard, vagues et poussee laterale sur le bateau.
- gizmos de rayon externe/interne et direction de poussee.

`BoatDamageSystem` gere les degats par zones:
- `Hull`: collisions, recifs, grosses vagues; provoque infiltration.
- `Sail`: tempete et rafales; reduit propulsion.
- `Rudder`: impacts arriere et tempete; reduit direction.
- `Mast`: tempete/vagues; rend la voile instable.

L'eau interne alourdit le `Rigidbody`, reduit la flottabilite via `FloatingObject.SetExternalWaterModifiers`, augmente le drag et peut amener le bateau vers le naufrage si la coque reste endommagee.

Reparation FPS:
- Le joueur vise un `RepairablePart` et appuie sur `E`.
- Chaque reparation consomme `RepairResource` sur le bateau.
- `RepairTool` sur le joueur gere le cooldown et declenche la reparation.
- Les points reparables sont sur le layer `Interactable`.

`MaritimeEventManager` declenche pendant la navigation:
- `StormGustEvent`: rafale qui pousse et fait pivoter le bateau.
- `DangerousWaveEvent`: vague dangereuse qui ajoute une impulsion et peut abimer coque/mat.
- `FogBankEvent`: brouillard soudain local.
- `CurrentZoneEvent`: courant temporaire qui pousse les rigidbodies.
- `HiddenReefEvent`: recifs physiques temporaires a eviter.

`CurrentZone` peut aussi etre place manuellement ou genere dans les zones dangereuses du monde. Il pousse le bateau, caisses, tonneaux et autres rigidbodies dans son rayon avec falloff.

`MaritimeDangerBootstrap` securise la scene existante au demarrage Play: si les nouveaux composants n'ont pas encore ete regeneres par le menu Unity, il ajoute automatiquement les managers, les points reparables, l'outil de reparation joueur, la zone de tempete test et le courant test sans dupliquer les objets deja presents.

## Ports, economie et ameliorations

Le prototype contient un premier hub maritime jouable:
- `PortManager`: racine logique du port.
- `DockingZone`: trigger d'amarrage, maintien de `E`, ralentissement et stabilisation par forces Rigidbody.
- `PortServicePoint`: base commune des services FPS.
- `ResourceMerchant`: achat/vente de bois, tissu, corde et fer.
- `ShipUpgradeMerchant`: achat d'ameliorations bateau.
- `RepairMerchant`: reparation portuaire et vidange de l'eau interne.
- `PortUIController`: UI IMGUI minimale avec ressources, confirmation, succes/erreur.

Economie:
- `PlayerCurrency`: pieces.
- `ResourceInventory`: bois, tissu, corde, fer avec capacite.
- `TradeItem` et `TradeDatabase`: base extensible pour offres de commerce.

Ameliorations achetables:
- `Voile renforcee`: propulsion et maniement voile.
- `Gouvernail ameliore`: force de gouvernail et maniement.
- `Coque renforcee`: reduit degats coque et infiltration.
- `Stockage augmente`: augmente stockage ressources et planches de reparation.
- `Petit canon`: visuel placeholder sur le pont.
- `Pont superieur`: plateforme placeholder avec collision.

Les upgrades s'appliquent au bateau existant via `BoatUpgradeSystem`; aucun remplacement de prefab bateau. Les stats sont poussees dans `BoatHelmController`, `BoatDamageSystem`, `RepairResource` et `ResourceInventory`.

Hierarchy recommandee pour un port manuel:
- `Port`
- `Dock`
- `DockAnchor`
- `DockingZone` avec `BoxCollider isTrigger` et `DockingZone`
- `ResourceMerchant` avec collider trigger + `ResourceMerchant`
- `ShipUpgradeMerchant` avec collider trigger + `ShipUpgradeMerchant`
- `RepairMerchant` avec collider trigger + `RepairMerchant`
- batiments placeholder sur layer `World`

Test port:
- Un port runtime est cree au Play si la scene n'en contient pas encore.
- Le builder peut aussi generer un port de test via `PortManager.CreateRuntimePort`.
- Les ports proceduraux generes par `WorldChunk` recoivent docks, zone d'amarrage, marchands, chantier et courant calme de port.

Raccourcis debug port:
- `F6`: donne pieces et ressources.
- `F7`: ouvre le chantier naval.
- `F8`: ouvre le marchand ressources.
- `F9`: spawn un port proche du bateau.
- `F10`: reset les upgrades achetees.
- `F11`: genere un contrat de test.
- `F12`: complete l'objectif actif.

Pour ajouter un nouveau marchand:
1. Creer une classe qui herite de `PortServicePoint`.
2. Appeler `ConfigureService` dans `Awake`.
3. Implementer `OpenService` et ouvrir un ecran dans `PortUIController` ou une UI dediee.
4. Ajouter le marchand au port via `PortManager.RegisterService`.

Pour ajouter une nouvelle amelioration:
1. Ajouter une entree dans `BoatUpgradeType`.
2. Ajouter une `BoatUpgradeDefinition` dans `BoatUpgradeSystem.EnsureDefaultDefinitions`.
3. Appliquer son effet dans `ApplyPurchasedUpgrades`.
4. Ajouter un visuel dans `EnsureVisual`.

## Contrats, rumeurs et decouvertes

`QuestManager` pilote les contrats disponibles, actifs, acheves et rendus. Les contrats sont crees par `QuestDatabase` a partir du monde genere:
- livraison entre ports.
- exploration d'ile.
- recuperation dans une epave.
- chasse au tresor simple.
- enquete dans une zone dangereuse.
- escorte maritime placeholder.
- decouverte de nouveau port.

Chaque contrat contient des `QuestStep`, des `QuestObjective` et des `QuestReward`. Les objectifs peuvent demander d'atteindre un lieu, decouvrir un POI, tenir une position, interagir avec un repere ou revenir au port. Les recompenses utilisent directement `PlayerCurrency`, `ResourceInventory`, `BoatDamageSystem` et les plans d'amelioration de `BoatUpgradeSystem`.

Les ports proceduraux et le port runtime recoivent:
- `ContractBoard`: tableau de contrats interactif au FPS.
- `RumorSource`: maitre des rumeurs.
- `ContractBoardUI`: liste, acceptation et rendu des contrats.
- `RumorUI`: rumeurs de quai avec direction vague et distance approximative.

`ObjectiveTrackerUI` affiche l'objectif actif sans GPS moderne: titre du contrat, etape, distance approximative et direction relative au bateau. Les rumeurs preferent les indications de marin: babord, tribord, sillage, horizon et repaires naturels.

`DiscoveryManager` enregistre les lieux trouves. Les chunks du monde ajoutent automatiquement un `DiscoverableLocation` aux iles, ports, epaves, recifs et zones dangereuses. Une decouverte peut completer un objectif, afficher une notification, donner une petite recompense et reveler une rumeur liee.

Integration monde:
- `WorldManager.TryFindPoi` cherche une destination valide autour du bateau ou du port.
- `WorldManager.EnsurePoiNear` force un POI si le monde n'en a pas encore genere un a distance raisonnable.
- Le streamer refresh le chunk garanti pour eviter les contrats impossibles.

Pour ajouter un nouveau type de contrat:
1. Ajouter une entree dans `QuestContractType`.
2. Ajouter un builder dans `QuestDatabase.CreateContract`.
3. Mapper le type vers un `MaritimePoiType` dans `QuestManager.ResolveDestinationForType`.
4. Definir les objectifs et recompenses avec les classes existantes.

Pour ajouter une nouvelle rumeur:
1. Creer un `Rumor` avec titre, texte, position approximative et rayon d'incertitude.
2. L'ajouter via `RumorManager.AddRumor` ou `RumorManager.RevealRumor`.
3. Si elle vient d'un port, l'ajouter dans `RumorManager.CreateRumorForType` ou dans une future table de donnees.

Test boucle complete:
1. Lancer `Assets/Scenes/WaterPrototype.unity`.
2. Aller au port proche ou appuyer sur `F9`.
3. Interagir avec `TableauContrats`, accepter un contrat.
4. Suivre `ObjectiveTrackerUI`, naviguer vers la destination et accomplir l'objectif.
5. Revenir au port si le contrat le demande, rendre le contrat et verifier pieces/ressources/upgrades.
6. Interagir avec `MaitreRumeurs`, noter une rumeur puis chercher la zone indiquee.

Tests rapides:
- Naviguer vers `PrototypeStormZone` pour entrer dans une tempete.
- Traverser `PrototypeCurrentZone` pour sentir le courant.
- Laisser les evenements se declencher, ou appeler les methodes `ForceGust`, `ForceDangerousWave`, `ForceFogBank`, `ForceCurrentZone` depuis l'inspecteur/debug.
- Heurter un recif ou un rocher de monde: la coque prend des degats et l'eau interne monte.
- Viser un point `HullRepairPoint`, `SailRepairPoint`, `RudderRepairPoint` ou `MastRepairPoint`, puis appuyer sur `E`.
- `F3` affiche/cache le debug UI danger.

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
