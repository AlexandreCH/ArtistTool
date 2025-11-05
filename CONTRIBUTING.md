# Contributing to ArtistTool

Thank you for your interest in contributing to ArtistTool! This document provides guidelines for contributing to the project.

## Code of Conduct

Be respectful and constructive in all interactions.

## How to Contribute

### Reporting Bugs

1. Check if the bug has already been reported in [Issues](../../issues)
2. If not, create a new issue with:
   - Clear title and description
   - Steps to reproduce
   - Expected vs actual behavior
   - Screenshots if applicable
   - Environment details (.NET version, OS)

### Suggesting Features

1. Check existing [Issues](../../issues) for similar suggestions
2. Create a new issue with:
   - Clear description of the feature
   - Use cases and benefits
   - Possible implementation approach

### Submitting Pull Requests

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes
4. Follow the coding standards below
5. Add tests if applicable
6. Commit your changes (`git commit -m 'Add amazing feature'`)
7. Push to your branch (`git push origin feature/amazing-feature`)
8. Open a Pull Request

## Coding Standards

### C# Style

- Use C# 14 features where appropriate
- Follow [Microsoft C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use meaningful variable and method names
- Add XML documentation comments for public APIs
- Keep methods focused and single-purpose

### Blazor Components

- Use meaningful component names
- Separate concerns (markup, code, styles)
- Use `@rendermode` appropriately
- Follow Blazor best practices

### Logging

- Use structured logging with `ILogger<T>`
- Use appropriate log levels:
  - `Trace`: Very detailed, frequent operations
  - `Debug`: Debugging information
  - `Information`: General flow events
  - `Warning`: Unexpected but recoverable
  - `Error`: Failures and exceptions
- Include relevant context in log messages

### Database Operations

- Always use mutex protection for shared state
- Implement proper disposal patterns
- Use atomic file operations
- Handle exceptions appropriately

## Testing

- Add unit tests for new features
- Ensure all tests pass before submitting PR
- Maintain or improve code coverage

## Documentation

- Update README.md for new features
- Add inline code comments for complex logic
- Update API documentation as needed

## License

By contributing, you agree that your contributions will be licensed under the MIT License.
