using UnityEngine;

[CreateAssetMenu(fileName = "Card", menuName = "UNO/Card")]
public class CardData : ScriptableObject
{
  public CardColor color;
  public CardType type;
  public int number; // -1 if not a number card
}

public enum CardColor { Red, Green, Blue, Yellow, Wild }
public enum CardType  { Number, Skip, Reverse, DrawTwo, Wild, WildDrawFour }
