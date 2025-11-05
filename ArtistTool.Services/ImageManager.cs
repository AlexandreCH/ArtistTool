using ArtistTool.Domain;
using Microsoft.Extensions.Logging;

namespace ArtistTool.Services
{
    public class ImageManager : IImageManager
    {
        private readonly IPhotoDatabase photoDatabase;
        private readonly ILogger<ImageManager> _logger;
        private readonly string rootFolder;
        private readonly string imagesFolder;

        public ImageManager(IPhotoDatabase db, ILogger<ImageManager> logger)
        {
            photoDatabase = db;
            _logger = logger;
            rootFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), nameof(ArtistTool));
            if (!Directory.Exists(rootFolder))
            {
                Directory.CreateDirectory(rootFolder);
                _logger.LogInformation("Created root folder at {RootFolder}", rootFolder);
            }
            imagesFolder = Path.Combine(rootFolder, "images");
            if (!Directory.Exists(imagesFolder))
            {
                Directory.CreateDirectory(imagesFolder);
                _logger.LogInformation("Created images folder at {ImagesFolder}", imagesFolder);
            }
            else
            {
                _logger.LogDebug("Using existing images folder at {ImagesFolder}", imagesFolder);
            }
        }

        public string[] ValidExtensions { get; private set; } = [
            ".jpg",
            ".jpeg",
            ".gif",
            ".png",
            ".tif",
            ".tiff"
            ];

        public async Task<Photograph> SaveImageAsync(string filename, string contentType, Func<FileStream, Task> writeBytesAsync)
        {
            _logger.LogInformation("Attempting to save image {FileName} with content type {ContentType}", filename, contentType);
            
            if (!contentType.Contains("image"))
            {
                _logger.LogWarning("Invalid content type {ContentType} for file {FileName}", contentType, filename);
                throw new InvalidOperationException($"Content type {contentType} not supported for file '{filename}'");
            }
            
            var ext = Path.GetExtension(filename).ToLower();
            if (ValidExtensions.Contains(ext))
            {
                var id = Guid.NewGuid().ToString();
                _logger.LogDebug("Generated ID {ImageId} for {FileName}", id, filename);
                
                var photo = new Photograph
                {
                    Id = id,
                    FileName = filename,
                    Title = Path.GetFileNameWithoutExtension(filename),
                    ContentType = contentType,
                    Path = $"{Path.Combine(imagesFolder, id)}{ext}"
                };
                
                try
                {
                    _logger.LogDebug("Writing image bytes to {Path}", photo.Path);
                    await using FileStream fs = new(photo.Path, FileMode.Create);
                    await writeBytesAsync(fs);
                    _logger.LogDebug("Image bytes written successfully for {ImageId}", id);
                    
                    await photoDatabase.AddPhotographAsync(photo);
                    _logger.LogInformation("Successfully saved image {ImageId} - {Title} at {Path}", id, photo.Title, photo.Path);
                    return photo;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save image {FileName} with ID {ImageId}", filename, id);
                    
                    // Cleanup file if database add failed
                    if (File.Exists(photo.Path))
                    {
                        try
                        {
                            File.Delete(photo.Path);
                            _logger.LogDebug("Cleaned up orphaned image file {Path}", photo.Path);
                        }
                        catch (Exception cleanupEx)
                        {
                            _logger.LogWarning(cleanupEx, "Failed to cleanup orphaned image file {Path}", photo.Path);
                        }
                    }
                    throw;
                }
            }
            
            _logger.LogWarning("Unsupported extension {Extension} for file {FileName}", ext, filename);
            throw new InvalidOperationException($"Extension '{ext}' not supported for file '{filename}'");
        }

        public async Task<Stream> GetStreamForPhotoIdAsync(string id)
        {
            _logger.LogDebug("Getting stream for photo ID {PhotoId}", id);
            
            var photo = await photoDatabase.GetPhotographWithIdAsync(id);
            if (photo is not null)
            {
                if (!File.Exists(photo.Path))
                {
                    _logger.LogError("Photo file not found at {Path} for ID {PhotoId}", photo.Path, id);
                    throw new FileNotFoundException($"Photo file not found for id '{id}'", photo.Path);
                }
                
                _logger.LogDebug("Returning file stream for {Path}", photo.Path);
                return new FileStream(photo.Path, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            
            _logger.LogWarning("Photo with ID {PhotoId} not found in database", id);
            throw new InvalidOperationException($"File with id: '{id}' not found.");
        }
    }
}
