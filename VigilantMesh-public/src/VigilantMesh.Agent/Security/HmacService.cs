

using System.Security.Cryptography;
using System.Text;

namespace VigilantMeshAgent.Security;

public static class HmacService
{
    public static string GenerateSignature(string apiKey, string timestamp, string requestBody)
    {
        
        var keyBytes = Encoding.UTF8.GetBytes(apiKey);

        
        
        var stringToSign = $"{timestamp}.{requestBody}";
        var stringToSignBytes = Encoding.UTF8.GetBytes(stringToSign);

        
        using var hmac = new HMACSHA256(keyBytes);

        
        var signatureBytes = hmac.ComputeHash(stringToSignBytes);

        
        return Convert.ToBase64String(signatureBytes);
    }
}
