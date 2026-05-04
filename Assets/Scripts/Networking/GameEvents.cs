using System;
using System.Collections.Generic;

// Central event bus. UI subscribes here. NetworkGameManager fires here.
// Neither side needs a direct reference to the other.
public static class GameEvents
{
    // ── Fired by NetworkGameManager → GameUI ─────────

    // Shared public state: top card, turn order, direction, phase
    public static event Action<GameState> OnGameStateUpdated;

    // Private hand: only the local client receives this
    public static event Action<List<CardInstance>> OnLocalHandUpdated;

    public static event Action<HouseRulesConfig>    OnHouseRulesReceived;

    // Game over
    public static event Action<string> OnGameOver; // winnerId

    // ── Fired by UI → NetworkGameManager ─────────────

    // Local player wants to play a card
    public static event Action<CardInstance> OnPlayCardRequested;

    // Local player wants to draw a card
    public static event Action OnDrawCardRequested;

    // Local player chose a color after playing Wild
    public static event Action<CardColor> OnColorChosen;
    public static event Action OnUnoCalled;
    public static event Action OnUnoMissed;

    // ── Invokers ──────────────

    public static void RaiseGameStateUpdated(GameState state) => OnGameStateUpdated?.Invoke(state);
    public static void RaiseLocalHandUpdated(List<CardInstance> hand) => OnLocalHandUpdated?.Invoke(hand);
    public static void RaiseGameOver(string winnerId) => OnGameOver?.Invoke(winnerId);
    public static void RaiseHouseRulesReceived(HouseRulesConfig cfg)  => OnHouseRulesReceived?.Invoke(cfg);
    public static void RaisePlayCardRequested(CardInstance card) => OnPlayCardRequested?.Invoke(card);
    public static void RaiseDrawCardRequested() => OnDrawCardRequested?.Invoke();
    public static void RaiseColorChosen(CardColor color) => OnColorChosen?.Invoke(color);
    public static void RaiseUnoCalled() => OnUnoCalled?.Invoke();
    public static void RaiseUnoMissed() => OnUnoMissed?.Invoke();
}