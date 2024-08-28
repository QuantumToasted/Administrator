using System.Security.Cryptography;
using System.Text;
using Administrator.Database;
using Qommon;

namespace Administrator.Bot;

public static partial class DbModelExtensions
{
    public static bool HasSetting(this Guild guild, GuildSettings setting)
        => guild.Settings.HasFlag(setting);
    
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
            .Append(Convert.ToBase64String(idBytes).Replace("=", ""))
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