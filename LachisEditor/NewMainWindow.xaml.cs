using System.Windows;
using System.Windows.Data;
using System.Xml;
using Microsoft.Win32;
using Syncfusion.Data.Extensions;
using ioPath = System.IO.Path;

namespace LachisEditor
{
    public partial class NewMainWindow
    {
        string _strInitialDirectoryFolder;
        bool _blnCodeIsRunning = false;
        readonly string _strRecentDatabase;

        public NewMainWindow()
        {

            
            InitializeComponent();

            this.SetLanguage(null);
            this.Menu_SetAvailableLanguages();
            // this.rcbOptionUseTranslatedFields.IsChecked = (bool)App.Current.Properties[Const.OptionsUseTranslatedFields];
            // this.cbtOptionUseTranslatedFields.IsChecked = (bool)App.Current.Properties[Const.OptionsUseTranslatedFields];
            // this.rcbOptionUseForeignKeyLookup.IsChecked = (bool)App.Current.Properties[Const.OptionsUseForeignKeyLookup];
            // this.cbtOptionUseForeignKeyLookup.IsChecked = (bool)App.Current.Properties[Const.OptionsUseForeignKeyLookup];
            // this.rcbOptionUseTeamFilter.IsChecked = (bool)App.Current.Properties[Const.OptionsUseTeamFilter];
            // this.cbtOptionUseTeamFilter.IsChecked = (bool)App.Current.Properties[Const.OptionsUseTeamFilter];
            //DBLoader.Databases_FillList(this.cboDBSelection, false);

        }

        private void SetLanguage(string strLanguage)
        {
            if (strLanguage != null)
                LanguageOptions.SelectedLanguage = strLanguage;
            ((XmlDataProvider)(this.FindResource("Lang"))).Document = LanguageOptions.XmlLanguage;
            
            //TODO: Adjust Control Labels by language
            //this.rcbOptionUseTranslatedFields.Caption = LanguageOptions.Text("MainWindow/ApplicationMenu/Options/UseTranslatedFields");
            //this.rcbOptionUseForeignKeyLookup.Caption = LanguageOptions.Text("MainWindow/ApplicationMenu/Options/UseForeignKeyLookup");
            //this.rcbOptionUseTeamFilter.Caption = LanguageOptions.Text("MainWindow/ApplicationMenu/Options/UseTeamFilter");
        }
        
        private void Menu_SetAvailableLanguages()
        {
            //TODO: Set Available Languages
            /*foreach (string s in LanguageOptions.AvailableLanguages)
            {
                RadioButtonTool rbt = new RadioButtonTool();
                rbt.Click += new RoutedEventHandler(this.rbtLanguage_OnClick);
                rbt.IsChecked = (s == LanguageOptions.SelectedLanguage);
                rbt.Caption = s;
                rbt.Tag = s;
                try { rbt.LargeImage = BitmapFrame.Create(new Uri(string.Concat(LanguageOptions.LanguageFolder, s, ".jpg"))); }
                catch { }
                this.rmtLanguage.Items.Add(rbt);
            }*/
        }
        
        void LoadDatabaseButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (this.SecurityCheckUnsavedData(true))
            {
                string strPath = "";
                OpenFileDialog dlg = new OpenFileDialog();
                dlg.Filter = "*.cdb (cyanide database)|*.cdb";
                if (!System.IO.Directory.Exists(_strInitialDirectoryFolder))
                    _strInitialDirectoryFolder = "";
                dlg.InitialDirectory = _strInitialDirectoryFolder;
                if ((bool) dlg.ShowDialog())
                    strPath = dlg.FileName;
                if (!string.IsNullOrEmpty(strPath))
                {
                    //if (ioPath.GetFileName(strPath).Contains(" "))
                    //   LanguageOptions.ShowMessage("MainWindow/Messages/FileNameInvalid", MessageBoxButton.OK);
                    //else
                    {
                        _strInitialDirectoryFolder = ioPath.GetDirectoryName(strPath);
                        this.StartLongJob("ImportDatabase", strPath);
                        _blnCodeIsRunning = true;
                        
                        //DBLoader.Databases_FillList(this.cboDBSelection, true);
                        //DBLoader.Tables_FillList(this.cboTableSelection);
                        FillCboTableSelection();

                        MainDataGrid.ItemsSource = DBLoader.GetDataView("DYN_cyclist", false);
                        _blnCodeIsRunning = false;
                    }
                }
            }
        }

        void FillCboTableSelection()
        {
            // Fill Table Selection ComboBox
            DBLoader.Tables_FillList(cboTableSelection);
        }

        bool SecurityCheckUnsavedData(bool blnCloseTable)
        {
            if (DBLoader.DataIsLoaded)
            {
                if (this.SaveTableBeforeClosing(blnCloseTable))
                {
                    switch (LanguageOptions.ShowMessage("MainWindow/Messages/SecurityCheckUnsavedData",
                        MessageBoxButton.YesNoCancel))
                    {
                        case MessageBoxResult.Yes:
                            DBLoader.SaveDatabase();
                            return true;
                        case MessageBoxResult.No:
                            DBLoader.CloseDatabase();
                            return true;
                        case MessageBoxResult.Cancel:
                            return false;
                    }
                }

                return false;
            }

            return true;
        }

        bool SaveTableBeforeClosing(bool blnCloseTable)
        {
            //TODO:  Implement

            return false;
            // if (this.MainFrame.Content != null)
            // {
            //     bool blnSaveOK = ((Pages.Table)this.MainFrame.Content).SaveBeforeClosing();
            //     if (blnCloseTable)
            //         this.MainFrame.Content = null;
            //     return blnSaveOK;
            // }
            // return true;
        }

        private void StartLongJob(string strJob, string strValue)
        {
            _blnCodeIsRunning = true;
            ProgressWindow w = new ProgressWindow();
            w.StartLongJob(strJob, strValue);
            _blnCodeIsRunning = false;
            w = null;
        }

        void SponsorsTableButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_blnCodeIsRunning) return;
            if (!DBLoader.DataIsLoaded)
            {
                //BUG: Here occurs an error if no data is loaded
                LanguageOptions.ShowMessage("MainWindow/Messages/NoDataLoaded", MessageBoxButton.OK);
                return;
            }
            
            MainDataGrid.ItemsSource = DBLoader.GetDataView("DYN_sponsor", false);
        }
    }
}