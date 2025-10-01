#region Using declarations
using NinjaTrader.Custom.AddOns.OpenAutoATR;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    #region AutoATR Class
    public class AutoATR
    {
        private CircularBuffer<double> _atrBuffer;

        public double ATRMultiplier { get; set; }
        public double RangePercentage { get; set; }
        public double Current { get; set; }
        public double Median { get; private set; }
        public double HighPlusMedianAtr { get; set; }
        public double LowMinusMedianAtr { get; set; }
        public double HighLowMedianAtr { get; set; }
        public double UpperRange { get; private set; }
        public double LowerRange { get; private set; }

        public void Initialize(int medianPeriod) => _atrBuffer = new CircularBuffer<double>(medianPeriod);

        public void AddATR(double atr) => _atrBuffer.Add(atr);

        public void CalculateMedian()
        {
            if (_atrBuffer.Count == 0) return;
            var sorted = _atrBuffer.GetLastNArray(_atrBuffer.Count);
            Array.Sort(sorted);
            int mid = sorted.Length / 2;
            Median = (sorted.Length % 2 == 0) ? (sorted[mid - 1] + sorted[mid]) / 2.0 : sorted[mid];
        }

        private void UpdateRanges()
        {
            double range = HighPlusMedianAtr - LowMinusMedianAtr;
            UpperRange = HighPlusMedianAtr - range * (RangePercentage / 100.0);
            LowerRange = LowMinusMedianAtr + range * (RangePercentage / 100.0);
        }

        public void SetPrices(double high, double low)
        {
            if (high > HighPlusMedianAtr || low < LowMinusMedianAtr)
            {
                HighPlusMedianAtr = high + (Median * ATRMultiplier);
                LowMinusMedianAtr = low - (Median * ATRMultiplier);
                HighLowMedianAtr = (HighPlusMedianAtr + LowMinusMedianAtr) / 2.0;
                UpdateRanges();
            }
        }
    }
    #endregion

    public class OpenAutoATR : Indicator
    {
        public const string GROUP_NAME_GENERAL = "General";
        public const string GROUP_NAME = "Open Auto ATR";

        private AutoATR _autoATR;

        private Brush _highColor;
        private Brush _medianColor;
        private Brush _lowColor;
        private Brush _upperRangeColor;
        private Brush _lowerRangeColor;
        private Brush _atrBlockBackgroundColor;
        private Brush _atrBlockTextColor;

        private SharpDX.Direct2D1.SolidColorBrush dxHighBrush;
        private SharpDX.Direct2D1.SolidColorBrush dxMedianBrush;
        private SharpDX.Direct2D1.SolidColorBrush dxLowBrush;
        private SharpDX.Direct2D1.SolidColorBrush dxUpperRangeBrush;
        private SharpDX.Direct2D1.SolidColorBrush dxLowerRangeBrush;
        private SharpDX.Direct2D1.SolidColorBrush dxBlockBgBrush;
        private SharpDX.Direct2D1.SolidColorBrush dxBlockTextBrush;

        private SharpDX.Direct2D1.StrokeStyle dashStrokeStyle;
        private readonly float[] dashArray = new float[] { 2f, 2f };

        private SharpDX.DirectWrite.Factory dwFactory;
        private SharpDX.DirectWrite.TextFormat textFormat;
        private SharpDX.DirectWrite.TextLayout textLayout;
        private string lastBlockText;
        private int lastBlockTextTick;

        private double lastHigh, lastMid, lastLow, lastUpper, lastLower;

        private SharpDX.Color lastHighDx, lastMedianDx, lastLowDx, lastUpperDx, lastLowerDx, lastBgDx, lastTextDx;

        #region General Properties

        [NinjaScriptProperty]
        [Display(Name = "Version", Description = "Open Auto ATR version.", Order = 0, GroupName = GROUP_NAME_GENERAL)]
        [ReadOnly(true)]
        public string Version
        {
            get { return "2.0.0"; }
            set { }
        }

        #endregion

        [NinjaScriptProperty]
        [Display(Name = "ATR", Description = "The ATR period.", Order = 0, GroupName = GROUP_NAME)]
        public int ATRPeriod { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "ATR Multiplier", Description = "The ATR multiplier.", Order = 1, GroupName = GROUP_NAME)]
        public double ATRMultiplier { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Median Period", Description = "The median period to calculate.", Order = 2, GroupName = GROUP_NAME)]
        public double MedianPeriod { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Fixed Length", Description = "The length of the lines.", Order = 3, GroupName = GROUP_NAME)]
        public float FixedLength { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Right Offset", Description = "The offset for the lines from the right.", Order = 4, GroupName = GROUP_NAME)]
        public float RightOffset { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Range Percentage", Description = "The range for the upper and lower ranges.", Order = 5, GroupName = GROUP_NAME)]
        public double RangePercentage { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Line Opacity", Description = "The opacity for the lines. (0 to 255)", Order = 6, GroupName = GROUP_NAME)]
        public byte LineOpacity { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Range Opacity", Description = "The opacity for the ranges. (0 to 255)", Order = 7, GroupName = GROUP_NAME)]
        public byte RangeOpacity { get; set; }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "High Color", Description = "The high line color.", Order = 8, GroupName = GROUP_NAME)]
        public Brush HighColor { get => _highColor; set => _highColor = value; }
        [Browsable(false)]
        public string HighColorSerialize { get => Serialize.BrushToString(_highColor); set => _highColor = Serialize.StringToBrush(value); }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Median Color", Description = "The median line color.", Order = 9, GroupName = GROUP_NAME)]
        public Brush MedianColor { get => _medianColor; set => _medianColor = value; }
        [Browsable(false)]
        public string MedianColorSerialize { get => Serialize.BrushToString(_medianColor); set => _medianColor = Serialize.StringToBrush(value); }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Low Color", Description = "The low line color.", Order = 10, GroupName = GROUP_NAME)]
        public Brush LowColor { get => _lowColor; set => _lowColor = value; }
        [Browsable(false)]
        public string LowColorSerialize { get => Serialize.BrushToString(_lowColor); set => _lowColor = Serialize.StringToBrush(value); }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Upper Range Color", Description = "The upper range color.", Order = 11, GroupName = GROUP_NAME)]
        public Brush UpperRangeColor { get => _upperRangeColor; set => _upperRangeColor = value; }
        [Browsable(false)]
        public string UpperRangeColorSerialize { get => Serialize.BrushToString(_upperRangeColor); set => _upperRangeColor = Serialize.StringToBrush(value); }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Lower Range Color", Description = "The lower range color.", Order = 12, GroupName = GROUP_NAME)]
        public Brush LowerRangeColor { get => _lowerRangeColor; set => _lowerRangeColor = value; }
        [Browsable(false)]
        public string LowerRangeColorSerialize { get => Serialize.BrushToString(_lowerRangeColor); set => _lowerRangeColor = Serialize.StringToBrush(value); }

        [NinjaScriptProperty]
        [Display(Name = "Display ATR Block", Description = "Enable to display the ATR block", Order = 13, GroupName = GROUP_NAME)]
        public bool DisplayATRBlock { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "ATR Block Top Offset", Description = "The offset for the ATR block from the top.", Order = 14, GroupName = GROUP_NAME)]
        public float ATRBlockTopOffset { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "ATR Block Left Offset", Description = "The offset for the ATR block from the left.", Order = 15, GroupName = GROUP_NAME)]
        public float ATRBlockLeftOffset { get; set; }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "ATR Block Background Color", Description = "The background color for the ATR block.", Order = 16, GroupName = GROUP_NAME)]
        public Brush ATRBlockBackgroundColor { get => _atrBlockBackgroundColor; set => _atrBlockBackgroundColor = value; }
        [Browsable(false)]
        public string ATRBlockBackgroundColorSerialize { get => Serialize.BrushToString(_atrBlockBackgroundColor); set => _atrBlockBackgroundColor = Serialize.StringToBrush(value); }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "ATR Block Text Color", Description = "The text color for the ATR block.", Order = 17, GroupName = GROUP_NAME)]
        public Brush ATRBlockTextColor { get => _atrBlockTextColor; set => _atrBlockTextColor = value; }
        [Browsable(false)]
        public string ATRBlockTextColorSerialize { get => Serialize.BrushToString(_atrBlockTextColor); set => _atrBlockTextColor = Serialize.StringToBrush(value); }

        [NinjaScriptProperty]
        [Display(Name = "Use Dashed Lines", Description = "If false, uses solid lines (faster).", Order = 18, GroupName = GROUP_NAME)]
        public bool UseDashedLines { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "ATR Block Update (ms)", Description = "Minimum ms between ATR block text layout rebuilds.", Order = 19, GroupName = GROUP_NAME)]
        public int ATRBlockUpdateMs { get; set; }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"Automatically draws the ATR and updates once high or low ATR is broken.";
                Name = "_OpenAutoATR";
                Calculate = Calculate.OnEachTick;
                IsOverlay = true;
                DisplayInDataBox = true;
                DrawOnPricePanel = true;
                DrawHorizontalGridLines = true;
                DrawVerticalGridLines = true;
                PaintPriceMarkers = true;
                ScaleJustification = ScaleJustification.Right;
                IsSuspendedWhileInactive = true;

                ATRPeriod = 14;
                ATRMultiplier = 1;
                MedianPeriod = 4;
                FixedLength = 400;
                RightOffset = 15;
                LineOpacity = 200;
                RangeOpacity = 25;
                RangePercentage = 20;

                HighColor = Brushes.DarkRed;
                MedianColor = Brushes.DarkGray;
                LowColor = Brushes.DarkGreen;
                UpperRangeColor = Brushes.DarkRed;
                LowerRangeColor = Brushes.DarkGreen;

                DisplayATRBlock = true;
                ATRBlockTopOffset = 10;
                ATRBlockLeftOffset = 10;
                ATRBlockBackgroundColor = Brushes.Black;
                ATRBlockTextColor = Brushes.White;

                UseDashedLines = true;
                ATRBlockUpdateMs = 250; // Throttle text layout rebuilds

                // Data Box plots
                AddPlot(new Stroke(Brushes.DarkRed, 2), PlotStyle.Dot, "High");
                AddPlot(new Stroke(Brushes.DarkGray, 2), PlotStyle.Dot, "Median");
                AddPlot(new Stroke(Brushes.DarkGreen, 2), PlotStyle.Dot, "Low");
                AddPlot(new Stroke(Brushes.Transparent, 2), PlotStyle.Dot, "Current ATR");
                AddPlot(new Stroke(Brushes.DarkRed, 2), PlotStyle.Dot, "Upper Range");
                AddPlot(new Stroke(Brushes.DarkGreen, 2), PlotStyle.Dot, "Lower Range");
            }
            else if (State == State.Configure)
            {
                Plots[0].Brush = HighColor;
                Plots[1].Brush = MedianColor;
                Plots[2].Brush = LowColor;
                Plots[3].Brush = Brushes.Transparent;
                Plots[4].Brush = UpperRangeColor;
                Plots[5].Brush = LowerRangeColor;
            }
            else if (State == State.DataLoaded)
            {
                _autoATR = new AutoATR
                {
                    ATRMultiplier = ATRMultiplier,
                    RangePercentage = RangePercentage
                };
                _autoATR.Initialize((int)MedianPeriod);
            }
            else if (State == State.Terminated)
            {
                DisposeDx();
            }
        }

        // Use autoscale hook instead of Draw.Rectangle() workaround
        public override void OnCalculateMinMax()
        {
            if (CurrentBar < ATRPeriod)
                return;

            // Include the ATR bands in autoscale
            if (!double.IsNaN(_autoATR.HighPlusMedianAtr))
                MaxValue = Math.Max(MaxValue, _autoATR.HighPlusMedianAtr);

            if (!double.IsNaN(_autoATR.LowMinusMedianAtr))
                MinValue = Math.Min(MinValue, _autoATR.LowMinusMedianAtr);
        }

        public override void OnRenderTargetChanged()
        {
            DisposeDx();

            if (RenderTarget != null)
            {
                dxHighBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, DxColorFromBrush(HighColor, LineOpacity, out lastHighDx));
                dxMedianBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, DxColorFromBrush(MedianColor, LineOpacity, out lastMedianDx));
                dxLowBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, DxColorFromBrush(LowColor, LineOpacity, out lastLowDx));
                dxUpperRangeBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, DxColorFromBrush(UpperRangeColor, RangeOpacity, out lastUpperDx));
                dxLowerRangeBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, DxColorFromBrush(LowerRangeColor, RangeOpacity, out lastLowerDx));
                dxBlockBgBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, DxColorFromBrush(ATRBlockBackgroundColor, 255, out lastBgDx));
                dxBlockTextBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, DxColorFromBrush(ATRBlockTextColor, 255, out lastTextDx));

                if (UseDashedLines)
                {
                    var strokeProps = new SharpDX.Direct2D1.StrokeStyleProperties
                    {
                        DashStyle = SharpDX.Direct2D1.DashStyle.Custom,
                        DashOffset = 0
                    };
                    dashStrokeStyle = new SharpDX.Direct2D1.StrokeStyle(RenderTarget.Factory, strokeProps, dashArray);
                }

                dwFactory = new SharpDX.DirectWrite.Factory();
                textFormat = new SharpDX.DirectWrite.TextFormat(
                    dwFactory, "Arial",
                    SharpDX.DirectWrite.FontWeight.Normal,
                    SharpDX.DirectWrite.FontStyle.Normal,
                    12f)
                {
                    TextAlignment = SharpDX.DirectWrite.TextAlignment.Leading,
                    ParagraphAlignment = SharpDX.DirectWrite.ParagraphAlignment.Near
                };

                lastBlockText = null;
                lastBlockTextTick = 0;
            }

            base.OnRenderTargetChanged();
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < ATRPeriod) return;

            double currentATR = Math.Round(ATR(ATRPeriod)[0], 2);
            _autoATR.Current = currentATR;
            _autoATR.AddATR(currentATR);
            _autoATR.CalculateMedian();
            _autoATR.SetPrices(High[0], Low[0]);

            // Update plots for the Data Box
            Values[0][0] = _autoATR.HighPlusMedianAtr;
            Values[1][0] = _autoATR.HighLowMedianAtr;
            Values[2][0] = _autoATR.LowMinusMedianAtr;
            Values[3][0] = _autoATR.Current;
            Values[4][0] = _autoATR.UpperRange;
            Values[5][0] = _autoATR.LowerRange;
        }

        protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
        {
            if (CurrentBar < ATRPeriod || RenderTarget == null || chartControl == null || chartScale == null || !IsVisible)
                return;

            // Refresh brushes if user changed colors/opacity at runtime
            EnsureBrush(ref dxHighBrush, HighColor, LineOpacity, ref lastHighDx);
            EnsureBrush(ref dxMedianBrush, MedianColor, LineOpacity, ref lastMedianDx);
            EnsureBrush(ref dxLowBrush, LowColor, LineOpacity, ref lastLowDx);
            EnsureBrush(ref dxUpperRangeBrush, UpperRangeColor, RangeOpacity, ref lastUpperDx);
            EnsureBrush(ref dxLowerRangeBrush, LowerRangeColor, RangeOpacity, ref lastLowerDx);
            EnsureBrush(ref dxBlockBgBrush, ATRBlockBackgroundColor, 255, ref lastBgDx);
            EnsureBrush(ref dxBlockTextBrush, ATRBlockTextColor, 255, ref lastTextDx);

            float chartWidth = ChartPanel?.W ?? chartControl.ChartPanels[0].W;
            float rightOffset = RightOffset;
            float fixedLength = FixedLength;
            float leftX = chartWidth - rightOffset - fixedLength;
            float rightX = chartWidth - rightOffset;

            var h = _autoATR.HighPlusMedianAtr;
            var m = _autoATR.HighLowMedianAtr;
            var l = _autoATR.LowMinusMedianAtr;
            var ur = _autoATR.UpperRange;
            var lr = _autoATR.LowerRange;

            // Early out if nothing changed and no info block
            if (!DisplayATRBlock &&
                AlmostEq(h, lastHigh) && AlmostEq(m, lastMid) && AlmostEq(l, lastLow) &&
                AlmostEq(ur, lastUpper) && AlmostEq(lr, lastLower))
                return;

            // Rectangles (fills)
            DrawRectangle(leftX, rightX, dxUpperRangeBrush, ur, h, chartScale);
            DrawRectangle(leftX, rightX, dxLowerRangeBrush, lr, l, chartScale);

            // Geometry antialias off (faster for horizontals)
            var prevAA = RenderTarget.AntialiasMode;
            RenderTarget.AntialiasMode = SharpDX.Direct2D1.AntialiasMode.Aliased;

            // Text AA to grayscale (cheaper than ClearType)
            var prevTA = RenderTarget.TextAntialiasMode;
            RenderTarget.TextAntialiasMode = SharpDX.Direct2D1.TextAntialiasMode.Grayscale;

            // Lines
            DrawLine(leftX, rightX, dxHighBrush, h, chartScale);
            DrawLine(leftX, rightX, dxMedianBrush, m, chartScale);
            DrawLine(leftX, rightX, dxLowBrush, l, chartScale);

            // ATR Block
            if (DisplayATRBlock)
                DrawATRBlock(_autoATR.Current, h - m);

            // restore modes
            RenderTarget.AntialiasMode = prevAA;
            RenderTarget.TextAntialiasMode = prevTA;

            lastHigh = h; lastMid = m; lastLow = l; lastUpper = ur; lastLower = lr;
        }

        private void DrawLine(float x0, float x1, SharpDX.Direct2D1.SolidColorBrush brush, double price, ChartScale chartScale)
        {
            float y = chartScale.GetYByValue(price);

            if (UseDashedLines && dashStrokeStyle != null)
                RenderTarget.DrawLine(new SharpDX.Vector2(x0, y), new SharpDX.Vector2(x1, y), brush, 2f, dashStrokeStyle);
            else
                RenderTarget.DrawLine(new SharpDX.Vector2(x0, y), new SharpDX.Vector2(x1, y), brush, 2f);
        }

        private void DrawRectangle(float leftX, float rightX, SharpDX.Direct2D1.SolidColorBrush brush, double topVal, double bottomVal, ChartScale chartScale)
        {
            float topY = chartScale.GetYByValue(topVal);
            float bottomY = chartScale.GetYByValue(bottomVal);
            if (bottomY < topY) { var tmp = topY; topY = bottomY; bottomY = tmp; }
            var rect = new SharpDX.RectangleF(leftX, topY, rightX - leftX, bottomY - topY);
            RenderTarget.FillRectangle(rect, brush);
        }

        private void DrawATRBlock(double currentAtr, double priceDistance)
        {
            if (dwFactory == null || textFormat == null || dxBlockBgBrush == null || dxBlockTextBrush == null)
                return;

            float x = ATRBlockLeftOffset;
            float y = ATRBlockTopOffset;
            float width = 135f;
            float height = 40f;
            float padding = 5f;

            // Throttle TextLayout rebuilds
            int now = Environment.TickCount;
            string text = $"ATR: {Math.Round(currentAtr, 2)}\nPrice Distance: {Math.Round(priceDistance, 2)}";
            if (text != lastBlockText && (now - lastBlockTextTick >= ATRBlockUpdateMs))
            {
                textLayout?.Dispose();
                textLayout = new SharpDX.DirectWrite.TextLayout(dwFactory, text, textFormat, width - 2 * padding, height - 2 * padding);
                lastBlockText = text;
                lastBlockTextTick = now;
            }

            var rect = new SharpDX.RectangleF(x, y, width, height);
            RenderTarget.FillRectangle(rect, dxBlockBgBrush);
            if (textLayout != null)
                RenderTarget.DrawTextLayout(new SharpDX.Vector2(x + padding, y + padding), textLayout, dxBlockTextBrush);
        }

        private SharpDX.Color DxColorFromBrush(Brush brush, byte alpha, out SharpDX.Color cachedOut)
        {
            var c = ((SolidColorBrush)brush).Color;
            cachedOut = new SharpDX.Color(c.R, c.G, c.B, alpha);
            return cachedOut;
        }

        private void EnsureBrush(ref SharpDX.Direct2D1.SolidColorBrush brushRef, Brush wpfBrush, byte alpha, ref SharpDX.Color lastDx)
        {
            var c = ((SolidColorBrush)wpfBrush).Color;
            var desired = new SharpDX.Color(c.R, c.G, c.B, alpha);

            if (brushRef == null || !desired.Equals(lastDx))
            {
                brushRef?.Dispose();
                brushRef = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, desired);
                lastDx = desired;
            }
        }

        private void DisposeDx()
        {
            dashStrokeStyle?.Dispose(); dashStrokeStyle = null;
            dxHighBrush?.Dispose(); dxHighBrush = null;
            dxMedianBrush?.Dispose(); dxMedianBrush = null;
            dxLowBrush?.Dispose(); dxLowBrush = null;
            dxUpperRangeBrush?.Dispose(); dxUpperRangeBrush = null;
            dxLowerRangeBrush?.Dispose(); dxLowerRangeBrush = null;
            dxBlockBgBrush?.Dispose(); dxBlockBgBrush = null;
            dxBlockTextBrush?.Dispose(); dxBlockTextBrush = null;

            textLayout?.Dispose(); textLayout = null;
            textFormat?.Dispose(); textFormat = null;
            dwFactory?.Dispose(); dwFactory = null;

            lastBlockText = null;
            lastBlockTextTick = 0;
        }

        private static bool AlmostEq(double a, double b) => Math.Abs(a - b) < 1e-9;
    }
}



