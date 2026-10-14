using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Xml;
using System.Diagnostics;

namespace RemixHondaFlash
{
    #region XDF Axis & Table Models

    public class XdfAxis
    {
        public string Id { get; set; }           // "x", "y", "z"
        public string UniqueId { get; set; }
        public string Units { get; set; }        // "RPM", "TPS %", "ms", "deg BTDC", "kPa"
        public int IndexCount { get; set; }      // Number of points (e.g. 29)
        public int Address { get; set; }         // Address in binary
        public int ElementSizeBytes { get; set; }// 1 (8-bit) or 2 (16-bit)
        public bool IsLsbFirst { get; set; }     // Little-endian vs Big-endian
        public string MathEquation { get; set; } // e.g. "X/1000", "X*0.3572-15.72", "X*0.0976"
        public string[] Labels { get; set; }     // Pre-defined breakpoint labels (e.g. "500", "1000"...)
        public string LinkObjId { get; set; }    // Linked breakpoint object ID

        public XdfAxis(string id)
        {
            Id = id;
            Units = "";
            IndexCount = 1;
            Address = 0;
            ElementSizeBytes = 1;
            IsLsbFirst = true;
            MathEquation = "X";
            Labels = new string[0];
            LinkObjId = "";
        }
    }

    public class XdfTable
    {
        public string UniqueId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string CategoryName { get; set; }
        public XdfAxis AxisX { get; set; }       // Columns (usually TPS % or Load)
        public XdfAxis AxisY { get; set; }       // Rows (usually RPM)
        public XdfAxis AxisZ { get; set; }       // Values (Fuel ms, Spark deg, etc.)
        public int DecimalPlaces { get; set; }   // Formatted decimal digits
        public float MinValue { get; set; }
        public float MaxValue { get; set; }

        public XdfTable()
        {
            UniqueId = "";
            Title = "Untitled Table";
            Description = "";
            CategoryName = "General";
            AxisX = new XdfAxis("x");
            AxisY = new XdfAxis("y");
            AxisZ = new XdfAxis("z");
            DecimalPlaces = 2;
            MinValue = -100f;
            MaxValue = 1000f;
        }

        public int Rows
        {
            get { return (AxisY != null && AxisY.IndexCount > 0) ? AxisY.IndexCount : 1; }
        }

        public int Cols
        {
            get { return (AxisX != null && AxisX.IndexCount > 0) ? AxisX.IndexCount : 1; }
        }

        public float GetRawValue(byte[] bin, int row, int col)
        {
            if (bin == null) return 0f;
            int elemSize = AxisZ.ElementSizeBytes <= 0 ? 1 : AxisZ.ElementSizeBytes;
            int offset = AxisZ.Address + ((row * Cols + col) * elemSize);
            if (offset < 0 || offset + elemSize > bin.Length) return 0f;

            if (elemSize == 1)
            {
                return bin[offset];
            }
            else if (elemSize == 2)
            {
                if (AxisZ.IsLsbFirst)
                    return (ushort)(bin[offset] | (bin[offset + 1] << 8));
                else
                    return (ushort)((bin[offset] << 8) | bin[offset + 1]);
            }
            else if (elemSize == 4)
            {
                if (AxisZ.IsLsbFirst)
                    return BitConverter.ToUInt32(bin, offset);
                else
                    return (uint)((bin[offset] << 24) | (bin[offset + 1] << 16) | (bin[offset + 2] << 8) | bin[offset + 3]);
            }
            return 0f;
        }

        public float GetDisplayValue(byte[] bin, int row, int col)
        {
            float raw = GetRawValue(bin, row, col);
            return EvaluateMath(raw, AxisZ.MathEquation);
        }

        public void SetDisplayValue(byte[] bin, int row, int col, float displayVal)
        {
            if (bin == null) return;
            float raw = ReverseMath(displayVal, AxisZ.MathEquation);
            int elemSize = AxisZ.ElementSizeBytes <= 0 ? 1 : AxisZ.ElementSizeBytes;
            int offset = AxisZ.Address + ((row * Cols + col) * elemSize);
            if (offset < 0 || offset + elemSize > bin.Length) return;

            if (elemSize == 1)
            {
                bin[offset] = (byte)Math.Max(0, Math.Min(255, (int)Math.Round(raw)));
            }
            else if (elemSize == 2)
            {
                ushort val = (ushort)Math.Max(0, Math.Min(65535, (int)Math.Round(raw)));
                if (AxisZ.IsLsbFirst)
                {
                    bin[offset] = (byte)(val & 0xFF);
                    bin[offset + 1] = (byte)((val >> 8) & 0xFF);
                }
                else
                {
                    bin[offset] = (byte)((val >> 8) & 0xFF);
                    bin[offset + 1] = (byte)(val & 0xFF);
                }
            }
            else if (elemSize == 4)
            {
                uint val = (uint)Math.Max(0, Math.Min(uint.MaxValue, (long)Math.Round(raw)));
                if (AxisZ.IsLsbFirst)
                {
                    byte[] bytes = BitConverter.GetBytes(val);
                    Array.Copy(bytes, 0, bin, offset, 4);
                }
                else
                {
                    bin[offset] = (byte)((val >> 24) & 0xFF);
                    bin[offset + 1] = (byte)((val >> 16) & 0xFF);
                    bin[offset + 2] = (byte)((val >> 8) & 0xFF);
                    bin[offset + 3] = (byte)(val & 0xFF);
                }
            }
        }

