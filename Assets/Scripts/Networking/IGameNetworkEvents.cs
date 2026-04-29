using System.Collections.Generic;

// Networking implements it. GameLogic calls it.
public interface IGameNetworkEvents
{
    void OnCardPlayed(string playerId, CardInstance card);
    void OnCardDrawn(string playerId);
    void OnColorChosen(string playerId, CardColor color);
    void OnHandSwap(string playerA, string playerB); // Rule 7
    void OnHandPassDirection(bool clockwise); // Rule 0
    void OnReactionEventStart(); // Rule 8
    void OnReactionReceived(string playerId); // Rule 8
    void OnGameStateUpdated(GameState newState);
    void OnGameOver(string winnerId);
}