# Klondike Solitaire — Game Design Document
**Version:** 1.0
**Date:** 2026-04-08
**Status:** Complete — Ready for Architecture Phase

---

## 1. Executive Summary

A classic Klondike Solitaire card game built in Unity 6 for desktop. Single-screen gameplay with no menus, no sound, and no monetization — pure card game experience. Features smooth animations for all card movements, unlimited undo with score reversal, a cycling hint system, auto-complete detection, and the iconic cascading win animation. Landscape orientation at 1920x1080.

---

## 2. Core Concept

- **Genre:** Card Game / Patience / Solitaire
- **Sub-genre:** Klondike (single-player)
- **Core Fantasy:** The satisfying zen of sorting chaos into order — the "one more game" compulsion of classic solitaire
- **Unique Selling Points:** Clean implementation with smooth animations, hint cycling, and auto-complete
- **Reference Games:** Microsoft Solitaire (Windows classic), Solitaire by MobilityWare
- **Differentiation:** Stripped to essentials — no ads, no menus, no distractions. Just the game.

---

## 3. Target Audience & Platform

- **Demographics:** Anyone who enjoys classic card games
- **Platform:** Desktop (standalone)
- **Resolution:** 1920x1080, landscape orientation
- **Session Length:** 2–15 minutes per game
- **Play Pattern:** Pick up and play, no commitment required

---

## 4. Core Gameplay Loop

```
Deal → Evaluate Board → Make Move → Score Update → Repeat
                                         │
                              ┌──────────┴──────────┐
                              ▼                      ▼
                      All cards on           No valid moves
                      foundations?            detected?
                         │                      │
                         ▼                      ▼
                    WIN CASCADE            "No more moves"
                    ANIMATION               prompt shown
                         │                      │
                         ▼                      ▼
                     New Game               New Game
```

**Detailed Flow:**

1. **Deal:** 28 cards dealt to 7 tableau columns (column N gets N cards, top card face-up). Remaining 24 cards form the stock pile.
2. **Play:** Player moves cards between tableau, foundations, waste, and stock using drag-and-drop or tap-to-move.
3. **Score:** Points awarded/deducted per move (see Section 5.2).
4. **Auto-Complete Check:** After every move, check if all cards are face-up and stock/waste are empty. If so, show auto-complete button.
5. **No-Moves Check:** After every move, check if any valid moves remain. If none, display prompt.
6. **Win:** When all 52 cards are on the 4 foundations (A→K per suit), trigger cascading win animation.

---

## 5. Game Mechanics

### 5.1 Table Layout

All areas exist within a single game screen in landscape orientation:

| Area | Count | Description |
|------|-------|-------------|
| **Stock** | 1 | Face-down draw pile (top-left). Click/tap to draw 1 card to waste. |
| **Waste** | 1 | Face-up pile next to stock. Top card is playable. |
| **Foundations** | 4 | Build piles (top-right). Build A→K by suit. |
| **Tableau** | 7 | Main play columns (bottom). Build K→A in alternating colors. |

**Empty slot visuals:**
- Empty tableau column: card base outline (accepts only Kings)
- Empty foundation: card base outline with suit indicator
- Empty stock: card base outline (click to recycle waste back to stock)

### 5.2 Scoring (Classic Point-Based)

| Action | Points |
|--------|--------|
| Waste → Tableau | +5 |
| Waste → Foundation | +10 |
| Tableau → Foundation | +10 |
| Foundation → Tableau | −15 |
| Turn over face-down Tableau card | +5 |
| Recycle waste to stock | No penalty (Draw 1 mode) |

- **Minimum score:** 0 (clamped, never displays negative)
- **Maximum theoretical score:** 760 (all cards to foundation via optimal path + all tableau flips)
- Score displayed on HUD at all times
- Undo reverses the score change of the undone action

### 5.3 Card Movement Rules

**Tableau Rules:**
- Cards build downward in alternating colors (red on black, black on red)
- Any face-up sub-sequence of correctly ordered cards can be moved as a group
- Only a King (or King-led sequence) can be placed on an empty tableau column
- When a face-down card is exposed by moving cards off it, it is automatically flipped face-up (+5 points)

**Foundation Rules:**
- Each foundation builds upward by suit: A → 2 → 3 → ... → Q → K
- Only one card at a time can be moved to/from a foundation
- Cards can be moved back from foundation to tableau (−15 points)

**Stock/Waste Rules:**
- Click/tap stock to draw 1 card face-up to waste
- Only the top waste card is playable
- When stock is empty, click/tap the empty stock area to recycle all waste cards back to stock (maintaining order, no shuffle)
- Unlimited passes through the stock

