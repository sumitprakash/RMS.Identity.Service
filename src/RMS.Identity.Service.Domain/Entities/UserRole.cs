namespace RMS.Identity.Service.Domain.Entities;

public class UserRole
{
    public long UserRoleID { get; set; }

    public long UserID { get; set; }

    public long RoleID { get; set; }

    public DateTime AssignedAt { get; set; }

    public long? AssignedBy { get; set; }

    public Role? Role { get; set; }
}
