namespace Apkaweb.Models
{
    public class Permissions
    {
        public int PermissionId { get; set; }
        public string OwnerUsername { get; set; }
        public string TargetUsername { get; set; }
    }
}