using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Security.Cryptography.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using static System.Net.Mime.MediaTypeNames;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409


//Lottery drawing program by Sinon Söderlund, please disregard solution name, UWP was not being cooperative when i created it.
namespace please
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }
        /// <summary>
        /// Function for intercepting changes to the lottery number boxes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBox_TextChangedLotNum(object sender, TextChangedEventArgs e)
        {
            var txt = sender as TextBox;
            if (txt.Text.Length == 1)
            {            //code to fix caret input position from https://stackoverflow.com/questions/57533133/uwp-beforetextchanged-cursor-moving-in-front-of-text

                txt.SelectionStart = txt.Text.Length;
                txt.SelectionLength = 0;
            }
            else if(txt.Text.Length > 2)
            {
                //to stop people from being clever and inserting multiple zeros before their number
                txt.Undo();
                _ = displayMessageAsync("Warning", "Only 1 or 2 digit values between 1 and 35 are allowed");
            }
            //rangechecking input, if input is out of permissible range, undo input and display warning message
            int value;
            if (int.TryParse(txt.Text, out value))
            {
                if (value < 1 || value > 35)
                {
                    txt.Undo();
                    _ = displayMessageAsync("Warning", "Only values between 1 and 35 are allowed");
                }
            }
        }
        /// <summary>
        /// Function for intercepting changes to the lottery draw ammount number
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBox_TextChangedNumDraws(object sender, TextChangedEventArgs e)
        {
            var txt = sender as TextBox;
            if (txt.Text.Length == 1)
            {                //code to fix caret input position from https://stackoverflow.com/questions/57533133/uwp-beforetextchanged-cursor-moving-in-front-of-text

                txt.SelectionStart = txt.Text.Length;
                txt.SelectionLength = 0;
            }
            if (txt.Text.Length != 0 && !long.TryParse(txt.Text,out _))
            { //makes sure number of draws is a valid number unless its an empty string
                txt.Undo();
                _ = displayMessageAsync("Warning", "Invalid number of draws");
            }

        }
        /// <summary>
        /// ignore
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBlock_SelectionChanged(object sender, RoutedEventArgs e)
        {

        }
        /// <summary>
        /// function to intercept a lostfocus event from the lottery draws number box, toensure it contains valid input no matter what
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBox_LostFocusNumDraws(object sender, RoutedEventArgs e)
        {
            //ensures the number of draws is a valid number

            var txt = sender as TextBox;
            if (txt.Text == "0")
            {
                _ = displayMessageAsync("Warning", "0 is not a valid number of draws");
                txt.Text = "1";
            }
            else if (txt.Text.Length == 0)
            {
                txt.Text = "1";
            }
        }

        /// <summary>
        /// Portions of code to prevent non-numerical input taken from https://mzikmund.dev/blog/number-only-textbox-in-uwp
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void TextBox_OnBeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
        {
            if (args.NewText.Any(c => !char.IsDigit(c)))
            {
                args.Cancel = true;
                _ = displayMessageAsync("Warning", "Only numbers are allowed as input");
            }
        }



        /// <summary>
        /// code to show a simple pop-up message from https://stackoverflow.com/questions/38103634/how-do-i-popup-in-message-in-uwp
        /// </summary>
        /// <param name="title">message title</param>
        /// <param name="content">message content</param>
        /// <returns></returns>
        public async Task displayMessageAsync(String title, String content)
        {
            var messageDialog = new MessageDialog(content, title);
            messageDialog.CancelCommandIndex = 1;
            var cmdResult = await messageDialog.ShowAsync();
        }
        /// <summary>
        /// function that intercepts the call when the start lottery button is clicked, responsible for the execution of the main program.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_Click(object sender, RoutedEventArgs e)
        {
            //3 sets of data processing flows, ensures that data input si valid, and elsewise terminates execution
            string[] sLotNums = checkLotValidity();
            if (sLotNums == null)
                return;

            byte[] lotNums = populateLotNums(sLotNums);
            if (lotNums == null)
                return;

            long ulNumDraws = getNumDraws();
            if (ulNumDraws == 0)
                return;


            //converts input lottery numbers into a bitarray and then executes program
            BinHelper binLots = new BinHelper(lotNums);
            //long[] results= LotDrawing(binLots, ulNumDraws);
            long[] results = new long[3];
            Parallel.For(0L, (long)ulNumDraws, x => doLoopDraw(binLots, ref results));
            resultsSetter(results);
        }


        /// <summary>
        /// takes an array of results from the lottery drawing and updates the UI to display them
        /// </summary>
        /// <param name="result">lenght 3 array of results</param>
        private void resultsSetter(long[] result)
        {
            _5rat.Text = "5 rätt : " + result[0];
            _6rat.Text = "6 rätt : " + result[1];
            _7rat.Text = "7 rätt : " + result[2];
        }




        /// <summary>
        /// Draws the draws of the lotto numbers, returns and array with the number of 5,6 and 7 hit draws
        /// </summary>
        /// <param name="lotSet">the BinHelper "with" the lottto numbers</param>
        /// <param name="nrOfDraws">Number of draws that should be performed</param>
        /// <returns></returns>
        private long[] LotDrawing (BinHelper lotSet, long nrOfDraws)
        {
            long[] results = new long[3];
            Random random = new Random();
            BinHelper DupeCatcher = new BinHelper();
            for (long i = 0; i < nrOfDraws; i++) 
            {
                DupeCatcher.Reset();
                byte numberOfHits = 0; byte val = 0;

                for (byte lots = 0; lots < 7 ; lots++)
                {

                    do{val = (byte)random.Next(1, 36);}
                    while (DupeCatcher[val]);

                    DupeCatcher[val] = true;

                    if (lotSet[val])
                    {
                        numberOfHits++;
                    }
                }
                switch (numberOfHits)
                {
                    case 5:
                        results[0]++;break;
                    case 6:
                        results[1]++; break;
                    case 7:
                        results[2]++; break;
                    default: break;
                }
            }
            return results;
        }
        /// <summary>
        /// Somewhat uglier version of LotDrawing for use with Parallel.For
        /// </summary>
        /// <param name="lotSet">the BinHelper of the LotNums</param>
        /// <param name="results">refrence to the set of results</param>
        private void doLoopDraw(BinHelper lotSet, ref long[] results)
        {
            BinHelper DupeCatcher = new BinHelper();
            byte numberOfHits = 0; byte val = 0;

                for (byte lots = 0; lots < 7; lots++)
                {

                    do { val = (byte)RandomGen2.Next(1, 36); }
                    while (DupeCatcher[val]);

                    DupeCatcher[val] = true;

                    if (lotSet[val])
                    {
                        numberOfHits++;
                    }
                }
            if (numberOfHits >= 5)
            {
                numberOfHits -= 5;
                Interlocked.Increment(ref results[numberOfHits]);
            }
            DupeCatcher.Reset();
        }


        /// <summary>
        /// code for randomization in paralellized functions from https://devblogs.microsoft.com/pfxteam/getting-random-numbers-in-a-thread-safe-way/
        /// </summary>
        public static class RandomGen2
        {
            [ThreadStatic]
            private static Random _local;

            public static int Next(int minvval, int maxval)
            {
                Random inst = _local;
                if (inst == null)
                {

                    _local = inst = new Random();
                }
                return inst.Next(minvval,maxval);
            }
        }

        /// <summary>
        /// Gets the number of draws from the numDraws textbox
        /// </summary>
        /// <returns></returns>
        private long getNumDraws()
        {
            long result;
            if(long.TryParse(numDraws.Text, out result))
            {
                if(result != 0)
                    return result;

            }
            _ = displayMessageAsync("Warning", "Invalid number of draws");
            return 0;

        }

        /// <summary>
        /// collects values from lotto number fields and ensures partial validity.
        /// </summary>
        /// <returns></returns>
        private string[] checkLotValidity()
        {
            string[] lotFields = new string[7];
            lotFields[0] = lottoNumber1.Text;
            lotFields[1] = lottoNumber2.Text;
            lotFields[2] = lottoNumber3.Text;
            lotFields[3] = lottoNumber4.Text;
            lotFields[4] = lottoNumber5.Text;
            lotFields[5] = lottoNumber6.Text;
            lotFields[6] = lottoNumber7.Text;
            for(int i = 0; i < lotFields.Length; i++) 
            {
                if (lotFields[i].Length == 0)
                {
                    _ = displayMessageAsync("Warning", "All fields must be filled");
                    return null;
                }
            }
            return lotFields;
        }





        /// <summary>
        /// converts values to bytes and performs continues validity checking
        /// </summary>
        /// <param name="sLot">A string array containing the strings extracted and processed in checkLotValidity</param>
        /// <returns></returns>
        private byte[] populateLotNums(string[] sLot)
        {
            byte[] bLot = new byte[sLot.Length];
            for(int i = 0;i < sLot.Length;i++)
            {
                byte lVal;
                if (byte.TryParse(sLot[i], out lVal))
                {
                    if (lVal >= 1 && lVal <= 35)
                    {
                        bLot[i] = lVal;
                    }
                    else
                    {
                        _ = displayMessageAsync("Warning", "All fields be in the range between 1 and 35");
                        return null;
                    }
                }
                else
                {
                    _ = displayMessageAsync("Warning", "All fields must contain valid 1 or 2 digit numbers in the range between 1 and 35");
                    return null;
                }
            }
            for (int i = 0; i < bLot.Length; i++)
            {
                for (int j = i + 1; j < bLot.Length; j++)
                {
                    if (bLot[j] == bLot[i])
                    {
                        _ = displayMessageAsync("Warning", "All fields must be Unique");
                        return null;
                    }
                }
            }
            return bLot;
        }

        /// <summary>
        /// code taken from Microsoft Visual C# Step by Step, 10th Edition chapter 16, used as a faster aleternative to the array of lotto numbers
        /// </summary>
        private class BinHelper
        {
            private ulong oData = 0, cData = 0;
            /// <summary>
            /// Create a new BinHelper from another BinHelper 
            /// </summary>
            /// <param name="bH">the BinHelper to Be Copied</param>
            /// <param name="setOriginalDataTocData">if true, the original value of the new BinHelper will be set to the current value of bH.cValue, else it will be set to the value bH was initialized to</param>
            public BinHelper(BinHelper bH, bool setOriginalDataTocData)
            {
                this.cData = bH.cData;
                if(setOriginalDataTocData)
                    this.oData = bH.cData;
                else this.oData = bH.oData;
            }
            /// <summary>
            /// Intialize the values of BinHelper to a series of values in a byte array
            /// </summary>
            /// <param name="data">the set of values to be set</param>
            public BinHelper(byte[] data)
            {
                foreach (byte b in data)
                    this[b] = true;
                oData = cData;
            }

            /// <summary>
            /// Intilizes binhelper to a value of 0
            /// </summary>
            public BinHelper() 
            {
            }

            /// <summary>
            /// Resets the BinHelpers cData to the Binhelpers Initialization value
            /// </summary>
            public void Reset()
            {
                cData = oData;
            }
            //yes i am aware that the ranges are potentially problematic, however it is not an issue relevant to its use-context
            public bool this[byte i] 
            {
                get => (cData & (1UL << i)) != 0;

                set
                {
                    if (value) // turn the bit on if value is true; otherwise, turn it off 
                        cData |= (1UL << i);
                    else
                        cData &= ~(1UL << i);
                }

            } 

        }


    }
}