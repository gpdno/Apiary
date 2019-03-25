using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Alveo.UserCode;

namespace CuatomExpertAdvisor
{
    class Program
    {
        //const string pair = "EUR/USD";
        const string pair = "GBP/USD";
        //const string pair = "GBP/CHF";        //*
        //const string pair = "EUR/GBP";        //*
        //const string pair = "AUD/NZD";        //*
        //const string pair = "NZD/USD";
        //const string pair = "AUD/USD";
        //const string pair = "USD/JPY";
        //const string pair = "USD/CAD";
        //const string pair = "USD/CHF";
        static string simSymbol = pair;
        static string simSymbol2 = pair;
        static string version = "v1.0";
        static string strategy = "HMA_EA";
        //static Alveo.Common.Enums.TimeFrame timeframe = Alveo.Common.Enums.TimeFrame.Unknown;
        static Alveo.Common.Enums.TimeFrame timeframe = Alveo.Common.Enums.TimeFrame.M15;
        static HistoricalData.HistoricalData hist;

        static bool optimize = false;

        static string dataFileDir = "C:\\temp\\" + strategy + " " + pair.Replace('/', '-') + " " + timeframe.ToString() + " " + version + "\\";

        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine(strategy + " " + version + " - ConsoleApp " + pair);
                simSymbol2 = pair.Replace("/", "");
                // load Historical data
                hist = new HistoricalData.HistoricalData();
                hist.LoadDataFile(simSymbol2, timeframe.ToString());
                int indx = 0;
                var b = hist.dataItems[indx];
                // instantiate and configure EA for simulation
                var ea = new Alveo.UserCode.HMA_EA(optimize, hist.dataItems[0].BarTime, dataFileDir);
                ea.simSymbol = simSymbol;
                ea.simulate = true;
                ea.optimize = optimize;
                ea.simTimeframe = timeframe;
                ea.simAccountBalance = 2000;
                ea.pair = pair;
                ea.simBars = indx;
                ea.curBar = b;
                ea.simTime = b.BarTime;
                ea.simLocalTime = ea.simTime.ToLocalTime();
                ea.doInit();                                // Call EA's Init routine
                foreach (var di in hist.dataItems)          // Call EA's Start routine one Bar at a time
                {
                    b = hist.dataItems[indx++];
                    ea.simBars = indx;
                    ea.curBar = b;
                    ea.simTime = b.BarTime;
                    ea.simLocalTime = ea.simTime.ToLocalTime();
                    ea.doStart();
                }
                ea.closeAllTrades(reason: 99);              // Calose and Open trades
                var stats = ea.GetStats();                  // Gather statistics
                stats.winRate = (stats.numContracts > 0) ? (double)stats.numWins / (double)stats.numContracts : 0;
                stats.expectancy = stats.profitLoss / stats.numContracts;
                var fitness = (stats.numContracts > 0) ? stats.profitLoss : -1e10;
            }
            catch (Exception e)
            {
                Console.WriteLine("Test Program: returned Exception: " + e.Message);
                Console.WriteLine("Test Program: returned Exception: " + e.StackTrace);
            }
            return;
        }
    }
}
