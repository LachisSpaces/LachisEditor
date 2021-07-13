using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Data;
using LachisEditor.Annotations;
using LachisEditor.Models;
using Microsoft.Win32;
using Syncfusion.UI.Xaml.Grid;
using Syncfusion.Windows.Tools.Controls;
using ioPath = System.IO.Path;
using SelectionChangedEventArgs = System.Windows.Controls.SelectionChangedEventArgs;

namespace LachisEditor
{
    //TODO: Lookup Columns via: https://help.syncfusion.com/wpf/datagrid/column-types#gridmulticolumndropdownlist

    public sealed partial class NewMainWindow : INotifyPropertyChanged
    {
        #region Members

        string _strInitialDirectoryFolder;
        bool _blnCodeIsRunning;
        bool _useTeamFilter;

        public bool UseTeamFilter
        {
            get => _useTeamFilter;
            set
            {
                _useTeamFilter = value;
                OnPropertyChanged(nameof(UseTeamFilter));
            }
        }

        ObservableCollection<string> _existingTables;
        ObservableCollection<Team> _existingTeams;

        public ObservableCollection<string> ExistingTables
        {
            get => _existingTables;
            set
            {
                _existingTables = value;
                OnPropertyChanged(nameof(ExistingTables));
            }
        }

        public ObservableCollection<Team> ExistingTeams
        {
            get => _existingTeams;
            set
            {
                _existingTeams = value;
                OnPropertyChanged(nameof(ExistingTeams));
            }
        }

        #endregion

        #region Constructor and Initalization

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

            //Register Event Handlers
            CboTableSelection.SelectionChanged += CboTableSelection_OnSelectionChanged;
            MainDataGrid.ItemsSourceChanged += MainDataGridOnItemsSourceChanged;
            MasterDetailViewCheckbox.Checked += ToggleMasterDetailView;
            MasterDetailViewCheckbox.Unchecked += ToggleMasterDetailView;
        }

        #endregion

        #region Language Settings

        private void SetLanguage(string strLanguage)
        {
            if (strLanguage != null)
                LanguageOptions.SelectedLanguage = strLanguage;
            ((XmlDataProvider) (this.FindResource("Lang"))).Document = LanguageOptions.XmlLanguage;

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

        #endregion

        #region Saving Data

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

            return true;
            // if (this.MainFrame.Content != null)
            // {
            //     bool blnSaveOK = ((Pages.Table)this.MainFrame.Content).SaveBeforeClosing();
            //     if (blnCloseTable)
            //         this.MainFrame.Content = null;
            //     return blnSaveOK;
            // }
            // return true;
        }

        #endregion

        #region Job Handling

        private void StartLongJob(string strJob, string strValue)
        {
            _blnCodeIsRunning = true;
            ProgressWindow w = new ProgressWindow();
            w.StartLongJob(strJob, strValue);
            _blnCodeIsRunning = false;
            w = null;
        }

        #endregion

        #region Interface Implemantations

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Event Handlers

        #region Private Methods

        void ToggleMasterDetailView(object sender, RoutedEventArgs e)
        {
            //BUG: Deactivating Master Detail View does not work
            
            var tmpItemSource = MainDataGrid.ItemsSource;
            MainDataGrid.ItemsSource = null;
            MainDataGrid.AutoGenerateRelations = !MainDataGrid.AutoGenerateRelations;
            MainDataGrid.ItemsSource = tmpItemSource;
        }

        #endregion
        #region Button ClickHandlers

