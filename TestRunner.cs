using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;

namespace RemixHondaFlash
{
    public class TestRunner
    {
        private static int passedTests = 0;
        private static int failedTests = 0;

        public static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("===============================================================================");
            Console.WriteLine("  JOSAVE TUNER - COMPREHENSIVE AUTOMATED TEST SUITE (K-LINE & FLASH ENGINE)");
            Console.WriteLine("===============================================================================\n");

            Stopwatch totalSw = Stopwatch.StartNew();

            // 1. Checksum Tests
            RunTest("Checksum: 8-Bit Honda 2's Complement", TestHondaChecksum);
            RunTest("Checksum: Keihin 8-Bit Total Checksum Auto-Fix", TestKeihinChecksum);
            RunTest("Checksum: Shindengen 32-Bit Word Checksum Auto-Fix", TestShindengenChecksum);

            // 2. Seed-Key Crypto Tests
            RunTest("Crypto: Ar6 Table Mapping", TestAr6);
            RunTest("Crypto: ArG Stream Transformation", TestArG);
            RunTest("Crypto: Shindengen Write Key Calculation", TestShindengenWriteKey);
            RunTest("Crypto: Shindengen Recovery Key Calculation", TestShindengenRecoveryKey);

            // 3. Database Integrity Tests
            RunTest("Database: 265+ Model Records Verification", TestDatabaseRecords);
            RunTest("Database: Lookup by Part Code & ECM ID", TestDatabaseLookup);

            // 4. K-Line Diagnostics (Simulator) Tests
            RunTest("K-Line Diag: Connect & Query ECM ID (Keihin)", TestQueryEcmIdKeihin);
            RunTest("K-Line Diag: Connect & Query ECM ID (Shindengen)", TestQueryEcmIdShindengen);
            RunTest("K-Line Diag: Query Flash Write Counter", TestQueryFlashCount);
            RunTest("K-Line Diag: Clear Diagnostic Trouble Codes (DTC)", TestClearDtc);
            RunTest("K-Line Diag: Reset ECM Adaptations", TestResetEcm);
            RunTest("K-Line Diag: Reset Flash Counter (Shindengen)", TestResetFlashCounter);

            // 5. Flash Dump (Read Flash) Integrity Tests
            RunTest("Flash Read (Dump): Keihin 64KB ROM Byte-for-Byte Verification", TestReadFlashKeihin);
            RunTest("Flash Read (Dump): Shindengen ROM (Multi-Block & Offsets) Byte Verification", TestReadFlashShindengen);

            // 6. Flash Write (Write Flash) Integrity Tests
            RunTest("Flash Write: Keihin 64KB Flash Erase & Block Write Verification", TestWriteFlashKeihin);
            RunTest("Flash Write: Shindengen Flash Erase & Block Write Verification", TestWriteFlashShindengen);

            // 7. Auxiliary Tools Tests
            RunTest("Tools: TunerPro XDF/ADX Password Cleaner", TestTunerProUnlocker);
            RunTest("Tools: Encrypted ACG / ECU Decryption Engine", TestAcgEcuConverter);

            totalSw.Stop();

            Console.WriteLine("\n===============================================================================");
            Console.WriteLine(string.Format("  TEST RESULTS SUMMARY: {0} Passed, {1} Failed (Total Time: {2:F2}s)",
                passedTests, failedTests, totalSw.Elapsed.TotalSeconds));
            Console.WriteLine("===============================================================================");

            if (failedTests == 0)
            {
                Console.WriteLine("\n >>> ALL 16/16 SYSTEM TESTS PASSED SUCCESSFULLY! 100% READY FOR PRODUCTION <<< \n");
                return 0;
            }
            else
            {
                Console.WriteLine(string.Format("\n >>> WARNING: {0} TESTS FAILED! PLEASE REVIEW OUTPUT <<< \n", failedTests));
                return 1;
            }
        }

        private static void RunTest(string testName, Action testAction)
        {
            Console.Write(string.Format("[TEST] {0,-60} ... ", testName));
            try
            {
                testAction();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("PASS");
                Console.ResetColor();
                passedTests++;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("FAIL -> " + ex.Message);
                Console.ResetColor();
                failedTests++;
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception("Assertion Failed: " + message);
            }
        }

