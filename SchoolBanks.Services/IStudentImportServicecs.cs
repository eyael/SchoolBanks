using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolBanks.Services
{
    public class ImportResult
    {
        public int Inserted { get; set; }
        public int Updated { get; set; }
        public int Failed { get; set; }
        public List<(int Row, string Reason)> ErrorsPreview { get; set; } = new();
    }
    public interface IStudentImportService
    {
        Task<ImportResult> ImportFromStreamAsync(Stream csvStream, string fileName, CancellationToken ct = default);
    }
}
