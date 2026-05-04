using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class CardFront : MonoBehaviour
{
    [Header("Layers")]
    [SerializeField] private Image cardBase;
    [SerializeField] private Image cardSymbol;

    [Header("Base sprites — assign in Inspector")]
    [SerializeField] private Sprite baseRed;
    [SerializeField] private Sprite baseGreen;
    [SerializeField] private Sprite baseBlue;
    [SerializeField] private Sprite baseYellow;
    [SerializeField] private Sprite baseWild;

    // Symbol sprites loaded from Resources at runtime
    private CardInstance _data;

    public void Setup(CardInstance card)
    {
        _data = card;
        // If the symbol asset already contains the full wild artwork (background + symbol),
        // hide the base layer and use the symbol image directly for wilds
        bool isWildType = card.type == CardType.Wild || card.type == CardType.WildDrawFour;
        if (isWildType)
        {
            cardBase.enabled = false;
            cardBase.sprite = null;
        }
        else
        {
            // Get the color base sprite; if not assigned, disable the base layer so
            // the card renders using only the symbol sprite.
            var baseSprite = GetBaseSprite(card.color);
            if (baseSprite == null)
            {
                cardBase.enabled = false;
                cardBase.sprite = null;
            }
            else
            {
                cardBase.enabled = true;
                cardBase.sprite = baseSprite;
            }
        }

        cardSymbol.sprite = GetSymbolSprite(card);
    }

    private Sprite GetBaseSprite(CardColor color) => color switch
    {
        CardColor.Red    => baseRed,
        CardColor.Green  => baseGreen,
        CardColor.Blue   => baseBlue,
        CardColor.Yellow => baseYellow,
        _                => baseWild   // Wild
    };

    private Sprite GetSymbolSprite(CardInstance card)
    {
        string name = card.type switch
        {
            CardType.Number       => $"_{card.number}",
            CardType.Skip         => "_skip",
            CardType.Reverse      => "_reverse",
            CardType.DrawTwo      => "_drawtwo",
            CardType.Wild         => "_wild",
            CardType.WildDrawFour => "_wild_draw",
            _                     => "_0"
        };
        // Load from Resources/CardSymbols/
        return Resources.Load<Sprite>($"CardSymbols/{name}");
    }

    // Called by CardHandUI to grey out unplayable cards
    public void SetPlayable(bool playable)
    {
        var col = playable ? Color.white : new Color(0.45f, 0.45f, 0.45f, 1f);
        cardBase.color   = col;
        cardSymbol.color = col;
        GetComponent<Button>().interactable = playable;
    }
}