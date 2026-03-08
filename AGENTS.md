# Agent Instructions for MtG Collection Tracker

## Overview
The repository contains a cross‑platform MTG collection manager built with .NET 8.0 and Avalonia. The code is organized into several projects:

- `MtgCollectionTracker` - Avalonia Views, ViewModels and app services.
- `MtgCollectionTracker.Core` – domain logic, services, and data models.
- `MtgCollectionTracker.Data` – EF Core DbContext and migrations.
- `MtgCollectionTracker.Desktop`, `.Android`, `.iOS`, `.Browser` – UI front‑ends.
- `MtgCollectionTracker.Console` – CLI utilities.

All code is written in **C# 12** (preview) using nullable reference types. The repository uses **dotnet format**, **stylecop.json**, and **editorconfig** for formatting, but the CI workflow does not run a dedicated linter step; instead `dotnet build` will surface most style warnings.

---

## Build / Lint / Test Commands

| Command | Purpose | Notes |
|---------|---------|-------|
| `dotnet restore` | Restore NuGet packages. | Run once per clone or before a build. |
| `dotnet format --verify-no-changes` | Verify formatting matches editorconfig. | Fails if any file is out of sync – useful for pre‑commit hooks. |
| `dotnet build -c Release` | Compile the entire solution. | Generates binaries in `bin/Release`. |
| `dotnet test` | Run all unit tests. | Uses `--no-build` to skip rebuilding if already built. |
| `dotnet test --filter "FullyQualifiedName~Namespace.Class.TestMethod"` | Run a single test or a subset. | Replace the filter with the desired fully‑qualified name. |
| `dotnet ef migrations script -o migrations.sql` | Generate SQL migration scripts for deployment. | Requires `Microsoft.EntityFrameworkCore.Tools`. |

### CI Workflow
The GitHub Actions workflow (`.github/workflows/dotnet.yml`) performs the following steps on each push to `master`:
1. Restores packages.
2. Builds with `--no-restore`.
3. Runs tests with normal verbosity.
4. Publishes binaries for Windows, Linux, macOS, and AppImage using **PupNet**.
5. Uploads artifacts and creates a draft release when a tag is pushed.

If you need to run only the tests for `MtgCollectionTracker.Core`, use:
```
dotnet test src/MtgCollectionTracker.Core/tests --no-build
```
Replace the path with any project containing tests.

---

## Code Style Guidelines

1. **Formatting** – Use `dotnet format` or Visual Studio’s formatting rules. All files should follow the editorconfig in the repo root:
   - Indentation: 4 spaces, no tabs.
   - Line endings: LF.
   - File header: optional comment block with file purpose.
2. **Imports** – Order namespaces alphabetically and group them into system/standard first, then third‑party, then project namespaces. Empty line between groups.
3. **Naming Conventions**
   - Public types (classes, structs, enums): PascalCase.
   - Private fields: `_camelCase` with underscore prefix.
   - Parameters and local variables: camelCase.
   - Constants: `PascalCase` with `const` keyword; static readonly fields use PascalCase.
   - Methods: PascalCase. Extension methods should be in a static class named `XxxExtensions`.
4. **Nullability** – Enable nullable reference types (`#nullable enable`). All public APIs must explicitly document nullability via XML comments or `?` annotations.
5. **Error Handling**
   - Prefer `ArgumentException`, `ArgumentNullException`, and `InvalidOperationException` for argument validation.
   - Do not swallow exceptions; log them using the injected `ILogger<T>` and rethrow if critical.
   - Use `try/catch` only when you can recover or provide a meaningful fallback.
6. **Async/Await** – All IO-bound operations should be asynchronous. Avoid `.Result` or `.Wait()` to prevent deadlocks.
7. **Dependency Injection** – Register services in `Program.cs` using the built‑in DI container. Prefer constructor injection and keep classes loosely coupled.
8. **Unit Tests** – Use xUnit with the `[Fact]` attribute. Arrange/Act/Assert pattern. Mock dependencies via Moq or NSubstitute.
9. **Logging** – Use Microsoft.Extensions.Logging. Do not log sensitive data such as API keys.
10. **Documentation** – XML comments for public APIs. Use `<summary>`, `<param>`, `<returns>` tags. Keep summaries concise (1‑2 sentences).

---

## Repository Specific Rules

- The `MtgCollectionTracker.Core` project is the single source of truth for business logic. All UI projects should consume it via dependency injection.
- Database migrations are managed through EF Core. Do not edit the migration files manually; generate new ones with `dotnet ef migrations add <Name>`.
- The `ScryfallMetadataResolver` must always prefer paper editions when resolving card data. If a non‑paper set is returned, search for a paper printing and fall back only if none exist.

---

## Copilot / Cursor Rules
No explicit `.github/copilot-instructions.md`, `.cursor/rules/`, or `.cursorrules` files are present in this repository. Agents should rely solely on the guidelines above.
