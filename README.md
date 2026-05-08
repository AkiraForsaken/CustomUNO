# 🃏 Custom UNO Online

A multiplayer turn-based card game built in Unity for the **Game Programming — SEM252** course project. Players can create or join a room using a room code and play a full match of Custom UNO under a set of custom house rules, with all game state managed by a host-authoritative server using **Unity Netcode for GameObjects**.

---

## Table of Contents

1. [Project Overview](#project-overview)
2. [How to Play](#how-to-play)
3. [Networking Architecture](#networking-architecture)
4. [Game Rules](#game-rules)
   - [Standard Rules](#standard-rules)
   - [Rule of 0](#rule-of-0)
   - [Rule of 7](#rule-of-7)
   - [Rule of 8 — Reaction Event](#rule-of-8--reaction-event)
   - [No Win with an Action Card](#no-win-with-an-action-card)
   - [Stacking Rule for +2 and +4](#stacking-rule-for-2-and-4)
5. [Custom Rule Design Decisions](#custom-rule-design-decisions)
6. [User Interface Guide](#user-interface-guide)
7. [Bot / AI Opponent](#bot--ai-opponent)
8. [Project Structure](#project-structure)
9. [Known Limitations](#known-limitations)

---

## Project Overview

**Custom UNO Online** implements the standard UNO rule set extended with five required house rules from the SEM252 project specification. The game supports 2–4 human players with an optional bot to fill empty seats, communicating over a host-authoritative network built on Unity's **Netcode for GameObjects** library.

Key technical highlights:

- Host-authoritative design: all game logic (shuffle, deal, legality checking, effect resolution, win detection) executes exclusively on the host and is broadcast to clients.
- Room-code-based matchmaking so players on the same network can join without manually entering an IP address.
- Reaction event system for the Rule of 8 with a server-side countdown timer and late-response penalties.
- Stacking penalty chain supporting mixed +2/+4 sequences with accumulation and forced draw resolution.
- Optional bot player that fills any unoccupied seat with simple rule-following AI.

---

## How to Play

### Creating a Room (Host)

1. Launch the game and click **Create Room** on the main menu.
2. A **6-character room code** is displayed on screen (e.g., `G6HJPF`).
3. Share this code with other players.
4. Wait for 1–3 additional players (or add a bot to fill seats).
5. Click **Start Game** once the lobby has 2 or more players.

### Joining a Room (Client)

1. Launch the game and click **Join Room**.
2. Enter the room code shared by the host and click **Connect**.
3. Wait in the lobby until the host starts the game.

### During a Match

- **Your hand** is displayed at the bottom of the screen.
- Cards that are **legal to play** are highlighted; illegal cards are dimmed.
- Click a card to play it. If it is a Wild or Wild Draw Four, a **color picker** will appear.
- If you have no legal card, click **Draw** to draw one card. If the drawn card is playable, you may play it immediately.
- Special prompts appear automatically when rules require interaction (color selection, target selection for card 7, reaction button for card 8).

---

## Networking Architecture

### Technology

The game uses **Unity Netcode for GameObjects (NGO)**, Unity's first-party networking library. The Unity Relay service is **not** used; players connect directly over LAN using the `UnityTransport` component configured for direct IP.

### Room Code Resolution

When the host creates a room, the game generates a room code and registers it alongside the host's **local IP address** in a lightweight in-memory directory that runs on the host itself. When a client enters the code and clicks Connect, the client sends a UDP broadcast on the local subnet to locate the host machine that owns that code. Once the IP is resolved, a standard NGO `NetworkManager.StartClient()` connection is established.

### Host Authority

The host is the sole authority for all gameplay decisions. Clients send **requests** (RPCs) to the host; the host validates each request, updates the `NetworkVariable`-backed game state, and broadcasts the result to all clients. Clients never modify game state directly.

The following actions are validated server-side before any state change occurs:

| Action | Validation |
|--------|------------|
| Play a card | Correct player's turn; card matches top of discard pile by color, number, or type; not an illegal final-card win attempt |
| Draw a card | Correct player's turn; no legal card was available (or player chose to draw) |
| Stack a penalty card | New penalty value ≥ current accumulated penalty value |
| Card 7 hand swap | Target player is valid and currently in the match |
| Card 8 reaction | Event is currently active; player has not already responded |
| Wild color selection | A Wild or Wild Draw Four was just played by this player |

### State Synchronization

All shared game state is stored in `NetworkVariable<T>` fields on the `GameManager` NetworkObject:

- `currentPlayerIndex` — whose turn it is
- `playDirection` — clockwise (`1`) or counter-clockwise (`-1`)
- `topCardColor` / `topCardValue` — current top of discard pile
- `pendingDrawPenalty` — accumulated draw count from stacked +2/+4
- `reactionEventActive` — whether the card-8 reaction window is open

Player hand sizes are synchronized as a `NetworkList<int>` (one entry per player). Actual card data in each player's hand is sent only to that player via targeted ClientRpc calls, never broadcast to all clients, to prevent cheating.

---

## Game Rules

### Standard Rules

- Each player starts with **7 cards**.
- The deck contains number cards (0–9) in four colors (Red, Green, Blue, Yellow), plus Skip, Reverse, Draw Two, Wild, and Wild Draw Four.
- On your turn, you must play one legal card: it must match the top discard card by **color**, **number**, or **card type**, or be a Wild / Wild Draw Four.
- If you cannot play, draw one card. If the drawn card is playable, you may play it immediately; otherwise your turn ends.
- **Reverse in a 2-player match** behaves like a **Skip** (see [Custom Rule Design Decisions](#custom-rule-design-decisions)).
- **Win condition:** a player wins when they play their last card, subject to the [No Win with an Action Card](#no-win-with-an-action-card) rule.

---

### Rule of 0

When a player plays a **0 card**:

1. The player is prompted to choose a direction: **clockwise** or **counter-clockwise**.
2. All players simultaneously pass their **entire hand** in the chosen direction.
   - In a clockwise pass: each player gives their hand to the next player in clockwise order.
   - In a counter-clockwise pass: each player gives their hand to the next player in counter-clockwise order.
3. The hand transfer is resolved before the next turn begins.
4. **The turn order does not change** — only the hands move. The next player in the existing turn order takes their turn with their newly received hand.

The current play direction arrow in the UI updates to indicate which direction the hand pass traveled.

---

### Rule of 7

When a player plays a **7 card**:

1. The player is prompted to select **one target** from the other players currently in the match.
2. The two players swap their **entire hands**.
3. The swap is applied server-side and the updated hands are sent to each affected player. All other players see the updated card counts immediately.

The target selection UI displays all valid opponents with their current card count to help the active player make a decision.

---

### Rule of 8 — Reaction Event

When a player plays an **8 card**, a timed reaction event is triggered:

1. The host broadcasts the start of the event to all clients.
2. A **reaction button** appears on every player's screen simultaneously.
3. Players have a fixed window of **3 seconds** to click the button.
4. The **last player** to respond within the window draws **2 cards** as a penalty.
5. Any player who does **not** respond before the 3-second deadline is treated as the latest responder and also draws 2 cards.
6. If multiple players fail to respond in time, **all of them** draw 2 cards.
7. Each player may submit only one valid response. Duplicate clicks are ignored.

The host is the final authority on response order. A server-side timestamp is recorded for each response; the host determines the last responder after the window closes and then distributes penalties before play continues.

---

### No Win with an Action Card

A player **cannot win** by playing the following cards as their final card:

- Skip
- Reverse
- Draw Two
- Wild
- Wild Draw Four

If a player has exactly one card remaining and that card is one of the above, attempting to play it is treated as an **illegal move** and is blocked by the host. The player must either play a different legal card or draw if no other legal option exists.

> Example: A player holds only a Wild card. They cannot end the game by playing it. They must draw until they can play a non-action card, or until they acquire a second card that allows a different winning sequence.

---

### Stacking Rule for +2 and +4

Stacking is permitted under a strict escalation rule:

- After a **+2** is played, the next player may respond with a **+2** or a **+4**.
- After a **+4** is played, the next player may respond only with a **+4**.
- Each stacked card adds its value to the running penalty total.
- The chain continues until a player **cannot** or **chooses not** to stack.
- That player draws the **full accumulated penalty** and **loses their turn**.

**Example:**

| Player | Action | Accumulated Penalty |
|--------|--------|---------------------|
| A | Plays +2 | 2 |
| B | Stacks +2 | 4 |
| C | Stacks +4 | 8 |
| D | Cannot stack | Draws 8 cards, loses turn |

Once the penalty is resolved, the accumulated value resets to zero and normal play resumes.

---

## Custom Rule Design Decisions

### Reverse in a 2-Player Match

In a standard 2-player game, a Reverse card could simply flip the direction, which would have no real effect since there are only two players. We chose to treat **Reverse as a Skip** in 2-player matches — the current player plays the Reverse, and the opponent loses their next turn (effectively giving the current player two consecutive turns). This decision is consistent with the most widely adopted house-rule interpretation and creates meaningful strategic value for the card.

### Rule of 0 — Turn Order Unchanged

The specification leaves it optional whether the hand-pass direction also changes the turn order. We chose **not** to change the turn order, so the Rule of 0 purely affects cards in players' hands and not the sequence of play. This keeps the rule's effect focused and prevents it from compounding unexpectedly with Reverse or Skip effects active at the same time.

### Card 8 Reaction Window Duration

The specification leaves the response window duration as an example ("for example, 3 seconds"). We fixed the window at **3 seconds** because it provides enough time for a human player to react across a LAN connection while still creating the time-pressure tension the rule is designed to deliver.

---

## User Interface Guide

The in-game HUD displays all required information at all times:

| UI Element | Location | Description |
|------------|----------|-------------|
| Your hand | Bottom center | Your cards; legal plays are highlighted in white, illegal ones are dimmed |
| Opponent card counts | Top of screen (one panel per opponent) | Shows each opponent's name and number of remaining cards |
| Discard pile | Center | The top card of the discard pile, showing its color and value |
| Draw pile | Center-left | Clickable pile; click to draw when it is your turn |
| Play direction arrow | Center | Rotates to show clockwise or counter-clockwise turn order |
| Active player indicator | Above each player panel | A glowing border highlights whose turn it is |
| Pending penalty counter | Center-top | Shown in red when a draw penalty chain is active; displays total cards to be drawn |
| Color picker | Center overlay | Appears after playing a Wild or Wild Draw Four |
| Target picker | Center overlay | Appears after playing a 7; lists all valid opponents |
| Reaction button | Center overlay | Appears during a card-8 event; includes a countdown bar |
| End-of-game screen | Full screen overlay | Shows winner, final scores, and a Return to Lobby button |

---

## Bot / AI Opponent

A simple bot can be added by the host before starting the match. The bot occupies a player seat and takes actions automatically on its turn using the following strategy:

- **Card selection:** Plays a random legal card from its hand.
- **Wild color selection:** Chooses the color that appears most often in its current hand. If the hand is empty or all colors are tied, a random color is chosen.
- **Rule of 7 target:** Selects a random valid opponent.
- **Rule of 8 reaction:** Responds immediately with no delay (simulates instant click).
- **Stacking:** Always stacks a valid penalty card if one is available; never passes.

The bot is implemented as a server-side coroutine running on the host. It has a small artificial delay (0.5–1.0 seconds) before each action to make gameplay feel natural. Bot turns are validated by the same host-side logic as human turns.

---

## Project Structure

```
Assets/
├── Scenes/
│   ├── MainMenu.unity          # Room creation and join screen
│   ├── Lobby.unity             # Pre-game waiting room
│   └── Game.unity              # Main gameplay scene
├── Scripts/
│   ├── Networking/
│   │   ├── RoomManager.cs      # Room code generation and resolution
│   │   └── ConnectionHandler.cs
│   ├── Game/
│   │   ├── GameManager.cs      # Host-authoritative game state and rule logic
│   │   ├── DeckManager.cs      # Deck, draw pile, discard pile management
│   │   ├── CardRules.cs        # Legality checking and effect resolution
│   │   ├── ReactionEvent.cs    # Card-8 timed reaction system
│   │   └── BotPlayer.cs        # AI bot coroutine
│   ├── UI/
│   │   ├── HandDisplay.cs      # Local player hand rendering
│   │   ├── OpponentPanel.cs    # Opponent card count display
│   │   ├── ColorPicker.cs      # Wild color selection overlay
│   │   ├── TargetPicker.cs     # Card-7 target selection overlay
│   │   └── HUDManager.cs       # Top-level HUD orchestration
│   └── Data/
│       ├── CardData.cs         # Card type definitions and enums
│       └── PlayerState.cs      # Per-player networked state
├── Prefabs/
│   ├── Card.prefab
│   ├── PlayerPanel.prefab
│   └── ReactionButton.prefab
└── Resources/
    └── CardSprites/            # Card face and back sprites
```

---

## Known Limitations

- **LAN only:** The game does not use Unity Relay or any external matchmaking service, so all players must be on the same local network. Remote play over the internet is not supported.
- **No reconnect:** If a client disconnects mid-game, the match cannot be resumed. The remaining players are returned to the lobby.
- **Player leave before game starts:** If a client leaves the lobby before the host starts the game, their slot is freed and the host may start with the remaining players or add a bot. Player departure during an active match is not gracefully handled.
- **No persistent statistics:** Win/loss records are not saved between sessions.
