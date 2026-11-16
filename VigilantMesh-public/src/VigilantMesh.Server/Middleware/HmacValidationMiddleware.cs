

using System.Security.Cryptography;
using System.Text;

namespace VigilantMeshServer.Middleware;

public class HmacValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<HmacValidationMiddleware> _logger;
    private readonly string _apiKey;

    public HmacValidationMiddleware(RequestDelegate next, IConfiguration config, ILogger<HmacValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        
        _apiKey = config["Security:ApiKey"] ?? "";

        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogCritical("A CHAVE SECRETA HMAC (Security:ApiKey) NÃO FOI CARREGADA! Todos os pedidos serão rejeitados.");
        }
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.Equals("/health", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue("X-Request-Timestamp", out var timestampValue) ||
            !context.Request.Headers.TryGetValue("X-Signature", out var signatureValue))
        {
            _logger.LogWarning("Pedido bloqueado. Cabeçalhos de assinatura em falta.");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Cabeçalhos de assinatura HMAC em falta.");
            return;
        }

        var timestampString = timestampValue.ToString();
        var clientSignature = signatureValue.ToString();

        if (!long.TryParse(timestampString, out var timestamp))
        {
            _logger.LogWarning("Timestamp inválido recebido: {Timestamp}", timestampString);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Timestamp inválido.");
            return;
        }

        var requestTime = DateTimeOffset.FromUnixTimeSeconds(timestamp);
        if (Math.Abs((DateTimeOffset.UtcNow - requestTime).TotalMinutes) > 5)
        {
            _logger.LogWarning("Timestamp expirado. Potencial replay attack. RequestTime: {RequestTime}", requestTime);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Timestamp expirado.");
            return;
        }

        
        
        
        context.Request.EnableBuffering();
        string requestBody;
        using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true))
        {
            requestBody = await reader.ReadToEndAsync();
            
            context.Request.Body.Position = 0;
        }
        

        var keyBytes = Encoding.UTF8.GetBytes(_apiKey);
        var stringToSign = $"{timestampString}.{requestBody}";
        var stringToSignBytes = Encoding.UTF8.GetBytes(stringToSign);
        using var hmac = new HMACSHA256(keyBytes);
        var serverSignatureBytes = hmac.ComputeHash(stringToSignBytes);
        var serverSignature = Convert.ToBase64String(serverSignatureBytes);

        if (!TryValidateSignature(serverSignature, clientSignature))
        {
            _logger.LogWarning("Validação HMAC falhou.");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Assinatura inválida.");
            return;
        }

        _logger.LogInformation("Validação HMAC bem-sucedida para o pedido.");
        await _next(context);
    }

    private static bool TryValidateSignature(string expected, string provided)
    {
        try
        {
            var expectedBytes = Convert.FromBase64String(expected);
            var providedBytes = Convert.FromBase64String(provided);
            return CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}