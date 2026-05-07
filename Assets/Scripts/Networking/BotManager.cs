using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class BotManager : MonoBehaviour
{
    public static BotManager Instance { get; private set; }

    // Reserved fake client IDs that will never collide with real NGO client IDs
    public static readonly ulong[] BOT_IDS = { 9001UL, 9002UL, 9003UL };

    private int botCount = 0;

    public int  BotCount => botCount;
    public bool CanAddBot(int currentPlayerCount) => 
        currentPlayerCount < GameState.MAX_PLAYERS && botCount < BOT_IDS.Length;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool AddBot()
    {
        if (botCount >= BOT_IDS.Length) return false;
        botCount++;
        return true;
    }

    public bool RemoveBot()
    {
        if (botCount <= 0) return false;
        botCount--;
        return true;
    }

    public void SetBotCount(int count)
    {
        botCount = Mathf.Clamp(count, 0, BOT_IDS.Length);
    }

    public bool IsBot(ulong clientId)
    {
        for (int i = 0; i < botCount; i++)
            if (BOT_IDS[i] == clientId) return true;
        return false;
    }

    public string GetBotName(ulong botId)
    {
        for (int i = 0; i < BOT_IDS.Length; i++)
            if (BOT_IDS[i] == botId) return $"Bot {i + 1}";
        return "Bot";
    }

    // Returns only the IDs of currently active bots
    public List<ulong> GetActiveBotIds()
    {
        var list = new List<ulong>();
        for (int i = 0; i < botCount; i++)
            list.Add(BOT_IDS[i]);
        return list;
    }

    public void Reset() => botCount = 0;
}