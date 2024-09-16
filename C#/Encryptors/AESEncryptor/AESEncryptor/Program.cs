using System;
using System.IO;
using System.Security.Cryptography;

class Program
{
    static void Main(string[] args)
    {
        byte[] shellcode = File.ReadAllBytes(args[0]);

        byte[] key = new byte[32];
        byte[] iv = new byte[16];

        using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
        {
            rng.GetBytes(key);
            rng.GetBytes(iv);
        }

        byte[] encryptedBytes;
        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = key;
            aesAlg.IV = iv;
            aesAlg.Padding = PaddingMode.PKCS7;
            aesAlg.Mode = CipherMode.CBC;

            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
            encryptedBytes = encryptor.TransformFinalBlock(shellcode, 0, shellcode.Length);
        }

        string encryptedBase64 = Convert.ToBase64String(encryptedBytes);

        // Decrypt Base64-encoded shellcode with AES
        byte[] decryptedBytes;
        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = key;
            aesAlg.IV = iv;
            aesAlg.Padding = PaddingMode.PKCS7;
            aesAlg.Mode = CipherMode.CBC;

            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
            byte[] encryptedBytesArray = Convert.FromBase64String(encryptedBase64);
            decryptedBytes = decryptor.TransformFinalBlock(encryptedBytesArray, 0, encryptedBytesArray.Length);
        }

        string decryptedShellcode = Convert.ToBase64String(decryptedBytes);

        Console.WriteLine("[i] Debug information:\n");
        Console.WriteLine($"\tkey = \"{Convert.ToBase64String(key)}\"\n\tiv = \"{Convert.ToBase64String(iv)}\"");

        string sha256Original = BitConverter.ToString(SHA256.Create().ComputeHash(shellcode)).Replace("-", "");
        string sha256Encrypted = BitConverter.ToString(SHA256.Create().ComputeHash(encryptedBytes)).Replace("-", "");
        string sha256Decrypted = BitConverter.ToString(SHA256.Create().ComputeHash(decryptedBytes)).Replace("-", "");

        Console.WriteLine($"\n\tSHA256 of original shellcode:\t{sha256Original}");
        Console.WriteLine($"\tSHA256 of encrypted shellcode:\t{sha256Encrypted}");
        Console.WriteLine($"\tSHA256 of decrypted shellcode:\t{sha256Decrypted}");
               
        Console.WriteLine("\n[i] C# code to decrypt shellcode:\n");

        string csharpOutput = $@"string key = ""{Convert.ToBase64String(key)}"";
string iv = ""{Convert.ToBase64String(iv)}"";
string encryptedBase64 = ""{encryptedBase64}"";

byte[] decryptedBytes;
using (Aes aesAlg = Aes.Create())
{{
    aesAlg.Key = Convert.FromBase64String(key);
    aesAlg.IV = Convert.FromBase64String(iv);
    aesAlg.Padding = PaddingMode.PKCS7;
    aesAlg.Mode = CipherMode.CBC;

    ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
    byte[] encryptedBytesArray = Convert.FromBase64String(encryptedBase64);
    decryptedBytes = decryptor.TransformFinalBlock(encryptedBytesArray, 0, encryptedBytesArray.Length);
}}";

        Console.WriteLine(csharpOutput);

        Console.WriteLine("\n[i] PowerShell code to decrypt shellcode:\n");

        string powershellOutput = $@"$Key = [System.Convert]::FromBase64String(""{Convert.ToBase64String(key)}"")
$IV = [System.Convert]::FromBase64String(""{Convert.ToBase64String(iv)}"")
$EncryptedBytes = [System.Convert]::FromBase64String(""{encryptedBase64}"")

$AesManaged = New-Object Security.Cryptography.AesManaged
$AesManaged.Key = $Key
$AesManaged.IV = $IV
$AesManaged.Mode = [Security.Cryptography.CipherMode]::CBC
$AesManaged.Padding = [Security.Cryptography.PaddingMode]::PKCS7
$Decryptor = $AesManaged.CreateDecryptor($AesManaged.Key, $AesManaged.IV)

$DecryptedBytes = $Decryptor.TransformFinalBlock($EncryptedBytes, 0, $EncryptedBytes.Length)
    
[Byte[]] $buf = $DecryptedBytes
";

        Console.WriteLine(powershellOutput);
    }
}