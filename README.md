# Prism Guard

**A 3D RTS game in which the player has to defend against waves of enemies.**

Prism Guard is a strategy and base-management prototype built in Unity. You build a base around a magic crystal, gather and process resources, research upgrades and produce units, and you defend the crystal from zombie waves. Workers handle the day-to-day work on their own. Soldiers, towers and an experimental hero unit trained with ML-Agents handle the fighting.

This project is my Final Degree Project (*Trabajo Fin de Grado*) for the Degree in Computer Engineering (*Grado en Ingeniería Informática*) at the University of Cádiz. [Project report (Spanish)](TFG_MarcosMoralesMarquez.pdf)

**Want to try it?** Download the [Windows build](https://github.com/kirtashK/TFG-PrismGuard/releases/latest) and see [Playing the Windows build](#playing-the-windows-build).

| | |
|---|---|
| **Author** | Marcos Morales Márquez |
| **Director** | Kevin Jesús Valle Gómez |
| **Co-director** | José Miguel Aragón Jurado |
| **Date** | June 2026 |
| **Rating** | 9/10 |

## About the project

The goal was a playable prototype that combines the usual RTS and management mechanics in one technical framework: construction, resource management, task assignment, research, production and real-time combat. Machine learning is used to drive the decisions of a special unit.

The project is a technical, functional prototype. It does not include a full campaign, final content balancing or a large amount of final art. The work focuses on a solid, scalable and maintainable base that new mechanics and content can be added to in later iterations.

Inspirations include RimWorld, Age of Empires, Oxygen Not Included, Diplomacy is Not an Option, Factorio and Dwarf Fortress.

## Gameplay

The player plans the colony and the AI carries it out:

- **You** decide where structures go, what to research, which units to produce and where soldiers should move or stand guard.
- **Workers** pick up tasks automatically: chopping trees, mining, hauling items between buildings, constructing blueprints and doing research.
- **Soldiers and towers** attack enemies that come within range.
- **Zombie waves** spawn periodically from flagged spawn points and march on your base.
- **If the crystal is destroyed, you lose.** The game pauses, the camera orbits the shattered crystal and the defeat screen shows your final score.

Killing enemies and clearing waves grants **score**, which is also the currency used in the shop.

## Features

### Construction
- Construction panel with structure categories, loaded dynamically through Addressables.
- Placement preview that you can rotate before confirming. It turns **green** when the position is valid, **red** when it is blocked and **blue** once it has been placed as a blueprint.
- Workers deliver the required materials to the blueprint and build it into the finished structure.
- Defensive structures: wooden walls and arrow towers that fire projectiles.

### Resources, processing and storage
- **Resource nodes**: trees, stone, and copper, iron and gold veins. Nodes can be finite or infinite and respawning.
- **Gathering flags** mark the nodes within a radius around them, and workers harvest those nodes automatically.
- **Processors** turn input items into output items using configurable recipes. Recipes can define batches, processing time, optional fuel and output caps.
  - Sawmill: logs → planks
  - Quarry: rocks → stone blocks
  - Crude and Advanced Smelter: ores → copper, iron and gold ingots
- **Warehouses** accept items by category (plants, rocks, ores, resources, manufactured goods) and have a limited capacity that can be upgraded.
- **Thresholds** stop gatherers and processors from producing once there is enough stock.
- **Inventory panel** showing the total stock across the base.
- **Shop** to buy and sell items with score.

### Task system
- A centralized `TaskManager` that structures register tasks with: gathering, item transport, construction and research.
- Idle workers request the best available task based on priority and NavMesh path distance.
- Input and output reservations prevent concurrent workers from claiming the same task or item.
- Workers flee when attacked, drop what they are carrying and release their reservations.

### Research
- Research benches where workers spend time and resources on research projects.
- Research projects can have prerequisites, and their effects are data-driven:
  - **Modify stat**: health, damage, speed, gathering radius, storage capacity, research speed, concurrent batches and so on, applied per unit or structure type or globally.
  - **Unlock structure**: smelters, ore gathering flags, shop, tower.
  - **Unlock unit**: for example, the Medium Soldier.

### Units and production
- **Workers**: a state machine (`Idle`, `Moving`, `Working`, `MultiTransport`) that executes tasks (`GatherResourceTask`, `MoveItemTask`, `BuildTask`, `ResearchTask`).
- **Soldiers**: a state machine (`Idle`, `Move`, `Chase`, `Attack`). After a move order they guard the area around their destination, and they return to it once there are no enemies left.
- **Enemies**: a state machine (`Idle`, `Guard`, `Chase`, `Attack`) that targets workers, soldiers and structures.
- **Agent Soldier (hero)**: a special unit controlled by a neural network trained with reinforcement learning. See [Machine learning](#machine-learning-the-agent-soldier) below.
- **Unit factories** (the Crystal and the Barracks) handle production orders. Each order waits for its required items to be delivered and then uses a concurrent production slot.

### RTS selection and orders
- Click to select, drag to box-select, `Shift` to add to the selection.
- Right-click to send move/guard orders. Groups move in formation, and their destinations are spread out so the units don't all converge on the same point.
- Selection halos on units and a marker at the ordered destination.

### Waves
- Each wave has a budget that grows over time: `(initialBudget + linearDelta · n) · rⁿ`.
- The wave "buys" random enemies with that budget, split into cheap, medium and expensive tiers by configurable percentages.
- A random spawn point is chosen each wave and marked with a flag, so the player can see where the attack is coming from.
- Clearing a wave grants a score reward and heals units and structures by a configurable amount.

### Stats
- A data-driven stat system. Stats are referenced through `StatKey` assets.
- `StatModifierManager` computes final values from base data plus active modifiers, and it notifies units and structures through events when those modifiers change.

### Interface
- Panels for construction, research, unit production, warehouses, shop, inventory and thresholds. All panels share a common `BasePanel` behaviour and close with `Esc`, a right-click or a click outside the panel.
- Health bars, floating damage and healing text, tooltips, wave banners and an enemy counter.
- Game speed control (pause, x1, x2, x3), pause menu, defeat screen and a debug menu.

## Machine learning: the Agent Soldier

The `AgentSoldier` is an experimental hero unit built with [Unity ML-Agents](https://github.com/Unity-Technologies/ml-agents) and trained with **PPO**. It decides which enemy to attack and when to use its abilities.

**Observations**
- Its own health and attack cooldown (normalized)
- The number of nearby enemies
- The relative position and distance of the `k` nearest enemies (default `k = 4`)
- Cooldown and context information for up to 3 ability slots

**Actions** (two discrete branches)
1. Target: one of the `k` nearest enemies, or no target
2. Action: do nothing, basic attack, or use ability 1, 2 or 3

**Abilities** (`HeroAbility` ScriptableObjects)
- **AoE Attack**: damages every valid enemy in a radius.
- **AoE Heal**: heals the agent and nearby allies, measuring effective versus wasted healing.
- **Healing Potion**: heals the agent and penalizes overhealing.

**Rewards**
- Positive: damage dealt, successful attacks, kills, useful healing, and the crystal surviving the episode.
- Negative: damage taken, death, wasted healing, damage to the crystal, and a small penalty per step.

Training runs in `ML TrainingScene`. There, `TrainingManager` spawns agents and enemies, runs time-limited episodes, hands out shared rewards and resets everything at the end of each episode. The trainer configuration is in `Assets/ML-Agents/trainer_config.yaml`, and the trained models (`.onnx`) are in `Assets/ML-Agents/results/`.

## Architecture

The project uses a modular, component-based architecture that keeps **data**, **game logic**, **UI** and **support systems** separate.

- **Data-driven design**: items, resources, recipes, structures, units, enemies, research, stats and abilities are all `ScriptableObject` assets under `Assets/Data`. Content can be tuned in the editor without touching code. Many of these assets are loaded by label through **Addressables**.
- **Bootstrap**: a `BootStrap` scene is loaded additively before any other scene. It initializes the persistent managers and then redirects to the main menu.
- **Managers**: singletons such as `GameManager`, `WaveManager`, `TaskManager`, `ResearchManager`, `StatModifierManager`, `WarehouseManager`, `ItemConsumerManager`, `ScoreManager`, `SelectionManager`, `OrderManager`, `InputManager`, `UIManager` and `GameSpeedManager`.
- **Item routing**: `ItemConsumerManager` decides where an item should go. Buildings that consume the item get priority over warehouses.
- **Input**: Unity Input System with separate action maps (Global, UI, Gameplay, Camera, Construction). The maps are enabled or disabled depending on the current input mode.
- **Navigation**: Unity AI Navigation (NavMesh) for all units.

**Design patterns and principles**
- **State Pattern**: worker, soldier and enemy AI.
- **Singleton Pattern**: global managers.
- **Flyweight Pattern**: shared ScriptableObject data across instances.
- **Command Pattern**: UI actions, such as the menu buttons.
- **Observer / event-driven** communication between systems.
- **Object pooling**: combat text and inventory UI slots.
- **SOLID** principles throughout.

## Controls

| Action | Input |
|---|---|
| Move camera | `W` `A` `S` `D` / arrow keys |
| Move camera faster | Hold `Left Shift` |
| Pan camera | Right mouse drag |
| Rotate camera | Middle mouse button |
| Zoom | Mouse wheel |
| Select / box select | Left click / left click + drag |
| Add to selection | `Shift` + left click |
| Move / guard order | Right click |
| Place structure / cancel | Left click / right click |
| Rotate structure | `R` |
| Pause / x1 / x2 / x3 speed | `Space` or `0` / `1` / `2` / `3` |
| Close panel / pause menu | `Esc` |
| Debug menu | `K` |

## Tech stack

- **Unity 6.4** (`6000.4.0f1`) with the Universal Render Pipeline (URP)
- **C#**
- Unity ML-Agents (PPO)
- Unity Input System
- AI Navigation (NavMesh)
- Addressables
- Unity UI / TextMesh Pro
- Animation Rigging

**Tooling**: Visual Studio Community 2026, Git and GitHub with a Gitflow branching model (more than 50 pull requests over the project), Fork, Obsidian for notes, LaTeX/Overleaf for the report, and Paint.NET for images.

## Getting started

### Playing the Windows build
You don't need Unity to play. A prebuilt version for 64-bit Windows is available on the [Releases page](https://github.com/kirtashK/TFG-PrismGuard/releases/latest).

1. Download `Build.zip` from the latest release.
2. Extract the whole archive. The game does not run from inside the zip, and `TFG.exe` needs the `TFG_Data` folder and the other files next to it.
3. Run `TFG.exe` inside the extracted `Build` folder.

The executable is not signed, so Windows SmartScreen may show a warning the first time. Choose **More info → Run anyway** to start the game.

The rest of this section is only needed if you want to open the project in the Unity editor.

### Requirements
- Windows 11 (the target platform during development)
- Unity `6000.4.0f1`, installed through Unity Hub
- A C# editor such as Visual Studio, for development
- Python and the `mlagents` package, only if you want to train the agent

### Opening the project
1. Clone the repository:
   ```bash
   git clone https://github.com/kirtashK/TFG-PrismGuard.git
   ```
2. Open the `TFG Unity` folder with Unity Hub and wait for the import and script compilation to finish.
3. Check that the console shows no compilation errors.
4. Open `Assets/Scenes/MainMenu`, or any scene, since the bootstrap redirects to the main menu. Then press Play.

### Building
1. Open **File → Build Profiles** (Build Settings).
2. Select the target platform (Windows).
3. Make sure the required scenes are in the scene list: `BootStrap`, `MainMenu` and `Demo Scene`.
4. Click **Build**. This produces a folder with the executable and its data files, which you can run without the editor.

### Scenes
| Scene | Purpose |
|---|---|
| `BootStrap` | Initializes persistent managers |
| `MainMenu` | Main menu |
| `Demo Scene` | Main playable level (started with "New game") |
| `SampleScene` | Development and testing scene (not redirected by the bootstrap) |
| `ML TrainingScene` | Training arena for the Agent Soldier |

### Training the agent
From the `TFG Unity` folder:
```bash
mlagents-learn Assets/ML-Agents/trainer_config.yaml --run-id=<run_name>
```
Press Play in `ML TrainingScene` when prompted. Results are written to `results/<run_name>/`, and the generated `.onnx` model can be assigned in the agent's Behavior Parameters.

To monitor training:
```bash
tensorboard --logdir results
```

## Project structure

```
TFG Unity/
├── Assets/
│   ├── Art/            # Models, animations, textures, materials, shaders
│   ├── Data/           # ScriptableObject data (items, recipes, structures, units, research, stats…)
│   ├── ML-Agents/      # Trainer config and trained models
│   ├── Prefabs/        # Units, structures, items, resources, managers, UI
│   ├── Scenes/
│   ├── Scripts/
│   │   ├── Game/               # Game flow, camera, input, game speed, debug
│   │   ├── Inventory/          # Inventory UI and thresholds
│   │   ├── ItemConsumer/       # Item routing
│   │   ├── Items/
│   │   ├── Research/           # Research system and effects
│   │   ├── Resources/          # Resource nodes and recipes
│   │   ├── Score/
│   │   ├── Selectable & Order/ # Selection and RTS orders
│   │   ├── Stat/               # Stat keys and modifiers
│   │   ├── Structure/          # Construction, defenses, processors, warehouses, shop, unit factories
│   │   ├── Target/             # Targeting and sensors
│   │   ├── UI/
│   │   ├── Units/              # Workers, soldiers, enemies, ML agent
│   │   └── Wave/
│   └── Terrain/
├── Packages/
└── ProjectSettings/
```

## Future work

- **More content**: more structures, units, recipes, research projects, enemies and resources. One example is a blacksmith that forges the swords needed to train soldiers, which adds an intermediate production step.
- **Smarter worker AI**: better prioritization and automatic redistribution of tasks. Right now tasks are assigned only by priority and distance.
- **Tactical combat AI**: soldiers currently target the nearest enemy, so groups tend to focus the same target and waste attacks on weak enemies.
- **UI polish**: more complete screens, better status indicators, and animations and effects.
- **Save and load**, and persistence between sessions.
- **Unit tests** for the most critical systems, to reduce manual testing.

## Credits

- Character models: [GanzSe Free Modular Character – Fantasy Low Poly Pack](https://assetstore.unity.com/packages/3d/characters/humanoids/fantasy/ganzse-free-modular-character-fantasy-low-poly-pack-321521)
- Animations: [Mixamo](https://www.mixamo.com/)
- Additional 3D models: [itch.io game assets](https://itch.io/game-assets)
- Icons: [Flaticon](https://www.flaticon.com/)
- Font: Liberation Sans
- [Unity ML-Agents](https://github.com/Unity-Technologies/ml-agents)

## License

- **Source code**: [MIT License](LICENSE)
- **Documentation** (project report): [Creative Commons BY-NC-SA 4.0](https://creativecommons.org/licenses/by-nc-sa/4.0/)

Third-party assets keep their own licenses.
