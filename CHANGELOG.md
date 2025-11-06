# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- **AI-Powered Marketing & Canvas Preview** (`/market/{id}` page)
  - Canvas visualization generation using AI image generation
  - AI-powered market research for competitive pricing
  - Automatic headline creation (5-10 word attention-grabbers)
  - Professional marketing copy generation (2-3 paragraphs)
  - Twitter/X post generation with hashtags (<280 chars)
  - One-click Twitter text copy to clipboard
  - Professional e-commerce product showcase layout
  - Disabled "Buy Now" and "Add to Cart" buttons (ready for future integration)
  - "Market This" button on photo detail page
  - Enhanced visual design with gradient backgrounds and improved contrast
  - Responsive design for mobile and tablet
- **AI-Powered Photo Analysis** using Azure OpenAI with Managed Identity
  - Automatic image description generation using GPT-4o vision model
  - Intelligent title generation from image content and filename
  - Smart tag generation (5-15 relevant tags per photo)
  - Automatic category assignment from existing categories
  - IntelligentPhotoDatabase wrapper for AI enrichment
  - PhotoIntelligenceService for AI operations
  - AzureOpenAIClientProvider with DefaultAzureCredential support
  - Image generation capabilities through IChatClient for canvas previews
- **Photo Detail Page** (`/photo/{id}`)
  - Full-screen photo viewing
  - Title and description display
  - Category pills display
  - Tag display with styling
  - Edit mode for updating photo metadata
- **Photo Editing Features**
  - Inline title editing
  - Description textarea
  - Category selection via checkboxes (excludes "All" category)
  - Tag management (add with Enter key, remove with × button)
  - Save/Cancel actions with loading states
- **Enhanced Photo Upload**
  - Toggle upload/view mode
  - Fixed multi-file upload bug with InputFile component
  - Upload results display with file sizes
  - Automatic grid refresh after upload
- **Category Management UI**
  - AddCategory component with styled form
  - Name and description inputs
  - Save/Cancel actions
  - Toggle add/view mode
  - Automatic category list refresh
- **Photo Grid Enhancements**
  - Clickable photo tiles linking to detail page
  - Title overlay with semi-transparent background
  - Description shown on hover with fade effect
  - Responsive grid layout with gaps
  - Enhanced styling with gradients and shadows
- **Database Improvements**
  - UpdatePhotographAsync method for editing photos
  - Duplicate photo prevention on app restart
  - Thread-safe read/write operations with mutex protection
  - Atomic file write operations with temp file pattern
  - Comprehensive structured logging throughout all layers
  - Debug logging configuration in appsettings.Development.json
- **Navigation Updates**
  - Removed Counter and Weather sample pages
  - Simplified navigation menu (Home only)
- **Repository Setup**
  - Git initialization with proper .gitignore
  - MIT License
  - README.md with comprehensive documentation
  - CONTRIBUTING.md guidelines
  - CHANGELOG.md (this file)
  - .gitattributes for line ending handling

### Fixed
- **File Locking Issues**
  - Fixed IOException when AI tries to read file during upload
  - Ensured FileStream fully closed before AI analysis
  - Added retry logic with FileShare.Read in PhotoIntelligenceService
- **Database Initialization**
  - Fixed deadlock caused by GetAwaiter().GetResult() in Program.cs
  - Moved InitAsync to after app.Build() with proper await
  - Added initialization for IntelligentPhotoDatabase
- **Photo Management**
  - Duplicate photo entries on app restart (Contains check before adding)
  - Multi-file upload issues with InputFile component
  - Nullability warnings in photograph resolution (Where filter for nulls)
  - Async warning by making SetActiveCategoryAsync properly async

### Changed
- **AI Provider Architecture**
  - Migrated from OpenAI to Azure OpenAI
  - Replaced API key authentication with Managed Identity (DefaultAzureCredential)
  - Created IAIClientProvider interface for abstraction
  - Implemented AzureOpenAIClientProvider with Azure.AI.OpenAI SDK
  - Custom adapter (OpenAIChatClientAdapter) bridges OpenAI SDK to Microsoft.Extensions.AI
- **Project Structure**
  - Added ArtistTool.Intelligence project for AI capabilities
  - Separated concerns: Domain, Services, Intelligence layers
  - Enhanced dependency injection setup in Program.cs

### Security
- **Azure Managed Identity**
  - No API keys or secrets stored in code or configuration
  - Automatic credential rotation by Azure
  - Centralized access management via Azure RBAC
  - Audit trail in Azure AD logs
  - Works across environments (dev, staging, prod)
  - Supports local development via Azure CLI
- **File Security**
  - Content-type validation for uploaded images
  - File extension whitelist for uploads (jpg, jpeg, png, gif, tif, tiff)
  - Atomic write operations to prevent corruption
  - Proper file handle management to prevent locking

### Performance
- Vision analysis: ~2-5 seconds per image
- Title generation: ~1 second per photo
- Tag/category generation: ~2 seconds per photo
- Canvas visualization generation: ~3-5 seconds per image
- Marketing content generation: ~2-3 seconds per photo
- Total AI enrichment time: 5-10 seconds per photo (upload)
- Total marketing preview generation: 8-13 seconds per photo
- Optimized file operations with proper disposal patterns

## [0.1.0] - 2025-01-XX

### Added
- Initial project structure with .NET 10
- Core domain models (Photograph, Category, Tag)
- Basic CRUD operations
- File-based persistence layer (PersistentPhotoDatabase)
- Image management service
- Blazor Server with Interactive rendering
- Photo upload functionality (multiple files, up to 20MB each)
- Category management system
- Tag support for photos
- Styled UI components with CSS isolation
- .NET Aspire support

[Unreleased]: https://github.com/JeremyLikness/ArtistTool/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/JeremyLikness/ArtistTool/releases/tag/v0.1.0
