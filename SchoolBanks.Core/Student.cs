using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolBanks.Core
{
    public class Student
    {
        public int StudentId { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public string StudentNumber { get; set; } = "";
        public string? SISID { get; set; }           // may be generated
        public int? Grade { get; set; }
        public int? GraduationYear { get; set; }
    }

    public class StudentListDto
    {
        public string? StudentNumber { get; set; }
        public string? Name { get; set; }
        public int? Grade { get; set; }
        public int? GraduationYear { get; set; }
        public string? Status { get; set; }
        public User? User { get; set; }
    }
}