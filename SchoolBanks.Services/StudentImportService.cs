using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SchoolBanks.Data;
using Microsoft.Extensions.Logging;
using SchoolBanks.Core;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;


namespace SchoolBanks.Services
{
 public class StudentImportService : IStudentImportService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<StudentImportService> _log;
        public StudentImportService(AppDbContext db, ILogger<StudentImportService> log)
        {
            _db = db; _log = log;
        }

        public async Task<ImportResult> ImportFromStreamAsync(Stream csvStream, string fileName, CancellationToken ct = default)
        {
            var result = new ImportResult();

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                IgnoreBlankLines = true,
                TrimOptions = TrimOptions.Trim,
            };

            using var reader = new StreamReader(csvStream);
            using var csv = new CsvReader(reader, config);

            await csv.ReadAsync();
            csv.ReadHeader();// skip header (spec requires header skip)
            var row = 1; // header row number
                         // load existing students by StudentNumber to upsert
            var existing = await _db.Students.Include(s => s.User)
                                    .ToDictionaryAsync(s => s.StudentNumber, StringComparer.OrdinalIgnoreCase, ct);

            var toAdd = new List<Student>();
            var toUpdate = new List<Student>();

            while (await csv.ReadAsync())
            {
                row++;
                var studentNumber = csv.GetField("StudentNumber")?.Trim();
                var firstName = csv.GetField("FirstName")?.Trim();
                var lastName = csv.GetField("LastName")?.Trim();
                var statusText = csv.GetField("Status")?.Trim();
                var sisid = csv.TryGetField("SISID", out string? s) ? s?.Trim() : null;
                var gradeText = csv.TryGetField("Grade", out string? g) ? g?.Trim() : null;
                var gradYearText = csv.TryGetField("GraduationYear", out string? gy) ? gy?.Trim() : null;

                // validation
                if (string.IsNullOrWhiteSpace(studentNumber))
                {
                    result.Failed++;
                    result.ErrorsPreview.Add((row, "Missing StudentNumber"));
                    continue;
                }
                if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
                {
                    result.Failed++;
                    result.ErrorsPreview.Add((row, "Missing FirstName or LastName"));
                    continue;
                }
                if (!Enum.TryParse<UserStatus>(statusText, true, out var status))
                {
                    result.Failed++;
                    result.ErrorsPreview.Add((row, "Invalid Status (must be Active or Inactive)"));
                    continue;
                }

                if (existing.TryGetValue(studentNumber, out var existingStudent))
                {
                    // update
                    existingStudent.Grade = int.TryParse(gradeText, out var gr) ? gr : (int?)null;
                    existingStudent.GraduationYear = int.TryParse(gradYearText, out var gyv) ? gyv : (int?)null;
                    existingStudent.SISID = string.IsNullOrWhiteSpace(sisid) ? existingStudent.SISID : sisid;
                    existingStudent.User!.FirstName = firstName;
                    existingStudent.User.LastName = lastName;
                    existingStudent.User.Status = status;
                    toUpdate.Add(existingStudent);
                }
                else
                {
                    var user = new User { FirstName = firstName, LastName = lastName, Status = status };
                    var student = new Student
                    {
                        StudentNumber = studentNumber,
                        SISID = string.IsNullOrWhiteSpace(sisid) ? null : sisid,
                        Grade = int.TryParse(gradeText, out var gr) ? gr : (int?)null,
                        GraduationYear = int.TryParse(gradYearText, out var gyv) ? gyv : (int?)null,
                        User = user
                    };
                    toAdd.Add(student);
                    // add to existing dictionary so duplicates in CSV update the same instance
                    existing[studentNumber] = student;
                }
            }

            if (toAdd.Any())
            {
                _db.Students.AddRange(toAdd);
            }
            // updates are already tracked; save once to get StudentIds
            await _db.SaveChangesAsync(ct);

            // generate SISIDs for any students still with blank SISID
            var studentsNeedingSISID = await _db.Students.Where(s => string.IsNullOrEmpty(s.SISID)).ToListAsync(ct);
            foreach (var s in studentsNeedingSISID)
            {
                // generate: SB + left-padded zeros + numeric StudentId so total length is >= 10
                s.SISID = "SB" + s.StudentId.ToString().PadLeft(8, '0'); // SB + 8 digits => length 10
            }
            await _db.SaveChangesAsync(ct);

            result.Inserted = toAdd.Count;
            result.Updated = toUpdate.Count;
            return result;
        }
    }
}