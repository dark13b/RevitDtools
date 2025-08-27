# Contributing to RevitDtools

Thank you for your interest in contributing to RevitDtools! This document provides guidelines and information for contributors.

## 🚀 Getting Started

### Prerequisites
- Visual Studio 2022 or VS Code with C# extension
- Autodesk Revit 2026 (for testing)
- .NET 8.0 SDK
- Git

### Development Environment Setup
1. Fork the repository
2. Clone your fork locally:
   ```bash
   git clone https://github.com/yourusername/RevitDtools.git
   cd RevitDtools
   ```
3. Create a new branch for your feature:
   ```bash
   git checkout -b feature/your-feature-name
   ```
4. Build the project:
   ```bash
   dotnet build
   ```

## 📝 Code Guidelines

### C# Coding Standards
- Follow Microsoft C# coding conventions
- Use meaningful names for variables, methods, and classes
- Add XML documentation for public APIs
- Keep methods focused and single-purpose
- Use async/await for I/O operations

### Project Structure
```
RevitDtools/
├── Core/
│   ├── Commands/          # Revit external commands
│   ├── Services/          # Business logic services
│   ├── Models/            # Data models
│   └── Interfaces/        # Service contracts
├── UI/
│   └── Windows/           # WPF user interfaces
├── Utilities/             # Helper classes and utilities
└── Tests/                 # Unit and integration tests
```

### Naming Conventions
- **Commands**: End with `Command` (e.g., `BatchColumnByLineCommand`)
- **Services**: End with `Service` (e.g., `FamilyManagementService`)
- **Interfaces**: Start with `I` (e.g., `IFamilyManager`)
- **Models**: Descriptive nouns (e.g., `ColumnParameters`)

## 🔧 Development Workflow

### Adding New Features
1. Create an issue describing the feature
2. Create a feature branch from `main`
3. Implement the feature with tests
4. Update documentation if needed
5. Submit a pull request

### Bug Fixes
1. Create an issue describing the bug
2. Create a bugfix branch from `main`
3. Fix the issue with appropriate tests
4. Submit a pull request

### Testing
- Add unit tests for new functionality
- Test with actual Revit projects when possible
- Ensure all existing tests pass
- Test on different Revit project types (architectural, structural, MEP)

## 📋 Pull Request Process

### Before Submitting
- [ ] Code builds without errors or warnings
- [ ] All tests pass
- [ ] Code follows project conventions
- [ ] Documentation is updated
- [ ] Commit messages are clear and descriptive

### PR Template
```markdown
## Description
Brief description of changes

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation update

## Testing
- [ ] Unit tests added/updated
- [ ] Manual testing completed
- [ ] Tested in Revit environment

## Checklist
- [ ] Code follows style guidelines
- [ ] Self-review completed
- [ ] Documentation updated
- [ ] No new warnings introduced
```

## 🐛 Reporting Issues

### Bug Reports
Include the following information:
- Revit version and build number
- Operating system
- Steps to reproduce
- Expected vs actual behavior
- Screenshots or error messages
- Sample files if applicable

### Feature Requests
- Clear description of the proposed feature
- Use case and benefits
- Mockups or examples if applicable
- Implementation suggestions (optional)

## 🏗️ Architecture Guidelines

### Transaction Management
- Always use proper transaction scoping
- Avoid nested transactions
- Handle transaction failures gracefully
- Use meaningful transaction names

### Error Handling
- Use structured logging with context
- Provide user-friendly error messages
- Implement graceful degradation
- Log errors with sufficient detail for debugging

### Performance
- Minimize Revit API calls in loops
- Use bulk operations when available
- Cache frequently accessed data
- Profile performance-critical code

## 📚 Resources

### Revit API Documentation
- [Revit API Developer Guide](https://www.revitapidocs.com/)
- [Autodesk Developer Network](https://www.autodesk.com/developer-network)
- [Revit API Forum](https://forums.autodesk.com/t5/revit-api-forum/bd-p/160)

### .NET Resources
- [C# Programming Guide](https://docs.microsoft.com/en-us/dotnet/csharp/)
- [.NET API Browser](https://docs.microsoft.com/en-us/dotnet/api/)

## 🤝 Community

### Communication
- Use GitHub Issues for bug reports and feature requests
- Use GitHub Discussions for questions and general discussion
- Be respectful and constructive in all interactions
- Help others when you can

### Code of Conduct
- Be inclusive and welcoming
- Respect different viewpoints and experiences
- Focus on what's best for the community
- Show empathy towards other community members

## 📄 License

By contributing to RevitDtools, you agree that your contributions will be licensed under the MIT License.

---

Thank you for contributing to RevitDtools! 🎉