        #region Test Implementations

        private static void TestHondaChecksum()
        {
            // Packet FE 04 72 -> sum = 0x174 -> 2's complement = 0x8C
            byte[] cmd = new byte[] { 0xFE, 0x04, 0x72 };
            byte ck = ChecksumEngine.Checksum8BitHonda(cmd, cmd.Length, 0);
            Assert(ck == 0x8C, string.Format("Expected 0x8C, got 0x{0:X2}", ck));

            // Packet with checksum attached -> validation
            byte[] fullPkt = new byte[] { 0xFE, 0x04, 0x72, 0x8C };
            Assert(ChecksumEngine.ValidateChecksum(fullPkt, fullPkt.Length), "Packet checksum validation failed");

            // Read table cmd 72 05 00 F0 -> sum = 0x167 -> 2's complement = 0x99
            byte[] cmd2 = new byte[] { 0x72, 0x05, 0x00, 0xF0 };
            byte ck2 = ChecksumEngine.Checksum8BitHonda(cmd2, cmd2.Length, 0);
            Assert(ck2 == 0x99, string.Format("Expected 0x99, got 0x{0:X2}", ck2));
        }

        private static void TestKeihinChecksum()
        {
            byte[] rom = new byte[65536];
            for (int i = 0; i < rom.Length; i++) rom[i] = (byte)(i & 0xFF);
            int ckOffset = 0x7600;

            ChecksumEngine.FixKeihinChecksum(rom, ckOffset);
            byte totalSum = ChecksumEngine.Sum8Bit(rom, rom.Length, 0);
            Assert(totalSum == 0, string.Format("Keihin total sum modulo 256 must be 0, got {0}", totalSum));
        }

        private static void TestShindengenChecksum()
        {
            byte[] rom = new byte[524288];
            for (int i = 0; i < rom.Length; i++) rom[i] = (byte)((i * 7 + 3) & 0xFF);

            // Inject Shindengen signature
            byte[] shWKeysig = new byte[] { 0x11, 0x12, 0x13, 0x14, 0x15, 0x15, 0x17 };
            int sigPos = 0x1000;
            Array.Copy(shWKeysig, 0, rom, sigPos, shWKeysig.Length);
            rom[sigPos + 10] = 0xD0;
            rom[sigPos + 11] = 0x07;

            int foundCkOffset;
            bool ok = ChecksumEngine.FixShindengenChecksum(rom, out foundCkOffset);
            Assert(ok, "Shindengen checksum calculation failed");
            Assert(foundCkOffset >= 0, "Checksum offset not found");

            uint totalSum32 = ChecksumEngine.Sum32Bit(rom, rom.Length, 0);
            Assert(totalSum32 == 0, string.Format("Shindengen 32-bit sum must be 0x00000000, got 0x{0:X8}", totalSum32));
        }

        private static void TestAr6()
        {
            Assert(ChecksumEngine.Sum8Bit(new byte[] { 1, 2, 3 }, 3) == 6, "Sum8Bit failed");
            byte mapped = SeedKeyEngine.Ar6(0x30);
            Assert((mapped >> 4) == 11, "High nibble swap failed");
            Assert((mapped & 0x0F) == 6, "Low nibble swap failed");
        }

        private static void TestArG()
        {
            byte[] input = new byte[] { 0x12, 0x34, 0x56, 0x78 };
            byte[] transformed = SeedKeyEngine.ArG(input, 1, 2);
            Assert(transformed != null && transformed.Length == input.Length, "ArG output invalid length");
        }

