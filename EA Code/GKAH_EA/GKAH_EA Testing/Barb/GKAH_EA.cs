/*
 * * LEGAL DISCLAIMER *

DISCLAIMER: Forex trading involves substantial risk of loss and is not suitable for every investor. 
The valuation of forex pairs my fluctuate and may lose more than the original investment. The impact of seasonal 
and geopolitical events factor into market prices. The highly leveraged nature of Forex trading means that small 
market movements will have a great impact on your trading account and this can work against you, leading to large 
losses and you may sustain a total loss greater than the amount you deposited into your account. You are responsible 
for all the risks and financial resources you use and for the chosen trading system. You should not engage in trading 
unless you fully understand the nature of the transactions you are entering into and the extent of your exposure to loss. 
If you do not fully understand these risks you must seek independent advice from your financial advisor.
All trading strategies are used at your own risk.  Any content in this Expet Advisor (EA) should not be relied upon 
as advice or construed as providing recommendations of any kind. It is your responsibility to confirm and decide which 
trades to make. Trade only with risk capital; that is, trade with money that, if lost, will not adversely impact your 
lifestyle and your ability to meet your financial obligations. Past results are no indication of future performance. 
In no event should the content of this correspondence be construed as an express or implied promise or guarantee.
Loss-limiting strategies such as stop loss orders may not be effective because market conditions or technological issues 
may make it impossible to execute such orders. Any strategies provided in this correspondence EA is intended solely for 
informational purposes and is obtained from sources believed to be reliable. Information is in no way guaranteed. 
No guarantee of any kind is implied or possible where projections of future conditions are attempted.  None of the content
presented in this EA constitutes a recommendation that any particular security, portfolio of securities, 
transaction or investment strategy is suitable for any specific person.
 
 This EA relies heavely on the work and instrustions provide by Don Baechtel.  Thank you Don!
 */

#region usingDirectives
// DLL Libararies to use
// **Make sure that locations to these Libraries have been added to the Project References in Solution
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using Alveo.Interfaces.UserCode;
using Alveo.Common.Classes;
using Alveo.Common.Enums;
#endregion

namespace Alveo.UserCode
{
    [Serializable]
    [Description("")]
    public class GKAH_EA : ExpertAdvisorBase   // **Expert Advisor Class name must be the same as the filename
    {
        public enum PriceTypes      // selectable Price Types
        {
            PRICE_CLOSE = 0,
            PRICE_OPEN = 1,
            PRICE_HIGH = 2,
            PRICE_LOW = 3,
            PRICE_MEDIAN = 4,
            PRICE_TYPICAL = 5,
            PRICE_WEIGHTED = 6,
            PRICE_OHLC = 7,
            PRICE_P7 = 8
        }

        #region User Settings          // ** Add Alveo EA User Settings Declared here

        /*[Category("CCI")]
        [Description("Period in Bars [ex: 7]")]
        public int CCI_period { get; set; }

        [Category("CCI")]
        [Description("CCI factor [ex: 0.015]")]
        public double CCI_factor { get; set; }*/

        [Category("Settings")]
        [Description("Stoploss limit in Pips is set to 5")]
        public int Stoploss; //{ get; set; }

        [Category("Settings")]
        [Description("TakeProfit limit in Pipsis set to 3")]
        public int TakeProfit; //{ get; set; }

        [Category("Settings")]
        [Description("Number of Standard lots to trade. [ex: 0.05]")]
        public double Quantity; //{ get; set; }

        [Category("Settings")]
        [Description("Maximum Bid/Ask Spread in Pips to Open trades. [ex: 2.5]")]
        public int MaxSpread; //{ get; set; }

        #endregion

        #region EA variables    // ** Declare EA variables here
        string version = "r0.31 2x4";        // EA version - used to identify the output file
        datetime datetime0 = 0;             // minimum datetime
        public string pair = "EUR/USD";     // default curency
        bool startSession;                  // start of session flag
        bool sessionEnded;
        DateTime sessionStartTM;
        TimeSpan sessionClock;
        int countBars;

        // simulation variables
        public bool simulate;               // simulation mode flag
        public bool optimize;               // optimization mode flag
        public string simSymbol;            // symbol for simulation
        public double simAccountBalance;    // Account Balance for Simulation
        public double simFreeMargin;        // Account Free Margin for Simulation
        public DateTime simTime;            // UTC Time for simulation
        public DateTime simLocalTime;       // Local Time for simulation
        internal TimeFrame simTimeframe;    // chart timeframe for simulation
        public int simBars;                 // number of chart bars for simulation
        public bool TPflag;
        public bool SLflag;
        Random rnd = new Random(2874);      // random selector for Sl or TP simulation selector
        public int CCI_period;              //
        public PriceTypes PriceType;        //

        string symbol;                      // chart symbol
        double pipPos;                      // Pip position
        double point;                       // Point multipler
        int ticketNum;                      // order ticketNum
        internal double curPrice;           // current Price
        bool candle;
        int accountNumber;                  // account number
        DateTime curTime;                   // current UTC Time
        internal BarData curBar;            // current Bar
        internal EA_State s;                // EA State Class object
        internal bool riskLimitReached;     // riskLimitReached
        internal bool tradingclosed;        // tradingclosed
        internal bool maintPeriod;          // maintenance period
        internal bool paused;               // trading Paused, no new trades
        internal Object TLock;              // Lock for TradeLog file access
        internal Object SLock;              // Lock for Start access
        internal double accountBalance;     // account balance
        internal double accountFreeMargin;  // account balance
        internal double riskLimit;          // in Pips,  i.e. 1.8% of AccountBallance for 1 Standard lot
        string magicStr;
        TimeFrame timeFrame;
        internal DateTime OldTime, OldTime2, OldTime3, nextDay;
        int curBars;
        int total;
        internal string dataFileDir;            //directory for data files
        System.IO.StreamWriter tradeLogFile;    // StreamWriter for tradeLogFile
        Dictionary<string, double> pipValues;
        bool maxRiskReached;
        bool firstLine;
        bool initialized;
        int TIF;                                // Time In Force for Pending orders in hours

        CCIobj cci;                             // CCI Indicator instance variable

        TimeSpan fridayPause = new TimeSpan(12 + 2, 00, 00);         // Local time
        TimeSpan fridayclose = new TimeSpan(12 + 4, 50, 00);         // Local time
        TimeSpan dailyMaintStart = new TimeSpan(12 + 4, 50, 00);     // Local time
        TimeSpan dailyMaintEnd = new TimeSpan(12 + 5, 15, 00);       // Local time
        TimeSpan sundayOpen = new TimeSpan(22, 00, 00);              // UTC time

        int sessionSundayOpen = 0;     // 00:00
        int sessionSundayClose = 86400; // 24:00
        int sessionMondayThursdayOpen = 0;     // 00:00
        int sessionMondayThursdayClose = 86400; // 24:00
        int sessionFridayOpen = 0;     // 00:00
        int sessionFridayClose = 86400; // 24:00
        bool sessionIgnoreSunday = true; // will not trade on Sunday when set to true
        bool sessionCloseAtSessionClose = true;
        bool sessionCloseAtFridayClose = true;

        DateTime lastTime;
        DateTime lastReport;
        DateTime oldFilled;
        TimeSpan updaterate;
        TimeSpan reportRate;
        #endregion

        #region EA constructors      // **Declare EA Constructors and Initialize Class variables
        public GKAH_EA()      // Class constuctor, called by Alveo
        {
            simulate = false;       // default values
            optimize = false;
            InitEA();
        }

        public GKAH_EA(bool optimizing, DateTime time, string path) : this()  // constructor called by simulation
        {
            simulate = true;
            optimize = optimizing;
            simTime = time;
            dataFileDir = path;
        }

        public void InitEA()  // ** Initiatization of EA variables on creation
        {
            // Initializa EA variables
            copyright = "(C)2019 GPDNO Trading LLC. all rights reserved";
            link = "";
            simSymbol = pair;
            simTimeframe = TimeFrame.M5; // sets timeframe for trading pair
            simBars = 0;
            symbol = simSymbol;
            ticketNum = 0;
            countBars = 0;
            TIF = 0;

            // ** Default User Setting values
            CCI_period = 7;  // fixed CCI Period
            Stoploss = 4;  // stop loss in pips
            TakeProfit = 2;  // take profit in pips
            Quantity = 0.5; // lot size
            MaxSpread = 25;
            PriceType = PriceTypes.PRICE_TYPICAL; // used for calculating CCI

            curPrice = double.MinValue;
            curTime = GetCurTime();
            curBar = null;
            curBars = 0;
            riskLimit = 1.8;                // in Pips,  i.e. 1.8% of AccountBallance for 1 Standard lot
            riskLimitReached = false;
            simAccountBalance = 10000;
            simFreeMargin = simAccountBalance;
            accountNumber = 0;
            riskLimitReached = false;
            tradingclosed = false;
            maintPeriod = false;
            paused = false;
            s = new EA_State();         // create EA State object
            sessionEnded = true;
            sessionStartTM = DateTime.MinValue;
            firstLine = true;
            maxRiskReached = false;
            initialized = true;
            candle = false;
            //LoadPipValues();
        }

        #endregion

        #region EA external Entry points
        //external interface for Simulator
        internal int doInit()
        {
            return Init();
        }

        //+------------------------------------------------------------------+"
        //| EA initialization function                                       |"
        //|                                                                  |"
        //| called by Alveo to init EA when refreshed ore restarted          |"
        //+------------------------------------------------------------------+"

        // ** Initialize EA variables when the EA is restarted by Alveo
        protected override int Init()
        {
            try     // EXception Handling in case something goes wrong
            {
                if (!optimize)
                    LogPrint("Init:  started.");
                initialized = true;
                timeFrame = GetTimeframe();
                curTime = GetCurTime();
                countBars = 0;
                LoadPipValues();
                sessionEnded = true;  // start new session
                maxRiskReached = false;
                paused = false;
                symbol = GetSymbol();
                var digits = GetDigits();
                pipPos = Math.Pow(10, digits - 1);
                point = 1 / pipPos / 10;
                //TickValue = GetTickValue();
                var tf = timeFrame;
                var tfStr = (tf != TimeFrame.Unknown) ? tf.ToString() : "S10";
                dataFileDir = "C:\\temp\\" + this.Name + " " + pair.Replace('/', '-') + " " + tfStr + " v" + version + "\\"; // output file name
                magicStr = this.Name + "," + symbol.Replace("/", "") + "," + tfStr;
                if (s == null)
                    s = new EA_State();
                s.ClearState();
                OldTime = GetCurTime();
                OldTime2 = OldTime;
                OldTime3 = OldTime;
                nextDay = new DateTime(
                      OldTime3.Year,
                      OldTime3.Month,
                      OldTime3.Day,
                      0, 0, 0, 0) + TimeSpan.FromHours(24);
                if (s.firstRun)     // initialization on firstRun
                {
                    if (!optimize)
                    {
                        if (!System.IO.Directory.Exists("C:\\temp"))
                            System.IO.Directory.CreateDirectory("C:\\temp");
                        if (!System.IO.Directory.Exists(dataFileDir))
                            System.IO.Directory.CreateDirectory(dataFileDir);
                    }
                    sessionEnded = true;
                    s.firstRun = false;
                }
                accountNumber = GetAccountNumber();
                s.startingBalance = GetAccountBalance();
                s.closedOrders.Clear();
                lastTime = DateTime.MinValue;
                lastReport = DateTime.MinValue;
                oldFilled = DateTime.MinValue;
                updaterate = new TimeSpan(0, 0, 10);    // 10 seconds for CheckExits
                reportRate = new TimeSpan(0, 15, 0);    // 15 minutes for status report
                curBar = GetCurBar();
                s.dI = curBar;
                InitInds();                             // initialize Indicators
                if (!optimize)
                    LogPrint(" Init: Initialization successful. Verion=" + version);
            }
            catch (Exception e)
            {
                LogPrint("Init:" + e.Message);
                LogPrint(e.StackTrace);
            }
            return 0;
        }

        //+------------------------------------------------------------------+"
        //| expert deinitialization function                                 |"
        //|                                                                  |"
        //| called by Alveo to unitialize EA before termination.             |"
        //+------------------------------------------------------------------+"
        protected override int Deinit()
        {
            if (!optimize)
                LogPrint(" Deinit.");
            // ** Do any cleanup required before EA terminates
            return 0;
        }

        // eternal interface for Simulator
        internal int doStart()
        {
            return this.Start();
        }

