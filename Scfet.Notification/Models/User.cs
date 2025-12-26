using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scfet.Notification.Models
{
    public class User
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? Group { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;

        public string FullName => $"{FirstName} {LastName}";
    }

    public enum UserRole
    {
        Student = 1,
        Teacher = 2,
        Administrator = 3
    }
}
