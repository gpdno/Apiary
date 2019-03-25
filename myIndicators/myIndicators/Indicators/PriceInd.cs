/*
 * * LEGAL DISCLAIMER *

The currency markets can do ANYTHING at ANY TIME.
No one can guarantee or forecast how these results will perform or behave in future markets.
Anyone who uses this product or this information is responsible for deciding If, Where, When and How this product and information are used.
Anyone who uses this product or this information is responsible and liable for any outcomes that might result from the use of this product or this information.
There is no warranty or guarantee provided or implied for this product or this information for any purpose.
*/

using System;
using System.IO;
using System.ComponentModel;
using System.Windows.Media;
using Alveo.Interfaces.UserCode;
using Alveo.UserCode;
using Alveo.Common;
using Alveo.Common.Classes;

namespace Alveo.UserCode
{
    [Serializable]
    [Description("")]
    public class PriceInd : IndicatorBase
    {
        #region Properties
        // Indicator User Settings
        [Category("Settings")]
        public PriceTypes PriceType { get; set; }

        // Global Variables
        Array<double> priceDisplay;
        bool clearLog;
        #endregion

        public PriceInd()   // Class Constructor
        {
            copyright = "(C) 2019, Entity3 LLC";
            link = "";

            // Initialize settings
            PriceType = PriceTypes.PRICE_CLOSE;
            // Initialize some values
            priceDisplay = new Array<double>();
            indicator_buffers = 1;
            indicator_chart_window = true;  // Indicator overlays Price Chart
            SetIndexStyle(0, DRAW_ARROW, 0, width: 2, clr: Red);    // Red dot
            SetIndexArrow(0, 159);  // Draw dot
            SetIndexBuffer(0, priceDisplay);
            IndicatorBuffers(indicator_buffers);
            IndicatorShortName("PriceInd(" + PriceType + ")");  // for Alveo to display
            clearLog = true;
        }

        //+------------------------------------------------------------------+");
        //| Custom indicator initialization function                         |");
        //+------------------------------------------------------------------+");
        protected override int Init()
        {
            try  // to catch and handle Exceptions that might occur in this code block
            {
                SetIndexBuffer(0, priceDisplay);
                clearLog = true;
            }
            catch (Exception ex)    // catch and Print any exceptions that may have happened
            {
                Print(this.Name + ": Init: Exception: " + ex.Message);
                Print(this.Name + ":" + ex.StackTrace);
            }
            return 0;
        }

        //+------------------------------------------------------------------+");
        //| Custom indicator deinitialization function                       |");
        //+------------------------------------------------------------------+");
        protected override int Deinit()
        {
            // Whatever cleanup is needed
            return 0;
        }

        //+------------------------------------------------------------------+");
        //| Custom indicator iteration function                              |");
        //+------------------------------------------------------------------+");
        protected override int Start()
        {
            try
            {
                int counted_bars = IndicatorCounted();  // determine if Indicator buffer need updating
                var nBars = ChartBars.Count;            // number of Bars on Chart
                var e = nBars - counted_bars - 1;       // number of bars to update
                if (e < 1)                              // no data to process
                    return 0;                           // we're out of here, for now   
                if (counted_bars < 1)
                    UpdateBuffers(nBars);               // do all bars on the chart
                else
                    UpdateBuffers(e + 1);               // do only bars needed
            }
            catch (Exception ex)    // catch and Print any exceptions that may have happened
            {
                Print(this.Name + ": Start: Exception: " + ex.Message);
                Print(this.Name + ":" + ex.StackTrace);
            }
            return 0;
        }

        //+------------------------------------------------------------------+
        //| AUTO GENERATED CODE. THIS METHODS USED FOR INDICATOR CACHING     |
        //+------------------------------------------------------------------+
        #region Auto Generated Code

        [Description("Parameters order Symbol, TimeFrame, PriceType")]
        public override bool IsSameParameters(params object[] values)
        {
            // return true if no setting have changed
            if (values.Length != 3)
                return false;
            if (!CompareString(Symbol, (string)values[0]))
                return false;
            if (TimeFrame != (int)values[1])
                return false;
            if (PriceType != (PriceTypes)values[2])
                return false;
            return true;
        }

