namespace RMS.Identity.Service.Domain.Entities
{
    public class Role
    {
        public long RoleID { get; set; }
        public Guid RoleUUID { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsSystemRole { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}