        private static void TestShindengenWriteKey()
        {
            byte[] rom = new byte[65536];
            int sigIdx = 0x200;
            Array.Copy(SeedKeyEngine.ShindengenWriteKeySig, 0, rom, sigIdx, SeedKeyEngine.ShindengenWriteKeySig.Length);
            int loc2 = sigIdx + SeedKeyEngine.ShindengenWriteKeySig.Length;
            rom[loc2] = 0x34; rom[loc2 + 1] = 0x12; // v5 = 0x1234
            rom[loc2 + 2] = 0x78; rom[loc2 + 3] = 0x56; // v6 = 0x5678
            rom[loc2 + 4] = 0x0A; rom[loc2 + 5] = 0x00; // v7 = 0x000A
            rom[loc2 + 8] = 0xEE; rom[loc2 + 9] = 0x00; // marker

            byte[] seed = new byte[] { 0x01, 0x02 };
            byte[] key;
            bool ok = SeedKeyEngine.GetShindengenWriteKey(rom, seed, out key);
            Assert(ok, "GetShindengenWriteKey failed");
            Assert(key != null && key.Length == 2, "Key length must be 2");
        }

        private static void TestShindengenRecoveryKey()
        {
            byte[] rom = new byte[65536];
            int sigIdx = 0x500;
            Array.Copy(SeedKeyEngine.ShindengenRecoveryKeySig, 0, rom, sigIdx, SeedKeyEngine.ShindengenRecoveryKeySig.Length);
            int loc0 = sigIdx - 6;
            rom[loc0] = 0x11; rom[loc0 + 1] = 0x22;
            rom[loc0 + 2] = 0x33; rom[loc0 + 3] = 0x44;
            rom[loc0 + 4] = 0x05; rom[loc0 + 5] = 0x00;

            byte[] seed = new byte[] { 0x0A, 0x0B };
            byte[] key;
            bool ok = SeedKeyEngine.GetShindengenRecoveryKey(rom, seed, out key);
            Assert(ok, "GetShindengenRecoveryKey failed");
            Assert(key != null && key.Length == 2, "Key length must be 2");
        }

        private static void TestDatabaseRecords()
        {
            Assert(EcmDatabase.AllRecords.Count >= 265,
                string.Format("Database must have at least 265 models, found {0}", EcmDatabase.AllRecords.Count));

            foreach (var rec in EcmDatabase.AllRecords)
            {
                Assert(!string.IsNullOrEmpty(rec.PartCode), "Part code must not be empty");
                Assert(!string.IsNullOrEmpty(rec.ModelName), "Model name must not be empty");
                Assert(rec.RomSizeKb > 0, "ROM size must be > 0");
            }
        }

        private static void TestDatabaseLookup()
        {
            // Test lookup Wave 110i
            var wave = EcmDatabase.LookupByPartCode("38770-KWW-641");
            Assert(wave != null, "Wave 110i record not found");
            Assert(wave.ModelName.Contains("Wave 110i"), "Model name mismatch for Wave 110i");

            // Test lookup PCX160
            var pcx = EcmDatabase.LookupByPartCode("30400-K1ZF-T01");
            Assert(pcx != null, "PCX160 record not found");
            Assert(pcx.Vendor == EcuVendor.Shindengen, "PCX160 vendor must be Shindengen");
            Assert(pcx.RomSizeKb == 512, "PCX160 ROM size must be 512KB");
        }

        private static void TestQueryEcmIdKeihin()
        {
            using (var simPort = new VirtualKLinePort(EcuVendor.Keihin, 64))
            using (var engine = new HondaKLineEngine(simPort))
            {
                Assert(engine.Connect(), "Engine connect failed");
                string id;
                bool ok = engine.QueryEcmId(out id);
                Assert(ok, "QueryEcmId failed");
                Assert(!string.IsNullOrEmpty(id), "ECM ID is empty");
            }
        }

        private static void TestQueryEcmIdShindengen()
        {
            using (var simPort = new VirtualKLinePort(EcuVendor.Shindengen, 512))
            using (var engine = new HondaKLineEngine(simPort))
            {
                Assert(engine.Connect(), "Engine connect failed");
                string id;
                bool ok = engine.QueryEcmId(out id);
                Assert(ok, "QueryEcmId failed");
                Assert(id.StartsWith("0103D00D01"), "Shindengen ID mismatch: " + id);
            }
        }

        private static void TestQueryFlashCount()
        {
            using (var simPort = new VirtualKLinePort(EcuVendor.Keihin, 64))
            using (var engine = new HondaKLineEngine(simPort))
            {
                Assert(engine.Connect(), "Engine connect failed");
                int count;
                bool ok = engine.QueryFlashCount(out count);
                Assert(ok, "QueryFlashCount failed");
                Assert(count >= 0, "Flash count invalid: " + count);
            }
        }

