using System;

[Serializable]
public class CardInstance
{
  public string cardId; // ex: "Red_7", "Wild_DrawFour"
  public CardColor color;
  public CardType type;
  public int number;

  public bool IsActionCard() =>
    type == CardType.Skip || type == CardType.Reverse ||
    type == CardType.DrawTwo || type == CardType.Wild ||
    type == CardType.WildDrawFour;
}