using Microsoft.AspNetCore.Identity;

namespace FinanceAPI.Models
{
    public class ApplicationUser : IdentityUser
    {
        //For local authentication (the undeclared fields such as Id, PasswordHash and Email are in IdentityUser class)
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = String.Empty;
        public string? JWT { get; set; }


        //For Google authentication:
        public string? GoogleId { get; set; }
        public string? GoogleEmail {  get; set; }
        public string? GoogleAccessToken { get; set; }
        public string? GoogleRefreshToken { get; set; }

        //Additional fields:
        public string? ProfilePictureUrl { get; set; }

    }
}
