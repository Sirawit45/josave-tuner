using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Threading;
using System.IO.Ports;

namespace RemixHondaFlash
{
    #region Enums & Data Structures

    public enum EcuType
    {
        Unknown = -1,
        KeihinOther = 0,
        KeihinH8C = 1,
        KeihinM32R = 2,
        ShindengenV850 = 3,
        ShindengenRH850 = 4,
        ShindengenRH850B = 5
    }

    public enum EcuVendor
    {
        Unknown,
        Keihin,
        Shindengen
    }

    public class EcmInfo
    {
        public int Index { get; set; }
        public string PartCode { get; set; }
        public string EcmId { get; set; }
        public string ModelName { get; set; }
        public int RomSizeKb { get; set; }
        public int StartOffset { get; set; }
        public int ChecksumOffset { get; set; }
        public EcuVendor Vendor { get; set; }
        public EcuType DetectedType { get; set; }

        public override string ToString()
        {
            return string.Format("[{0}] {1} - {2} (ID: {3})", Vendor, ModelName, PartCode, EcmId);
        }
    }

    public class FlashProgressEventArgs : EventArgs
    {
        public int BytesProcessed { get; set; }
        public int TotalBytes { get; set; }
        public double Percentage { get; set; }
        public double SpeedKbps { get; set; }
        public TimeSpan Elapsed { get; set; }
        public string StatusMessage { get; set; }
        public bool IsReading { get; set; }
    }

    #endregion

    #region Checksum & Crypto Utilities

    public static class ChecksumEngine
    {
        /// <summary>
        /// Calculates standard 8-bit sum (sum modulo 256)
        /// </summary>
        public static byte Sum8Bit(byte[] data, int dataSize, int startPos = 0)
        {
            if (data == null || dataSize <= 0 || startPos < 0 || startPos + dataSize > data.Length)
                return 0;

            int end = startPos + dataSize;
            byte sum = 0;
            for (int i = startPos; i < end; i++)
            {
                sum = (byte)(sum + data[i]);
            }
            return sum;
        }

        /// <summary>
        /// Calculates 32-bit sum of little-endian 4-byte words
        /// </summary>
        public static uint Sum32Bit(byte[] data, int dataSize, int startPos = 0)
        {
            if (data == null || dataSize <= 0 || startPos < 0 || startPos + dataSize > data.Length)
                return 0;

            int end = startPos + dataSize;
            uint sum = 0;
            for (int i = startPos; i + 4 <= end; i += 4)
            {
                sum += BitConverter.ToUInt32(data, i);
            }
            return sum;
        }

        /// <summary>
        /// Calculates Honda K-Line 8-bit checksum: 2's complement of sum (0x100 - sum)
        /// </summary>
        public static byte Checksum8BitHonda(byte[] data, int dataSize, int startPos = 0)
        {
            if (data == null || dataSize <= 0 || startPos < 0 || startPos + dataSize > data.Length)
                return 0;

            int end = startPos + dataSize;
            int sum = 0;
            for (int i = startPos; i < end; i++)
            {
                sum += data[i];
            }
            return (byte)(((sum ^ 0xFF) + 1) & 0xFF);
        }

        /// <summary>
        /// Validates packet checksum (last byte equals Honda Checksum of preceding bytes)
        /// </summary>
        public static bool ValidateChecksum(byte[] data, int dataSize)
        {
            if (data == null || dataSize < 2) return false;
            byte expected = Checksum8BitHonda(data, dataSize - 1, 0);
            return expected == data[dataSize - 1];
        }

        /// <summary>
        /// Calculates and updates Keihin 8-bit total checksum
        /// </summary>
        public static void FixKeihinChecksum(byte[] binBytes, int checksumOffset)
        {
            if (binBytes == null || checksumOffset < 0 || checksumOffset >= binBytes.Length) return;

            byte sum;
            if (checksumOffset == 0)
            {
                sum = Sum8Bit(binBytes, binBytes.Length - 1, 1);
            }
            else
            {
                byte sumBefore = Sum8Bit(binBytes, checksumOffset, 0);
                byte sumAfter = Sum8Bit(binBytes, binBytes.Length - (checksumOffset + 1), checksumOffset + 1);
                sum = (byte)(sumBefore + sumAfter);
            }

            binBytes[checksumOffset] = (byte)((-sum) & 0xFF);
        }

        /// <summary>
        /// Calculates and updates Shindengen 32-bit total checksum
        /// </summary>
        public static bool FixShindengenChecksum(byte[] binBytes, out int checksumOffset)
        {
            checksumOffset = -1;
            if (binBytes == null || binBytes.Length < 16) return false;

            byte[] shWKeysig = new byte[] { 0x11, 0x12, 0x13, 0x14, 0x15, 0x15, 0x17 };
            int sigIndex = PatternSearch(binBytes, shWKeysig);
            if (sigIndex < 0) return false;

            int loc2 = sigIndex + shWKeysig.Length;
            int foundLoc = -1;

            while (loc2 < binBytes.Length - 6)
            {
                if (binBytes[loc2] == 0xD0 && binBytes[loc2 + 1] == 0x07)
                {
                    foundLoc = loc2;
                    if ((loc2 & 3) != 0)
                    {
                        foundLoc = loc2 - (loc2 % 4); // Align
                    }
                    break;
                }
                loc2++;
            }

            if (foundLoc < 4) return false;

            checksumOffset = foundLoc - 4;
            uint sumBefore = Sum32Bit(binBytes, checksumOffset, 0);
            uint sumAfter = Sum32Bit(binBytes, binBytes.Length - foundLoc, foundLoc);
            uint totalSum = sumBefore + sumAfter;
            uint finalChecksum = (uint)(0 - totalSum);

            byte[] ckBytes = BitConverter.GetBytes(finalChecksum);
            Array.Copy(ckBytes, 0, binBytes, checksumOffset, 4);
            return true;
        }

        public static int PatternSearch(byte[] data, byte[] pattern, int start = 0)
        {
            if (data == null || pattern == null || data.Length < pattern.Length) return -1;
            for (int i = start; i <= data.Length - pattern.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (data[i + j] != pattern[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match) return i;
            }
            return -1;
        }
    }

    public static class SeedKeyEngine
    {
        public static readonly byte[] ShindengenWriteKeySig = new byte[] { 0x11, 0x12, 0x13, 0x14, 0x15, 0x15, 0x17 };
        public static readonly byte[] ShindengenRecoveryKeySig = new byte[] { 0x91, 0x91, 0x0D, 0x00, 0x9E, 0x8D };

        public static byte Ar6(byte b)
        {
            byte high = (byte)(b >> 4);
            byte low = (byte)(b & 0x0F);

            switch (high)
            {
                case 3: high = 11; break;
                case 4: high = 12; break;
                case 5: high = 13; break;
                case 6: high = 14; break;
                case 11: high = 3; break;
                case 12: high = 4; break;
                case 13: high = 5; break;
                case 14: high = 6; break;
            }

            switch (low)
            {
                case 0: low = 6; break;
                case 1: low = 5; break;
                case 2: low = 4; break;
                case 4: low = 10; break;
                case 5: low = 9; break;
                case 6: low = 8; break;
                case 8: low = 14; break;
                case 9: low = 13; break;
                case 10: low = 12; break;
                case 12: low = 2; break;
                case 13: low = 1; break;
                case 14: low = 0; break;
            }

            return (byte)((high << 4) | low);
        }