        //+------------------------------------------------------------------+"
        //| expert start function                                            |"
        //|                                                                  |"
        //| called by Alveo for every new bar on chart                       |"
        //| and when the Bid or Ask price change (several times per second.  |"
        //+------------------------------------------------------------------+"
        protected override int Start()
        {
            try
            {
                if (SLock == null)
                    SLock = new object();
                lock (SLock)            // only enter once
                {
                    if (!simulate && Chart == null)     // must have Chart if Alveo
                        return 0;
                    symbol = GetSymbol();
                    curTime = GetCurTime();
                    bool report = false;
                    if (curTime.Subtract(lastReport) >= reportRate)  // report status evry so often
                    {
                        lastReport = curTime;
                        report = true;
                        if (!optimize)
                            LogPrint("Start: is running OK.  OKtoTrade=" + s.OKtoTrade);
                    }
                    if (IsEaStopped)        // if  IsEaStopped, then Exit
                    {
                        if (!optimize && report)
                            LogPrint("Start: IsEaStopped=" + IsEaStopped);
                        return 0;
                    }
                    if (!simulate && curTime.Subtract(lastTime) >= updaterate)  // run every 10 sec
                    {
                        lastTime = curTime;
                        if (!simulate)
                        {
                            double price = iClose(null, 0, 0);
                            CheckExits(price);      // Check order Exit conditions 
                        }
                    }
                    curBar = GetCurBar();       // get current most recent Bar
                    s.dI = curBar;
                    var nBars = GetBars();      // When a new Bar closes
                    if (nBars <= curBars)       // no new Bar
                        return 0;
                    curBars = nBars;
                    if (curBar == null)
                    {
                        if (!optimize)
                            LogPrint("Start: No chart Bars are available!! ");
                        return 0;
                    }
                    curPrice = s.dI.close;
                    accountBalance = GetAccountBalance();
                    accountFreeMargin = GetFreeMargin();
                    riskLimit = 1.8 * accountBalance / 100 / 10; // 1.8 percent risk limit in Pips for 1 Standard lot
                    if (DetectChanged(ref s.OKtoTrade, CheckOKToTrade()))
                    {
                        if (!optimize)
                            LogPrint(" OKtoTrade=" + s.OKtoTrade.ToString() + " DateTime=" + GetCurTime());
                    }
                    if (s.OKtoTrade && CheckSessionStarted())
                    {
                        startSession = true;
                        sessionStartTM = curTime;
                        sessionClock = new TimeSpan(0, 0, 0);
                        sessionEnded = false;
                        countBars = 0;
                    }
                    if (sessionEnded || sessionStartTM.Year < 1980)
                        sessionClock = new TimeSpan(0, 0, 0);
                    else
                        sessionClock = curTime.Subtract(sessionStartTM);
                    countBars++;
                    double thePrice = GetThePrice(PriceType, ref s.dI.bar);
                    cci.Calc(thePrice);
                    Monitor();                  // Monitor all orders
                    if (sessionEnded)
                        return 0;
                    Strategy();                 // execute trading Strategy
                    startSession = false;       // start of session is complete
                }
            }
            catch (Exception e)  // in case something goes horribly wrong
            {
                LogPrint(" Start: Exception: " + e.Message);
                LogPrint("Exception: " + e.StackTrace);
                SLock = null;
                throw e;
            }
            return 0;
        }
        #endregion

        #region Encapsulated Data Classes
        // encapsulated EA State Class
        // holds EA State data
        internal class EA_State
        {
            internal bool firstRun;
            internal bool isConnected;
            internal bool Stopped;
            internal bool isTradeAllowed;
            internal bool OKtoTrade;
            internal Statistics stats;
            internal BarData dI;
            internal int targetDir;
            internal double startingBalance;
            internal int ordersThisHour;
            internal int ordersThisDay;
            internal int tradesThisHour;
            internal int tradesThisDay;
            private Object thisLock;
            internal Dictionary<long, Order> buyOpenOrders = null;
            internal Dictionary<long, Order> sellOpenOrders = null;
            internal Dictionary<long, Order> longLimitOrders = null;
            internal Dictionary<long, Order> shortLimitOrders = null;
            internal Dictionary<long, Order> closedOrders = null;
            internal Dictionary<long, Order> orders = null;

            /// <summary>
            /// DENA3_State Class object constructor
            /// </summary>
            public EA_State()
            {
                firstRun = true;
                isConnected = true;
                Stopped = false;
                isTradeAllowed = false;
                OKtoTrade = true;
                stats = new Statistics(0);
                thisLock = new Object();
                buyOpenOrders = null;
                sellOpenOrders = null;
                longLimitOrders = null;
                shortLimitOrders = null;
                closedOrders = null;
                orders = null;
            }

            /// <summary>
            /// DENA3_State initialization
            /// </summary>
            internal void ClearState()
            {
                isConnected = true;
                Stopped = false;
                isTradeAllowed = false;
                stats = new Statistics(0);
                dI = null;
                targetDir = 0;
                startingBalance = double.MinValue;
                ordersThisHour = 0;
                ordersThisDay = 0;
                tradesThisHour = 0;
                tradesThisDay = 0;
                buyOpenOrders = new Dictionary<long, Order>();
                sellOpenOrders = new Dictionary<long, Order>();
                longLimitOrders = new Dictionary<long, Order>();
                shortLimitOrders = new Dictionary<long, Order>();
                closedOrders = new Dictionary<long, Order>();
                orders = new Dictionary<long, Order>();
            }

            /// <summary>
            /// Save DENA3_State to restore file
            /// <param name="restore">ref to System.IO.StreamWriter</param>
            /// </summary>
            public struct Statistics
            {
                internal double profitLoss;
                internal double maxProfit;
                internal double maxLoss;
                internal double maxDrawdown;
                internal double maxDailyDrawdown;
                internal double largestDailyDrawdown;
                internal bool exceededDailyDrawdown;
                internal int numContracts;
                internal int numWins;
                internal int numLoss;
                internal double avgWin;
                internal double avgLoss;
                internal double winRate;
                internal double expectancy;
                internal int numOpen;
                internal int numDays;
                internal int numHours;
                internal int numTrades;
                internal int numOrders;
                internal int orderExits;
                internal int computeStats;
                internal double sumPips;
                internal int sumWins;
                internal int sumLoss;
                internal double avgWin2;
                internal double avgLoss2;
                internal double avgWinBars;
                internal double avgLossBars;
                internal int numEntries;
                internal int numJISO;

                /// <summary>
                /// Initialize Trading Statistics
                /// </summary>
                internal Statistics(int x)
                {
                    profitLoss = 0;
                    maxProfit = 0;
                    maxLoss = 0;
                    maxDrawdown = 0;
                    maxDailyDrawdown = 0;
                    largestDailyDrawdown = 0;
                    exceededDailyDrawdown = false;
                    numContracts = 0;
                    numWins = 0;
                    numLoss = 0;
                    avgWin = 0;
                    avgLoss = 0;
                    winRate = 0;
                    expectancy = 0;
                    numOpen = 0;
                    numDays = 0;
                    numHours = 0;
                    numTrades = 0;
                    numOrders = 0;
                    orderExits = 0;
                    computeStats = 0;
                    sumPips = 0;
                    sumWins = 0;
                    sumLoss = 0;
                    avgWin2 = 0;
                    avgLoss2 = 0;
                    avgWinBars = 0;
                    avgLossBars = 0;
                    numEntries = 0;
                    numJISO = 0;
                }
            }
        }

        // encapsulated BarData Class
        // holds data needed for each chart Bar
        public class BarData
        {
            internal string symbol;
            internal Bar bar;
            internal DateTime BarTime;
            internal double open;
            internal double high;
            internal double low;
            internal double close;
            internal long volume;
            internal double bid;
            internal double ask;
            internal bool startOfSession;
            internal bool buySide;
            internal double typical;
            internal double change;
            internal double pctChange;
            internal double HEMA;
            internal double CCI;
            internal double smoothedK;
            internal double smoothedD;
            internal double slowedD;
            internal double spread;
            internal DayOfWeek wd;
            internal TimeSpan tod;

            /// <summary>
            /// EA BarData Item Class
            /// </summary>
            public BarData()
            {
                BarData_Init();
            }

            /// <summary>
            /// BarData object constructor with Bar data
            /// </summary>
            public BarData(Bar ibar, double iBid = 0, double iAsk = 0)
            {
                BarData_Init();
                bar = ibar;
                BarTime = ibar.BarTime;
                open = (double)ibar.Open;
                high = (double)ibar.High;
                low = (double)ibar.Low;
                close = (double)ibar.Close;
                volume = ibar.Volume;
                wd = BarTime.DayOfWeek;
                tod = BarTime.TimeOfDay;
                bid = iBid;
                ask = iAsk;
                if (bid * ask > 0)  // if bid and ask are both > 0
                    spread = ask - bid;
            }

            /// <summary>
            /// BarData initialization
            /// </summary>
            internal void BarData_Init()
            {
                symbol = "";
                BarTime = DateTime.MinValue;
                high = 0;
                low = 0;
                close = 0;
                volume = -1;
                bid = 0;
                ask = 0;
                startOfSession = true;
                buySide = true;
                typical = 0;
                change = 0;
                HEMA = 0;
                CCI = 0;
                smoothedK = 50;
                smoothedD = 50;
                slowedD = 50;
                pctChange = 0;
                spread = 0;
                wd = BarTime.DayOfWeek;
                tod = BarTime.TimeOfDay;
            }
        }
        #endregion

        // Monitor for Closed orders
        // Monitor for Start of Day and start of Hour
        // Simulate market actions
        // **Add evants and actions that you want to monitor or to simulate

        #region Strategy


        // Trade Strategy
        // ** Add your Trade Strategy here
        internal void Strategy()
        {

            try
            {
                if (!optimize) LogPrint("Strategy started.");
                if (sessionEnded)
                    return;
                DoStartSession();                   // if startSession: initialize data; closeAllTrades; cleanup
                total = GetTotalOrders();           // get list and count of current trades
                RemoveClosedOrders();               // remove any Closed orders from lists
                if (!OKtoEnterTrades())             // conditions not OK to Enter trades
                    return;
                double thePrice = GetThePrice(PriceType, ref s.dI.bar);
                double openPrice = s.dI.open;
                double closePrice = s.dI.close;
                Debug.WriteLine("price is " + thePrice);
                CheckExits(thePrice);
                total = GetTotalOrders();           // get list and count of current trades

                if (total <= 1)
                {
                    Bar theBar = s.dI.bar;
                    if (Quantity < 0.01 || CheckMaxSpread() || paused || s.stats.exceededDailyDrawdown || countBars < 5)
                        return;  // conditions not right to Enter trades
                    int ticket1 = 0;
                    var digits = GetDigits();
                    var points = GetPoints();
                    int sl = Stoploss * 10;  // Points
                    int tp = TakeProfit * 10;  // Points
                    candle = false;
                    if (openPrice < closePrice) candle = true; // True == Green Candle  False == Red Candle
                    //Debug.WriteLine("Hello...the price is " + thePrice + "the candle is " + candle);
                    if ( cci.prevposcci == false && cci.lessneg100 == true && candle == true)
                    {
                        LogPrint("CCI Strategy#1 Long - CCI is below 100 and rising: " + cci.value);
                        Debug.WriteLine("Enter long" + cci.value);
                        s.targetDir = 1;                // Open Market trade, Side = Buy
                        if (CheckRiskTooHigh(sl))
                            return;
                        ticket1 = CreateOrder(type: TradeType.Market, lotsize: 0.50, entryPrice: 0, stoploss: sl, takeprofit: tp);
                    }
                    else if (cci.prevposcci == true && cci.greaterplus100 == true && candle == false)
                    {
                        LogPrint("CCI Strategy#1 Short - is above 100 and falling: " + cci.value);
                        Debug.WriteLine("Enter short " + cci.value);
                        s.targetDir = -1;               // Open Market trade, Side = Sell
                        if (CheckRiskTooHigh(sl))
                            return;
                        CreateOrder(type: TradeType.Market, lotsize: 0.50, entryPrice: 0, stoploss: sl, takeprofit: tp);
                    }
                    
                }
            }
            catch (Exception e)
            {
                LogPrint(" Exception: " + e.Message);
                LogPrint(" Exception: " + e.StackTrace);
            }
        }

