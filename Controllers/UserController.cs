using FinanceAPI.Data;
using FinanceAPI.Models;
using FinanceAPI.Requests;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FinanceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly string _jwtSecret;

        public UserController(IUserRepository userRepository, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _jwtSecret = configuration["Jwt:Secret"];
        }

        [HttpPost("signup")]
        public async Task<IActionResult> SignUp(SignUpRequest model)
        {
            try
            {
                if(!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                //Check if user already exists
                var existingUser = await _userRepository.GetUserByEmailAsync(model.Email);
                if(existingUser != null)
                {
                    return Conflict("User with this email already exists");
                }

                //Hash the password before storing
                var passwordHash = new PasswordHasher<ApplicationUser>().HashPassword(null, model.Password);

                //Upload profile picture and get URL
                var profilePictureUrl = await UploadProfilePicture(model.ProfilePicture);

                //create new instance
                var newUser = new ApplicationUser
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    PasswordHash = passwordHash,
                    ProfilePictureUrl = profilePictureUrl
                };

                //generate and store token:
                var token = GenerateJwtToken(newUser);
                newUser.JWT = token;

                //store the user on database
                await _userRepository.AddUserAsync(newUser);

                return Ok(new { 
                    Token = token,
                    User = newUser
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while processing your request: {ex.Message}");
            }
        }

        [HttpPost("signin")]
        public async Task<IActionResult> SignIn(SignInRequest model)
        {
            if(!ModelState.IsValid) { return BadRequest(ModelState); }

            try
            {
                //validate email and password
                var user = await _userRepository.GetUserByEmailAsync(model.Email);

                if(user == null || !VerifyPassword(user.PasswordHash, model.Password))
                {
                    return Unauthorized("Invalid email or password");
                }

                var token = GenerateJwtToken(user);
                user.JWT = token;

                await _userRepository.UpdateUserAsync(user);

                return Ok(new { 
                    Token = token,
                    User = user});
            }
            catch (Exception ex)
            {

                return StatusCode(500, ex.Message);
            }
        }

        private bool VerifyPassword(string hashedPassword, string providedPassword)
        {
            var passwordHasher = new PasswordHasher<ApplicationUser>();
            var result = passwordHasher.VerifyHashedPassword(null, hashedPassword, providedPassword);

            return result == PasswordVerificationResult.Success;
        }

        private string GenerateJwtToken(ApplicationUser user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSecret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new System.Security.Claims.ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, user.Id.ToString()),
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private async Task<string> UploadProfilePicture(IFormFile profilePicture)
        {
            if(profilePicture != null && profilePicture.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(profilePicture.FileName);

                var filePath = Path.Combine("wwwroot", "uploads", fileName);

                using(var stream = new FileStream(filePath, FileMode.Create))
                {
                    await profilePicture.CopyToAsync(stream);
                }

                return $"/uploads/{fileName}";
            }
            else
            {
                return null;
            }
        }
    }
}
