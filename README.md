# SudokuWASM

A modern, responsive Sudoku game built with Blazor WebAssembly, featuring offline capabilities and progressive web app (PWA) functionality.

**Play now at: [sudokuwasm.duckdns.org](https://sudokuwasm.duckdns.org)**

## Features

- **Cross-Platform**: Runs on any device with a web browser
- **Offline Support**: Play without an internet connection thanks to service worker caching
- **Progressive Web App**: Install on mobile devices for native-like experience
- **Responsive Design**: Optimized layouts for both desktop and mobile devices
- **Multiple Difficulty Levels**: Easy, Medium, Hard, and Expert puzzles
- **Smart Note-Taking**: Pencil mode for candidate numbers
- **Game Statistics**: Track your progress and performance
- **Auto-Save**: Never lose your progress with automatic game state persistence
- **Hint System**: Get help when you're stuck
- **Undo/Redo**: Make mistakes? No problem!
- **Beautiful UI**: Modern design with Tailwind CSS and smooth animations

## Architecture

This project follows a clean, component-based architecture utilizing modern web technologies:

### Technology Stack

- **.NET 9.0**: Latest version of .NET for enhanced performance
- **Blazor WebAssembly**: Client-side C# execution in the browser
- **Tailwind CSS**: Utility-first CSS framework for styling
- **Progressive Web App (PWA)**: Service worker for offline capabilities
- **Local Storage**: Client-side persistence for game state and statistics

### Project Structure

```
SudokuWASM/
|-- Components/               # Reusable Blazor components
|   |-- DesktopGameLayout.razor    # Desktop-optimized layout
|   |-- MobileGameLayout.razor     # Mobile-optimized layout
|   |-- SudokuGrid.razor           # Main game grid component
|   |-- GameControls.razor         # Game control buttons
|   |-- NumberPad.razor            # Number input interface
|   |-- ScoreArea.razor            # Score and statistics display
|   |-- DifficultySelector.razor   # Difficulty selection
|   |-- LoadingScreen.razor        # Loading animation
|   |-- ConfirmationModal.razor    # Confirmation dialog
|   |-- GameOverModal.razor        # Game over screen
|   |-- VictoryModal.razor         # Victory celebration
|   |-- StatisticsModal.razor      # Game statistics view
|   `-- PauseOverlay.razor         # Pause screen overlay
|-- Pages/                   # Blazor pages
|   |-- SudokuGame.razor          # Main game page
|   `-- SudokuGame.razor.cs       # Game logic code-behind
|-- Services/                # Business logic and utilities
|   |-- IGamePersistenceService.cs           # Persistence interface
|   |-- LocalStorageGamePersistenceService.cs # Local storage implementation
|   |-- GameStatePersistenceService.cs       # Game state management
|   |-- GameScoringService.cs                # Scoring calculations
|   |-- GameTimingService.cs                 # Timer functionality
|   |-- GameAnimationService.cs              # Animation utilities
|   `-- CellStylingService.cs                # Dynamic styling
|-- Models/                  # Data models
|   `-- GameState.cs              # Game state and statistics models
|-- Core Game Logic/         # Sudoku engine
|   |-- SudokuBoard.cs            # Main board logic
|   `-- SudokuSolver.cs           # Puzzle generation and solving
|-- wwwroot/                 # Static web assets
|   |-- index.html               # Main HTML page
|   |-- manifest.webmanifest     # PWA manifest
|   |-- service-worker.js        # Service worker for offline support
|   |-- tailwind.css            # Compiled Tailwind styles
|   `-- css/app.css             # Custom styles
`-- Shared/                  # Shared components
    `-- GameErrorBoundary.razor   # Error handling
```
````````

## Core Components

### Game Engine
- **SudokuBoard**: Core game logic, puzzle validation, and state management
- **SudokuSolver**: Puzzle generation with guaranteed unique solutions
- **GameState**: Comprehensive state management for save/load functionality

### User Interface
- **Responsive Layouts**: Separate optimized layouts for desktop and mobile
- **Component Architecture**: Modular, reusable Blazor components
- **Dynamic Styling**: CSS classes generated based on game state

### Services Layer
- **Persistence**: Local storage-based game state and statistics persistence
- **Scoring**: Dynamic scoring system based on difficulty and performance
- **Timing**: Precise game timing with pause/resume functionality
- **Animation**: Smooth transitions and visual feedback

## Getting Started

### Prerequisites
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js](https://nodejs.org/) (for Tailwind CSS compilation)

### Development Setup

1. **Clone the repository**
   ```bash
   git clone <your-repository-url>
   cd SudokuWASM
   ```

2. **Install Tailwind CSS**
   ```bash
   npm install tailwindcss @tailwindcss/cli
   ```

3. **Start Tailwind CSS compilation** (in a separate terminal)
   ```bash
   npx @tailwindcss/cli -i ./styles/input.css -o ./wwwroot/tailwind.css --watch
   ```

4. **Restore dependencies and run**
   ```bash
   dotnet restore
   dotnet run
   ```

5. **Open your browser** to `https://localhost:5001`

### Building for Production

```bash
dotnet publish -c Release
```

The published files will be in `bin/Release/net9.0/publish/wwwroot/`

## Game Features

### Difficulty Levels
- **Easy**: 40+ clues, forgiving scoring
- **Medium**: 30-35 clues, balanced challenge
- **Hard**: 25-30 clues, advanced patterns
- **Expert**: 20-25 clues, maximum difficulty

### Scoring System
- Points awarded for correct placements
- Bonus multipliers for difficulty level
- Penalties for wrong moves
- Time-based scoring bonuses

### Statistics Tracking
- Games played and won per difficulty
- Best completion times
- Perfect games (no hints or mistakes)
- Historical performance data

## Progressive Web App

This Sudoku game is a full PWA with:
- **Offline Play**: Complete functionality without internet connection
- **Install Prompt**: Add to home screen on mobile devices
- **Background Updates**: Automatic updates when connection is restored
- **Responsive Design**: Native-like experience across all devices

## State Management

The game features comprehensive state management:
- **Auto-Save**: Game state is automatically saved after each move
- **Resume Games**: Interrupted games can be resumed exactly where you left off
- **Statistics Persistence**: Long-term tracking of your Sudoku journey
- **Cross-Session**: State persists across browser sessions

## Design Philosophy

- **Mobile-First**: Designed primarily for touch interfaces
- **Accessibility**: Keyboard navigation and screen reader support
- **Performance**: Optimized for smooth 60fps animations
- **User Experience**: Intuitive interface with helpful visual feedback

## Contributing

Contributions are welcome! This project demonstrates modern Blazor WebAssembly development practices including:
- Component-based architecture
- Service layer abstraction
- Client-side state management
- Progressive Web App implementation
- Responsive design patterns

## License

This project is open source and available under the [MIT License](LICENSE).

---

*Built with love using Blazor WebAssembly and .NET 9*