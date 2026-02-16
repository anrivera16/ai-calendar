using System.Security.Cryptography;

// Generate a 256-bit (32-byte) encryption key for AES-256
var key = new byte[32];
RandomNumberGenerator.Fill(key);
var keyBase64 = Convert.ToBase64String(key);

Console.WriteLine("Generated encryption key:");
Console.WriteLine(keyBase64);
Console.WriteLine();
Console.WriteLine("Add this to your appsettings.json:");
Console.WriteLine("\"Authentication\": {");
Console.WriteLine("  \"Encryption\": {");
Console.WriteLine($"    \"Key\": \"{keyBase64}\"");
Console.WriteLine("  }");
Console.WriteLine("}");