        void ImportDatabaseButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (this.SecurityCheckUnsavedData(true))
            {
                string path = "";
                OpenFileDialog dlg = new OpenFileDialog();
                dlg.Filter = "*.cdb (cyanide database)|*.cdb";
                if (!System.IO.Directory.Exists(_strInitialDirectoryFolder))
                    _strInitialDirectoryFolder = "";
                dlg.InitialDirectory = _strInitialDirectoryFolder;
                if ((bool) dlg.ShowDialog())
                    path = dlg.FileName;
                if (!string.IsNullOrEmpty(path))
                {
                    //if (ioPath.GetFileName(strPath).Contains(" "))
                    //   LanguageOptions.ShowMessage("MainWindow/Messages/FileNameInvalid", MessageBoxButton.OK);
                    //else
                    {
                        _strInitialDirectoryFolder = ioPath.GetDirectoryName(path);
                        this.StartLongJob("ImportDatabase", path);
                        _blnCodeIsRunning = true;

                        //DBLoader.Databases_FillList(this.cboDBSelection, true);
                        //DBLoader.Tables_FillList(this.cboTableSelection);
                        ExistingTables = new ObservableCollection<string>(DBLoader.GetExistingTableNames());
                        ExistingTeams = new ObservableCollection<Team>(DBLoader.GetExistingTeams());

                        // MainDataGrid.ItemsSource = DBLoader.GetDataView("DYN_cyclist", UseTeamFilter);
                        MainDataGrid.ItemsSource = DBLoader.DsCyanideDb.Tables["DYN_team"];
                        _blnCodeIsRunning = false;
                    }
                }
            }
        }

        void SaveDatabaseButton_OnClick(object sender, RoutedEventArgs e)
        {
            DBLoader.SaveDatabase();
        }

        private void ExportDatabaseButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!DBLoader.DataIsLoaded)
                LanguageOptions.ShowMessage("MainWindow/Messages/NoDataLoaded", MessageBoxButton.OK);
            else if (this.SecurityCheckUnsavedData(true))
            {
                string strPath = "";
                SaveFileDialog dlg = new SaveFileDialog();
                dlg.Filter = "*.cdb (cyanide database)|*.cdb";
                if (!System.IO.Directory.Exists(_strInitialDirectoryFolder))
                    _strInitialDirectoryFolder = "";
                dlg.InitialDirectory = _strInitialDirectoryFolder;
                if ((bool) dlg.ShowDialog())
                    strPath = dlg.FileName;
                if (!string.IsNullOrEmpty(strPath))
                {
                    if (ioPath.GetFileName(strPath).Contains(" "))
                        LanguageOptions.ShowMessage("MainWindow/Messages/FileNameInvalid", MessageBoxButton.OK);
                    else
                    {
                        _strInitialDirectoryFolder = ioPath.GetDirectoryName(strPath);
                        this.StartLongJob("ExportDatabase", strPath);
                    }
                }
            }
        }

        #region Filter Button Handlers

        //TODO: Using the Command-Pattern would be a better solution
        bool CanExecuteFilterCommand()
        {
            if (_blnCodeIsRunning) return false;
            if (!DBLoader.DataIsLoaded)
            {
                LanguageOptions.ShowMessage("MainWindow/Messages/NoDataLoaded", MessageBoxButton.OK);
                return false;
            }

            return true;
        }

        void TeamsTableButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!CanExecuteFilterCommand()) return;

            MainDataGrid.ItemsSource = DBLoader.DsCyanideDb.Tables["DYN_team"];
        }

        void SponsorsTableButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!CanExecuteFilterCommand()) return;

            MainDataGrid.ItemsSource = DBLoader.DsCyanideDb.Tables["DYN_sponsor"];
        }

        void RacesTableButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!CanExecuteFilterCommand()) return;

            MainDataGrid.ItemsSource = DBLoader.DsCyanideDb.Tables["STA_race"];
        }

        void SponsorGoalsTableButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!CanExecuteFilterCommand()) return;

            MainDataGrid.ItemsSource = DBLoader.DsCyanideDb.Tables["DYN_objectif"];
        }

        void CyclistsTableButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!CanExecuteFilterCommand()) return;

            MainDataGrid.ItemsSource = DBLoader.DsCyanideDb.Tables["DYN_cyclist"];
        }

        void StagesTableButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!CanExecuteFilterCommand()) return;

            MainDataGrid.ItemsSource = DBLoader.DsCyanideDb.Tables["STA_stage"];
        }

        void CboTableSelection_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!CanExecuteFilterCommand()) return;

            var tableSelectionComboBox = sender as RibbonComboBox;
            if (tableSelectionComboBox == null) return;
            if (string.IsNullOrEmpty(tableSelectionComboBox.SelectedValue.ToString())) return;
            
            MainDataGrid.ItemsSource =
                DBLoader.DsCyanideDb.Tables[tableSelectionComboBox.SelectedValue.ToString()];
        }

        void MainDataGridOnItemsSourceChanged(object sender, GridItemsSourceChangedEventArgs e)
        {
            CboTableSelection.SelectionChanged -= CboTableSelection_OnSelectionChanged;
            CboTableSelection.SelectedValue = (MainDataGrid.ItemsSource as DBTable)?.TableName;
            CboTableSelection.SelectionChanged += CboTableSelection_OnSelectionChanged;
            
            //Fill MassEditing Select Column ComboBox
            var columns = DBLoader.GetColumnNames(MainDataGrid.ItemsSource.ToString());
            MassEditColumnSelectionComboBox.ItemsSource = columns;
        }

        #endregion

        #endregion

        #endregion


    }
}