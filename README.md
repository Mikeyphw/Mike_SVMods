# Mike's Stardew Valley Mods

A collection of Stardew Valley mods focused on bird-related gameplay improvements, including hatching mechanics, rooster controls, and wild egg spawning.

## Mods Overview

This repository contains multiple mods that work together to enhance the bird/farming experience in Stardew Valley:

### BirdsOfAFeather (Core Library)
A shared dependency library providing common functionality used by other mods.

- **Mod ID**: `Mikew.birdsofafeather`
- **Version**: 1.0.1
- **Description**: A core mod for Mikew mods providing shared utilities, helpers, and game state queries.

**Features**:
- `DailyLuckHelper` - Daily luck calculations and chance adjustments
- `WeightedTable<T>` - Generic weighted random selection
- `GameStateQueries` - Complex game state evaluation
- `DataManager` - Generic data loading/saving
- `ObjectCloner` - Deep/shallow object cloning
- Extension methods and utilities
- Config management via Generic Mod Config Menu (GMCM)

### LetMeHatchThisEgg
Adds missing hatching data for animals that produce eggs but cannot be incubated.

- **Mod ID**: `Mikew.lmh`
- **Version**: 1.0.4
- **Nexus Mods**: [25175](https://www.nexusmods.com/stardewvalley/mods/25175)
- **Required Dependencies**: BirdsOfAFeather, ContentPatcher
- **Optional Dependencies**: Prismatic Valley Framework, LetMeHatchThisEgg (self), Blue Egg

**Features**:
- Adds hatching data for egg-producing animals
- Configurable breeding options
- Integration with RoosterAddon for rooster hatching

### RoosterAddon
An addon for the Roosters mod that enables hatching and control of roosters.

- **Mod ID**: `Mikew.roostersaddon.smapi`
- **Version**: 2.0.1
- **Nexus Mods**: [22555](https://www.nexusmods.com/stardewvalley/mods/22555)
- **Required Dependencies**: Meara.Roosters, ContentPatcher

**Features**:
- Rooster hatching from chicken eggs
- Rooster control options
- Color variants (blue, brown, white, gold, void)

### WildEggsHatching
Adds wild egg spawning throughout the game world with a progression system.

- **Status**: Actively refactored
- **Architecture**: Clean architecture with event-driven communication

**Features**:
- Wild egg spawning in various locations
- 6-step progression system
- Note-based clues for egg locations
- Multiple egg types (small/large, regular/magic)

## Project Structure

```
Mike_SVMods/
├── Directory.Build.props          # Shared build configuration
├── README.md                       # This file
├── BirdsOfAFeather/               # Core shared library
│   ├── Framework/                 # Config, APIs, interfaces
│   ├── Helpers/                   # Utility classes and extensions
│   ├── Models/                    # Data models
│   └── Tests/                     # Unit tests
├── LetMeHatchThisEgg/             # Hatching data mod
│   ├── Breeding/                  # Breeding system
│   ├── Cloning/                   # Animal cloning
│   ├── Config/                    # Configuration
│   ├── Framework/                 # API interfaces
│   ├── Helpers/                   # Utility classes
│   ├── Integrations/              # Third-party integrations
│   ├── Managers/                  # Game asset management
│   └── UI/                        # Menu interfaces
├── RoosterAddon/                  # Rooster control addon
│   ├── Framework/                 # API interfaces
│   ├── Helpers/                   # Utility classes
│   └── assets/                    # Sprites and textures
├── WildEggsHatching/              # Wild egg spawning
│   ├── refactor/                  # New clean architecture
│   │   ├── Core/                  # Domain models, events, enums
│   │   ├── Data/                  # Data loading
│   │   ├── State/                 # State persistence
│   │   ├── Progression/           # Progression logic
│   │   ├── Spawning/              # Egg spawning
│   │   ├── Tracking/              # Hatch detection
│   │   └── Notes/                 # Note spawning
│   └── WildEggsHatching_orig/    # Original implementation
└── Tests/
    └── BirdsOfAFeather.Tests/    # Unit tests
```

## Building

### Prerequisites

- .NET 8.0 SDK
- Visual Studio Code or Visual Studio 2022 (optional)

### Build Commands

```bash
# Build all mods
dotnet build

# Build specific mod
dotnet build BirdsOfAFeather/BirdsOfAFeather.csproj
dotnet build LetMeHatchThisEgg/LetMeHatchThisEgg.csproj
dotnet build RoosterAddon/RoosterAddon.csproj

# Run tests
dotnet test
```

### Output

Built mods are located in each mod's `bin/` directory:
- `BirdsOfAFeather/bin/Debug/net8.0/BirdsOfAFeather.dll`
- `LetMeHatchThisEgg/bin/Debug/net8.0/LetMeHatchThisEgg.dll`
- `RoosterAddon/bin/Debug/net8.0/RoosterAddon.dll`

## Shared Components

### BirdsOfAFeather provides:

| Component | Description |
|-----------|-------------|
| `DailyLuckHelper` | Daily luck calculations and chance adjustments |
| `WeightedTable<T>` | Generic weighted random selection |
| `GameStateQueries` | Complex game state evaluation |
| `DataManager<T>` | Generic data loading/saving |
| `ObjectCloner` | Deep/shallow object cloning |
| `CoreModConfig` | Base configuration class |
| Extension Methods | Various utility extensions |

### Architecture Patterns

- **Event-Driven** (WildEggsHatching refactor): Publish/Subscribe via EventBus
- **Service-Oriented**: Separate services for spawning, progression, tracking
- **Clean Architecture**: Clear separation between domain, data, and infrastructure layers

## Mod Dependencies

```
RoosterAddon
    └── Meara.Roosters (required)
    └── Mikew.lmh (optional)

LetMeHatchThisEgg
    └── Mikew.birdsofafeather (required)
    └── ContentPatcher (required)

WildEggsHatching
    └── Mikew.birdsofafeather (recommended)
```

## Configuration

All mods support [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098) for in-game configuration.

### BirdsOfAFeather Options
- **Extra Debug Info**: Enable verbose logging
- **Testing**: Enable testing-specific logging

## Development

### Code Style

- C# 11.0 with nullable reference types
- XML documentation for public APIs
- Consistent naming conventions

### Testing
