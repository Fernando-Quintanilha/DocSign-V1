using System.Security.Cryptography;
using System.Text;

namespace HoleriteSign.Api.Services;

/// <summary>
/// AES-256-GCM encryption service for PII (CPF, Birth Date).
/// Key is derived from config "Encryption:Key" via SHA-256.
/// </summary>
public class EncryptionService
{
    private readonly byte[] _key;

    public EncryptionService(IConfiguration config)
    {
        var keyString = config["Encryption:Key"]
            ?? throw new InvalidOperationException("Encryption:Key not configured in appsettings.");
        // Derive a 256-bit key using SHA-256
        _key = SHA256.HashData(Encoding.UTF8.GetBytes(keyString));
    }

    /// <summary>Encrypt plaintext to bytes (nonce + ciphertext + tag).</summary>
    public byte[] Encrypt(string plaintext)
    {
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce = new byte[AesGcm.NonceByteSizes.MaxSize]; // 12 bytes
        RandomNumberGenerator.Fill(nonce);

        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[AesGcm.TagByteSizes.MaxSize]; // 16 bytes

        using var aes = new AesGcm(_key, AesGcm.TagByteSizes.MaxSize);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        // Format: nonce (12) + ciphertext (N) + tag (16)
        var result = new byte[nonce.Length + ciphertext.Length + tag.Length];
        nonce.CopyTo(result, 0);
        ciphertext.CopyTo(result, nonce.Length);
        tag.CopyTo(result, nonce.Length + ciphertext.Length);

        return result;
    }

    /// <summary>Decrypt bytes (nonce + ciphertext + tag) to plaintext.</summary>
    public string Decrypt(byte[] encryptedData)
    {
        if (encryptedData.Length < 28) // 12 nonce + 0 data + 16 tag minimum
        {
            // Fallback: try reading as raw UTF-8 (legacy MVP data)
            return Encoding.UTF8.GetString(encryptedData);
        }

        var nonceSize = AesGcm.NonceByteSizes.MaxSize; // 12
        var tagSize = AesGcm.TagByteSizes.MaxSize;     // 16

        var nonce = encryptedData[..nonceSize];
        var tag = encryptedData[^tagSize..];
        var ciphertext = encryptedData[nonceSize..^tagSize];

        var plaintext = new byte[ciphertext.Length];

        try
        {
            using var aes = new AesGcm(_key, tagSize);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);
            return Encoding.UTF8.GetString(plaintext);
        }
        catch (CryptographicException)
        {
            // Fallback: legacy data stored as raw UTF-8 bytes
            return Encoding.UTF8.GetString(encryptedData);
        }
    }
}
