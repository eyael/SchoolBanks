
using CsvHelper.Configuration;
using SchoolBanks.Web.Pages.Students; // for StudentImportDto
using SchoolBanks.Data;               // for AppDbContext
using Microsoft.EntityFrameworkCore;

public class StudentImportMap : ClassMap<StudentImportDto>
{
    private readonly AppDbContext _db;

    public StudentImportMap(AppDbContext db)
    {
        _db = db;

        Map(m => m.StudentNumber).Name("StudentNumber");
        Map(m => m.FirstName).Name("FirstName");
        Map(m => m.LastName).Name("LastName");
        Map(m => m.Grade).Name("Grade");
        Map(m => m.GraduationYear).Name("GraduationYear");

        // UserId is resolved by first+last name dynamically
        Map(m => m.UserId).Convert(row =>
        {
            var first = row.Row.GetField("FirstName");
            var last = row.Row.GetField("LastName");
            var user = _db.Users.FirstOrDefault(u => u.FirstName == first && u.LastName == last);
            return user?.UserId ?? 0; // 0 means user not found
        });
    }
}