        [Description("Parameters order Symbol, TimeFrame, PriceType")]
        public override void SetIndicatorParameters(params object[] values)
        {
            // update setting if needed
            if (values.Length != 3)
                throw new ArgumentException("Invalid parameters number");
            Symbol = (string)values[0];
            TimeFrame = (int)values[1];
            PriceType = (PriceTypes)values[2];
        }

        #endregion

        public enum PriceTypes
        {
            PRICE_CLOSE = 0,
            PRICE_OPEN = 1,
            PRICE_HIGH = 2,
            PRICE_LOW = 3,
            PRICE_MEDIAN = 4,   // (b.High + b.Low) / 2
            PRICE_TYPICAL = 5,  // (b.High + b.Low + b.Close) / 3;
            PRICE_WEIGHTED = 6, // (b.High + b.Low + 2 * b.Close) / 4;
            PRICE_OHLC = 7,     // (b.Open + b.High + b.Low + b.Close) / 4
            PRICE_P7 = 8        // (b.Open + b.High + 2 * b.Low + 3 * b.Close) / 7
        }

        public double GetThePrice(PriceTypes type, Bar b)
        {
            // use the Price type and Bar to calculate the Price
            double price = -1;
            switch (type)
            {
                case PriceTypes.PRICE_CLOSE:
                    price = (double)b.Close;
                    break;
                case PriceTypes.PRICE_OPEN:
                    price = (double)b.Open;
                    break;
                case PriceTypes.PRICE_HIGH:
                    price = (double)b.High;
                    break;
                case PriceTypes.PRICE_LOW:
                    price = (double)b.Low;
                    break;
                case PriceTypes.PRICE_MEDIAN:
                    price = (double)(b.High + b.Low) / 2;
                    break;
                case PriceTypes.PRICE_TYPICAL:
                    price = (double)(b.High + b.Low + b.Close) / 3;
                    break;
                case PriceTypes.PRICE_WEIGHTED:
                    price = (double)(b.High + b.Low + 2 * b.Close) / 4;
                    break;
                case PriceTypes.PRICE_OHLC:
                    price = Math.Round((double)(b.Open + b.High + b.Low + b.Close) / 4, 5);
                    break;
                case PriceTypes.PRICE_P7:
                    price = Math.Round((double)(b.Open + b.High + 2 * b.Low + 3 * b.Close) / 7, 5);
                    break;
            }
            return price;
        }

        internal void UpdateBuffers(int count)
        {
            // update count number of values in buffer
            for (var i = 0; i < count - 1; i++)
            {
                int indx = count - i - 1;   // get bar data
                Bar b = ChartBars[indx];
                double thePrice = GetThePrice(PriceType, b);
                priceDisplay[indx] = thePrice;
                //Print("PriceInd: indx=" + indx + " thePrice=" + thePrice);
                string header = (!clearLog) ? null : "BarTime, Index, thePrice";
                string msg = b.BarTime
                    + ", " + indx 
                    + ", " + thePrice.ToString("F5");
                WriteLog(header: header, msg: msg, clear: clearLog);
                clearLog = false;
            }
        }

        internal void WriteLog(string header = null, string msg = null, bool clear = false)
        {
            // Delete Log file if clear = true
            // Append header and msg to Log file
            string dataFileDir = "C:\\temp\\";                  // file location
            string LogFilename = this.ToString() + ".Log.csv";   // file name
            if (!System.IO.Directory.Exists(dataFileDir))       // create directory if !Exists
                System.IO.Directory.CreateDirectory(dataFileDir);
            var path = dataFileDir + LogFilename;
            if (clear)                                          // Delete log file if clear=true
                System.IO.File.Delete(path);
            if (!string.IsNullOrEmpty(header) || !string.IsNullOrEmpty(msg)) // if header or msg has contents
            {
                // use StreamWriter to appeng header and msg lines to file and then Close the file
                System.IO.StreamWriter tradeLogFile = new System.IO.StreamWriter(path, append: true);
                //FileInfo f = new FileInfo(path);
                if (!string.IsNullOrEmpty(header))
                    tradeLogFile.WriteLine(header);
                if (!string.IsNullOrEmpty(msg))
                    tradeLogFile.WriteLine(msg);
                tradeLogFile.Close();
            }
        }
    }
}

