# ArtistTool

A Blazor Server application for managing and organizing photographs with **AI-powered automatic tagging, categorization, and description generation** using Azure OpenAI.

## Features

### ?? AI-Powered Photo Analysis
- **Automatic Descriptions**: GPT-4o vision model analyzes images and generates detailed, evocative descriptions
- **Smart Titles**: AI creates compelling 3-7 word titles based on image content and filename
- **Intelligent Tagging**: Automatically generates 5-15 relevant tags (subjects, colors, moods, styles, techniques)
- **Category Mapping**: Intelligently assigns photos to existing categories
- **Secure Authentication**: Uses Azure Managed Identity (no API keys required!)

### ?? Photo Management
- **Batch Upload**: Upload up to 10 files simultaneously (20MB each)
- **Supported Formats**: JPG, JPEG, PNG, GIF, TIF, TIFF
- **Full-Screen Viewer**: Dedicated photo detail page with metadata
- **Edit Metadata**: Update titles, descriptions, categories, and tags
- **Photo Grid**: Responsive tile grid with hover effects and clickable tiles

### ??? Organization
- **Categories**: Create custom categories to organize your collection
- **Tags**: Flexible tagging system with easy add/remove
- **Search Ready**: Structured metadata for future search capabilities

### ?? Data Management
- **Persistent Storage**: JSON-based database with atomic writes
- **Thread-Safe**: Mutex-protected operations for concurrent access
- **Auto-Recovery**: Handles missing files and corruption gracefully
- **Comprehensive Logging**: Debug-level logging for troubleshooting

## Technology Stack

- **.NET 10** (Preview) with C# 14
- **Blazor Server** with Interactive Server rendering
- **Azure OpenAI** (GPT-4o for vision and text)
- **Azure Managed Identity** (DefaultAzureCredential)
- **Microsoft.Extensions.AI** - AI abstraction layer
- **ASP.NET Core** with .NET Aspire support
- **File-based storage** with atomic write operations

## Project Structure

```
ArtistTool/
??? ArtistTool/                  # Main Blazor web application
?   ??? Components/
?   ?   ??? Pages/              # Home, Photo detail pages
?   ?   ??? Layout/             # NavMenu, MainLayout
?   ?   ??? AddCategory.razor   # Category creation component
?   ?   ??? PhotosGrid.razor    # Photo grid display
?   ?   ??? PhotoTile.razor     # Individual photo tile
?   ?   ??? UploadPhoto.razor   # Multi-file upload component
?   ??? Program.cs              # App configuration
??? ArtistTool.Domain/           # Domain models and persistence
?   ??? Photograph.cs           # Photo model with metadata
?   ??? Category.cs             # Category model
?   ??? Tag.cs                  # Tag model
?   ??? PhotoDatabase.cs        # In-memory database
?   ??? PersistentPhotoDatabase.cs # JSON file persistence
??? ArtistTool.Intelligence/     # AI-powered features
?   ??? IntelligentPhotoDatabase.cs # AI enrichment wrapper
?   ??? PhotoIntelligenceService.cs # AI analysis logic
?   ??? AzureOpenAIClientProvider.cs # Azure OpenAI client
?   ??? IAIClientProvider.cs    # AI client abstraction
??? ArtistTool.Services/         # Business logic
?   ??? ImageManager.cs         # File upload and storage
?   ??? IImageManager.cs        # Service interface
??? ArtistTool.AppHost/          # .NET Aspire orchestration
??? ArtistTool.ServiceDefaults/  # Shared service configuration
```

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (Preview)
- **Azure OpenAI resource** (for AI features)
- **Azure CLI** (for local development with Managed Identity)
- A modern web browser

### Quick Start (Without AI)

1. Clone the repository:
   ```bash
   git clone https://github.com/JeremyLikness/ArtistTool.git
   cd ArtistTool
   ```

2. Comment out AI services in `Program.cs`:
   ```csharp
   // builder.Services.AddSingleton<IAIClientProvider>(...);
   // builder.Services.AddSingleton<PhotoIntelligenceService>();
   // Use PersistentPhotoDatabase instead:
   builder.Services.AddSingleton<IPhotoDatabase, PersistentPhotoDatabase>();
   ```