#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
    public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
    {
        private OpenAutoATR[] cacheOpenAutoATR;
        public OpenAutoATR OpenAutoATR(string version, int aTRPeriod, double aTRMultiplier, double medianPeriod, float fixedLength, float rightOffset, double rangePercentage, byte lineOpacity, byte rangeOpacity, Brush highColor, Brush medianColor, Brush lowColor, Brush upperRangeColor, Brush lowerRangeColor, bool displayATRBlock, float aTRBlockTopOffset, float aTRBlockLeftOffset, Brush aTRBlockBackgroundColor, Brush aTRBlockTextColor)
        {
            return OpenAutoATR(Input, version, aTRPeriod, aTRMultiplier, medianPeriod, fixedLength, rightOffset, rangePercentage, lineOpacity, rangeOpacity, highColor, medianColor, lowColor, upperRangeColor, lowerRangeColor, displayATRBlock, aTRBlockTopOffset, aTRBlockLeftOffset, aTRBlockBackgroundColor, aTRBlockTextColor);
        }

        public OpenAutoATR OpenAutoATR(ISeries<double> input, string version, int aTRPeriod, double aTRMultiplier, double medianPeriod, float fixedLength, float rightOffset, double rangePercentage, byte lineOpacity, byte rangeOpacity, Brush highColor, Brush medianColor, Brush lowColor, Brush upperRangeColor, Brush lowerRangeColor, bool displayATRBlock, float aTRBlockTopOffset, float aTRBlockLeftOffset, Brush aTRBlockBackgroundColor, Brush aTRBlockTextColor)
        {
            if (cacheOpenAutoATR != null)
                for (int idx = 0; idx < cacheOpenAutoATR.Length; idx++)
                    if (cacheOpenAutoATR[idx] != null && cacheOpenAutoATR[idx].Version == version && cacheOpenAutoATR[idx].ATRPeriod == aTRPeriod && cacheOpenAutoATR[idx].ATRMultiplier == aTRMultiplier && cacheOpenAutoATR[idx].MedianPeriod == medianPeriod && cacheOpenAutoATR[idx].FixedLength == fixedLength && cacheOpenAutoATR[idx].RightOffset == rightOffset && cacheOpenAutoATR[idx].RangePercentage == rangePercentage && cacheOpenAutoATR[idx].LineOpacity == lineOpacity && cacheOpenAutoATR[idx].RangeOpacity == rangeOpacity && cacheOpenAutoATR[idx].HighColor == highColor && cacheOpenAutoATR[idx].MedianColor == medianColor && cacheOpenAutoATR[idx].LowColor == lowColor && cacheOpenAutoATR[idx].UpperRangeColor == upperRangeColor && cacheOpenAutoATR[idx].LowerRangeColor == lowerRangeColor && cacheOpenAutoATR[idx].DisplayATRBlock == displayATRBlock && cacheOpenAutoATR[idx].ATRBlockTopOffset == aTRBlockTopOffset && cacheOpenAutoATR[idx].ATRBlockLeftOffset == aTRBlockLeftOffset && cacheOpenAutoATR[idx].ATRBlockBackgroundColor == aTRBlockBackgroundColor && cacheOpenAutoATR[idx].ATRBlockTextColor == aTRBlockTextColor && cacheOpenAutoATR[idx].EqualsInput(input))
                        return cacheOpenAutoATR[idx];
            return CacheIndicator<OpenAutoATR>(new OpenAutoATR() { Version = version, ATRPeriod = aTRPeriod, ATRMultiplier = aTRMultiplier, MedianPeriod = medianPeriod, FixedLength = fixedLength, RightOffset = rightOffset, RangePercentage = rangePercentage, LineOpacity = lineOpacity, RangeOpacity = rangeOpacity, HighColor = highColor, MedianColor = medianColor, LowColor = lowColor, UpperRangeColor = upperRangeColor, LowerRangeColor = lowerRangeColor, DisplayATRBlock = displayATRBlock, ATRBlockTopOffset = aTRBlockTopOffset, ATRBlockLeftOffset = aTRBlockLeftOffset, ATRBlockBackgroundColor = aTRBlockBackgroundColor, ATRBlockTextColor = aTRBlockTextColor }, input, ref cacheOpenAutoATR);
        }
    }
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
    public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
    {
        public Indicators.OpenAutoATR OpenAutoATR(string version, int aTRPeriod, double aTRMultiplier, double medianPeriod, float fixedLength, float rightOffset, double rangePercentage, byte lineOpacity, byte rangeOpacity, Brush highColor, Brush medianColor, Brush lowColor, Brush upperRangeColor, Brush lowerRangeColor, bool displayATRBlock, float aTRBlockTopOffset, float aTRBlockLeftOffset, Brush aTRBlockBackgroundColor, Brush aTRBlockTextColor)
        {
            return indicator.OpenAutoATR(Input, version, aTRPeriod, aTRMultiplier, medianPeriod, fixedLength, rightOffset, rangePercentage, lineOpacity, rangeOpacity, highColor, medianColor, lowColor, upperRangeColor, lowerRangeColor, displayATRBlock, aTRBlockTopOffset, aTRBlockLeftOffset, aTRBlockBackgroundColor, aTRBlockTextColor);
        }

        public Indicators.OpenAutoATR OpenAutoATR(ISeries<double> input, string version, int aTRPeriod, double aTRMultiplier, double medianPeriod, float fixedLength, float rightOffset, double rangePercentage, byte lineOpacity, byte rangeOpacity, Brush highColor, Brush medianColor, Brush lowColor, Brush upperRangeColor, Brush lowerRangeColor, bool displayATRBlock, float aTRBlockTopOffset, float aTRBlockLeftOffset, Brush aTRBlockBackgroundColor, Brush aTRBlockTextColor)
        {
            return indicator.OpenAutoATR(input, version, aTRPeriod, aTRMultiplier, medianPeriod, fixedLength, rightOffset, rangePercentage, lineOpacity, rangeOpacity, highColor, medianColor, lowColor, upperRangeColor, lowerRangeColor, displayATRBlock, aTRBlockTopOffset, aTRBlockLeftOffset, aTRBlockBackgroundColor, aTRBlockTextColor);
        }
    }
}