        public static byte[] ArG(byte[] data, int param0, int param1)
        {
            byte[] result = new byte[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                int high = ((data[i] >> 4) + param0) << 4;
                int low = ((data[i] % 16) + param1) % 16;
                byte v1 = Ar6((byte)(high + low));

                byte v2;
                if (v1 == 15)
                {
                    v2 = 255;
                }
                else if (v1 == 31 || v1 == 47 || v1 == 63 || v1 == 79 || v1 == 95 || v1 == 111 ||
                         v1 == 127 || v1 == 143 || v1 == 159 || v1 == 175 || v1 == 191 || v1 == 207 ||
                         v1 == 223 || v1 == 239)
                {
                    v2 = (byte)(v1 - 16);
                }
                else if (v1 == 255)
                {
                    v2 = 255;
                }
                else
                {
                    v2 = (byte)(v1 - 16);
                }
                result[i] = v2;
            }
            return result;
        }

        public static bool GetShindengenWriteKey(byte[] binBytes, byte[] seedBytes, out byte[] keyBytes)
        {
            keyBytes = new byte[2];
            if (binBytes == null || seedBytes == null || seedBytes.Length < 2) return false;

            int sigIdx = ChecksumEngine.PatternSearch(binBytes, ShindengenWriteKeySig);
            if (sigIdx < 0) return false;

            int loc2 = sigIdx + ShindengenWriteKeySig.Length;
            while (loc2 < binBytes.Length - 10)
            {
                if (binBytes[loc2 + 8] == 0xEE && (binBytes[loc2 + 9] == 0x00 || binBytes[loc2 + 9] == 0xFF))
                {
                    ushort seedVal = (ushort)((seedBytes[0] << 8) | seedBytes[1]);
                    ushort v5 = (ushort)(binBytes[loc2] | (binBytes[loc2 + 1] << 8));
                    ushort v6 = (ushort)(binBytes[loc2 + 2] | (binBytes[loc2 + 3] << 8));
                    ushort v7 = (ushort)(binBytes[loc2 + 4] | (binBytes[loc2 + 5] << 8));

                    ushort v8 = (ushort)(v6 * (seedVal + v5));
                    ushort keyVal;
                    if (v7 > 0)
                    {
                        keyVal = (ushort)(v8 + (seedVal % v7));
                    }
                    else
                    {
                        keyVal = (ushort)(v8 + seedVal);
                    }

                    keyBytes[0] = (byte)((keyVal >> 8) & 0xFF);
                    keyBytes[1] = (byte)(keyVal & 0xFF);
                    return true;
                }
                loc2++;
            }
            return false;
        }

        public static bool GetShindengenRecoveryKey(byte[] binBytes, byte[] seedBytes, out byte[] keyBytes)
        {
            keyBytes = new byte[2];
            if (binBytes == null || seedBytes == null || seedBytes.Length < 2) return false;

            int sigIdx = ChecksumEngine.PatternSearch(binBytes, ShindengenRecoveryKeySig);
            if (sigIdx < 6) return false;

            int loc0 = sigIdx - 6;
            ushort seedVal = (ushort)((seedBytes[0] << 8) | seedBytes[1]);
            ushort loc2 = (ushort)(binBytes[loc0] | (binBytes[loc0 + 1] << 8));
            ushort loc3 = (ushort)(binBytes[loc0 + 2] | (binBytes[loc0 + 3] << 8));
            ushort loc4 = (ushort)(binBytes[loc0 + 4] | (binBytes[loc0 + 5] << 8));

            ushort v5 = (ushort)(loc3 * (seedVal + loc2));
            ushort keyVal;
            if (loc4 > 0)
            {
                keyVal = (ushort)(v5 + (seedVal % loc4));
            }
            else
            {
                keyVal = (ushort)(v5 + seedVal);
            }

            keyBytes[0] = (byte)((keyVal >> 8) & 0xFF);
            keyBytes[1] = (byte)(keyVal & 0xFF);
            return true;
        }
    }

    #endregion

    #region Auxiliary Tools: Password Cleaner & File Converter

    public static class TunerProPasswordCleaner
    {
        private const string MasterKey = "$9Ad0$!#*)1QqPow^";

        public static bool DecryptAndClean(string filePath, out string openPassword, out string modifyPassword, out string cleanXml)
        {
            openPassword = "";
            modifyPassword = "";
            cleanXml = "";

            if (!File.Exists(filePath)) return false;
            byte[] fileBytes = File.ReadAllBytes(filePath);

            try
            {
                byte[] decrypted = DecryptCryptoApi(fileBytes, MasterKey);
                string text = Encoding.UTF8.GetString(decrypted).Replace("\r", "");

                // Check for TunerPro headers
                if (!text.Contains("ADXHEADER") && !text.Contains("XDFHEADER"))
                {
                    // Maybe not encrypted or already plain text
                    text = Encoding.UTF8.GetString(fileBytes).Replace("\r", "");
                    if (!text.Contains("ADXHEADER") && !text.Contains("XDFHEADER"))
                    {
                        return false;
                    }
                }

                // Parse open password
                int opStart = text.IndexOf("<openpassword>");
                if (opStart >= 0)
                {
                    int opEnd = text.IndexOf("</openpassword>", opStart);
                    if (opEnd >= 0)
                    {
                        openPassword = text.Substring(opStart + 14, opEnd - (opStart + 14)).Trim();
                    }
                }

                // Parse modify / edit password
                int mpStart = text.IndexOf("<modifypassword>");
                if (mpStart >= 0)
                {
                    int mpEnd = text.IndexOf("</modifypassword>", mpStart);
                    if (mpEnd >= 0)
                    {
                        modifyPassword = text.Substring(mpStart + 16, mpEnd - (mpStart + 16)).Trim();
                    }
                }
                else
                {
                    int epStart = text.IndexOf("<editpassword>");
                    if (epStart >= 0)
                    {
                        int epEnd = text.IndexOf("</editpassword>", epStart);
                        if (epEnd >= 0)
                        {
                            modifyPassword = text.Substring(epStart + 14, epEnd - (epStart + 14)).Trim();
                        }
                    }
                }

                // Clean passwords and reset flags
                string cleaned = text;
                cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"<openpassword>.*?</openpassword>", "");
                cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"<modifypassword>.*?</modifypassword>", "");
                cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"<editpassword>.*?</editpassword>", "");

