# ArtistTool

A Blazor Server application for managing and organizing photographs with categories and tags.

## Features

- **Photo Upload**: Upload multiple images (up to 10 files, 20MB each)
- **Category Management**: Organize photos into custom categories
- **Tag Support**: Add searchable tags to photos
- **Photo Editing**: Edit titles, descriptions, categories, and tags
- **Photo Viewing**: Full-screen photo viewer with metadata display
- **Persistent Storage**: Photos and metadata stored locally with JSON-based database

## Technology Stack

- **.NET 10** (Preview)
- **Blazor Server** with Interactive Server rendering
- **C# 14**
- **ASP.NET Core**
- **File-based storage** with atomic writes

## Project Structure

```
ArtistTool/
??? ArtistTool/              # Main Blazor web application
??? ArtistTool.Domain/       # Domain models and database layer
??? ArtistTool.Services/     # Business logic and image management
??? ArtistTool.AppHost/      # .NET Aspire orchestration
??? ArtistTool.ServiceDefaults/ # Shared service configuration
```

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (Preview)
- A modern web browser

### Running the Application

1. Clone the repository:
   ```bash
   git clone <repository-url>
   cd ArtistTool
   ```

2. Run the application:
   ```bash
   cd ArtistTool
   dotnet run
   ```

3. Open your browser to `https://localhost:7290` (or the URL shown in the console)

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
      "ArtistTool": "Debug",
      "ArtistTool.Domain": "Debug",
      "ArtistTool.Services": "Debug"
    }
  }
}
```

### Data Storage

Photos and metadata are stored in:
- **Windows**: `%APPDATA%\ArtistTool\`
- **macOS/Linux**: `~/.config/ArtistTool/`

## Usage

### Uploading Photos

1. Click **Add photographs** button
2. Select one or more image files (jpg, jpeg, png, gif, tif, tiff)
3. Wait for upload to complete
4. Click **Done** to return to the gallery

### Managing Categories

1. Click **Add category** button
2. Enter a name and description
3. Click **Save**

### Editing Photos

1. Click on any photo thumbnail to view it
2. Click the **Edit** button
3. Modify title, description, categories (checkboxes), or tags
4. Press Enter to add tags, × to remove them
5. Click **Save** to persist changes

## Architecture

### Persistence Layer

- **PhotoDatabase**: In-memory database implementation
- **PersistentPhotoDatabase**: File-based persistence with mutex-protected operations
- **Atomic writes**: Temp file ? Move pattern to prevent corruption

### Image Management

- **ImageManager**: Handles file uploads and storage
- **Thumbnail support**: ThumbnailPath property for optimized display
- **Content-type validation**: Only image types accepted

### Concurrency

- **SemaphoreSlim**: Protects all database operations
- **Thread-safe**: Read/write operations serialized
- **Disposal pattern**: Proper cleanup of resources

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

Built with:
- [Blazor](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
- [ASP.NET Core](https://dotnet.microsoft.com/apps/aspnet)
- [.NET Aspire](https://learn.microsoft.com/dotnet/aspire/)
