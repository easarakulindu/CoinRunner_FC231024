# CoinRunner FC231024

A Unity 2D platformer following Lab Sheet 02, including all five extra challenges.

Open this project with Unity **6000.6.5f1**, open `Assets/Scenes/SampleScene.unity`, and press **Play**. Use the Game view's **16:9 Aspect** setting.

## Controls

- **A / D** or **Left / Right arrows**: move.
- **Space**: jump from the ground or a platform. Release early for a shorter jump.
- Touch yellow coins to collect them. The counter shows the number collected out of six.
- Red spikes and falling below the level return you to the start; collected coins stay collected.
- Reach the white goal pole to complete the level. **Play Again** resets the whole level and coin count.

## Project contents

- `Assets/Scenes/SampleScene.unity`: the complete editable level, player, camera, UI, moving platform, and goal.
- `Assets/Scripts`: player physics, interactions, camera follow, game state, and moving-platform behavior.
- `Assets/Prefabs`: reusable Coin and Spike prefabs.
- `Assets/Tiles`: ground tile, shape sprites, and the NoFriction physics material.
- `Assets/Palettes/LevelPalette.prefab`: the ground tile palette.
- `Assets/TextMesh Pro`: official TMP Essentials, including the font license.
- `Assets/Tests/PlayMode`: gameplay checks, available through **Window > General > Test Runner > PlayMode**.

All five challenges are included: `Coins: n / 6`, zero friction, variable jump height, a facing eye, and a kinematic platform with three units of horizontal travel. The player inherits its horizontal motion while standing on it.

The original tile coordinates and six coin positions follow the sheet. Extra spikes are at `(16.5, -3)` and `(19.5, -3)`. The moving platform starts at `(8, 2.5)`, measures `2 × 0.4`, and travels to `(11, 2.5)` and back. Its height leaves clearance for the normal gap jump. These details were chosen with your authorization.

The Unity product name is `CoinRunner_FC231024`; the existing project directory remains `CoinRunner`.

## Implementation notes

Input is read in `Update`, while velocity changes happen in `FixedUpdate`. Ground checks use the Ground layer. A jump request is consumed once, and upward velocity prevents a second jump while the feet are still leaving the ground. The camera follows in `LateUpdate`.

The scene setup utility creates saved assets and objects; gameplay does not depend on that utility. It refuses to overwrite a scene that already contains a Player. Its configuration is stored in `ProjectSettings/CoinRunnerSetup.json`.

## Verification

All **10 PlayMode tests passed** in Unity 6000.6.5f1. They check the level references, movement and facing, grounded jumping and early release, both fixed platforms, the gap, coin triggers, spike and fall respawn, platform riding and reversal, camera follow, and the goal/restart flow. Tests inject input at the movement controller boundary and run Unity physics and scene interactions.

The HUD, whole-level view, and win screen were rendered and visually inspected. Test results and preview images are in the local ignored `Logs` folder.