        public string GetRowLabel(byte[] bin, int row)
        {
            if (AxisY.Labels != null && row < AxisY.Labels.Length && !string.IsNullOrEmpty(AxisY.Labels[row]))
            {
                return AxisY.Labels[row];
            }
            if (AxisY.Address > 0 && bin != null)
            {
                int elemSize = AxisY.ElementSizeBytes <= 0 ? 2 : AxisY.ElementSizeBytes;
                int off = AxisY.Address + (row * elemSize);
                if (off >= 0 && off + elemSize <= bin.Length)
                {
                    float raw = (elemSize == 1) ? bin[off] : (ushort)(bin[off] | (bin[off + 1] << 8));
                    float val = EvaluateMath(raw, AxisY.MathEquation);
                    return val.ToString("F0");
                }
            }
            return (row + 1).ToString();
        }

        public string GetColLabel(byte[] bin, int col)
        {
            if (AxisX.Labels != null && col < AxisX.Labels.Length && !string.IsNullOrEmpty(AxisX.Labels[col]))
            {
                return AxisX.Labels[col];
            }
            if (AxisX.Address > 0 && bin != null)
            {
                int elemSize = AxisX.ElementSizeBytes <= 0 ? 2 : AxisX.ElementSizeBytes;
                int off = AxisX.Address + (col * elemSize);
                if (off >= 0 && off + elemSize <= bin.Length)
                {
                    float raw = (elemSize == 1) ? bin[off] : (ushort)(bin[off] | (bin[off + 1] << 8));
                    float val = EvaluateMath(raw, AxisX.MathEquation);
                    return val.ToString("F1") + "%";
                }
            }
            return (col + 1).ToString();
        }

        public static float EvaluateMath(float x, string eq)
        {
            if (string.IsNullOrEmpty(eq) || eq.Trim() == "X") return x;
            eq = eq.Replace(" ", "").ToUpper();

            try
            {
                // Pattern: X/1000
                if (eq.StartsWith("X/"))
                {
                    float div = float.Parse(eq.Substring(2), System.Globalization.CultureInfo.InvariantCulture);
                    return div != 0 ? x / div : x;
                }
                // Pattern: X*factor
                if (eq.StartsWith("X*") && !eq.Contains("+") && !eq.Contains("-"))
                {
                    float mul = float.Parse(eq.Substring(2), System.Globalization.CultureInfo.InvariantCulture);
                    return x * mul;
                }
                // Pattern: X*factor-offset
                if (eq.Contains("X*") && eq.Contains("-"))
                {
                    int mulEnd = eq.IndexOf("-");
                    float mul = float.Parse(eq.Substring(2, mulEnd - 2), System.Globalization.CultureInfo.InvariantCulture);
                    float off = float.Parse(eq.Substring(mulEnd + 1), System.Globalization.CultureInfo.InvariantCulture);
                    return (x * mul) - off;
                }
                // Pattern: X*factor+offset
                if (eq.Contains("X*") && eq.Contains("+"))
                {
                    int mulEnd = eq.IndexOf("+");
                    float mul = float.Parse(eq.Substring(2, mulEnd - 2), System.Globalization.CultureInfo.InvariantCulture);
                    float off = float.Parse(eq.Substring(mulEnd + 1), System.Globalization.CultureInfo.InvariantCulture);
                    return (x * mul) + off;
                }
                // Pattern: X-offset
                if (eq.StartsWith("X-"))
                {
                    float off = float.Parse(eq.Substring(2), System.Globalization.CultureInfo.InvariantCulture);
                    return x - off;
                }
                // Pattern: X+offset
                if (eq.StartsWith("X+"))
                {
                    float off = float.Parse(eq.Substring(2), System.Globalization.CultureInfo.InvariantCulture);
                    return x + off;
                }
            }
            catch { }
            return x;
        }

        public static float ReverseMath(float displayVal, string eq)
        {
            if (string.IsNullOrEmpty(eq) || eq.Trim() == "X") return displayVal;
            eq = eq.Replace(" ", "").ToUpper();

            try
            {
                if (eq.StartsWith("X/"))
                {
                    float div = float.Parse(eq.Substring(2), System.Globalization.CultureInfo.InvariantCulture);
                    return displayVal * div;
                }
                if (eq.StartsWith("X*") && !eq.Contains("+") && !eq.Contains("-"))
                {
                    float mul = float.Parse(eq.Substring(2), System.Globalization.CultureInfo.InvariantCulture);
                    return mul != 0 ? displayVal / mul : displayVal;
                }
                if (eq.Contains("X*") && eq.Contains("-"))
                {
                    int mulEnd = eq.IndexOf("-");
                    float mul = float.Parse(eq.Substring(2, mulEnd - 2), System.Globalization.CultureInfo.InvariantCulture);
                    float off = float.Parse(eq.Substring(mulEnd + 1), System.Globalization.CultureInfo.InvariantCulture);
                    return mul != 0 ? (displayVal + off) / mul : displayVal;
                }
                if (eq.Contains("X*") && eq.Contains("+"))
                {
                    int mulEnd = eq.IndexOf("+");
                    float mul = float.Parse(eq.Substring(2, mulEnd - 2), System.Globalization.CultureInfo.InvariantCulture);
                    float off = float.Parse(eq.Substring(mulEnd + 1), System.Globalization.CultureInfo.InvariantCulture);
                    return mul != 0 ? (displayVal - off) / mul : displayVal;
                }
                if (eq.StartsWith("X-"))
                {
                    float off = float.Parse(eq.Substring(2), System.Globalization.CultureInfo.InvariantCulture);
                    return displayVal + off;
                }
                if (eq.StartsWith("X+"))
                {
                    float off = float.Parse(eq.Substring(2), System.Globalization.CultureInfo.InvariantCulture);
                    return displayVal - off;
                }
            }
            catch { }
            return displayVal;
        }
    }

    public class XdfConstant
    {
        public string UniqueId { get; set; }
        public string Title { get; set; }
        public string CategoryName { get; set; }
        public int Address { get; set; }
        public int ElementSizeBytes { get; set; }
        public bool IsLsbFirst { get; set; }
        public string MathEquation { get; set; }
        public string Units { get; set; }
        public int DecimalPlaces { get; set; }

