# 📖 Student Management (ASP.NET Core Razor Pages)

This module provides a **student management page** with the ability to:

- ✅ List students with **search + filtering**  
- ✅ Import students from **CSV files** (update if existing, insert if new)  
- ✅ Export **filtered student data** to CSV  

---

## 🚀 Features

1. **Student List**  
   - Displays students with name, grade, status, graduation year  
   - Supports filtering by name, grade, and status  

2. **CSV Import**  
   - Reads a CSV file with columns:  
     ```
     StudentNumber,FirstName,LastName,Status,SISID,Grade,GraduationYear
     ```  
   - If `StudentNumber` already exists → updates the existing student  
   - If `StudentNumber` does not exist → creates a new student (linked to existing user if found)  

3. **CSV Export**  
   - Exports **only the filtered data** to CSV  
   - Uses the same filters from the page (name, grade, status)  

---

## 🛠 Requirements

- .NET 8 (or .NET 7/6 with minimal adjustments)
- Packages:
  - `Microsoft.EntityFrameworkCore`
  - `Microsoft.EntityFrameworkCore.SqlServer` (or `Sqlite` depending on your DB)
  - `CsvHelper`

---

## ⚙️ Setup

1. **DbContext Configuration**

In `Program.cs`:

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddRazorPages();
```

2. **Middleware Configuration**

Add `UseAntiforgery()` between `UseRouting()` and `MapRazorPages()`:

```csharp
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorPages();
```

3. **Razor Page Binding**

`Index.cshtml.cs` uses `[BindProperty(SupportsGet = true)]` for filters:

```csharp
[BindProperty(SupportsGet = true)] public string? Name { get; set; }
[BindProperty(SupportsGet = true)] public int? Grade { get; set; }
[BindProperty(SupportsGet = true)] public string? Status { get; set; }
```

---

## 🧑‍💻 CSV Import Example

```csharp
foreach (var row in records)
{
    var student = await _db.Students
        .Include(s => s.User)
        .FirstOrDefaultAsync(s => s.StudentNumber == row.StudentNumber);

    if (student != null)
    {
        student.Grade = row.Grade;
        student.GraduationYear = row.GraduationYear;
    }
    else
    {
        var user = await _db.Users.FirstOrDefaultAsync(u =>
            u.FirstName == row.FirstName && u.LastName == row.LastName);

        if (user != null)
        {
            _db.Students.Add(new Student
            {
                StudentNumber = row.StudentNumber,
                User = user,
                Grade = row.Grade,
                GraduationYear = row.GraduationYear
            });
        }
    }
}
await _db.SaveChangesAsync();
```

---

## 📤 CSV Export Example

In `Index.cshtml`:

```html
<a asp-page-handler="Export"
   asp-route-Name="@Model.Name"
   asp-route-Grade="@Model.Grade"
   asp-route-Status="@Model.Status">
   Export CSV
</a>
```

In `Index.cshtml.cs`:

```csharp
public async Task<FileResult> OnGetExportAsync()
{
    await OnGetAsync(); // Uses filters to build Students list
    // Write CSV from Students collection
}
```

---

## ⚠️ Common Errors & Fixes

| Error                                                                                 | Fix                                                                                     |
|--------------------------------------------------------------------------------------|----------------------------------------------------------------------------------------|
| `AntiforgeryValidationException: The required antiforgery request token was not provided` | Ensure your `<form method="post">` includes `@Html.AntiForgeryToken()` and `app.UseAntiforgery()` is added. |
| `Cannot insert duplicate key row ... IX_Students_StudentNumber`                       | Check for existing `StudentNumber` before insert; update instead of inserting.          |
| `CsvHelper.HeaderValidationException`                                                | Set `csv.Configuration.MissingFieldFound = null;` to ignore missing optional headers.   |

---

## 📂 Example Project Structure

```
/Pages/Students/
   Index.cshtml
   Index.cshtml.cs
   StudentImportDto.cs
   StudentImportMap.cs
/Data/
   AppDbContext.cs
/Core/
   Student.cs
   User.cs
```