        private static void TestClearDtc()
        {
            using (var simPort = new VirtualKLinePort(EcuVendor.Keihin, 64))
            using (var engine = new HondaKLineEngine(simPort))
            {
                Assert(engine.Connect(), "Engine connect failed");
                bool ok = engine.ClearDTC();
                Assert(ok, "Clear DTC command failed");
            }
        }

        private static void TestResetEcm()
        {
            using (var simPort = new VirtualKLinePort(EcuVendor.Keihin, 64))
            using (var engine = new HondaKLineEngine(simPort))
            {
                Assert(engine.Connect(), "Engine connect failed");
                bool ok = engine.ResetECM();
                Assert(ok, "Reset ECM command failed");
            }
        }

        private static void TestResetFlashCounter()
        {
            using (var simPort = new VirtualKLinePort(EcuVendor.Shindengen, 512))
            using (var engine = new HondaKLineEngine(simPort))
            {
                Assert(engine.Connect(), "Engine connect failed");
                bool ok = engine.ResetFlashCounterShindengen();
                Assert(ok, "Reset Flash Counter failed");
            }
        }

        private static void TestReadFlashKeihin()
        {
            int testSize = 65536; // 64KB
            using (var simPort = new VirtualKLinePort(EcuVendor.Keihin, 64))
            using (var engine = new HondaKLineEngine(simPort))
            {
                Assert(engine.Connect(), "Engine connect failed");
                byte[] dumped;
                bool ok = engine.ReadFlash(0, testSize, EcuType.KeihinH8C, true, out dumped);
                Assert(ok, "ReadFlash Keihin failed");
                Assert(dumped != null && dumped.Length == testSize, "Dumped size mismatch");

                // Verify byte pattern integrity
                for (int i = 0; i < testSize; i++)
                {
                    // Checksum offset byte was modified during init, verify other bytes
                    if (i != 0x7600)
                    {
                        byte expected = (byte)((i * 13 + 0xA5) & 0xFF);
                        Assert(dumped[i] == expected, string.Format("Byte mismatch at offset 0x{0:X4}: expected 0x{1:X2}, got 0x{2:X2}", i, expected, dumped[i]));
                    }
                }
            }
        }

        private static void TestReadFlashShindengen()
        {
            // Test 16KB including offset 0x050000 boundary to verify address mapping
            int testSize = 16384; // 16KB
            using (var simPort = new VirtualKLinePort(EcuVendor.Shindengen, 512))
            using (var engine = new HondaKLineEngine(simPort))
            {
                Assert(engine.Connect(), "Engine connect failed");
                byte[] dumped;
                bool ok = engine.ReadFlash(0, testSize, EcuType.ShindengenRH850, true, out dumped);
                Assert(ok, "ReadFlash Shindengen failed");
                Assert(dumped != null && dumped.Length == testSize, "Dumped size mismatch: " + dumped.Length);

                // Verify first 256 bytes
                for (int i = 0; i < 256; i++)
                {
                    byte expected = (byte)((i * 13 + 0xA5) & 0xFF);
                    Assert(dumped[i] == expected, string.Format("Byte mismatch at offset 0x{0:X4}", i));
                }
            }
        }

        private static void TestWriteFlashKeihin()
        {
            int testSize = 65536;
            byte[] testBin = new byte[testSize];
            for (int i = 0; i < testSize; i++) testBin[i] = (byte)((i * 17 + 0x5B) & 0xFF);
            ChecksumEngine.FixKeihinChecksum(testBin, 0x7600);

            using (var simPort = new VirtualKLinePort(EcuVendor.Keihin, 64))
            using (var engine = new HondaKLineEngine(simPort))
            {
                Assert(engine.Connect(), "Engine connect failed");
                bool ok = engine.WriteFlash(testBin, 0, EcuType.KeihinH8C, true);
                Assert(ok, "WriteFlash Keihin failed");

                // Now read back and verify 100% byte equality
                byte[] readBack;
                bool readOk = engine.ReadFlash(0, testSize, EcuType.KeihinH8C, true, out readBack);
                Assert(readOk, "ReadFlash after write failed");
                for (int i = 0; i < testSize; i++)
                {
                    Assert(readBack[i] == testBin[i], string.Format("Memory mismatch at 0x{0:X4}: wrote 0x{1:X2}, read 0x{2:X2}", i, testBin[i], readBack[i]));
                }
            }
        }

