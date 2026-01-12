using System.Security.Cryptography;
using SystemZip = System.IO.Compression;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
//using Ionic.Zip;

/*
 * The file is for implementation of my versions of GHTCP Methods 
 */

namespace GH_Toolkit_Core.Methods
{
    public class GHTCP
    {
        // The Decrypt AES methods are ported from Onyx Toolkit by Michael Tolly (who in turn ported it from C# to Haskell)
        // It's a perfect loop!

        private static readonly byte[] sghSetlistKey = [ 61, 250, 11, 73, 254, 154, 8, 191, 18, 188, 243, 32, 246, 40, 148, 145, 62, 219, 250, 196, 15, 63, 217, 91, 29, 73, 8, 22, 197, 186, 176, 81 ];
        private static readonly byte[] sghSetlistIV = [ 18, 188, 243, 32, 246, 40, 148, 145, 247, 74, 25, 30, 94, 26, 230, 111 ];
        private static readonly byte[] sghSongsKey = [ 45, 219, 244, 185, 119, 192, 19, 251, 134, 93, 62, 50, 245, 33, 177, 178, 192, 184, 16, 30, 114, 253, 61, 49, 248, 198, 204, 123, 91, 48, 188, 103 ];
        private static readonly byte[] sghSongsIV = [ 134, 93, 62, 50, 245, 33, 177, 178, 239, 48, 31, 166, 143, 179, 180, 49 ];
        private static readonly string sghZipPassword = "SGH9ZIP2PASS4MXKR";
        private static readonly string tghZipPassword = "TGH9ZIP2PASS4MXKR";


        public static byte[] DecryptSetlist(byte[] dataToDecrypt)
        {
            return DecryptAES(dataToDecrypt, sghSetlistKey, sghSetlistIV);
        }

        public static byte[] DecryptSongs(byte[] dataToDecrypt)
        {
            return DecryptAES(dataToDecrypt, sghSongsKey, sghSongsIV);
        }
        public static void ExtractSghZip(string zipPath, string extractPath, out bool isEncrypted)
        {
            ExtractZip(zipPath, extractPath, out isEncrypted, sghZipPassword);
        }

        public static void ExtractSongsFromSgh(string zipPath, string extractPath, out bool isEncrypted)
        {
            ExtractSongs(zipPath, extractPath, out isEncrypted, sghZipPassword);
        }

