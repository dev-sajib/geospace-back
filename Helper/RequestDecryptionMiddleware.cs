using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace geospace_back.Helper
{
    public class RequestDecryptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IWebHostEnvironment _env;

        public RequestDecryptionMiddleware(RequestDelegate next, IWebHostEnvironment env)
        {
            _next = next;
            _env = env;
        }

        public async Task Invoke(HttpContext context)
        {
            // Only decrypt for POST/PUT/PATCH with JSON
            if (!context.Request.ContentType?.Contains("application/json") ?? true ||
                (context.Request.Method != HttpMethods.Post &&
                 context.Request.Method != HttpMethods.Put &&
                 context.Request.Method != HttpMethods.Patch))
            {
                await _next(context);
                return;
            }

            context.Request.EnableBuffering();

            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
            var bodyStr = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;

            if (!string.IsNullOrWhiteSpace(bodyStr))
            {
                try
                {
                    var json = JsonNode.Parse(bodyStr);
                    var encryptedBase64 = json?["data"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(encryptedBase64))
                    {
                        var decryptedJson = AesEncryptionHelper.Decrypt(encryptedBase64);
                        var newBody = Encoding.UTF8.GetBytes(decryptedJson);
                        context.Request.Body = new MemoryStream(newBody);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Request decryption failed: " + ex.Message);
                    // fallback: pass original body
                    context.Request.Body.Position = 0;
                }
            }

            await _next(context);
        }
    }
}
