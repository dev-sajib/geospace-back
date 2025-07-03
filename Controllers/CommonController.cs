using geospace_back.DTO;
using geospace_back.Helper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace geospace_back.Controllers
{
    [ApiController]
    public class CommonController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public CommonController(IConfiguration configuration, IWebHostEnvironment hostingEnvironment)
        {
            _configuration = configuration;
            _hostingEnvironment = hostingEnvironment;
        }

        [AllowAnonymous]
        [HttpPost("Login")]
        public IActionResult Login(LoginDTO obj)
        {
            try
            {
                string encryptedPassword = new Shr2Shr.Api.Strings.Crypto().Encrypt(obj.Password);

                var cmdParameters = new Dictionary<string, SqlParameter>
                {
                    ["@Email"] = new SqlParameter("@Email", obj.Email),
                    ["@PasswordHash"] = new SqlParameter("@PasswordHash", encryptedPassword)
                };

                var resultDataSet = DbHelperExtensions.ExecuteQuery("SP_Login", true, cmdParameters);

                if (resultDataSet == null || resultDataSet.Tables.Count == 0 || resultDataSet.Tables[0].Rows.Count == 0)
                {
                    return Unauthorized(new { Message = "An unexpected error occurred during login." });
                }

                var userRow = resultDataSet.Tables[0].Rows[0];
                var userDetails = new
                {
                    UserId = Convert.ToInt32(userRow["UserId"]),
                    Email = userRow["Email"].ToString(),
                    RoleId = Convert.ToInt32(userRow["RoleId"]),
                    RoleName = userRow["RoleName"].ToString()
                };

                var tokenString = GenerateJwtToken(userDetails.UserId.ToString(), userDetails.RoleName);

                var response = new
                {
                    Token = tokenString,
                    UserDetails = userDetails
                };

                return Ok(response);
            }
            catch (SqlException ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An internal server error occurred.", Details = ex.Message });
            }
        }

        private string GenerateJwtToken(string userId, string role)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["JWT:Key"]);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, userId),
                    new Claim(ClaimTypes.Role, role),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                }),
                Expires = DateTime.UtcNow.AddHours(12),
                Issuer = _configuration["JWT:Issuer"],
                Audience = _configuration["JWT:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        [HttpGet("GetMenusByRoleId")]
        public IActionResult GetMenusByRoleId(int RoleId)
        {
            try
            {
                var cmdParameters = new Dictionary<string, SqlParameter>
                {
                    ["@RoleId"] = new SqlParameter("@RoleId", RoleId)
                };

                var jsonResult = DbHelperExtensions.ExecuteProcedureAndReturnJson("SP_GetMenusByRole", cmdParameters);

                if (string.IsNullOrEmpty(jsonResult))
                {
                    return Ok("[]");
                }

                return Content(jsonResult, "application/json");
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred.", Details = ex.Message });
            }
        }
    }
}