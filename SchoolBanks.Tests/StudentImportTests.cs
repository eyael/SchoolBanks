using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SchoolBanks.Data;
using SchoolBanks.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolBanks.Tests
{
    public class StudentImportTests
    {
        [Fact]
        public async Task Import_GeneratesSISID_WhenMissing()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"db_{Guid.NewGuid()}").Options;
            using var db = new AppDbContext(options);
            var logger = NullLogger<StudentImportService>.Instance; 
            var svc = new StudentImportService(db, logger);

            var csv = "StudentNumber,FirstName,LastName,Status\r\nA1001,Jordan,Lee,Active\r\n";
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(csv));
            var res = await svc.ImportFromStreamAsync(ms, "test.csv");
            Assert.Equal(1, res.Inserted);
            var student = await db.Students.SingleAsync(s => s.StudentNumber == "A1001");
            Assert.StartsWith("SB", student.SISID);
            Assert.True(student.SISID!.Length >= 10);
        }

        [Fact]
        public async Task Import_SkipsHeader_AndReportsMissingStudentNumber()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"db_{Guid.NewGuid()}").Options;
            using var db = new AppDbContext(options);
            var logger = NullLogger<StudentImportService>.Instance;
            var svc = new StudentImportService(db, logger);

            var csv = "StudentNumber,FirstName,LastName,Status\r\n,John,Doe,Active\r\n";
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(csv));
            var res = await svc.ImportFromStreamAsync(ms, "test.csv");
            Assert.Equal(0, res.Inserted);
            Assert.Equal(1, res.Failed);
            Assert.Contains(res.ErrorsPreview, e => e.Reason.Contains("Missing StudentNumber"));
        }
    }
}