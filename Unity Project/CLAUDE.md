# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Unity curve editor project implementing a Bézier curve path creation system. The codebase provides tools for creating, editing, and utilizing smooth curves in Unity through custom editor functionality.

## Unity Version

Unity 6000.2.2f1 (6000.2.2f1)

## Core Architecture

The project follows a clean component-based architecture with three main layers:

### Core Path System
- **`Bezier.cs`**: Static utility class providing cubic and quadratic Bézier curve evaluation methods
- **`Path.cs`**: Serializable path data structure that manages Bézier curve points, handles path operations (add/split/delete segments), and provides automatic control point management
- **`PathCreator.cs`**: MonoBehaviour component that holds Path data and configuration for editor visualization (colors, sizes, display options)

### Editor Tools
- **`PathEditor.cs`**: Custom Unity editor for PathCreator that provides:
  - Scene view interaction (shift+click to add segments, right-click to delete)
  - Inspector GUI for path settings (closed/open paths, auto control points)
  - Real-time visual feedback with handle manipulation
  - Undo/Redo integration

### Runtime Examples
- **`PathPlacer.cs`**: Example component demonstrating how to use paths at runtime by placing objects along evenly-spaced points

## Key Design Patterns

### Point Storage System
- Points stored as flat List<Vector2> with every 3rd point being an anchor
- Control points stored adjacent to their anchors: [anchor, control, control, anchor, ...]
- Closed paths add connecting control points automatically

### Auto Control Point Management
- Automatic smooth curve generation when `AutoSetControlPoints` is enabled
- Manual control point adjustment available when disabled
- Intelligent neighbor-aware control point positioning

## Common Development Tasks

### Building/Testing
This is a Unity project - open in Unity Editor for development and testing. No separate build commands needed for the core path system.

### Working with the Path System
- Create new paths by adding PathCreator component to GameObjects
- Access path data through `PathCreator.path` property
- Use `Path.CalculateEvenlySpacedPoints()` for runtime object placement
- Extend editor functionality by modifying `PathEditor.cs`

## File Structure
```
Assets/
├── Bezier.cs                    # Core Bézier math utilities
├── Path.cs                      # Path data structure and operations  
├── PathCreator.cs               # Unity component wrapper
├── Editor/
│   └── PathEditor.cs           # Custom editor for path creation
└── Examples/
    └── PathPlacer.cs           # Runtime usage example
```