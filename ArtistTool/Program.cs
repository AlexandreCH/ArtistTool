using ArtistTool.Components;
using ArtistTool.Domain;
using ArtistTool.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register database with proper async initialization
builder.Services.AddSingleton<IPhotoDatabase>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<PersistentPhotoDatabase>>();
    var db = new PersistentPhotoDatabase(logger);
    return db;
});

builder.Services.AddSingleton<IImageManager, ImageManager>();

var app = builder.Build();

// Initialize database after building the app
var dbLogger = app.Services.GetRequiredService<ILogger<Program>>();
var database = app.Services.GetRequiredService<IPhotoDatabase>();
if (database is PersistentPhotoDatabase persistentDb)
{
    dbLogger.LogInformation("Initializing PersistentPhotoDatabase");
    await persistentDb.InitAsync();
    dbLogger.LogInformation("PersistentPhotoDatabase initialized successfully");
}

app.MapDefaultEndpoints();

app.MapGet("/images/{type}/{id}", async (string type, string id, IPhotoDatabase db, ILogger<Program> logger) =>
{
    logger.LogDebug("Image request: type={Type}, id={Id}", type, id);
    
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
