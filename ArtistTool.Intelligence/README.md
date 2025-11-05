# ArtistTool.Intelligence

AI-powered photo analysis and enrichment using Microsoft.Extensions.AI and Azure OpenAI with Managed Identity.

## Features

### Automatic Photo Enrichment

When photos are added without complete metadata, the `IntelligentPhotoDatabase` automatically:

1. **Generates Descriptions** - Uses vision AI to analyze the image and create detailed, evocative descriptions
2. **Creates Titles** - Generates compelling, artistic titles based on the description and filename
3. **Suggests Tags** - Creates 5-15 relevant tags covering subjects, colors, moods, styles, techniques, and themes
4. **Assigns Categories** - Intelligently maps photos to existing categories

## Architecture

### Components

- **`IntelligentPhotoDatabase`** - Decorator around `PersistentPhotoDatabase` that adds AI enrichment
- **`PhotoIntelligenceService`** - Core AI analysis logic
- **`IAIClientProvider`** - Interface for AI client abstraction
- **`AzureOpenAIClientProvider`** - Azure OpenAI implementation with Managed Identity support

### AI Models Used

- **Vision Model** (gpt-4o): Image analysis and description generation
- **Conversational Model** (gpt-4o): Title generation, tagging, and categorization

### Authentication

Uses **Azure Managed Identity** (DefaultAzureCredential) which supports:
- ? Managed Identity (System or User-assigned)
- ? Azure CLI credentials (for local development)
- ? Visual Studio credentials (for local development)
- ? Environment variables (for local development)
- ? Workload Identity (for Kubernetes)

**No API keys or secrets required!**

## Usage

### Setup with Azure OpenAI + Managed Identity

```csharp
// Configure AI provider with Managed Identity
var aiProvider = new AzureOpenAIClientProvider(
    endpoint: "https://your-openai-resource.openai.azure.com/",
    conversationalDeployment: "gpt-4o",
    visionDeployment: "gpt-4o"
);

// Create intelligence service
var intelligenceService = new PhotoIntelligenceService(
    aiProvider,
    loggerFactory.CreateLogger<PhotoIntelligenceService>()
);

// Create intelligent database
var database = new IntelligentPhotoDatabase(
    loggerFactory.CreateLogger<PersistentPhotoDatabase>(),
    loggerFactory.CreateLogger<IntelligentPhotoDatabase>(),
    intelligenceService
);

// Initialize
await database.InitAsync();
```

### Azure Setup

#### 1. Create Azure OpenAI Resource

```bash
az cognitiveservices account create \
  --name your-openai-resource \
  --resource-group your-rg \
  --kind OpenAI \
  --sku S0 \
  --location eastus
```

#### 2. Deploy Models

```bash
# Deploy GPT-4o for both conversational and vision
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

#### 3. Assign Managed Identity Permissions

For **App Service / Azure Functions**:
```bash
# Enable system-assigned managed identity
az webapp identity assign --name your-app --resource-group your-rg

# Get the principal ID
PRINCIPAL_ID=$(az webapp identity show --name your-app --resource-group your-rg --query principalId -o tsv)

# Assign "Cognitive Services OpenAI User" role
az role assignment create \
  --assignee $PRINCIPAL_ID \
  --role "Cognitive Services OpenAI User" \
  --scope /subscriptions/{subscription-id}/resourceGroups/your-rg/providers/Microsoft.CognitiveServices/accounts/your-openai-resource
```

For **Local Development** (Azure CLI):
```bash
# Login with Azure CLI
az login

# Assign role to your user account
az role assignment create \
  --assignee your-email@domain.com \
  --role "Cognitive Services OpenAI User" \
  --scope /subscriptions/{subscription-id}/resourceGroups/your-rg/providers/Microsoft.CognitiveServices/accounts/your-openai-resource
```

### Automatic Enrichment

```csharp
// Photo with minimal metadata
var photo = new Photograph 
{
    Id = Guid.NewGuid().ToString(),
    FileName = "sunset.jpg",
    Path = "/path/to/sunset.jpg",
    ContentType = "image/jpeg"
    // No title, description, tags, or categories
};

// AI will automatically enrich on add
await database.AddPhotographAsync(photo);

// Result will have:
// - Title: "Golden Hour Serenity by the Sea"
// - Description: "A breathtaking sunset over calm ocean waters..."
// - Tags: ["sunset", "ocean", "golden hour", "landscape", "nature", ...]
// - Categories: ["Landscape", "Nature"]
```

### Partial Enrichment

If you provide some metadata, only missing fields are generated:

```csharp
var photo = new Photograph 
{
    FileName = "portrait.jpg",
    Title = "Maria", // Already has title
    // No description - will be generated
    // No tags/categories - will be generated
};

await database.AddPhotographAsync(photo);
```

### Disable AI (Optional)

Pass `null` for `intelligenceService` to use as a standard persistent database:

```csharp
var database = new IntelligentPhotoDatabase(
    persistentLogger,
    intelligentLogger,
    intelligenceService: null // No AI enrichment
);
```

### Using Specific Credentials

For advanced scenarios, you can provide a specific credential:

```csharp
using Azure.Identity;

// Use user-assigned managed identity
var credential = new ManagedIdentityCredential(clientId: "your-client-id");