        public static void ExtractTghZip(string zipPath, string extractPath, out bool isEncrypted)
        {
            ExtractZip(zipPath, extractPath, out isEncrypted, tghZipPassword);
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
            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, cryptoTransform, CryptoStreamMode.Write))
                {
                    cs.Write(data, 0, data.Length);
                    cs.FlushFinalBlock();
                }
                return ms.ToArray();
            }
        }
        public static void MakeUnprotectedZip(string extractPath, string zipPath)
        {
            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }
            SystemZip.ZipFile.CreateFromDirectory(extractPath, zipPath);
        }
        private static void ExtractZip(string zipPath, string extractPath, out bool isEncrypted, string password = "")
        {
            /*
            isEncrypted = false;
            using (ZipFile zip = ZipFile.Read(zipPath))
            {
                try
                {
                    zip.ExtractAll(extractPath, ExtractExistingFileAction.OverwriteSilently);
                }
                catch (BadPasswordException)
                {
                    isEncrypted = true;
                    zip.Password = password;
                    zip.ExtractAll(extractPath, ExtractExistingFileAction.OverwriteSilently);
                }
                catch (Exception e)
                {
                    Console.WriteLine("An error occurred while extracting the zip file: " + e.Message);
                    throw;
                }
            }*/
            isEncrypted = false;

            try
            {
                using (FileStream fs = File.OpenRead(zipPath))
                using (ZipFile zipFile = new ZipFile(fs))
                {
                    if (!string.IsNullOrEmpty(password))
                    {
                        zipFile.Password = password; // Set the password for potentially encrypted files
                    }

                    bool passwordUsed = false;

                    foreach (ZipEntry entry in zipFile)
                    {
                        if (!entry.IsFile) continue; // Skip directories

                        // Check if the entry is encrypted
                        if (entry.IsCrypted && !string.IsNullOrEmpty(password))
                        {
                            passwordUsed = true; // Mark that a password was needed for extraction
                        }

                        string entryFileName = entry.Name;
                        byte[] buffer = new byte[4096]; // Buffer size

                        // Create output directory structure if necessary
                        string fullPath = Path.Combine(extractPath, entryFileName);
                        string directoryName = Path.GetDirectoryName(fullPath);
                        if (directoryName.Length > 0)
                        {
                            Directory.CreateDirectory(directoryName);
                        }

                        // Extract file
                        using (Stream zipStream = zipFile.GetInputStream(entry))
                        using (FileStream streamWriter = File.Create(fullPath))
                        {
                            StreamUtils.Copy(zipStream, streamWriter, buffer);
                        }
                    }

                    // Set isEncrypted to true if the archive was password-protected and the password was used
                    isEncrypted = passwordUsed;
                }
            }
            catch (ZipException ex) when (ex.Message.Contains("Wrong password"))
            {
                Console.WriteLine("The zip file is encrypted, and an incorrect or no password was provided.");
                throw;
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred while extracting the zip file: " + e.Message);
                throw;
            }
        }

        private static void ExtractSongs(string zipPath, string extractPath, out bool isEncrypted, string password = "")
        {
            /*
            string songs = "songs.info";
            isEncrypted = false;

            using (ZipFile zip = ZipFile.Read(zipPath))
            {
                var songsFile = zip[songs];
                if (songsFile == null)
                {
                    throw new Exception("The SGH file does not contain a songs.info file");
                }
                try
                {
                    songsFile.Extract(extractPath, ExtractExistingFileAction.OverwriteSilently);
                }
                catch (BadPasswordException)
                {
                    isEncrypted = true;
                    songsFile.Password = password;
                    songsFile.Extract(extractPath, ExtractExistingFileAction.OverwriteSilently);
                }
                catch (Exception e)
                {
                    Console.WriteLine("An error occurred while extracting the zip file: " + e.Message);
                    throw;
                }
            }*/
            string songs = "songs.info";
            isEncrypted = false;

            try
            {
                using (FileStream fs = File.OpenRead(zipPath))
                using (ZipFile zipFile = new ZipFile(fs))
                {
                    if (!string.IsNullOrEmpty(password))
                    {
                        zipFile.Password = password; // Set the password for potentially encrypted files
                    }

                    ZipEntry songsFile = zipFile.GetEntry(songs);
                    if (songsFile == null)
                    {
                        throw new Exception("The SGH file does not contain a songs.info file");
                    }

                    bool passwordUsed = false;

                    // Check if the entry is encrypted
                    if (songsFile.IsCrypted && !string.IsNullOrEmpty(password))
                    {
                        passwordUsed = true;
                    }

                    Directory.CreateDirectory(extractPath);

                    // Extract the songs.info file
                    string outputPath = Path.Combine(extractPath, songs);
                    byte[] buffer = new byte[4096]; // Buffer size

                    using (Stream zipStream = zipFile.GetInputStream(songsFile))
                    using (FileStream streamWriter = File.Create(outputPath))
                    {
                        StreamUtils.Copy(zipStream, streamWriter, buffer);
                    }

                    // Set isEncrypted to true if the archive was password-protected and the password was used
                    isEncrypted = passwordUsed;
                }
            }
            catch (ZipException ex) when (ex.Message.Contains("Wrong password"))
            {
                Console.WriteLine("The zip file is encrypted, and an incorrect or no password was provided.");
                throw;
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred while extracting the zip file: " + e.Message);
                throw;
            }
        }
    }
}
