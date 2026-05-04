public static class TurnManager
{
    // Bắt buộc dùng 'ref' vì GameState là struct (tham trị), nếu không dùng 'ref', 
    // hàm sẽ chỉ thay đổi bản sao của GameState chứ không tác động đến bản gốc trên Server.

    /// <summary>
    /// Chuyển lượt sang người chơi tiếp theo dựa trên chiều đánh hiện tại.
    /// </summary>
    public static void NextPlayer(ref GameState state)
    {
        if (state.playerCount == 0) return;

        int step = state.isClockwise ? 1 : -1;
        
        // Dùng playerCount (số lượng thực tế) thay cho MAX_PLAYERS
        state.currentPlayerIndex = (state.currentPlayerIndex + step + state.playerCount) % state.playerCount;
    }

    /// <summary>
    /// Bỏ qua người chơi tiếp theo (dùng cho thẻ Skip).
    /// </summary>
    public static void SkipNextPlayer(ref GameState state)
    {
        if (state.playerCount == 0) return;

        int step = state.isClockwise ? 2 : -2;
        state.currentPlayerIndex = (state.currentPlayerIndex + step + state.playerCount) % state.playerCount;
    }

    /// <summary>
    /// Đảo chiều vòng xoay (dùng cho thẻ Reverse).
    /// </summary>
    public static void ReverseDirection(ref GameState state)
    {
        state.isClockwise = !state.isClockwise;
    }

    /// <summary>
    /// Lấy ID của người chơi đang giữ lượt hiện tại.
    /// </summary>
    public static ulong GetCurrentPlayerId(ref GameState state)
    {
        if (state.playerCount == 0) return 0; // Trả về 0 (Host ID mặc định) hoặc throw Exception tùy thiết kế
        return state.playerOrder[state.currentPlayerIndex];
    }
}