using Microsoft.AspNetCore.Identity;

namespace CarRental.Models
{
    public class User : IdentityUser<int>
    {
        public int RoleId { get; set; }
        public Role Role { get; set; }
    }
}