        public XdfConstant()
        {
            UniqueId = "";
            Title = "Constant";
            CategoryName = "Scalars";
            Address = 0;
            ElementSizeBytes = 2;
            IsLsbFirst = true;
            MathEquation = "X";
            Units = "";
            DecimalPlaces = 0;
        }

        public float GetDisplayValue(byte[] bin)
        {
            if (bin == null || Address < 0 || Address + ElementSizeBytes > bin.Length) return 0f;
            float raw = (ElementSizeBytes == 1) ? bin[Address] :
                        (ElementSizeBytes == 2) ? (IsLsbFirst ? (ushort)(bin[Address] | (bin[Address + 1] << 8)) : (ushort)((bin[Address] << 8) | bin[Address + 1])) :
                        BitConverter.ToUInt32(bin, Address);
            return XdfTable.EvaluateMath(raw, MathEquation);
        }

        public void SetDisplayValue(byte[] bin, float val)
        {
            if (bin == null || Address < 0 || Address + ElementSizeBytes > bin.Length) return;
            float raw = XdfTable.ReverseMath(val, MathEquation);
            if (ElementSizeBytes == 1)
            {
                bin[Address] = (byte)Math.Max(0, Math.Min(255, (int)Math.Round(raw)));
            }
            else if (ElementSizeBytes == 2)
            {
                ushort s = (ushort)Math.Max(0, Math.Min(65535, (int)Math.Round(raw)));
                if (IsLsbFirst)
                {
                    bin[Address] = (byte)(s & 0xFF);
                    bin[Address + 1] = (byte)((s >> 8) & 0xFF);
                }
                else
                {
                    bin[Address] = (byte)((s >> 8) & 0xFF);
                    bin[Address + 1] = (byte)(s & 0xFF);
                }
            }
        }
    }

    #endregion

    #region XDF Definition Parser & Factory

    public class XdfDefinition
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public List<string> Categories { get; private set; }
        public List<XdfTable> Tables { get; private set; }
        public List<XdfConstant> Constants { get; private set; }

        public XdfDefinition()
        {
            Title = "Honda Definition";
            Description = "";
            Categories = new List<string>();
            Tables = new List<XdfTable>();
            Constants = new List<XdfConstant>();
        }

        public static string DecodeThaiEntities(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";

            Encoding cp874;
            try { cp874 = Encoding.GetEncoding(874); } catch { cp874 = Encoding.UTF8; }

            // Step 1: Decode HTML entities like &#161;&#210;...
            if (input.Contains("&#"))
            {
                StringBuilder sb = new StringBuilder();
                int i = 0;
                while (i < input.Length)
                {
                    if (input[i] == '&' && i + 1 < input.Length && input[i + 1] == '#')
                    {
                        List<byte> bytes = new List<byte>();
                        while (i < input.Length && input[i] == '&' && i + 1 < input.Length && input[i + 1] == '#')
                        {
                            int semi = input.IndexOf(';', i);
                            if (semi > i + 2)
                            {
                                string numStr = input.Substring(i + 2, semi - (i + 2));
                                int byteVal;
                                if (int.TryParse(numStr, out byteVal) && byteVal >= 0 && byteVal <= 255)
                                {
                                    bytes.Add((byte)byteVal);
                                    i = semi + 1;
                                    continue;
                                }
                            }
                            break;
                        }
                        if (bytes.Count > 0)
                        {
                            sb.Append(cp874.GetString(bytes.ToArray()));
                        }
                        else
                        {
                            sb.Append(input[i]);
                            i++;
                        }
                    }
                    else
                    {
                        sb.Append(input[i]);
                        i++;
                    }
                }
                input = sb.ToString();
            }

            // Step 2: Decode Latin-1 Mojibake where bytes 0xA0..0xFF were interpreted as Latin-1 code points
            // (e.g. "¡ÒÃ¨èÒÂ¹éÓÁÑ¹ã¹à¡ÕÂÃì" -> "การจ่ายน้ำมันในเกียร์")
            bool hasMojibake = false;
            for (int k = 0; k < input.Length; k++)
            {
                if (input[k] >= 0xA0 && input[k] <= 0xFF)
                {
                    hasMojibake = true;
                    break;
                }
            }

            if (hasMojibake)
            {
                StringBuilder sb2 = new StringBuilder();
                int j = 0;
                while (j < input.Length)
                {
                    if (input[j] >= 0xA0 && input[j] <= 0xFF)
                    {
                        List<byte> rawBytes = new List<byte>();
                        while (j < input.Length && input[j] >= 0xA0 && input[j] <= 0xFF)
                        {
                            rawBytes.Add((byte)input[j]);
                            j++;
                        }
                        sb2.Append(cp874.GetString(rawBytes.ToArray()));
                    }
                    else
                    {
                        sb2.Append(input[j]);
                        j++;
                    }
                }
                input = sb2.ToString();
            }

            return input;
        }

