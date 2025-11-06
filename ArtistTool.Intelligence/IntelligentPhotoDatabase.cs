using ArtistTool.Domain;
using Microsoft.Extensions.Logging;

namespace ArtistTool.Intelligence
{
    public class IntelligentPhotoDatabase(
        ILogger<PersistentPhotoDatabase> persistentLogger,
        ILogger<IntelligentPhotoDatabase> logger,
        PhotoIntelligenceService? intelligenceService = null) : IPhotoDatabase
    {
        private readonly PersistentPhotoDatabase _persistentDatabase = new(persistentLogger);

        public async Task InitAsync(CancellationToken cancellationToken = default)
        {
            await _persistentDatabase.InitAsync(cancellationToken);
        }

        public Task AddCategoryAsync(Category category)
        {
            return _persistentDatabase.AddCategoryAsync(category);
        }

        public async Task AddPhotographAsync(Photograph photograph)
        {
            // prepare the title - it defaults to the filename without the extension
            if (photograph.Title == Path.GetFileNameWithoutExtension(photograph.FileName))
            {
                photograph.Title = string.Empty;
            }

            // If intelligence service is available and photo needs enrichment, analyze it
            if (intelligenceService != null && 
                (string.IsNullOrWhiteSpace(photograph.Title) || 
                 string.IsNullOrWhiteSpace(photograph.Description) ||
                 photograph.Tags.Length == 0 ||
                 photograph.Categories.Length == 0))
            {
                try
                {
                    logger.LogInformation("Enriching photograph {Id} with AI analysis", photograph.Id);
                    
                    var availableCategories = await _persistentDatabase.ListCategoriesAsync();
                    var analysis = await intelligenceService.AnalyzePhotoAsync(
                        photograph.Path,
                        photograph.FileName,
                        photograph.ContentType,
                        string.IsNullOrWhiteSpace(photograph.Description) ? null : photograph.Description,
                        string.IsNullOrWhiteSpace(photograph.Title) ? null : photograph.Title,
                        availableCategories
                    );

                    // Apply AI suggestions
                    if (string.IsNullOrWhiteSpace(photograph.Title))
                    {
                        photograph.Title = analysis.Title;
                    }

                    if (string.IsNullOrWhiteSpace(photograph.Description))
                    {
                        photograph.Description = analysis.Description;
                    }

                    if (photograph.Tags.Length == 0 && analysis.SuggestedTags.Count > 0)
                    {
                        photograph.Tags = analysis.SuggestedTags.ToArray();
                    }

                    if (photograph.Categories.Length == 0 && analysis.SuggestedCategories.Count > 0)
                    {
                        photograph.Categories = analysis.SuggestedCategories.ToArray();
                    }

                    logger.LogInformation("AI enrichment complete for {Id}: Title='{Title}', {TagCount} tags, {CategoryCount} categories",
                        photograph.Id, photograph.Title, photograph.Tags.Length, photograph.Categories.Length);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to enrich photograph {Id} with AI, proceeding without enrichment", photograph.Id);
                    // Continue without AI enrichment
                }
            }

            await _persistentDatabase.AddPhotographAsync(photograph);
        }

        public Task UpdatePhotographAsync(Photograph photograph)
        {
            return _persistentDatabase.UpdatePhotographAsync(photograph);
        }

        public Task<Photograph?> GetPhotographWithIdAsync(string id)
        {
            return _persistentDatabase.GetPhotographWithIdAsync(id);
        }

        public Task<IEnumerable<Category>> ListCategoriesAsync()
        {
            return _persistentDatabase.ListCategoriesAsync();
        }

        public Task<IEnumerable<string>> ListPhotoIdsAsync()
        {
            return _persistentDatabase.ListPhotoIdsAsync();
        }

        public Task<IEnumerable<Tag>> ListTagsAsync()
        {
            return _persistentDatabase.ListTagsAsync();
        }
    }
}
