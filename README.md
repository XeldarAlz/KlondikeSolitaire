# Klondike Solitaire

A classic Klondike Solitaire card game built in Unity 6. The entire project was generated through AI-assisted automation using [Helm](https://github.com/XeldarAlz/helm) — from game design document to fully tested implementation.

> Completed in **9 phases**, **45 tasks**, and **335 passing tests** (100% success rate).

## About

This repository is both a playable solitaire game and a reference project demonstrating what a disciplined, multi-agent AI pipeline can produce: strict Model-View-System architecture, pure C# game logic with zero Unity coupling, comprehensive automated tests, and a draw-call budget honored from the first commit.

- **Clean Klondike rules** — standard draw-1, alternating-color tableau, A→K foundations
- **Smooth animations** — tweened moves, flip, auto-complete, and the iconic win cascade
- **Unlimited undo** with score reversal
- **Cycling hint system** and automatic no-moves detection
- **No menus, no ads, no distractions** — just the game

## Performance

| Metric | Value |
|--------|-------|
| FPS | ~1000+ |
| Draw Calls | 5 |
| Batches | 5 (40 saved by batching) |
| CPU (main) | ~0.9 ms |
| Scripts | ~0.131 ms |
| Rendering | ~0.040 ms |
| Textures | 5 / ~9.1 MB |

### Rendering Optimizations

- **Dynamic batching** — 42 sprite draws reduced to 2 batches
- **Strip sprites** — overlapping cards use cropped sprites, minimal overdraw
- **Hidden renderers** — only the top card of a stack is rendered
- **Sprite atlas** — one atlas, all cards in the same batch
- **Object pool** — 52 `CardView` instances created upfront, zero runtime `Instantiate`

## Architecture

```
Core/       → Pure C# (models, enums, messages) — no Unity dependencies
Systems/    → Game logic (deal, move, undo, hint, scoring)
Views/      → Unity layer (board, input, animation, UI)
```

- **VContainer** for dependency injection — each system receives only what it needs
- **MessagePipe** for pub/sub — loosely-coupled cross-system communication
- **Assembly definitions** enforce layer isolation at compile time
- **Input System (new)** — all input routed through a thin `InputView` adapter
- **UniTask** everywhere — no coroutines, full cancellation support

## Tech Stack

Unity 6 · URP · VContainer · MessagePipe · UniTask · PrimeTween · Input System · NUnit / Unity Test Framework

## Testing

335 tests across EditMode and PlayMode, all passing:

- **EditMode (299):** Every core system — deal, move, undo, hint, scoring, auto-complete, validation, enumeration, no-moves detection, game flow
- **PlayMode (36):** Integration tests across the full view/system stack

Run via `Window → General → Test Runner` in the Unity Editor.

## Getting Started

**Requirements:** Unity 6 (6000.0+).

1. Clone this repository
2. Open the project in Unity Hub
3. Open `Assets/Scenes/Game.unity`
4. Press **Play**

## How to Play

- **Drag** or **click** a card to move it
- Build the **tableau** down in alternating colors (red ↔ black)
- Build the four **foundations** up by suit, Ace to King
- Click the **stock** (top-left) to draw to the waste
- When the stock is empty, click it again to recycle the waste
- Use **Undo** to reverse the last move (score adjusts accordingly)
- Use **Hint** to cycle through valid moves
- Win by placing all 52 cards onto the foundations

## Documentation

Full project documentation lives in [`docs/`](docs/):

| Document | Description |
|----------|-------------|
| [GDD.md](docs/GDD.md) | Game Design Document — design decisions, mechanics, rules |
| [TDD.md](docs/TDD.md) | Technical Design Document — architecture, systems, dependencies |
| [WORKFLOW.md](docs/WORKFLOW.md) | Phased execution plan with tasks |
| [PROGRESS.md](docs/PROGRESS.md) | Orchestrator progress tracking |

## About the Pipeline

The code, tests, and documentation in this repository were produced by [Helm](https://github.com/XeldarAlz/helm), a multi-agent orchestration system for Claude Code that coordinates specialized agents (architect, coder, tester, reviewer, committer) through a phased build. Every rule in `.claude/rules/` — pure-C# logic, zero allocations on hot paths, mandatory tests, draw-call budgeting — was enforced automatically during generation.

## Author

**Serdar Alemdar** — [@XeldarAlz](https://github.com/XeldarAlz)

Contributions, issues, and feedback are welcome.
