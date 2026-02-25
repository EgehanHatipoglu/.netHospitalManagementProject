# Skill: SQLite & EF Core Mastery

## Description
Expertise in managing local SQLite databases using Entity Framework Core within a .NET environment.

## Capabilities
- **Migration Management:** Generating and applying EF Core migrations (`dotnet ef migrations add`).
- **Query Optimization:** Writing efficient LINQ queries with `.AsNoTracking()` for read-only operations.
- **Error Handling:** Handling SQLite-specific constraints (Unique, Foreign Key) gracefully.
- **Data Seeding:** Creating initial seed data for Hospital settings (Default Admin, Sample Departments).

## Reference Snippet
When creating a Repository, use this pattern:
```csharp
public async Task<IEnumerable<T>> GetAllAsync() {
    return await _context.Set<T>().AsNoTracking().ToListAsync();
}
```
