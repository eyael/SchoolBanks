using CsvHelper;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.EntityFrameworkCore;
using SchoolBanks.Data;
using SchoolBanks.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IStudentImportService, StudentImportService>();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN"; // Optional: for AJAX
});

var app = builder.Build();
app.MapRazorPages();

// API endpoint that returns the JSON import summary
app.MapPost("/api/students/import", async (IFormFile file, IStudentImportService importService, ILogger<Program> logger) =>
{
    if (file == null || file.Length == 0)
        return Results.BadRequest(new { error = "file required" });

    try
    {
        using var s = file.OpenReadStream();
        var result = await importService.ImportFromStreamAsync(s, file.FileName);
        return Results.Json(result);
    }
    catch (CsvHelperException che)
    {
        logger.LogError(che, "CSV parse error: {FileName}", file.FileName);
        return Results.BadRequest(new { error = "CSV parse error", detail = che.Message });
    }
    catch (DbUpdateException dbex)
    {
        logger.LogError(dbex, "DB error importing {FileName}", file.FileName);
        return Results.BadRequest( new { error = "database error", detail = dbex.InnerException?.Message ?? dbex.Message });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Unexpected error importing {FileName}", file.FileName);
        return Results.BadRequest(new { error = "unexpected error", detail = ex.Message });
    }
});


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();



app.UseAuthentication();
app.UseAuthorization();
app.UseRouting();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