        public void Monitor()
        {
            int total = GetTotalOrders();
            // count trading numDays
            curTime = GetCurTime();
            if (curTime >= nextDay || initialized)
            {
                initialized = false;
                if (!optimize)
                    LogPrint(" Start of Day. curTime=" + curTime);
                nextDay = new DateTime(
                  curTime.Year,
                  curTime.Month,
                  curTime.Day,
                  0, 0, 0, 0) + TimeSpan.FromHours(24);
                OldTime3 = curTime;
                s.stats.numDays++;
                s.ordersThisDay = 0;
                s.tradesThisDay = 0;
                accountBalance = GetAccountBalance();
                var pctDd = 100 * s.stats.maxDailyDrawdown / accountBalance;  // update statistics
                if (s.stats.largestDailyDrawdown > pctDd)
                    s.stats.largestDailyDrawdown = pctDd;
                s.startingBalance = accountBalance;
                s.stats.maxDailyDrawdown = 0;
                s.stats.exceededDailyDrawdown = false;
                riskLimitReached = false;
            }
            // count trading numHours
            if (OldTime2 > curTime || curTime.Subtract(OldTime2) >= TimeSpan.FromHours(1))
            {
                if (!optimize)
                    LogPrint(" New Hour. ");
                s.stats.numHours++;
                OldTime2 = curTime;
                s.ordersThisHour = 0;
                s.tradesThisHour = 0;
                if (!simulate)
                    LogPrint(" Monitor still running. " + curTime.ToLongTimeString());
            }
            var drawdown = accountBalance - s.startingBalance;
            if (drawdown < s.stats.maxDailyDrawdown)
                s.stats.maxDailyDrawdown = drawdown;
            var pctDrawdown = 100 * s.stats.maxDailyDrawdown / s.startingBalance;
            if (Math.Abs(pctDrawdown) > 4.75)
            {
                if (!optimize)
                    LogPrint(" CreateOrder: exceededDailyDrawdown " + pctDrawdown.ToString("F2"));
                s.stats.exceededDailyDrawdown = true;
            }
            if (s.stats.exceededDailyDrawdown)
            {
                closeAllTrades(reason: 3);
                return; // wait until next day to trade
            }
            RemoveClosedOrders();
            var highest = GetCurBar().high;
            var lowest = GetCurBar().low;
            if (simulate)
            {
                // check buyOpenOrders for TP or SL
                if (s.buyOpenOrders.Count > 0)
                {
                    foreach (var order in s.buyOpenOrders.Values)
                    {
                        double tp = (double)order.TakeProfit;
                        double sl = (double)order.StopLoss;

                        TPflag = (tp > 0 && highest >= tp);
                        SLflag = (sl > 0 && lowest <= sl);
                        if (TPflag && SLflag)  // randomly reset one flag, if both are set
                        {
                            var nxtRnd = rnd.Next(1, 10);
                            var selectTP = (nxtRnd > 5);
                            if (selectTP)
                                SLflag = false;
                            else
                                TPflag = false;
                        }
                        if (TPflag)
                        {
                            if (!optimize)
                            {
                                LogPrint("**Buy Order TakeProfit. Ticket=" + order.Id);
                            }
                            ExitOpenTrade(reason: 1, order: order, price: tp + 1 * point);
                            //s.closedOrders.Add(order.Id, order);
                            if (simulate)
                                order.CloseType = CloseType.TakeProfit;
                        }
                        else if (SLflag)
                        {
                            if (!optimize)
                            {
                                LogPrint("!!Buy Order StopLoss. Ticket=" + order.Id);
                                LogPrint("sl=" + sl + " lowest=" + lowest);
                            }
                            ExitOpenTrade(reason: 2, order: order, price: sl - 1 * point);
                            //s.closedOrders.Add(order.Id, order);
                            if (simulate)
                                order.CloseType = CloseType.StopLoss;
                            if (order.Profit < 0)
                                ;
                        }
                    }
                }

                // check sellOpenOrders for TP or SL
                if (s.sellOpenOrders.Count > 0)
                {
                    foreach (var order in s.sellOpenOrders.Values)
                    {
                        double tp = (double)order.TakeProfit;
                        double sl = (double)order.StopLoss;
                        TPflag = (tp > 0 && lowest <= tp);
                        SLflag = (sl > 0 && highest >= sl);
                        if (TPflag && SLflag)  // randomly reset one flag
                        {
                            var selectTP = (rnd.Next(1, 10) > 5);
                            if (selectTP)
                                SLflag = false;
                            else
                                TPflag = false;
                        }
                        if (TPflag)
                        {
                            if (!optimize)
                                LogPrint("**Sell Order TakeProfit. Ticket=" + order.Id);
                            ExitOpenTrade(reason: 3, order: order, price: tp - 1 * point);
                            //s.closedOrders.Add(order.Id, order);
                            if (simulate)
                                order.CloseType = CloseType.TakeProfit;
                        }
                        if (SLflag)
                        {
                            if (!optimize)
                            {
                                LogPrint("!!Sell Order StopLoss. Ticket=" + order.Id);
                                LogPrint("sl=" + sl + " highest=" + highest);
                            }
                            ExitOpenTrade(reason: 4, order: order, price: sl + 1 * point);
                            //s.closedOrders.Add(order.Id, order);
                            if (simulate)
                                order.CloseType = CloseType.StopLoss;
                            if (order.Profit < 0)
                                ;
                        }
                    }
                }
                RemoveClosedOrders();
            }

            Dictionary<long, Order> openOrders = s.buyOpenOrders;
            openOrders = openOrders.Concat(s.sellOpenOrders).ToDictionary(x => x.Key, x => x.Value);
            // check closed recentOrders for Trailing StopLoss
            int count = openOrders.Count;
            if (count > 0)
            {
                var orderIDs = openOrders.Keys.ToList();
                if (false && !optimize && count > 1)
                {
                    LogPrint("#2 recentOrders.Count=" + count);
                    LogPrint("orderIDs.Count=" + orderIDs.Count);
                }
                foreach (int ticketID in orderIDs)
                {
                    Order order;
                    if (!openOrders.TryGetValue(ticketID, out order))
                    {
                        if (!optimize)
                            LogPrint("recentOrders not found. ticket=" + ticketID);
                        continue;
                    }
                    var tm = GetOrderCloseTime(ticketID);
                    if (false && !optimize && count > 1)
                    {
                        LogPrint("ticketID=" + ticketID + ".tm=" + tm.ToString());
                        LogPrint("orderIDs.Count=" + orderIDs.Count);
                    }

                    if (tm.DateTime.Year > 1980) // Closed
                    {
                        order.ClosePrice = (decimal)GetOrderClosePrice(ticketID);
                        if (!optimize)
                            LogPrint("Monitor: Order closed. ticket=" + ticketID + " ClosePrice=" + order.ClosePrice);
                        if (order.CloseType == CloseType.Unknown)
                        {
                            order.CloseType = CloseType.Market;
                            if (order.Side == TradeSide.Buy)
                            {
                                if ((double)order.ClosePrice >= (double)order.TakeProfit - 10 * point)
                                    order.CloseType = CloseType.TakeProfit;
                                else if ((double)order.ClosePrice <= (double)order.StopLoss + 10 * point)
                                    order.CloseType = CloseType.StopLoss;
                            }
                            else if (order.Side == TradeSide.Sell)
                            {
                                if ((double)order.ClosePrice <= (double)order.TakeProfit + 10 * point)
                                    order.CloseType = CloseType.TakeProfit;
                                else if ((double)order.ClosePrice >= (double)order.StopLoss - 10 * point)
                                    order.CloseType = CloseType.StopLoss;
                            }
                        }
                        if (!optimize)
                            LogPrint("Monitor: Order CloseType=" + order.CloseType.ToString()
                                + " ClosePrice=" + order.ClosePrice
                                + " TakeProfit=" + order.TakeProfit
                                + " StopLoss=" + order.StopLoss
                                );
                        if (!simulate && order.Quantity > 0)
                        {
                            if (!optimize)
                                LogPrint("Monitor: ticket=" + ticketID + " CloseType=" + order.CloseType.ToString());
                            TPflag = (order.CloseType == CloseType.TakeProfit);
                            SLflag = (order.CloseType == CloseType.StopLoss);
                        }
                        TradeClosed(order);
                        if (!optimize)
                        {
                            string msg = "";
                            if (TPflag)
                                msg += " TPflag=" + TPflag;
                            if (SLflag)
                                msg += " SLflag=" + SLflag;
                            LogPrint("Monitor: Closed order. ID=" + order.Id
                                + " Side=" + ((order.Side == OP_BUY) ? "Buy" : "Sell")
                                + msg
                                + " CloseTime=" + tm.ToString());
                        }
                    }
                    else // !closed
                    {
                        // update Trailing Stoploss
                        if (false && (double)order.Quantity > 0)
                        {
                            if (!paused && s.OKtoTrade)
                                CheckOrderModify((int)order.Id, 40, ref order);
                        }
                    }
                }
            }
            RemoveClosedOrders();
        }

        internal void CheckExits(double thePrice)   // Check order Exit conditions      
        {
            total = GetTotalOrders();               // get list and count of current trades
            if (total > 0)
            {
                /*if ((s.buyOpenOrders.Count > 0)    // Close orders if HMA changed direction
                    || (s.sellOpenOrders.Count > 0))
                {
                    if (!optimize)
                        LogPrint("CheckExits: CCI trend changed. isBelow=" + " isAbove=");
                    closeAllTrades(reason: 55);
                    total = GetTotalOrders();
                }*/
                RemoveClosedOrders();
            }
            return;
        }
        #endregion

        #region Utillity Routimes

        internal void LoadPipValues()
        {
            StreamReader sr = null;
            string path = "C://temp//Pip Value.csv";
            try
            {
                pipValues = new Dictionary<string, double>();
                sr = System.IO.File.OpenText(path);
                string[] seps = { "," };    // commas separted values
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();    // split input line into terms
                    string[] terms = line.Split(seps, StringSplitOptions.RemoveEmptyEntries);
                    if (terms.Length < 2 || terms[0].Length < 7)
                        continue;   // not correct format, get next line
                    double pipValue;
                    if (!double.TryParse(terms[1], out pipValue))  // try to parse pipvalue
                        continue;           // continue with next line if parse double failed.
                    if (pipValue <= 0)
                        continue;           // continue with next line if value is not valid
                    pipValues.Add(terms[0], pipValue);  // Add pipValue to pipValues Dictionalry, key=pair
                }
            }
            catch (Exception e)
            {
                LogPrint("LoadPipValues:" + e.Message);
                LogPrint("LoadPipValues:" + e.StackTrace);
                LogPrint("LoadPipValues: path=" + path);
            }
            if (sr != null)     // close streanreader if not null
                sr.Close();
        }

        internal void InitInds()
        {
            cci = new CCIobj(this, CCI_period);
            if (simulate)
                return;
            RefreshRates();
            Bar theBar;
            double thePrice = 0;
            var count = Math.Min(ChartBars.Count - 1, 200);
            for (int i = count; i > 0; i--)
            {
                theBar = ChartBars[i];
                thePrice = GetThePrice(PriceType, ref theBar);
                cci.Calc(thePrice);
            }
            LogPrint("InitInds: count=" + count
                + " thePrice=" + thePrice.ToString("F5")
                + " cci=" + cci.value.ToString("F5")
                );
        }

        internal bool CheckRiskTooHigh(double sl)
        {
            bool riskTooHigh = false;
            double pipvalue = 1.0;
            if (pipValues.Count > 0)
                pipValues.TryGetValue(symbol, out pipvalue);
            var risk = Quantity * sl * pipvalue;
            var freeMargin = accountFreeMargin;
            double dailyRiskPct = 100;
            double tradeRiskPct = 100;
            if (s.startingBalance > 0)
            {
                tradeRiskPct = 100 * risk / accountBalance; // risk per trade
                dailyRiskPct = 100 * (s.startingBalance - freeMargin + 1 * risk) / s.startingBalance;   // risk for 2 trades
            }
            if (tradeRiskPct > 1.8 || dailyRiskPct > 4.5)
                riskTooHigh = true;
            else if (freeMargin - 1 * risk < 50)
                riskTooHigh = true;
            if (!optimize && riskTooHigh)
                LogPrint("riskTooHigh. sl=" + sl + " tradeRiskPct=" + tradeRiskPct + " dailyRiskPct=" + dailyRiskPct);
            return riskTooHigh;
        }

        internal bool OKtoEnterTrades()
        {
            bool oKtoEnterTrades = true;
            CheckPaused();                      // do not trade if Friday afternoon
            if (paused || !s.OKtoTrade || s.tradesThisHour > 25 || s.tradesThisDay > 24 * 3)    // check conditions
            {
                if (!paused && s.OKtoTrade && !optimize)    // must be order limit
                    LogPrint("Strategy: trade limit. tradesThisHour=" + s.tradesThisHour + " tradesThisDay=" + s.tradesThisDay);
                oKtoEnterTrades = false;
            }
            else if (CheckRiskLimit())                           // do not Enter trades if max Risk Limit reached
                oKtoEnterTrades = false;
            return oKtoEnterTrades;
        }

        internal bool CheckMaxSpread()
        {
            bool spreadTooHigh = false;
            int spread = (int)Math.Round((GetAsk() - GetBid()) / point);
            spreadTooHigh = (spread > MaxSpread && MaxSpread == 0);
            return spreadTooHigh;
        }

        internal void DoStartSession()
        {
            if (startSession)  // clear previos data;
            {
                if (!optimize)
                {
                    var msg = (" startSession=" + startSession);
                    LogPrint(msg);
                }
                closeAllTrades(reason: 1);
            }
        }

        internal int GetOrderCntBefore(TradeSide side, DateTime before)
        {
            int count = 0;
            Dictionary<long, Order> orders = new Dictionary<long, Order>();
            switch (side)
            {
                case TradeSide.Buy:
                    orders = orders.Concat(s.buyOpenOrders).ToDictionary(x => x.Key, x => x.Value);
                    orders = orders.Concat(s.longLimitOrders).ToDictionary(x => x.Key, x => x.Value);
                    break;
                case TradeSide.Sell:
                    orders = orders.Concat(s.sellOpenOrders).ToDictionary(x => x.Key, x => x.Value);
                    orders = orders.Concat(s.shortLimitOrders).ToDictionary(x => x.Key, x => x.Value);
                    break;
            }
            if (orders.Values.Count > 0)
            {
                foreach (var order in orders.Values)
                {
                    if (before.Subtract(order.OpenDate) > new TimeSpan(0, 0, 0))
                        count++;
                }
            }
            return count;
        }

