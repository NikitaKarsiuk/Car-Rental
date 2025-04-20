using Microsoft.AspNetCore.Identity;

namespace CarRental.Models
{
    public class Role : IdentityRole<int>
    {
        public List<User> Users { get; set; } = new List<User>();
    }
}
