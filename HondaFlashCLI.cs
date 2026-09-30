using System;
using System.IO;
using System.Text;
using System.IO.Ports;
using System.Diagnostics;
using System.Collections.Generic;

namespace RemixHondaFlash
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("===============================================================================");
            Console.WriteLine("  JOSAVE TUNER - Professional Honda Motorcycle ECU Studio (K-Line)");
            Console.WriteLine("  Compatible with Keihin & Shindengen ECUs (ISO 14230 / KWP2000 / D2XX / COM)");
            Console.WriteLine("===============================================================================\n");

            if (args.Length == 0 || args[0] == "-h" || args[0] == "--help")
            {
                PrintUsage();
                return;
            }

            string command = args[0].ToLowerInvariant();
            string portName = GetArgValue(args, "--port", "FTDI"); // Default to FTDI D2XX

            try
            {
                switch (command)
                {
                    case "info":
                    case "detect":
                        RunEcmInfo(portName);
                        break;

                    case "read":
                    case "dump":
                        if (args.Length < 2) { Console.WriteLine("Error: Missing output file path. Usage: read <output.bin> [--size <kb>] [--port FTDI|COMx]"); return; }
                        string sizeKbStr = GetArgValue(args, "--size", "");
                        bool fastRead = HasFlag(args, "--fast");
                        RunReadFlash(portName, args[1], sizeKbStr, fastRead);
                        break;

                    case "write":
                    case "flash":
                        if (args.Length < 2) { Console.WriteLine("Error: Missing binary file path. Usage: write <file.bin> [--fast] [--port COMx|FTDI]"); return; }
                        bool fastWrite = HasFlag(args, "--fast");
                        string startOffsetHex = GetArgValue(args, "--start-offset", "");
                        RunWriteFlash(portName, args[1], fastWrite, startOffsetHex);
                        break;

                    case "clear-dtc":
                        RunClearDtc(portName);
                        break;

                    case "reset-ecm":
                        RunResetEcm(portName);
                        break;

                    case "reset-flash":
                        RunResetFlash(portName);
                        break;

                    case "fix-checksum":
                        if (args.Length < 2) { Console.WriteLine("Error: Missing binary file path. Usage: fix-checksum <file.bin>"); return; }
                        RunFixChecksum(args[1]);
                        break;

                    case "unlock-xdf":
                    case "unlock-adx":
                        if (args.Length < 2) { Console.WriteLine("Error: Missing file path. Usage: unlock-xdf <file.xdf|file.adx> [out-file]"); return; }
                        string outPath = args.Length >= 3 ? args[2] : "";
                        RunUnlockXdf(args[1], outPath);
                        break;

                    case "convert-acg":
                        if (args.Length < 2) { Console.WriteLine("Error: Missing file path. Usage: convert-acg <file.acg> [out.bin]"); return; }
                        RunConvertAcg(args[1], args.Length >= 3 ? args[2] : "");
                        break;

                    case "convert-ecu":
                        if (args.Length < 2) { Console.WriteLine("Error: Missing file path. Usage: convert-ecu <file.ecu> [out.bin]"); return; }
                        RunConvertEcu(args[1], args.Length >= 3 ? args[2] : "");
                        break;

                    case "list-db":
                        RunListDatabase(GetArgValue(args, "--search", ""));
                        break;

                    case "gui":
                        LaunchGui();
                        break;

                    default:
                        Console.WriteLine("Unknown command: " + command);
                        PrintUsage();
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n[ERROR] An error occurred: " + ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Commands:");
            Console.WriteLine("  info                                  Connect and read ECM ID, Part Code & Flash Count");
            Console.WriteLine("  read <out.bin> [--size <kb>] [--fast] Read/Dump full ROM from ECM via K-Line");
            Console.WriteLine("  write <bin-file> [--fast] [--port ..] Flash binary into ECM");
            Console.WriteLine("  clear-dtc                             Clear Diagnostic Trouble Codes (DTC)");
            Console.WriteLine("  reset-ecm                             Reset ECM memory and adaptations");
            Console.WriteLine("  reset-flash                           Reset Shindengen Flash Write Counter");
            Console.WriteLine("  fix-checksum <bin-file>               Automatically detect ECU and correct checksum");
            Console.WriteLine("  unlock-xdf <file.xdf|file.adx>        Remove passwords from TunerPro XDF / ADX");
            Console.WriteLine("  convert-acg <file.acg> [out.bin]      Decrypt ACG file to BIN");
            Console.WriteLine("  convert-ecu <file.ecu> [out.bin]      Decrypt ECU file to BIN");
            Console.WriteLine("  list-db [--search <keyword>]          Show list of 265+ supported Honda ECM models");
            Console.WriteLine("  gui                                   Launch graphical user interface");
            Console.WriteLine("\nOptions:");
            Console.WriteLine("  --port <FTDI|COMx>                    Specify interface (default: FTDI D2XX)");
            Console.WriteLine("  --fast                                Enable high-speed flashing mode (62500 baud)");
            Console.WriteLine("  --start-offset <hex>                  Custom start offset in hex (e.g. 0x8000)");
            Console.WriteLine("  --size <kb>                           ROM size in KB (e.g. 64, 128, 256, 384, 512, 1024)");
        }

        private static IKLinePort CreatePort(string portSpec)
        {
            if (portSpec != null && (portSpec.StartsWith("SIM", StringComparison.OrdinalIgnoreCase) || portSpec.StartsWith("VIRTUAL", StringComparison.OrdinalIgnoreCase)))
            {
                if (portSpec.IndexOf("SH", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Console.WriteLine("[Interface] Using Virtual ECM Simulator (Shindengen 512KB)...");
                    return new VirtualKLinePort(EcuVendor.Shindengen, 512);
                }
                else
                {
                    Console.WriteLine("[Interface] Using Virtual ECM Simulator (Keihin 64KB)...");
                    return new VirtualKLinePort(EcuVendor.Keihin, 64);
                }
            }
            if (string.IsNullOrEmpty(portSpec) || portSpec.Equals("FTDI", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("[Interface] Using native FTDI D2XX driver...");
                return new FtdiD2xxPort(0);
            }
            else
            {
                Console.WriteLine("[Interface] Using Serial Port: " + portSpec);
                return new SerialKLinePort(portSpec);
            }
        }

        private static void RunEcmInfo(string portSpec)
        {
            using (IKLinePort port = CreatePort(portSpec))
            using (HondaKLineEngine engine = new HondaKLineEngine(port))
            {
                Console.WriteLine("[K-Line] Initializing ISO 14230 Fast Init...");
                if (!engine.Connect())
                {
                    Console.WriteLine("[K-Line] Connection failed. Please check cable connection and turn on ignition!");
                    return;
                }

                Console.WriteLine("[K-Line] Connected successfully! Querying ECM...");
                string ecmId;
                if (engine.QueryEcmId(out ecmId))
                {
                    Console.WriteLine("-------------------------------------------------------------------------------");
                    Console.WriteLine("  ECM ID         : " + ecmId);
                    var record = EcmDatabase.LookupByEcmId(ecmId);
                    if (record != null)
                    {
                        Console.WriteLine("  Model          : " + record.ModelName);
                        Console.WriteLine("  Part Code      : " + record.PartCode);
                        Console.WriteLine("  Vendor / MCU   : " + record.Vendor + " (" + record.RomSizeKb + " KB)");
                        Console.WriteLine("  Start Offset   : 0x" + record.StartOffset.ToString("X"));
                        Console.WriteLine("  Checksum Offset: 0x" + record.ChecksumOffset.ToString("X"));
                    }
                    else
                    {
                        Console.WriteLine("  Part Code      : (Custom / Unknown in standard database)");
                    }

                    int count;
                    if (engine.QueryFlashCount(out count))
                    {
                        Console.WriteLine("  Flash Counter  : " + count);
                    }
                    Console.WriteLine("-------------------------------------------------------------------------------");
                }
                else
                {
                    Console.WriteLine("[K-Line] Failed to read ECM ID. ECU might be asleep or busy.");
                }
            }
        }

        private static void RunReadFlash(string portSpec, string outputPath, string sizeKbStr, bool fastRead)
        {
            using (IKLinePort port = CreatePort(portSpec))
            using (HondaKLineEngine engine = new HondaKLineEngine(port))
            {
                Console.WriteLine("[K-Line] Connecting to ECM...");
                if (!engine.Connect())
                {
                    Console.WriteLine("[K-Line] Connection failed. Check 12V power and cable.");
                    return;
                }

                string ecmId;
                engine.QueryEcmId(out ecmId);

                int romSize = 64 * 1024;
                EcuType ecuType = EcuType.KeihinOther;

                if (!string.IsNullOrEmpty(ecmId))
                {
                    var rec = EcmDatabase.LookupByEcmId(ecmId);
                    if (rec != null)
                    {
                        Console.WriteLine("Identified Model: " + rec.ModelName + " (" + rec.PartCode + ")");
                        romSize = rec.RomSizeKb * 1024;
                        ecuType = (rec.Vendor == EcuVendor.Shindengen) ? EcuType.ShindengenRH850 : EcuType.KeihinOther;
                    }
                }

                if (!string.IsNullOrEmpty(sizeKbStr))
                {
                    int k = Convert.ToInt32(sizeKbStr);
                    romSize = k * 1024;
                }

                Console.WriteLine(string.Format("[Dump] Reading {0} KB ROM from ECM...", romSize / 1024));

                byte[] dumped;
                bool ok = engine.ReadFlash(0, romSize, ecuType, fastRead, out dumped, (sender, e) =>
                {
                    Console.Write("\r[Dump] Progress: {0:F1}% ({1:F1} KB / {2:F1} KB) Speed: {3:F1} KB/s Time: {4:mm\\:ss}   ",
                        e.Percentage, e.BytesProcessed / 1024.0, e.TotalBytes / 1024.0, e.SpeedKbps, e.Elapsed);
                });

                Console.WriteLine();
                if (ok && dumped != null && dumped.Length > 0)
                {
                    File.WriteAllBytes(outputPath, dumped);
                    Console.WriteLine("\n===============================================================================");
                    Console.WriteLine("  [SUCCESS] FLASH READ (DUMP) COMPLETED SUCCESSFULLY!");
                    Console.WriteLine("  Dumped File: " + outputPath + " (" + dumped.Length + " bytes)");
                    Console.WriteLine("===============================================================================");
                }
                else
                {
                    Console.WriteLine("\n[FAILED] Failed to read ROM from ECM.");
                }
            }
        }

        private static void RunWriteFlash(string portSpec, string binPath, bool fastWrite, string startOffsetHex)
        {
            if (!File.Exists(binPath))
            {
                Console.WriteLine("File not found: " + binPath);
                return;
            }

            byte[] binBytes = File.ReadAllBytes(binPath);
            Console.WriteLine("Loaded binary: " + binPath + " (" + binBytes.Length + " bytes)");

            EcuType ecuType = EcmDatabase.DetectEcuTypeFromBin(binBytes);
            Console.WriteLine("Detected ECU Type: " + ecuType);

            int startOffset, ckOffset;
            EcmDatabase.DetectOffsets(binBytes, ecuType, out startOffset, out ckOffset);

            if (!string.IsNullOrEmpty(startOffsetHex))
            {
                startOffsetHex = startOffsetHex.Replace("0x", "").Replace("0X", "");
                startOffset = Convert.ToInt32(startOffsetHex, 16);
            }

            Console.WriteLine(string.Format("Start Offset: 0x{0:X}, Checksum Offset: 0x{1:X}, Fast Write: {2}", startOffset, ckOffset, fastWrite));

            using (IKLinePort port = CreatePort(portSpec))
            using (HondaKLineEngine engine = new HondaKLineEngine(port))
            {
                Console.WriteLine("[K-Line] Connecting to ECM...");
                if (!engine.Connect())
                {
                    Console.WriteLine("[K-Line] Connection failed. Please check cable and power.");
                    return;
                }

                Console.WriteLine("[Flash] Starting flash write process...");
                bool ok = engine.WriteFlash(binBytes, startOffset, ecuType, fastWrite, (sender, e) =>
                {
                    Console.Write("\r[Flash] Progress: {0:F1}% ({1:F1} KB / {2:F1} KB) Speed: {3:F1} KB/s Time: {4:mm\\:ss}   ",
                        e.Percentage, e.BytesProcessed / 1024.0, e.TotalBytes / 1024.0, e.SpeedKbps, e.Elapsed);
                });

                Console.WriteLine();
                if (ok)
                {
                    Console.WriteLine("\n===============================================================================");
                    Console.WriteLine("  [SUCCESS] FLASH WRITE COMPLETED SUCCESSFULLY!");
                    Console.WriteLine("  Please turn ignition switch OFF for 10 seconds, then turn ON to restart ECM.");
                    Console.WriteLine("===============================================================================");
                }
                else
                {
                    Console.WriteLine("\n[FAILED] Flash write failed! Please check power, connections, and retry.");
                }
            }
        }

        private static void RunClearDtc(string portSpec)
        {
            using (IKLinePort port = CreatePort(portSpec))
            using (HondaKLineEngine engine = new HondaKLineEngine(port))
            {
                Console.WriteLine("[K-Line] Connecting...");
                if (!engine.Connect())
                {
                    Console.WriteLine("[K-Line] Failed to connect.");
                    return;
                }

                Console.WriteLine("[K-Line] Sending Clear DTC command (72 05 60 01 28)...");
                if (engine.ClearDTC())
                {
                    Console.WriteLine("[SUCCESS] DTC Codes cleared successfully! Please turn ignition OFF for 5s then ON.");
                }
                else
                {
                    Console.WriteLine("[FAILED] Could not clear DTC codes.");
                }
            }
        }

        private static void RunResetEcm(string portSpec)
        {
            using (IKLinePort port = CreatePort(portSpec))
            using (HondaKLineEngine engine = new HondaKLineEngine(port))
            {
                Console.WriteLine("[K-Line] Connecting...");
                if (!engine.Connect())
                {
                    Console.WriteLine("[K-Line] Failed to connect.");
                    return;
                }

                Console.WriteLine("[K-Line] Sending Reset ECM command (72 05 60 03 26)...");
                if (engine.ResetECM())
                {
                    Console.WriteLine("[SUCCESS] ECM reset completed! Please turn ignition OFF for 5s then ON.");
                }
                else
                {
                    Console.WriteLine("[FAILED] ECM reset failed.");
                }
            }
        }

        private static void RunResetFlash(string portSpec)
        {
            using (IKLinePort port = CreatePort(portSpec))
            using (HondaKLineEngine engine = new HondaKLineEngine(port))
            {
                Console.WriteLine("[K-Line] Connecting...");
                if (!engine.Connect())
                {
                    Console.WriteLine("[K-Line] Failed to connect.");
                    return;
                }

                Console.WriteLine("[K-Line] Sending Shindengen flash counter reset sequence...");
                if (engine.ResetFlashCounterShindengen())
                {
                    Console.WriteLine("[SUCCESS] Shindengen Flash Counter reset to 0! Please turn ignition OFF for 5s then ON.");
                }
                else
                {
                    Console.WriteLine("[FAILED] Flash counter reset failed.");
                }
            }
        }

        private static void RunFixChecksum(string binPath)
        {
            if (!File.Exists(binPath))
            {
                Console.WriteLine("File not found: " + binPath);
                return;
            }

            byte[] binBytes = File.ReadAllBytes(binPath);
            Console.WriteLine("Loaded file: " + binPath + " (" + binBytes.Length + " bytes)");

            EcuType ecuType = EcmDatabase.DetectEcuTypeFromBin(binBytes);
            Console.WriteLine("Detected MCU / ECU Architecture: " + ecuType);

            int startOffset, ckOffset;
            EcmDatabase.DetectOffsets(binBytes, ecuType, out startOffset, out ckOffset);

            if (ecuType == EcuType.ShindengenV850 || ecuType == EcuType.ShindengenRH850 || ecuType == EcuType.ShindengenRH850B)
            {
                if (ChecksumEngine.FixShindengenChecksum(binBytes, out ckOffset))
                {
                    string outPath = Path.Combine(Path.GetDirectoryName(binPath), Path.GetFileNameWithoutExtension(binPath) + "_cksum.bin");
                    File.WriteAllBytes(outPath, binBytes);
                    Console.WriteLine(string.Format("[SUCCESS] Shindengen 32-bit checksum updated at 0x{0:X}!", ckOffset));
                    Console.WriteLine("Saved to: " + outPath);
                }
                else
                {
                    Console.WriteLine("[ERROR] Failed to locate Shindengen checksum table.");
                }
            }
            else
            {
                ChecksumEngine.FixKeihinChecksum(binBytes, ckOffset);
                string outPath = Path.Combine(Path.GetDirectoryName(binPath), Path.GetFileNameWithoutExtension(binPath) + "_cksum.bin");
                File.WriteAllBytes(outPath, binBytes);
                Console.WriteLine(string.Format("[SUCCESS] Keihin 8-bit checksum updated at 0x{0:X} (Checksum byte = 0x{1:X2})!", ckOffset, binBytes[ckOffset]));
                Console.WriteLine("Saved to: " + outPath);
            }
        }

        private static void RunUnlockXdf(string inputPath, string outputPath)
        {
            string openPass, modPass, cleanXml;
            if (TunerProPasswordCleaner.DecryptAndClean(inputPath, out openPass, out modPass, out cleanXml))
            {
                if (string.IsNullOrEmpty(outputPath))
                {
                    outputPath = Path.Combine(Path.GetDirectoryName(inputPath), Path.GetFileNameWithoutExtension(inputPath) + "_unlocked" + Path.GetExtension(inputPath));
                }

                File.WriteAllText(outputPath, cleanXml, Encoding.UTF8);
                Console.WriteLine("-------------------------------------------------------------------------------");
                Console.WriteLine("  [SUCCESS] TunerPro File Unlocked!");
                if (!string.IsNullOrEmpty(openPass)) Console.WriteLine("  Open Password   : " + openPass);
                if (!string.IsNullOrEmpty(modPass)) Console.WriteLine("  Modify Password : " + modPass);
                Console.WriteLine("  Unlocked File   : " + outputPath);
                Console.WriteLine("-------------------------------------------------------------------------------");
            }
            else
            {
                Console.WriteLine("[ERROR] Failed to decrypt or unlock file. File may not be a password-protected TunerPro file.");
            }
        }

        private static void RunConvertAcg(string inputPath, string outputPath)
        {
            try
            {
                byte[] bin = FileConverterEngine.ConvertAcgToBin(inputPath);
                if (string.IsNullOrEmpty(outputPath))
                {
                    outputPath = Path.Combine(Path.GetDirectoryName(inputPath), Path.GetFileNameWithoutExtension(inputPath) + ".bin");
                }
                File.WriteAllBytes(outputPath, bin);
                Console.WriteLine("[SUCCESS] ACG converted to BIN (" + bin.Length + " bytes) -> " + outputPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[ERROR] Failed to convert ACG: " + ex.Message);
            }
        }

        private static void RunConvertEcu(string inputPath, string outputPath)
        {
            try
            {
                byte[] bin = FileConverterEngine.ConvertEcuToBin(inputPath);
                if (string.IsNullOrEmpty(outputPath))
                {
                    outputPath = Path.Combine(Path.GetDirectoryName(inputPath), Path.GetFileNameWithoutExtension(inputPath) + ".bin");
                }
                File.WriteAllBytes(outputPath, bin);
                Console.WriteLine("[SUCCESS] ECU converted to BIN (" + bin.Length + " bytes) -> " + outputPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[ERROR] Failed to convert ECU: " + ex.Message);
            }
        }

        private static void RunListDatabase(string query)
        {
            query = (query ?? "").ToLowerInvariant().Trim();
            Console.WriteLine("===============================================================================");
            Console.WriteLine("  JOSAVE TUNER - SUPPORTED HONDA MOTORCYCLE ECM MODELS (" + EcmDatabase.AllRecords.Count + " models)");
            Console.WriteLine("===============================================================================");

            int count = 0;
            foreach (var r in EcmDatabase.AllRecords)
            {
                if (string.IsNullOrEmpty(query) ||
                    r.ModelName.ToLowerInvariant().Contains(query) ||
                    r.PartCode.ToLowerInvariant().Contains(query) ||
                    r.EcmId.ToLowerInvariant().Contains(query))
                {
                    Console.WriteLine(string.Format("  [{0,3}] {1,-35} | Part: {2,-16} | ID: {3,-10} | {4,4} KB | Start: 0x{5,-5:X} | Checksum: 0x{6:X}",
                        r.Index + 1, r.ModelName, r.PartCode, r.EcmId, r.RomSizeKb, r.StartOffset, r.ChecksumOffset));
                    count++;
                }
            }

            Console.WriteLine("\nTotal matched models: " + count);
        }

        private static void LaunchGui()
        {
            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            string guiPath = Path.Combine(exeDir, "JosaveTuner.exe");
            if (File.Exists(guiPath))
            {
                Console.WriteLine("[JOSAVE TUNER] Launching Graphical User Interface...");
                Process.Start(guiPath);
            }
            else
            {
                Console.WriteLine("[ERROR] JosaveTuner.exe not found at: " + guiPath);
            }
        }

        private static string GetArgValue(string[] args, string flag, string defaultVal)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i].Equals(flag, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }
            return defaultVal;
        }

        private static bool HasFlag(string[] args, string flag)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Equals(flag, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
