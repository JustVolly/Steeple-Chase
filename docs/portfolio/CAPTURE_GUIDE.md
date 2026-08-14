# Steeple Chase — Portfolio Capture Guide

This guide defines a compact media set for a recruiter-facing showcase. The goal is to demonstrate movement feel, camera quality, and reusable obstacle systems without exposing the full private development repository.

## Hero GIF — 8 to 12 seconds

Use one visually strong route and keep the character moving for the entire clip:

1. Start with a clean forward run.
2. Jump across a gap or onto a moving platform.
3. Pass one rotating/orbiting or swinging hazard.
4. Show the camera following smoothly through the movement.
5. End with a successful landing or obstacle clear.

Recommended source: 16:9, 1920×1080, 60 FPS. Export a lighter WebP/GIF/MP4 derivative for GitHub while keeping the original capture for a portfolio site or reel.

## Screenshots

Capture 4–5 images that each demonstrate a different system:

- **Hero traversal:** character and environment composition at a readable scale.
- **Orbit hazard:** hammer/orbit obstacle with the path visually understandable.
- **Moving platform:** player interacting with a two-point or rotating platform.
- **Distinct environment:** strongest frame from `Underworld`, `neon`, or another visually different scene.
- **Camera composition:** a frame that shows useful depth and framing rather than a close-up.

Do not use Scene view, wireframes, Inspector panels, debug overlays, editor gizmos, or repeated angles.

## Short Gameplay Reel — 25 to 40 seconds

Suggested edit:

- 0–4 s — title card: `Steeple Chase — 3D Obstacle Platformer`
- 4–12 s — responsive run + jump sequence
- 12–20 s — moving platform / rotating obstacle
- 20–28 s — orbiting or swinging hammer section
- 28–35 s — visually distinct environment and clean finish
- final 2–3 s — `Unity · C# · Vüsal Aliyev`

Prioritize successful traversal and system variety. Avoid long menus, repeated failures, static shots, and test-only content.

## Optional Technical Clip

A separate 10–15 second engineering clip can be useful for a portfolio site:

- show the same obstacle with two different Inspector configurations
- demonstrate a `TwoPointMover` easing change
- demonstrate camera look-ahead or collision response
- show coyote time / jump buffering only if the behaviour is visually clear

This clip should complement gameplay, not replace it.

## README Media Layout

When captures are ready, place publishable files under:

```text
docs/portfolio/media/
├── steeple-chase-hero.webp
├── steeple-chase-orbit.webp
├── steeple-chase-platform.webp
├── steeple-chase-environment.webp
└── steeple-chase-camera.webp
```

Place the hero animation directly below the README introduction. Use screenshots after `What I Built` and before the deeper engineering breakdown.

## Quality Checklist

- standalone build or clean Game view
- 16:9 framing
- stable 60 FPS source capture when possible
- no Unity editor chrome
- no debug overlay or gizmos
- show the player interacting with systems, not empty scenery
- one clear mechanic per screenshot
- keep camera motion smooth
- verify publication rights for every visible third-party asset before public release