### 5.4 Input Mechanics

Both input methods are supported simultaneously:

**Drag-and-Drop:**
- Press and hold on a valid card to pick it up
- Card (and any cards below it in a tableau sub-sequence) follow the pointer
- Dragged cards render above all other cards (elevated z-order)
- Release over a valid target: card animates to snap into place
- Release over an invalid target or empty space: card animates back to original position with a light shake

**Tap-to-Move:**
- Single tap on a playable card: auto-moves to the best valid target
- Priority order for auto-move target: Foundation → Tableau (leftmost valid column)
- Single tap on stock: draws 1 card to waste
- Single tap on empty stock: recycles waste to stock

**Double-Tap:**
- Double-tap on any card that can legally go to a foundation: auto-sends to foundation with animation

### 5.5 Undo System

- **Scope:** Unlimited undo, all the way back to the initial deal
- **Granularity:** Each individual action is one undo step
- **Score reversal:** Undo reverses the score change of the undone action
- **Actions that create undo steps:**
  - Move card(s) between any piles (tableau, foundation, waste)
  - Draw card from stock to waste
  - Recycle waste to stock
  - Flip face-down tableau card (auto-triggered, counts as part of the move that exposed it — single undo step)
- **Undo of auto-complete:** Each card moved during auto-complete is an individual undo step
- **Undo button** on the HUD, disabled when undo stack is empty

### 5.6 Hint System

- **Trigger:** Hint button on HUD
- **Behavior:** Highlights one valid move — the source card and all its valid destination(s)
- **Cycling:** Each subsequent press of the hint button cycles to the next available valid move
- **Cycle resets** when the board state changes (any move, undo, draw)
- **No valid moves:** Nothing happens (button press is a no-op)
- **Highlight style:** Visual emphasis on the card and valid drop zones (glow, outline, or color tint — implementation decides)

### 5.7 Auto-Complete

- **Detection condition:** All 52 cards are face-up AND stock pile is empty AND waste pile is empty
- **Trigger:** Auto-complete button appears on HUD when condition is met
- **Behavior:** Cards are automatically moved to foundations one at a time, animated, in optimal order (lowest available card first across all tableau columns)
- **Scoring:** Each card moved to foundation during auto-complete scores normally (+10 per card)
- **Undo:** Each auto-complete card movement is an individual undo step

### 5.8 No-Moves Detection

- **Check:** After every move, evaluate if any valid moves remain across the entire board (including drawing from stock, recycling waste)
- **Condition:** No cards can be moved to any valid position AND stock has no cards to draw AND waste cannot be recycled (or recycling would not produce new moves — simplified: if stock and waste are both empty and no tableau/foundation moves exist)
- **Prompt:** Display "No more moves available" overlay/message with a "New Game" button
- **Note:** This is a best-effort detection. The check evaluates if any immediate move is possible, not deep solvability analysis

### 5.9 Win Condition & Celebration

- **Condition:** All 4 foundations contain 13 cards each (A through K)
- **Animation:** Classic cascading card animation — cards launch from foundations and bounce off the edges of the screen with physics-like trajectories
- **Post-win:** Display final score, "New Game" button overlaid on the cascade animation

---

## 6. Game Systems

### 6.1 Card System

- **Purpose:** Represent and manage the 52-card deck
- **Data:** Each card has a Suit (Hearts, Diamonds, Clubs, Spades) and Rank (A, 2–10, J, Q, K)
- **State:** Face-up or face-down
- **Color:** Red (Hearts, Diamonds) or Black (Clubs, Spades) — derived from suit
- **Visual assembly:** Cards are composed from layered sprites:
  - Card front background (`card_front.png`)
  - Card back (`card_back.png`) when face-down
  - Rank label (from `card numbers/new/`)
  - Suit symbol (from `semi/new/`)
  - Figure artwork for J, Q, K (from `figures/red/` or `figures/black/`)

### 6.2 Pile System

- **Purpose:** Manage the 13 piles (1 stock, 1 waste, 4 foundations, 7 tableau columns)
- **Operations:** Add card(s), remove card(s), peek top card, check if accepts card(s)
- **Validation:** Each pile type has its own acceptance rules (see Section 5.3)
- **State:** Ordered list of cards per pile, tracks which are face-up/down

### 6.3 Move Validation System

- **Purpose:** Determine if a proposed move is legal
- **Inputs:** Source card(s), source pile, destination pile
- **Outputs:** Boolean (valid/invalid)
- **Used by:** Drag-and-drop validation, tap-to-move target selection, hint system, no-moves detection, auto-complete