// Or use workload identity for Kubernetes
var credential = new WorkloadIdentityCredential();

// Or use a specific service principal
var credential = new ClientSecretCredential(
    tenantId: "your-tenant-id",
    clientId: "your-client-id",
    clientSecret: "your-client-secret"
);

var aiProvider = new AzureOpenAIClientProvider(
    endpoint: "https://your-openai-resource.openai.azure.com/",
    credential: credential,
    conversationalDeployment: "gpt-4o",
    visionDeployment: "gpt-4o"
);
```

## Configuration

### appsettings.json

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://your-openai-resource.openai.azure.com/",
    "ConversationalDeployment": "gpt-4o",
    "VisionDeployment": "gpt-4o"
  }
}
```

### Environment Variables (for local dev)

```bash
# Azure CLI will be used automatically by DefaultAzureCredential
az login

# Or set environment variables for service principal
export AZURE_CLIENT_ID="your-client-id"
export AZURE_CLIENT_SECRET="your-client-secret"
export AZURE_TENANT_ID="your-tenant-id"
```

## AI Analysis Pipeline

1. **Vision Analysis** (if description missing)
   - Sends image to vision model
   - Receives detailed description (2-3 sentences)
   - Focuses on composition, subject, lighting, mood, artistic elements

2. **Title Generation** (if title missing)
   - Uses description + filename
   - Creates concise, evocative title (3-7 words)
   - Artistic and compelling

3. **Tag & Category Generation** (if missing)
   - Analyzes title and description
   - Creates comprehensive tag list (5-15 tags)
   - Maps to existing categories only
   - Returns structured JSON response

## Logging

All AI operations are logged with structured logging:

```
info: Starting AI analysis for photo: sunset.jpg
debug: Generated description: A breathtaking sunset...
debug: Generated title: Golden Hour Serenity
debug: Generated 12 tags and 2 categories
info: AI enrichment complete for photo-123: Title='Golden Hour Serenity', 12 tags, 2 categories
```

## Error Handling

- AI failures are logged but don't prevent photo storage
- Photos are saved with partial metadata if AI fails
- Graceful degradation to basic functionality
- DefaultAzureCredential tries multiple auth methods automatically

## Dependencies

- `Microsoft.Extensions.AI` - AI abstraction layer
- `Microsoft.Extensions.AI.OpenAI` - OpenAI integration
- `Azure.AI.OpenAI` - Azure OpenAI SDK
- `Azure.Identity` - Azure authentication with Managed Identity
- `ArtistTool.Domain` - Domain models

## Security Benefits of Managed Identity

? **No secrets in code or config**
? **Automatic credential rotation** by Azure
? **Centralized access management** via Azure RBAC
? **Audit trail** in Azure AD logs
? **Works across environments** (dev, staging, prod)
? **Supports local development** via Azure CLI

## DefaultAzureCredential Chain

DefaultAzureCredential tries these methods in order:
1. **Environment variables** (for CI/CD)
2. **Workload Identity** (for AKS)
3. **Managed Identity** (for Azure services)
4. **Visual Studio** (for local dev)
5. **Azure CLI** (for local dev)
6. **Azure PowerShell** (for local dev)

## Performance Considerations

- Vision analysis is the slowest operation (~2-5 seconds)
- Title generation is fast (~1 second)
- Tag/category generation is moderate (~2 seconds)
- Total enrichment time: 5-10 seconds per photo
- Consider async/background processing for bulk uploads

## Cost Considerations

Using Azure OpenAI GPT-4o (as of 2025):
- Vision analysis: ~$0.01-0.02 per image
- Text generation: ~$0.001-0.002 per photo
- Estimated total: ~$0.01-0.03 per enriched photo
- No additional cost for Managed Identity authentication

## Deployment Examples

### Azure App Service

```bash
# Create app with managed identity
az webapp create --name myapp --resource-group myrg --plan myplan --runtime "DOTNET|10.0"
az webapp identity assign --name myapp --resource-group myrg

# Configure app settings
az webapp config appsettings set --name myapp --resource-group myrg --settings \
  AzureOpenAI__Endpoint="https://your-resource.openai.azure.com/" \
  AzureOpenAI__ConversationalDeployment="gpt-4o" \
  AzureOpenAI__VisionDeployment="gpt-4o"
```

### Azure Container Apps

```bash
# Create container app with managed identity
az containerapp create \
  --name myapp \
  --resource-group myrg \
  --environment myenv \
  --system-assigned \
  --env-vars \
    AzureOpenAI__Endpoint="https://your-resource.openai.azure.com/" \
    AzureOpenAI__ConversationalDeployment="gpt-4o" \
    AzureOpenAI__VisionDeployment="gpt-4o"
```

### AKS with Workload Identity

```yaml
apiVersion: v1
kind: Pod
metadata:
  name: artisttool
  labels:
    azure.workload.identity/use: "true"
spec:
  serviceAccountName: artisttool-sa
  containers:
  - name: app
    image: artisttool:latest
    env:
    - name: AzureOpenAI__Endpoint
      value: "https://your-resource.openai.azure.com/"
```

## Future Enhancements

- Batch processing for multiple photos
- Caching of similar descriptions
- Fine-tuned models for photography domain
- Similarity search across photo embeddings
- Smart album creation based on content analysis
- Support for Azure AI Vision service alongside OpenAI
