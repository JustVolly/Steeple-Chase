<div align="center">

# Steeple Chase

### A 3D obstacle-platformer focused on responsive traversal, camera feel, and reusable physics-driven hazards.

`Unity 6000.5.2f1` · `C#` · `Unity Input System` · `Rigidbody Physics`

</div>

---

## Overview

**Steeple Chase** is a Unity 3D obstacle-platforming project built around traversal through moving, rotating, swinging, and orbiting hazards. The engineering focus is on reusable gameplay systems rather than one-off scene scripting: character feel, camera behaviour, moving platforms, and obstacle motion are implemented as configurable components that can be reused across different environments.

The development repository contains several gameplay/test environments, including `Main`, `DeathRun`, `Underworld`, `heaven`, and `neon`.

> **Portfolio status:** private development source. Public-facing media can be published separately without exposing the complete project or third-party assets.

---

## What I Built

### Responsive Rigidbody Character Controller

`ProfessionalCharacterController` is a custom Rigidbody-based controller with:

- camera-relative movement
- walk, run, and run-boost states
- keyboard and optional mobile joystick input
- coyote time
- jump buffering
- slope-aware ground detection
- grounded-state loss buffering
- configurable air control
- smoothed visual-model rotation
- animation parameter integration
- collision/rotation stability controls
- runtime debug overlay and ground-check gizmos

The controller separates the physical player root from the rotating visual model, keeping collisions stable while preserving responsive facing and animation behaviour.

### Reusable Platform Camera

`ProfessionalPlatformCamera` provides a configurable camera system with:

- independent X/Y/Z follow controls
- smooth position tracking
- optional dead zone
- movement look-ahead
- fixed-height and bounded-height modes
- perspective and orthographic zoom control
- camera collision handling
- manual, target, and world-point rotation modes
- finish-target focus blending
- camera shake
- debug gizmos

The camera is reusable across scenes and can be tuned without hard-coding a specific level layout.

### Physics-Driven Hazard Systems

The obstacle layer is component-based and includes:

- circular/orbiting hammer motion
- mirrored orbit behaviour
- continuous, ping-pong, and target-angle movement modes
- swinging hazards
- rotating platforms
- reusable two-point moving platforms
- configurable easing curves
- Rigidbody-compatible movement
- body-relative and tangent-based obstacle orientation

`HammerOrbitAroundBody` can follow a generated body circumference while changing orientation strategy independently from its motion path. `TwoPointMover` exposes forward/return speeds, wait times, easing modes, custom curves, and Rigidbody movement as reusable Inspector settings.

---

## Engineering Highlights

### Movement Feel Over Minimal Input Mapping

The character controller includes coyote time and jump buffering so a jump can still register around the edge of a platform or shortly before landing. Air control, run boost, slope validation, and visual rotation are independently tunable.

### Camera as a Gameplay System

Instead of a basic `transform.position = target.position + offset` follow script, the camera composes tracking, dead-zone logic, look-ahead, bounds, collision correction, rotation, zoom, finish focus, and shake into a reusable pipeline.

### Data-Driven Obstacle Tuning

Hazard scripts expose movement modes and tuning through serialized fields. Designers can create different obstacle behaviours from the same component by changing speed, endpoints, angles, waits, easing, mirroring, and rotation strategy rather than duplicating scripts.

### Defensive Runtime Configuration

The portfolio pass keeps existing serialized fields and public entry points intact while improving validation around reusable components. For example, `TwoPointMover` now clamps invalid speed/wait values during editor validation and safely handles reset calls when movement endpoints are missing.

---

## Selected Source

| System | File | What it demonstrates |
| --- | --- | --- |
| Character movement | `Assets/Scripts/ProfessionalCharacterController.cs` | Rigidbody movement, jump feel, state handling, input and animation integration |
| Platform camera | `Assets/Scripts/ProfessionalPlatformCamera.cs` | Smooth follow, collision, look-ahead, focus and camera feedback |
| Circular hazard | `Assets/Scripts/HammerOrbitAroundBody.cs` | Reusable orbit motion, mirroring, motion modes and orientation strategies |
| Swinging hazards | `Assets/Scripts/HammerBodySwing.cs` / `Assets/Scripts/TrapHammerAroundBody.cs` | Configurable obstacle motion |
| Moving platform | `Assets/Scripts/TwoPointMover.cs` | Bidirectional platform motion, easing curves and Rigidbody support |
| Orbit geometry | `Assets/Scripts/BodyCircumferenceDrawer.cs` | World-space path/orbit support for gameplay hazards |

---

## Controls

Default desktop input implemented by the custom controller:

- **Move:** `WASD` or arrow keys
- **Jump:** `Space`
- **Run / build boost:** `Left Shift`
- **Mobile:** optional `FixedJoystick` plus public jump/run button callbacks

Bindings and UI wiring can still be configured per scene through Unity.

---

## Project Structure

```text
Steeple-Chase/
├── Assets/
│   ├── Animation/
│   ├── Funny_Characters/
│   ├── Joystick/
│   ├── Platform/
│   ├── Scenes/
│   ├── Scripts/
│   └── Settings/
├── Packages/
├── ProjectSettings/
└── README.md
```

---

## Tech Stack

- **Engine:** Unity `6000.5.2f1`
- **Language:** C#
- **Input:** Unity Input System + optional mobile joystick
- **Movement:** Rigidbody-based 3D controller
- **Camera:** custom reusable follow/collision system
- **Environment:** reusable physics-driven obstacle components

---

## Portfolio Media

The recommended recruiter-facing screenshots, hero GIF, and short gameplay reel are defined in [`docs/portfolio/CAPTURE_GUIDE.md`](docs/portfolio/CAPTURE_GUIDE.md). The capture plan prioritizes movement feel and system variety instead of showing every development scene.

---

<div align="center">

**VÜSAL ALİYEV**

*Game Developer · Software Engineer*

</div>
