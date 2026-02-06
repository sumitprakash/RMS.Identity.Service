using System.Collections.Generic;
using System.Data;

namespace RMS.Identity.Service.Domain.Entities
{
    public class UserAccount
    {
        public long UserID { get; set; }
        public Guid UserUUID { get; set; }
        public long CompanyID { get; set; }
        public string Username { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public string? DisplayName { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public long? CreatedBy { get; set; }
        public List<Role> Roles { get; set; } = new List<Role>();
    }
}