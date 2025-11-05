# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Initial release of ArtistTool
- Photo upload functionality (multiple files, up to 20MB each)
- Category management system
- Tag support for photos
- Photo editing (title, description, categories, tags)
- Full-screen photo viewer
- Persistent JSON-based storage
- Comprehensive logging throughout application
- Thread-safe database operations with mutex protection
- Atomic file write operations
- Blazor Server with Interactive rendering
- Styled UI components with CSS isolation
- Photo grid display with hover effects
- .NET Aspire support

### Fixed
- Duplicate photo entries on app restart
- Multi-file upload issues with InputFile
- Deadlock in database initialization
- Nullability warnings in photograph resolution

### Security
- Content-type validation for uploaded images
- File extension whitelist for uploads
- Atomic write operations to prevent corruption

## [0.1.0] - 2025-01-XX

### Added
- Initial project structure
- Core domain models (Photograph, Category, Tag)
- Basic CRUD operations
- File-based persistence layer
- Image management service

[Unreleased]: https://github.com/username/ArtistTool/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/username/ArtistTool/releases/tag/v0.1.0
