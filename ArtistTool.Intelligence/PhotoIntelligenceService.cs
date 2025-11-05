using ArtistTool.Domain;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ArtistTool.Intelligence
{
    public class PhotoIntelligenceService(IAIClientProvider aiClientProvider, ILogger<PhotoIntelligenceService> logger)
    {
        public async Task<PhotoAnalysisResult> AnalyzePhotoAsync(
            string imagePath, 
            string fileName, 
            string? existingDescription = null, 
            string? existingTitle = null, IEnumerable<Category>? availableCategories = null)
        {
            logger.LogInformation("Starting AI analysis for photo: {FileName}", fileName);

            var result = new PhotoAnalysisResult
            {
                OriginalFileName = fileName,
                Description = existingDescription ?? string.Empty,
                Title = existingTitle ?? string.Empty
            };

            try
            {
                // Step 1: Generate description if not provided
                if (string.IsNullOrWhiteSpace(result.Description))
                {
                    result.Description = await GenerateDescriptionAsync(imagePath, fileName);
                    logger.LogDebug("Generated description: {Description}", result.Description);
                }

                // Step 2: Generate title if not provided
                if (string.IsNullOrWhiteSpace(result.Title))
                {
                    result.Title = await GenerateTitleAsync(fileName, result.Description);
                    logger.LogDebug("Generated title: {Title}", result.Title);
                }

                // Step 3: Generate tags and categories
                var (tags, categories) = await GenerateTagsAndCategoriesAsync(result.Title, result.Description, availableCategories);
                result.SuggestedTags = tags;
                result.SuggestedCategories = categories;
                
                logger.LogInformation("AI analysis complete: Title='{Title}', Tags={TagCount}, Categories={CategoryCount}", 
                    result.Title, tags.Count, categories.Count);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to analyze photo {FileName}", fileName);
                throw;
            }

            return result;
        }

        private async Task<string> GenerateDescriptionAsync(string imagePath, string fileName)
        {
            logger.LogDebug("Generating description from image for {FileName}", fileName);
            
            var visionClient = aiClientProvider.GetVisionClient();
            
            // Read image with retry logic to handle potential file locking issues
            byte[] imageBytes;
            int retryCount = 0;
            const int maxRetries = 3;
            const int retryDelayMs = 500;
            
            while (true)
            {
                try
                {
                    // Use FileShare.Read to allow concurrent reads if needed
                    using var fs = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    using var ms = new MemoryStream();
                    await fs.CopyToAsync(ms);
                    imageBytes = ms.ToArray();
                    break;
                }
                catch (IOException ex) when (retryCount < maxRetries)
                {
                    retryCount++;
                    logger.LogWarning(ex, "File lock detected on attempt {Attempt} of {MaxRetries}, retrying after {Delay}ms", 
                        retryCount, maxRetries, retryDelayMs);
                    await Task.Delay(retryDelayMs);
                }
            }
            
            var imageContent = new ImageContent(imageBytes, "image/jpeg");

            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, "You are an expert art and photography analyst. Provide detailed, vivid descriptions of photographs focusing on composition, subject matter, lighting, mood, and artistic elements. Keep descriptions concise but evocative (2-3 sentences)."),
                new(ChatRole.User, [
                    new TextContent("Describe this photograph in detail:"),
                    imageContent
                ])
            };

            var response = await visionClient.CompleteAsync(messages);
            return response.Message.Text ?? "No description available";
        }

        private async Task<string> GenerateTitleAsync(string fileName, string description)
        {
            logger.LogDebug("Generating title from description and filename: {FileName}", fileName);
            
            var conversationalClient = aiClientProvider.GetConversationalClient();
            
            var prompt = $@"Based on the following information, generate a compelling, concise title (3-7 words) for this photograph:

File name: {fileName}
Description: {description}

Provide ONLY the title, nothing else. Make it artistic and evocative.";

            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, "You are an expert at creating compelling, artistic titles for photographs. Generate titles that are concise, evocative, and capture the essence of the image."),
                new(ChatRole.User, prompt)
            };

            var response = await conversationalClient.CompleteAsync(messages);
            return response.Message.Text?.Trim().Trim('"') ?? Path.GetFileNameWithoutExtension(fileName);
        }

        private async Task<(List<string> tags, List<string> categories)> GenerateTagsAndCategoriesAsync(
            string title, 
            string description, 
            IEnumerable<Category>? availableCategories)
        {
            logger.LogDebug("Generating tags and categories for: {Title}", title);
            
            var conversationalClient = aiClientProvider.GetConversationalClient();
            var categoryNames = availableCategories?.Select(c => c.Name).Where(n => n != "All").ToList() ?? new List<string>();
            
            var categoriesSection = categoryNames.Any() 
                ? $"Available categories (choose ONLY from these): {string.Join(", ", categoryNames)}" 
                : "No predefined categories available.";

            var prompt = $@"Analyze this photograph and generate relevant tags and categories.

Title: {title}
Description: {description}

{categoriesSection}

Generate:
1. Tags: Create as many relevant tags as needed (5-15 tags). Include subjects, colors, moods, artistic styles, techniques, themes, etc.
2. Categories: Select 1-3 most appropriate categories from the available list. If no categories fit well, return an empty list for categories.

Return your response in this exact JSON format:
{{
  ""tags"": [""tag1"", ""tag2"", ""tag3""],
  ""categories"": [""category1"", ""category2""]
}}

Important: 
- Only use tags that are genuinely relevant
- Only select categories that truly match from the available list
- If no categories fit, return an empty categories array";

            var messages = new List<ChatMessage>
            {
                new ChatMessage(ChatRole.System, "You are an expert at photo categorization and tagging. Generate accurate, relevant tags and select appropriate categories. Always respond with valid JSON only."),
                new ChatMessage(ChatRole.User, prompt)
            };

            var response = await conversationalClient.CompleteAsync(messages);
            var jsonResponse = response.Message.Text ?? "{}";
            
            // Clean up markdown code blocks if present
            jsonResponse = jsonResponse.Trim();
            if (jsonResponse.StartsWith("```json"))
            {
                jsonResponse = jsonResponse.Substring(7);
            }
            if (jsonResponse.StartsWith("```"))
            {
                jsonResponse = jsonResponse.Substring(3);
            }
            if (jsonResponse.EndsWith("```"))
            {
                jsonResponse = jsonResponse.Substring(0, jsonResponse.Length - 3);
            }
            jsonResponse = jsonResponse.Trim();

            try
            {
                var result = JsonSerializer.Deserialize<TagCategoryResponse>(jsonResponse, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });
                
                if (result != null)
                {
                    // Filter categories to only include valid ones
                    var validCategories = result.Categories
                        ?.Where(c => categoryNames.Contains(c, StringComparer.OrdinalIgnoreCase))
                        .ToList() ?? new List<string>();
                    
                    logger.LogDebug("Generated {TagCount} tags and {CategoryCount} categories", 
                        result.Tags?.Count ?? 0, validCategories.Count);
                    
                    return (result.Tags ?? new List<string>(), validCategories);
                }
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "Failed to parse AI response as JSON: {Response}", jsonResponse);
            }

            return (new List<string>(), new List<string>());
        }

        private class TagCategoryResponse
        {
            public List<string>? Tags { get; set; }
            public List<string>? Categories { get; set; }
        }
    }

    public class PhotoAnalysisResult
    {
        public string OriginalFileName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> SuggestedTags { get; set; } = new();
        public List<string> SuggestedCategories { get; set; } = new();
    }
}
