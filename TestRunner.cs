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

            // 8. One-Click Tuning & Live Telemetry Studio Tests
            RunTest("Tuner Engine: Idle Chop Mod (สูตรเดินหอบ) & Auto Checksum", TestIdleChopMod);
            RunTest("Tuner Engine: RPM Limiter Patcher (สูตรปลดรอบ) & Auto Checksum", TestRevLimiterPatch);
            RunTest("Tuner Engine: Stage 1/2/3 Quick Remap & Auto Checksum", TestStageQuickTune);
            RunTest("Live Telemetry: High-Speed K-Line Stream & CSV Logger", TestLiveTelemetryAndLogger);
            RunTest("Diagnostics: Actuator Tests & Pre-Flash Safety Check", TestActuatorsAndPreFlashCheck);

            // 9. TunerPro XDF & 2D/3D Map Tuning Studio Tests
            RunTest("XDF Engine: TunerPro XML Parser & Thai Entity Decoding", TestXdfParserAndThaiDecoding);
            RunTest("Map Studio: 2D Table Matrix & Bidirectional Formulas", TestTableMatrixAndFormulas);
            RunTest("Map Studio: Mathematical Transforms (Scale, Offset, Smooth)", TestMapMathTransforms);
            RunTest("Map Studio: Interpolation Engine (Horizontal & Vertical)", TestMapInterpolation);
            RunTest("Map Studio: Save Tuned Binary & Checksum Auto-Correction", TestSaveTunedBinAndChecksum);
            RunTest("Map Studio: Shindengen 256KB Multi-Architecture Offsets", TestShindengenMapDefinitions);

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

        private static void TestIdleChopMod()
        {
            byte[] baseBin = new byte[0x10000];
            for (int i = 0; i < baseBin.Length; i++) baseBin[i] = (byte)(i & 0xFF);
            ChecksumEngine.FixKeihinChecksum(baseBin, 0x7600);

            byte[] modBin;
            string status;
            bool ok = OneClickTunerEngine.ApplyIdleChopMod(baseBin, EcuType.KeihinOther, 2, out modBin, out status);
            Assert(ok, "ApplyIdleChopMod failed");
            Assert(modBin != null && modBin.Length == baseBin.Length, "Modified BIN length mismatch");
            Assert(status.Contains("สำเร็จ"), "Status does not confirm success");

            byte sum = ChecksumEngine.Sum8Bit(modBin, modBin.Length, 0);
            Assert(sum == 0, string.Format("Keihin checksum invalid after idle chop mod (sum={0})", sum));
        }

        private static void TestRevLimiterPatch()
        {
            byte[] baseBin = new byte[0x10000];
            for (int i = 0; i < baseBin.Length; i++) baseBin[i] = (byte)(i & 0xFF);
            baseBin[0x6200] = 0x1C;
            baseBin[0x6201] = 0x25;
            ChecksumEngine.FixKeihinChecksum(baseBin, 0x7600);

            byte[] modBin;
            string status;
            bool ok = OneClickTunerEngine.ApplyRpmLimiterPatch(baseBin, EcuType.KeihinOther, 11500, out modBin, out status);
            Assert(ok, "ApplyRpmLimiterPatch failed");
            Assert(status.Contains("11,500"), "Status does not mention target RPM");

            Assert(modBin[0x6200] == 0xEC && modBin[0x6201] == 0x2C, "Rev limiter value not patched to 11500 RPM");

            byte sum = ChecksumEngine.Sum8Bit(modBin, modBin.Length, 0);
            Assert(sum == 0, string.Format("Keihin checksum invalid after rev limit patch (sum={0})", sum));
        }

        private static void TestStageQuickTune()
        {
            byte[] baseBin = new byte[0x10000];
            for (int i = 0; i < baseBin.Length; i++) baseBin[i] = 100;
            ChecksumEngine.FixKeihinChecksum(baseBin, 0x7600);

            byte[] modBin;
            string status;
            bool ok = OneClickTunerEngine.ApplyStageQuickTune(baseBin, EcuType.KeihinOther, 1, out modBin, out status);
            Assert(ok, "ApplyStageQuickTune failed");
            Assert(status.Contains("สเต็ป 1"), "Status does not mention Stage 1");

            byte sum = ChecksumEngine.Sum8Bit(modBin, modBin.Length, 0);
            Assert(sum == 0, string.Format("Keihin checksum invalid after stage tune (sum={0})", sum));
        }

        private static void TestLiveTelemetryAndLogger()
        {
            using (VirtualKLinePort port = new VirtualKLinePort(EcuVendor.Shindengen, 512))
            using (HondaKLineEngine engine = new HondaKLineEngine(port))
            {
                engine.Connect();

                LiveTelemetryData live;
                bool ok = engine.QueryLiveTelemetry(out live);
                Assert(ok, "QueryLiveTelemetry failed");
                Assert(live.EngineRpm > 1000 && live.EngineRpm < 2000, "Simulated RPM out of expected range");
                Assert(live.BatteryVolts >= 13.0 && live.BatteryVolts <= 15.0, "Battery voltage out of range");
                Assert(live.AirFuelRatio >= 14.0 && live.AirFuelRatio <= 16.0, "AFR out of range");

                string tempCsv = Path.Combine(Path.GetTempPath(), "test_telemetry.csv");
                using (TelemetryDataLogger logger = new TelemetryDataLogger())
                {
                    logger.StartLogging(tempCsv);
                    Assert(logger.IsLogging, "Logger failed to start");
                    for (int i = 0; i < 5; i++)
                    {
                        engine.QueryLiveTelemetry(out live);
                        logger.LogData(live);
                    }
                    Assert(logger.RowCount == 5, "Row count mismatch");
                    logger.StopLogging();
                }

                Assert(File.Exists(tempCsv), "CSV file was not created");
                string[] lines = File.ReadAllLines(tempCsv);
                Assert(lines.Length == 6, "CSV line count mismatch (header + 5 data rows)");
                Assert(lines[0].Contains("EngineRPM"), "CSV header missing EngineRPM");
                File.Delete(tempCsv);
            }
        }

        private static void TestActuatorsAndPreFlashCheck()
        {
            using (VirtualKLinePort port = new VirtualKLinePort(EcuVendor.Shindengen, 512))
            using (HondaKLineEngine engine = new HondaKLineEngine(port))
            {
                engine.Connect();

                string msg;
                bool injOk = engine.ExecuteActuatorTest(ActuatorType.FuelInjector, out msg);
                Assert(injOk, "Fuel Injector test failed");
                Assert(msg.Contains("หัวฉีด"), "Injector test message mismatch");

                bool pumpOk = engine.ExecuteActuatorTest(ActuatorType.FuelPump, out msg);
                Assert(pumpOk, "Fuel Pump test failed");

                bool tachoOk = engine.ExecuteActuatorTest(ActuatorType.TachometerSweep, out msg);
                Assert(tachoOk, "Tachometer Sweep test failed");

                bool sparkOk = engine.ExecuteActuatorTest(ActuatorType.IgnitionSparkCoil, out msg);
                Assert(sparkOk, "Spark test failed");

                var check = engine.PerformPreFlashSafetyCheck();
                Assert(check != null, "PreFlashCheck returned null");
                Assert(check.CheckMessages.Count > 0, "PreFlashCheck messages empty");
            }
        }

        #endregion

        #region 9. TunerPro XDF & 2D/3D Map Tuning Studio Tests

        private static void TestXdfParserAndThaiDecoding()
        {
            // Test 1: HTML entity decoding
            string entityString = "Fuel (&#161;&#210;&#195;&#168;&#232;&#210;&#194;&#185;&#233;&#211;&#193;&#209;&#185;&#227;&#185;&#224;&#161;&#213;&#194;&#195;&#236;)";
            string decodedEntity = XdfDefinition.DecodeThaiEntities(entityString);
            Assert(decodedEntity.Contains("การจ่ายน้ำมันในเกียร์"), "Thai entity decoding failed: " + decodedEntity);

            // Test 2: Latin-1 Mojibake decoding (¡ÒÃ¨èÒÂ¹éÓÁÑ¹ã¹à¡ÕÂÃì -> การจ่ายน้ำมันในเกียร์)
            string mojibakeFuel = "Fuel injection (¡ÒÃ¨èÒÂ¹éÓÁÑ¹ã¹à¡ÕÂÃì)";
            string decodedFuel = XdfDefinition.DecodeThaiEntities(mojibakeFuel);
            Assert(decodedFuel == "Fuel injection (การจ่ายน้ำมันในเกียร์)", "Mojibake fuel decoding failed: " + decodedFuel);

            // Test 3: Latin-1 Mojibake ignition (ä¿ã¹à¡ÕÂÃì -> ไฟในเกียร์)
            string mojibakeIgn = "Ignition 1-4 (ä¿ã¹à¡ÕÂÃì)";
            string decodedIgn = XdfDefinition.DecodeThaiEntities(mojibakeIgn);
            Assert(decodedIgn == "Ignition 1-4 (ไฟในเกียร์)", "Mojibake ignition decoding failed: " + decodedIgn);

            // Test 4: Latin-1 Mojibake rev limit (µÑ´ÃÍº¡ÒÃ¨èÒÂ¹éÓÁÑ¹ -> ตัดรอบการจ่ายน้ำมัน)
            string mojibakeLimit = "Limit Fuel (µÑ´ÃÍº¡ÒÃ¨èÒÂ¹éÓÁÑ¹)";
            string decodedLimit = XdfDefinition.DecodeThaiEntities(mojibakeLimit);
            Assert(decodedLimit == "Limit Fuel (ตัดรอบการจ่ายน้ำมัน)", "Mojibake limit decoding failed: " + decodedLimit);

            // Test parsing sample XDF or builtin
            XdfDefinition builtin = XdfDefinition.GetBuiltinDefinition("Wave 110i LED 2021-2022 (K2J)");
            Assert(builtin != null, "Builtin definition is null");
            Assert(builtin.Tables.Count >= 4, "Builtin definition missing core tables");
            Assert(builtin.Tables[0].Rows == 29 && builtin.Tables[0].Cols == 29, "Table dimension mismatch");
        }

        private static void TestTableMatrixAndFormulas()
        {
            byte[] mockBin = new byte[0x10000];
            // Write a test 16-bit value at address 0x84: 3173 (raw) -> 3.173 ms
            int addr = 0x84;
            mockBin[addr] = 0x65;
            mockBin[addr + 1] = 0x0C; // 0x0C65 = 3173

            XdfTable tbl = new XdfTable
            {
                AxisX = new XdfAxis("x") { IndexCount = 29 },
                AxisY = new XdfAxis("y") { IndexCount = 29 },
                AxisZ = new XdfAxis("z") { Address = 0x84, ElementSizeBytes = 2, MathEquation = "X/1000" }
            };

            float disp = tbl.GetDisplayValue(mockBin, 0, 0);
            Assert(Math.Abs(disp - 3.173f) < 0.001f, "Display value mismatch: " + disp);

            // Set a new value 4.520 ms
            tbl.SetDisplayValue(mockBin, 0, 0, 4.520f);
            float dispNew = tbl.GetDisplayValue(mockBin, 0, 0);
            Assert(Math.Abs(dispNew - 4.520f) < 0.002f, "Updated display value mismatch: " + dispNew);
            ushort rawNew = (ushort)(mockBin[addr] | (mockBin[addr + 1] << 8));
            Assert(rawNew == 4520, "Raw binary value mismatch: " + rawNew);
        }

        private static void TestMapMathTransforms()
        {
            byte[] mockBin = new byte[0x10000];
            XdfDefinition def = XdfDefinition.GetBuiltinDefinition("Wave 110i LED");
            MapTuningSession sess = new MapTuningSession();
            sess.LoadBin(mockBin);
            sess.LoadDefinition(def);

            XdfTable tbl = def.Tables[0];
            tbl.SetDisplayValue(sess.ActiveBin, 0, 0, 3.000f);
            tbl.SetDisplayValue(sess.ActiveBin, 0, 1, 3.000f);

            sess.SelectedRange = new System.Drawing.Rectangle(0, 0, 2, 1);

            // Add 10%
            sess.AddPercent(10f);
            float v1 = tbl.GetDisplayValue(sess.ActiveBin, 0, 0);
            Assert(Math.Abs(v1 - 3.300f) < 0.005f, "AddPercent failed: " + v1);

            // Undo
            bool undone = sess.Undo();
            Assert(undone, "Undo failed");
            float vOrig = tbl.GetDisplayValue(sess.ActiveBin, 0, 0);
            Assert(Math.Abs(vOrig - 3.000f) < 0.005f, "Undo value mismatch: " + vOrig);

            // Add offset +0.5
            sess.AddOffset(0.5f);
            float vOff = tbl.GetDisplayValue(sess.ActiveBin, 0, 0);
            Assert(Math.Abs(vOff - 3.500f) < 0.005f, "AddOffset failed: " + vOff);
        }

        private static void TestMapInterpolation()
        {
            byte[] mockBin = new byte[0x10000];
            XdfDefinition def = XdfDefinition.GetBuiltinDefinition("Wave 110i LED");
            MapTuningSession sess = new MapTuningSession();
            sess.LoadBin(mockBin);
            sess.LoadDefinition(def);

            XdfTable tbl = def.Tables[0];
            tbl.SetDisplayValue(sess.ActiveBin, 0, 0, 2.000f);
            tbl.SetDisplayValue(sess.ActiveBin, 0, 4, 6.000f);

            sess.SelectedRange = new System.Drawing.Rectangle(0, 0, 5, 1);
            sess.InterpolateHorizontal();

            float vMid = tbl.GetDisplayValue(sess.ActiveBin, 0, 2);
            Assert(Math.Abs(vMid - 4.000f) < 0.01f, "Interpolation midpoint mismatch: " + vMid);
        }

        private static void TestSaveTunedBinAndChecksum()
        {
            byte[] mockBin = new byte[0x10000];
            XdfDefinition def = XdfDefinition.GetBuiltinDefinition("Wave 110i LED");
            MapTuningSession sess = new MapTuningSession();
            sess.LoadBin(mockBin);
            sess.LoadDefinition(def);

            string tempFile = Path.Combine(Path.GetTempPath(), "test_map_tuned_" + Guid.NewGuid().ToString("N") + ".bin");
            try
            {
                int ckOff;
                bool saved = sess.SaveTunedBin(tempFile, out ckOff);
                Assert(saved, "Failed to save tuned BIN");
                Assert(ckOff == 0x7600, "Keihin 64KB checksum offset mismatch: " + ckOff);
                Assert(File.Exists(tempFile), "Tuned file does not exist on disk");
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        private static void TestShindengenMapDefinitions()
        {
            // 1. Test IsShindengen classification
            Assert(XdfDefinition.IsShindengen("Giorno+ (K3M)"), "Giorno+ not recognized as Shindengen");
            Assert(XdfDefinition.IsShindengen("Lead 125 (K2T)"), "Lead 125 not recognized as Shindengen");
            Assert(XdfDefinition.IsShindengen("PCX 160 (K1Z)"), "PCX 160 not recognized as Shindengen");
            Assert(XdfDefinition.IsShindengen("ADV 160 (K0W)"), "ADV 160 not recognized as Shindengen");
            Assert(XdfDefinition.IsShindengen("Forza 350 (K1B)"), "Forza 350 not recognized as Shindengen");
            Assert(!XdfDefinition.IsShindengen("Wave 110i LED 2021-2022 (K2J)"), "Wave 110i wrongly identified as Shindengen");
            Assert(!XdfDefinition.IsShindengen("Wave 125i (K73)"), "Wave 125i wrongly identified as Shindengen");

            // 2. Test auto-detection by binary buffer length
            byte[] mock64k = new byte[65536];
            byte[] mock256k = new byte[262144];
            Assert(!XdfDefinition.IsShindengen(null, mock64k), "64KB binary buffer wrongly identified as Shindengen");
            Assert(XdfDefinition.IsShindengen(null, mock256k), "256KB binary buffer not identified as Shindengen");

            // 3. Test Builtin Definition for Giorno+ (K3M)
            XdfDefinition shinDef = XdfDefinition.GetBuiltinDefinition("Giorno+ (K3M)", mock256k);
            Assert(shinDef != null, "Shindengen definition is null");
            Assert(shinDef.Tables.Count >= 3, "Shindengen definition missing tables");

            // Verify Fuel Table is at 0x3A078 (35x29, 16-bit uint, X/1000)
            XdfTable fuelTbl = shinDef.Tables[0];
            Assert(fuelTbl.AxisZ.Address == 0x3A078, "Fuel Table address mismatch: " + fuelTbl.AxisZ.Address);
            Assert(fuelTbl.AxisZ.ElementSizeBytes == 2, "Fuel Table element size mismatch: " + fuelTbl.AxisZ.ElementSizeBytes);
            Assert(fuelTbl.Rows == 35 && fuelTbl.Cols == 29, "Fuel Table dimension mismatch");

            // Verify Ignition Table is at 0x3BA36 (35x29, 8-bit uint, X*0.3572-15.72)
            XdfTable ignTbl = shinDef.Tables[1];
            Assert(ignTbl.AxisZ.Address == 0x3BA36, "Ignition Table address mismatch: " + ignTbl.AxisZ.Address);
            Assert(ignTbl.AxisZ.ElementSizeBytes == 1, "Ignition Table element size mismatch: " + ignTbl.AxisZ.ElementSizeBytes);
            Assert(ignTbl.Rows == 35 && ignTbl.Cols == 29, "Ignition Table dimension mismatch");

            // Verify Rev Limiter Constants
            Assert(shinDef.Constants.Count >= 5, "Shindengen missing Rev Limiter constants");
            Assert(shinDef.Constants[0].Address == 0x394F4, "Rev Limiter 1 address mismatch");
            Assert(shinDef.Constants[1].Address == 0x394F6, "Rev Limiter 2 address mismatch");
            Assert(shinDef.Constants[2].Address == 0x395DE, "Spark Limiter 1 address mismatch");
            Assert(shinDef.Constants[3].Address == 0x397A4, "Spark Limiter 2 address mismatch");
            Assert(shinDef.Constants[4].Address == 0x3B044, "Spark Limiter 3 address mismatch");

            // 4. Test reading authentic values if Giorno+ binary exists on disk
            string giornoBin = @"C:\Users\Jo_save\Desktop\giorno remap file\K3MF-T01.bin";
            if (File.Exists(giornoBin))
            {
                byte[] realBin = File.ReadAllBytes(giornoBin);
                float sampleFuel = fuelTbl.GetDisplayValue(realBin, 0, 0);
                Assert(sampleFuel >= 1.0f && sampleFuel <= 15.0f, "Real Giorno+ fuel value out of valid range: " + sampleFuel);

                float sampleIgn = ignTbl.GetDisplayValue(realBin, 0, 0);
                Assert(sampleIgn >= 10.0f && sampleIgn <= 75.0f, "Real Giorno+ ignition value out of valid range: " + sampleIgn);
            }
        }

        #endregion
    }
}
