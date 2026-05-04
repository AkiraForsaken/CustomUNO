// CardValidator.cs
public static class CardValidator
{
    // Returns true if playing 'card' is a legal move given the current state.
    public static bool IsLegal(CardInstance card, GameState state, int playerHandCount)
    {
        if (state.playerCount == 0) return false;

        // Định nghĩa các thẻ chức năng (Action Cards)
        bool isActionCard = card.type == CardType.Skip ||
                            card.type == CardType.Reverse ||
                            card.type == CardType.DrawTwo ||
                            card.type == CardType.Wild ||
                            card.type == CardType.WildDrawFour;

        // 1. Kiểm tra luật "No Action Card Win"
        // Nếu người chơi chỉ còn 1 lá và định đánh lá chức năng -> Bất hợp pháp
        if (playerHandCount == 1 && isActionCard)
        {
            return false; 
        }

        // 2. Kiểm tra luật Stacking (Cộng dồn +2, +4)
        // Yêu cầu: Cần đảm bảo state có thuộc tính pendingPenalty > 0 khi có người bị phạt
        if (state.pendingPenalty > 0)
        {
            // Nếu đang bị phạt, chỉ được đánh đè thẻ phạt đúng luật
            if (card.type == CardType.WildDrawFour) 
            {
                return true; // +4 luôn đè được mọi penalty
            }
            
            if (card.type == CardType.DrawTwo && state.topCard.type != CardType.WildDrawFour)
            {
                return true; // +2 đè được +2, nhưng KHÔNG đè được +4
            }
            
            return false; // Nếu không có thẻ phạt hợp lệ để đỡ, không được đánh lá nào khác
        }

        // 3. Luật Uno cơ bản (khi không bị phạt rút bài)
        if (card.type == CardType.Wild || card.type == CardType.WildDrawFour)
            return true;

        bool matchesColor  = card.color == state.activeColor;
        bool matchesNumber = card.type == CardType.Number
                             && state.topCard.type == CardType.Number
                             && card.number == state.topCard.number;
        bool matchesType   = card.type != CardType.Number
                             && card.type == state.topCard.type;

        return matchesColor || matchesNumber || matchesType;
    }
}