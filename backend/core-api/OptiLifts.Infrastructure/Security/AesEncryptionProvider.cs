using System.Security.Cryptography;
using System.Text;

namespace OptiLifts.Infrastructure.Security;

public interface IEncryptionProvider
{
    string Encrypt(string rawText);
    string Decrypt(string encryText);
}

public class AesEncryptionProvider : IEncryptionProvider
{
    private readonly byte[] _key;

    public AesEncryptionProvider(string keyB64)
    {
        _key = Convert.FromBase64String(keyB64);
        if (_key.Length != 32)
        {
            throw new ArgumentException("Invalid encryption key length. Expected 32 bytes for AES-256.");
        }

    }

    public string Encrypt(string rawText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV(); //aes generates random 16bit IV

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var memStream = new MemoryStream();
        memStream.Write(aes.IV, 0, aes.IV.Length); //add IV first for decryption

        using (var crypStream = new CryptoStream(memStream, encryptor, CryptoStreamMode.Write))
        using (var streamWriter = new StreamWriter(crypStream))
        {
            streamWriter.Write(rawText);
        }

        return Convert.ToBase64String(memStream.ToArray());
    }

    public string Decrypt(string encryText)
    {
        var cipher = Convert.FromBase64String(encryText);

        using var aes = Aes.Create();
        aes.Key = _key;

        //iv
        var iv = new byte[16];
        Array.Copy(cipher, 0, iv, 0, iv.Length);
        aes.IV = iv;

        //cipher itself
        var cipherBytes = new byte[cipher.Length - iv.Length];
        Array.Copy(cipher, iv.Length, cipherBytes, 0, cipherBytes.Length);

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var memStream = new MemoryStream(cipherBytes);
        using var crypStream = new CryptoStream(memStream, decryptor, CryptoStreamMode.Read);
        using var reader = new StreamReader(crypStream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}