3. Run the application:
   ```bash
   cd ArtistTool
   dotnet run
   ```

4. Open browser to `https://localhost:7290`

### Full Setup (With AI)

#### 1. Create Azure OpenAI Resource

```bash
az cognitiveservices account create \
  --name your-openai-resource \
  --resource-group your-rg \
  --kind OpenAI \
  --sku S0 \
  --location eastus
```

#### 2. Deploy GPT-4o Model

```bash
az cognitiveservices account deployment create \
  --name your-openai-resource \
  --resource-group your-rg \
  --deployment-name gpt-4o \
  --model-name gpt-4o \
  --model-version "2024-05-13" \
  --model-format OpenAI \
  --sku-capacity 10 \
  --sku-name "Standard"
```

#### 3. Configure Managed Identity

**For Local Development:**
```bash
# Login with Azure CLI
az login

# Assign your user account the "Cognitive Services OpenAI User" role
az role assignment create \
  --assignee your-email@domain.com \
  --role "Cognitive Services OpenAI User" \
  --scope /subscriptions/{subscription-id}/resourceGroups/your-rg/providers/Microsoft.CognitiveServices/accounts/your-openai-resource
```

**For Azure App Service:**
```bash
# Enable system-assigned managed identity
az webapp identity assign --name your-app --resource-group your-rg

# Get the principal ID
PRINCIPAL_ID=$(az webapp identity show --name your-app --resource-group your-rg --query principalId -o tsv)

# Assign role
az role assignment create \
  --assignee $PRINCIPAL_ID \
  --role "Cognitive Services OpenAI User" \
  --scope /subscriptions/{subscription-id}/resourceGroups/your-rg/providers/Microsoft.CognitiveServices/accounts/your-openai-resource
```

#### 4. Configure Application

Add to `appsettings.json`:
```json
{
  "AzureOpenAI": {
    "Endpoint": "https://your-openai-resource.openai.azure.com/",
    "ConversationalDeployment": "gpt-4o",
    "VisionDeployment": "gpt-4o"
  }
}
```

#### 5. Run with AI

```bash
dotnet run
```

### Running with .NET Aspire

```bash
cd ArtistTool.AppHost
dotnet run
```

## Configuration

### Logging

Configure logging levels in `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "ArtistTool": "Debug",
      "ArtistTool.Domain": "Debug",
      "ArtistTool.Domain.PersistentPhotoDatabase": "Debug",
      "ArtistTool.Services": "Debug",
      "ArtistTool.Services.ImageManager": "Debug",
      "ArtistTool.Intelligence": "Debug"
    }
  }
}
```

### Data Storage

