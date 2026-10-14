using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Collections.Generic;

namespace RemixHondaFlash
{
    #region 2D Heatmap Table Grid Control

    public class TableGridControl : Control
    {
        private MapTuningSession session;
        private HScrollBar hScroll;
        private VScrollBar vScroll;

        private const int HeaderRowHeight = 28;
        private const int HeaderColWidth = 65;
        private const int CellWidth = 58;
        private const int CellHeight = 24;

        private bool isSelecting = false;
        private Point selectStartCell = Point.Empty;
        private Point currentHoverCell = Point.Empty;

        // Live engine cursor
        private int liveRow = -1;
        private int liveCol = -1;
        private Timer livePulseTimer;
        private float pulsePhase = 0f;

        // Diff mode
        public bool ShowDiffMode { get; set; }

        public event EventHandler SelectionChanged;

        public TableGridControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.Selectable | ControlStyles.ResizeRedraw, true);

            this.BackColor = Color.FromArgb(16, 20, 29);
            this.Font = new Font("Segoe UI", 9f, FontStyle.Regular);

            hScroll = new HScrollBar { Dock = DockStyle.Bottom, Visible = false };
            vScroll = new VScrollBar { Dock = DockStyle.Right, Visible = false };

            hScroll.Scroll += (s, e) => Invalidate();
            vScroll.Scroll += (s, e) => Invalidate();

            this.Controls.Add(hScroll);
            this.Controls.Add(vScroll);

