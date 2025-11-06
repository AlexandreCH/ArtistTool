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
            string contentType,
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
                    result.Description = await GenerateDescriptionAsync(imagePath, contentType, fileName);
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

        public async Task<CanvasPreviewResult> GenerateCanvasPreviewAsync(Photograph photo)
        {
            logger.LogInformation("Generating canvas preview for photo: {PhotoId} - {Title}", photo.Id, photo.Title);

            try
            {
                // Step 1: Generate canvas preview visualization
                var canvasPreviewPath = await GenerateCanvasVisualizationAsync(photo);
                logger.LogDebug("Generated canvas preview at {Path}", canvasPreviewPath);

                // Step 2: Generate marketing content (price, headline, copy, twitter)
                var marketingContent = await GenerateMarketingContentAsync(photo);
                logger.LogDebug("Generated marketing content: Headline='{Headline}', Price=${Price}", 
                    marketingContent.Headline, marketingContent.Price);

                return new CanvasPreviewResult
                {
                    PhotoId = photo.Id,
                    PathToCanvasPreview = canvasPreviewPath,
                    Headline = marketingContent.Headline,
                    MarketingCopy = marketingContent.MarketingCopy,
                    TwitterText = marketingContent.TwitterText,
                    Price = marketingContent.Price
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to generate canvas preview for photo {PhotoId}", photo.Id);
                throw;
            }
        }

        private async Task<string> GenerateCanvasVisualizationAsync(Photograph photo)
        {
            logger.LogDebug("Generating canvas visualization image for {PhotoId}", photo.Id);

            var previewPath = Path.Combine(
                Path.GetDirectoryName(photo.Path)!,
                $"{photo.Id}_canvas_preview.png");

            if (File.Exists(previewPath))
            {
                logger.LogInformation("Canvas preview already exists at {Path}, skipping generation", previewPath);
                return previewPath;
            }

            // Step 1: Retrieve the original image
            byte[] imageBytes;
            using (var fs = new FileStream(photo.Path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var ms = new MemoryStream())
            {
                await fs.CopyToAsync(ms);
                imageBytes = ms.ToArray();
            }

            var imageContent = new DataContent(imageBytes, photo.ContentType);

            var canvasPrompt = "Transform the image into a photorealistic visualization of a fine art photograph displayed as a gallery-wrapped canvas print on a white wall, viewed from a 45-degree angle showing the depth and wrapped edges of the canvas.";
            
            logger.LogDebug("Generated canvas visualization prompt: {Prompt}", canvasPrompt);

            // Step 2: Generate the actual canvas visualization image
            var imageClient = aiClientProvider.GetImageClient();
            
            logger.LogInformation("Requesting image generation for canvas preview...");
            var imageGenRequest = new ImageGenerationRequest
            {
              OriginalImages = [imageContent],
              Prompt = canvasPrompt 
            };
            
            var imageGenResponse = await imageClient.GenerateAsync(imageGenRequest);

            // Extract generated image from response
            var generatedImageContent = imageGenResponse.Contents
                .OfType<DataContent>()
                .FirstOrDefault();

            if (generatedImageContent?.Data != null)
            {
             
                await File.WriteAllBytesAsync(previewPath, generatedImageContent.Data);
                
                logger.LogInformation("Canvas visualization image saved to {Path} ({Size} bytes)", 
                    previewPath, generatedImageContent.Data.Length);
                
                return previewPath;
            }
            else
            {
                // Fallback: save the prompt if image generation isn't available
                logger.LogWarning("Image generation response did not contain image data. Saving prompt instead.");
                
                var promptPath = Path.Combine(
                    Path.GetDirectoryName(photo.Path)!,
                    $"{photo.Id}_canvas_prompt.txt"
                );
                
                var fallbackContent = $"Canvas Visualization Prompt:\n{canvasPrompt}\n\nResponse Text:\n{imageGenResponse.Contents.OfType<TextContent>().FirstOrDefault()?.Text}";
                await File.WriteAllTextAsync(promptPath, fallbackContent);
                
                logger.LogInformation("Canvas visualization prompt saved to {Path}. Configure image generation in Azure OpenAI to generate actual images.", promptPath);
                
                return promptPath;
            }
        }

        private async Task<MarketingContent> GenerateMarketingContentAsync(Photograph photo)
        {
            logger.LogDebug("Generating marketing content for {PhotoId} - {Title}", photo.Id, photo.Title);
            
            var marketingPreviewPath = Path.Combine(
                Path.GetDirectoryName(photo.Path)!,
                $"{photo.Id}_marketing.json");

            if (File.Exists(marketingPreviewPath))
            {
                return JsonSerializer.Deserialize<MarketingContent>(
                    await File.ReadAllTextAsync(marketingPreviewPath))!;
            }

            var conversationalClient = aiClientProvider.GetConversationalClient();

            var prompt = $@"You are a marketing expert for fine art photography prints. Generate compelling marketing content for this canvas print.

Photo Details:
- Title: {photo.Title}
- Description: {photo.Description}
- Tags: {string.Join(", ", photo.Tags)}
- Categories: {string.Join(", ", photo.Categories)}

Tasks:
1. Research typical pricing for similar fine art canvas prints and suggest a competitive starting price in USD (consider size, subject matter, artistic value)
2. Create an attention-grabbing headline (5-10 words) that would make someone want to buy this print
3. Write compelling marketing copy (2-3 paragraphs) for a product page that highlights the artistic value, emotional impact, and how it would enhance a space
4. Create a Twitter/X post (under 280 characters) with 2-3 relevant hashtags to drive traffic to the product page

Make the content professional, compelling, and focused on the emotional and aesthetic value of the artwork.";

            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, "You are an expert marketing professional specializing in fine art photography sales. You understand pricing strategy, compelling copywriting, and social media engagement. Always respond with valid JSON only."),
                new(ChatRole.User, prompt)
            };

            var response = await conversationalClient.GetResponseAsync<MarketingContent>(messages);
            await File.WriteAllTextAsync(marketingPreviewPath, JsonSerializer.Serialize(response.Result));
            return response.Result;
        }

        private async Task<string> GenerateDescriptionAsync(string imagePath, string contentType, string fileName)
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
            
            var imageContent = new DataContent(imageBytes, contentType);

            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, "You are an expert art and photography analyst. Provide detailed, vivid descriptions of photographs focusing on composition, subject matter, lighting, mood, and artistic elements. Keep descriptions concise but evocative (2-3 sentences)."),
                new(ChatRole.User, [
                    new TextContent("Describe this photograph in detail:"),
                    imageContent
                ])
            };

            var response = await visionClient.GetResponseAsync(messages);
            return response.Text ?? "No description available";
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

            var response = await conversationalClient.GetResponseAsync(messages);
            return response.Text?.Trim().Trim('"') ?? Path.GetFileNameWithoutExtension(fileName);
        }

        private async Task<(List<string> tags, List<string> categories)> GenerateTagsAndCategoriesAsync(
            string title, 
            string description, 
            IEnumerable<Category>? availableCategories)
        {
            logger.LogDebug("Generating tags and categories for: {Title}", title);
            
            var conversationalClient = aiClientProvider.GetConversationalClient();
            var categoryNames = availableCategories?.Select(c => c.Name).Where(n => n != "All").ToList() ?? [];
            
            var categoriesSection = categoryNames.Count != 0
                ? $"Available categories (choose ONLY from these): {string.Join(", ", categoryNames)}" 
                : "No predefined categories available.";

            var prompt = $@"Analyze this photograph and generate relevant tags and categories.

Title: {title}
Description: {description}

{categoriesSection}

Generate:
1. Tags: Create as many relevant tags as needed (5-15 tags). Include subjects, colors, moods, artistic styles, techniques, themes, etc.
2. Categories: Select 1-3 most appropriate categories from the available list. If no categories fit well, return an empty list for categories.

Important: 
- Only use tags that are genuinely relevant
- Only select categories that truly match from the available list
- If no categories fit, return an empty categories array";

            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, "You are an expert at photo categorization and tagging. Generate accurate, relevant tags and select appropriate categories. Always respond with valid JSON only."),
                new(ChatRole.User, prompt)
            };

            var response = await conversationalClient.GetResponseAsync<TagCategoryResponse>(messages);
            var result = response.Result;
            
            // Filter categories to only include valid ones
            var validCategories = result.Categories
                ?.Where(c => categoryNames.Contains(c, StringComparer.OrdinalIgnoreCase))
                .ToList() ?? [];
                    
            logger.LogDebug("Generated {TagCount} tags and {CategoryCount} categories", 
                result.Tags?.Count ?? 0, validCategories.Count);
                    
            return (result.Tags ?? [], validCategories);
        }

        private class TagCategoryResponse
        {
            public List<string>? Tags { get; set; }
            public List<string>? Categories { get; set; }
        }

        private class MarketingContentResponse
        {
            public decimal? Price { get; set; }
            public string? Headline { get; set; }
            public string? MarketingCopy { get; set; }
            public string? TwitterText { get; set; }
        }

        private class MarketingContent
        {
            public decimal Price { get; set; }
            public string Headline { get; set; } = string.Empty;
            public string MarketingCopy { get; set; } = string.Empty;
            public string TwitterText { get; set; } = string.Empty;
        }
    }

    public class CanvasPreviewResult
    {
        public string PhotoId { get; set; } = string.Empty;
        public string PathToCanvasPreview { get; set; } = string.Empty;
        public string Headline {  get; set; } = string.Empty;
        public string MarketingCopy { get; set; } = string.Empty;
        public string TwitterText { get; set; } = string.Empty;
        public decimal Price { get; set; } = 0;
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
