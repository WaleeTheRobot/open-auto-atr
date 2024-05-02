#region Using declarations
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;

namespace NinjaTrader.NinjaScript.Indicators
{
    public class AutoATR
    {
        public double ATRMultiplier { get; set; }
        public double RangePercentage { get; set; }
        public double Current { get; set; }
        public double Median { get; set; }
        public double HighPlusMedianAtr { get; set; }
        public double LowMinusMedianAtr { get; set; }
        public double HighLowMedianAtr { get; set; }
        public double UpperRange { get; private set; }
        public double LowerRange { get; private set; }

        public void Reset()
        {
            HighPlusMedianAtr = 0;
            LowMinusMedianAtr = 0;
            HighLowMedianAtr = 0;
            UpperRange = 0;
            LowerRange = 0;
        }

        public void SetMedianATR(List<double> atrs)
        {
            if (atrs == null || atrs.Count == 0)
            {
                return;
            }

            atrs.Sort();

            int midIndex = atrs.Count / 2;

            if (atrs.Count % 2 == 0)
            {
                Median = (atrs[midIndex - 1] + atrs[midIndex]) / 2.0;
            }
            else
            {
                Median = atrs[midIndex];
            }
        }

        private void UpdateRanges()
        {
            double range = HighPlusMedianAtr - LowMinusMedianAtr;
            UpperRange = HighPlusMedianAtr - range * (RangePercentage / 100);
            LowerRange = LowMinusMedianAtr + range * (RangePercentage / 100);
        }

        public void SetPrices(double high, double low)
        {
            // Update only if current high breaks previous HighPlusMedianAtr
            // or if current low breaks previous LowMinusMedianAtr
            if (high > HighPlusMedianAtr || low < LowMinusMedianAtr)
            {
                double highPlusMedianAtr = high + (Median * ATRMultiplier);
                double lowMinusMedianAtr = low - (Median * ATRMultiplier);

                HighPlusMedianAtr = highPlusMedianAtr;
                LowMinusMedianAtr = lowMinusMedianAtr;
                // Since we only have two numbers
                HighLowMedianAtr = (highPlusMedianAtr + lowMinusMedianAtr) / 2;

                UpdateRanges();
            }
        }
    }

    public class OpenAutoATR : Indicator
    {
        public const string GROUP_NAME = "Open Auto ATR";

        private AutoATR _autoATR;
        private Brush _highColor;
        private Brush _medianColor;
        private Brush _lowColor;
        private Brush _upperRangeColor;
        private Brush _lowerRangeColor;

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
        public Brush HighColor
        {
            get { return _highColor; }
            set { _highColor = value; }
        }

        [Browsable(false)]
        public string HighColorSerialize
        {
            get { return Serialize.BrushToString(_highColor); }
            set { _highColor = Serialize.StringToBrush(value); }
        }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Median Color", Description = "The median line color.", Order = 9, GroupName = GROUP_NAME)]
        public Brush MedianColor
        {
            get { return _medianColor; }
            set { _medianColor = value; }
        }

        [Browsable(false)]
        public string MedianColorSerialize
        {
            get { return Serialize.BrushToString(_medianColor); }
            set { _medianColor = Serialize.StringToBrush(value); }
        }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Low Color", Description = "The low line color.", Order = 10, GroupName = GROUP_NAME)]
        public Brush LowColor
        {
            get { return _lowColor; }
            set { _lowColor = value; }
        }

        [Browsable(false)]
        public string LowColorSerialize
        {
            get { return Serialize.BrushToString(_lowColor); }
            set { _lowColor = Serialize.StringToBrush(value); }
        }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Upper Range Color", Description = "The upper range color.", Order = 11, GroupName = GROUP_NAME)]
        public Brush UpperRangeColor
        {
            get { return _upperRangeColor; }
            set { _upperRangeColor = value; }
        }

        [Browsable(false)]
        public string UpperRangeColorSerialize
        {
            get { return Serialize.BrushToString(_upperRangeColor); }
            set { _upperRangeColor = Serialize.StringToBrush(value); }
        }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Lower Range Color", Description = "The lower range color.", Order = 12, GroupName = GROUP_NAME)]
        public Brush LowerRangeColor
        {
            get { return _lowerRangeColor; }
            set { _lowerRangeColor = value; }
        }

        [Browsable(false)]
        public string LowerRangeColorSerialize
        {
            get { return Serialize.BrushToString(_lowerRangeColor); }
            set { _lowerRangeColor = Serialize.StringToBrush(value); }
        }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"Automatically draws the ATR and update once high or low ATR is broken.";
                Name = "OpenAutoATR";
                Calculate = Calculate.OnEachTick;
                IsOverlay = true;
                DisplayInDataBox = true;
                DrawOnPricePanel = true;
                DrawHorizontalGridLines = true;
                DrawVerticalGridLines = true;
                PaintPriceMarkers = true;
                ScaleJustification = NinjaTrader.Gui.Chart.ScaleJustification.Right;
                //Disable this property if your indicator requires custom values that cumulate with each new market data event. 
                //See Help Guide for additional information.
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
            }
            else if (State == State.Configure)
            {
            }
            else if (State == State.DataLoaded)
            {
                _autoATR = new AutoATR
                {
                    ATRMultiplier = ATRMultiplier,
                    RangePercentage = RangePercentage
                };
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < ATRPeriod)
            {
                return;
            }

