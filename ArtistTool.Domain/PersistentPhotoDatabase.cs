using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ArtistTool.Domain
{
    public class PersistentPhotoDatabase : IPhotoDatabase, IDisposable
    {
        private readonly string rootFolder;
        private readonly string photosPath;
        private readonly string categoriesPath;
        private readonly string tagsPath;
        private readonly SemaphoreSlim mutex = new(1, 1);
        private readonly ILogger<PersistentPhotoDatabase> _logger;
        private bool disposed;
        private bool initialized;

        private readonly PhotoDatabase _photoDatabase = new();
        private readonly JsonSerializerOptions jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public PersistentPhotoDatabase(ILogger<PersistentPhotoDatabase> logger)
        {
            _logger = logger;
            rootFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), nameof(ArtistTool));
            photosPath = Path.Combine(rootFolder, "photos-index.json");
            categoriesPath = Path.Combine(rootFolder, "categories.json");
            tagsPath = Path.Combine(rootFolder, "tags.json");
            if (!Directory.Exists(rootFolder))
            {
                Directory.CreateDirectory(rootFolder);
                _logger.LogInformation("Created data directory at {RootFolder}", rootFolder);
            }
            else
            {
                _logger.LogDebug("Using existing data directory at {RootFolder}", rootFolder);
            }
        }

        public async Task InitAsync(CancellationToken cancellationToken = default)
        {
            if (initialized)
            {
                _logger.LogDebug("Database already initialized, skipping");
                return;
            }

            if (!File.Exists(categoriesPath) && !File.Exists(photosPath) && !File.Exists(tagsPath))
            {
                _logger.LogInformation("Fresh database initialization - no existing data files found");
                initialized = true;
                return;
            }

            _logger.LogInformation("Initializing database from persisted files");
            await mutex.WaitAsync(cancellationToken);
            try
            {
                if (initialized)
                {
                    _logger.LogDebug("Database initialized by another thread, skipping");
                    return;
                }

                if (File.Exists(categoriesPath))
                {
                    _logger.LogDebug("Loading categories from {CategoriesPath}", categoriesPath);
                    var categories = JsonSerializer.Deserialize<Category[]>(await File.ReadAllTextAsync(categoriesPath, cancellationToken), jsonOptions)
                        ?? throw new InvalidOperationException("Categories list is corrupt.");
                    _logger.LogInformation("Loaded {Count} categories", categories.Length);
                    foreach (var category in categories)
                    {
                        await _photoDatabase.AddCategoryAsync(category);
                    }
                }

                if (File.Exists(photosPath))
                {
                    _logger.LogDebug("Loading photo index from {PhotosPath}", photosPath);
                    var photoIds = JsonSerializer.Deserialize<string[]>(await File.ReadAllTextAsync(photosPath, cancellationToken), jsonOptions)
                        ?? throw new InvalidOperationException("Photos list is corrupt.");
                    _logger.LogInformation("Found {Count} photos in index", photoIds.Length);
                    
                    var loadedCount = 0;
                    var skippedCount = 0;
                    foreach (var id in photoIds)
                    {
                        var photoPath = Path.Combine(rootFolder, $"{id}.json");
                        if (!File.Exists(photoPath))
                        {
                            _logger.LogWarning("Photo file not found for id {PhotoId}, skipping", id);
                            skippedCount++;
                            continue;
                        }
                        var photo = JsonSerializer.Deserialize<Photograph>(await File.ReadAllTextAsync(photoPath, cancellationToken), jsonOptions)
                            ?? throw new InvalidOperationException($"Unable to load photo with id: {id}");
                        await _photoDatabase.AddPhotographAsync(photo);
                        loadedCount++;
                    }
                    _logger.LogInformation("Successfully loaded {LoadedCount} photos, skipped {SkippedCount}", loadedCount, skippedCount);
                }

                initialized = true;
                _logger.LogInformation("Database initialization completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize database from persisted files");
                throw;
            }
            finally
            {
                mutex.Release();
            }
        }

        public async Task AddCategoryAsync(Category category)
        {
            ThrowIfDisposed();
            _logger.LogDebug("Adding category {CategoryName}", category.Name);
            await mutex.WaitAsync();
            try
            {
                await _photoDatabase.AddCategoryAsync(category);
                await SerializeCategoriesAsync();
                _logger.LogInformation("Added category {CategoryName} with {PhotoCount} photos", category.Name, category.Photographs.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add category {CategoryName}", category.Name);
                throw;
            }
            finally
            {
                mutex.Release();
            }
        }

        public async Task AddPhotographAsync(Photograph photograph)
        {
            ThrowIfDisposed();
            _logger.LogDebug("Adding photograph {PhotoId} - {Title}", photograph.Id, photograph.Title);
            await mutex.WaitAsync();
            try
            {
                await _photoDatabase.AddPhotographAsync(photograph);
                await SerializePhotoAsync(photograph);
                await SerializeCategoriesAsync();
                await SerializeTagsAsync();
                _logger.LogInformation("Added photograph {PhotoId} - {Title} to {CategoryCount} categories and {TagCount} tags",
                    photograph.Id, photograph.Title, photograph.Categories.Length, photograph.Tags.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add photograph {PhotoId} - {Title}", photograph.Id, photograph.Title);
                throw;
            }
            finally
            {
                mutex.Release();
            }
        }

        public async Task UpdatePhotographAsync(Photograph photograph)
        {
            ThrowIfDisposed();
            _logger.LogDebug("Updating photograph {PhotoId} - {Title}", photograph.Id, photograph.Title);
            await mutex.WaitAsync();
            try
            {
                await _photoDatabase.UpdatePhotographAsync(photograph);
                await SerializePhotoAsync(photograph);
                await SerializeCategoriesAsync();
                await SerializeTagsAsync();
                _logger.LogInformation("Updated photograph {PhotoId} - {Title} with {CategoryCount} categories and {TagCount} tags",
                    photograph.Id, photograph.Title, photograph.Categories.Length, photograph.Tags.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update photograph {PhotoId} - {Title}", photograph.Id, photograph.Title);
                throw;
            }
            finally
            {
                mutex.Release();
            }
        }

        public async Task<Photograph?> GetPhotographWithIdAsync(string id)
        {
            ThrowIfDisposed();
            _logger.LogTrace("Getting photograph {PhotoId}", id);
            await mutex.WaitAsync();
            try
            {
                var result = await _photoDatabase.GetPhotographWithIdAsync(id);
                if (result is null)
                {
                    _logger.LogWarning("Photograph {PhotoId} not found", id);
                }
                return result;
            }
            finally
            {
                mutex.Release();
            }
        }

        public async Task<IEnumerable<Category>> ListCategoriesAsync()
        {
            ThrowIfDisposed();
            _logger.LogTrace("Listing all categories");
            await mutex.WaitAsync();
            try
            {
                var result = await _photoDatabase.ListCategoriesAsync();
                _logger.LogDebug("Retrieved {Count} categories", result.Count());
                return result;
            }
            finally
            {
                mutex.Release();
            }
        }

        public async Task<IEnumerable<string>> ListPhotoIdsAsync()
        {
            ThrowIfDisposed();
            _logger.LogTrace("Listing all photo IDs");
            await mutex.WaitAsync();
            try
            {
                var result = await _photoDatabase.ListPhotoIdsAsync();
                _logger.LogDebug("Retrieved {Count} photo IDs", result.Count());
                return result;
            }
            finally
            {
                mutex.Release();
            }
        }

        public async Task<IEnumerable<Tag>> ListTagsAsync()
        {
            ThrowIfDisposed();
            _logger.LogTrace("Listing all tags");
            await mutex.WaitAsync();
            try
            {
                var result = await _photoDatabase.ListTagsAsync();
                _logger.LogDebug("Retrieved {Count} tags", result.Count());
                return result;
            }
            finally
            {
                mutex.Release();
            }
        }

        private async Task SerializeCategoriesAsync()
        {
            _logger.LogDebug("Serializing categories to {Path}", categoriesPath);
            var categories = (await _photoDatabase.ListCategoriesAsync()).ToArray();
            await AtomicWriteAsync(categoriesPath, JsonSerializer.Serialize(categories, jsonOptions));
            _logger.LogDebug("Serialized {Count} categories", categories.Length);
        }

        private async Task SerializeTagsAsync()
        {
            _logger.LogDebug("Serializing tags to {Path}", tagsPath);
            var tags = (await _photoDatabase.ListTagsAsync()).ToArray();
            await AtomicWriteAsync(tagsPath, JsonSerializer.Serialize(tags, jsonOptions));
            _logger.LogDebug("Serialized {Count} tags", tags.Length);
        }

        private async Task SerializePhotoAsync(Photograph photo)
        {
            _logger.LogDebug("Serializing photograph {PhotoId} to disk", photo.Id);
            var photoJson = JsonSerializer.Serialize(photo, jsonOptions);
            var photoPath = Path.Combine(rootFolder, $"{photo.Id}.json");
            await AtomicWriteAsync(photoPath, photoJson);
            
            var ids = (await _photoDatabase.ListPhotoIdsAsync()).ToArray();
            await AtomicWriteAsync(photosPath, JsonSerializer.Serialize(ids, jsonOptions));
            _logger.LogDebug("Serialized photograph {PhotoId} and updated index with {Count} photos", photo.Id, ids.Length);
        }

        private async Task AtomicWriteAsync(string path, string content)
        {
            var tmp = path + ".tmp";
            try
            {
                await File.WriteAllTextAsync(tmp, content);
                File.Move(tmp, path, true);
                _logger.LogTrace("Atomic write completed for {Path}", path);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Atomic write failed for {Path}, cleaning up temp file", path);
                if (File.Exists(tmp))
                {
                    try { File.Delete(tmp); } catch { /* best effort */ }
                }
                throw;
            }
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                _logger.LogError("Attempted to use disposed PersistentPhotoDatabase");
                throw new ObjectDisposedException(nameof(PersistentPhotoDatabase));
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            _logger.LogInformation("Disposing PersistentPhotoDatabase");
            disposed = true;
            mutex.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