            livePulseTimer = new Timer { Interval = 50 };
            livePulseTimer.Tick += (s, e) =>
            {
                if (liveRow >= 0 && liveCol >= 0)
                {
                    pulsePhase += 0.15f;
                    if (pulsePhase > (float)Math.PI * 2f) pulsePhase = 0f;
                    Invalidate();
                }
            };
            livePulseTimer.Start();
        }

        public void BindSession(MapTuningSession sess)
        {
            session = sess;
            UpdateScrollbars();
            Invalidate();
        }

        public void SetLiveEnginePosition(float currentRpm, float currentTps)
        {
            if (session == null || session.ActiveTable == null || session.ActiveBin == null) return;
            XdfTable tbl = session.ActiveTable;

            // Find closest row (RPM)
            int bestR = -1;
            float bestRpmDiff = float.MaxValue;
            for (int r = 0; r < tbl.Rows; r++)
            {
                string rLbl = tbl.GetRowLabel(session.ActiveBin, r);
                float rVal;
                if (float.TryParse(rLbl, out rVal))
                {
                    float diff = Math.Abs(rVal - currentRpm);
                    if (diff < bestRpmDiff)
                    {
                        bestRpmDiff = diff;
                        bestR = r;
                    }
                }
            }

            // Find closest col (TPS)
            int bestC = -1;
            float bestTpsDiff = float.MaxValue;
            for (int c = 0; c < tbl.Cols; c++)
            {
                string cLbl = tbl.GetColLabel(session.ActiveBin, c).Replace("%", "");
                float cVal;
                if (float.TryParse(cLbl, out cVal))
                {
                    float diff = Math.Abs(cVal - currentTps);
                    if (diff < bestTpsDiff)
                    {
                        bestTpsDiff = diff;
                        bestC = c;
                    }
                }
            }

            if (bestR != liveRow || bestC != liveCol)
            {
                liveRow = bestR;
                liveCol = bestC;
                Invalidate();
            }
        }

        public void ClearLivePosition()
        {
            liveRow = -1;
            liveCol = -1;
            Invalidate();
        }

        public void UpdateScrollbars()
        {
            if (session == null || session.ActiveTable == null)
            {
                hScroll.Visible = false;
                vScroll.Visible = false;
                return;
            }

            int totalW = HeaderColWidth + (session.ActiveTable.Cols * CellWidth);
            int totalH = HeaderRowHeight + (session.ActiveTable.Rows * CellHeight);

            int viewW = ClientSize.Width - (vScroll.Visible ? vScroll.Width : 0);
            int viewH = ClientSize.Height - (hScroll.Visible ? hScroll.Height : 0);

            if (totalW > viewW)
            {
                hScroll.Visible = true;
                hScroll.Maximum = Math.Max(0, totalW - viewW + 50);
            }
            else
            {
                hScroll.Visible = false;
                hScroll.Value = 0;
            }

            if (totalH > viewH)
            {
                vScroll.Visible = true;
                vScroll.Maximum = Math.Max(0, totalH - viewH + 50);
            }
            else
            {
                vScroll.Visible = false;
                vScroll.Value = 0;
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateScrollbars();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (session == null || session.ActiveTable == null || e.Button != MouseButtons.Left) return;

            Point cell = HitTestCell(e.X, e.Y);
            if (cell.X >= 0 && cell.Y >= 0)
            {
                isSelecting = true;
                selectStartCell = cell;
                session.SelectedRange = new Rectangle(cell.X, cell.Y, 1, 1);
                Invalidate();
                if (SelectionChanged != null) SelectionChanged(this, EventArgs.Empty);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (session == null || session.ActiveTable == null) return;

            if (isSelecting)
            {
                Point cell = HitTestCell(e.X, e.Y);
                if (cell.X >= 0 && cell.Y >= 0)
                {
                    int minX = Math.Min(selectStartCell.X, cell.X);
                    int maxX = Math.Max(selectStartCell.X, cell.X);
                    int minY = Math.Min(selectStartCell.Y, cell.Y);
                    int maxY = Math.Max(selectStartCell.Y, cell.Y);

                    session.SelectedRange = new Rectangle(minX, minY, (maxX - minX) + 1, (maxY - minY) + 1);
                    Invalidate();
                    if (SelectionChanged != null) SelectionChanged(this, EventArgs.Empty);
                }
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            isSelecting = false;
        }

        private Point HitTestCell(int x, int y)
        {
            if (session == null || session.ActiveTable == null) return new Point(-1, -1);
            int scrollX = hScroll.Visible ? hScroll.Value : 0;
            int scrollY = vScroll.Visible ? vScroll.Value : 0;

            int adjX = x - HeaderColWidth + scrollX;
            int adjY = y - HeaderRowHeight + scrollY;

            if (adjX < 0 || adjY < 0) return new Point(-1, -1);

            int col = adjX / CellWidth;
            int row = adjY / CellHeight;

            if (row >= 0 && row < session.ActiveTable.Rows && col >= 0 && col < session.ActiveTable.Cols)
            {
                return new Point(col, row);
            }
            return new Point(-1, -1);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.Clear(this.BackColor);

            if (session == null || session.ActiveTable == null || session.ActiveBin == null)
            {
                using (var brush = new SolidBrush(Color.FromArgb(120, 140, 170)))
                {
                    string msg = "กรุณาเปิดไฟล์ BIN หรือเลือกโมเดลรถ เพื่อแสดงตารางจูน";
                    var sz = g.MeasureString(msg, this.Font);
                    g.DrawString(msg, this.Font, brush, (Width - sz.Width) / 2, (Height - sz.Height) / 2);
                }
                return;
            }

            XdfTable tbl = session.ActiveTable;
            byte[] bin = session.ActiveBin;
            byte[] origBin = session.OriginalBin;

            int scrollX = hScroll.Visible ? hScroll.Value : 0;
            int scrollY = vScroll.Visible ? vScroll.Value : 0;

            // Find min/max for heatmap
            float minVal = float.MaxValue;
            float maxVal = float.MinValue;
            for (int r = 0; r < tbl.Rows; r++)
            {
                for (int c = 0; c < tbl.Cols; c++)
                {
                    float v = tbl.GetDisplayValue(bin, r, c);
                    if (v < minVal) minVal = v;
                    if (v > maxVal) maxVal = v;
                }
            }
            if (Math.Abs(maxVal - minVal) < 0.0001f) maxVal = minVal + 1f;

            // 1. Draw Data Cells
            using (var cellFont = new Font("Consolas", 8.5f, FontStyle.Regular))
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            using (var borderPen = new Pen(Color.FromArgb(30, 36, 48), 1))
            using (var selPen = new Pen(Color.FromArgb(0, 229, 255), 2))
            using (var selBrush = new SolidBrush(Color.FromArgb(50, 0, 229, 255)))
            {
                for (int r = 0; r < tbl.Rows; r++)
                {
                    int cellY = HeaderRowHeight + (r * CellHeight) - scrollY;
                    if (cellY + CellHeight < HeaderRowHeight || cellY > Height) continue;

                    for (int c = 0; c < tbl.Cols; c++)
                    {
                        int cellX = HeaderColWidth + (c * CellWidth) - scrollX;
                        if (cellX + CellWidth < HeaderColWidth || cellX > Width) continue;

                        Rectangle cellRect = new Rectangle(cellX, cellY, CellWidth, CellHeight);
                        float val = tbl.GetDisplayValue(bin, r, c);

                        // Heatmap color
                        Color cellBg;
                        Color textClr;

                        if (ShowDiffMode && origBin != null)
                        {
                            float origVal = tbl.GetDisplayValue(origBin, r, c);
                            float diff = val - origVal;
                            if (Math.Abs(diff) > 0.001f)
                            {
                                cellBg = diff > 0 ? Color.FromArgb(20, 80, 40) : Color.FromArgb(90, 25, 25);
                                textClr = diff > 0 ? Color.FromArgb(0, 255, 128) : Color.FromArgb(255, 100, 100);
                            }
                            else
                            {
                                cellBg = Color.FromArgb(24, 28, 38);
                                textClr = Color.FromArgb(120, 130, 150);
                            }
                        }
                        else
                        {
                            float norm = (val - minVal) / (maxVal - minVal);
                            cellBg = GetHeatmapColor(norm);
                            textClr = (norm > 0.65f) ? Color.Black : Color.White;
                        }

                        using (var bgBrush = new SolidBrush(cellBg))
                        {
                            g.FillRectangle(bgBrush, cellRect);
                        }
                        g.DrawRectangle(borderPen, cellRect);

                        // Draw value string
                        string valStr = ShowDiffMode && origBin != null ?
                            (val - tbl.GetDisplayValue(origBin, r, c)).ToString("+#0.000;-#0.000;0.000") :
                            val.ToString("F" + tbl.DecimalPlaces);

                        using (var txtBrush = new SolidBrush(textClr))
                        {
                            g.DrawString(valStr, cellFont, txtBrush, cellRect, sf);
                        }

                        // Selected cell highlight
                        if (session.SelectedRange.Contains(c, r))
                        {
                            g.FillRectangle(selBrush, cellRect);
                            g.DrawRectangle(selPen, cellRect);
                        }

                        // Live Telemetry engine cursor (pulsing neon green)
                        if (r == liveRow && c == liveCol)
                        {
                            int pulseAlpha = (int)(180 + 75 * Math.Sin(pulsePhase));
                            using (var livePen = new Pen(Color.FromArgb(pulseAlpha, 0, 255, 120), 2.5f))
                            using (var liveBrush = new SolidBrush(Color.FromArgb(40, 0, 255, 120)))
                            {
                                Rectangle lRect = new Rectangle(cellRect.X + 1, cellRect.Y + 1, cellRect.Width - 2, cellRect.Height - 2);
                                g.FillRectangle(liveBrush, lRect);
                                g.DrawRectangle(livePen, lRect);
                            }
                        }
                    }
                }
            }

            // 2. Draw Sticky Header Row (Columns = TPS / Load)
            using (var headBgBrush = new SolidBrush(Color.FromArgb(22, 28, 40)))
            using (var headTextBrush = new SolidBrush(Color.FromArgb(0, 229, 255)))
            using (var headBorderPen = new Pen(Color.FromArgb(45, 55, 75), 1))
            using (var headFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                g.FillRectangle(headBgBrush, 0, 0, Width, HeaderRowHeight);

                for (int c = 0; c < tbl.Cols; c++)
                {
                    int cellX = HeaderColWidth + (c * CellWidth) - scrollX;
                    if (cellX + CellWidth < HeaderColWidth || cellX > Width) continue;

                    Rectangle hRect = new Rectangle(cellX, 0, CellWidth, HeaderRowHeight);
                    string label = tbl.GetColLabel(bin, c);
                    g.DrawString(label, headFont, headTextBrush, hRect, sf);
                    g.DrawRectangle(headBorderPen, hRect);
                }
            }

            // 3. Draw Sticky Header Column (Rows = RPM)
            using (var headBgBrush = new SolidBrush(Color.FromArgb(22, 28, 40)))
            using (var headTextBrush = new SolidBrush(Color.FromArgb(255, 215, 64)))
            using (var headBorderPen = new Pen(Color.FromArgb(45, 55, 75), 1))
            using (var headFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                g.FillRectangle(headBgBrush, 0, HeaderRowHeight, HeaderColWidth, Height - HeaderRowHeight);

                for (int r = 0; r < tbl.Rows; r++)
                {
                    int cellY = HeaderRowHeight + (r * CellHeight) - scrollY;
                    if (cellY + CellHeight < HeaderRowHeight || cellY > Height) continue;

                    Rectangle hRect = new Rectangle(0, cellY, HeaderColWidth, CellHeight);
                    string label = tbl.GetRowLabel(bin, r);
                    g.DrawString(label, headFont, headTextBrush, hRect, sf);
                    g.DrawRectangle(headBorderPen, hRect);
                }
            }

            // 4. Draw Top-Left Corner Box
            using (var cornerBrush = new SolidBrush(Color.FromArgb(16, 22, 32)))
            using (var cornerPen = new Pen(Color.FromArgb(60, 75, 100), 1))
            using (var labelBrush = new SolidBrush(Color.FromArgb(160, 180, 210)))
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            using (var cornerFont = new Font("Segoe UI", 7.5f, FontStyle.Bold))
            {
                Rectangle corner = new Rectangle(0, 0, HeaderColWidth, HeaderRowHeight);
                g.FillRectangle(cornerBrush, corner);
                g.DrawRectangle(cornerPen, corner);
                g.DrawString("RPM \\ TPS", cornerFont, labelBrush, corner, sf);
            }
        }

        private Color GetHeatmapColor(float t)
        {
            t = Math.Max(0f, Math.Min(1f, t));
            // 5-stop gradient: Deep Blue -> Cyan -> Green -> Yellow -> Red
            if (t < 0.25f)
            {
                float local = t / 0.25f;
                return BlendColor(Color.FromArgb(16, 35, 100), Color.FromArgb(0, 160, 220), local);
            }
            else if (t < 0.50f)
            {
                float local = (t - 0.25f) / 0.25f;
                return BlendColor(Color.FromArgb(0, 160, 220), Color.FromArgb(0, 200, 100), local);
            }
            else if (t < 0.75f)
            {
                float local = (t - 0.50f) / 0.25f;
                return BlendColor(Color.FromArgb(0, 200, 100), Color.FromArgb(235, 210, 20), local);
            }
            else
            {
                float local = (t - 0.75f) / 0.25f;
                return BlendColor(Color.FromArgb(235, 210, 20), Color.FromArgb(225, 40, 40), local);
            }
        }

        private Color BlendColor(Color c1, Color c2, float t)
        {
            int r = (int)(c1.R + (c2.R - c1.R) * t);
            int g = (int)(c1.G + (c2.G - c1.G) * t);
            int b = (int)(c1.B + (c2.B - c1.B) * t);
            return Color.FromArgb(Math.Max(0, Math.Min(255, r)), Math.Max(0, Math.Min(255, g)), Math.Max(0, Math.Min(255, b)));
        }
    }

    #endregion

    #region 3D Perspective Surface Mesh Graph Control

    public class Surface3DGraphControl : Control
    {
        private MapTuningSession session;

        // Viewing angles
        private float azimuth = 45f;      // Rotation around Z axis (0 - 360 deg)
        private float elevation = 32f;    // Tilt above ground (10 - 80 deg)
        private float zoomScale = 1.0f;   // Zoom multiplier

        private Point mouseLastPos;
        private bool isRotating = false;

        // Live engine beacon in 3D
        private int liveRow = -1;
        private int liveCol = -1;
        private Timer pulseTimer;
        private float pulseScale = 1.0f;
        private bool pulseDir = true;

        public Surface3DGraphControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.Selectable | ControlStyles.ResizeRedraw, true);

            this.BackColor = Color.FromArgb(12, 16, 24);
            this.Font = new Font("Segoe UI", 9f, FontStyle.Regular);

            pulseTimer = new Timer { Interval = 40 };
            pulseTimer.Tick += (s, e) =>
            {
                if (liveRow >= 0 && liveCol >= 0)
                {
                    pulseScale += pulseDir ? 0.08f : -0.08f;
                    if (pulseScale > 1.6f) pulseDir = false;
                    else if (pulseScale < 0.8f) pulseDir = true;
                    Invalidate();
                }
            };
            pulseTimer.Start();
        }

        public void BindSession(MapTuningSession sess)
        {
            session = sess;
            Invalidate();
        }

        public void SetLiveEnginePosition(float currentRpm, float currentTps)
        {
            if (session == null || session.ActiveTable == null || session.ActiveBin == null) return;
            XdfTable tbl = session.ActiveTable;

            // Find closest row
            int bestR = -1;
            float bestRpmDiff = float.MaxValue;
            for (int r = 0; r < tbl.Rows; r++)
            {
                string rLbl = tbl.GetRowLabel(session.ActiveBin, r);
                float rVal;
                if (float.TryParse(rLbl, out rVal))
                {
                    float diff = Math.Abs(rVal - currentRpm);
                    if (diff < bestRpmDiff) { bestRpmDiff = diff; bestR = r; }
                }
            }

            // Find closest col
            int bestC = -1;
            float bestTpsDiff = float.MaxValue;
            for (int c = 0; c < tbl.Cols; c++)
            {
                string cLbl = tbl.GetColLabel(session.ActiveBin, c).Replace("%", "");
                float cVal;
                if (float.TryParse(cLbl, out cVal))
                {
                    float diff = Math.Abs(cVal - currentTps);
                    if (diff < bestTpsDiff) { bestTpsDiff = diff; bestC = c; }
                }
            }

            if (bestR != liveRow || bestC != liveCol)
            {
                liveRow = bestR;
                liveCol = bestC;
                Invalidate();
            }
        }

        public void ClearLivePosition()
        {
            liveRow = -1;
            liveCol = -1;
            Invalidate();
        }

        public void ResetView()
        {
            azimuth = 45f;
            elevation = 32f;
            zoomScale = 1.0f;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right)
            {
                isRotating = true;
                mouseLastPos = e.Location;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (isRotating)
            {
                int dx = e.X - mouseLastPos.X;
                int dy = e.Y - mouseLastPos.Y;

                azimuth = (azimuth + dx * 0.6f) % 360f;
                if (azimuth < 0) azimuth += 360f;

                elevation = Math.Max(12f, Math.Min(80f, elevation - dy * 0.5f));

                mouseLastPos = e.Location;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            isRotating = false;
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (e.Delta > 0) zoomScale = Math.Min(3.0f, zoomScale * 1.1f);
            else if (e.Delta < 0) zoomScale = Math.Max(0.4f, zoomScale * 0.9f);
            Invalidate();
        }

        private struct Point3D
        {
            public float X, Y, Z;
            public Point3D(float x, float y, float z) { X = x; Y = y; Z = z; }
        }

        private struct ProjectedQuad
        {
            public PointF P0, P1, P2, P3;
            public float AvgZ;
            public float AvgValueNorm;
            public int Row, Col;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.Clear(this.BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (session == null || session.ActiveTable == null || session.ActiveBin == null)
            {
                using (var brush = new SolidBrush(Color.FromArgb(120, 140, 170)))
                {
                    string msg = "กรุณาเปิดไฟล์ BIN หรือเลือกโมเดลรถ เพื่อแสดงกราฟ 3 มิติ";
                    var sz = g.MeasureString(msg, this.Font);
                    g.DrawString(msg, this.Font, brush, (Width - sz.Width) / 2, (Height - sz.Height) / 2);
                }
                return;
            }

            XdfTable tbl = session.ActiveTable;
            byte[] bin = session.ActiveBin;

            int rows = tbl.Rows;
            int cols = tbl.Cols;

            // Find min/max and average for elevation normalization
            float minVal = float.MaxValue;
            float maxVal = float.MinValue;
            float sumVal = 0f;
            int validCount = 0;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    float v = tbl.GetDisplayValue(bin, r, c);
                    if (float.IsNaN(v) || float.IsInfinity(v)) v = 0f;
                    if (v < minVal) minVal = v;
                    if (v > maxVal) maxVal = v;
                    sumVal += v;
                    validCount++;
                }
            }
            float avgVal = validCount > 0 ? sumVal / validCount : 0f;
            if (float.IsInfinity(minVal) || float.IsInfinity(maxVal) || Math.Abs(maxVal - minVal) < 0.0001f)
            {
                maxVal = minVal + 1f;
            }

            // Projection parameters
            float centerX = Width / 2f;
            float centerY = Height / 2f + 35f;
            float baseScale = Math.Min(Width, Height) * 0.40f * zoomScale;
            float heightScale = 0.52f;

            float radAzimuth = azimuth * (float)Math.PI / 180f;
            float radElevation = elevation * (float)Math.PI / 180f;
            float cosA = (float)Math.Cos(radAzimuth);
            float sinA = (float)Math.Sin(radAzimuth);
            float cosE = (float)Math.Cos(radElevation);
            float sinE = (float)Math.Sin(radElevation);

            Func<Point3D, Tuple<PointF, float>> project = (p3) =>
            {
                // Normalize X, Y to [-1, 1]
                float nx = (p3.X / Math.Max(1f, cols - 1f) * 2f) - 1f;
                float ny = (p3.Y / Math.Max(1f, rows - 1f) * 2f) - 1f;
                float nz = p3.Z * heightScale;

                // Rotate azimuth (around Z)
                float rx = nx * cosA - ny * sinA;
                float ry = nx * sinA + ny * cosA;
                float rz = nz;

                // Rotate elevation (around X)
                float px = rx;
                float py = ry * cosE - rz * sinE;
                float pz = ry * sinE + rz * cosE;

                // Screen coordinates
                float sx = centerX + px * baseScale;
                float sy = centerY - py * baseScale;
                return new Tuple<PointF, float>(new PointF(sx, sy), pz);
            };

            float span = maxVal - minVal;
            if (span <= 0.00001f) span = 1f;

            // Draw Ground Base Plane & Wireframe Grid
            using (var baseGridPen = new Pen(Color.FromArgb(28, 38, 56), 1f))
            using (var dropLinePen = new Pen(Color.FromArgb(50, 70, 95), 1f) { DashStyle = DashStyle.Dot })
            using (var axisFont = new Font("Segoe UI", 8f, FontStyle.Bold))
            using (var axisBrush = new SolidBrush(Color.FromArgb(120, 150, 180)))
            {
                int rStep = Math.Max(1, (rows - 1) / 5);
                for (int r = 0; r < rows; r += rStep)
                {
                    var pS = project(new Point3D(0, r, 0f)).Item1;
                    var pE = project(new Point3D(cols - 1, r, 0f)).Item1;
                    g.DrawLine(baseGridPen, pS, pE);
                }
                int cStep = Math.Max(1, (cols - 1) / 5);
                for (int c = 0; c < cols; c += cStep)
                {
                    var pS = project(new Point3D(c, 0, 0f)).Item1;
                    var pE = project(new Point3D(c, rows - 1, 0f)).Item1;
                    g.DrawLine(baseGridPen, pS, pE);
                }

                // 4 Corner drop lines from surface to ground plane
                int[] cr = new int[] { 0, 0, rows - 1, rows - 1 };
                int[] cc = new int[] { 0, cols - 1, cols - 1, 0 };
                for (int i = 0; i < 4; i++)
                {
                    float v = Math.Max(0f, Math.Min(1f, (tbl.GetDisplayValue(bin, cr[i], cc[i]) - minVal) / span));
                    var ptTop = project(new Point3D(cc[i], cr[i], v)).Item1;
                    var ptBot = project(new Point3D(cc[i], cr[i], 0f)).Item1;
                    g.DrawLine(dropLinePen, ptTop, ptBot);
                }

                // Axis indicators at base
                var pOrigin = project(new Point3D(0, 0, 0f)).Item1;
                var pTpsEnd = project(new Point3D(cols - 1, 0, 0f)).Item1;
                var pRpmEnd = project(new Point3D(0, rows - 1, 0f)).Item1;
                g.DrawString("TPS % ->", axisFont, axisBrush, (pOrigin.X + pTpsEnd.X) / 2 - 15, (pOrigin.Y + pTpsEnd.Y) / 2 + 6);
                g.DrawString("RPM ->", axisFont, axisBrush, (pOrigin.X + pRpmEnd.X) / 2 - 15, (pOrigin.Y + pRpmEnd.Y) / 2 + 6);
            }

            // Build Quad List
            List<ProjectedQuad> quadList = new List<ProjectedQuad>();
            PointF liveBeaconPoint = PointF.Empty;

            for (int r = 0; r < rows - 1; r++)
            {
                for (int c = 0; c < cols - 1; c++)
                {
                    float v0 = Math.Max(0f, Math.Min(1f, (tbl.GetDisplayValue(bin, r, c) - minVal) / span));
                    float v1 = Math.Max(0f, Math.Min(1f, (tbl.GetDisplayValue(bin, r, c + 1) - minVal) / span));
                    float v2 = Math.Max(0f, Math.Min(1f, (tbl.GetDisplayValue(bin, r + 1, c + 1) - minVal) / span));
                    float v3 = Math.Max(0f, Math.Min(1f, (tbl.GetDisplayValue(bin, r + 1, c) - minVal) / span));

                    var p0 = project(new Point3D(c, r, v0));
                    var p1 = project(new Point3D(c + 1, r, v1));
                    var p2 = project(new Point3D(c + 1, r + 1, v2));
                    var p3 = project(new Point3D(c, r + 1, v3));

                    float avgZ = (p0.Item2 + p1.Item2 + p2.Item2 + p3.Item2) / 4f;
                    float avgQuadNorm = (v0 + v1 + v2 + v3) / 4f;

                    quadList.Add(new ProjectedQuad
                    {
                        P0 = p0.Item1,
                        P1 = p1.Item1,
                        P2 = p2.Item1,
                        P3 = p3.Item1,
                        AvgZ = avgZ,
                        AvgValueNorm = avgQuadNorm,
                        Row = r,
                        Col = c
                    });

                    if (r == liveRow && c == liveCol)
                    {
                        liveBeaconPoint = p0.Item1;
                    }
                }
            }

            // Painter's Algorithm: Sort quads back to front
            quadList.Sort((a, b) => a.AvgZ.CompareTo(b.AvgZ));

            // Render Quads
            using (var wirePen = new Pen(Color.FromArgb(40, 255, 255, 255), 1))
            using (var selPen = new Pen(Color.FromArgb(0, 229, 255), 2))
            {
                foreach (var q in quadList)
                {
                    PointF[] poly = new PointF[] { q.P0, q.P1, q.P2, q.P3 };
                    Color quadColor = Get3DSurfaceColor(q.AvgValueNorm);

                    using (var brush = new SolidBrush(quadColor))
                    {
                        g.FillPolygon(brush, poly);
                    }

                    if (session.SelectedRange.Contains(q.Col, q.Row))
                    {
                        g.DrawPolygon(selPen, poly);
                    }
                    else
                    {
                        g.DrawPolygon(wirePen, poly);
                    }
                }
            }

            // Render Live Telemetry 3D Beacon
            if (liveRow >= 0 && liveCol >= 0 && !liveBeaconPoint.IsEmpty)
            {
                float radius = 9f * pulseScale;
                using (var glowBrush = new SolidBrush(Color.FromArgb(120, 0, 255, 120)))
                using (var beaconBrush = new SolidBrush(Color.FromArgb(240, 0, 255, 120)))
                using (var beaconPen = new Pen(Color.White, 2f))
                {
                    g.FillEllipse(glowBrush, liveBeaconPoint.X - radius, liveBeaconPoint.Y - radius, radius * 2, radius * 2);
                    g.FillEllipse(beaconBrush, liveBeaconPoint.X - 5, liveBeaconPoint.Y - 5, 10, 10);
                    g.DrawEllipse(beaconPen, liveBeaconPoint.X - 5, liveBeaconPoint.Y - 5, 10, 10);

                    // Line down to base
                    var basePt = project(new Point3D(liveCol, liveRow, 0f)).Item1;
                    using (var linePen = new Pen(Color.FromArgb(160, 0, 255, 120), 1.5f) { DashStyle = DashStyle.Dot })
                    {
                        g.DrawLine(linePen, liveBeaconPoint, basePt);
                    }
                }
            }

            // Overlay HUD / Angles & Controls guide
            using (var hudBrush = new SolidBrush(Color.FromArgb(180, 200, 220)))
            using (var hudFont = new Font("Segoe UI", 8.5f, FontStyle.Regular))
            {
                string info = string.Format("หมุนมุมมอง: {0:F0}° | ก้ม/เงย: {1:F0}° | ซูม: {2:F1}x\n(คลิกเมาส์ซ้ายลาก = หมุน 3D | สกอร์ลเมาส์ = ซูม)", azimuth, elevation, zoomScale);
                g.DrawString(info, hudFont, hudBrush, 12, 12);
            }

            // Overlay Top-Right Data Legend Badge
            string zUnit = tbl.AxisZ != null ? tbl.AxisZ.Units : "";
            string statsText = string.Format("ต่ำสุด: {0:F2} | เฉลี่ย: {1:F2} | สูงสุด: {2:F2} {3}", minVal, avgVal, maxVal, zUnit);
            using (var badgeFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            using (var badgeBg = new SolidBrush(Color.FromArgb(190, 18, 24, 38)))
            using (var badgeBorder = new Pen(Color.FromArgb(50, 70, 100), 1f))
            using (var textBrush = new SolidBrush(Color.FromArgb(0, 229, 255)))
            {
                var sz = g.MeasureString(statsText, badgeFont);
                float bx = Width - sz.Width - 24;
                float by = 12;
                if (bx > 250)
                {
                    g.FillRectangle(badgeBg, bx, by, sz.Width + 14, sz.Height + 18);
                    g.DrawRectangle(badgeBorder, bx, by, sz.Width + 14, sz.Height + 18);
                    g.DrawString(statsText, badgeFont, textBrush, bx + 7, by + 4);

                    // Mini gradient bar
                    float barW = sz.Width;
                    float barH = 5;
                    float barX = bx + 7;
                    float barY = by + sz.Height + 7;
                    for (int i = 0; i < (int)barW; i++)
                    {
                        float t = i / barW;
                        using (var pBrush = new SolidBrush(Get3DSurfaceColor(t)))
                        {
                            g.FillRectangle(pBrush, barX + i, barY, 1, barH);
                        }
                    }
                }
            }
        }

        private Color Get3DSurfaceColor(float t)
        {
            t = Math.Max(0f, Math.Min(1f, t));
            if (t < 0.25f)
            {
                float local = t / 0.25f;
                return BlendColor(Color.FromArgb(20, 45, 120), Color.FromArgb(0, 180, 225), local);
            }
            else if (t < 0.50f)
            {
                float local = (t - 0.25f) / 0.25f;
                return BlendColor(Color.FromArgb(0, 180, 225), Color.FromArgb(0, 210, 110), local);
            }
            else if (t < 0.75f)
            {
                float local = (t - 0.50f) / 0.25f;
                return BlendColor(Color.FromArgb(0, 210, 110), Color.FromArgb(240, 215, 30), local);
            }
            else
            {
                float local = (t - 0.75f) / 0.25f;
                return BlendColor(Color.FromArgb(240, 215, 30), Color.FromArgb(235, 45, 45), local);
            }
        }

        private Color BlendColor(Color c1, Color c2, float t)
        {
            int r = (int)(c1.R + (c2.R - c1.R) * t);
            int g = (int)(c1.G + (c2.G - c1.G) * t);
            int b = (int)(c1.B + (c2.B - c1.B) * t);
            return Color.FromArgb(Math.Max(0, Math.Min(255, r)), Math.Max(0, Math.Min(255, g)), Math.Max(0, Math.Min(255, b)));
        }
    }

    #endregion
}
