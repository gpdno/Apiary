
using System;
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
    public class TemplateInd : IndicatorBase
    {
        #region Properties

        [Category("Settings")]
        public int param_1 { get; set; }

        #endregion

        public TemplateInd()
        {
            indicator_buffers = 0;
            //fooBar = 0;
            copyright = "";
            link = "";
            param_1 = 0;
        }

        //+-------- EXTERNAL PARAMETERS HERE --------+
        //[Category("My Category")]
        //[DisplayName("My Display Name")]
        //public int fooBar { get; set; }

        //+------------------------------------------------------------------+");
        //| Custom indicator initialization function                         |");
        //+------------------------------------------------------------------+");
        protected override int Init()
        {
            // ENTER YOUR CODE HERE
            return 0;
        }

        //+------------------------------------------------------------------+");
        //| Custom indicator deinitialization function                       |");
        //+------------------------------------------------------------------+");
        protected override int Deinit()
        {
            // ENTER YOUR CODE HERE
            return 0;
        }

        //+------------------------------------------------------------------+");
        //| Custom indicator iteration function                              |");
        //+------------------------------------------------------------------+");
        protected override int Start()
        {
            int counted_bars = IndicatorCounted();
            // ENTER YOUR CODE HERE
            return 0;
        }


        //+------------------------------------------------------------------+
        //| AUTO GENERATED CODE. THIS METHODS USED FOR INDICATOR CACHING     |
        //+------------------------------------------------------------------+
        #region Auto Generated Code

        [Description("Parameters order Symbol, TimeFrame, param_1")]
        public override bool IsSameParameters(params object[] values)
        {
            if (values.Length != 3)
                return false;

            if (!CompareString(Symbol, (string)values[0]))
                return false;

            if (TimeFrame != (int)values[1])
                return false;

            if (param_1 != (int)values[2])
                return false;

            return true;
        }

        [Description("Parameters order Symbol, TimeFrame, param_1")]
        public override void SetIndicatorParameters(params object[] values)
        {
            if (values.Length != 3)
                throw new ArgumentException("Invalid parameters number");

            Symbol = (string)values[0];
            TimeFrame = (int)values[1];
            param_1 = (int)values[2];

        }

        #endregion
    }
}

