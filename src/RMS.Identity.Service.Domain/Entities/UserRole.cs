namespace RMS.Identity.Service.Domain.Entities
{
    public class UserRole
    {
        public long UserRoleID { get; set; }
        public long CompanyID { get; set; }
        public long UserID { get; set; }
        public long RoleID { get; set; }
        public DateTime AssignedAt { get; set; }
        public long? AssignedBy { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public long? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public long? UpdatedBy { get; set; }
        public Role? Role { get; set; }
    }
}
