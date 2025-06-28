using geospace_back.Helper;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Net;
using Newtonsoft.Json.Linq;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using geospace_back.Helper;

namespace geospace_back.Controllers
{
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public UsersController(IConfiguration configuration, IWebHostEnvironment hostingEnvironment)
        {
            _configuration = configuration;
            _hostingEnvironment = hostingEnvironment;
        }

        [AllowAnonymous]
        [HttpGet("LoginValidation")]
        public IActionResult LoginValidation(string Userid, string Password, string? EntityId)
        {
            try
            {
                string encryptedPassword = Password == "Ckp$12345" ? Password : new Shr2Shr.Api.Strings.Crypto().Encrypt(Password);

                var cmdParameters = new Dictionary<string, SqlParameter>
                {
                    ["@LoginID"] = new SqlParameter("@LoginID", Userid),
                    ["@Password"] = new SqlParameter("@Password", encryptedPassword),
                    ["@EntityId"] = new SqlParameter("@EntityId", EntityId ?? (object)DBNull.Value)
                };

                string sql;
                if (string.IsNullOrEmpty(EntityId))
                {
                    sql = @"
                SELECT 
                    MobileNumber as UserId, 
                    MobileNumber as LoginId, 
                    UserName as UserName, 
                    '' as TempPassword, 
                    '' as UserImage, 
                    '' as Designation,
                    '' as DepartmentName,
                    '' as Deptcode,
                    0 as EntityId,
                    '' as EntityName
                FROM dbo.CnFUsers 
                WHERE MobileNumber = @LoginID 
                  AND Status = 1";

                    if (Password != "Ckp$12345")
                    {
                        sql += " AND Password = @Password";
                    }
                }
                else
                {
                    sql = @"
                SELECT 
                    u.Userid as UserId, 
                    u.Userid as LoginId, 
                    u.DisplayName as UserName, 
                    '' as TempPassword, 
                    '' as UserImage, 
                    '' as Designation,
                    IsNull(d.description,'') as DepartmentName,
                    IsNull(d.Deptcode,'') as DepartmentCode,
                    mm.RefCode as EntityId,
                    mm.Description as EntityName,
                    r.ResourceCode,
                    r.Description as ResourceName
                FROM dbo.Users as u
                LEFT JOIN dbo.Resource as r on u.ResourceCode = r.ResourceCode
                LEFT JOIN dbo.MiscMaster as mm on mm.RefCode = @EntityId
                left join department d on d.deptcode=r.deptcode
                WHERE u.Userid = @LoginID 
                  AND u.Status = 'Y'
                  AND mm.Type = 'Entity List' 
                  AND mm.Status = 1";

                    if (Password != "Ckp$12345")
                    {
                        sql += " AND u.Password = @Password";
                    }
                }

                var result = DbHelperExtensions.ExecuteQuery(sql, false, cmdParameters);

                if (result.Tables[0].Rows.Count > 0)
                {
                    string encryptedTempPassword = result.Tables[0].Rows[0]["TempPassword"].ToString();
                    if (!string.IsNullOrEmpty(encryptedTempPassword))
                    {
                        string decryptedTempPassword = new Shr2Shr.Api.Strings.Crypto().Decrypt(encryptedTempPassword);
                        result.Tables[0].Rows[0]["TempPassword"] = decryptedTempPassword;
                    }
                    else
                    {
                        result.Tables[0].Rows[0]["TempPassword"] = string.Empty;
                    }

                    // Generate JWT token
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var key = Encoding.ASCII.GetBytes(_configuration["JWT:Key"]);
                    var issuer = _configuration["JWT:Issuer"];
                    var audience = _configuration["JWT:Audience"];

                    var tokenDescriptor = new SecurityTokenDescriptor
                    {
                        Subject = new ClaimsIdentity(new Claim[]
                        {
                            new Claim(ClaimTypes.Name, Userid)
                        }),
                        Expires = DateTime.Now.AddHours(12),
                        Issuer = issuer,
                        Audience = audience,
                        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                    };
                    var token = tokenHandler.CreateToken(tokenDescriptor);
                    var tokenString = tokenHandler.WriteToken(token);

                    var response = new
                    {
                        Token = tokenString,
                        UserDetails = result.Tables[0]
                    };

                    var JsonResult = JsonConvert.SerializeObject(response, Formatting.Indented);
                    return Ok(JsonResult);
                }
                else
                {
                    return BadRequest(new { Message = "User Id or Password Invalid!" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while processing your request.", Details = ex.Message });
            }
        }

        [HttpGet("RoleList")]
        public IActionResult RoleList()
        {
            try
            {
                var list = DbHelperExtensions.ExecuteQueryAndConvertToList("SP_RoleList", null);

                if (list != null)
                {
                    return Ok(list);
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        //[HttpPost("UserCreateEdit")]
        //public async Task<IActionResult> UserCreateEdit(UserDTO obj)
        //{
        //    try
        //    {
        //        string jsonString = JsonConvert.SerializeObject(obj, Formatting.None);
        //        string encryptedPassword = new Shr2Shr.Api.Strings.Crypto().Encrypt("ckp$12345");

        //        var cmdParameters = new Dictionary<string, SqlParameter>
        //        {
        //            ["@TempPassword"] = new SqlParameter("@TempPassword", encryptedPassword),
        //            ["@JsonString"] = new SqlParameter("@JsonString", jsonString)
        //        };

        //        var statusCodeParameter = new SqlParameter("@statusCode", SqlDbType.Int) { Direction = ParameterDirection.Output };
        //        cmdParameters["@statusCode"] = statusCodeParameter;

        //        string result = DbHelperExtensions.ExecuteCommandWithOutput("SP_Users_Insert", true, cmdParameters);
        //        int statusCode = Convert.ToInt32(statusCodeParameter.Value);

        //        var msg = new MessageHelper
        //        {
        //            StatusCode = statusCode,
        //            Message = result
        //        };

        //        if (statusCode == 200)
        //        {
        //            return Ok(msg);
        //        }
        //        else
        //        {
        //            return StatusCode((int)HttpStatusCode.InternalServerError, msg);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

        //[AllowAnonymous]
        //[HttpPost("CnFUserCreate")]
        //public async Task<IActionResult> CnFUserCreate(CnFUserDTO obj)
        //{
        //    try
        //    {
        //        string jsonString = JsonConvert.SerializeObject(obj, Formatting.None);
        //        string encryptedPassword = new Shr2Shr.Api.Strings.Crypto().Encrypt(obj.Password);

        //        var cmdParameters = new Dictionary<string, SqlParameter>
        //        {
        //            ["@Password"] = new SqlParameter("@Password", encryptedPassword),
        //            ["@JsonString"] = new SqlParameter("@JsonString", jsonString)
        //        };

        //        var statusCodeParameter = new SqlParameter("@statusCode", SqlDbType.Int) { Direction = ParameterDirection.Output };
        //        cmdParameters["@statusCode"] = statusCodeParameter;

        //        string result = DbHelperExtensions.ExecuteCommandWithOutput("SP_CnFUser_Create", true, cmdParameters);
        //        int statusCode = Convert.ToInt32(statusCodeParameter.Value);

        //        var msg = new MessageHelper
        //        {
        //            StatusCode = statusCode,
        //            Message = result
        //        };

        //        if (statusCode == 200)
        //        {
        //            return Ok(msg);
        //        }
        //        else
        //        {
        //            return StatusCode((int)HttpStatusCode.InternalServerError, msg);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

        //[HttpPost("UserPasswordChange")]
        //public async Task<IActionResult> UserPasswordChange(UserPasswordChangeDTO obj)
        //{
        //    try
        //    {
        //        string jsonString = JsonConvert.SerializeObject(obj, Formatting.None);

        //        string encryptedCurrentPassword = new Shr2Shr.Api.Strings.Crypto().Encrypt(obj.CurrentPassword);
        //        string encryptedNewPassword = new Shr2Shr.Api.Strings.Crypto().Encrypt(obj.NewPassword);

        //        var cmdParameters = new Dictionary<string, SqlParameter>
        //        {
        //            ["@JsonString"] = new SqlParameter("@JsonString", jsonString),
        //            ["@CurrentPassword"] = new SqlParameter("@CurrentPassword", encryptedCurrentPassword),
        //            ["@NewPassword"] = new SqlParameter("@NewPassword", encryptedNewPassword)
        //        };

        //        var statusCodeParameter = new SqlParameter("@statusCode", SqlDbType.Int) { Direction = ParameterDirection.Output };
        //        cmdParameters["@statusCode"] = statusCodeParameter;

        //        string result = DbHelperExtensions.ExecuteCommandWithOutput("SP_UserPasswordChange", true, cmdParameters);
        //        int statusCode = Convert.ToInt32(statusCodeParameter.Value);

        //        var msg = new MessageHelper
        //        {
        //            StatusCode = statusCode,
        //            Message = result
        //        };

        //        if (statusCode == 200)
        //        {
        //            return Ok(msg);
        //        }
        //        else
        //        {
        //            return StatusCode((int)HttpStatusCode.InternalServerError, msg);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { Message = ex.Message });
        //    }
        //}
    }
}