                if (filePath.EndsWith(".adx", StringComparison.OrdinalIgnoreCase))
                {
                    cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"<flags>0x[0-9a-fA-F]+</flags>", "<flags>0x1</flags>");
                }
                else
                {
                    cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"<flags>0x[0-9a-fA-F]+</flags>", "<flags>0x10000</flags>");
                }

                cleanXml = cleaned;
                return true;
            }
            catch
            {
                return false;
            }
        }

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool CryptAcquireContext(out IntPtr phProv, string pszContainer, string pszProvider, uint dwProvType, uint dwFlags);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool CryptCreateHash(IntPtr hProv, uint Algid, IntPtr hKey, uint dwFlags, out IntPtr phHash);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool CryptHashData(IntPtr hHash, byte[] pbData, uint dwDataLen, uint dwFlags);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool CryptDeriveKey(IntPtr hProv, uint Algid, IntPtr hBaseData, uint dwFlags, out IntPtr phKey);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool CryptDecrypt(IntPtr hKey, IntPtr hHash, bool Final, uint dwFlags, byte[] pbData, ref uint pdwDataLen);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool CryptDestroyKey(IntPtr hKey);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool CryptDestroyHash(IntPtr hHash);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool CryptReleaseContext(IntPtr hProv, uint dwFlags);

        private const uint PROV_RSA_FULL = 1;
        private const uint CRYPT_VERIFYCONTEXT = 0xF0000000;
        private const uint CALG_MD5 = 0x00008003;
        private const uint CALG_RC4 = 0x00006801;

        private static byte[] DecryptCryptoApi(byte[] data, string password)
        {
            IntPtr hProv = IntPtr.Zero;
            IntPtr hHash = IntPtr.Zero;
            IntPtr hKey = IntPtr.Zero;
            byte[] buffer = (byte[])data.Clone();
            uint len = (uint)buffer.Length;

            try
            {
                if (!CryptAcquireContext(out hProv, null, "Microsoft Enhanced Cryptographic Provider v1.0", PROV_RSA_FULL, CRYPT_VERIFYCONTEXT))
                {
                    CryptAcquireContext(out hProv, null, null, PROV_RSA_FULL, CRYPT_VERIFYCONTEXT);
                }

                CryptCreateHash(hProv, CALG_MD5, IntPtr.Zero, 0, out hHash);
                byte[] passBytes = Encoding.ASCII.GetBytes(password);
                CryptHashData(hHash, passBytes, (uint)passBytes.Length, 0);
                CryptDeriveKey(hProv, CALG_RC4, hHash, 0, out hKey);
                CryptDecrypt(hKey, IntPtr.Zero, true, 0, buffer, ref len);

                byte[] res = new byte[len];
                Array.Copy(buffer, 0, res, 0, len);
                return res;
            }
            finally
            {
                if (hKey != IntPtr.Zero) CryptDestroyKey(hKey);
                if (hHash != IntPtr.Zero) CryptDestroyHash(hHash);
                if (hProv != IntPtr.Zero) CryptReleaseContext(hProv, 0);
            }
        }
    }

    public static class FileConverterEngine
    {
        private const string EcuKey = "@ecu_homdaa!&2023*mnhdaK#^&hcbaHBQD@0lanmBV#!";
        private const string AcgKey = "@Shinden@9919";

        public static byte[] ConvertEcuToBin(string ecuFilePath)
        {
            string base64 = File.ReadAllText(ecuFilePath).Trim();
            string decryptedStr = DecryptEcuString(base64, EcuKey);
            return ConvertHexOrBinaryStringToBytes(decryptedStr);
        }

        public static byte[] ConvertAcgToBin(string acgFilePath)
        {
            string base64 = File.ReadAllText(acgFilePath).Trim();
            string decryptedStr = DecryptAcgString(base64, AcgKey);
            return ConvertHexOrBinaryStringToBytes(decryptedStr);
        }

        private static string DecryptEcuString(string base64Content, string key)
        {
            using (RijndaelManaged rm = new RijndaelManaged())
            using (MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider())
            {
                byte[] keyHash = md5.ComputeHash(Encoding.ASCII.GetBytes(key));
                byte[] key32 = new byte[32];
                Array.Copy(keyHash, 0, key32, 0, 16);
                Array.Copy(keyHash, 0, key32, 15, 16);

                rm.Key = key32;
                rm.Mode = CipherMode.CBC;
                rm.IV = new byte[16];

                using (ICryptoTransform decryptor = rm.CreateDecryptor())
                {
                    byte[] cipherBytes = Convert.FromBase64String(base64Content);
                    byte[] plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
                    return Encoding.UTF8.GetString(plainBytes);
                }
            }
        }

        private static string DecryptAcgString(string base64Content, string key)
        {
            using (RijndaelManaged rm = new RijndaelManaged())
            using (MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider())
            {
                byte[] keyHash = md5.ComputeHash(Encoding.BigEndianUnicode.GetBytes(key));
                byte[] key32 = new byte[32];
                Array.Copy(keyHash, 0, key32, 6, 16);
                Array.Copy(keyHash, 0, key32, 2, 16);

                rm.Key = key32;
                rm.Mode = CipherMode.CBC;
                rm.IV = new byte[16];

                using (ICryptoTransform decryptor = rm.CreateDecryptor())
                {
                    byte[] cipherBytes = Convert.FromBase64String(base64Content);
                    byte[] plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
                    return Encoding.UTF8.GetString(plainBytes);
                }
            }
        }

        public static string EncryptEcuString(string plainText, string key = EcuKey)
        {
            using (RijndaelManaged rm = new RijndaelManaged())
            using (MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider())
            {
                byte[] keyHash = md5.ComputeHash(Encoding.ASCII.GetBytes(key));
                byte[] key32 = new byte[32];
                Array.Copy(keyHash, 0, key32, 0, 16);
                Array.Copy(keyHash, 0, key32, 15, 16);

                rm.Key = key32;
                rm.Mode = CipherMode.CBC;
                rm.IV = new byte[16];

                using (ICryptoTransform encryptor = rm.CreateEncryptor())
                {
                    byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                    byte[] cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                    return Convert.ToBase64String(cipherBytes);
                }
            }
        }

        public static string EncryptAcgString(string plainText, string key = AcgKey)
        {
            using (RijndaelManaged rm = new RijndaelManaged())
            using (MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider())
            {
                byte[] keyHash = md5.ComputeHash(Encoding.BigEndianUnicode.GetBytes(key));
                byte[] key32 = new byte[32];
                Array.Copy(keyHash, 0, key32, 6, 16);
                Array.Copy(keyHash, 0, key32, 2, 16);

                rm.Key = key32;
                rm.Mode = CipherMode.CBC;
                rm.IV = new byte[16];

                using (ICryptoTransform encryptor = rm.CreateEncryptor())
                {
                    byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                    byte[] cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                    return Convert.ToBase64String(cipherBytes);
                }
            }
        }

        private static byte[] ConvertHexOrBinaryStringToBytes(string text)
        {
            if (string.IsNullOrEmpty(text)) return new byte[0];
            text = text.Trim('\0', ' ', '\r', '\n', '\t').Replace(" ", "").Replace("\r", "").Replace("\n", "").Replace("-", "");
            if (text.Length % 2 != 0) return Encoding.UTF8.GetBytes(text);

            try
            {
                byte[] bytes = new byte[text.Length / 2];
                for (int i = 0; i < bytes.Length; i++)
                {
                    bytes[i] = Convert.ToByte(text.Substring(i * 2, 2), 16);
                }
                return bytes;
            }
            catch
            {
                return Encoding.UTF8.GetBytes(text);
            }
        }
    }

    #endregion

    #region Hardware Drivers: FTDI D2XX, SerialPort & Virtual Simulator

    public class DetectedHardware
    {
        public bool IsCableConnected { get; set; }
        public string CableName { get; set; }
        public string PortName { get; set; }
        public string SerialNumber { get; set; }
        public bool IsFtdiDirect { get; set; }

        public override string ToString()
        {
            if (IsFtdiDirect)
            {
                return string.IsNullOrEmpty(SerialNumber) ? CableName : string.Format("{0} [{1}]", CableName, SerialNumber);
            }
            return CableName;
        }
    }

    public static class HardwareDetector
    {
        [DllImport("ftd2xx.dll")]
        private static extern int FT_CreateDeviceInfoList(ref uint numdevs);

        [DllImport("ftd2xx.dll")]
        private static extern int FT_GetDeviceInfoDetail(uint index, ref uint flags, ref uint type, ref uint id, ref uint locId, byte[] serialNumber, byte[] description, ref IntPtr ftHandle);

        private static string GetAsciiNullTerminated(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return "";
            int nullIdx = Array.IndexOf(bytes, (byte)0);
            if (nullIdx >= 0)
            {
                return Encoding.ASCII.GetString(bytes, 0, nullIdx).Trim();
            }
            return Encoding.ASCII.GetString(bytes).Trim('\0', ' ');
        }

        public static List<DetectedHardware> DetectAllHardware()
        {
            var list = new List<DetectedHardware>();

            // 1. FTDI Direct Devices
            try
            {
                uint numDevs = 0;
                int status = FT_CreateDeviceInfoList(ref numDevs);
                if (status == 0 && numDevs > 0)
                {
                    for (uint i = 0; i < numDevs; i++)
                    {
                        uint flags = 0, type = 0, id = 0, locId = 0;
                        byte[] sn = new byte[64];
                        byte[] desc = new byte[64];
                        IntPtr handle = IntPtr.Zero;

                        if (FT_GetDeviceInfoDetail(i, ref flags, ref type, ref id, ref locId, sn, desc, ref handle) == 0)
                        {
                            string descStr = GetAsciiNullTerminated(desc);
                            string snStr = GetAsciiNullTerminated(sn);
                            list.Add(new DetectedHardware
                            {
                                IsCableConnected = true,
                                CableName = string.IsNullOrEmpty(descStr) ? "FTDI K-Line Cable" : descStr,
                                SerialNumber = snStr,
                                PortName = "FTDI D2XX (Direct)",
                                IsFtdiDirect = true
                            });
                        }
                    }
                }
            }
            catch { }

            // 2. Windows Serial COM Ports
            try
            {
                foreach (string port in SerialPort.GetPortNames())
                {
                    list.Add(new DetectedHardware
                    {
                        IsCableConnected = true,
                        CableName = "USB Serial Port (" + port + ")",
                        SerialNumber = "",
                        PortName = port,
                        IsFtdiDirect = false
                    });
                }
            }
            catch { }

            return list;
        }
    }

    public interface IKLinePort : IDisposable
    {
        bool Open();
        void Close();
        bool IsOpen { get; }
        bool InitKLineFast();
        void SetBaudRate(int baud);
        void SetTimeouts(int readMs, int writeMs);
        void Purge();
        int Write(byte[] buffer, int count);
        int Read(byte[] buffer, int count);
        int BytesAvailable { get; }
    }

    public class FtdiD2xxPort : IKLinePort
    {
        private IntPtr ftHandle = IntPtr.Zero;
        private int deviceIndex = 0;

        [DllImport("ftd2xx.dll")]
        private static extern int FT_Open(int deviceNumber, ref IntPtr ftHandle);

        [DllImport("ftd2xx.dll")]
        private static extern int FT_Close(IntPtr ftHandle);

        [DllImport("ftd2xx.dll")]
        private static extern int FT_Purge(IntPtr ftHandle, uint mask);

        [DllImport("ftd2xx.dll")]
        private static extern int FT_SetBitMode(IntPtr ftHandle, byte mask, byte mode);

        [DllImport("ftd2xx.dll")]
        private static extern int FT_SetDataCharacteristics(IntPtr ftHandle, byte wordLength, byte stopBits, byte parity);

        [DllImport("ftd2xx.dll")]
        private static extern int FT_SetBaudRate(IntPtr ftHandle, uint baudRate);

        [DllImport("ftd2xx.dll")]
        private static extern int FT_SetTimeouts(IntPtr ftHandle, uint readTimeout, uint writeTimeout);

        [DllImport("ftd2xx.dll")]
        private static extern int FT_SetLatencyTimer(IntPtr ftHandle, byte latency);

        [DllImport("ftd2xx.dll")]
        private static extern int FT_GetQueueStatus(IntPtr ftHandle, ref uint amountInRxQueue);

        [DllImport("ftd2xx.dll")]
        private static extern int FT_Read(IntPtr ftHandle, byte[] lpBuffer, uint bytesToRead, ref uint bytesRead);

        [DllImport("ftd2xx.dll")]
        private static extern int FT_Write(IntPtr ftHandle, byte[] lpBuffer, uint bytesToWrite, ref uint bytesWritten);

        public FtdiD2xxPort(int deviceIndex = 0)
        {
            this.deviceIndex = deviceIndex;
        }

        public bool IsOpen { get { return ftHandle != IntPtr.Zero; } }

        public bool Open()
        {
            if (IsOpen) Close();
            int res = FT_Open(deviceIndex, ref ftHandle);
            return res == 0 && ftHandle != IntPtr.Zero;
        }

        public void Close()
        {
            if (ftHandle != IntPtr.Zero)
            {
                FT_Close(ftHandle);
                ftHandle = IntPtr.Zero;
            }
        }

        public bool InitKLineFast()
        {
            if (!IsOpen && !Open()) return false;

            FT_Purge(ftHandle, 3);
            FT_SetBitMode(ftHandle, 0, 0);
            FT_SetDataCharacteristics(ftHandle, 8, 0, 0);
            FT_SetBaudRate(ftHandle, 10400);
            FT_SetTimeouts(ftHandle, 50, 0);
            FT_SetLatencyTimer(ftHandle, 8);

            FT_SetBitMode(ftHandle, 1, 1);
            byte[] low = new byte[] { 0x00 };
            uint written = 0;
            FT_Write(ftHandle, low, 1, ref written);
            Thread.Sleep(70);

            byte[] high = new byte[] { 0x01 };
            FT_Write(ftHandle, high, 1, ref written);

            FT_SetBitMode(ftHandle, 0, 0);
            FT_SetBaudRate(ftHandle, 10400);
            FT_Purge(ftHandle, 3);
            Thread.Sleep(130);

            return true;
        }

        public void SetBaudRate(int baud)
        {
            if (IsOpen) FT_SetBaudRate(ftHandle, (uint)baud);
        }

        public void SetTimeouts(int readMs, int writeMs)
        {
            if (IsOpen) FT_SetTimeouts(ftHandle, (uint)readMs, (uint)writeMs);
        }

        public void Purge()
        {
            if (IsOpen) FT_Purge(ftHandle, 3);
        }

        public int Write(byte[] buffer, int count)
        {
            if (!IsOpen) return 0;
            uint written = 0;
            FT_Write(ftHandle, buffer, (uint)count, ref written);
            return (int)written;
        }

        public int Read(byte[] buffer, int count)
        {
            if (!IsOpen) return 0;
            uint read = 0;
            FT_Read(ftHandle, buffer, (uint)count, ref read);
            return (int)read;
        }

        public int BytesAvailable
        {
            get
            {
                if (!IsOpen) return 0;
                uint amount = 0;
                FT_GetQueueStatus(ftHandle, ref amount);
                return (int)amount;
            }
        }

        public void Dispose()
        {
            Close();
        }
    }

    public class SerialKLinePort : IKLinePort
    {
        private SerialPort port;
        private string portName;

        public SerialKLinePort(string portName)
        {
            this.portName = portName;
        }

        public bool IsOpen { get { return port != null && port.IsOpen; } }

        public bool Open()
        {
            if (IsOpen) Close();
            try
            {
                port = new SerialPort(portName, 10400, Parity.None, 8, StopBits.One);
                port.ReadTimeout = 100;
                port.WriteTimeout = 100;
                port.Open();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Close()
        {
            if (port != null)
            {
                if (port.IsOpen) port.Close();
                port.Dispose();
                port = null;
            }
        }

        public bool InitKLineFast()
        {
            if (!IsOpen && !Open()) return false;

            try
            {
                port.DiscardInBuffer();
                port.DiscardOutBuffer();

                port.BreakState = true;
                Thread.Sleep(70);
                port.BreakState = false;
                Thread.Sleep(130);

                port.DiscardInBuffer();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void SetBaudRate(int baud)
        {
            if (IsOpen) port.BaudRate = baud;
        }

        public void SetTimeouts(int readMs, int writeMs)
        {
            if (IsOpen)
            {
                port.ReadTimeout = readMs;
                port.WriteTimeout = writeMs;
            }
        }

        public void Purge()
        {
            if (IsOpen)
            {
                port.DiscardInBuffer();
                port.DiscardOutBuffer();
            }
        }

        public int Write(byte[] buffer, int count)
        {
            if (!IsOpen) return 0;
            port.Write(buffer, 0, count);
            return count;
        }

        public int Read(byte[] buffer, int count)
        {
            if (!IsOpen) return 0;
            return port.Read(buffer, 0, count);
        }

        public int BytesAvailable
        {
            get { return IsOpen ? port.BytesToRead : 0; }
        }

        public void Dispose()
        {
            Close();
        }
    }

    /// <summary>
    /// Virtual ECU Simulator Port for 100% offline verification & automated testing
    /// </summary>
    public class VirtualKLinePort : IKLinePort
    {
        private bool isOpen = false;
        private EcuVendor simVendor;
        private byte[] rxQueue = new byte[65536];
        private int rxCount = 0;
        private byte[] mockRom;
        private int flashCounter = 3;

        public VirtualKLinePort(EcuVendor vendor = EcuVendor.Keihin, int romSizeKb = 64)
        {
            this.simVendor = vendor;
            this.mockRom = new byte[romSizeKb * 1024];

            // Fill mock ROM with recognizable calibration test pattern
            for (int i = 0; i < mockRom.Length; i++)
            {
                mockRom[i] = (byte)((i * 13 + 0xA5) & 0xFF);
            }

            if (vendor == EcuVendor.Shindengen)
            {
                // Inject Shindengen signatures & seed key parameters for validation
                byte[] shWKeysig = new byte[] { 0x11, 0x12, 0x13, 0x14, 0x15, 0x15, 0x17 };
                int sigPos = 0x200;
                Array.Copy(shWKeysig, 0, mockRom, sigPos, shWKeysig.Length);
                // Parameters: v5=0x1234, v6=0x5678, v7=0x000A, marker 0xEE 0x00
                mockRom[sigPos + 7] = 0x34; mockRom[sigPos + 8] = 0x12;
                mockRom[sigPos + 9] = 0x78; mockRom[sigPos + 10] = 0x56;
                mockRom[sigPos + 11] = 0x0A; mockRom[sigPos + 12] = 0x00;
                mockRom[sigPos + 15] = 0xEE; mockRom[sigPos + 16] = 0x00;
                mockRom[sigPos + 20] = 0xD0; mockRom[sigPos + 21] = 0x07;
                ChecksumEngine.FixShindengenChecksum(mockRom, out sigPos);
            }
            else
            {
                // Inject Keihin checksum
                int ckOff = (mockRom.Length == 0x10000) ? 0x7600 : (mockRom.Length == 0x20000 ? 0xDFEF : 0x3FFF8);
                ChecksumEngine.FixKeihinChecksum(mockRom, ckOff);
            }
        }

        public bool IsOpen { get { return isOpen; } }

        public bool Open()
        {
            isOpen = true;
            rxCount = 0;
            return true;
        }

        public void Close()
        {
            isOpen = false;
            rxCount = 0;
        }

        public bool InitKLineFast()
        {
            isOpen = true;
            rxCount = 0;
            return true;
        }

        public void SetBaudRate(int baud) { }
        public void SetTimeouts(int readMs, int writeMs) { }

        public void Purge()
        {
            rxCount = 0;
        }

        public int Write(byte[] buffer, int count)
        {
            if (!isOpen) return 0;

            byte[] tx = new byte[count];
            Array.Copy(buffer, 0, tx, 0, count);

            // 1. Put TX Echo in RX buffer first
            AppendRx(tx);

            // 2. Generate simulated ECM Response
            byte[] resp = ProcessEcmCommand(tx);
            if (resp != null && resp.Length > 0)
            {
                AppendRx(resp);
            }

            return count;
        }

        private byte[] ProcessEcmCommand(byte[] cmd)
        {
            if (cmd.Length < 3) return null;

            // 1. Tester Present: FE 04 72 8C
            if (cmd[0] == 0xFE && cmd[1] == 0x04 && cmd[2] == 0x72)
            {
                return new byte[] { 0xFE, 0x04, 0x72, 0x8C };
            }

            // 2. Read ECM ID: 72 05 00 F0 99 (Reply: 10 bytes total -> 72 0A 00 F0 [ID: 5 bytes] Cksum)
            if (cmd[0] == 0x72 && cmd[2] == 0x00 && cmd[3] == 0xF0)
            {
                byte[] reply = (simVendor == EcuVendor.Shindengen) ?
                    new byte[] { 0x72, 0x0A, 0x00, 0xF0, 0x01, 0x03, 0xD0, 0x0D, 0x01, 0x00 } : // PCX160 ID 0103D00D01
                    new byte[] { 0x72, 0x0A, 0x00, 0xF0, 0x01, 0x05, 0x55, 0x0D, 0x01, 0x00 };  // Wave110i ID 0105550D01
                reply[reply.Length - 1] = ChecksumEngine.Checksum8BitHonda(reply, reply.Length - 1, 0);
                return reply;
            }

            // Fallback command: 72 07 72 00 00 05 10
            if (cmd[0] == 0x72 && cmd[1] == 0x07 && cmd[2] == 0x72)
            {
                byte[] reply = (simVendor == EcuVendor.Shindengen) ?
                    new byte[] { 0x72, 0x0A, 0x72, 0x00, 0x01, 0x03, 0xD0, 0x0D, 0x01, 0x00 } :
                    new byte[] { 0x72, 0x0A, 0x72, 0x00, 0x01, 0x05, 0x55, 0x0D, 0x01, 0x00 };
                reply[reply.Length - 1] = ChecksumEngine.Checksum8BitHonda(reply, reply.Length - 1, 0);
                return reply;
            }

            // 3. Read Flash Count (Keihin 7D 06 01 01 03 78 / Shindengen 7B 06 02 01 03 79) - Reply: 7 bytes
            if ((cmd[0] == 0x7D || cmd[0] == 0x7B) && cmd[1] == 0x06)
            {
                byte header = (simVendor == EcuVendor.Shindengen) ? (byte)0x7C : (byte)0x7E;
                byte sub = (simVendor == EcuVendor.Shindengen) ? (byte)0x02 : (byte)0x01;
                byte[] reply = new byte[] { header, 0x07, sub, 0x01, 0x03, (byte)flashCounter, 0x00 };
                reply[6] = ChecksumEngine.Checksum8BitHonda(reply, 6, 0);
                return reply;
            }

            // 4. Clear DTC: 72 05 60 01 28
            if (cmd[0] == 0x72 && cmd[2] == 0x60 && cmd[3] == 0x01)
            {
                return new byte[] { 0x72, 0x05, 0x60, 0x01, 0x28 };
            }

            // 5. Reset ECM: 72 05 60 03 26
            if (cmd[0] == 0x72 && cmd[2] == 0x60 && cmd[3] == 0x03)
            {
                return new byte[] { 0x72, 0x05, 0x60, 0x03, 0x26 };
            }

            // 6. Shindengen Seed Request: 7B 07 02 05 00 00 [Cksum]
            if (cmd[0] == 0x7B && cmd[1] == 0x07 && cmd[2] == 0x02 && cmd[3] == 0x05 && cmd[4] == 0x00 && cmd[5] == 0x00)
            {
                byte[] reply = new byte[] { 0x7C, 0x07, 0x02, 0x05, 0x12, 0x34, 0x00 };
                reply[6] = ChecksumEngine.Checksum8BitHonda(reply, 6, 0);
                return reply;
            }

            // 7. Security Key Response / Unlock ACK
            if ((cmd[0] == 0x7B && cmd[1] == 0x07 && cmd[2] == 0x02 && cmd[3] == 0x06) || (cmd[0] == 0x7D && (cmd[3] == 0x02 || cmd[3] == 0x03)))
            {
                byte header = (simVendor == EcuVendor.Shindengen) ? (byte)0x7C : (byte)0x7E;
                byte sub = (simVendor == EcuVendor.Shindengen) ? (byte)0x02 : (byte)0x01;
                byte[] reply = new byte[] { header, 0x05, sub, 0x02, 0x00 };
                reply[4] = ChecksumEngine.Checksum8BitHonda(reply, 4, 0);
                return reply;
            }

            // 8. Baud rate switch ACK (7C 07 02 0E 01 00 6C or 7E 07 01 0E 01 90 DB)
            if ((cmd[0] == 0x7C || cmd[0] == 0x7E) && cmd[1] == 0x07 && cmd[3] == 0x0E)
            {
                byte header = (simVendor == EcuVendor.Shindengen) ? (byte)0x7C : (byte)0x7E;
                byte sub = (simVendor == EcuVendor.Shindengen) ? (byte)0x02 : (byte)0x01;
                byte[] reply = new byte[] { header, 0x05, sub, 0x0E, 0x00 };
                reply[4] = ChecksumEngine.Checksum8BitHonda(reply, 4, 0);
                return reply;
            }

            // 9. Erase Flash ACK
            if ((cmd[0] == 0x7C || cmd[0] == 0x7E) && cmd[1] == 0x0B && cmd[3] == 0x0B)
            {
                byte header = (simVendor == EcuVendor.Shindengen) ? (byte)0x7C : (byte)0x7E;
                byte sub = (simVendor == EcuVendor.Shindengen) ? (byte)0x02 : (byte)0x01;
                byte[] reply = new byte[] { header, 0x05, sub, 0x0B, 0x00 };
                reply[4] = ChecksumEngine.Checksum8BitHonda(reply, 4, 0);
                return reply;
            }

            // 10. Write Flash Block: [Header] [Len 135] [Sub] [Addr2] [Addr1] [Addr0] [Data 128B] [Cksum]
            if ((cmd[0] == 0x7E || cmd[0] == 0x7C) && cmd.Length >= 134)
            {
                int addr = (cmd[3] << 16) | (cmd[4] << 8) | cmd[5];
                int chunkLen = cmd.Length - 7;
                if (addr >= 0 && addr + chunkLen <= mockRom.Length)
                {
                    Array.Copy(cmd, 6, mockRom, addr, chunkLen);
                }

                byte header = (simVendor == EcuVendor.Shindengen) ? (byte)0x7C : (byte)0x7E;
                byte sub = (simVendor == EcuVendor.Shindengen) ? (byte)0x02 : (byte)0x01;
                byte[] reply = new byte[] { header, 0x05, sub, 0x0A, 0x00 };
                reply[4] = ChecksumEngine.Checksum8BitHonda(reply, 4, 0);
                return reply;
            }

            // 11. Read Flash Block: [Header 7D/7B] 0x07 [Sub 01/02] [Addr2] [Addr1] [Addr0] [Cksum]
            if ((cmd[0] == 0x7D || cmd[0] == 0x7B) && cmd[1] == 0x07)
            {
                int addr = (cmd[3] << 16) | (cmd[4] << 8) | cmd[5];
                int chunkLen = 128;
                byte header = (simVendor == EcuVendor.Shindengen) ? (byte)0x7C : (byte)0x7E;
                byte sub = (simVendor == EcuVendor.Shindengen) ? (byte)0x02 : (byte)0x01;

                byte pktLen = (byte)(chunkLen + 7);
                byte[] reply = new byte[pktLen];
                reply[0] = header;
                reply[1] = pktLen;
                reply[2] = sub;
                reply[3] = cmd[3];
                reply[4] = cmd[4];
                reply[5] = cmd[5];

                if (addr >= 0 && addr + chunkLen <= mockRom.Length)
                {
                    Array.Copy(mockRom, addr, reply, 6, chunkLen);
                }
                else
                {
                    for (int i = 6; i < 6 + chunkLen; i++) reply[i] = 0xFF;
                }

                reply[pktLen - 1] = ChecksumEngine.Checksum8BitHonda(reply, pktLen - 1, 0);
                return reply;
            }

            // 12. Generic Diagnostic session exit / Reset flash counter ACK
            byte hOut = (simVendor == EcuVendor.Shindengen) ? (byte)0x7C : (byte)0x7E;
            byte[] genAck = new byte[] { hOut, 0x05, 0x01, 0x0C, 0x00 };
            genAck[4] = ChecksumEngine.Checksum8BitHonda(genAck, 4, 0);
            return genAck;
        }

        private void AppendRx(byte[] data)
        {
            if (rxCount + data.Length <= rxQueue.Length)
            {
                Array.Copy(data, 0, rxQueue, rxCount, data.Length);
                rxCount += data.Length;
            }
        }

        public int Read(byte[] buffer, int count)
        {
            if (!isOpen || rxCount == 0) return 0;
            int r = Math.Min(count, rxCount);
            Array.Copy(rxQueue, 0, buffer, 0, r);

            // Shift remaining
            if (r < rxCount)
            {
                Array.Copy(rxQueue, r, rxQueue, 0, rxCount - r);
            }
            rxCount -= r;
            return r;
        }

        public int BytesAvailable
        {
            get { return rxCount; }
        }

        public void Dispose()
        {
            Close();
        }
    }

    #endregion

    #region Honda K-Line Protocol & Flash Engine

    public class HondaKLineEngine : IDisposable
    {
        private IKLinePort port;
        private readonly object commLock = new object();

        public HondaKLineEngine(IKLinePort port)
        {
            this.port = port;
        }

        public IKLinePort Port { get { return port; } }

        public bool Connect()
        {
            lock (commLock)
            {
                return port.InitKLineFast();
            }
        }

        public void Disconnect()
        {
            lock (commLock)
            {
                port.Close();
            }
        }

        /// <summary>
        /// Sends command and handles K-Line half-duplex echo stripping and reply validation
        /// </summary>
        public bool SendCommand(byte[] command, out byte[] reply, int sleepMs = 0)
        {
            reply = null;
            if (!port.IsOpen) return false;

            lock (commLock)
            {
                port.Purge();
                int commandSize = command.Length;
                int timeoutMs = 60 + (commandSize * 2);

                port.Write(command, commandSize);

                if (sleepMs > 0)
                {
                    Thread.Sleep(sleepMs);
                }

                Stopwatch sw = Stopwatch.StartNew();
                byte[] rawBuf = new byte[512];
                int totalRead = 0;
                int expectedReplyLen = 0;

                while (sw.ElapsedMilliseconds < timeoutMs)
                {
                    int avail = port.BytesAvailable;
                    if (avail > 0)
                    {
                        byte[] chunk = new byte[avail];
                        int r = port.Read(chunk, avail);
                        if (r > 0 && totalRead + r <= rawBuf.Length)
                        {
                            Array.Copy(chunk, 0, rawBuf, totalRead, r);
                            totalRead += r;

                            // Once echo + reply header received
                            if (totalRead >= commandSize + 2 && expectedReplyLen == 0)
                            {
                                expectedReplyLen = rawBuf[commandSize + 1]; // Byte 1 of reply is packet length
                            }

                            if (expectedReplyLen > 0 && totalRead >= commandSize + expectedReplyLen)
                            {
                                // Extract reply
                                reply = new byte[expectedReplyLen];
                                Array.Copy(rawBuf, commandSize, reply, 0, expectedReplyLen);

                                sw.Stop();
                                port.Purge();
                                return ChecksumEngine.ValidateChecksum(reply, expectedReplyLen);
                            }
                        }
                    }
                    Thread.Sleep(2);
                }

                sw.Stop();
                port.Purge();
                return false;
            }
        }

        public bool QueryEcmId(out string ecmId)
        {
            ecmId = "";
            byte[] cmdTesterPresent = new byte[] { 0xFE, 0x04, 0x72, 0x8C };
            byte[] reply;

            if (!SendCommand(cmdTesterPresent, out reply, 10))
            {
                return false;
            }

            // Command Read Data Table 00 F0: 72 05 00 F0 99
            byte[] cmdReadId = new byte[] { 0x72, 0x05, 0x00, 0xF0, 0x99 };
            if (SendCommand(cmdReadId, out reply, 10))
            {
                if (reply != null && reply.Length >= 8)
                {
                    int startIdx = (reply.Length >= 10 && reply[2] == 0x00 && reply[3] == 0xF0) ? 4 : 2;
                    if (reply.Length >= startIdx + 5)
                    {
                        byte[] idBytes = new byte[5];
                        Array.Copy(reply, startIdx, idBytes, 0, 5);
                        ecmId = BitConverter.ToString(idBytes).Replace("-", "");
                        return true;
                    }
                }
            }

            // Fallback command: 72 07 72 00 00 05 10
            byte[] cmdReadId2 = new byte[] { 0x72, 0x07, 0x72, 0x00, 0x00, 0x05, 0x10 };
            if (SendCommand(cmdReadId2, out reply, 10))
            {
                if (reply != null && reply.Length >= 8)
                {
                    int startIdx = (reply.Length >= 10) ? 4 : 2;
                    if (reply.Length >= startIdx + 5)
                    {
                        byte[] idBytes = new byte[5];
                        Array.Copy(reply, startIdx, idBytes, 0, 5);
                        ecmId = BitConverter.ToString(idBytes).Replace("-", "");
                        return true;
                    }
                }
            }

            return false;
        }

        public bool QueryFlashCount(out int flashCount)
        {
            flashCount = -1;
            byte[] reply;

            // Try Keihin Flash count query: 7D 06 01 01 03 78
            byte[] cmdKhCount = new byte[] { 0x7D, 0x06, 0x01, 0x01, 0x03, 0x78 };
            if (SendCommand(cmdKhCount, out reply, 10))
            {
                if (reply != null && reply.Length >= 5)
                {
                    flashCount = reply[3];
                    return true;
                }
            }

            // Try Shindengen Flash count query: 7B 06 02 01 03 79
            byte[] cmdShCount = new byte[] { 0x7B, 0x06, 0x02, 0x01, 0x03, 0x79 };
            if (SendCommand(cmdShCount, out reply, 10))
            {
                if (reply != null && reply.Length >= 5)
                {
                    flashCount = reply[3];
                    return true;
                }
            }

            return false;
        }

        public bool ClearDTC()
        {
            byte[] reply;
            byte[] cmd1 = new byte[] { 0xFE, 0x04, 0x72, 0x8C };
            byte[] cmd2 = new byte[] { 0x72, 0x05, 0x00, 0xF0, 0x99 };
            byte[] cmd3 = new byte[] { 0x72, 0x05, 0x60, 0x01, 0x28 };

            SendCommand(cmd1, out reply, 100);
            Thread.Sleep(500);
            SendCommand(cmd2, out reply, 100);
            Thread.Sleep(500);
            return SendCommand(cmd3, out reply, 100);
        }

        public bool ResetECM()
        {
            byte[] reply;
            byte[] cmd1 = new byte[] { 0xFE, 0x04, 0x72, 0x8C };
            byte[] cmd2 = new byte[] { 0x72, 0x05, 0x00, 0xF0, 0x99 };
            byte[] cmd3 = new byte[] { 0x72, 0x05, 0x60, 0x03, 0x26 };

            SendCommand(cmd1, out reply, 100);
            Thread.Sleep(500);
            SendCommand(cmd2, out reply, 100);
            Thread.Sleep(500);
            return SendCommand(cmd3, out reply, 100);
        }

        public bool ResetFlashCounterShindengen()
        {
            byte[] reply;
            byte[] unlockSeq = new byte[] { 0x7B, 0x08, 0x02, 0x02, 0x50, 0x19, 0x76, 0x9A };
            byte[] eraseCmd = new byte[] { 0x7C, 0x0B, 0x02, 0x0B, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0x6F };

            SendCommand(unlockSeq, out reply, 100);
            Thread.Sleep(200);
            return SendCommand(eraseCmd, out reply, 100);
        }

        public bool ReadFlash(
            int startOffset,
            int totalBytesToRead,
            EcuType ecuType,
            bool fastRead,
            out byte[] dumpedData,
            EventHandler<FlashProgressEventArgs> progressHandler = null)
        {
            dumpedData = null;
            if (totalBytesToRead <= 0) totalBytesToRead = 0x10000;

            bool isShindengen = (ecuType == EcuType.ShindengenV850 ||
                                 ecuType == EcuType.ShindengenRH850 ||
                                 ecuType == EcuType.ShindengenRH850B);

            // 1. Pre-Read Unlock & Authentication
            byte[] reply;
            if (isShindengen)
            {
                byte[] seedReq = new byte[] { 0x7B, 0x07, 0x02, 0x05, 0x00, 0x00, 0x00 };
                seedReq[6] = ChecksumEngine.Checksum8BitHonda(seedReq, 6, 0);
                SendCommand(seedReq, out reply, 50);

                if (fastRead)
                {
                    byte[] fastCmd = new byte[] { 0x7C, 0x07, 0x02, 0x0E, 0x01, 0x00, 0x6C };
                    if (SendCommand(fastCmd, out reply, 50))
                    {
                        port.SetBaudRate(62500);
                        Thread.Sleep(50);
                    }
                }
            }
            else
            {
                byte[] unlock1 = new byte[] { 0x7D, 0x08, 0x01, 0x02, 0x50, 0x47, 0x4D, 0x94 };
                byte[] unlock2 = new byte[] { 0x7D, 0x08, 0x01, 0x03, 0x2D, 0x46, 0x49, 0xBB };
                SendCommand(unlock1, out reply, 50);
                Thread.Sleep(50);
                SendCommand(unlock2, out reply, 50);
                Thread.Sleep(50);

                if (fastRead)
                {
                    byte[] fastCmd = new byte[] { 0x7E, 0x07, 0x01, 0x0E, 0x01, 0x90, 0xDB };
                    if (SendCommand(fastCmd, out reply, 50))
                    {
                        port.SetBaudRate(62500);
                        Thread.Sleep(50);
                    }
                }
            }

            // 2. Reading Blocks
            dumpedData = new byte[totalBytesToRead];
            int chunkSize = 128;
            int bytesRead = 0;
            Stopwatch sw = Stopwatch.StartNew();

            for (int currOffset = startOffset; currOffset < startOffset + totalBytesToRead; currOffset += chunkSize)
            {
                int currentChunk = Math.Min(chunkSize, (startOffset + totalBytesToRead) - currOffset);
                bool chunkSuccess = false;

                for (int retry = 0; retry < 3; retry++)
                {
                    byte header = isShindengen ? (byte)0x7B : (byte)0x7D;
                    byte[] readReq = new byte[7];
                    readReq[0] = header;
                    readReq[1] = 0x07;
                    readReq[2] = isShindengen ? (byte)0x02 : (byte)0x01;
                    readReq[3] = (byte)((currOffset >> 16) & 0xFF);
                    readReq[4] = (byte)((currOffset >> 8) & 0xFF);
                    readReq[5] = (byte)(currOffset & 0xFF);
                    readReq[6] = ChecksumEngine.Checksum8BitHonda(readReq, 6, 0);

                    if (SendCommand(readReq, out reply, 5) && reply != null && reply.Length >= currentChunk + 7)
                    {
                        Array.Copy(reply, 6, dumpedData, currOffset - startOffset, currentChunk);
                        chunkSuccess = true;
                        break;
                    }

                    byte[] tableReq = new byte[7];
                    tableReq[0] = 0x72;
                    tableReq[1] = 0x07;
                    tableReq[2] = 0x72;
                    tableReq[3] = (byte)((currOffset >> 8) & 0xFF);
                    tableReq[4] = (byte)(currOffset & 0xFF);
                    tableReq[5] = (byte)currentChunk;
                    tableReq[6] = ChecksumEngine.Checksum8BitHonda(tableReq, 6, 0);

                    if (SendCommand(tableReq, out reply, 5) && reply != null && reply.Length >= currentChunk + 3)
                    {
                        Array.Copy(reply, 3, dumpedData, currOffset - startOffset, currentChunk);
                        chunkSuccess = true;
                        break;
                    }

                    Thread.Sleep(10);
                }

                if (!chunkSuccess)
                {
                    sw.Stop();
                    port.SetBaudRate(10400);
                    return false;
                }

                bytesRead += currentChunk;

                if (progressHandler != null)
                {
                    double pct = (double)bytesRead / totalBytesToRead * 100.0;
                    double speed = (bytesRead / 1024.0) / Math.Max(0.1, sw.Elapsed.TotalSeconds);
                    progressHandler(this, new FlashProgressEventArgs
                    {
                        BytesProcessed = bytesRead,
                        TotalBytes = totalBytesToRead,
                        Percentage = pct,
                        SpeedKbps = speed,
                        Elapsed = sw.Elapsed,
                        IsReading = true,
                        StatusMessage = string.Format("Reading {0:F1} KB / {1:F1} KB ({2:F1}%)",
                            bytesRead / 1024.0, totalBytesToRead / 1024.0, pct)
                    });
                }
            }

            sw.Stop();

            byte[] exitCmd = isShindengen ?
                new byte[] { 0x7C, 0x05, 0x02, 0x0C, 0x71 } :
                new byte[] { 0x7E, 0x05, 0x01, 0x0C, 0x70 };

            byte[] exitReply;
            SendCommand(exitCmd, out exitReply, 50);
            port.SetBaudRate(10400);

            return true;
        }

        public bool WriteFlash(
            byte[] binBytes,
            int startOffset,
            EcuType ecuType,
            bool fastWrite,
            EventHandler<FlashProgressEventArgs> progressHandler = null)
        {
            if (binBytes == null || binBytes.Length == 0) return false;

            bool isShindengen = (ecuType == EcuType.ShindengenV850 ||
                                 ecuType == EcuType.ShindengenRH850 ||
                                 ecuType == EcuType.ShindengenRH850B);

            // 1. Pre-Write Authentication
            if (isShindengen)
            {
                if (!ShindengenPreWrite(binBytes, fastWrite))
                    return false;
            }
            else
            {
                if (!KeihinPreWrite(fastWrite))
                    return false;
            }

            // 2. Block Writing
            Stopwatch sw = Stopwatch.StartNew();
            int totalBytesToWrite = binBytes.Length - startOffset;
            int blockSize = 128;
            int bytesWritten = 0;

            for (int currOffset = startOffset; currOffset < binBytes.Length; currOffset += blockSize)
            {
                int chunkLen = Math.Min(blockSize, binBytes.Length - currOffset);
                byte[] chunk = new byte[chunkLen];
                Array.Copy(binBytes, currOffset, chunk, 0, chunkLen);

                byte header = isShindengen ? (byte)0x7C : (byte)0x7E;
                byte pktLen = (byte)(chunkLen + 7);
                byte[] packet = new byte[pktLen];
                packet[0] = header;
                packet[1] = pktLen;
                packet[2] = isShindengen ? (byte)0x02 : (byte)0x01;
                packet[3] = (byte)((currOffset >> 16) & 0xFF);
                packet[4] = (byte)((currOffset >> 8) & 0xFF);
                packet[5] = (byte)(currOffset & 0xFF);
                Array.Copy(chunk, 0, packet, 6, chunkLen);
                packet[pktLen - 1] = ChecksumEngine.Checksum8BitHonda(packet, pktLen - 1, 0);

                byte[] reply;
                if (!SendCommand(packet, out reply, 5))
                {
                    Thread.Sleep(10);
                    if (!SendCommand(packet, out reply, 10))
                    {
                        sw.Stop();
                        port.SetBaudRate(10400);
                        return false;
                    }
                }

                bytesWritten += chunkLen;
                if (progressHandler != null)
                {
                    double pct = (double)bytesWritten / totalBytesToWrite * 100.0;
                    double speed = (bytesWritten / 1024.0) / Math.Max(0.1, sw.Elapsed.TotalSeconds);
                    progressHandler(this, new FlashProgressEventArgs
                    {
                        BytesProcessed = bytesWritten,
                        TotalBytes = totalBytesToWrite,
                        Percentage = pct,
                        SpeedKbps = speed,
                        Elapsed = sw.Elapsed,
                        IsReading = false,
                        StatusMessage = string.Format("Writing {0:F1} KB / {1:F1} KB ({2:F1}%)",
                            bytesWritten / 1024.0, totalBytesToWrite / 1024.0, pct)
                    });
                }
            }

            sw.Stop();

            byte[] exitCmd = isShindengen ?
                new byte[] { 0x7C, 0x05, 0x02, 0x0C, 0x71 } :
                new byte[] { 0x7E, 0x05, 0x01, 0x0C, 0x70 };

            byte[] exitReply;
            SendCommand(exitCmd, out exitReply, 100);
            port.SetBaudRate(10400);
            return true;
        }

        private bool KeihinPreWrite(bool fastWrite)
        {
            byte[] reply;
            byte[] unlock1 = new byte[] { 0x7D, 0x08, 0x01, 0x02, 0x50, 0x47, 0x4D, 0x94 };
            byte[] unlock2 = new byte[] { 0x7D, 0x08, 0x01, 0x03, 0x2D, 0x46, 0x49, 0xBB };
            byte[] eraseCmd = new byte[] { 0x7E, 0x0B, 0x01, 0x0B, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0x6E };

            SendCommand(unlock1, out reply, 50);
            Thread.Sleep(100);
            SendCommand(unlock2, out reply, 50);
            Thread.Sleep(100);

            if (fastWrite)
            {
                byte[] fastCmd = new byte[] { 0x7E, 0x07, 0x01, 0x0E, 0x01, 0x90, 0xDB };
                if (SendCommand(fastCmd, out reply, 50))
                {
                    port.SetBaudRate(62500);
                    Thread.Sleep(50);
                }
            }

            return SendCommand(eraseCmd, out reply, 200);
        }

        private bool ShindengenPreWrite(byte[] binBytes, bool fastWrite)
        {
            byte[] reply;
            byte[] seedReq = new byte[] { 0x7B, 0x07, 0x02, 0x05, 0x00, 0x00, 0x00 };
            seedReq[6] = ChecksumEngine.Checksum8BitHonda(seedReq, 6, 0);

            if (!SendCommand(seedReq, out reply, 50) || reply == null || reply.Length < 6)
            {
                return false;
            }

            byte[] seed = new byte[] { reply[3], reply[4] };
            byte[] key;
            if (!SeedKeyEngine.GetShindengenWriteKey(binBytes, seed, out key))
            {
                if (!SeedKeyEngine.GetShindengenRecoveryKey(binBytes, seed, out key))
                {
                    return false;
                }
            }

            byte[] keyResp = new byte[] { 0x7B, 0x07, 0x02, 0x06, key[0], key[1], 0x00 };
            keyResp[6] = ChecksumEngine.Checksum8BitHonda(keyResp, 6, 0);

            if (!SendCommand(keyResp, out reply, 50))
            {
                return false;
            }

            if (fastWrite)
            {
                byte[] fastCmd = new byte[] { 0x7C, 0x07, 0x02, 0x0E, 0x01, 0x00, 0x6C };
                if (SendCommand(fastCmd, out reply, 50))
                {
                    port.SetBaudRate(62500);
                    Thread.Sleep(50);
                }
            }

            byte[] eraseCmd = new byte[] { 0x7C, 0x0B, 0x02, 0x0B, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0x6F };
            return SendCommand(eraseCmd, out reply, 200);
        }

        public void Dispose()
        {
            Disconnect();
            if (port != null)
            {
                port.Dispose();
                port = null;
            }
        }
    }

    #endregion
}
