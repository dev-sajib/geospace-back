using geospace_back.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Data.SqlClient;
using Microsoft.Net.Http.Headers;
using System.Net;

namespace geospace_back.Controllers
{
    [ApiController]
    public class DocumentController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        public DocumentController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpGet("GetFile")]
        public IActionResult GetFile(string path)
        {
            // Ensure the path is absolute and the file exists
            if (Path.IsPathRooted(path) && System.IO.File.Exists(path))
            {
                var provider = new FileExtensionContentTypeProvider();
                if (!provider.TryGetContentType(path, out var contentType))
                {
                    contentType = "application/octet-stream";
                }

                var fileBytes = System.IO.File.ReadAllBytes(path);
                var contentDisposition = new System.Net.Mime.ContentDisposition
                {
                    FileName = Path.GetFileName(path),
                    Inline = contentType.StartsWith("image/") || contentType == "application/pdf"
                };

                Response.Headers.Add("Content-Disposition", contentDisposition.ToString());
                Response.Headers.Add("Content-Type", contentType);
                return File(fileBytes, contentType);
            }
            else
            {
                return NotFound();
            }
        }

        private string GetDocumentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "PDF",
                ".xls" => "Excel",
                ".xlsx" => "Excel",
                ".jpg" => "Image",
                ".jpeg" => "Image",
                ".png" => "Image",
                ".doc" => "Word",
                ".docx" => "Word",
                _ => "Unknown"
            };
        }

        [HttpPost("MultipleFileUpload")]
        public async Task<IActionResult> MultipleFileUpload(string UserId, string SourceCode, string SourceName, string FilePath, List<IFormFile> files)
        {
            var root = Path.Combine(Directory.GetCurrentDirectory(), FilePath);
            try
            {
                if (!Directory.Exists(root))
                {
                    Directory.CreateDirectory(root);
                }

                var dateTime = DateTime.Now;

                foreach (var file in files)
                {
                    var FileName = file.FileName;
                    var FinalFilePath = Path.Combine(root, FileName);

                    // Check if the file already exists
                    if (System.IO.File.Exists(FinalFilePath))
                    {
                        var conflictMsg = new MessageHelper
                        {
                            StatusCode = (int)HttpStatusCode.Conflict,
                            Message = $"File '{FileName}' already exists."
                        };
                        return StatusCode((int)HttpStatusCode.Conflict, conflictMsg);
                    }

                    // Log the final file path
                    Console.WriteLine($"Final file path: {FinalFilePath}");

                    using (var stream = new FileStream(FinalFilePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    var fileInfo = new FileInfo(FinalFilePath);
                    var size = fileInfo.Length;
                    var id = Guid.NewGuid().ToString();
                    var DocumentType = GetDocumentType(FileName);

                    var cmdParameters = new Dictionary<string, SqlParameter>
                    {
                        ["@__SessionId"] = new SqlParameter("@__SessionId", UserId),
                        ["@SourceCode"] = new SqlParameter("@SourceCode", SourceCode),
                        ["@SourceName"] = new SqlParameter("@SourceName", SourceName),
                        ["@FileName"] = new SqlParameter("@FileName", FileName),
                        ["@Path"] = new SqlParameter("@Path", FinalFilePath),
                        ["@CreatedBy"] = new SqlParameter("@CreatedBy", UserId),
                        ["@DocumentType"] = new SqlParameter("@DocumentType", DocumentType)
                    };

                    var result = DbHelperExtensions.ExecuteQuery("CKPVisionERP_Documents_Insert", true, cmdParameters);
                }

                var successMsg = new MessageHelper
                {
                    StatusCode = 200,
                    Message = "Attachment Upload Successfully."
                };

                return Ok(successMsg);
            }
            catch (UnauthorizedAccessException ex)
            {
                var forbiddenMsg = new MessageHelper
                {
                    StatusCode = (int)HttpStatusCode.Forbidden,
                    Message = "Access to the path is denied. Please check the directory permissions.",
                };
                return StatusCode((int)HttpStatusCode.Forbidden, forbiddenMsg);
            }
            catch (Exception ex)
            {
                var errorMsg = new MessageHelper
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError,
                    Message = ex.Message,
                };
                return StatusCode((int)HttpStatusCode.InternalServerError, errorMsg);
            }
        }
    }
}