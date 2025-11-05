namespace ArtistTool.Domain
{
    public class Category
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Photographs { get; set; } = [];
    }
}
