# Repository Guidelines

## Project Structure & Module Organization
- Keep the product brief in project.md until a docs\ folder is added; future design notes belong under docs\.
- Place application code in src\ with domain folders: src\Player for playback, src\Library for queue management, src\UI for the Windows 11 shell, and src\Common for shared helpers.
- Store media, sample tracks, and artwork in assets\; reserve assets\sample for fixtures that can ship in Git.
- Put automated tests in tests\ mirroring the namespace tree so each module has a peer test folder.
- Configuration files such as settings.json or theme.json live in config\ and should stay environment agnostic.

## Build, Test, and Development Commands
- Restore dependencies with dotnet restore at the repository root to prime every project.
- Build the desktop app with dotnet build Karaoke.sln -c Release.
- Launch the debug experience via dotnet run --project src\UI\Karaoke.UI.csproj.
- Execute the full test suite with dotnet test --no-build.
- Format code before pushing by running dotnet format.

## Coding Style & Naming Conventions
- Target .NET 8, enable nullable reference types, and prefer async/await for I/O or file scans.
- Use four spaces, braces on new lines, PascalCase for classes and namespaces, camelCase for locals and parameters, and _camelCase for private fields.
- Prefix view models with ViewModel, services with Service, and data transfer objects with Dto.
- Keep XAML names in PascalCase and align resource keys with the related view to simplify binding.

## Testing Guidelines
- Write unit tests with xUnit; place them in files ending with Tests.cs using the Arrange-Act-Assert pattern.
- Aim for at least 80 percent line coverage on src\Player and src\Library where correctness is critical.
- Validate media path resolution with integration tests that read from assets\sample.
- Use dotnet test --collect:"XPlat Code Coverage" to generate coverage locally before a pull request.

## Commit & Pull Request Guidelines
- Follow Conventional Commits (feat:, fix:, chore:, docs:) so automated release notes stay consistent.
- Keep commits scoped to one concern and describe the user impact or bug reference in the body when relevant.
- Pull requests need a concise summary, linked issue or ticket, screenshots or GIFs for UI changes, and a checklist confirming tests, formatting, and config updates.
- Document new library folder expectations or hotkey changes in project.md whenever the UX shifts.

## Security & Configuration Tips
- Never commit personal music libraries; reference synthetic fixtures in assets\sample only.
- Store machine specific paths and API keys in config\local.settings.json, excluded through .gitignore.
- Sanitize imported song metadata before persisting to prevent script injection or markup mishaps in the UI.

