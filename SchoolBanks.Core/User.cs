using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolBanks.Core
{
    public enum UserStatus { Active, Inactive }

    public class User
    {
        public int UserId { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public UserStatus Status { get; set; } = UserStatus.Active;
        public ICollection<Student> Students { get; set; } = new List<Student>();
    }

}