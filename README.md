<div align="center">

# Steeple Chase

### A Unity obstacle-platforming project focused on responsive movement, camera feel, and reusable gameplay hazards.

`Unity 6000.5.2f1` · `C#` · `Unity Input System` · `Physics-driven gameplay`

</div>

---

## Overview

**Steeple Chase** is a 3D Unity project built around traversal through obstacle-heavy environments. The project is used to explore and implement responsive character movement, camera behaviour, and reusable moving hazard systems rather than relying on one-off scene scripting.

The repository currently contains multiple playable/test scenes, including `Main`, `DeathRun`, `Underworld`, `heaven`, and `neon`.

> **Portfolio status:** Private development repository. Gameplay media and a public showcase build can be added separately without exposing the full project source.

---

## Technical Highlights

### Character Controller

The custom `ProfessionalCharacterController` is Rigidbody-based and includes:

- camera-relative movement
- walk / run / run-boost states
- keyboard and optional mobile joystick input
- coyote time
- jump buffering
- slope-aware ground detection
- configurable air control
- visual-model rotation smoothing
- animation parameter integration
- collision/rotation stability controls
- runtime debug support

This gives the controller a more forgiving and responsive feel than a minimal movement implementation while keeping the tuning exposed through serialized settings.

### Platform Camera

`ProfessionalPlatformCamera` implements a configurable follow system with:

- per-axis follow controls
- position smoothing
- dead-zone support
- movement look-ahead
- fixed or bounded camera height
- perspective / orthographic zoom control
- camera collision handling
- target / world-point look modes
- finish-target focus
- camera shake
- debug gizmos

The camera is designed as a reusable gameplay system rather than being hard-coded to a single scene.

### Obstacle & Hazard Systems

The project contains reusable C# components for obstacle motion, including:

- circular hammer/orbit movement
- mirrored orbit behaviour
- continuous, ping-pong and target-angle motion modes
- hammer swing behaviour
- rotating platforms
- two-point moving platforms
- Rigidbody-compatible obstacle motion
- runtime debug controls

`HammerOrbitAroundBody` also supports several orientation strategies such as tangent-following, body-relative rotation, looking toward/away from the orbit centre, and preserving the initial rotation.

---

## Selected Source

| System | File | What it demonstrates |
| --- | --- | --- |
| Character movement | `Assets/Scripts/ProfessionalCharacterController.cs` | Rigidbody movement, jump feel, state handling, input and animation integration |
| Platform camera | `Assets/Scripts/ProfessionalPlatformCamera.cs` | Smooth follow, collision, look-ahead, focus and camera feedback |
| Circular hazard | `Assets/Scripts/HammerOrbitAroundBody.cs` | Reusable obstacle motion, multiple motion modes and orientation strategies |
| Swinging hazards | `Assets/Scripts/HammerBodySwing.cs` / `TrapHammerSwing.cs` | Configurable moving obstacle behaviour |
| Moving platforms | `Assets/Scripts/TwoPointMover.cs` / `TrapPlatformRotator.cs` | Reusable environment motion systems |
| Orbit geometry | `Assets/Scripts/BodyCircumferenceDrawer.cs` | World-space path/orbit support for gameplay hazards |

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

## Scenes

The repository includes several environment / gameplay scenes used during development:

- `Main`
- `DeathRun`
- `Underworld`
- `heaven`
- `neon`

A polished portfolio version should surface only the strongest finished route(s) through gameplay video or GIFs instead of asking a reviewer to inspect every development scene.

---

## Portfolio Presentation Plan

For a public-facing version, the recommended review path is:

**Gameplay clip → Core mechanic → My implementation → Selected source → Technical decisions → Result**

The next presentation upgrades are:

1. Add a 10–20 second gameplay GIF/video at the top of this README.
2. Add 3–5 screenshots showing distinct obstacles/environments.
3. Add a short controls section.
4. Add one concise technical breakdown diagram for movement + camera + obstacle interaction.
5. Add a downloadable build or unlisted gameplay video when ready.

---

## Development Notes

- **Engine:** Unity Editor `6000.5.2f1`
- **Language:** C#
- **Input:** Unity Input System with optional mobile joystick support
- **Movement:** Rigidbody-based
- **Repository:** private development source

---

<div align="center">

**VÜSAL ALİYEV**

*Build it. Show it. Explain it.*

</div>
