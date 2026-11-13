namespace ECommerce.Application.Knowledge;

// Splits a question into the words worth matching. Shared by the knowledge search and the
// product matcher so both ignore the same filler words.
internal static class TextTokens
{
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "a", "an", "is", "are", "am", "was", "were", "do", "does", "did", "can", "could",
        "would", "should", "i", "you", "we", "my", "me", "your", "our", "to", "of", "and", "or",
        "for", "in", "on", "at", "it", "this", "that", "with", "have", "has", "had", "be", "been",
        "will", "if", "what", "how", "when", "where", "which", "who", "there", "any", "about",
        "please", "tell", "know", "want", "need", "get", "give"
    };

    public static IReadOnlyList<string> From(string text) =>
        text.ToLowerInvariant()
            .Split(new[] { ' ', ',', '.', '!', '?', ';', ':', '-', '/', '\'', '"' },
                StringSplitOptions.RemoveEmptyEntries)
            .Where(word => word.Length >= 3 && !StopWords.Contains(word))
            .Distinct()
            .ToList();
}
