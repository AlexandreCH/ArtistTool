using ArtistTool;
using ArtistTool.Components;
using ArtistTool.Domain;
using ArtistTool.Intelligence;
using ArtistTool.Services;
using ArtistTool.Workflows;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var useAI = builder.Configuration["Options:UseAI"]?.ToLower() == "true";
var useWorkflows = builder.Configuration["Options:UseWorkflows"]?.ToLower() == "true";

if (useAI)
{ 
    builder.Services.AddSingleton(new AIOptions
    {
        UseAI = true,
        ShowMarketing = builder.Configuration["Options:MarketingMode"]?.ToLower() == "true",
        UseWorkflows = useWorkflows
    });

    builder.Services.AddIntelligenceServices(key => builder.Configuration[key] ?? string.Empty);
    
    // Add workflow services if enabled
    if (useWorkflows)
    {
        builder.Services.AddWorkflowServices();
    }
}
else
{
    builder.Services.AddSingleton(new AIOptions
    {
        UseAI = false,
        ShowMarketing = false,
        UseWorkflows = false
    });
    builder.Services.AddDomainServices();
}

builder.Services.AddServices();

var app = builder.Build();

var dbLogger = app.Services.GetRequiredService<ILogger<Program>>();
var database = app.Services.GetRequiredService<IPhotoDatabase>();    
dbLogger.LogInformation("Initializing PhotoDatabase");
await database.InitAsync(CancellationToken.None);
dbLogger.LogInformation("PhotoDatabase initialized successfully");

// Initialize workflows if enabled
var aiOptions = app.Services.GetRequiredService<AIOptions>();
if (aiOptions.UseWorkflows)
{
    dbLogger.LogInformation("Initializing AI Workflows");
    try
    {
        await app.Services.InitializeClientsAsync();
        dbLogger.LogInformation("AI Workflows initialized successfully");
    }
    catch (Exception ex)
    {
        dbLogger.LogError(ex, "Failed to initialize AI Workflows");
        // Don't fail the application startup, just log the error
    }
}

app.MapDefaultEndpoints();

app.MapGet("/report/{photoId}/{reportId:int}/{filename}",
    async (string photoId, int reportId, string filename, IProjectManager pm, ILogger<Program> logger) =>
{
    logger.LogDebug("Report request: photoId={PhotoId}, reportId={ReportId}, filename={Filename}", photoId, reportId, filename);

    try
    {
        var (content, mediaType) = await pm.GetReportAssetAsync(photoId, reportId, filename);
        if (mediaType.StartsWith("image", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogDebug("Serving report image asset: photoId={PhotoId}, reportId={ReportId}, filename={Filename}", photoId, reportId, filename);
            return Results.File(content.ToArray(), mediaType, filename);
        }
        else
        {
            logger.LogDebug("Serving report text asset: photoId={PhotoId}, reportId={ReportId}, filename={Filename}", photoId, reportId, filename);
            return Results.Content(System.Text.Encoding.UTF8.GetString(content.Span), mediaType);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error retrieving report asset: photoId={PhotoId}, reportId={ReportId}, filename={Filename}", photoId, reportId, filename);
        return Results.StatusCode(500);
    }
});

app.MapGet("/images/{type}/{id}", async (string type, string id, IPhotoDatabase db, ILogger<Program> logger) =>
{
    logger.LogDebug("Image request: type={Type}, id={Id}", type, id);
    
    // Check if this is a canvas preview request
    if (id.EndsWith("_canvas_preview"))
    {
        var photoId = id.Replace("_canvas_preview", "");
        var photoForCanvas = await db.GetPhotographWithIdAsync(photoId);
        if (photoForCanvas is null)
        {
            logger.LogWarning("Photo not found for canvas preview: id={Id}", photoId);
            return Results.NotFound();
        }
        
        var canvasPreviewPath = Path.Combine(
            Path.GetDirectoryName(photoForCanvas.Path)!,
            $"{photoId}_canvas_preview.png"
        );
        
        if (File.Exists(canvasPreviewPath))
        {
            logger.LogDebug("Serving canvas preview from {Path}", canvasPreviewPath);
            return Results.File(canvasPreviewPath, "image/png");
        }
        
        logger.LogWarning("Canvas preview not found at {Path}", canvasPreviewPath);
        return Results.NotFound();
    }
    
    var photo = await db.GetPhotographWithIdAsync(id);
    if (photo is null)
    {
        logger.LogWarning("Image not found: id={Id}", id);
        return Results.NotFound();
    }
    
    var path = type == "thumb" ? photo.ThumbnailPath : photo.Path;
    logger.LogDebug("Serving image from {Path}", path);
    return Results.File(path, photo.ContentType, photo.FileName);
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