        internal Order FindMostRecentFill()
        {
            Order mostRecent = null;
            total = GetTotalOrders();
            DateTime latestFilled = DateTime.MinValue;
            //if (!optimize) LogPrint("FindMostRecentFill: Started. cnt=" + s.orders.Values.Count);
            foreach (var order in s.orders.Values)
            {
                //if (!optimize) LogPrint("FindMostRecentFill: order ID=" + order.Id + " fillDate=" + order.FillDate);
                if (order.FillDate == null)
                    continue;
                var side = order.Side;
                DateTime fillDate = (DateTime)order.FillDate;
                //if (!optimize) LogPrint("FindMostRecentFill: Filled order found. ID=" + order.Id + " fillDate=" + fillDate);
                if (fillDate.Subtract(latestFilled) > new TimeSpan(0, 0, 1))
                {
                    latestFilled = fillDate;
                    mostRecent = order;
                    //if (!optimize) LogPrint("FindMostRecentFill: Filled order most recent. ID=" + mostRecent.Id + " fillDate=" + fillDate);
                }
            }
            if (latestFilled.Equals(oldFilled))
                mostRecent = null;
            else if (mostRecent != null)
            {
                oldFilled = latestFilled;
                if (!optimize)
                    LogPrint("FindMostRecentFill: new Fill found. ID=" + mostRecent.Id);
            }
            return mostRecent;
        }

        internal bool CheckFreeMargin()
        {
            bool enough = true;
            total = GetTotalOrders();
            accountFreeMargin = ComputeFreeMargin();
            var risk = ComputeOrderRisk(40 * point, symbol, Quantity);
            var remaining = accountFreeMargin - risk;
            var dailyrisklimit = 0.04 * s.startingBalance;
            var dailyRisk = s.startingBalance - remaining;
            if (dailyRisk >= dailyrisklimit)
                enough = false;
            return enough;
        }

        internal bool CheckRiskLimit()
        {
            maxRiskReached = false;
            total = GetTotalOrders();
            if (total > 0)
            {
                accountFreeMargin = GetFreeMargin();
                var dailyrisklimit = 0.045 * s.startingBalance;
                var dailyRisk = s.startingBalance - accountFreeMargin;
                if (dailyRisk > dailyrisklimit)
                {
                    if (!optimize && !maxRiskReached)
                        LogPrint("Strategy: dailyrisklimit reached. dailyrisklimit=" + dailyrisklimit + " dailyRisk=" + dailyRisk + " total=" + total);
                    maxRiskReached = true;
                }
            }
            return maxRiskReached;
        }

        internal bool CheckNewRisk(Order order)
        {
            var side = order.Side;
            var type = order.Type;
            if (type == TradeType.Limit)
            {
                int cnt = (side == TradeSide.Buy) ? s.buyOpenOrders.Values.Count : s.sellOpenOrders.Values.Count;
                if (cnt < 1)
                    return false;
            }
            var dailyrisklimit = s.startingBalance - 0.04 * s.startingBalance;
            double price = (double)((order.OpenPrice > 0) ? order.OpenPrice : order.Price);
            var mult = (order.Side == TradeSide.Buy) ? 1 : -1;
            double slDist = mult * (price - (double)order.StopLoss);
            double risk = ComputeOrderRisk(slDist, order.Symbol, (double)order.Quantity);
            accountFreeMargin = GetFreeMargin();
            if (accountFreeMargin - risk <= dailyrisklimit)
            {
                if (!optimize)
                {
                    LogPrint("CheckNewRisk: dailyrisklimit reached. dailyrisklimit=" + dailyrisklimit);
                }
                return true;
            }
            return false;
        }

        internal void CheckSLtrigger()
        {
            GetTotalOrders();
            CheckOrders(ref s.buyOpenOrders);
            CheckOrders(ref s.sellOpenOrders);
        }

