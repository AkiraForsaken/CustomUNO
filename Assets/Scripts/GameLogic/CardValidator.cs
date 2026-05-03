// CardValidator.cs
// Member 1 owns this file. The stub compiles immediately so the rest
// of the UI can be built and tested before game logic is complete.
public static class CardValidator
{
    // Returns true if playing 'card' is a legal move given the current state.
    // Rules: Wild/WildDrawFour are always legal.
    //        Otherwise card must match activeColor, OR match topCard number (Number cards),
    //        OR match topCard type (action cards).
    //        "No action card win" rule: also checked here when hand will be empty after play.
    public static bool IsLegal(CardInstance card, GameState state)
    {
        // ── STUB: always returns true until Member 1 implements ───
        // Replace the line below with real validation logic.
        return true;

        // ── Implementation guide for Member 1 ────────────────────
        // if (state == null || state.topCard == null) return false;
        //
        // if (card.type == CardType.Wild || card.type == CardType.WildDrawFour)
        //     return true;
        //
        // bool matchesColor  = card.color == state.activeColor;
        // bool matchesNumber = card.type == CardType.Number
        //                      && state.topCard.type == CardType.Number
        //                      && card.number == state.topCard.number;
        // bool matchesType   = card.type != CardType.Number
        //                      && card.type == state.topCard.type;
        //
        // return matchesColor || matchesNumber || matchesType;
    }
}