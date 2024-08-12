using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using Qommon;

namespace Administrator.Database;

public static class DbModelExtensions
{
    internal static string FormatKey<T>(this INumberKeyedDbEntity<T> entity) where T : INumber<T>
    {
        return $"`[#{entity.Id}]`";
    }
    
    public static string RegenerateApiKey(this Guild guild)
    {
        var idBytes = Encoding.Default.GetBytes(guild.GuildId.ToString());
        var cryptoBytes = new byte[32];
        var saltBytes = new byte[16];
        
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(cryptoBytes);
            rng.GetBytes(saltBytes);
        }

        var hashBytes = GetHashBytes(cryptoBytes, saltBytes);
        /*
        var pbkdf2 = new Rfc2898DeriveBytes(cryptoBytes, saltBytes, 10000, HashAlgorithmName.SHA256);

        var hash = pbkdf2.GetBytes(32);
        var hashBytes = new byte[48];

        Array.Copy(saltBytes, 0, hashBytes, 0, 16);
        Array.Copy(hash, 0, hashBytes, 16, 32);
        */

        guild.ApiKeySalt = saltBytes;
        guild.ApiKeyHash = hashBytes;

        return new StringBuilder()
            .Append(Convert.ToBase64String(idBytes))
            .Append('.')
            .Append(Convert.ToBase64String(cryptoBytes))
            .ToString();
    }

    public static bool CheckApiKeyHash(this Guild guild, byte[] cryptoBytes)
    {
        Guard.IsNotNull(guild.ApiKeySalt);
        Guard.IsNotNull(guild.ApiKeyHash);
        
        var hashBytes = GetHashBytes(cryptoBytes, guild.ApiKeySalt);
        return guild.ApiKeyHash.SequenceEqual(hashBytes);
    }

    private static byte[] GetHashBytes(byte[] cryptoBytes, byte[] saltBytes)
    {
        var pbkdf2 = new Rfc2898DeriveBytes(cryptoBytes, saltBytes, 10000, HashAlgorithmName.SHA256);

        var hash = pbkdf2.GetBytes(32);
        var hashBytes = new byte[48];
        
        Array.Copy(saltBytes, 0, hashBytes, 0, 16);
        Array.Copy(hash, 0, hashBytes, 16, 32);

        return hashBytes;
    }
}