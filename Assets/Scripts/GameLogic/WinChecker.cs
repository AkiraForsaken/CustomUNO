public static class WinChecker
{
    /// <summary>
    /// Kiểm tra xem người chơi đã đạt điều kiện thắng chưa.
    /// Hàm này nên được gọi sau khi một người chơi vừa đánh bài thành công.
    /// </summary>
    /// <param name="playerHandCount">Số bài còn lại trên tay của người chơi sau khi đánh</param>
    /// <returns>True nếu thắng, False nếu chưa</returns>
    public static bool HasWon(int playerHandCount)
    {
        // Vì CardValidator đã chặn việc đánh lá Action cuối cùng, 
        // nên nếu một người chơi có thể đánh lá bài cuối cùng hợp lệ và số bài về 0, 
        // người đó chắc chắn chiến thắng.
        return playerHandCount == 0;
    }
}