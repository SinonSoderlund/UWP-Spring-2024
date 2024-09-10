using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409


//Calculator program by Sinon Söderlund
namespace Calculator
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        private Calculation calculon;
        public MainPage()
        {
            this.InitializeComponent();
        }
        //intercepts buttons clicks, scoops out the button lable, and directs it towards the calculator function it fits based on button tag, finally updates display
        private void button_Click(object sender, RoutedEventArgs e)
        {
            if(calculon == null) { calculon = new Calculation(); }
            Button cInp = sender as Button;
            string s = cInp.Content.ToString().ToLower();
            if(cInp.Tag.ToString() == "Numerical")
                button_ClickNumerical(s);
            else if (cInp.Tag.ToString() == "Symbol")
                button_ClickSymbol(s);
            updateDisplay();
        }
        /// <summary>
        /// buttonclick function for numbers, nulls calculon if input results in invalid number
        /// </summary>
        /// <param name="s">nummerical representation string</param>
        private void button_ClickNumerical(string s)
        {
            if (!calculon.AddNum(s))
            {
                calculon = null;
                _ = displayMessageAsync("Warning!", $"Invalid input, please ensure entered values are within the permitted value range of {int.MinValue}-{int.MaxValue}");
            }

        }

        /// <summary>
        /// Buttonclick function for symbols, nulls calculon if symbol C or an invalid command is passed
        /// </summary>
        /// <param name="s">symbol string</param>
        private void button_ClickSymbol(string s)
        {
            if (s == "c")
            {
                calculon = null;
                return;
            } 
            else if(!calculon.AddSym(s))
            {
                calculon = null;
                return;
            }
        }

        /// <summary>
        /// updates values in outputbox based on calculon data, unless there is no calculon, in which case output 0
        /// </summary>
        public void updateDisplay()
        {
            string d;
            if (calculon != null)
            {
                d = calculon.FetchDisplay();
            }
            else
                d = "0";
            OutputBox.Text = d;
        }


        /// <summary>
        /// Stateful calculation utility,implemented so to better function with the irratic flow of UI originated events
        /// </summary>
        private class Calculation
        {
            private int lhsI = 0, rhsI = 0;
            private string lhsS, Sym, rhsS;
            private enum State { iINIT, iLHS, iSYM, iRHS, iCALC};
            private State cState;
            public Calculation ()
            {
                cState = State.iINIT;
                lhsS = "0";
            }
            /// <summary>
            /// inserrts string s onto the current input string
            /// </summary>
            /// <param name="s">string to be added</param>
            /// <returns></returns>
            public bool AddNum(string s)
            {
                switch (cState) 
                {
                default: return false;

                case State.iINIT:
                        {
                            cState = State.iLHS;
                            lhsS = s;
                            return ValidityCheck(lhsS);
                        }
                case State.iLHS:
                        {
                            lhsS += s;
                            return ValidityCheck(lhsS);
                        }
                    case State.iSYM: 
                        {
                            cState = State.iRHS;
                            rhsS = s;
                            return ValidityCheck(rhsS);
                        }
                    case State.iRHS: 
                        {
                            rhsS += s;
                            return ValidityCheck(rhsS);

                        }
                    case State.iCALC:
                        {
                            cState = State.iLHS;
                            lhsS = s;
                            return ValidityCheck(lhsS);
                        }
                }
            }
            /// <summary>
            /// turns string s into the manipulation symbol sym
            /// </summary>
            /// <param name="s">string containing a valid manipulation symbol</param>
            /// <returns></returns>
            public bool AddSym(string s)
            {
                switch (s)
                {
                    default:return false;
                    case "+":
                    case "-":
                    case "*":
                    case "/":
                        {
                            switch (cState)
                            {
                                default: return false;

                                case State.iINIT:
                                    {
                                        _ = displayMessageAsync("Warning!", "Invalid input, please ensure youve entered left hand side values before supplying a manipulation symbol");
                                        return false;
                                    }
                                case State.iLHS:
                                    {
                                        cState = State.iSYM;
                                        Sym = s;
                                        return true;
                                    }
                                case State.iSYM:
                                    {
                                        Sym = s;
                                        return true;
                                    }
                                case State.iRHS:
                                    {
                                        if(!calcTime())
                                            return false;
                                        cState = State.iSYM;
                                        Sym = s;
                                        return true;
                                    }
                                case State.iCALC:
                                    {
                                        cState = State.iSYM;
                                        Sym = s;
                                        return true;
                                    }

                            }
                        }
                    case "=":
                        {
                            switch (cState)
                            {
                                default: return false;

                                case State.iINIT:
                                case State.iLHS:
                                case State.iSYM:
                                    {
                                        _ = displayMessageAsync("Warning!", "Invalid input, Ensure that Both sides of the calculation are filled in before requesting result");
                                        return true;
                                    }
                                case State.iRHS:
                                case State.iCALC:
                                    {
                                        cState = State.iCALC;
                                        return calcTime();
                                    }
                            }
                        }
                }
            }

            /// <summary>
            /// makes sure numerical representation strings actually represents a valid int
            /// </summary>
            /// <param name="s">string containing numerical representation</param>
            /// <returns></returns>
            public bool ValidityCheck(string s) 
            {
                if (int.TryParse(s,out _))
                {
                    return true;
                }
                return false;
            }

            /// <summary>
            /// Calculates a result out of the contents of calculation data, assuming that such data is valid.
            /// </summary>
            /// <returns></returns>
            private bool calcTime()
            {
                if (!int.TryParse(lhsS, out lhsI) || !int.TryParse(rhsS, out rhsI))
                {
                    _ = displayMessageAsync("Warning!", $"Invalid input, please ensure entered values are within the permitted value range of {int.MinValue}-{int.MaxValue}");
                    return false;
                }
                try
                {
                    checked
                    {
                        switch (Sym)
                        {
                            default: return false;
                            case "+":
                                {
                                    lhsI += rhsI; lhsS = lhsI.ToString(); return true;
                                }
                            case "-":
                                {
                                    lhsI -= rhsI; lhsS = lhsI.ToString(); return true;
                                }
                            case "*":
                                {
                                    lhsI *= rhsI; lhsS = lhsI.ToString(); return true;
                                }
                            case "/":
                                {
                                    if(rhsI == 0)
                                    {
                                        _ = displayMessageAsync("Warning!", "Illegal action, 0-divisions are not permitted");
                                        return false;
                                    }
                                    lhsI /= rhsI; lhsS = lhsI.ToString(); return true;
                                }
                        }
                    }
                }
                catch
                {
                    _ = displayMessageAsync("Warning!", $"Invalid input, please ensure entered values are within the permitted value range of {int.MinValue}-{int.MaxValue}");
                    return false;
                }
            }
            /// <summary>
            /// constructs a display string for the current calculation state
            /// </summary>
            /// <returns></returns>
            public string FetchDisplay()
            {
                switch (cState)
                {
                    default: return "0";

                    case State.iINIT:
                        {
                            return "0";
                        }
                    case State.iLHS:
                        {
                            return lhsS;
                        }
                    case State.iSYM:
                        {
                            return $"{lhsS} {Sym}";
                        }
                    case State.iRHS:
                        {
                            return $"{lhsS} {Sym} {rhsS}";
                        }
                    case State.iCALC:
                        {
                            return lhsS;
                        }

                }
            }
        }




        /// <summary>
        /// code to show a simple pop-up message from https://stackoverflow.com/questions/38103634/how-do-i-popup-in-message-in-uwp
        /// </summary>
        /// <param name="title">message title</param>
        /// <param name="content">message content</param>
        /// <returns></returns>
        public static async Task displayMessageAsync(String title, String content)
        {
            var messageDialog = new MessageDialog(content, title);
            messageDialog.CancelCommandIndex = 1;
            var cmdResult = await messageDialog.ShowAsync();
        }

        private void textBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
