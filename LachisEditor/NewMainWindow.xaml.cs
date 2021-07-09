using System.Windows;
using Microsoft.Win32;
using Syncfusion.Data.Extensions;
using ioPath = System.IO.Path;

namespace LachisEditor
{
    public partial class NewMainWindow
    {
        string _strInitialDirectoryFolder;
        bool _blnCodeIsRunning = false;

        public NewMainWindow()
        {
            InitializeComponent();
        }

        void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            /*var ofd = new OpenFileDialog();
            var result = ofd.ShowDialog();
            if (result==true)
            {
                //MessageBox.Show(ofd.FileName);
                var dbLoader = new DBLoader();
                dbLoader.ProgressStart("ImportDatabase", ofd.FileName);
            }*/

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

                        MainDataGrid.ItemsSource = DBLoader.GetDataView("DYN_cyclist", false);
                        _blnCodeIsRunning = false;
                    }
                }
            }
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
    }
}