Photos and metadata are stored in:
- **Windows**: `%APPDATA%\ArtistTool\`
- **macOS/Linux**: `~/.config/ArtistTool/`

Directory structure:
```
ArtistTool/
??? images/                    # Photo files (by GUID)
??? photos-index.json         # List of photo IDs
??? categories.json           # Category definitions
??? tags.json                 # Tag definitions
??? {photo-id}.json           # Individual photo metadata
```

## Usage

### Uploading Photos

1. Click **Add photographs** button (toggles uploader)
2. Select one or more image files
3. Upload progress and results displayed
4. **With AI**: Photos are automatically analyzed and enriched
5. Click **Done** to close uploader and refresh gallery

**AI Enrichment Process:**
- Vision model describes the image (2-3 sentences)
- Title is generated from description + filename
- 5-15 relevant tags are created
- Photo is mapped to existing categories

### Managing Categories

1. Click **Add category** button (toggles form)
2. Enter **Name** and **Description**
3. Click **Save**
4. New category appears in the list
5. Photos can be assigned to this category

### Viewing Photos

1. Browse photos in grid on Home page
2. Hover over photo to see description
3. Click photo tile to open detail page
4. View full-size image with metadata
5. See title, description, categories, and tags

### Editing Photos

1. Open photo detail page
2. Click **Edit** button
3. Update fields:
   - **Title**: Text input
   - **Description**: Textarea
   - **Categories**: Checkboxes (select multiple)
   - **Tags**: Type and press Enter to add, × to remove
4. Click **Save** to persist changes
5. Click **Cancel** to discard changes

## Architecture

### AI Integration

- **IntelligentPhotoDatabase**: Decorator pattern wrapping PersistentPhotoDatabase
- **PhotoIntelligenceService**: Core AI analysis logic with retry mechanisms
- **AzureOpenAIClientProvider**: Manages Azure OpenAI clients with Managed Identity
- **OpenAIChatClientAdapter**: Bridges Azure OpenAI SDK to Microsoft.Extensions.AI interface

### Persistence Layer

- **PhotoDatabase**: In-memory implementation with thread-safe collections
- **PersistentPhotoDatabase**: JSON file persistence with:
  - Mutex protection for all operations
  - Atomic writes (temp file ? move pattern)
  - Comprehensive structured logging
  - Duplicate prevention
  - Disposal pattern with IDisposable

### Image Management

- **ImageManager**: 
  - File upload with content-type validation
  - Extension whitelist enforcement
  - Unique GUID-based naming
  - Cleanup on failure
  - Proper file handle management (fixes locking issues)

### Security

- **No API Keys**: Uses Azure Managed Identity (DefaultAzureCredential)
- **RBAC**: Azure role-based access control
- **Validation**: Content-type and extension checks
- **Atomic Operations**: Prevents file corruption
- **Audit Trail**: All access logged in Azure AD

## Performance

### AI Analysis Times
- **Vision analysis**: 2-5 seconds (image description)
- **Title generation**: ~1 second
- **Tag/category generation**: ~2 seconds
- **Total enrichment**: 5-10 seconds per photo

### Optimization Tips
- Consider background job processing for bulk uploads
- Use .NET Aspire for distributed deployments
- Enable response caching for static assets
- Monitor Azure OpenAI token usage

## Cost Considerations

Using Azure OpenAI GPT-4o (approximate costs as of 2025):
- **Vision analysis**: $0.01-0.02 per image
- **Text generation**: $0.001-0.002 per photo
- **Estimated total**: $0.01-0.03 per AI-enriched photo
- **Managed Identity**: No additional cost

## Troubleshooting

### File Locking Errors
Fixed in current version. If you encounter issues:
- Ensure FileStream is disposed before database operations
- Check retry logic in PhotoIntelligenceService
- Review logs for file access patterns

### AI Not Working
1. Verify Azure OpenAI endpoint in configuration
2. Check Managed Identity permissions (Cognitive Services OpenAI User role)
3. For local dev: Ensure `az login` is completed
4. Review logs in ArtistTool.Intelligence namespace

### Database Initialization Hangs
Fixed in current version. If issues persist:
- Check for GetAwaiter().GetResult() anti-patterns
- Ensure InitAsync is called after app.Build()
- Review mutex usage in PersistentPhotoDatabase

## Contributing

Contributions are welcome! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

Key areas for contribution:
- Search functionality
- Album/collection features
- Export capabilities
- Thumbnail generation
- Batch operations
- Additional AI providers
- UI/UX enhancements

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

Built with:
- [Blazor](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
- [ASP.NET Core](https://dotnet.microsoft.com/apps/aspnet)
- [.NET Aspire](https://learn.microsoft.com/dotnet/aspire/)
- [Azure OpenAI](https://azure.microsoft.com/products/ai-services/openai-service)
- [Microsoft.Extensions.AI](https://devblogs.microsoft.com/dotnet/introducing-microsoft-extensions-ai-preview/)

## Roadmap

### Planned Features
- ?? Search by title, description, tags, or categories
- ?? Album/collection management
- ??? Automatic thumbnail generation
- ?? Export photos with metadata
- ?? Batch editing operations
- ?? Photo similarity search using embeddings
- ?? Analytics dashboard
- ?? Multi-user support with authentication
- ?? Azure Blob Storage integration
- ?? Docker containerization

### Future AI Enhancements
- Smart album creation based on content
- Duplicate photo detection
- Photo quality assessment
- Automatic face detection and tagging
- Style transfer and editing suggestions
- Natural language search ("show me sunset photos")

---

**Star ? this repository if you find it useful!**

For questions or issues, please open an [issue on GitHub](https://github.com/JeremyLikness/ArtistTool/issues).
