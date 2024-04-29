using System.Security.Cryptography;

/*
 * The file is for implementation of my versions of GHTCP Methods 
 */

namespace GH_Toolkit_Core.Methods
{
    public class GHTCP
    {
        // The private methods are ported from Onyx Toolkit by Michael Tolly (who in turn ported it from C# to Haskell)
        // It's a perfect loop!

        private static readonly byte[] sghSetlistKey = [ 61, 250, 11, 73, 254, 154, 8, 191, 18, 188, 243, 32, 246, 40, 148, 145, 62, 219, 250, 196, 15, 63, 217, 91, 29, 73, 8, 22, 197, 186, 176, 81 ];
        private static readonly byte[] sghSetlistIV = [ 18, 188, 243, 32, 246, 40, 148, 145, 247, 74, 25, 30, 94, 26, 230, 111 ];
        private static readonly byte[] sghSongsKey = [ 45, 219, 244, 185, 119, 192, 19, 251, 134, 93, 62, 50, 245, 33, 177, 178, 192, 184, 16, 30, 114, 253, 61, 49, 248, 198, 204, 123, 91, 48, 188, 103 ];
        private static readonly byte[] sghSongsIV = [ 134, 93, 62, 50, 245, 33, 177, 178, 239, 48, 31, 166, 143, 179, 180, 49 ];
        
        public static byte[] DecryptSetlist(byte[] dataToDecrypt)
        {
            return DecryptAES(dataToDecrypt, sghSetlistKey, sghSetlistIV);
        }
        public static byte[] DecryptSongs(byte[] dataToDecrypt)
        {
            return DecryptAES(dataToDecrypt, sghSongsKey, sghSongsIV);
        }

        private static byte[] DecryptAES(byte[] dataToDecrypt, byte[] key, byte[] iv)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.IV = iv;
                aesAlg.Mode = CipherMode.CBC;
                aesAlg.Padding = PaddingMode.PKCS7; 

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                try
                {
                    return PerformCryptography(dataToDecrypt, decryptor);
                }
                catch (CryptographicException e)
                {
                    Console.WriteLine("A Cryptographic error occurred: " + e.Message);
                    return null;
                }
            }
        }

        private static byte[] PerformCryptography(byte[] data, ICryptoTransform cryptoTransform)
        {
            using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, cryptoTransform, CryptoStreamMode.Write))
                {
                    cs.Write(data, 0, data.Length);
                    cs.FlushFinalBlock();
                }
                return ms.ToArray();
            }
        }
    }
}
