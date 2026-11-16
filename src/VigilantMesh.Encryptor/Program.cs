
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

Console.WriteLine("--- Ferramenta de Criptografia de appsettings.json ---");

if (!OperatingSystem.IsWindows())
{
    Console.Error.WriteLine("VigilantMesh.Encryptor requer Windows porque usa DPAPI.");
    return;
}

string originalPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
if (!File.Exists(originalPath))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"Erro: Arquivo 'appsettings.json' não encontrado em: {originalPath}");
    Console.WriteLine("\nCopie o appsettings.json do VigilantMeshAgent para esta pasta e tente novamente.");
    Console.ResetColor();
    return;
}

Console.WriteLine("Arquivo original encontrado. Lendo configurações...");
var jsonString = File.ReadAllText(originalPath);
var jsonNode = JsonNode.Parse(jsonString)!;

var heartbeatSection = jsonNode["Heartbeat"]!;
string[] fieldsToEncrypt = { "ServerUrl", "ApiKey" };

foreach (var field in fieldsToEncrypt)
{
    var originalValue = heartbeatSection[field]!.GetValue<string>();
    if (string.IsNullOrEmpty(originalValue)) continue;

    Console.WriteLine($"Criptografando o campo: {field}...");
    var originalBytes = Encoding.UTF8.GetBytes(originalValue);

    
    
    var encryptedBytes = ProtectedData.Protect(originalBytes, null, DataProtectionScope.LocalMachine);

    var encryptedBase64 = Convert.ToBase64String(encryptedBytes);
    heartbeatSection[field] = encryptedBase64;
}

string protectedPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.protected.json");
var options = new JsonSerializerOptions { WriteIndented = true };
File.WriteAllText(protectedPath, jsonNode.ToJsonString(options));

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("\nSucesso!");
Console.WriteLine($"Arquivo '{Path.GetFileName(protectedPath)}' criado com as configurações protegidas.");
Console.ResetColor();
Console.WriteLine("Você pode agora usar este arquivo na publicação do seu VigilantMeshAgent.");
