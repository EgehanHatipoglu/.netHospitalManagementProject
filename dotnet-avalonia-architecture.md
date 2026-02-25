# .NET & Avalonia Architecture Rules

## Core Principles
- **Pattern:** Always use the MVVM (Model-View-ViewModel) pattern.
- **Framework:** Strictly use `CommunityToolkit.Mvvm` for ObservableObjects and RelayCommands.
- **Data Access:** Use the **Repository Pattern** for all database operations. No direct DB calls inside ViewModels.
- **Database:** SQLite with Entity Framework Core (EF Core).

## Coding Standards
- **Naming:** PascalCase for classes/methods, camelCase for private fields (with `_` prefix).
- **Dependency Injection:** Use Constructor Injection for Services and Repositories.
- **Avalonia UI:** Use Compiled Bindings (`x:DataType`) in XAML for better performance and compile-time checking.

## Restrictions
- Do not use `Code-Behind` (MainView.axaml.cs) for business logic.
- Avoid global static states; prefer Scoped or Singleton services via DI.
