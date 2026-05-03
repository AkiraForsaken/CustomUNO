using System;
using System.Collections.Generic;

// Central event bus. UI subscribes here. NetworkGameManager fires here.
// Neither side needs a direct reference to the other.
public static class GameEvents
{
    // ── Fired by NetworkGameManager → consumed by GameUI ─────────

    // Shared public state: top card, turn order, direction, phase
    public static event Action<GameState> OnGameStateUpdated;

    // Private hand: only the local client receives this
    public static event Action<List<CardInstance>> OnLocalHandUpdated;

    // Game over
    public static event Action<string> OnGameOver; // winnerId

    // ── Fired by UI → consumed by NetworkGameManager ─────────────

    // Local player wants to play a card
    public static event Action<CardInstance> OnPlayCardRequested;

    // Local player wants to draw a card
    public static event Action OnDrawCardRequested;

    // Local player chose a color after playing Wild
    public static event Action<CardColor> OnColorChosen;

    // ── Invokers (called by whoever fires the event) ──────────────

    public static void RaiseGameStateUpdated(GameState state)   => OnGameStateUpdated?.Invoke(state);
    public static void RaiseLocalHandUpdated(List<CardInstance> hand) => OnLocalHandUpdated?.Invoke(hand);
    public static void RaiseGameOver(string winnerId)           => OnGameOver?.Invoke(winnerId);

    public static void RaisePlayCardRequested(CardInstance card) => OnPlayCardRequested?.Invoke(card);
    public static void RaiseDrawCardRequested()                  => OnDrawCardRequested?.Invoke();
    public static void RaiseColorChosen(CardColor color)         => OnColorChosen?.Invoke(color);
}