using ArtistTool.Domain;

namespace ArtistTool.Services
{
    public interface IImageManager
    {
        string[] ValidExtensions { get; }

        Task<Photograph> SaveImageAsync(string filename, string contentType, Func<FileStream, Task> writeBytesAsync);
        Task<Stream> GetStreamForPhotoIdAsync(string id);
    }
}