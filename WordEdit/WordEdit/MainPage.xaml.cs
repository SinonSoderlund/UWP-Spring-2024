using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.WindowManagement;
using Windows.UI.WindowManagement.Preview;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Storage.Pickers;
using Windows.UI.Xaml.Documents;
using Windows.Storage.Provider;
using Windows.Storage.Streams;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409
//Text editor by sinon söderlund
namespace WordEdit
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// 

    public sealed partial class MainPage : Page
    {

        private TextHelper texter;
        public MainPage()
        {
            this.InitializeComponent();
            texter = new TextHelper(ref TextField);
            texter.updateTitle();

        }

        //Takes events from program buttons and executes action corresponding to its editor name
        private async void button_Click(object sender, RoutedEventArgs e)
        {
            Button action = sender as Button;
            switch (action.Name)
            {
                default: break;
                case "NewText":
                    {
                        texter.FlushData();
                        break;
                    }
                case "Open":
                    {
                        await texter.openFile();
                        break;
                    }
                case "Save":
                    {
                        await texter.Save();
                        break;
                    }
                case "SaveAs":
                    {
                        await texter.SaveAs();
                        break;
                    }
                case "Close":
                    {
                        if(!await texter.wantSave())
                            break;
                        Application.Current.Exit();
                        break;
                    }
            }
            texter.updateTitle();
        }

        //alerts texthelper that the contents of the text editor text field has changed
        private void textBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            texter.TextSaveStateChanged();
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

        /// <summary>
        /// Overload that takes a param array of strings that are options that can be seleced, first entry is default option, final entry is default cancel  
        /// </summary>
        /// <param name="title">message title</param>
        /// <param name="content">message content</param>
        /// <param name="options">array of options</param>
        /// <returns>Returns the selected option</returns>
        public static async Task<IUICommand> displayMessageAsync(String title, String content, params string[] options)
        {
            // this should just result in optionless version of the function to fire, but yakno, better safe than sorry
            if (options.Length == 0)
                return null;
            var messageDialog = new MessageDialog(content, title);
            foreach ( var option in options ) 
            {
                messageDialog.Commands.Add(new UICommand(option));
            }
            messageDialog.DefaultCommandIndex = 0;
            messageDialog.CancelCommandIndex = (uint)options.Length-1;
            return await messageDialog.ShowAsync();
        }


        /// <summary>
        /// Manages the actual comings and going of saving and reading text files
        /// </summary>
        class TextHelper
        {
            private string name = "untitled", text = string.Empty;
            private bool isSaved = true, isFiled = false, cBlock = false;
            private StorageFile file = null;
            private FileSavePicker fileSavePicker;
            private FileOpenPicker fileOpenPicker;
            TextBox textBox;
            private ApplicationView appView;

            /// <summary>
            /// Texthelper constructor
            /// </summary>
            /// <param name="textfield">The text editor text field.</param>
            public TextHelper(ref TextBox textfield)
            {
                appView = ApplicationView.GetForCurrentView();
                //from https://learn.microsoft.com/en-us/windows/uwp/files/quickstart-save-a-file-with-a-picker oh god why isnt there just one file picker
                fileSavePicker = new FileSavePicker();
                fileSavePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                fileSavePicker.FileTypeChoices.Add("Plain Text", new List<string>() { ".txt" });
                fileSavePicker.SuggestedFileName = "New Document";
                fileOpenPicker = new FileOpenPicker();
                fileOpenPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                fileOpenPicker.FileTypeFilter.Clear();
                fileOpenPicker.FileTypeFilter.Add(".txt");
                textBox = textfield;
            }


            /// <summary>
            /// Assembles the program title, consisting of the name of the file and, if there are unsaved changed, an asterisk prefix
            /// </summary>
            /// <returns>title</returns>
            public string GetTitle()
            {
                return $"{(isSaved ? string.Empty : "*")}{name}";
            }

            /// <summary>
            /// If text was saved and function is not blocked, text is marked as changed and title is updated, otherwise if function is blocked, then future function calls are unblocked
            /// </summary>
            public void TextSaveStateChanged()
            {
                if (isSaved && !cBlock)
                {
                    isSaved = false;
                    updateTitle();
                }
                else if (cBlock)
                    cBlock = false;
            }
            /// <summary>
            /// Calls SaveAs if the file hasnt already been stored on drive, otherwise calls saveFile
            /// </summary>
            /// <returns>returns true unless action failed or was aborted by user</returns>
            public async Task<bool> Save()
            {
                if (!isFiled)
                {
                    if(!await SaveAs())
                        return false;
                }
                else if (!await saveFile())
                    return false;
                return true;
            }
            /// <summary>
            /// Calls wantSave if there are unsaves changes, then resets all document specific data
            /// </summary>
            public async void FlushData()
            {
                cBlock = true;
                if(!await wantSave())
                {
                    cBlock = false;
                    return;
                }
                textBox.Text = string.Empty;
                isFiled = false;
                isSaved = true;
                name = "untitled";
                text = string.Empty;
                file = null;
                updateTitle();
            }
            /// <summary>
            /// Queries user on if they want to save unsaved changes if there are any, calls save if yes
            /// </summary>
            /// <returns>returns true unless action fails or is aborted by user</returns>
            public async Task<bool> wantSave()
            {
                if (!isSaved)
                {
                    IUICommand result = await displayMessageAsync("Filen är inte sparad", "Ändringar kommer förloras om de inte sparas, vill du spara?", "Ja", "Nej", "Avbryt");
                    if (result.Label == "Ja")
                    {
                        if(!await Save())
                            return false;
                    }
                    else if (result.Label == "Avbryt")
                        return false;
                }
                return true;
            }

            /// <summary>
            /// Runs GetTitle and then sets window title to that title
            /// </summary>
            public void updateTitle()
            {
                appView.Title = GetTitle();
            }

            /// <summary>
            /// Lets user pick a file location to save to, then calls saveFile
            /// </summary>
            /// <returns>returns true unless action fails or is aborted by user.</returns>
            public async Task<bool> SaveAs()
            {
                file = await fileSavePicker.PickSaveFileAsync();
                if (file == null)
                {
                    return false;
                }
                name = file.Name;
                isFiled = true;
                if(!await saveFile())
                    return false;
                return true;
            }

            /// <summary>
            /// Saves the content of textbox to the file stored in 'file', unless it doesnt exist any more
            /// </summary>
            /// <returns>Returns true unless task failed</returns>
            private async Task<bool> saveFile()
            {
                //Code to detect if source file has been deleted from https://stackoverflow.com/questions/37992296/how-can-i-detect-if-a-storagefile-was-renamed-or-deleted-in-a-uwp-app
                if (isFiled)
                {
                    try
                    {
                        var stream = await file.OpenReadAsync();
                        stream.Dispose();
                    }
                    catch (FileNotFoundException e)
                    {
                        _ = displayMessageAsync("Kan inte spara", "Källfilen finns inte längre, vänligen spara igen genom spara som...");
                        isSaved = false;
                        isFiled = false;
                        return false;
                    }
                }
                text = textBox.Text;
                //from https://learn.microsoft.com/en-us/windows/uwp/files/quickstart-save-a-file-with-a-picker for use of filepickers and associated loading/saving
                CachedFileManager.DeferUpdates(file);
                 _ = FileIO.WriteTextAsync(file, text);
                FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);
                if (status == FileUpdateStatus.Complete)
                {
                    isSaved = true;
                }
                else
                {
                    return false;
                }
                updateTitle();
                return true;
            }
            /// <summary>
            /// Opens file via fileopenpicker, and updates texthelper status
            /// </summary>
            /// <returns>returns true unless action failed or was aborted by the user</returns>
            public async Task<bool> openFile()
            {
                if(!await wantSave()) return false;
                var tfile = await fileOpenPicker.PickSingleFileAsync();
                if (tfile == null)
                {
                    return false;
                }
                else
                    file = tfile;
                name = file.Name;
                isFiled = true;
                //from https://learn.microsoft.com/en-us/windows/uwp/files/quickstart-save-a-file-with-a-picker for use of filepickers and associated loading/saving
                CachedFileManager.DeferUpdates(file);
                text = await FileIO.ReadTextAsync(file);
                FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);
                if (status == FileUpdateStatus.Complete)
                {
                    isSaved = true;
                    cBlock = true;
                    textBox.Text = text;
                }
                else
                {
                    return false;
                }
                updateTitle();
                return true;
            }
        }
    }
}