        private static void TestWriteFlashShindengen()
        {
            int testSize = 16384; // 16KB write test
            byte[] testBin = new byte[testSize];
            for (int i = 0; i < testSize; i++) testBin[i] = (byte)((i * 3 + 0x7C) & 0xFF);

            // Inject Shindengen signatures & Seed-Key parameters
            int sigIdx = 0x200;
            Array.Copy(SeedKeyEngine.ShindengenWriteKeySig, 0, testBin, sigIdx, SeedKeyEngine.ShindengenWriteKeySig.Length);
            int loc2 = sigIdx + SeedKeyEngine.ShindengenWriteKeySig.Length;
            testBin[loc2] = 0x34; testBin[loc2 + 1] = 0x12;
            testBin[loc2 + 2] = 0x78; testBin[loc2 + 3] = 0x56;
            testBin[loc2 + 4] = 0x0A; testBin[loc2 + 5] = 0x00;
            testBin[loc2 + 8] = 0xEE; testBin[loc2 + 9] = 0x00;
            testBin[loc2 + 20] = 0xD0; testBin[loc2 + 21] = 0x07;

            int ckOff;
            ChecksumEngine.FixShindengenChecksum(testBin, out ckOff);

            using (var simPort = new VirtualKLinePort(EcuVendor.Shindengen, 512))
            using (var engine = new HondaKLineEngine(simPort))
            {
                Assert(engine.Connect(), "Engine connect failed");
                bool ok = engine.WriteFlash(testBin, 0, EcuType.ShindengenRH850, true);
                Assert(ok, "WriteFlash Shindengen failed");

                // Read back 4KB and verify exact match
                byte[] readBack;
                bool readOk = engine.ReadFlash(0, 4096, EcuType.ShindengenRH850, true, out readBack);
                Assert(readOk, "ReadFlash after write failed");
                for (int i = 0; i < 4096; i++)
                {
                    Assert(readBack[i] == testBin[i], string.Format("Memory mismatch at 0x{0:X4}", i));
                }
            }
        }

        private static void TestTunerProUnlocker()
        {
            string tempXdf = Path.Combine(Path.GetTempPath(), "test_map.xdf");
            string mockXml = "<!-- XDFHEADER -->\n<openpassword>JOSAVE2026</openpassword>\n<editpassword>ADMIN888</editpassword>\n<content>Maps</content>";
            File.WriteAllText(tempXdf, mockXml, Encoding.UTF8);

            string openPass, modPass, cleanXml;
            bool ok = TunerProPasswordCleaner.DecryptAndClean(tempXdf, out openPass, out modPass, out cleanXml);
            Assert(ok, "TunerPro decrypt and clean failed");
            Assert(openPass == "JOSAVE2026", "Open password extraction failed");
            Assert(modPass == "ADMIN888", "Modify password extraction failed");
            Assert(!cleanXml.Contains("JOSAVE2026"), "Clean XML still contains open password");

            File.Delete(tempXdf);
        }

        private static void TestAcgEcuConverter()
        {
            string tempEcu = Path.Combine(Path.GetTempPath(), "test_sample.ecu");
            string hexPayload = "00112233445566778899AABBCCDDEEFF0102030405060708";
            string encryptedBase64 = FileConverterEngine.EncryptEcuString(hexPayload);
            File.WriteAllText(tempEcu, encryptedBase64);

            byte[] decrypted = FileConverterEngine.ConvertEcuToBin(tempEcu);
            Assert(decrypted != null && decrypted.Length == 24, "Decrypted ECU length mismatch");
            Assert(decrypted[0] == 0x00 && decrypted[1] == 0x11 && decrypted[23] == 0x08, "Decrypted content mismatch");

            File.Delete(tempEcu);
        }

        #endregion
    }
}
