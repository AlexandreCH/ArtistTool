using System.Collections.Concurrent;

namespace ArtistTool.Services
{
    public class ImageRegistry : IImageRegistry
    {
        private readonly ConcurrentBag<string> titles = [];
        private readonly ConcurrentDictionary<string, string> fullInfoByTitle = [];
        private readonly ConcurrentDictionary<string, string> fullInfoByFilename = [];

        public string GetImageInformationByTitle(string title) => fullInfoByTitle.TryGetValue(title, out string? details) ? details : title;

        public string GetImageInformationByFilename(string filename) => fullInfoByFilename.TryGetValue(filename, out string? details) ? details : filename;

        public string[] GetTitles() => [.. titles];
       
        public void RegisterImage(string filename, string title, string description)
        {
            titles.Add(title);
            fullInfoByTitle.TryAdd(title, $"Filename: {filename}\nTitle: {title}\nDescription: {description}");
            fullInfoByFilename.TryAdd(filename, $"Filename: {filename}\nTitle: {title}\nDescription: {description}");
        }
    }
}
