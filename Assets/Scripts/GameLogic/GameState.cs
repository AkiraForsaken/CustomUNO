using System;
using System.Collections.Generic;

[Serializable]
public class GameState
{
  public List<string> playerOrder = new(); // player IDs in turn order
  public int currentPlayerIndex;
  public bool isClockwise = true;
  public CardInstance topCard;
  public CardColor activeColor; // for Wild overrides
  public int pendingDrawPenalty; // for stacking rule
  public GamePhase phase;
  public Dictionary<string, int> handCounts = new(); // playerId → card count
}

public enum GamePhase
{
  WaitingForPlayers,
  Playing,
  ColorSelection,
  TargetSelection, // Rule of 7
  ReactionEvent, // Rule of 8
  GameOver
}