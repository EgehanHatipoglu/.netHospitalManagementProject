# UI/UX & Frontend Standards for Avalonia HMS

## 1. Design System & Theme
- **Base Theme:** Always use `FluentTheme` (Windows 11 style) or `Semi.Avalonia` for a modern, high-end look.
- **Color Palette (Medical Trust):**
  - **Primary:** `#2563eb` (Medical Blue)
  - **Success:** `#16a34a` (Safe/Healthy)
  - **Warning:** `#ea580c` (Urgent/Attention)
  - **Background:** `#f8fafc` (Light Gray/White for cleanliness)
- **Corner Radius:** Use a consistent `8px` or `12px` for buttons, cards, and input fields to give a modern feel.

## 2. Layout & Navigation
- **Sidebar:** Implement a persistent left-side navigation with icons for "Dashboard, Patients, Appointments, Billing, Settings".
- **Spacing:** Follow the **8pt Grid System**. All margins and paddings should be multiples of 8 (8, 16, 24, 32).
- **Header:** Every page must have a clear Title and a Breadcrumb (e.g., Patients > Add New Patient).

## 3. Component Guidelines
- **DataGrids:** - Use alternating row colors for readability.
  - Headers must be bold and sticky.
  - Include a "Status Badge" (e.g., Rounded border for "Active", "Discharged", "Critical").
- **Forms:** - Use `Watermark` in TextBoxes.
  - Group related fields in `Expander` or `Border` cards with subtle box-shadows.
- **Icons:** Use the `Lucide.Avalonia` or `Material.Icons.Avalonia` library. Never use plain text buttons where an icon + text can be used.

## 4. Interaction & UX
- **Feedback:** Show a `ProgressBar` or `Spinner` during database operations (SQLite async calls).
- **Dialogs:** Use modern ContentDialogs or Overlay Drawers instead of pop-up Windows for simple inputs.
- **Empty States:** If a search returns no patients, show a "No patients found" illustration instead of an empty grid.

## 5. XAML Best Practices
- **Styles:** Define colors and font sizes in `App.axaml` as Resources. Do not hardcode hex codes in Views.
- **Responsiveness:** Use `Grid` with `*` and `Auto` definitions. Ensure the app looks good at 1280x720 and 1920x1080.