### 6.4 Scoring System

- **Purpose:** Track and display the player's score
- **State:** Current score (integer, clamped to 0)
- **Operations:** Add points, deduct points (with clamp), reset
- **Integration:** Receives score events from move execution, supports reversal via undo

### 6.5 Undo System

- **Purpose:** Record and reverse all game actions
- **State:** Stack of action records (command pattern)
- **Each record stores:** Source pile, destination pile, card(s) moved, score delta, any auto-flip that occurred
- **Operations:** Push action, pop and reverse action, clear stack

### 6.6 Hint System

- **Purpose:** Find and cycle through valid moves
- **State:** List of all currently valid moves, current cycle index
- **Operations:** Scan board for valid moves, highlight current hint, advance to next hint
- **Reset trigger:** Any board state change

### 6.7 Auto-Complete System

- **Purpose:** Detect trivially winnable state and execute automated foundation moves
- **Detection:** Check after every move if all conditions are met (all face-up, no stock/waste)
- **Execution:** Iteratively move the lowest-rank available card to its foundation, animated with delay between each

### 6.8 No-Moves Detection System

- **Purpose:** Detect when no valid moves remain
- **Check:** Enumerate all possible moves. If zero, trigger prompt.
- **Scope:** Checks tableau→tableau, tableau→foundation, waste→tableau, waste→foundation, stock draw, waste recycle

### 6.9 Deal System

- **Purpose:** Shuffle and deal a new game
- **Operations:** Shuffle 52 cards (Fisher-Yates), deal to 7 tableau columns, remainder to stock
- **Reset:** Clears all piles, resets score to 0, clears undo stack

---

## 7. UI/UX Flow

### 7.1 Screen Inventory

**Single screen only — the Game Screen:**

```
┌─────────────────────────────────────────────────────┐
│  [Stock] [Waste]          [F1] [F2] [F3] [F4]      │
│                                                      │
│                                                      │
│   [T1] [T2] [T3] [T4] [T5] [T6] [T7]              │
│                                                      │
│                                                      │
│                                                      │
│                                                      │
├─────────────────────────────────────────────────────┤
│  HUD: [Score: 0]    [Undo] [Hint] [New Game]       │
│       [Auto-Complete] (shown conditionally)          │
└─────────────────────────────────────────────────────┘
```

### 7.2 HUD Elements

| Element | Position | Behavior |
|---------|----------|----------|
| Score | Bottom-left area | Displays "Score: {value}", updates on every scoring action |
| Undo button | Bottom bar | Disabled (dimmed) when undo stack is empty. Icon: `undo.png` |
| Hint button | Bottom bar | Always enabled. Icon: `hints.png` |
| New Game button | Bottom bar | Always enabled. Deals fresh game immediately. Icon: `new-game.png` |
| Auto-Complete button | Bottom bar (conditional) | Only visible when auto-complete condition is met. Disappears after activation or if condition breaks (undo) |

### 7.3 Overlays

| Overlay | Trigger | Content |
|---------|---------|---------|
| No Moves | No valid moves detected | "No more moves available" text + "New Game" button |
| Win | All foundations complete | Final score display + cascade animation + "New Game" button |

### 7.4 Card Visual States

| State | Appearance |
|-------|------------|
| Face-down | Card back sprite |
| Face-up (idle) | Assembled card face (front + rank + suit + optional figure) |
| Dragging | Same as face-up, elevated z-order, slight scale-up or shadow |
| Hint highlighted | Glow/outline effect on card and valid destination pile(s) |
| Invalid drop | Snap-back animation with light shake |

---

## 8. Art Direction

### 8.1 Visual Style

- **Style:** Classic, clean playing card aesthetic
- **Background:** Green felt texture (solid color or subtle texture)
- **Cards:** Traditional playing card design, component-assembled from sprites
- **UI:** Minimal, icon-based HUD buttons

### 8.2 Existing Art Assets

All art is provided in `Assets/Art/Sprites/`:

| Asset | Path | Usage |
|-------|------|-------|
| Card front background | `cards/card_front.png` | Base for all face-up cards |
| Card back | `cards/card_back.png` | Face-down cards |
| Card base/outline | `cards/card_base.png` | Empty pile placeholders |
| Card bottom | `cards/cart_bottom.png` | Bottom of stacked cards (if needed) |
| Rank labels (A–K) | `cards/card numbers/new/` | 13 rank sprites |
| Suit symbols | `cards/semi/new/` | Hearts, diamonds, spades, clubs (+ jokers, unused) |
| Red figures (J, Q, K) | `cards/figures/red/` | jack.png, queen.png, re.png (king) |
| Black figures (J, Q, K) | `cards/figures/black/` | jack.png, queen.png, re.png (king) |
| HUD icons | `options/icons/` | undo.png, hints.png, new-game.png |
| Suit icons (HUD/foundation) | `home/` | hearts.png, diamonds.png, spades.png, flowers.png |

