---
description: Create a new module in Hospital Management System (Patient, Appointment, Staff)
---
# Workflow: /hms-module

**Trigger:** User wants to create a new module (e.g., Patient, Appointment, Staff).

**Step 1: Entity Definition**
- Create the Entity class in `Models/`.
- Add the necessary properties and EF Core attributes (`[Key]`, `[Required]`).
- Update `AppDbContext.cs` to include the new DbSet.

**Step 2: Data Access**
- Create an Interface `I[Module]Repository` in `Interfaces/`.
- Implement `[Module]Repository` in `Repositories/` using EF Core.

**Step 3: ViewModel Logic**
- Create `[Module]ViewModel.cs` inheriting from `ViewModelBase`.
- Inject the Repository via constructor.
- Implement Search, Add, and Delete commands using `[RelayCommand]`.

**Step 4: Avalonia View**
- Create `[Module]View.axaml` and its `.cs` file.
- Set `x:DataType` to the corresponding ViewModel.
- Use DataGrid or ListBox for data display with proper bindings.

**Step 5: Registration**
- Register the new Repository and ViewModel in the Dependency Injection container (usually in `App.axaml.cs` or `ServiceConfigurator.cs`).
