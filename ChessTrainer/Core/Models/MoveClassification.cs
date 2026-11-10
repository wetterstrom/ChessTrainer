namespace ChessTrainer.Core.Models;

public enum MoveQuality
{
    None,
    Brilliant,   // !! (Turkosa)
    Great,       // !  (Blå)
    Best,        // ★  (Grön)
    Excellent,   // ✓  (Ljusgrön)
    Good,        // ○  (Grågrön)
    Book,        // 📖 (Brun/Guld)
    Inaccuracy,  // ?! (Gul)
    Mistake,     // ?  (Orange)
    Blunder      // ?? (Röd)
}

public static class MoveQualityExtensions
{
    public static (string Badge, string Title, string ColorHex) GetMeta(this MoveQuality quality) => quality switch
    {
        MoveQuality.Brilliant => ("!!", "Briljant drag!", "#1baca6"),
        MoveQuality.Great => ("!", "Stort drag!", "#5c8bb0"),
        MoveQuality.Best => ("★", "Bästa draget", "#96bc4b"),
        MoveQuality.Excellent => ("✓", "Utmärkt drag", "#96bc4b"),
        MoveQuality.Good => ("●", "Bra drag", "#779954"),
        MoveQuality.Book => ("📖", "Bokdrag", "#a88865"),
        MoveQuality.Inaccuracy => ("?!", "Oprecist drag", "#f0c15c"),
        MoveQuality.Mistake => ("?", "Misstag", "#e6912c"),
        MoveQuality.Blunder => ("??", "Blunder", "#ca3431"),
        _ => ("", "", "transparent")
    };
}