### 8.3 Sprite Atlases

Pre-configured:
- `Assets/Art/SpriteAtlases/Cards.spriteatlas` — All card-related sprites
- `Assets/Art/SpriteAtlases/UI.spriteatlas` — All UI elements

### 8.4 Animation Specifications

| Animation | Description | Estimated Duration |
|-----------|-------------|--------------------|
| Card move | Smooth translation from source to destination | 0.15–0.25s |
| Card flip | 3D Y-axis rotation (or scale X squash-stretch) revealing face/back | 0.2–0.3s |
| Card deal | Sequential card placement during initial deal | 0.05–0.1s per card |
| Invalid drop snap-back | Card returns to origin with light horizontal shake | 0.2–0.3s |
| Stack pickup | Cards fan slightly while dragging | Immediate |
| Auto-complete | Cards fly to foundations one by one | 0.1–0.15s per card |
| Win cascade | Cards launch from foundations, bounce off screen edges | 3–5s total |
| Hint highlight | Pulse/glow effect on card and destinations | Looping while active |
| Score change | Score value animates (count up/down) | 0.2–0.3s |

---

## 9. Audio Design

No audio. No music, no SFX.

---

## 10. Economy & Progression

No economy or progression systems. Each game is standalone. Score exists only within the current game session and is not persisted.

---

## 11. Technical Requirements

- **Engine:** Unity 6
- **Platform:** Desktop (Windows/Mac standalone)
- **Resolution:** 1920x1080 landscape
- **Architecture:** Model-View-System (MVS) with VContainer DI, MessagePipe messaging, UniTask async
- **Animation:** PrimeTween for all card and UI animations
- **Rendering:** 2D sprites with sprite atlases for minimal draw calls
- **Performance targets:**
  - 60 FPS constant
  - Zero GC allocations during gameplay (all pooled/cached)
  - Minimal draw calls via sprite atlasing and material sharing

---

## 12. Content Scope (MVP)

This IS the MVP. No phased rollout needed — the game is fully scoped as described.

### Must-Have (v1.0)
- Complete Klondike Solitaire with all rules
- Drag-and-drop + tap-to-move + double-tap-to-foundation
- Classic point-based scoring (clamped to 0)
- Unlimited undo with score reversal
- Hint cycling system
- Auto-complete detection and execution
- No-moves detection with prompt
- All animations (move, flip, deal, snap-back, win cascade)
- HUD with score, undo, hint, new game, auto-complete buttons

### Not In Scope
- Main menu / settings screen
- Sound / music
- Save / resume
- Statistics / game history
- Multiple draw modes (Draw 3)
- Card back selection
- Timed mode
- Monetization
- Localization
- Accessibility features beyond default

---

## 13. Monetization

None. This is a non-commercial project.

---

## 14. Accessibility

No specific accessibility features planned for v1.0. Default Unity accessibility applies.

---

## 15. Analytics & KPIs

None. No analytics tracking.

---

## 16. Glossary

| Term | Definition |
|------|------------|
| **Stock** | The face-down draw pile. Cards are drawn one at a time to the waste. |
| **Waste** | The face-up pile where drawn stock cards land. Only the top card is playable. |
| **Foundation** | One of 4 piles where cards are built up by suit from Ace to King. Completing all 4 wins the game. |
| **Tableau** | One of 7 columns where cards are arranged. Cards build downward in alternating colors. |
| **Recycle** | Moving all waste cards back to the stock pile when the stock is empty. |
| **Auto-complete** | Automated process of moving all remaining cards to foundations when the game is trivially winnable. |
| **Hint cycling** | Each press of the hint button highlights the next available valid move. |
| **Cascading animation** | The classic win celebration where cards bounce off the edges of the screen. |
| **Face card** | Jack, Queen, or King — cards with figure artwork. |
| **Rank** | The value of a card: A, 2, 3, 4, 5, 6, 7, 8, 9, 10, J, Q, K. |
| **Suit** | One of four card categories: Hearts (red), Diamonds (red), Clubs (black), Spades (black). |
| **Alternating colors** | A tableau building rule requiring red cards on black and black cards on red. |
