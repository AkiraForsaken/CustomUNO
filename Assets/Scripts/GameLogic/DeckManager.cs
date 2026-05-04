using System.Collections.Generic;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    // Sử dụng CardInstance dựa trên cấu trúc đã có trong dự án
    public List<CardInstance> drawPile = new List<CardInstance>();
    public List<CardInstance> discardPile = new List<CardInstance>();

    /// <summary>
    /// Rút 1 lá bài từ Draw Pile. Tự động xào lại bài nếu cạn.
    /// </summary>
    public CardInstance DrawCard()
    {
        if (drawPile.Count == 0)
        {
            ReshuffleDiscardIntoDraw();
        }

        // Nếu vẫn trống (ví dụ trường hợp cực hiếm: tất cả bài đều đang ở trên tay người chơi)
        if (drawPile.Count == 0) 
        {
            Debug.LogWarning("Không còn bài nào để rút!");
            return default;
        }

        CardInstance drawnCard = drawPile[0];
        drawPile.RemoveAt(0);
        return drawnCard;
    }

    /// <summary>
    /// Thêm lá bài vừa đánh vào Discard Pile.
    /// </summary>
    public void DiscardCard(CardInstance card)
    {
        discardPile.Add(card);
    }

    /// <summary>
    /// Xử lý Edge Case: Lấy Discard Pile xào lại làm Draw Pile, giữ lại lá trên cùng.
    /// </summary>
    private void ReshuffleDiscardIntoDraw()
    {
        // Cần ít nhất 2 lá trong chồng bài vứt để có thể xào lại (1 lá giữ lại, 1 lá xào)
        if (discardPile.Count <= 1) return; 

        // Rút lá trên cùng (lá vừa đánh) ra khỏi danh sách
        CardInstance topCard = discardPile[discardPile.Count - 1];
        discardPile.RemoveAt(discardPile.Count - 1);

        // Đưa toàn bộ bài đã đánh ngược trở lại chồng bài rút
        drawPile.AddRange(discardPile);
        discardPile.Clear();

        // Đặt lá bài trên cùng quay trở lại chồng bài vứt
        discardPile.Add(topCard);

        // Xào lại chồng bài rút
        ShuffleDeck(drawPile);
        Debug.Log("Draw pile đã cạn. Đã xào lại Discard pile thành Draw pile mới.");
    }

    /// <summary>
    /// Thuật toán Fisher-Yates để trộn bài
    /// </summary>
    public void ShuffleDeck(List<CardInstance> deck)
    {
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            CardInstance temp = deck[i];
            deck[i] = deck[randomIndex];
            deck[randomIndex] = temp;
        }
    }

    /// <summary>
    /// Khởi tạo bộ bài UNO chuẩn 108 lá và xào bài
    /// </summary>
    public void BuildStandardDeck()
    {
        drawPile.Clear();
        discardPile.Clear();

        // 4 màu cơ bản
        CardColor[] colors = { CardColor.Red, CardColor.Green, CardColor.Blue, CardColor.Yellow };

        foreach (CardColor c in colors)
        {
            // 1 lá số 0 cho mỗi màu
            drawPile.Add(new CardInstance { type = CardType.Number, color = c, number = 0 });

            // 2 lá số từ 1-9 cho mỗi màu
            for (int i = 1; i <= 9; i++)
            {
                drawPile.Add(new CardInstance { type = CardType.Number, color = c, number = i });
                drawPile.Add(new CardInstance { type = CardType.Number, color = c, number = i });
            }

            // 2 lá Action (Skip, Reverse, Draw Two) cho mỗi màu
            for (int i = 0; i < 2; i++)
            {
                drawPile.Add(new CardInstance { type = CardType.Skip, color = c, number = -1 });
                drawPile.Add(new CardInstance { type = CardType.Reverse, color = c, number = -1 });
                drawPile.Add(new CardInstance { type = CardType.DrawTwo, color = c, number = -1 });
            }
        }

        // 4 lá Wild và 4 lá Wild Draw Four (Màu sẽ được đổi khi người chơi chọn)
        for (int i = 0; i < 4; i++)
        {
            // Dùng tạm màu Red làm mặc định, không ảnh hưởng luật chơi vì loại thẻ là Wild
            drawPile.Add(new CardInstance { type = CardType.Wild, color = CardColor.Red, number = -1 });
            drawPile.Add(new CardInstance { type = CardType.WildDrawFour, color = CardColor.Red, number = -1 });
        }

        // Khởi tạo xong 108 lá thì tiến hành xào bài luôn
        ShuffleDeck(drawPile);
        Debug.Log($"[DeckManager] Đã khởi tạo và xào bộ bài chuẩn với {drawPile.Count} lá!");
    }

    // Ghi chú: Hàm BuildStandardDeck() khởi tạo 108 lá bài từ CardData sẽ được gọi bởi GameManager 
    // khi GamePhase.WaitingForPlayers chuyển sang GamePhase.Playing.
}