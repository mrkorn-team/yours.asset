#nullable enable
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;

/// <summary>
/// String-based AES protector interface
/// </summary>
public interface IAesDataProtector
{
  string Protect(string plaintext, string? purpose = null);
  string? Unprotect(string protectedText, string? purpose = null);
}

public class AesDataProtector : IDataProtector, IAesDataProtector
{
  private readonly byte[] _masterKey;
  private readonly string _purpose;

  private AesDataProtector(byte[] masterKey, string purpose = "")
  {
    _masterKey = masterKey ?? throw new ArgumentNullException(nameof(masterKey));
    _purpose = purpose;
  }

  // ---------------- Factory from human-friendly passphrase + salt ----------------
  public static AesDataProtector FromPassphrase(string apple, string banana)
  {
    if (string.IsNullOrEmpty(apple))
      throw new ArgumentNullException(nameof(apple));
    if (string.IsNullOrEmpty(banana))
      throw new ArgumentNullException(nameof(banana));

    using var kdf = new Rfc2898DeriveBytes(apple, Encoding.UTF8.GetBytes(banana), 100_000, HashAlgorithmName.SHA256);
    var masterKey = kdf.GetBytes(32); // AES-256
    return new AesDataProtector(masterKey);
  }

  // ---------------- Generate secure passphrase + salt ----------------
  public static (string passphrase, string salt) GenerateBase64SecureKeys(int length = 32)
  {
    byte[] passBytes = new byte[length];
    byte[] saltBytes = new byte[length];
    RandomNumberGenerator.Fill(passBytes);
    RandomNumberGenerator.Fill(saltBytes);
    string pass = Convert.ToBase64String(passBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    string salt = Convert.ToBase64String(saltBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    return (pass, salt);
  }

  public static (string passphrase, string salt) GenerateFriendlySecureKeys(int wordsCount = 3)
  {
    if (wordsCount <= 0)
      wordsCount = 3;

    // Simple built-in wordlist (you can expand this)
    string[] wordlist = new[]
    {
        "apple","banana","cherry","dragon","elephant","falcon","grape","honey","ice","jungle",
        "kiwi","lemon","mango","nectar","orange","peach","quartz","rose","sun","tiger",
        "umbrella","violet","wolf","xenon","yellow","zebra"
    };

    string RandomWords()
    {
      var sb = new StringBuilder();
      using var rng = RandomNumberGenerator.Create();
      for (int i = 0; i < wordsCount; i++)
      {
        byte[] buf = new byte[4];
        rng.GetBytes(buf);
        var idx = BitConverter.ToUInt32(buf, 0) % wordlist.Length;
        sb.Append(wordlist[idx]);
        if (i < wordsCount - 1)
          sb.Append('-'); // dash-separated
      }
      return sb.ToString();
    }

    string passphrase = RandomWords(); // human-friendly words
    string salt = RandomWords();       // human-friendly words
    return (passphrase, salt);
  }

  // ---------------- Internal AES/HMAC key derivation ----------------
  private void DeriveKeys(string purpose, out byte[] aesKey, out byte[] hmacKey)
  {
    using var hmac = new HMACSHA256(_masterKey);
    var purposeBytes = Encoding.UTF8.GetBytes(purpose ?? "");
    var hash = hmac.ComputeHash(purposeBytes);

    aesKey = new byte[32];
    hmacKey = new byte[32];
    Array.Copy(hash, 0, aesKey, 0, 32);
    Array.Copy(hash, 0, hmacKey, 0, 32);
  }

  // ---------------- Compression helpers ----------------
  private static byte[] Compress(byte[] data)
  {
    using var ms = new MemoryStream();
    using (var gzip = new GZipStream(ms, CompressionLevel.Optimal, true))
      gzip.Write(data, 0, data.Length);
    return ms.ToArray();
  }

  private static byte[] Decompress(byte[] data)
  {
    using var ms = new MemoryStream(data);
    using var gzip = new GZipStream(ms, CompressionMode.Decompress);
    using var output = new MemoryStream();
    gzip.CopyTo(output);
    return output.ToArray();
  }

  // ---------------- Core byte[] Protect/Unprotect ----------------
  public byte[] Protect(byte[] plaintext)
  {
    if (plaintext is null)
      throw new ArgumentNullException(nameof(plaintext));

    DeriveKeys(_purpose, out var aesKey, out var hmacKey);
    var compressed = Compress(plaintext);

    using var aes = Aes.Create()!;
    aes.Key = aesKey;
    aes.Mode = CipherMode.CBC;
    aes.Padding = PaddingMode.PKCS7;
    aes.GenerateIV();

    byte[] cipherBytes;
    using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
    using (var ms = new MemoryStream())
    {
      ms.Write(aes.IV, 0, aes.IV.Length);
      using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
      cs.Write(compressed, 0, compressed.Length);
      cs.FlushFinalBlock();
      cipherBytes = ms.ToArray();
    }

    using var hmac = new HMACSHA256(hmacKey);
    var tag = hmac.ComputeHash(cipherBytes);

    var final = new byte[cipherBytes.Length + tag.Length];
    Array.Copy(cipherBytes, 0, final, 0, cipherBytes.Length);
    Array.Copy(tag, 0, final, cipherBytes.Length, tag.Length);

    return final;
  }

  public byte[] Unprotect(byte[] protectedData)
  {
    if (protectedData is null)
      return null!;
    if (protectedData.Length < 32)
      throw new CryptographicException("Invalid data.");

    DeriveKeys(_purpose, out var aesKey, out var hmacKey);

    var tagLength = 32;
    var cipherBytes = new byte[protectedData.Length - tagLength];
    var tag = new byte[tagLength];
    Array.Copy(protectedData, 0, cipherBytes, 0, cipherBytes.Length);
    Array.Copy(protectedData, cipherBytes.Length, tag, 0, tag.Length);

    using var hmac = new HMACSHA256(hmacKey);
    var expectedTag = hmac.ComputeHash(cipherBytes);
    if (!CryptographicOperations.FixedTimeEquals(expectedTag, tag))
      throw new CryptographicException("HMAC validation failed. Ciphertext may be tampered.");

    using var aes = Aes.Create()!;
    aes.Key = aesKey;
    aes.Mode = CipherMode.CBC;
    aes.Padding = PaddingMode.PKCS7;

    var iv = new byte[aes.BlockSize / 8];
    Array.Copy(cipherBytes, 0, iv, 0, iv.Length);
    aes.IV = iv;

    var actualCipher = new byte[cipherBytes.Length - iv.Length];
    Array.Copy(cipherBytes, iv.Length, actualCipher, 0, actualCipher.Length);

    using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
    using var ms = new MemoryStream(actualCipher);
    using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
    using var output = new MemoryStream();
    cs.CopyTo(output);

    return Decompress(output.ToArray());
  }

  // ---------------- Protect/Unprotect string + optional purpose + URL-safe ----------------
  public string Protect(string plaintext, string? purpose = null)
  {
    if (plaintext is null)
      throw new ArgumentNullException(nameof(plaintext));
    var protector = purpose != null ? (AesDataProtector)CreateProtector(purpose) : this;
    var bytes = Encoding.UTF8.GetBytes(plaintext);
    string base64 = Convert.ToBase64String(protector.Protect(bytes));
    return base64.TrimEnd('=').Replace('+', '-').Replace('/', '_');
  }

  public string? Unprotect(string protectedText, string? purpose = null)
  {
    if (string.IsNullOrEmpty(protectedText))
      return null;
    var protector = purpose != null ? (AesDataProtector)CreateProtector(purpose) : this;

    string padded = protectedText.Replace('-', '+').Replace('_', '/');
    switch (padded.Length % 4)
    {
      case 2:
        padded += "==";
        break;
      case 3:
        padded += "=";
        break;
    }

    var bytes = Convert.FromBase64String(padded);
    var plainBytes = protector.Unprotect(bytes);
    return Encoding.UTF8.GetString(plainBytes);
  }

  // ---------------- IDataProtector ----------------
  public IDataProtector CreateProtector(string purpose)
  {
    string combinedPurpose = string.IsNullOrEmpty(_purpose) ? purpose : _purpose + "/" + purpose;
    return new AesDataProtector(_masterKey, combinedPurpose);
  }
}

public static class AesDataProtectorExtension
{
  public static IServiceCollection RegisterIAesDataProtector(this WebApplicationBuilder builder,
    string? passphrase = null, string? salt = null)
  {
    // Create apple + banana:
    //var (passphrase_, salt_) = AesDataProtector.GenerateFriendlySecureKeys(1);
    //Debug.WriteLine($"Passphrase: {passphrase_}, Salt: {salt_}");

    passphrase ??= builder.Configuration[$"DataProtection:Passphrase"]
      ?? Environment.GetEnvironmentVariable("DATA_PROTECTION_PASSPHRASE");
    salt ??= builder.Configuration["DataProtection:Salt"]
      ?? Environment.GetEnvironmentVariable("DATA_PROTECTION_SALT");
    if (string.IsNullOrEmpty(passphrase) || string.IsNullOrEmpty(salt))
      throw new InvalidOperationException("Missing passphrase and/or salt for DataProtection.");
    builder.Services.AddSingleton<IAesDataProtector>(sp => AesDataProtector.FromPassphrase(passphrase, salt));
    return builder.Services;
  }
}