        internal void CheckOrders(ref Dictionary<long, Order> orders)
        {
            double dir = 0;
            if (orders.Count > 0)
            {
                foreach (var ordr in orders.Values)
                {
                    Order order = ordr;
                    if (order != null)
                    {
                        int ticket = (int)order.Id;
                        dir = (order.Side == TradeSide.Buy) ? 1 : -1;
                        if (ticket > 0 && dir != 0 && s != null && s.dI != null)
                        {
                            if (order.Comment.Contains(magicStr))
                            {
                                var dist = dir * (double)(order.OpenPrice - order.StopLoss);
                                if (dist > 0)   // SL not beyong BE
                                {
                                    double thePrice = GetThePrice(PriceType, ref s.dI.bar);
                                    order.Profit = (decimal)(dir * (thePrice - (double)order.OpenPrice));
                                    if ((double)order.Profit >= 0 * point * 10)
                                    {
                                        if (!optimize)
                                            LogPrint("CheckSLtrigger: Moving SL beyond BE. "
                                                + " order.Profit=" + order.Profit.ToString("F5")
                                                + " OpenPrice=" + order.OpenPrice
                                                );
                                        ModifyOrder(ticket, 0 * 10, ref order);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        internal void CheckPaused()
        {
            var LT = GetLocalCurTime();
            var prevPaused = paused;
            paused = ((LT.DayOfWeek == System.DayOfWeek.Friday && LT.TimeOfDay >= fridayPause));
            if (paused)
            {
                if (!optimize && !prevPaused)
                    LogPrint(" Trading Paused. Friday afternoon." + LT.ToString());
            }
            if (!paused)
            {
                if (!optimize && prevPaused)
                    LogPrint(" Trading Resumed. local time=" + LT.ToString());
            }
        }

        internal void CheckOrderModify(int ticket, double SLlimit, ref Order order)
        {
            ModifyOrder(ticket, SLlimit, ref order);
        }

        internal void RemoveClosedOrders()  // Remove Closed ordrs from Open Orders Lists
        {
            if (s.closedOrders.Count > 0)
            {
                foreach (var iD in s.closedOrders.Keys)
                {
                    if (s.buyOpenOrders.Count > 0)
                        s.buyOpenOrders.Remove(iD);
                    if (s.sellOpenOrders.Count > 0)
                        s.sellOpenOrders.Remove(iD);
                    if (s.longLimitOrders.Count > 0)
                        s.longLimitOrders.Remove(iD);
                    if (s.shortLimitOrders.Count > 0)
                        s.shortLimitOrders.Remove(iD);
                }
                s.closedOrders.Clear();
            }
        }

        // external interface for Simulation Statistics
        internal EA_State.Statistics GetStats()
        {
            EA_State.Statistics stats = s.stats;
            return stats;
        }

        internal struct MqlDateTime
        {
            internal int year;           // Year
            internal int mon;            // Month
            internal int day;            // Day
            internal int hour;           // Hour
            internal int min;            // Minutes
            internal int sec;            // Seconds
            internal int day_of_week;    // Day of week (0-Sunday, 1-Monday, ... ,6-Saturday)
            internal int day_of_year;    // Day number of the year (January 1st is assigned the number value of zero)
        };

        internal void TimeToStruct(
            DateTime dt,            // date and time
            ref MqlDateTime dt_struct      // structure for the adoption of values
        )
        {
            dt_struct.year = dt.Year;           // Year
            dt_struct.mon = dt.Month;            // Month
            dt_struct.day = dt.Day;            // Day
            dt_struct.hour = dt.Hour;           // Hour
            dt_struct.min = dt.Minute;            // Minutes
            dt_struct.sec = dt.Second;            // Seconds
            dt_struct.day_of_week = (int)dt.DayOfWeek;    // Day of week (0-Sunday, 1-Monday, ... ,6-Saturday)
            dt_struct.day_of_year = dt.DayOfYear;    // Day number of the year (January 1st is assigned the number value of zero)
        }

        int PeriodSeconds(TimeFrame period = TimeFrame.Unknown)     // chart period
        {
            int seconds = (period != TimeFrame.Unknown) ? (int)period * 60 : 1;
            return seconds;
        }

        bool IsOutOfSession()
        {
            MqlDateTime time0 = new MqlDateTime();
            var curTime = (datetime)GetCurTime();
            TimeToStruct(curTime, ref time0);
            int weekDay = time0.day_of_week;
            long timeFromMidnight = curTime % 86400;
            int periodLength = PeriodSeconds(timeFrame);
            bool skipTrade = false;

            if (weekDay == 0)
            {
                if (sessionIgnoreSunday) return true;
                int lastBarFix = sessionCloseAtSessionClose ? periodLength : 0;
                skipTrade = timeFromMidnight < sessionSundayOpen || timeFromMidnight + lastBarFix > sessionSundayClose;
            }
            else if (weekDay < 5)
            {
                int lastBarFix = sessionCloseAtSessionClose ? periodLength : 0;
                skipTrade = timeFromMidnight < sessionMondayThursdayOpen || timeFromMidnight + lastBarFix > sessionMondayThursdayClose;
            }
            else
            {
                int lastBarFix = sessionCloseAtFridayClose || sessionCloseAtSessionClose ? periodLength : 0;
                skipTrade = timeFromMidnight < sessionFridayOpen || timeFromMidnight + lastBarFix > sessionFridayClose;
            }
            return (skipTrade);
        }

        internal void ComputeStats(double dProfit, Order order)
        {
            if (order.FillDate == null)
                return;
            var qty = (double)order.Quantity;
            if (qty > 0)
            {
                s.stats.profitLoss += dProfit;  // update profitLoss
                simAccountBalance += dProfit;
                accountBalance += dProfit;
                s.stats.numContracts++;  // update numContracts
                if (dProfit >= 0)
                {
                    double count = (double)s.stats.sumWins;
                    s.stats.sumWins++;
                    s.stats.avgWin2 = (s.stats.avgWin2 * count + dProfit) / (double)s.stats.sumWins;
                }
                else // dProfit < 0
                {
                    double count = (double)s.stats.sumLoss;
                    s.stats.sumLoss++;
                    s.stats.avgLoss2 = (s.stats.avgLoss2 * count + dProfit) / (double)s.stats.sumLoss;
                }

                // calculate avgWin, avgLoss, numWins, numLoss, maxProfit, maxLoss, maxDrawdown
                if (dProfit >= 0)
                {
                    s.stats.avgWin = (s.stats.numWins * s.stats.avgWin + Math.Abs(dProfit)) / (s.stats.numWins + 1);
                    s.stats.numWins++;
                }
                else // (dProfit <= 0), Loss
                {
                    s.stats.avgLoss = (s.stats.numLoss * s.stats.avgLoss + Math.Abs(dProfit)) / (s.stats.numLoss + 1);
                    s.stats.numLoss++;
                    ;
                }
                if (dProfit > s.stats.maxProfit)
                    s.stats.maxProfit = dProfit;
                else if (dProfit < s.stats.maxLoss)
                    s.stats.maxLoss = dProfit;
                if (s.stats.profitLoss < s.stats.maxDrawdown) // compute maxDrawdown
                    s.stats.maxDrawdown = s.stats.profitLoss;
                s.stats.computeStats++; // count num Order statistics computed
            }
        }

        internal void WriteTradeLog(string msg, bool clear = false)
        {
            if (TLock == null)
                TLock = new object();
            lock (TLock)  // exclussive lock, one call access at a time
            {
                // Write msg to TradeLog, Delete and Create new file if clear=true
                var tf = timeFrame;
                var tfStr = (tf != TimeFrame.Unknown) ? tf.ToString() : "S1";
                var account = GetAccountNumber();
                string tradeLogFilename = this.Name + " TradeLog_" + symbol.Replace("/", "") + "_" + tfStr + "_" + account + ".csv";
                if (!System.IO.Directory.Exists(dataFileDir))
                    System.IO.Directory.CreateDirectory(dataFileDir);
                if (clear)
                    System.IO.File.Delete(dataFileDir + tradeLogFilename);
                var path = dataFileDir + tradeLogFilename;
                tradeLogFile = new System.IO.StreamWriter(path, append: true);
                FileInfo f = new FileInfo(path);
                var msgWritten = false;
                if (!msgWritten)  // outut without scores
                    tradeLogFile.WriteLine(msg);
                tradeLogFile.Close();
            }
        }

        /// <summary>
        /// LogPrint: print msg to Alveo Log or to Console
        /// </summary>
        /// <returns>void</returns>
        internal void LogPrint(string msg, bool doIt = false)
        {
            if (optimize && !doIt)
                return;
            var tf = timeFrame;
            var tfStr = tf.ToString();
            string msg2 = this.Name + " [" + symbol + " " + tfStr + "]: " + msg;
            if (!simulate)
                Print(msg2);
            else
                Console.WriteLine(msg2);
        }

        // if value != flag, then change flag = value and return true, otherwise return flase (not changed)
        bool DetectChanged(ref bool flag, bool value)
        {
            bool changed;
            changed = (value != flag);
            if (changed)
            {
                flag = value;
            }
            return changed;
        }

        #endregion

        #region  Alveo simulation routines
        // **routines to call Aveo functions or simulate Alveo functionality in Visual Studio
        // **ALL function cals to Alveo must be encapsulated and simulated to run in Visual Studio
        // emulate Alveo Bars
        internal int GetBars()
        {
            if (simulate)
                return simBars;
            else // Alveo
                return Bars;
        }

        // Get most recent Bar time
        public DateTime GetLocalCurTime()
        {
            if (simulate)
                return simLocalTime;
            else
                return DateTime.Now;
        }

        public DateTime GetCurTime()
        {
            if (simulate)
                return simTime;
            else
                return UtcTimeNow();
        }

        // interface to get current UTC time just in case Alveo has a problem with the function
        DateTime UtcTimeNow()
        {
            DateTime utcNow = DateTime.UtcNow;
            return utcNow;
        }

        // Get Chart currency Symbol
        internal string GetSymbol()
        {
            string symbol = simSymbol;
            if (!simulate)
            {
                symbol = Symbol();
            }
            return symbol;
        }

        // Get Account Balance
        public double GetAccountBalance()
        {
            if (!simulate)
            {
                double accountBalance = AccountBalance();
                return accountBalance;
            }
            return simAccountBalance;  //simulated AccountBalance
        }

        public double GetFreeMargin()
        {
            if (!simulate)
            {
                double freeMargin = AccountFreeMargin();
                return freeMargin;
            }
            simFreeMargin = ComputeFreeMargin();
            return simFreeMargin;  //simulated AccountBalance
        }

        internal bool CheckMaintPeriod()
        {
            bool maint = false;
            var LT = GetLocalCurTime();
            if (LT.TimeOfDay > dailyMaintStart && LT.TimeOfDay < dailyMaintEnd)
                maint = true;
            return maint;
        }

        // checked conditions to determine if it is OK to Trade
        internal bool CheckOKToTrade()
        {
            if (accountBalance > 0 && accountBalance < 50.00)
            {
                if (!optimize)
                    LogPrint(" balance is too low !! AccountBalance = " + accountBalance.ToString("F2"));
                if (!simulate)
                    Sleep(1000);
                sessionEnded = true;
                return false;
            }

            if (DetectChanged(ref riskLimitReached, (s.startingBalance >= 0) ? accountBalance / s.startingBalance < 0.951 : false))
            {
                if (riskLimitReached)
                {
                    if (!optimize)
                    {
                        string msg = (" Start: riskLimitReached, trading prevented. "
                        + " startingBalance =" + s.startingBalance.ToString("F2")
                        + " accountBalance =" + accountBalance.ToString("F2"));
                        LogPrint(msg);
                        LogPrint(msg);
                    }
                    sessionEnded = true;
                }
            }
            if (riskLimitReached)
            {
                closeAllTrades(reason: 5);
                if (!simulate)
                    Sleep(1000); // sleep 1 sec
                sessionEnded = true;
                return false;
            }

            if (DetectChanged(ref tradingclosed, CheckTradingClosed()))
            {
                if (tradingclosed)
                {
                    if (!optimize)
                    {
                        string msg = (" Start: trading closed. ");
                        LogPrint(msg);
                        LogPrint(msg);
                    }
                    closeAllTrades(reason: 6);
                    if (!simulate)
                        Sleep(1000); // sleep 1 sec
                    return false;
                }
            }
            if (tradingclosed)
            {
                sessionEnded = true;
                return false;
            }

            if (DetectChanged(ref maintPeriod, CheckMaintPeriod()))
            {
                if (maintPeriod)
                {
                    if (!optimize)
                    {
                        string msg = (" Start: maintPeriod. ");
                        LogPrint(msg);
                    }
                    //CloseAllTrades(reason: 7);
                    if (!simulate)
                        Sleep(1000); // sleep 1 sec
                    return false;
                }
            }
            if (maintPeriod)
                return false;

            if (!simulate)  // Alveo functions
            {
                if (DetectChanged(ref s.isConnected, IsEAConnected()))
                {
                    if (!s.isConnected)
                    {
                        if (!optimize)
                        {
                            string msg = ("not connected! ");
                            LogPrint(msg);
                        }
                        if (!simulate)
                            Sleep(1000); // sleep 1 sec
                        return false;  // not connected to Server
                    }
                    else // isConnected
                    {
                        if (!optimize)
                            LogPrint(" is now connected!");
                    }
                }
                if (!s.isConnected)
                {
                    sessionEnded = true;
                    return false;
                }

                if (DetectChanged(ref s.isTradeAllowed, IsTradeAllowed()))
                {
                    if (!s.isTradeAllowed)
                    {
                        if (!optimize)
                            LogPrint(" EA not allowed to Trade !");
                        //closeAllTrades(reason: 7);
                        if (!simulate)
                            Sleep(1000); // sleep 1 sec
                        return false; // EA not allowed to Trade
                    }
                    else // isTradeAllowed
                    {
                        if (!optimize)
                            LogPrint(" is now IsTradeAllowed!");
                    }
                }
                if (!s.isTradeAllowed)
                {
                    sessionEnded = true;
                    return false;
                }

                if (DetectChanged(ref s.Stopped, IsEaStopped))
                {
                    if (s.Stopped)
                    {
                        if (!optimize)
                            LogPrint(" EA stopped !");
                        //closeAllTrades(reason: 8);
                        if (!simulate)
                            Sleep(1000); // sleep 1 sec
                        return false; // EA Stopped
                    }
                    else
                    {
                        if (!optimize)
                            LogPrint(" EA resumed !");
                    }
                }

                if (IsEaStopped)
                {
                    //closeAllTrades(reason: 9);
                    if (!simulate)
                        Sleep(1000); // sleep 1 sec
                    sessionEnded = true;
                    return false;
                }
            }
            return true; //OKToTrade
        }

        // Get latest Chart Bar
        internal BarData GetCurBar()
        {
            BarData dI = null;
            if (simulate)
            {
                dI = curBar;  // data from Simulator
                var b = new Bar();
                b.BarTime = dI.BarTime;
                b.Open = (decimal)dI.open;
                b.High = (decimal)dI.high;
                b.Low = (decimal)dI.low;
                b.Close = (decimal)dI.close;
                b.Volume = dI.volume;
                dI.bar = b;
                dI.ask = dI.close + 5 * point;
                dI.bid = dI.close - 5 * point;
                dI.spread = dI.ask - dI.bid;
            }
            else
            {
                int nBars = iBars(null, 0); // this symbol, this timeframe
                if (nBars <= 0)
                {
                    if (!optimize)
                        LogPrint("GetCurBar: iBars <= 0 !! iBars=" + nBars);
                    return dI;  // returns null
                }
                int last = Math.Max(1, 0);
                Bar b = ChartBars[last]; // last closed bar
                dI = new BarData(b, Bid, Ask);
                dI.bar = b;
            }
            return dI;
        }

        public double GetThePrice(PriceTypes type, ref Bar b)
        {
            double price = -1;
            var open = (double)b.Open;
            var high = (double)b.High;
            var low = (double)b.Low;
            var close = (double)b.Close;
            switch (type)
            {
                case PriceTypes.PRICE_CLOSE:
                    price = close;
                    break;
                case PriceTypes.PRICE_OPEN:
                    price = open;
                    break;
                case PriceTypes.PRICE_HIGH:
                    price = high;
                    break;
                case PriceTypes.PRICE_LOW:
                    price = low;
                    break;
                case PriceTypes.PRICE_MEDIAN:
                    price = (high + low) / 2;
                    break;
                case PriceTypes.PRICE_TYPICAL:
                    price = (high + low + close) / 3;
                    break;
                case PriceTypes.PRICE_WEIGHTED:
                    price = (high + low + 2 * close) / 4;
                    break;
                case PriceTypes.PRICE_OHLC:
                    price = Math.Round((open + high + low + close) / 4, 5);
                    break;
                case PriceTypes.PRICE_P7:
                    price = Math.Round((low + high + 2 * open + 3 * close) / 7, 5);
                    break;
            }
            return price;
        }

        // Exit the specified Order at the specified price
        internal int ExitOpenTrade(int reason, Order order, double price = 0, DateTime? before = null)
        {
            if (order == null)
                return -1;
            if (before != null && ((DateTime)before).Subtract(order.OpenDate) <= new TimeSpan(0, 0, 0))
                return -1;
            var orderID = (int)order.Id;
            var clsTime = GetOrderCloseTime(orderID);
            if (clsTime.DateTime.Year > 1970)
                return -1;  // already closed
            if (simulate && order.CloseDate == DateTime.MinValue)  // CloseDate not yet set
            {
                order.ClosePrice = (decimal)((price > 0) ? price : curPrice);
                order.CloseDate = curTime;
                order.CloseType = (reason == 50) ? CloseType.Market
                    : (reason == 55) ? CloseType.TakeProfit
                    : (reason == 56) ? CloseType.StopLoss
                    : CloseType.Unknown;
                if (order.CloseType == CloseType.StopLoss)
                    ;
            }
            else if (!simulate)
            {
                var lots = (double)order.Quantity;
                if (lots > 0)
                {
                    if (!optimize)
                        LogPrint("Closd order=" + orderID.ToString() + " lots=" + lots.ToString());
                    OrderDelete(orderID);
                    OrderClose(orderID, lots, 0, 0);
                    if (!simulate)
                        Sleep(100);
                }
                else    // lots <= 0
                {
                    order.ClosePrice = (decimal)((price > 0) ? price : curPrice);
                    order.CloseDate = curTime;
                }
            }
            if (order.CloseDate == DateTime.MinValue)  // CloseDate not yet set
            {
                order.CloseDate = GetOrderCloseTime(orderID);
            }
            s.stats.orderExits++; // count orderExits
            var dProfit = TradeClosed(order); // compute Order statistics
            dProfit = (double)((order.Profit == decimal.MinValue) ? 0 : order.Profit); // compute Order statistics
            if (!simulate)
            {
                LogPrint("closeOpenTrade orderID=" + orderID.ToString() + "  reason=" + reason.ToString()
                    + " price=" + order.ClosePrice.ToString("F5")
                    + " stoploss=" + order.StopLoss.ToString()
                    + "\n");
                LogPrint(" profitLoss=" + s.stats.profitLoss.ToString()
                    + " numWins=" + s.stats.numWins.ToString()
                    + " numLoss=" + s.stats.numLoss.ToString()
                    + " maxDrawdown=" + s.stats.maxDrawdown.ToString()
                    );
                //JournalOnExit(reason, order, bclose: (double)s.dI.close, dI: s.dI, dProfit: dProfit);
            }
            if (!optimize)
            {
                string msg = ("ExitOpenTrade: order=" + orderID + " reason=" + reason + " qty=" + order.Quantity + " dProfit=" + dProfit + " balance=" + accountBalance);
                LogPrint(msg);
            }
            return orderID;
        }

        // Specified Order was Closed, compute statistics
        public double TradeClosed(Order order)
        {
            if (order == null)
                return 0;
            if (order.Type == TradeType.Stop)
                return 0;
            var orderID = (int)order.Id;
            double dProfit;
            dProfit = GetProfit(order);
            if (dProfit == double.MinValue)
                dProfit = 0;
            order.Profit = (decimal)dProfit;
            if (!simulate)
            {
                order.CloseDate = GetOrderCloseTime(orderID);
                order.ClosePrice = (decimal)GetOrderClosePrice(orderID, order);
            }
            if (!s.closedOrders.Keys.Contains(orderID))  // if orderID is not in closedOrders list
            {
                s.closedOrders.Add(orderID, order); // add Order to closedOrders list
                ComputeStats(dProfit, order); // compute statiustics
                if (!optimize)
                {
                    string fdStr = "null";
                    if (order.FillDate.HasValue)
                    {
                        DateTime fd = (DateTime)(order.FillDate);
                        fdStr = fd.ToString("ddd dd MMM yyyy HH:mm:ss");
                    }
                    order.Comment +=  // add closing data to order.Comment
                        "," + fdStr
                        + "," + order.CloseDate.ToString("dd MMM yyyy HH:mm:ss")
                        + "," + order.ClosePrice.ToString("F5")
                        + "," + dProfit.ToString("F2");
                    LogPrint("TradeClosed: orderID=" + orderID
                        + " side=" + order.Side
                        + " price=" + order.ClosePrice.ToString("F5")
                        + " qty=" + order.Quantity.ToString("F2"));
                        //+ " dProfit=" + dProfit.ToString("F5"));
                    if (firstLine)
                    {
                        WriteTradeLog("EA,Symbol,TF,Id,side,qty,OpenDate,OpenPrice,StopLoss," +
                            "TakeProfit, OpenDate,CloseDate,closePrice,profit,closeType," +
                            "balance", true);
                        firstLine = false;
                    }
                    WriteTradeLog(order.Comment + ", " + order.CloseType.ToString() + ","
                        + simAccountBalance.ToString("F2")); // write traded data from order.Comment to TradeLog file
                }
            }
            return dProfit;
        }

        /// <summary>
        /// IsEAConnected: calls Alveo IsConnected()
        /// </summary>
        /// <returns>bool IsConnected</returns>
        bool IsEAConnected()
        {
            if (!simulate)
                return IsConnected();
            else
                return true;
        }

        // Close All trades for the specified reason and optionally clear buyOpenOrders and sellOpenOrders lists
        internal void closeAllTrades(int reason, bool clear = true, int Side = (int)TradeSide.Unknown, DateTime? before = null)
        {
            if (!optimize)
            {
                string sideStr = (Side != (int)TradeSide.Unknown) ? ((TradeSide)Side).ToString() : "All";
                string msg = ("closeAllTrades: Side=" + sideStr + " before=" + before + " reason=" + reason + " curTime=" + curTime.ToShortDateString() + " " + curTime.ToLongTimeString());
                LogPrint(msg);
            }
            List<long> closedTrades = new List<long>();
            if (!simulate) // Alveo functions
            {
                int total = GetTotalOrders();
                if (!optimize)
                    LogPrint("closeAllTrades: count=" + total);
                foreach (var order in s.orders.Values)
                {
                    if (Side < 0 || (int)order.Side == Side)
                    {
                        var id = ExitOpenTrade(reason, order, curPrice, before);
                        if (id > 0)
                            closedTrades.Add(id);
                    }
                }
            }
            else // simulate functions
            {
                bool buySide = (Side == (int)TradeSide.Buy || Side == (int)TradeSide.Unknown);
                bool sellSide = (Side == (int)TradeSide.Sell || Side == (int)TradeSide.Unknown);
                {
                    if (buySide)
                    {
                        foreach (var order in s.buyOpenOrders.Values)
                        {
                            var id = ExitOpenTrade(reason, order, curPrice, before);
                            if (id > 0)
                                closedTrades.Add(id);
                        }
                        foreach (var order in s.longLimitOrders.Values)
                        {
                            var id = ExitOpenTrade(reason, order, curPrice, before);
                            if (id > 0)
                                closedTrades.Add(id);
                        }
                    }
                    if (sellSide)
                    {
                        foreach (var order in s.sellOpenOrders.Values)
                        {
                            var id = ExitOpenTrade(reason, order, curPrice, before);
                            if (id > 0)
                                closedTrades.Add(id);
                        }
                        foreach (var order in s.shortLimitOrders.Values)
                        {
                            var id = ExitOpenTrade(reason, order, curPrice, before);
                            if (id > 0)
                                closedTrades.Add(id);
                        }
                    }
                }
            }
            if (clear)
            {
                if (closedTrades.Count > 0)
                {
                    foreach (var id in closedTrades)
                    {
                        s.buyOpenOrders.Remove(id);
                        s.longLimitOrders.Remove(id);
                        s.sellOpenOrders.Remove(id);
                        s.shortLimitOrders.Remove(id);
                    }
                }
            }
            if (!simulate)
                Sleep(1000); // sleep 1 sec
            return;
        }

        // if Time to Exit, then return true; otherwise false
        internal bool CheckTradingClosed()
        {
            bool closed = false;
            var tm = GetCurTime();
            var lt = GetLocalCurTime();
            closed = ((sessionIgnoreSunday && tm.DayOfWeek == System.DayOfWeek.Sunday)
                || tm.DayOfWeek == System.DayOfWeek.Saturday
                || (lt.DayOfWeek == System.DayOfWeek.Friday && lt.TimeOfDay > fridayclose)
                || (tm.DayOfWeek == System.DayOfWeek.Sunday && tm.TimeOfDay < sundayOpen)
                );
            return closed;
        }

        internal bool CheckSessionStarted()
        {
            bool started = false;
            var tm = GetCurTime();
            var lt = GetLocalCurTime();
            started = sessionEnded && !(tm.DayOfWeek == System.DayOfWeek.Saturday
                || (lt.DayOfWeek == System.DayOfWeek.Friday && lt.TimeOfDay > fridayclose)
                || (tm.DayOfWeek == System.DayOfWeek.Sunday && tm.TimeOfDay < sundayOpen)
                );
            return started;
        }

        // convert Alveo int err to string errorDescription
        string ErrorDescription(int err)
        {
            string errorDescription = "err# " + err.ToString();
            switch (err)
            {
                case 0:
                    errorDescription += " ERR_NO_ERROR";
                    break;
                case 1:
                    errorDescription += " ERR_NO_RESULT";
                    break;
                case 2:
                    errorDescription += " ERR_COMMON_ERROR";
                    break;
                case 3:
                    errorDescription += " ERR_INVALID_TRADE_PARAMETERS";
                    break;
                case 4:
                    errorDescription += " ERR_SERVER_BUSY";
                    break;
                case 5:
                    errorDescription += " ERR_OLD_VERSION";
                    break;
                case 6:
                    errorDescription += " ERR_NO_CONNECTION";
                    break;
                case 7:
                    errorDescription += " ERR_NOT_ENOUGH_RIGHTS";
                    break;
                case 8:
                    errorDescription += " ERR_TOO_FREQUENT_REQUESTS";
                    break;
                case 9:
                    errorDescription += " ERR_MALFUNCTIONAL_TRADE";
                    break;
                case 64:
                    errorDescription += " ERR_ACCOUNT_DISABLED";
                    break;
                case 65:
                    errorDescription += " ERR_INVALID_ACCOUNT";
                    break;
                case 128:
                    errorDescription += " ERR_TRADE_TIMEOUT";
                    break;
                case 129:
                    errorDescription += " ERR_INVALID_PRICE";
                    break;
                case 130:
                    errorDescription += " ERR_INVALID_STOPS";
                    break;
                case 131:
                    errorDescription += " ERR_INVALID_TRADE_volume";
                    break;
                case 132:
                    errorDescription += " ERR_MARKET_closed";
                    break;
                case 133:
                    errorDescription += " ERR_TRADE_DISABLED";
                    break;
                case 134:
                    errorDescription += " ERR_NOT_ENOUGH_MONEY";
                    break;
                case 135:
                    errorDescription += " ERR_PRICE_CHANGED";
                    break;
                case 136:
                    errorDescription += " ERR_OFF_QUOTES";
                    break;
                case 137:
                    errorDescription += " ERR_BROKER_BUSY";
                    break;
                case 138:
                    errorDescription += " ERR_REQUOTE";
                    break;
                case 139:
                    errorDescription += " ERR_ORDER_LOCKED";
                    break;
                case 140:
                    errorDescription += " ERR_LONG_POSITIONS_ONLY_ALlowED";
                    break;
                case 141:
                    errorDescription += " ERR_TOO_MANY_REQUESTS";
                    break;
                case 145:
                    errorDescription += " ERR_TRADE_MODIFY_DENIED";
                    break;
                case 146:
                    errorDescription += " ERR_TRADE_CONTEXT_BUSY";
                    break;
                case 147:
                    errorDescription += " ERR_TRADE_EXPIRATION_DENIED";
                    break;
                case 148:
                    errorDescription += " ERR_TRADE_TOO_MANY_ORDERS";
                    break;
                case 149:
                    errorDescription += " ERR_TRADE_HEDGE_PROHIBITED";
                    break;
                case 150:
                    errorDescription += " ERR_TRADE_PROHIBITED_BY_FIFO";
                    break;
                case 4000:
                    errorDescription += " ERR_NO_MQLERROR";
                    break;
                case 4001:
                    errorDescription += " ERR_WRONG_FUNCTION_POINTER";
                    break;
                case 4002:
                    errorDescription += " ERR_ARRAY_INDEX_OUT_OF_RANGE";
                    break;
                case 4003:
                    errorDescription += " ERR_NO_MEMORY_FOR_CALL_STACK";
                    break;
                case 4013:
                    errorDescription += " ERR_ZERO_DIVIDE";
                    break;
                case 4014:
                    errorDescription += " ERR_UNKNOWN_COMMAND";
                    break;
                case 4015:
                    errorDescription += " ERR_WRONG_JUMP";
                    break;
                case 4016:
                    errorDescription += " ERR_NOT_INITIALIZED_ARRAY";
                    break;
                case 4017:
                    errorDescription += " ERR_DLL_CALLS_NOT_ALlowED";
                    break;
                case 4018:
                    errorDescription += " ERR_CANNOT_LOAD_LIBRARY";
                    break;
                case 4019:
                    errorDescription += " ERR_CANNOT_CALL_FUNCTION";
                    break;
                case 4020:
                    errorDescription += " ERR_EXTERNAL_CALLS_NOT_ALlowED";
                    break;
                case 4021:
                    errorDescription += " ERR_NO_MEMORY_FOR_RETURNED_STR";
                    break;
                case 4022:
                    errorDescription += " ERR_SYSTEM_BUSY";
                    break;
                case 4051:
                    errorDescription += " ERR_INVALID_FUNCTION_PARAMVALUE";
                    break;
                case 4062:
                    errorDescription += " ERR_STRING_PARAMETER_EXPECTED";
                    break;
                case 4063:
                    errorDescription += " ERR_INTEGER_PARAMETER_EXPECTED";
                    break;
                case 4064:
                    errorDescription += " ERR_DOUBLE_PARAMETER_EXPECTED";
                    break;
                case 4065:
                    errorDescription += " ERR_ARRAY_AS_PARAMETER_EXPECTED";
                    break;
                case 4066:
                    errorDescription += " ERR_HISTORY_WILL_UPDATED";
                    break;
                case 4067:
                    errorDescription += " ERR_TRADE_ERROR";
                    break;
                case 4105:
                    errorDescription += " ERR_NO_ORDER_SELECTED";
                    break;
                case 4106:
                    errorDescription += " ERR_UNKNOWN_SYMBOL";
                    break;
                case 4107:
                    errorDescription += " ERR_INVALID_PRICE_PARAM";
                    break;
                case 4108:
                    errorDescription += " ERR_INVALID_TICKET";
                    break;
                case 4109:
                    errorDescription += " ERR_TRADE_NOT_ALLOWED";
                    break;
                default:
                    errorDescription += " unknown_err_code " + err.ToString();
                    break;
            }
            return errorDescription;
        }

        // Get Total number of Open orders
        internal int GetTotalOrders(TradeType type = TradeType.Unknown)
        {
            int total = 0;
            try
            {
                if (!simulate) // Alveo
                {
                    total = 0;
                    //LogPrint("GetTotalOrders: count=" + (s.buyOpenOrders.Count + s.sellOpenOrders.Count));
                    s.buyOpenOrders.Clear();
                    s.sellOpenOrders.Clear();
                    s.longLimitOrders.Clear();
                    s.shortLimitOrders.Clear();
                    int count = OrdersTotal();
                    if (count == 0)
                    {
                        return total;
                    }
                    for (int pos = 0; pos < count; pos++)
                    {
                        if (OrderSelect(pos, SELECT_BY_POS) == false)
                            continue;
                        string cm = OrderComment();
                        if (cm == null || cm.Length < 1)
                            continue;
                        if (!cm.Contains(magicStr))
                        {
                            var ticket = OrderTicket();
                            //("GetTotalOrders: order does not conatain magicStr. ticket=" + ticket + " magicStr=" + magicStr);
                            continue;
                        }
                        DateTime ctm = OrderCloseTime();
                        if (ctm.Year > 1980)
                            continue;                           // closed
                        var order = new Order();
                        order.Id = OrderTicket();
                        if (order.Id < 0)
                            continue;
                        order.Type = ConvertTradeType();
                        if (type >= 0 && order.Type != type)
                            continue;
                        order.Symbol = symbol;
                        order.Side = (TradeSide)(OrderType() % 2);
                        order.Comment = OrderComment();
                        order.OpenPrice = (decimal)OrderOpenPrice();
                        order.StopLoss = (decimal)OrderStopLoss();
                        order.TakeProfit = (decimal)OrderTakeProfit();
                        order.OpenDate = OrderOpenTime();
                        order.CloseDate = ctm;
                        order.ClosePrice = 0;
                        order.Profit = (decimal)OrderProfit();
                        order.Quantity = (decimal)OrderLots();
                        order.CloseType = CloseType.Unknown;
                        order.Price = (decimal)OrderPendingPrice();
                        datetime dt = OrderFillTime();
                        order.FillDate = (dt == null) ? null : (DateTime?)dt;
                        switch (order.Side)
                        {
                            case TradeSide.Buy:
                                if (order.Type == TradeType.Limit && order.FillDate == null)
                                    s.longLimitOrders.Add(order.Id, order);
                                else
                                    s.buyOpenOrders.Add(order.Id, order);
                                break;
                            case TradeSide.Sell:
                                if (order.Type == TradeType.Limit && order.FillDate == null)
                                    s.shortLimitOrders.Add(order.Id, order);
                                else
                                    s.sellOpenOrders.Add(order.Id, order);
                                break;
                            default:
                                continue;   // unknown
                        }
                        total++;
                    }
                }
                else // simulate
                {
                    total = s.buyOpenOrders.Count + s.sellOpenOrders.Count + s.longLimitOrders.Count + s.shortLimitOrders.Count;
                }
                s.orders.Clear();
                s.orders = s.orders.Concat(s.buyOpenOrders).ToDictionary(x => x.Key, x => x.Value);
                s.orders = s.orders.Concat(s.sellOpenOrders).ToDictionary(x => x.Key, x => x.Value);
                s.orders = s.orders.Concat(s.longLimitOrders).ToDictionary(x => x.Key, x => x.Value);
                s.orders = s.orders.Concat(s.shortLimitOrders).ToDictionary(x => x.Key, x => x.Value);
                simFreeMargin = ComputeFreeMargin();
                accountFreeMargin = simFreeMargin;
            }
            catch (Exception e)
            {
                LogPrint("GetTotalOrders Exception: " + e.Message);
                LogPrint("GetTotalOrders Exception: " + e.StackTrace);
            }
            return total;
        }

        internal TradeType ConvertTradeType()
        {
            TradeType type = TradeType.Unknown;
            var orderType = OrderType();
            switch (orderType)
            {
                case OP_BUY:
                    type = TradeType.Market;
                    break;
                case OP_SELL:
                    type = TradeType.Market;
                    break;
                case OP_BUYLIMIT:
                    type = TradeType.Limit;
                    break;
                case OP_SELLLIMIT:
                    type = TradeType.Limit;
                    break;
                case OP_BUYSTOP:
                    type = TradeType.Stop;
                    break;
                case OP_SELLSTOP:
                    type = TradeType.Stop;
                    break;
                default:
                    type = TradeType.Unknown;
                    break;
            }
            return type;
        }

        internal double ComputeFreeMargin()
        {
            double accountbalance = GetAccountBalance();
            double accountFreeMargin = accountbalance;
            double risk;
            foreach (var order in s.orders.Values)
            {
                double price = (double)((order.OpenPrice > 0) ? order.OpenPrice : order.Price);
                var mult = (order.Side == TradeSide.Buy) ? 1 : -1;
                double slDist = mult * (price - (double)order.StopLoss);
                risk = ComputeOrderRisk(slDist, order.Symbol, (double)order.Quantity);
                accountFreeMargin -= risk;
            }
            return accountFreeMargin;
        }

        internal double ComputeOrderRisk(double slDist, string symbol, double qty)
        {
            double risk = 0;
            if (slDist <= 0 || symbol.Length < 7 || qty <= 0)
                return risk;
            var pips = slDist / point / 10;
            double pipValue = (symbol == "EUR/GBP") ? 1.31 : 1.0;
            risk = Math.Max(pipValue * pips * qty * 10, 0);
            return risk;
        }

        internal double GetProfit(Order order)
        {
            double profit = 0;
            if (order.Type == TradeType.Limit && order.FillDate == null)
                return 0;
            if (order.Quantity > 0)
            {
                double dPips = 0;
                int ticketID = (int)order.Id;
                if (!simulate)
                {
                    if (OrderSelect(ticketID, SELECT_BY_TICKET) == true)
                        profit = OrderProfit();
                    else
                        profit = 0;
                }
                else  // simulate
                {
                    var side = order.Side;
                    dPips = GetPriceDiff(order) * pipPos;
                    dPips *= (side == TradeSide.Buy) ? 1.0 : -1.0;
                    profit = (double)order.Quantity * dPips * 10;
                }
            }
            return profit;
        }

        // return OrderCloseTime if closed, else return datetime0
        internal datetime GetOrderCloseTime(int ticketID)
        {
            Order order = null;
            DateTime closeTime = DateTime.MinValue;
            if (!simulate)
            {
                if (OrderSelect(ticketID, SELECT_BY_TICKET, pool: MODE_HISTORY) == true)
                {
                    closeTime = OrderCloseTime();
                    if (!optimize && closeTime.Year > 1980)
                        LogPrint("GetOrderCloseTime: Closed. ticketID=" + ticketID + " closeTime=" + closeTime.ToString());
                }
                else
                    closeTime = datetime0;
            }
            else // simulate
            {
                try
                {
                    Dictionary<long, Order> openOrders = s.buyOpenOrders;
                    openOrders = openOrders.Concat(s.sellOpenOrders).ToDictionary(x => x.Key, x => x.Value);
                    if (openOrders.Count > 0)
                        if (openOrders.Keys.Contains(ticketID))
                            order = openOrders[ticketID];
                }
                catch (KeyNotFoundException) // order not in openOrders
                {
                    order = null;
                }
                if (order != null) // order found
                {
                    if (order.CloseDate > DateTime.MinValue)  // order.CloseDate not default value
                        closeTime = order.CloseDate;
                    else
                        closeTime = datetime0;  // not closed
                }
            }
            return closeTime;
        }

        internal double GetOrderClosePrice(int ticketID)
        {
            Order order = null;
            double closePrice = 0;
            if (!simulate)
            {
                if (OrderSelect(ticketID, SELECT_BY_TICKET, pool: MODE_HISTORY) == true)
                {
                    closePrice = OrderClosePrice();
                    if (!optimize && closePrice > 0)
                        LogPrint("GetOrderClosePrice: Closed. ticketID=" + ticketID + " closePrice=" + closePrice.ToString());
                }
                else
                    closePrice = 0;
            }
            else // simulate
            {
                try
                {
                    Dictionary<long, Order> openOrders = s.buyOpenOrders;
                    openOrders = openOrders.Concat(s.sellOpenOrders).ToDictionary(x => x.Key, x => x.Value);
                    if (openOrders.Count > 0)
                        if (openOrders.Keys.Contains(ticketID))
                            order = openOrders[ticketID];
                }
                catch (KeyNotFoundException) // order not in recentOrders
                {
                    order = null;
                }
                if (order != null) // order found
                {
                    if (order.ClosePrice > 0)
                        closePrice = (double)order.ClosePrice;
                    else
                        closePrice = curPrice;  // not closed
                }
            }
            return closePrice;
        }

        // return OrderClosePrice - OrderOpenPrice
        internal double GetPriceDiff(Order order)
        {
            double priceDiff = 0;
            if (!simulate) // Alveo functions
                priceDiff = OrderClosePrice() - OrderOpenPrice();
            else
                priceDiff = (double)(order.ClosePrice - order.OpenPrice);
            return priceDiff;
        }

        internal double GetBid()
        {
            var digits = GetDigits();
            double bid = 0;
            if (!simulate)
                bid = Bid;
            else
                bid = Math.Round(curPrice - 5 * point, digits);
            return bid;
        }

        internal double GetAsk()
        {
            var digits = GetDigits();
            double ask = 0;
            if (!simulate)
                ask = Ask;
            else
                ask = Math.Round(curPrice + 5 * point, digits);
            return ask;
        }

        // Open a new Market Order in the targetDir with orderSL and orderTP
        public int CreateOrder(double lotsize = 0.01, int stoploss = 0, int takeprofit = 0, TradeType type = TradeType.Market, double entryPrice = 0, string cmnt = null)
        {
            double price = 0;
            int cmd = -1;
            if (type == TradeType.Market)
                cmd = (s.targetDir == 1) ? OP_BUY : (s.targetDir == -1) ? OP_SELL : -1;
            else if (type == TradeType.Limit)
            {
                cmd = (s.targetDir == 1) ? OP_BUYLIMIT : (s.targetDir == -1) ? OP_SELLLIMIT : -1;
                price = entryPrice;
            }
            if (cmd < 0)
                return -1;
            string cmntStr = (cmnt == null) ? magicStr : magicStr + "," + cmnt + cci.value;
            int ticket = TryCreateOrder(cmd, lotsize, price: price, stoploss: stoploss, takeprofit: takeprofit, comment: cmntStr);
            if (!optimize)
            {
                var total2 = GetTotalOrders();
                var dt = GetCurTime();
                LogPrint("CreateOrder: total=" + total2 + " ticket=" + ticket + " dt=" + dt);
            }
            return ticket;
        }

        // try to create order with the specified parameters
        public int TryCreateOrder(int cmd, double orderQty, double price = 0, double stoploss = 0, double takeprofit = 0, string comment = "", int magic = 0, int exprTimeMin = 0, color clr = null)
        {
            int ticket = SendOrder(cmd, orderQty, price: price, stoploss: stoploss, takeprofit: takeprofit, comment: comment);
            if (!optimize)
            {
                var total2 = GetTotalOrders();
                LogPrint("TryCreateOrder: total=" + total2 + " ticket=" + ticket + " comment=" + comment);
            }
            if (ticket != 0)
            {
                s.ordersThisHour++;  // increment ordersThisHour count
                s.ordersThisDay++;
                if (cmd < 2)
                {
                    s.tradesThisHour++;
                    s.tradesThisDay++;
                }
            }
            else if (!optimize)
            {
                string msg = "TryCreateOrder: SendOrder failed." + " Side=" + ((cmd == 0) ? "Buy" : "Sell");
                LogPrint(msg);
            }
            s.stats.numOrders++;  // increment numTrades count
            return ticket;
        }

        // Get Normalize Digits from Alveo
        internal int GetDigits()
        {
            if (!simulate)
                return Digits;  // Alveo function
            else if (symbol.EndsWith("JPY"))
                return 3;
            return 5;
        }

        // Send OrderSend request to Alveo
        internal int SendOrder(int cmd, double volume, double price = 0, int slippage = 5, double stoploss = 0, double takeprofit = 0, string comment = "")
        {
            if (!optimize)
                LogPrint("SendOrder: Started. cmd=" + cmd);
            Order order = null;
            int ticket = 0;
            if (!simulate)  // Alveo functions
            {
                int err = 0;
                int cnt = 0;
                if (volume > 0)
                {
                    while (ticket <= 0 && err != 4109 && err != 132 && err != 133)
                    {
                        order = FormulateOrder(cmd, volume, stoploss, takeprofit, price, comment);
                        if (CheckNewRisk(order))
                            break;
                        Sleep(1100);
                        ticket = OrderSend(order.Symbol, cmd, (double)order.Quantity, (double)order.Price, slippage,
                            (double)order.StopLoss, (double)order.TakeProfit, expiration: order.ExpirationDate, comment: comment);
                        if (ticket <= 0) // Alveo returned err
                        {
                            ticket = 0; // invalid
                            err = GetLastError();
                            LogPrint("SendOrder: OrderSend failed !!");
                            LogPrint("Try OrderSend: cmd=" + cmd
                                + " vol=" + order.Quantity
                                + " price=" + order.Price
                                + " slippage=" + slippage
                                + " stoploss=" + order.StopLoss
                                + " takeprofit=" + order.TakeProfit
                                + " comment=" + order.Comment
                                );
                            cnt++;
                            if (cnt > 10)  // try up to 10 times before Exception
                            {
                                string msg = "SendOrder: OrderSend failed !! err=" + err.ToString();
                                LogPrint(msg);
                            }
                        }
                        else    // success
                        {
                            order.Id = ticket;
                            if (!optimize)
                            {
                                string msg = "SendOrder: OrderSend success. ticket=" + order.Id;
                                LogPrint(msg);
                            }
                        }
                        Sleep(1100); // Sleep 2.2 sec between OrderSends
                    }
                }
            }
            else // simulate, Simulator, not Alveo
            {
                order = FormulateOrder(cmd, volume, stoploss, takeprofit, price, comment);
                if (CheckNewRisk(order))
                    return ticket;
                ticket = ++ticketNum;  // update simulated ticketNum
                order.Id = ticket;
            }
            if (ticket != 0)  // ticket valid
            {
                string cmdStr = "unknown";
                switch (cmd)
                {
                    case OP_BUY:
                        cmdStr = "OP_BUY";
                        s.buyOpenOrders.Add(ticket, order);  // add to buyOpenOrders list
                        break;
                    case OP_SELL:
                        cmdStr = "OP_SELL";
                        s.sellOpenOrders.Add(ticket, order);  // add to sellOpenOrders list
                        break;
                    case OP_BUYLIMIT:
                        cmdStr = "OP_BUYLIMIT";
                        if (s.longLimitOrders.Keys.Contains(ticket))
                            s.longLimitOrders.Remove(ticket);
                        s.longLimitOrders.Add(ticket, order);  // add to buyOpenOrders list
                        break;
                    case OP_SELLLIMIT:
                        cmdStr = "OP_SELLLIMIT";
                        if (s.shortLimitOrders.Keys.Contains(ticket))
                            s.shortLimitOrders.Remove(ticket);
                        s.shortLimitOrders.Add(ticket, order);  // add to sellOpenOrders list
                        break;
                }
                if (!optimize) // save Order data in order.Comment and LogPrint info
                {
                    var total = GetTotalOrders();
                    LogPrint("SendOrder: total=" + total);
                    string msg = String.Format(",{0},{1},{2},{3},{4},{5},{6}",
                        order.Id.ToString(),
                        order.Side.ToString(),
                        order.Quantity.ToString(),
                        order.OpenDate.ToString("ddd dd MMM yyyy HH:mm:ss"),
                        order.OpenPrice.ToString("F5"),
                        order.StopLoss.ToString("F5"),
                        order.TakeProfit.ToString("F5")
                        );
                    order.Comment = ((comment.Length > 0) ? comment : magicStr + ", ") + msg;
                    LogPrint("SendOrder: Opened order=" + order.Id
                        + " cmd=" + cmdStr
                        + " Side=" + order.Side.ToString()
                        + " OpenDate=" + order.OpenDate
                        + " OpenPrice=" + order.OpenPrice.ToString("F5")
                        + " vol=" + order.Quantity.ToString("F2")
                        + " StopLoss=" + order.StopLoss.ToString("F5")
                        + " TakeProfit=" + order.TakeProfit.ToString("F5")
                        + " ExpirationDate=" + order.ExpirationDate
                        );
                }
            }
            return ticket;  // unique ticket number
        }

        internal Order FormulateOrder(int cmd, double qty, double stoploss, double takeprofit, double price = 0, string comment = "")
        {
            Order order = new Order();
            double orderSL = 0;
            double orderTP = 0;
            double orderQty = qty;
            var digits = GetDigits();  // Normalize parameters to digits decimal places for Alveo function
            switch (cmd % 2)
            {
                case OP_BUY:
                    price = (price > 0) ? price : GetAsk();
                    orderSL = (stoploss > 0) ? price - stoploss * point : 0;
                    orderTP = (takeprofit > 0) ? price + takeprofit * point : 0;
                    order.Side = TradeSide.Buy;
                    break;
                case OP_SELL:
                    price = (price > 0) ? price : GetBid();
                    orderSL = (stoploss > 0) ? price + stoploss * point : 0;
                    orderTP = (takeprofit > 0) ? price - takeprofit * point : 0;
                    order.Side = TradeSide.Sell;
                    break;
                default:
                    throw new Exception("FormulateOrder: unknow cmd. cmd=" + cmd.ToString());
            }
            price = NormalizeDouble(price, digits);
            orderSL = NormalizeDouble(orderSL, digits);
            orderTP = NormalizeDouble(orderTP, digits);
            orderQty = Math.Max(orderQty, 0);
            orderQty = NormalizeDouble(qty, 2);
            order.Id = 0;
            order.Symbol = symbol;
            order.Price = (decimal)price;
            order.Quantity = (decimal)orderQty;
            order.StopLoss = (decimal)orderSL;
            order.TakeProfit = (decimal)orderTP;
            order.CloseDate = DateTime.MinValue;
            order.ClosePrice = 0;
            order.Profit = decimal.MinValue;
            order.OpenDate = GetCurTime();
            order.OpenPrice = (decimal)price;
            order.CloseType = CloseType.Unknown;
            order.Type = (cmd < 2) ? TradeType.Market : TradeType.Limit;
            order.FillDate = (cmd < 2) ? order.OpenDate : (DateTime?)null;
            order.ExpirationDate = (cmd < 2 || TIF < 1) ? (DateTime?)null : order.OpenDate + new TimeSpan(TIF, 0, 0);
            string cmnt = comment;
            if (comment.Length == 0)
                cmnt = this.Name;
            order.Comment = cmnt;
            return order;
        }

        internal bool ModifyOrder(int ticket, double stoploss, ref Order order)
        {
            bool res = false;
            int err = 0;
            int cnt = 0;
            if (!simulate)  // Alveo functions
            {
                if (!OrderSelect(ticket, SELECT_BY_TICKET))
                    return false;
                DateTime dt = OrderCloseTime();
                if (dt.Year > 1970)
                    return false;  // already closed
                while (!res && err != 4109 && err != 132 && err != 133)
                {
                    var prc = OrderOpenPrice();
                    var prevSL = OrderStopLoss();
                    double mult = (OrderType() == OP_BUY) ? 1 : -1;
                    var orderSL = NormalizeDouble(prc - mult * stoploss * point, Digits);
                    double diff = Math.Abs(curPrice - orderSL);
                    if (diff > 10 * point)
                    {
                        var tp = OrderTakeProfit();
                        res = OrderModify(ticket, prc, orderSL, tp, 0);
                        if (!res) // Alveo returned err
                        {
                            err = GetLastError();
                            if (err == 4108)
                                LogPrint(" OrderModify failed !! err=" + err);
                            cnt++;
                            if (cnt > 10)  // try up to 10 times before Exception
                                throw new Exception("SendOrder: OrderModify failed !! err=" + err.ToString());
                        }
                        else // res = true
                        {
                            if (!optimize)
                            {
                                string msg = "ModifyOrder: ticket=" + ticket + " side=" + mult + " prc=" + prc + " SL moved from " + prevSL + " to " + orderSL;
                                diff = mult * (orderSL - prc);
                                if (diff > 0)
                                    msg += " Profitable!!";
                                LogPrint(msg);
                            }
                        }
                        if (!simulate)
                            Sleep(200); // Sleep 0.2 sec between OrderModify
                    }
                    else
                        return false;
                }
            }
            else // simulate
            {
                double mult = (order.Side == TradeSide.Buy) ? 1 : -1;
                var digits = GetDigits();
                var orderSL = NormalizeDouble((double)order.OpenPrice - mult * stoploss * point, digits);
                double diff = Math.Abs(curPrice - orderSL);
                if (diff > 10 * point)
                {
                    var prevSL = order.StopLoss;
                    if (!optimize)
                    {
                        string msg = "ModifyOrder: ticket=" + ticket + " side=" + mult + " SL moved from " + prevSL + " to " + orderSL;
                        diff = mult * (orderSL - (double)order.OpenPrice);
                        if (diff > 0)
                            msg += " Profitable!!";
                        LogPrint(msg);
                    }
                    order.StopLoss = (decimal)orderSL;
                }
            }
            return res;  // unique ticket number
        }

        // emulate Alveo Chart.TimeFrame
        internal TimeFrame GetTimeframe()
        {
            TimeFrame tf = TimeFrame.M5;
            if (!simulate)
                tf = Chart.TimeFrame;
            else
                tf = simTimeframe;
            return tf;
        }

        internal int GetOrderType(int orderID, ref Order order)
        {
            int type = -1;
            if (!simulate)
            {
                if (OrderSelect(orderID, SELECT_BY_TICKET) == true)
                {
                    type = OrderType();
                }
                else if (!optimize)
                    LogPrint("GetOrderType: OrderSelect() returned error - " + GetLastError());
            }
            else
            {
                return (int)order.Type;
            }
            return type;
        }

        internal double GetOrderClosePrice(int orderID, Order order)
        {
            double clsPrice = (double)order.ClosePrice;
            if (!simulate)
            {
                if (order.Quantity > 0) // !virtual order
                {
                    if (OrderSelect(orderID, SELECT_BY_TICKET) == true)
                    {
                        clsPrice = OrderClosePrice();
                    }
                    else if (!optimize)
                        LogPrint("GetOrderClosePrice: OrderSelect() returned error - " + GetLastError());
                }
            }
            return clsPrice;
        }

        internal double GetOrderOpenPrice(int orderID, Order order)
        {
            double openPrice = (double)order.OpenPrice;
            if (!simulate)
            {
                if (order.Quantity > 0)
                {
                    if (OrderSelect(orderID, SELECT_BY_TICKET) == true)
                    {
                        openPrice = OrderOpenPrice();
                    }
                    else if (!optimize)
                        LogPrint("GetOrderOpenPrice: OrderSelect() returned error - " + GetLastError());
                }
            }
            return openPrice;
        }

        internal double GetPoints()
        {
            double point = 0.00001;
            pipPos = symbol.EndsWith("JPY") ? 100 : 10000;
            if (!simulate)
            {
                point = Point;
            }
            else
            {
                point = 1 / pipPos / 10;
            }
            return point;
        }

        internal int GetAccountNumber()  // Get account number
        {
            int account = 0;                    // default value for simulation
            if (!simulate)                      // Alveo running
                account = AccountNumber();      // Alveo function
            return account;
        }

        internal int GetPeriod()
        {
            int period = 1;     // default
            if (!simulate)
                period = Period();
            else
                period = (int)simTimeframe;
            return period;
        }
        #endregion

        #region Indicator Classes
        // HEMA Class Object
        internal class HMAobj
        {
            internal WMAobj wma1;
            internal WMAobj wma2;
            internal WMAobj wma3;
            internal int Period;
            double Threshold;
            int sqrtPeriod;
            internal bool isRrising;
            internal bool isFalling;
            internal int trendDir;
            internal int prevTrendDir;
            internal bool trendChanged;
            internal double value;
            internal double prevValue;
            internal bool firstrun;
            internal double term3;
            GKAH_EA ea;

            // HMAobj constructor
            HMAobj()
            {
                Period = 25;
                Threshold = 0;
                sqrtPeriod = (int)Math.Round(Math.Sqrt(Period));
                isRrising = false;
                isFalling = false;
                trendDir = 0;
                prevTrendDir = 0;
                trendChanged = false;
                value = double.MinValue;
                prevValue = value;
            }

            // HMAobj constructor with input parameters
            internal HMAobj(GKAH_EA ea, int period, int threshold) : this()   // do HEMA() first
            {
                this.ea = ea;
                Period = period;
                Threshold = (double)threshold * 1e-6;
                sqrtPeriod = (int)Math.Round(Math.Sqrt((double)Period));
                wma1 = new WMAobj(period, 0);
                wma2 = new WMAobj((int)Math.Round(((double)period + 1) / 2), 0);
                wma3 = new WMAobj(sqrtPeriod, threshold);
                firstrun = true;
            }

            internal void Init(double thePrice)  // Initialize Indicator
            {
                wma1.Init(thePrice);
                wma2.Init(thePrice);
                wma3.Init(thePrice);
                value = wma3.value;
                prevValue = value;
                isRrising = false;
                isFalling = false;
                trendDir = 0;
                prevTrendDir = 0;
                trendChanged = false;
                firstrun = false;
                term3 = 0;
            }

            internal double Calc(double thePrice)  // Calculate Indicator values
            {
                // HMA(n) = WMA(2 * WMA(n / 2) – WMA(n)), sqrt(n))
                if (Period < 2)
                    throw new Exception("HMAcalc: period < 2, invalid !!");
                if (Threshold < 0)
                    throw new Exception("HMAcalc: Threshold < 0, invalid !!");
                if (firstrun)
                {
                    Init(thePrice);
                }
                prevTrendDir = trendDir;
                prevValue = value;
                wma1.Calc(thePrice);
                wma2.Calc(thePrice);
                term3 = 2 * wma2.value - wma1.value;
                value = wma3.Calc(term3);
                isRrising = wma3.isRrising;
                isFalling = wma3.isFalling;
                trendDir = isRrising ? 1 : (isFalling ? -1 : 0);
                trendChanged = (trendDir * prevTrendDir < 0);
                return value;
            }
        }

        internal class WMAobj
        {
            internal int Period;
            double Threshold;
            internal double lastPrice;
            internal double prevValue;
            internal double value;
            internal bool isRrising;
            internal bool isFalling;
            internal int trendDir;
            internal int prevTrendDir;
            internal bool trendChanged;
            internal Queue<double> Q;
            internal int cnt2;

            WMAobj()
            {
                Period = 1;
                Threshold = 0;
                value = 0;
                lastPrice = double.MinValue;
                Q = new Queue<double>();
            }

            internal WMAobj(int period, int threshold = 0) : this()
            {
                Period = period;
                Threshold = (double)threshold * 1e-6;
            }

            internal void Init(double price)
            {
                value = price;
                prevValue = value;
                Q.Clear();
            }

            internal double Calc(double thePrice)
            {
                prevValue = value;
                value = 0;
                lastPrice = thePrice;
                Q.Enqueue(thePrice);
                while (Q.Count > Period)
                    Q.Dequeue();
                var arr = Q.ToArray();
                cnt2 = arr.Length;
                double sum = 0;
                double mult = 0;
                for (int i = 0; i < cnt2; i++)
                {
                    mult += 1;
                    value += mult * arr[i];
                    sum += mult;
                }
                if (sum > 0)
                    value /= sum;
                else
                    value = double.MinValue;
                var diff = value - prevValue;
                isRrising = (diff > Threshold);
                isFalling = (diff < -Threshold);
                prevTrendDir = trendDir;
                trendDir = isRrising ? 1 : (isFalling ? -1 : 0);
                trendChanged = (trendDir * prevTrendDir < 0);
                return value;
            }
        }

        #endregion

        #region Indicator Classes
        /*
        CCI Class Object
        
        Commodity Channel Index (CCI)
        tp = (high + low + close) / 3
        cci = (tp - SMA(tp)) / (Factor * meanDeviation(tp))
        
         */

        internal class CCIobj
        {
            internal int Period;
            internal bool pluscci;
            internal bool negcci;
            internal bool lessneg100;  //current cci is less than -100
            internal bool prevlessneg100; // previous cci
            internal bool prevposcci; //previous cci is greather than current cci
            internal bool greaterplus100;  //current cci is greater than 100
            internal bool prevgreaterplus100;
            internal bool firstrun;
            internal double value;
            internal double prevValue;
            internal Queue<double> Q;
            GKAH_EA ea;

            // CCIobj constructor
            CCIobj()
            {
                Period = 7;
                firstrun = true;
                Q = new Queue<double>();
            }

            // CCIobj constructor with input parameters
            internal CCIobj(GKAH_EA ea, int period) : this()   // do CCI() first
            {
                this.ea = ea;
                Period = period;
                firstrun = true;
            }

            internal void Init(double thePrice)  // Initialize Indicator
            {
                lessneg100 = false;
                prevlessneg100 = false;
                prevposcci = false;
                pluscci = false;
                negcci = false;
                greaterplus100 = false;
                prevgreaterplus100 = false;
                firstrun = false;
                value = double.MinValue;

                Q.Clear();
            }

            internal double Calc(double thePrice)  // Calculate Indicator values
            {
                //CCI -> CCI[i] = (TP[i] - SMA7[i])/(0.015 * MD[i])
                if (Period < 7)
                    throw new Exception("CCI calc: period < 7, invalid !!");

                if (firstrun) Init(thePrice);

                Q.Enqueue(thePrice);
                if (Q.Count < Period) return 0;

                while (Q.Count > Period) Q.Dequeue();

                var arr = Q.ToArray();

                double meanVal = 0;
                for (int i = 0; i < Period; i++) meanVal += arr[i];
                meanVal /= Period;

                double md = 0;
                for (int i = 0; i < Period; i++)
                {
                    md += Math.Abs(arr[i] - meanVal);
                }
                md /= Period;

                if (md < 1e-5) md = 1e-5;

                prevValue = value;
                value = (thePrice - meanVal) / 0.015 / md;

                prevgreaterplus100 = greaterplus100;
                prevlessneg100 = lessneg100;

                lessneg100 = false;
                greaterplus100 = false;
                prevposcci = false;

                if (value > 100) greaterplus100 = true;
                
                if (value < -100) lessneg100 = true;
                
                if (prevValue > value) prevposcci = true;
                
                /*
                Debug.WriteLine(" ");
                Debug.WriteLine("***Break***");
                Debug.WriteLine("the price " + thePrice);
                Debug.WriteLine("CCI is " + value);
                Debug.WriteLine("CCI is above +100 " + greaterplus100);
                Debug.WriteLine("CCI is below -100 " + greaterneg100);
                Debug.WriteLine("Previous CCI is greater than CCI " + prevposcci);
                Debug.WriteLine("***Break***");
                */

                return value;
            }
        }

        #endregion

    }
}