using System.Text;
using System.Text.Json;

namespace geospace_back.Helper
{
    public class ResponseEncryptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IWebHostEnvironment _env;

        public ResponseEncryptionMiddleware(RequestDelegate next, IWebHostEnvironment env)
        {
            _next = next;
            _env = env;
        }

        public async Task Invoke(HttpContext context)
        {
            var originalBodyStream = context.Response.Body;

            using var newBodyStream = new MemoryStream();
            context.Response.Body = newBodyStream;

            await _next(context);

            newBodyStream.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(newBodyStream).ReadToEndAsync();

            // Get the response content type
            var contentType = context.Response.ContentType ?? "";

            // Only encrypt if content type is JSON or text
            if ((contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase) ||
                 contentType.Contains("text/", StringComparison.OrdinalIgnoreCase)) &&
                !string.IsNullOrWhiteSpace(responseBody) &&
                context.Response.StatusCode == 200)
            {
                var encrypted = AesEncryptionHelper.Encrypt(responseBody);

                context.Response.ContentLength = Encoding.UTF8.GetByteCount(encrypted);
                context.Response.ContentType = "text/plain";
                context.Response.Body = originalBodyStream;
                await context.Response.WriteAsync(encrypted);
            }
            else
            {
                newBodyStream.Seek(0, SeekOrigin.Begin);
                await newBodyStream.CopyToAsync(originalBodyStream);
            }
        }
    }

}
