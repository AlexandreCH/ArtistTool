namespace ArtistTool.Domain
{
    public class Tag
    {
        public string TagName { get; set; } = string.Empty;
        public List<string> Photographs { get; set; } = [];
    }
}
