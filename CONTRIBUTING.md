# Contributing to ScreenGrid

Thanks for your interest in contributing! Here's how to get started.

## Development Setup

1. Install [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
2. Clone the repo and open in VS Code or Visual Studio
3. Run with `dotnet run -c Release`

## Making Changes

1. **Fork** the repository
2. Create a **feature branch** from `main`: `git checkout -b my-feature`
3. Make your changes
4. **Run tests**: `dotnet test -c Release` — all tests must pass
5. **Test manually** by running the app and verifying the overlay, grid editor, and snapping work
6. **Commit** with a clear message describing the change
7. **Push** and open a **Pull Request**

## Running Tests

The test suite uses [xUnit](https://xunit.net/) and lives in the `tests/` directory.

```bash
# Run all tests
dotnet test -c Release

# Run tests with detailed output
dotnet test -c Release --verbosity normal

# Run a specific test class
dotnet test -c Release --filter "FullyQualifiedName~GridConfigTests"
```

Tests cover:
- **GridRowDef** — column label generation for various ratio configurations
- **GridConfig** — default creation, JSON save/load roundtrip, error handling, constants
- **GridZone** — default property values, zone bounds for common screen splits

When adding new features, please include corresponding tests. CI runs `dotnet test` automatically on every push and pull request.

## What to Contribute

- **New grid presets** — add commonly useful layouts
- **Bug fixes** — especially around DPI, multi-monitor, or edge cases
- **UI improvements** — better overlay visuals, editor polish
- **Features** — keyboard shortcuts, multi-monitor zone spanning, etc.
- **Documentation** — screenshots, GIFs, better explanations

## Code Style

- Follow existing conventions in the codebase
- Use meaningful names; keep methods focused
- Add XML doc comments on public/internal APIs
- Test on both standard (1920×1080) and ultrawide (5120×1440) if possible

## Reporting Issues

Open a GitHub Issue with:
- Your screen resolution and DPI scaling
- Steps to reproduce
- Expected vs actual behavior
- Windows version

## License

By contributing, you agree that your contributions will be licensed under the [MIT License](LICENSE).