        public static XdfDefinition LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath)) return null;

            string xmlContent = "";
            try
            {
                // Try reading directly
                xmlContent = File.ReadAllText(filePath, Encoding.UTF8);
                if (!xmlContent.Contains("<XDFFORMAT") && !xmlContent.Contains("<XDFTABLE"))
                {
                    // Maybe encrypted with TunerPro
                    string op, mp, clean;
                    if (TunerProPasswordCleaner.DecryptAndClean(filePath, out op, out mp, out clean))
                    {
                        xmlContent = clean;
                    }
                }
            }
            catch
            {
                return null;
            }

            return ParseXdfXml(xmlContent);
        }

        private static string GetAttr(XmlNode node, string name, string fallback)
        {
            if (node == null || node.Attributes == null) return fallback;
            XmlAttribute a = node.Attributes[name];
            return (a != null && a.Value != null) ? a.Value : fallback;
        }

        private static string GetChildText(XmlNode node, string childName, string fallback)
        {
            if (node == null) return fallback;
            XmlNode c = node.SelectSingleNode(childName);
            return (c != null && c.InnerText != null) ? c.InnerText : fallback;
        }

        public static XdfDefinition ParseXdfXml(string xml)
        {
            XdfDefinition def = new XdfDefinition();
            if (string.IsNullOrEmpty(xml)) return def;

            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xml);

                // Categories
                Dictionary<int, string> catMap = new Dictionary<int, string>();
                XmlNodeList catNodes = doc.SelectNodes("//CATEGORY");
                if (catNodes != null)
                {
                    foreach (XmlNode node in catNodes)
                    {
                        int idx = 0;
                        string idxStr = GetAttr(node, "index", "");
                        if (!string.IsNullOrEmpty(idxStr))
                        {
                            idx = idxStr.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ?
                                  Convert.ToInt32(idxStr, 16) : int.Parse(idxStr);
                        }
                        string catName = DecodeThaiEntities(GetAttr(node, "name", ""));
                        catMap[idx] = catName;
                        if (!def.Categories.Contains(catName)) def.Categories.Add(catName);
                    }
                }

                // Table Nodes
                XmlNodeList tableNodes = doc.SelectNodes("//XDFTABLE");
                Dictionary<string, XdfTable> parsedTables = new Dictionary<string, XdfTable>();

                if (tableNodes != null)
                {
                    foreach (XmlNode tNode in tableNodes)
                    {
                        XdfTable tbl = new XdfTable();
                        tbl.UniqueId = GetAttr(tNode, "uniqueid", "");
                        tbl.Title = DecodeThaiEntities(GetChildText(tNode, "title", "Table"));
                        tbl.Description = DecodeThaiEntities(GetChildText(tNode, "description", ""));

                        // Category
                        XmlNode catMem = tNode.SelectSingleNode("CATEGORYMEM");
                        if (catMem != null && catMem.Attributes != null && catMem.Attributes["category"] != null)
                        {
                            int catIdx;
                            if (int.TryParse(catMem.Attributes["category"].Value, out catIdx) && catMap.ContainsKey(catIdx))
                            {
                                tbl.CategoryName = catMap[catIdx];
                            }
                        }

                        // Axes
                        XmlNodeList axes = tNode.SelectNodes("XDFAXIS");
                        if (axes != null)
                        {
                            foreach (XmlNode aNode in axes)
                            {
                                string id = GetAttr(aNode, "id", "").ToLower();
                                XdfAxis axis = (id == "x") ? tbl.AxisX : (id == "y") ? tbl.AxisY : tbl.AxisZ;
                                if (axis == null) continue;

                                axis.UniqueId = GetAttr(aNode, "uniqueid", "");
                                axis.Units = DecodeThaiEntities(GetChildText(aNode, "units", ""));

                                XmlNode idxCount = aNode.SelectSingleNode("indexcount");
                                int cnt;
                                if (idxCount != null && int.TryParse(idxCount.InnerText, out cnt))
                                {
                                    axis.IndexCount = cnt;
                                }

                                XmlNode embed = aNode.SelectSingleNode("EMBEDDEDDATA");
                                if (embed != null)
                                {
                                    string addrStr = GetAttr(embed, "mmedaddress", "");
                                    if (!string.IsNullOrEmpty(addrStr))
                                    {
                                        axis.Address = addrStr.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ?
                                                       Convert.ToInt32(addrStr, 16) : int.Parse(addrStr);
                                    }

                                    string sizeStr = GetAttr(embed, "mmedelementsizebits", "");
                                    int bits;
                                    if (!string.IsNullOrEmpty(sizeStr) && int.TryParse(sizeStr, out bits))
                                    {
                                        axis.ElementSizeBytes = Math.Max(1, bits / 8);
                                    }

                                    string rowsStr = GetAttr(embed, "mmedrowcount", "");
                                    string colsStr = GetAttr(embed, "mmedcolcount", "");
                                    int rCount, cCount;
                                    if (id == "z" && int.TryParse(rowsStr, out rCount) && int.TryParse(colsStr, out cCount))
                                    {
                                        tbl.AxisY.IndexCount = rCount;
                                        tbl.AxisX.IndexCount = cCount;
                                    }
                                }

                                XmlNode math = aNode.SelectSingleNode("MATH");
                                if (math != null && math.Attributes != null && math.Attributes["equation"] != null)
                                {
                                    axis.MathEquation = math.Attributes["equation"].Value;
                                }

                                XmlNode decPl = aNode.SelectSingleNode("decimalpl");
                                int dp;
                                if (decPl != null && int.TryParse(decPl.InnerText, out dp))
                                {
                                    tbl.DecimalPlaces = dp;
                                }

                                // Labels
                                XmlNodeList labelNodes = aNode.SelectNodes("LABEL");
                                if (labelNodes != null && labelNodes.Count > 0)
                                {
                                    axis.Labels = new string[labelNodes.Count];
                                    for (int l = 0; l < labelNodes.Count; l++)
                                    {
                                        axis.Labels[l] = GetAttr(labelNodes[l], "value", "");
                                    }
                                    axis.IndexCount = labelNodes.Count;
                                }

                                // Embedinfo link
                                XmlNode embedInfo = aNode.SelectSingleNode("embedinfo");
                                if (embedInfo != null && embedInfo.Attributes != null && embedInfo.Attributes["linkobjid"] != null)
                                {
                                    axis.LinkObjId = embedInfo.Attributes["linkobjid"].Value;
                                }
                            }
                        }

                        if (!parsedTables.ContainsKey(tbl.UniqueId))
                        {
                            parsedTables[tbl.UniqueId] = tbl;
                        }
                    }
                }

                // Resolve Axis Links (e.g. TPS Breakpoints or RPM Breakpoints)
                foreach (var kvp in parsedTables)
                {
                    XdfTable tbl = kvp.Value;
                    if (!string.IsNullOrEmpty(tbl.AxisX.LinkObjId) && parsedTables.ContainsKey(tbl.AxisX.LinkObjId))
                    {
                        XdfTable linked = parsedTables[tbl.AxisX.LinkObjId];
                        tbl.AxisX.Address = linked.AxisZ.Address;
                        tbl.AxisX.ElementSizeBytes = linked.AxisZ.ElementSizeBytes;
                        tbl.AxisX.MathEquation = linked.AxisZ.MathEquation;
                        tbl.AxisX.IndexCount = linked.AxisZ.IndexCount > 1 ? linked.AxisZ.IndexCount : linked.Rows;
                        tbl.AxisX.Labels = linked.AxisY.Labels;
                    }
                    if (!string.IsNullOrEmpty(tbl.AxisY.LinkObjId) && parsedTables.ContainsKey(tbl.AxisY.LinkObjId))
                    {
                        XdfTable linked = parsedTables[tbl.AxisY.LinkObjId];
                        tbl.AxisY.Address = linked.AxisZ.Address;
                        tbl.AxisY.ElementSizeBytes = linked.AxisZ.ElementSizeBytes;
                        tbl.AxisY.MathEquation = linked.AxisZ.MathEquation;
                        tbl.AxisY.IndexCount = linked.AxisZ.IndexCount > 1 ? linked.AxisZ.IndexCount : linked.Rows;
                        tbl.AxisY.Labels = linked.AxisY.Labels;
                    }

                    // Clean & Beautify category names
                    if (!string.IsNullOrEmpty(tbl.CategoryName))
                    {
                        string cLower = tbl.CategoryName.Trim().ToLowerInvariant();
                        if (cLower == "fuel" || cLower == "fuel tables" || cLower == "injection")
                            tbl.CategoryName = "⛽ น้ำมัน (Fuel)";
                        else if (cLower == "ignition" || cLower == "ignition tables" || cLower == "spark")
                            tbl.CategoryName = "🔥 องศาไฟ (Ignition)";
                        else if (cLower == "limiters" || cLower == "limits" || cLower == "limiter")
                            tbl.CategoryName = "⏱️ ลิมิต & ชดเชย (Limiters)";
                        else if (cLower == "breakpoints" || cLower == "tps" || cLower == "scales")
                            tbl.CategoryName = "📐 สเกลอ้างอิง (Breakpoints)";
                    }
                    else if (string.IsNullOrEmpty(tbl.CategoryName) || tbl.CategoryName == "General" || tbl.CategoryName == "1")
                    {
                        if (tbl.Title.Contains("Fuel") || tbl.Title.Contains("น้ำมัน") || tbl.Title.Contains("Injection")) tbl.CategoryName = "⛽ น้ำมัน (Fuel)";
                        else if (tbl.Title.Contains("Ignition") || tbl.Title.Contains("ไฟ")) tbl.CategoryName = "🔥 องศาไฟ (Ignition)";
                        else if (tbl.Title.Contains("Limit") || tbl.Title.Contains("ตัดรอบ") || tbl.Title.Contains("รอบ")) tbl.CategoryName = "⏱️ ลิมิตรอบ (Rev Limit)";
                        else tbl.CategoryName = "📊 ตารางอื่นๆ (General)";
                    }

                    def.Tables.Add(tbl);
                }

                // Constants
                XmlNodeList constNodes = doc.SelectNodes("//XDFCONSTANT");
                if (constNodes != null)
                {
                    foreach (XmlNode cNode in constNodes)
                    {
                        XdfConstant c = new XdfConstant();
                        c.UniqueId = GetAttr(cNode, "uniqueid", "");
                        c.Title = DecodeThaiEntities(GetChildText(cNode, "title", "Scalar"));
                        c.CategoryName = "สวิตช์ / ลิมิต";

                        XmlNode embed = cNode.SelectSingleNode("EMBEDDEDDATA");
                        if (embed != null)
                        {
                            string addrStr = GetAttr(embed, "mmedaddress", "");
                            if (!string.IsNullOrEmpty(addrStr))
                            {
                                c.Address = addrStr.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ?
                                            Convert.ToInt32(addrStr, 16) : int.Parse(addrStr);
                            }
                            string sizeStr = GetAttr(embed, "mmedelementsizebits", "");
                            int bits;
                            if (!string.IsNullOrEmpty(sizeStr) && int.TryParse(sizeStr, out bits))
                            {
                                c.ElementSizeBytes = Math.Max(1, bits / 8);
                            }
                        }

                        XmlNode math = cNode.SelectSingleNode("MATH");
                        if (math != null && math.Attributes != null && math.Attributes["equation"] != null)
                        {
                            c.MathEquation = math.Attributes["equation"].Value;
                        }
                        def.Constants.Add(c);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[XDF] Parse error: " + ex.Message);
            }

            return def;
        }

        #region Built-in Factory Presets for Honda Models

        public static XdfDefinition GetBuiltinDefinition(string modelName)
        {
            XdfDefinition def = new XdfDefinition();
            def.Title = modelName;

            // Generate standard RPM breakpoints (500 - 12000 RPM)
            string[] rpmLabels = new string[]
            {
                "500", "800", "1000", "1200", "1400", "1600", "1800", "2000", "2200", "2400",
                "2600", "2800", "3000", "3500", "4000", "4500", "5000", "5500", "6000", "6500",
                "7000", "7500", "8000", "8500", "9000", "9500", "10000", "10500", "11000"
            };

            // Generate standard TPS % breakpoints (0% - 100%)
            string[] tpsLabels = new string[]
            {
                "0.0%", "0.5%", "1.0%", "1.5%", "2.0%", "2.8%", "3.5%", "4.5%", "5.5%", "7.0%",
                "8.5%", "10.0%", "12.5%", "15.0%", "18.0%", "22.0%", "26.0%", "30.0%", "35.0%", "40.0%",
                "45.0%", "50.0%", "58.0%", "65.0%", "72.0%", "80.0%", "88.0%", "95.0%", "100.0%"
            };

            // 1. Fuel In-Gear (น้ำมัน 1-4)
            XdfTable fuelInGear = new XdfTable
            {
                UniqueId = "FUEL_INGEAR",
                Title = "น้ำมัน 1-4 (Fuel In-Gear 29x29)",
                Description = "ตารางหลักการจ่ายน้ำมันขณะเข้าเกียร์ (ความกว้างพัลส์หัวฉีดหน่วย ms)",
                CategoryName = "⛽ น้ำมัน (Fuel)",
                DecimalPlaces = 3,
                MinValue = 0.5f,
                MaxValue = 15.0f
            };
            fuelInGear.AxisX = new XdfAxis("x") { Units = "TPS %", IndexCount = 29, Labels = tpsLabels };
            fuelInGear.AxisY = new XdfAxis("y") { Units = "RPM", IndexCount = 29, Labels = rpmLabels };
            fuelInGear.AxisZ = new XdfAxis("z") { Units = "ms", ElementSizeBytes = 2, Address = 0x84, MathEquation = "X/1000" };
            def.Tables.Add(fuelInGear);

            // 2. Fuel Fast-Idle (น้ำมันเกียร์ว่าง N)
            XdfTable fuelNeutral = new XdfTable
            {
                UniqueId = "FUEL_NEUTRAL",
                Title = "น้ำมัน N (Fuel Fast-Idle 29x29)",
                Description = "ตารางการจ่ายน้ำมันขณะเกียร์ว่างหรือรอบเดินเบา Fast-Idle",
                CategoryName = "⛽ น้ำมัน (Fuel)",
                DecimalPlaces = 3,
                MinValue = 0.5f,
                MaxValue = 15.0f
            };
            fuelNeutral.AxisX = new XdfAxis("x") { Units = "TPS %", IndexCount = 29, Labels = tpsLabels };
            fuelNeutral.AxisY = new XdfAxis("y") { Units = "RPM", IndexCount = 29, Labels = rpmLabels };
            fuelNeutral.AxisZ = new XdfAxis("z") { Units = "ms", ElementSizeBytes = 2, Address = 0x716, MathEquation = "X/1000" };
            def.Tables.Add(fuelNeutral);

            // 3. Ignition In-Gear (ไฟ 1-4)
            XdfTable ignInGear = new XdfTable
            {
                UniqueId = "IGN_INGEAR",
                Title = "ไฟ 1-4 (Ignition In-Gear 29x29)",
                Description = "องศาไฟจุดระเบิดในเกียร์ 1 ถึง 4 (°BTDC)",
                CategoryName = "🔥 องศาไฟ (Ignition)",
                DecimalPlaces = 2,
                MinValue = -15f,
                MaxValue = 60f
            };
            ignInGear.AxisX = new XdfAxis("x") { Units = "TPS %", IndexCount = 29, Labels = tpsLabels };
            ignInGear.AxisY = new XdfAxis("y") { Units = "RPM", IndexCount = 29, Labels = rpmLabels };
            ignInGear.AxisZ = new XdfAxis("z") { Units = "°BTDC", ElementSizeBytes = 1, Address = 0xDA8, MathEquation = "X*0.3572-15.72" };
            def.Tables.Add(ignInGear);

            // 4. Ignition Neutral (ไฟเกียร์ว่าง N)
            XdfTable ignNeutral = new XdfTable
            {
                UniqueId = "IGN_NEUTRAL",
                Title = "ไฟ N (Ignition Neutral 29x29)",
                Description = "องศาไฟจุดระเบิดขณะเกียร์ว่าง (°BTDC)",
                CategoryName = "🔥 องศาไฟ (Ignition)",
                DecimalPlaces = 2,
                MinValue = -15f,
                MaxValue = 60f
            };
            ignNeutral.AxisX = new XdfAxis("x") { Units = "TPS %", IndexCount = 29, Labels = tpsLabels };
            ignNeutral.AxisY = new XdfAxis("y") { Units = "RPM", IndexCount = 29, Labels = rpmLabels };
            ignNeutral.AxisZ = new XdfAxis("z") { Units = "°BTDC", ElementSizeBytes = 1, Address = 0x10F1, MathEquation = "X*0.3572-15.72" };
            def.Tables.Add(ignNeutral);

            // 5. Injection Timing Degree
            XdfTable injDegree = new XdfTable
            {
                UniqueId = "INJ_DEGREE",
                Title = "องศาการฉีดน้ำมัน (Injection Degree)",
                Description = "องศาจังหวะการฉีดน้ำมันเทียบกับเพลาข้อเหวี่ยง",
                CategoryName = "⛽ น้ำมัน (Fuel)",
                DecimalPlaces = 1,
                MinValue = 0f,
                MaxValue = 720f
            };
            injDegree.AxisX = new XdfAxis("x") { Units = "TPS %", IndexCount = 29, Labels = tpsLabels };
            injDegree.AxisY = new XdfAxis("y") { Units = "RPM", IndexCount = 29, Labels = rpmLabels };
            injDegree.AxisZ = new XdfAxis("z") { Units = "°", ElementSizeBytes = 1, Address = 0x143A, MathEquation = "X*2.8125" };
            def.Tables.Add(injDegree);

            // 6. Constants (Scalars)
            def.Constants.Add(new XdfConstant
            {
                UniqueId = "REV_LIMIT_FUEL",
                Title = "ลิมิตรอบ ตัดน้ำมัน (Fuel Rev Limiter)",
                CategoryName = "⏱️ ลิมิต & ชดเชย",
                Address = 0x17A0,
                ElementSizeBytes = 2,
                MathEquation = "X/3.64086",
                Units = "RPM"
            });
            def.Constants.Add(new XdfConstant
            {
                UniqueId = "REV_LIMIT_IGN",
                Title = "ลิมิตรอบ ตัดไฟ (Spark Rev Limiter)",
                CategoryName = "⏱️ ลิมิต & ชดเชย",
                Address = 0x17A2,
                ElementSizeBytes = 2,
                MathEquation = "X/3.64086",
                Units = "RPM"
            });
            def.Constants.Add(new XdfConstant
            {
                UniqueId = "TARGET_IDLE_RPM",
                Title = "รอบเดินเบาเป้าหมาย (Target Idle RPM)",
                CategoryName = "⏱️ ลิมิต & ชดเชย",
                Address = 0x17A6,
                ElementSizeBytes = 2,
                MathEquation = "X/3.64086",
                Units = "RPM"
            });

            return def;
        }

        #endregion
    }

    #endregion

    #region Map Tuning Session & Undo Management

    public class MapTuningSession
    {
        public byte[] ActiveBin { get; private set; }
        public byte[] OriginalBin { get; private set; }
        public XdfDefinition Definition { get; private set; }
        public XdfTable ActiveTable { get; private set; }
        public string BinFilePath { get; set; }
        public string XdfFilePath { get; set; }

        private Stack<byte[]> undoStack = new Stack<byte[]>();
        private Stack<byte[]> redoStack = new Stack<byte[]>();
        private const int MaxUndo = 30;

        public Rectangle SelectedRange { get; set; } // Row, Col selection

        public MapTuningSession()
        {
            SelectedRange = new Rectangle(0, 0, 1, 1);
        }

        public void LoadBin(byte[] binData, string filePath = "")
        {
            if (binData == null) return;
            OriginalBin = (byte[])binData.Clone();
            ActiveBin = (byte[])binData.Clone();
            BinFilePath = filePath;
            undoStack.Clear();
            redoStack.Clear();
        }

        public void LoadDefinition(XdfDefinition def, string xdfPath = "")
        {
            Definition = def;
            XdfFilePath = xdfPath;
            if (def != null && def.Tables.Count > 0)
            {
                ActiveTable = def.Tables[0];
                SelectedRange = new Rectangle(0, 0, 1, 1);
            }
        }

        public void SelectTable(XdfTable tbl)
        {
            if (tbl == null) return;
            ActiveTable = tbl;
            SelectedRange = new Rectangle(0, 0, Math.Min(1, tbl.Cols), Math.Min(1, tbl.Rows));
        }

        private void PushUndo()
        {
            if (ActiveBin == null) return;
            undoStack.Push((byte[])ActiveBin.Clone());
            if (undoStack.Count > MaxUndo)
            {
                // Remove oldest
                var arr = undoStack.ToArray();
                undoStack.Clear();
                for (int i = arr.Length - 2; i >= 0; i--) undoStack.Push(arr[i]);
            }
            redoStack.Clear();
        }

        public bool CanUndo
        {
            get { return undoStack.Count > 0; }
        }

        public bool CanRedo
        {
            get { return redoStack.Count > 0; }
        }

        public bool Undo()
        {
            if (!CanUndo) return false;
            redoStack.Push((byte[])ActiveBin.Clone());
            ActiveBin = undoStack.Pop();
            return true;
        }

        public bool Redo()
        {
            if (!CanRedo) return false;
            undoStack.Push((byte[])ActiveBin.Clone());
            ActiveBin = redoStack.Pop();
            return true;
        }

        public void AddPercent(float percent)
        {
            if (ActiveTable == null || ActiveBin == null) return;
            PushUndo();
            float factor = 1.0f + (percent / 100.0f);

            int startR = Math.Max(0, SelectedRange.Y);
            int endR = Math.Min(ActiveTable.Rows - 1, SelectedRange.Bottom - 1);
            int startC = Math.Max(0, SelectedRange.X);
            int endC = Math.Min(ActiveTable.Cols - 1, SelectedRange.Right - 1);

            for (int r = startR; r <= endR; r++)
            {
                for (int c = startC; c <= endC; c++)
                {
                    float current = ActiveTable.GetDisplayValue(ActiveBin, r, c);
                    ActiveTable.SetDisplayValue(ActiveBin, r, c, current * factor);
                }
            }
        }

        public void AddOffset(float offset)
        {
            if (ActiveTable == null || ActiveBin == null) return;
            PushUndo();

            int startR = Math.Max(0, SelectedRange.Y);
            int endR = Math.Min(ActiveTable.Rows - 1, SelectedRange.Bottom - 1);
            int startC = Math.Max(0, SelectedRange.X);
            int endC = Math.Min(ActiveTable.Cols - 1, SelectedRange.Right - 1);

            for (int r = startR; r <= endR; r++)
            {
                for (int c = startC; c <= endC; c++)
                {
                    float current = ActiveTable.GetDisplayValue(ActiveBin, r, c);
                    ActiveTable.SetDisplayValue(ActiveBin, r, c, current + offset);
                }
            }
        }

        public void Scale(float factor)
        {
            if (ActiveTable == null || ActiveBin == null) return;
            PushUndo();

            int startR = Math.Max(0, SelectedRange.Y);
            int endR = Math.Min(ActiveTable.Rows - 1, SelectedRange.Bottom - 1);
            int startC = Math.Max(0, SelectedRange.X);
            int endC = Math.Min(ActiveTable.Cols - 1, SelectedRange.Right - 1);

            for (int r = startR; r <= endR; r++)
            {
                for (int c = startC; c <= endC; c++)
                {
                    float current = ActiveTable.GetDisplayValue(ActiveBin, r, c);
                    ActiveTable.SetDisplayValue(ActiveBin, r, c, current * factor);
                }
            }
        }

        public void SetAbsoluteValue(float val)
        {
            if (ActiveTable == null || ActiveBin == null) return;
            PushUndo();

            int startR = Math.Max(0, SelectedRange.Y);
            int endR = Math.Min(ActiveTable.Rows - 1, SelectedRange.Bottom - 1);
            int startC = Math.Max(0, SelectedRange.X);
            int endC = Math.Min(ActiveTable.Cols - 1, SelectedRange.Right - 1);

            for (int r = startR; r <= endR; r++)
            {
                for (int c = startC; c <= endC; c++)
                {
                    ActiveTable.SetDisplayValue(ActiveBin, r, c, val);
                }
            }
        }

        public void Smooth(float weight = 0.5f)
        {
            if (ActiveTable == null || ActiveBin == null) return;
            PushUndo();

            int startR = Math.Max(0, SelectedRange.Y);
            int endR = Math.Min(ActiveTable.Rows - 1, SelectedRange.Bottom - 1);
            int startC = Math.Max(0, SelectedRange.X);
            int endC = Math.Min(ActiveTable.Cols - 1, SelectedRange.Right - 1);

            float[,] buffer = new float[ActiveTable.Rows, ActiveTable.Cols];
            for (int r = 0; r < ActiveTable.Rows; r++)
                for (int c = 0; c < ActiveTable.Cols; c++)
                    buffer[r, c] = ActiveTable.GetDisplayValue(ActiveBin, r, c);

            for (int r = startR; r <= endR; r++)
            {
                for (int c = startC; c <= endC; c++)
                {
                    float sum = 0f;
                    int count = 0;
                    for (int dr = -1; dr <= 1; dr++)
                    {
                        for (int dc = -1; dc <= 1; dc++)
                        {
                            int nr = r + dr;
                            int nc = c + dc;
                            if (nr >= 0 && nr < ActiveTable.Rows && nc >= 0 && nc < ActiveTable.Cols)
                            {
                                sum += buffer[nr, nc];
                                count++;
                            }
                        }
                    }
                    float avg = count > 0 ? sum / count : buffer[r, c];
                    float smoothed = (buffer[r, c] * (1f - weight)) + (avg * weight);
                    ActiveTable.SetDisplayValue(ActiveBin, r, c, smoothed);
                }
            }
        }

        public void InterpolateHorizontal()
        {
            if (ActiveTable == null || ActiveBin == null) return;
            PushUndo();

            int startR = Math.Max(0, SelectedRange.Y);
            int endR = Math.Min(ActiveTable.Rows - 1, SelectedRange.Bottom - 1);
            int startC = Math.Max(0, SelectedRange.X);
            int endC = Math.Min(ActiveTable.Cols - 1, SelectedRange.Right - 1);
            if (endC <= startC) return;

            for (int r = startR; r <= endR; r++)
            {
                float vStart = ActiveTable.GetDisplayValue(ActiveBin, r, startC);
                float vEnd = ActiveTable.GetDisplayValue(ActiveBin, r, endC);
                for (int c = startC; c <= endC; c++)
                {
                    float t = (float)(c - startC) / (float)(endC - startC);
                    float interpolated = vStart + ((vEnd - vStart) * t);
                    ActiveTable.SetDisplayValue(ActiveBin, r, c, interpolated);
                }
            }
        }

        public void InterpolateVertical()
        {
            if (ActiveTable == null || ActiveBin == null) return;
            PushUndo();

            int startR = Math.Max(0, SelectedRange.Y);
            int endR = Math.Min(ActiveTable.Rows - 1, SelectedRange.Bottom - 1);
            int startC = Math.Max(0, SelectedRange.X);
            int endC = Math.Min(ActiveTable.Cols - 1, SelectedRange.Right - 1);
            if (endR <= startR) return;

            for (int c = startC; c <= endC; c++)
            {
                float vStart = ActiveTable.GetDisplayValue(ActiveBin, startR, c);
                float vEnd = ActiveTable.GetDisplayValue(ActiveBin, endR, c);
                for (int r = startR; r <= endR; r++)
                {
                    float t = (float)(r - startR) / (float)(endR - startR);
                    float interpolated = vStart + ((vEnd - vStart) * t);
                    ActiveTable.SetDisplayValue(ActiveBin, r, c, interpolated);
                }
            }
        }

        public bool SaveTunedBin(string savePath, out int checksumOffset)
        {
            checksumOffset = -1;
            if (ActiveBin == null || string.IsNullOrEmpty(savePath)) return false;

            try
            {
                byte[] toWrite = (byte[])ActiveBin.Clone();

                // Auto-Fix Checksum based on file length
                if (toWrite.Length == 0x10000) // Keihin 64KB
                {
                    ChecksumEngine.FixKeihinChecksum(toWrite, 0x7600);
                    checksumOffset = 0x7600;
                }
                else if (toWrite.Length == 0x20000) // Keihin 128KB
                {
                    ChecksumEngine.FixKeihinChecksum(toWrite, 0xDFEF);
                    checksumOffset = 0xDFEF;
                }
                else if (toWrite.Length == 0x40000) // Keihin 256KB
                {
                    ChecksumEngine.FixKeihinChecksum(toWrite, 0x3FFF8);
                    checksumOffset = 0x3FFF8;
                }
                else if (toWrite.Length == 0xC000 || toWrite.Length == 0xE000) // Shindengen 48KB / 56KB
                {
                    int shCk;
                    ChecksumEngine.FixShindengenChecksum(toWrite, out shCk);
                    checksumOffset = shCk;
                }

                File.WriteAllBytes(savePath, toWrite);
                BinFilePath = savePath;
                ActiveBin = toWrite;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    #endregion
}