namespace NinjaTrader.NinjaScript.Strategies
{
    public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
    {
        public Indicators.OpenAutoATR OpenAutoATR(string version, int aTRPeriod, double aTRMultiplier, double medianPeriod, float fixedLength, float rightOffset, double rangePercentage, byte lineOpacity, byte rangeOpacity, Brush highColor, Brush medianColor, Brush lowColor, Brush upperRangeColor, Brush lowerRangeColor, bool displayATRBlock, float aTRBlockTopOffset, float aTRBlockLeftOffset, Brush aTRBlockBackgroundColor, Brush aTRBlockTextColor)
        {
            return indicator.OpenAutoATR(Input, version, aTRPeriod, aTRMultiplier, medianPeriod, fixedLength, rightOffset, rangePercentage, lineOpacity, rangeOpacity, highColor, medianColor, lowColor, upperRangeColor, lowerRangeColor, displayATRBlock, aTRBlockTopOffset, aTRBlockLeftOffset, aTRBlockBackgroundColor, aTRBlockTextColor);
        }

        public Indicators.OpenAutoATR OpenAutoATR(ISeries<double> input, string version, int aTRPeriod, double aTRMultiplier, double medianPeriod, float fixedLength, float rightOffset, double rangePercentage, byte lineOpacity, byte rangeOpacity, Brush highColor, Brush medianColor, Brush lowColor, Brush upperRangeColor, Brush lowerRangeColor, bool displayATRBlock, float aTRBlockTopOffset, float aTRBlockLeftOffset, Brush aTRBlockBackgroundColor, Brush aTRBlockTextColor)
        {
            return indicator.OpenAutoATR(input, version, aTRPeriod, aTRMultiplier, medianPeriod, fixedLength, rightOffset, rangePercentage, lineOpacity, rangeOpacity, highColor, medianColor, lowColor, upperRangeColor, lowerRangeColor, displayATRBlock, aTRBlockTopOffset, aTRBlockLeftOffset, aTRBlockBackgroundColor, aTRBlockTextColor);
        }
    }
}

#endregion