            List<double> atrs = new List<double>();

            for (int i = 0; i < MedianPeriod; i++)
            {
                atrs.Add(Math.Round(ATR(ATRPeriod)[i], 2));
            }

            _autoATR.Current = Math.Round(ATR(ATRPeriod)[0], 2);
            _autoATR.SetMedianATR(atrs);
            _autoATR.SetPrices(High[0], Low[0]);
        }

        protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
        {
            if (CurrentBar < ATRPeriod)
            {
                return;
            }

            var highColor = ConvertToDxColor(HighColor, LineOpacity);
            var medianColor = ConvertToDxColor(MedianColor, LineOpacity);
            var lowColor = ConvertToDxColor(LowColor, LineOpacity);

            var upperRangeColor = ConvertToDxColor(UpperRangeColor, RangeOpacity);
            var lowerRangeColor = ConvertToDxColor(LowerRangeColor, RangeOpacity);

            using (var highBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, highColor))
            using (var medianBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, medianColor))
            using (var lowBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, lowColor))
            using (var upperRangeBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, upperRangeColor))
            using (var lowerRangeBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, lowerRangeColor))
            {
                float chartWidth = chartControl.ChartPanels[0].W;
                float rightOffset = RightOffset;
                float fixedLength = FixedLength;

                DrawRectangle(chartWidth, rightOffset, fixedLength, upperRangeBrush, _autoATR.UpperRange, _autoATR.HighPlusMedianAtr, chartScale);
                DrawRectangle(chartWidth, rightOffset, fixedLength, lowerRangeBrush, _autoATR.LowerRange, _autoATR.LowMinusMedianAtr, chartScale);

                DrawLine(chartWidth, rightOffset, fixedLength, highBrush, _autoATR.HighPlusMedianAtr, chartScale);
                DrawLine(chartWidth, rightOffset, fixedLength, medianBrush, _autoATR.HighLowMedianAtr, chartScale);
                DrawLine(chartWidth, rightOffset, fixedLength, lowBrush, _autoATR.LowMinusMedianAtr, chartScale);
            }
        }

        private void DrawLine(float chartWidth, float rightOffset, float length, SharpDX.Direct2D1.SolidColorBrush brush, double value, ChartScale chartScale)
        {
            float yValue = chartScale.GetYByValue(value);
            float startX = chartWidth - rightOffset - length;
            float endX = chartWidth - rightOffset;

            var dashes = new float[] { 2.0f, 2.0f };
            var strokeStyleProperties = new SharpDX.Direct2D1.StrokeStyleProperties()
            {
                DashStyle = SharpDX.Direct2D1.DashStyle.Custom,
                DashOffset = 0
            };

            using (var strokeStyle = new SharpDX.Direct2D1.StrokeStyle(RenderTarget.Factory, strokeStyleProperties, dashes))
            {
                RenderTarget.DrawLine(new SharpDX.Vector2(startX, yValue), new SharpDX.Vector2(endX, yValue), brush, 2, strokeStyle);
            }
        }

        private void DrawRectangle(float chartWidth, float rightOffset, float length, SharpDX.Direct2D1.SolidColorBrush brush, double topValue, double bottomValue, ChartScale chartScale)
        {
            float topY = chartScale.GetYByValue(topValue);
            float bottomY = chartScale.GetYByValue(bottomValue);
            float leftX = chartWidth - rightOffset - length;
            float rightX = chartWidth - rightOffset;
            SharpDX.RectangleF rect = new SharpDX.RectangleF(leftX, topY, rightX - leftX, bottomY - topY);
            RenderTarget.FillRectangle(rect, brush);
        }

        private SharpDX.Color ConvertToDxColor(Brush brush, byte alpha)
        {
            var color = ((SolidColorBrush)brush).Color;
            return new SharpDX.Color(color.R, color.G, color.B, alpha);
        }
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private OpenAutoATR[] cacheOpenAutoATR;
		public OpenAutoATR OpenAutoATR(int aTRPeriod, double aTRMultiplier, double medianPeriod, float fixedLength, float rightOffset, double rangePercentage, byte lineOpacity, byte rangeOpacity, Brush highColor, Brush medianColor, Brush lowColor, Brush upperRangeColor, Brush lowerRangeColor)
		{
			return OpenAutoATR(Input, aTRPeriod, aTRMultiplier, medianPeriod, fixedLength, rightOffset, rangePercentage, lineOpacity, rangeOpacity, highColor, medianColor, lowColor, upperRangeColor, lowerRangeColor);
		}

		public OpenAutoATR OpenAutoATR(ISeries<double> input, int aTRPeriod, double aTRMultiplier, double medianPeriod, float fixedLength, float rightOffset, double rangePercentage, byte lineOpacity, byte rangeOpacity, Brush highColor, Brush medianColor, Brush lowColor, Brush upperRangeColor, Brush lowerRangeColor)
		{
			if (cacheOpenAutoATR != null)
				for (int idx = 0; idx < cacheOpenAutoATR.Length; idx++)
					if (cacheOpenAutoATR[idx] != null && cacheOpenAutoATR[idx].ATRPeriod == aTRPeriod && cacheOpenAutoATR[idx].ATRMultiplier == aTRMultiplier && cacheOpenAutoATR[idx].MedianPeriod == medianPeriod && cacheOpenAutoATR[idx].FixedLength == fixedLength && cacheOpenAutoATR[idx].RightOffset == rightOffset && cacheOpenAutoATR[idx].RangePercentage == rangePercentage && cacheOpenAutoATR[idx].LineOpacity == lineOpacity && cacheOpenAutoATR[idx].RangeOpacity == rangeOpacity && cacheOpenAutoATR[idx].HighColor == highColor && cacheOpenAutoATR[idx].MedianColor == medianColor && cacheOpenAutoATR[idx].LowColor == lowColor && cacheOpenAutoATR[idx].UpperRangeColor == upperRangeColor && cacheOpenAutoATR[idx].LowerRangeColor == lowerRangeColor && cacheOpenAutoATR[idx].EqualsInput(input))
						return cacheOpenAutoATR[idx];
			return CacheIndicator<OpenAutoATR>(new OpenAutoATR(){ ATRPeriod = aTRPeriod, ATRMultiplier = aTRMultiplier, MedianPeriod = medianPeriod, FixedLength = fixedLength, RightOffset = rightOffset, RangePercentage = rangePercentage, LineOpacity = lineOpacity, RangeOpacity = rangeOpacity, HighColor = highColor, MedianColor = medianColor, LowColor = lowColor, UpperRangeColor = upperRangeColor, LowerRangeColor = lowerRangeColor }, input, ref cacheOpenAutoATR);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.OpenAutoATR OpenAutoATR(int aTRPeriod, double aTRMultiplier, double medianPeriod, float fixedLength, float rightOffset, double rangePercentage, byte lineOpacity, byte rangeOpacity, Brush highColor, Brush medianColor, Brush lowColor, Brush upperRangeColor, Brush lowerRangeColor)
		{
			return indicator.OpenAutoATR(Input, aTRPeriod, aTRMultiplier, medianPeriod, fixedLength, rightOffset, rangePercentage, lineOpacity, rangeOpacity, highColor, medianColor, lowColor, upperRangeColor, lowerRangeColor);
		}

		public Indicators.OpenAutoATR OpenAutoATR(ISeries<double> input , int aTRPeriod, double aTRMultiplier, double medianPeriod, float fixedLength, float rightOffset, double rangePercentage, byte lineOpacity, byte rangeOpacity, Brush highColor, Brush medianColor, Brush lowColor, Brush upperRangeColor, Brush lowerRangeColor)
		{
			return indicator.OpenAutoATR(input, aTRPeriod, aTRMultiplier, medianPeriod, fixedLength, rightOffset, rangePercentage, lineOpacity, rangeOpacity, highColor, medianColor, lowColor, upperRangeColor, lowerRangeColor);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.OpenAutoATR OpenAutoATR(int aTRPeriod, double aTRMultiplier, double medianPeriod, float fixedLength, float rightOffset, double rangePercentage, byte lineOpacity, byte rangeOpacity, Brush highColor, Brush medianColor, Brush lowColor, Brush upperRangeColor, Brush lowerRangeColor)
		{
			return indicator.OpenAutoATR(Input, aTRPeriod, aTRMultiplier, medianPeriod, fixedLength, rightOffset, rangePercentage, lineOpacity, rangeOpacity, highColor, medianColor, lowColor, upperRangeColor, lowerRangeColor);
		}

		public Indicators.OpenAutoATR OpenAutoATR(ISeries<double> input , int aTRPeriod, double aTRMultiplier, double medianPeriod, float fixedLength, float rightOffset, double rangePercentage, byte lineOpacity, byte rangeOpacity, Brush highColor, Brush medianColor, Brush lowColor, Brush upperRangeColor, Brush lowerRangeColor)
		{
			return indicator.OpenAutoATR(input, aTRPeriod, aTRMultiplier, medianPeriod, fixedLength, rightOffset, rangePercentage, lineOpacity, rangeOpacity, highColor, medianColor, lowColor, upperRangeColor, lowerRangeColor);
		}
	}
}

#endregion
