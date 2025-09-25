using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchoolBanks.Data;
using SchoolBanks.Core;

namespace SchoolBanks.Web.Pages.Students
{
    public class StudentListDto
    {
        public string StudentNumber { get; set; } = "";
        public string Name { get; set; } = "";
        public int? Grade { get; set; }
        public string Status { get; set; } = "";
        public int? GraduationYear { get; set; }
    }

    public class StudentImportDto
    {
        public string StudentNumber { get; set; } = "";
        public int UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Grade { get; set; }
        public string GraduationYear { get; set; }
        public string Status { get; set; } = "";
    }

    public class IndexModel : PageModel
    {
        private readonly AppDbContext _db;
        public IndexModel(AppDbContext db) => _db = db;

        [BindProperty(SupportsGet = true)] public string? Name { get; set; }
        [BindProperty(SupportsGet = true)] public int? Grade { get; set; }
        [BindProperty(SupportsGet = true)] public string? Status { get; set; }

        public List<StudentListDto> Students { get; set; } = new();

        // For file upload
        [BindProperty] public IFormFile? UploadFile { get; set; }

        public async Task OnGetAsync()
        {
            var q = _db.Students.Include(s => s.User).AsQueryable();

            if (!string.IsNullOrWhiteSpace(Name))
            {
                var n = Name.Trim().ToLower();
                q = q.Where(s => s.User!.FirstName.ToLower().Contains(n)
                              || s.User.LastName.ToLower().Contains(n));
            }

            if (Grade.HasValue)
                q = q.Where(s => s.Grade == Grade.Value);

            if (!string.IsNullOrWhiteSpace(Status) && Enum.TryParse<UserStatus>(Status, true, out var st))
                q = q.Where(s => s.User!.Status == st);

            Students = await q
                .OrderBy(s => s.StudentNumber)
                .Select(s => new StudentListDto
                {
                    StudentNumber = s.StudentNumber,
                    Name = s.User!.FirstName + " " + s.User.LastName,
                    Grade = s.Grade,
                    GraduationYear = s.GraduationYear,
                    Status = s.User.Status.ToString()
                })
                .ToListAsync();
        }

        public async Task<FileResult> OnGetExportAsync()
        {
            await OnGetAsync();

            using var ms = new MemoryStream();
            using var writer = new StreamWriter(ms, leaveOpen: true);
            var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture) { };
            using var csv = new CsvWriter(writer, csvConfig);

            csv.WriteField("StudentNumber");
            csv.WriteField("Name");
            csv.WriteField("Grade");
            csv.WriteField("Status");
            csv.WriteField("GraduationYear");
            csv.NextRecord();

            foreach (var s in Students)
            {
                csv.WriteField(s.StudentNumber);
                csv.WriteField(s.Name);
                csv.WriteField(s.Grade?.ToString() ?? "");
                csv.WriteField(s.Status);
                csv.WriteField(s.GraduationYear?.ToString() ?? "");
                csv.NextRecord();
            }

            writer.Flush();
            ms.Position = 0;
            return File(ms.ToArray(), "text/csv", "students_export.csv");
        }

        [ValidateAntiForgeryToken]
       
        public async Task<IActionResult> OnPostImportAsync()
        {
            if (UploadFile == null || UploadFile.Length == 0)
            {
                ModelState.AddModelError("", "Please select a CSV file.");
                await OnGetAsync();
                return Page();
            }

            using var stream = UploadFile.OpenReadStream();
            using var reader = new StreamReader(stream);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            //csv.Configuration.MissingFieldFound = null;
            csv.Context.RegisterClassMap(new StudentImportMap(_db)); // pass DbContext
                                                                     // Optional: ignore missing headers to prevent exceptions

            var records = csv.GetRecords<StudentImportDto>().ToList();

            foreach (var row in records)
            {
                string studentNumber = row.StudentNumber;
                string firstName = row.FirstName;
                string lastName = row.LastName;
                string? statusStr = row.Status;
                int? grade = string.IsNullOrWhiteSpace(row.Grade) ? null : int.Parse(row.Grade);
                int? graduationYear = string.IsNullOrWhiteSpace(row.GraduationYear) ? null : int.Parse(row.GraduationYear);

                // Find an existing student by StudentNumber
                var student = await _db.Students
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.StudentNumber == studentNumber);

                if (student != null)
                {
                    // Update existing student
                    student.Grade = grade;
                    student.GraduationYear = graduationYear;

                    // Optionally update linked user names if they changed
                    if (student.User != null)
                    {
                        student.User.FirstName = firstName;
                        student.User.LastName = lastName;
                        if (!string.IsNullOrWhiteSpace(statusStr) &&
                            Enum.TryParse<UserStatus>(statusStr, true, out var st))
                        {
                            student.User.Status = st;
                        }
                    }
                }
                else
                {
                    // Find user by name (in case we want to link existing user)
                    var user = await _db.Users
                        .FirstOrDefaultAsync(u => u.FirstName == firstName && u.LastName == lastName);

                    if (user != null)
                    {
                        // Create new student record for this user
                        var newStudent = new Student
                        {
                            StudentNumber = studentNumber,
                            User = user,
                            Grade = grade,
                            GraduationYear = graduationYear
                        };
                        _db.Students.Add(newStudent);
                    }
                    // else: user not found -> skip or log
                }
            }

            await _db.SaveChangesAsync();
            return RedirectToPage();


            //foreach (var row in records)
            //{
            //    string studentNumber = row.StudentNumber;
            //    string firstName = row.FirstName;
            //    string lastName = row.LastName;
            //    string? statusStr = row.Status;
            //    int? grade = string.IsNullOrWhiteSpace(row.Grade) ? null : int.Parse(row.Grade);
            //    int? graduationYear = string.IsNullOrWhiteSpace(row.GraduationYear) ? null : int.Parse(row.GraduationYear);

            //    // Find the user by name

            //    var user = await _db.Users.FirstOrDefaultAsync(u => u.FirstName == firstName && u.LastName == lastName);
            //    if (user != null)
            //    {
            //        if (user.Students.Count != 0)
            //        {
            //            // Update existing student
            //            user.Students.First().StudentNumber = studentNumber;
            //            user.Students.First().Grade = grade;
            //            user.Students.First().GraduationYear = graduationYear;
            //        }
            //        else
            //        {
            //            // Insert new student
            //            var student = new Student
            //            {
            //                StudentNumber = studentNumber,
            //                User = user,
            //                Grade = grade,
            //                GraduationYear = graduationYear
            //            };
            //            _db.Students.Add(student);
            //        }
            //    }
            //    else
            //    {
            //        // Optional: create a new user if not found
            //        var newUser = new User
            //        {
            //            FirstName = firstName,
            //            LastName = lastName,
            //            Status = Enum.TryParse<UserStatus>(statusStr, true, out var st) ? st : UserStatus.Active
            //        };

            //        var student = new Student
            //        {
            //            StudentNumber = studentNumber,
            //            User = newUser,
            //            Grade = grade,
            //            GraduationYear = graduationYear
            //        };

            //        _db.Users.Add(newUser);
            //        _db.Students.Add(student);
            //    }
            //}

            //await _db.SaveChangesAsync();
            //return RedirectToPage();

        }

    }
}
