using geospace_back.Helper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Data;
using System.Net;

namespace geospace_back.Controllers
{
    [ApiController]
    public class ExpenseReimbursementController : ControllerBase
    {
        [HttpGet("ExpenseView")]
        public IActionResult ExpenseView(DateTime FromDate, DateTime ToDate, bool IsFullRange, string? RequestEmployeeId)
        {
            try
            {
                var listParameters = new Dictionary<string, SqlParameter>
                {
                    ["@FromDate"] = new SqlParameter("@FromDate", FromDate),
                    ["@ToDate"] = new SqlParameter("@ToDate", ToDate),
                    ["@IsFullRange"] = new SqlParameter("@IsFullRange", IsFullRange),
                    ["@RequestEmployeeId"] = new SqlParameter("@RequestEmployeeId", RequestEmployeeId),
                };

                var list = DbHelperExtensions.ExecuteQueryAndConvertToList("SP_ExpenseView", listParameters);

                if (list != null && list.Count > 0)
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

        [HttpGet("ExpenseDetailsById")]
        public IActionResult ExpenseDetailsById(long ExpenseId)
        {
            try
            {
                var listParameters = new Dictionary<string, SqlParameter>
                {
                    ["@ExpenseId"] = new SqlParameter("@ExpenseId", ExpenseId)
                };

                var list = DbHelperExtensions.ExecuteQueryAndConvertToList("SP_ExpenseDetails_ById", listParameters);

                if (list != null && list.Count > 0)
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

        //[HttpPost("ExpenseCreateEdit")]
        //public async Task<IActionResult> ExpenseCreateEdit(ExpenseDTO obj)
        //{
        //    try
        //    {
        //        string jsonString = JsonConvert.SerializeObject(obj, Formatting.None);

        //        var cmdParameters = new Dictionary<string, SqlParameter>
        //        {
        //            ["@JsonString"] = new SqlParameter("@JsonString", jsonString)
        //        };

        //        var statusCodeParameter = new SqlParameter("@statusCode", SqlDbType.Int) { Direction = ParameterDirection.Output };
        //        cmdParameters["@statusCode"] = statusCodeParameter;

        //        string result = DbHelperExtensions.ExecuteCommandWithOutput("SP_ExpenseCreateEdit", true, cmdParameters);
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
        //        var msg = new MessageHelper
        //        {
        //            StatusCode = (int)HttpStatusCode.InternalServerError,
        //            Message = ex.Message,
        //        };
        //        return StatusCode((int)HttpStatusCode.InternalServerError, msg);
        //    }
        //}

    }
}
