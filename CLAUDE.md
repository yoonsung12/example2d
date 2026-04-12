# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Unity 2D game project using:
- **Unity 6** (version 6000.0.68f1)
- **Universal Render Pipeline (URP)** 17.0.4
- **New Input System** 1.18.0
- **Visual Scripting** 1.9.7
- **MCP for Unity** (CoplayDev, GitHub) — AI assistant integration

## Key Packages

| Package | Version | Purpose |
|---------|---------|---------|
| com.unity.feature.2d | 2.0.1 | 2D toolkit (Animation, Tilemap, SpriteShape, Aseprite, etc.) |
| com.unity.render-pipelines.universal | 17.0.4 | URP rendering |
| com.unity.inputsystem | 1.18.0 | New Input System |
| com.unity.visualscripting | 1.9.7 | Visual scripting |
| com.unity.timeline | 1.8.10 | Timeline/cutscene system |
| com.unity.ugui | 2.0.0 | UI Toolkit / uGUI |
| com.unity.test-framework | 1.6.0 | Unity Test Runner |

## Project Settings

- **Color Space**: Linear
- **Target**: Handheld / Portrait orientation
- **Default Resolution**: 1920×1080
- **Background Running**: Disabled
- **Unsafe Code**: Disabled

##반드시 지켜야할 점
-OOP 기반 설계
-계획부터 말하고 승인 받은 후에 작업 진행
-최적화를 고려한 코드 작성
-SOLID 원칙과 디자인 패턴 적용

## Project Structure

```
Assets/
├── Scenes/          # Game scenes (SampleScene.unity is the default)
├── Settings/        # URP renderer and render pipeline assets
│   └── Scenes/      # URP 2D scene template
├── InputSystem_Actions.inputactions   # Input action map
└── DefaultVolumeProfile.asset         # Post-processing volume
```

Scripts should be placed in `Assets/Scripts/`. Sprites, prefabs, and other assets follow standard Unity conventions.

## Development Workflow

Unity has no CLI build command — all building, testing, and running happens inside the Unity Editor.

- **Open project**: Launch Unity Hub and open `C:\Unityproject\2d`
- **Run tests**: Window → General → Test Runner → Run All (uses com.unity.test-framework)
- **Build**: File → Build Settings → Build
- **Play**: Press the Play button in the Editor toolbar

### Scripting Notes

- Use the **New Input System** (`InputSystem_Actions.inputactions`) — do not use legacy `Input.GetKey` APIs.
- URP shaders are required for all materials — do not use Built-in RP shaders.
- Visual Scripting graphs are stored alongside prefabs/GameObjects; prefer C# for complex logic.
- Unsafe code is disabled in the project; avoid `unsafe` blocks.

## MCP Integration

The project includes `com.coplaydev.unity-mcp`, which enables Claude Code (and other AI assistants) to interact with the Unity Editor directly via MCP. This allows scene inspection, asset querying, and Editor state access from this CLI.
