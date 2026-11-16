



using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json; 
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;


namespace VigilantMeshAgent.Security;

public class ProtectedJsonConfigurationProvider : FileConfigurationProvider
{
    public ProtectedJsonConfigurationProvider(FileConfigurationSource source) : base(source) { }

    [SupportedOSPlatform("windows")]
    public override void Load(Stream stream)
    {
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var jsonNode = JsonNode.Parse(stream)!;

            var heartbeatSection = jsonNode["Heartbeat"]!;
            string[] fieldsToDecrypt = { "ServerUrl", "ApiKey" };

            foreach (var field in fieldsToDecrypt)
            {
                var encryptedBase64 = heartbeatSection[field]!.GetValue<string>();
                if (string.IsNullOrEmpty(encryptedBase64)) continue;

                var encryptedBytes = Convert.FromBase64String(encryptedBase64);
                var originalBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.LocalMachine);
                var originalValue = Encoding.UTF8.GetString(originalBytes);
                heartbeatSection[field] = originalValue;
            }

            foreach (var jprop in jsonNode.AsObject())
            {
                ProcessJObject(jprop.Key, jprop.Value, data);
            }
            Data = data;
        }
        catch (Exception ex)
        {
            throw new FormatException("Erro ao ler ou descriptografar o arquivo de configura��o protegido.", ex);
        }
    }

    private void ProcessJObject(string prefix, JsonNode? node, Dictionary<string, string?> data)
    {
        if (node is JsonObject jobj)
        {
            foreach (var jprop in jobj)
            {
                ProcessJObject($"{prefix}:{jprop.Key}", jprop.Value, data);
            }
        }
        else if (node is JsonValue jval && jval.TryGetValue(out JsonElement element))
        {
            data[prefix] = element.ToString();
        }
    }
}

public class ProtectedJsonConfigurationSource : FileConfigurationSource
{
    public override IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        EnsureDefaults(builder);
        return new ProtectedJsonConfigurationProvider(this);
    }
}
