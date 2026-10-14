# Technical walkthrough

## Explain the AI in 60 seconds

The guard first tests distance and viewing angle, then raycasts to check whether a wall blocks the player. A visible player raises a suspicion meter instead of causing an instant alarm. At full suspicion the guard pursues using a NavMeshAgent. It stores the player's position only while it can see them; after losing sight it walks to the last known position, searches, and returns to patrol. Footsteps and decoys can redirect a calm guard into investigation.

## Code map

- `PlayerMotor.cs`: input, crouching, sprinting, collision and footsteps.
- `GuardBrain.cs`: perception, state transitions, navigation and capture.
- `VisionCone.cs`: clipped perception visualization.
- `MissionController.cs`: objectives, decoys, timer, pause and results.
- `MissionHUD.cs`: briefing, feedback, controls and AI debugging.
- `FollowCamera.cs`: bounded camera following.
- `PulseEffect.cs`: transient decoy feedback.
- `Editor/LevelBuilder.cs`: reproducible level authoring, route validation and macOS build.
- `Editor/GameplayChecks.cs`: real-scene integration checks.

Runtime scripts are under `Assets/SilentExtraction/Runtime`.

## Tradeoffs worth discussing

- A small enum-based state machine is easy to inspect at this scale. A larger game may benefit from separate state classes or a behavior tree.
- Raycasts provide inexpensive geometric visibility; they do not model lighting, peripheral sensitivity or partial body exposure.
- The vision mesh shows nominal standing detection range; crouching reduces actual detection range, not the displayed cone.
- Noise is an instantaneous radial event, not a physically travelling sound or projectile.
- The camera clamps its framing point to reduce views outside the facility.
- The test menu verifies important scenarios but is not a full automated test suite or a substitute for playtesting and profiling.

## Demonstration order

1. Start, walk and crouch; explain movement/noise tradeoffs.
2. Toggle Tab to show the guard's actual state and perception.
3. Enter the edge of the cone to show suspicion, then break sight with cover.
4. Aim near the guard and press Q to demonstrate investigation.
5. Retrieve the cyan core with E and return to extraction; press E again.
6. Show the relevant scripts and describe one limitation you would improve next.
