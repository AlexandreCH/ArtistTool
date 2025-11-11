namespace ArtistTool.Domain.Agents
{
    public class Critique
    {
        public string Area { get; set; } = string.Empty; // Composition, Technique, etc.
        public short Rating { get; set; } // 1 to 10 scale, 10 being the best
        public string Praise { get; set; } = string.Empty; // Positive feedback
        public string ImprovementSuggestion { get; set; } = string.Empty; // Constructive criticism
    }
}
