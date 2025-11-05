namespace ArtistTool.Domain
{
    public class Photograph
    {
        private string _thumnailPath = string.Empty;

        public string ThumbnailPath
        {
            get => string.IsNullOrWhiteSpace(_thumnailPath) ? Path : _thumnailPath;
            set => _thumnailPath = value ?? string.Empty;
        }
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string FileName { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;   
        public string ContentType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string[] Tags { get; set; } = [];
        public string[] Categories { get; set; } = [];

        public Photograph Clone() =>
            new()
            {
                ThumbnailPath = ThumbnailPath == Path ? string.Empty : ThumbnailPath,
                Categories = [.. Categories],
                Tags = [.. Tags],
                Title = Title,
                Id = Id,
                Description = Description,
                FileName = FileName,
                ContentType = ContentType,
                Path = Path
            };
    }
}
