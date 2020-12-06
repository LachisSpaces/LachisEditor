using Infragistics.Windows.DataPresenter;
using System.Windows.Media.Imaging;
using Infragistics.Windows.Ribbon;
using ioPath = System.IO.Path;
using System.Windows.Controls;
using System.ComponentModel;
using System.Windows.Data;
using Microsoft.Win32;
using System.Windows;
using System.Xml;
using System;

namespace LachisEditor
{
   public partial class MainWindow : Window
   {
      private bool _blnCodeIsRunning = false;
      private enum NavigateType { Normal = 0, OnOpen = 1 };
      private string _strInitialDirectoryFolder;
      private string _strRecentDatabase;

      public MainWindow()
      {
         DBLoader.ApplicationPath = ioPath.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
         XmlDocument xdocOptions = new XmlDocument();
         xdocOptions.Load(DBLoader.ApplicationPath + Const.ApplicationOptionFile);
         XmlNode xRoot = xdocOptions.SelectSingleNode(string.Concat("/", Const.TopNode));
         _strRecentDatabase = xRoot.SelectSingleNode(Const.OptionsRecentDatabase).InnerText;
         _strInitialDirectoryFolder = xRoot.SelectSingleNode(Const.OptionsFolderSaveGames).InnerText;
         App.Current.Properties[Const.OptionsUseTeamFilter] = xRoot.SelectSingleNode(Const.OptionsUseTeamFilter).InnerText == "1";
         App.Current.Properties[Const.OptionsUseTranslatedFields] = xRoot.SelectSingleNode(Const.OptionsUseTranslatedFields).InnerText == "1";
         App.Current.Properties[Const.OptionsUseForeignKeyLookup] = xRoot.SelectSingleNode(Const.OptionsUseForeignKeyLookup).InnerText == "1";
         App.Current.Properties[Const.TableFilterColumn] = "";
         App.Current.Properties[Const.TableFilterValue] = "";
         App.Current.Properties[Const.TableFilterType] = "";
         App.Current.Properties[Const.SelectedTable] = "";
         InitializeComponent();
         this.SetLanguage(null);
         this.Menu_SetAvailableLanguages();
         this.rcbOptionUseTranslatedFields.IsChecked = (bool)App.Current.Properties[Const.OptionsUseTranslatedFields];
         this.cbtOptionUseTranslatedFields.IsChecked = (bool)App.Current.Properties[Const.OptionsUseTranslatedFields];
         this.rcbOptionUseForeignKeyLookup.IsChecked = (bool)App.Current.Properties[Const.OptionsUseForeignKeyLookup];
         this.cbtOptionUseForeignKeyLookup.IsChecked = (bool)App.Current.Properties[Const.OptionsUseForeignKeyLookup];
         this.rcbOptionUseTeamFilter.IsChecked = (bool)App.Current.Properties[Const.OptionsUseTeamFilter];
         this.cbtOptionUseTeamFilter.IsChecked = (bool)App.Current.Properties[Const.OptionsUseTeamFilter];
         DBLoader.Databases_FillList(this.cboDBSelection, false);
      }

      private void Window_Closing(object sender, CancelEventArgs e)
      {
         if (this.SecurityCheckUnsavedData())
         {
            // Generelle Einstellungen speichern
            XmlDocument xdocOptions = new XmlDocument();
            xdocOptions.Load(DBLoader.ApplicationPath + Const.ApplicationOptionFile);
            XmlNode xRoot = xdocOptions.SelectSingleNode(string.Concat("/", Const.TopNode));
            xRoot.SelectSingleNode(Const.OptionsFolderSaveGames).InnerText = _strInitialDirectoryFolder;
            xRoot.SelectSingleNode(Const.OptionsUseTeamFilter).InnerText = (bool)App.Current.Properties[Const.OptionsUseTeamFilter] ? "1" : "0";
            xRoot.SelectSingleNode(Const.OptionsUseTranslatedFields).InnerText = (bool)App.Current.Properties[Const.OptionsUseTranslatedFields] ? "1" : "0";
            xRoot.SelectSingleNode(Const.OptionsUseForeignKeyLookup).InnerText = (bool)App.Current.Properties[Const.OptionsUseForeignKeyLookup] ? "1" : "0";
            xRoot.SelectSingleNode(Const.OptionsRecentDatabase).InnerText = DBLoader.LoadedDatabaseName;
            xdocOptions.Save(DBLoader.ApplicationPath + Const.ApplicationOptionFile);
         }
         else
            e.Cancel = true;
      }


      private void rbtDBImport_Click(object sender, RoutedEventArgs e)
      {
         if (this.SecurityCheckUnsavedData(true))
         {
            string strPath = "";
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "*.cdb (cyanide database)|*.cdb";
            if (!System.IO.Directory.Exists(_strInitialDirectoryFolder))
               _strInitialDirectoryFolder = "";
            dlg.InitialDirectory = _strInitialDirectoryFolder;
            if ((bool)dlg.ShowDialog())
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
                  DBLoader.Databases_FillList(this.cboDBSelection, true);
                  DBLoader.Tables_FillList(this.cboTableSelection);
                  _blnCodeIsRunning = false;
               }
            }
         }
      }

      private void rbtDBExport_Click(object sender, RoutedEventArgs e)
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
            if ((bool)dlg.ShowDialog())
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

      private void rbtDBOpen_Click(object sender, RoutedEventArgs e)
      {
         if (DBLoader.DataIsLoaded)
            LanguageOptions.ShowMessage("MainWindow/Messages/DatabaseLoadedAlready", MessageBoxButton.OK);
         else if (string.IsNullOrEmpty(_strRecentDatabase))
            LanguageOptions.ShowMessage("MainWindow/Messages/NoRecentDatabase", MessageBoxButton.OK);
         else
            this.cboDBSelection.Value = _strRecentDatabase;
      }

      private void rbtDBSave_Click(object sender, RoutedEventArgs e)
      {
         DBLoader.SaveDatabase();
      }

      private void rbtDBClose_Click(object sender, RoutedEventArgs e)
      {
         if (DBLoader.DataIsLoaded)
         {
            if (this.SaveTableBeforeClosing(true))
            {
               switch (LanguageOptions.ShowMessage("MainWindow/Messages/SecurityCheckUnsavedData", MessageBoxButton.YesNoCancel))
               {
                  case MessageBoxResult.Yes:
                     DBLoader.SaveDatabase();
                     break;
                  case MessageBoxResult.No:
                     break;
                  case MessageBoxResult.Cancel:
                     return;
               }
            }
            DBLoader.CloseDatabase();
         }
      }

      public void rbtLanguage_OnClick(object sender, RoutedEventArgs e)
      {
         this.SetLanguage(((RadioButtonTool)sender).Tag.ToString());
      }

      private void rcbOptionUseTranslatedFields_Click(object sender, RoutedEventArgs e)
      {
         if (_blnCodeIsRunning) return;
         _blnCodeIsRunning = true;
         bool blnIsChecked = (bool)((CheckBoxTool)sender).IsChecked;
         App.Current.Properties[Const.OptionsUseTranslatedFields] = blnIsChecked;
         this.cbtOptionUseTranslatedFields.IsChecked = blnIsChecked;
         _blnCodeIsRunning = false;
      }

      private void rcbOptionUseForeignKeyLookup_Click(object sender, RoutedEventArgs e)
      {
         if (_blnCodeIsRunning) return;
         _blnCodeIsRunning = true;
         bool blnIsChecked = (bool)((CheckBoxTool)sender).IsChecked;
         App.Current.Properties[Const.OptionsUseForeignKeyLookup] = blnIsChecked;
         this.cbtOptionUseForeignKeyLookup.IsChecked = blnIsChecked;
         if (this.MainFrame.Content != null)
            this.RefreshActualTable();
         _blnCodeIsRunning = false;
      }

      private void rcbOptionUseTeamFilter_Click(object sender, RoutedEventArgs e)
      {
         if (_blnCodeIsRunning) return;
         _blnCodeIsRunning = true;
         bool blnIsChecked = (bool)((CheckBoxTool)sender).IsChecked;
         App.Current.Properties[Const.OptionsUseTeamFilter] = blnIsChecked;
         this.cbtOptionUseTeamFilter.IsChecked = blnIsChecked;
         if (this.MainFrame.Content != null)
            this.RefreshActualTable();
         _blnCodeIsRunning = false;
      }

      private void rbtExit_Click(object sender, RoutedEventArgs e)
      {
         Application.Current.MainWindow.Close();
      }

      
      private void cmdDeleteDB_Click(object sender, RoutedEventArgs e)
      {
         //
      }
      
      private void cboDBSelection_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
      {
         if (_blnCodeIsRunning) return;
         string strDatabase = "";
         try { strDatabase = ((ComboEditorTool)sender).SelectedItem.ToString(); }
         catch { return; }
         if (!string.IsNullOrEmpty(strDatabase))
            if (this.SecurityCheckUnsavedData(true))
            {
               this.StartLongJob("LoadDatabase", strDatabase);
               if (DBLoader.DataIsLoaded)
               {
                  _blnCodeIsRunning = true;
                  DBLoader.Tables_FillList(this.cboTableSelection);
                  _blnCodeIsRunning = false;
               }
            }
      }


      private void cmdTableLayoutDef_Click(object sender, RoutedEventArgs e)
      {
         if (this.MainFrame.Content != null)
            ((Pages.Table)this.MainFrame.Content).ChangeLayoutDefinition();
      }

      private void cboTableSelection_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
      {
         if (_blnCodeIsRunning) return;
         if (!DBLoader.DataIsLoaded)
         {
            LanguageOptions.ShowMessage("MainWindow/Messages/NoDataLoaded", MessageBoxButton.OK);
            return;
         }
         string strActualSelection = "", strPreviousSelection = "";
         try { strActualSelection = ((ComboEditorTool)sender).SelectedItem.ToString(); }
         catch { return; }
         try { strPreviousSelection = App.Current.Properties[Const.SelectedTable].ToString(); }
         catch { }
         if (!string.IsNullOrEmpty(strActualSelection))
            if (SaveTableBeforeClosing(false))
            {
               if (strPreviousSelection != strActualSelection)
               {
                  App.Current.Properties[Const.SelectedTable] = strActualSelection;
                  DBLoader.Columns_FillList(this.cboFilterColumn, strActualSelection);
                  DBLoader.Columns_FillList(this.cboMassEditColumn, strActualSelection, true);
                  App.Current.Properties[Const.TableFilterColumn] = "";
               }
               Page p = new Pages.Table(strActualSelection);
               this.MainFrame.Content = p;
            }
      }

      private void rbtTableSelection_Click(object sender, RoutedEventArgs e)
      {
         if (_blnCodeIsRunning) return;
         if (!DBLoader.DataIsLoaded)
         {
            LanguageOptions.ShowMessage("MainWindow/Messages/NoDataLoaded", MessageBoxButton.OK);
            return;
         }
         string strTable = null;
         switch (((ButtonTool)sender).Id)
         {
            case "TableSelection_Manager":
               strTable = "DYN_manager";
               break;
            case "TableSelection_Sponsor":
               strTable = "DYN_sponsor";
               break;
            case "TableSelection_Objectif":
               strTable = "DYN_objectif";
               break;
            case "TableSelection_Team":
               strTable = "DYN_team";
               break;
            case "TableSelection_Cyclist":
               strTable = "DYN_cyclist";
               break;
            case "TableSelection_Race":
               strTable = "STA_race";
               break;
            case "TableSelection_Stage":
               strTable = "STA_stage";
               break;
         }
         if (!string.IsNullOrEmpty(strTable))
            this.cboTableSelection.Value = strTable;
      }


      private void cbtOptionUseTeamFilter_Click(object sender, RoutedEventArgs e)
      {
         if (_blnCodeIsRunning) return;
         _blnCodeIsRunning = true;
         bool blnIsChecked = (bool)((CheckBoxTool)sender).IsChecked;
         App.Current.Properties[Const.OptionsUseTeamFilter] = blnIsChecked;
         this.rcbOptionUseTeamFilter.IsChecked = blnIsChecked;
         if (this.MainFrame.Content != null)
            this.RefreshActualTable();
         _blnCodeIsRunning = false;
      }


      private void txtFilterValue_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
      {
         if (e.Key == System.Windows.Input.Key.Enter)
         {
            this.txtFilterValue.Value = this.txtFilterValue.Value.ToString().TrimEnd('\r', '\n');
            this.ApplyFilter();
         }
      }

      private void rbtFilterApply_Click(object sender, RoutedEventArgs e)
      {
         this.ApplyFilter();
      }

      private void ApplyFilter()
      { 
         App.Current.Properties[Const.TableFilterColumn] = this.cboFilterColumn.Value;
         App.Current.Properties[Const.TableFilterValue] = this.txtFilterValue.Value;
         App.Current.Properties[Const.TableFilterType] = this.cboFilterType.Value;
         _blnCodeIsRunning = true;
         if (this.MainFrame.Content != null)
            this.RefreshActualTable();
         _blnCodeIsRunning = false;
      }

      private void rbtFilterClear_Click(object sender, RoutedEventArgs e)
      {
         App.Current.Properties[Const.TableFilterColumn] = "";
         App.Current.Properties[Const.TableFilterValue] = "";
         App.Current.Properties[Const.TableFilterType] = "";
         this.cboFilterColumn.SelectedIndex = -1;
         this.cboFilterType.SelectedIndex = 0;
         this.txtFilterValue.Value = "";
         _blnCodeIsRunning = true;
         if (this.MainFrame.Content != null)
            this.RefreshActualTable();
         _blnCodeIsRunning = false;
      }


      private void txtMassEditValue_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
      {
         if (e.Key == System.Windows.Input.Key.Enter)
         {
            this.txtMassEditValue.Value = this.txtMassEditValue.Value.ToString().TrimEnd('\r', '\n');
            this.ApplyMassEdit();
         }
      }

      private void rbtMassEditApply_Click(object sender, RoutedEventArgs e)
      {
         this.ApplyMassEdit();
      }

      private void ApplyMassEdit()
      {
         if (LanguageOptions.ShowMessage("MainWindow/Messages/MassEditWarning", MessageBoxButton.OKCancel) != MessageBoxResult.OK)
            return;
         App.Current.Properties[Const.MasseditColumn] = this.cboMassEditColumn.Value;
         App.Current.Properties[Const.MasseditValue] = this.txtMassEditValue.Value;
         App.Current.Properties[Const.MasseditType] = this.cboMassEditType.Value;
         _blnCodeIsRunning = true;
         if (this.MainFrame.Content != null)
            ((Pages.Table)this.MainFrame.Content).ApplyMassEdit();
         _blnCodeIsRunning = false;
      }

      private void rbtMassEditClear_Click(object sender, RoutedEventArgs e)
      {
         App.Current.Properties[Const.MasseditColumn] = "";
         App.Current.Properties[Const.MasseditValue] = "";
         App.Current.Properties[Const.MasseditType] = "";
         this.cboMassEditColumn.SelectedIndex = -1;
         this.cboMassEditType.SelectedIndex = 0;
         this.txtMassEditValue.Value = "";
      }


      private void rbtLayoutReset_Click(object sender, RoutedEventArgs e)
      {
         CustomizationType ctType = CustomizationType.None;
         switch (((ButtonTool)sender).Id)
         {
            case "LayoutResetPosition":
               ctType = CustomizationType.FieldPosition;
               break;
            case "LayoutResetSize":
               ctType = CustomizationType.FieldExtent;
               break;
            case "LayoutResetSorting":
               ctType = CustomizationType.GroupingAndSorting;
               break;
         }
         if (this.MainFrame.Content != null)
            ((Pages.Table)this.MainFrame.Content).ResetLayout(ctType);
      }

      private void cbtOptionUseTranslatedFields_Click(object sender, RoutedEventArgs e)
      {
         if (_blnCodeIsRunning) return;
         _blnCodeIsRunning = true;
         bool blnIsChecked = (bool)((CheckBoxTool)sender).IsChecked;
         App.Current.Properties[Const.OptionsUseTranslatedFields] = blnIsChecked;
         this.rcbOptionUseTranslatedFields.IsChecked = blnIsChecked;
         _blnCodeIsRunning = false;
      }

      private void cbtOptionUseForeignKeyLookup_Click(object sender, RoutedEventArgs e)
      {
         if (_blnCodeIsRunning) return;
         _blnCodeIsRunning = true;
         bool blnIsChecked = (bool)((CheckBoxTool)sender).IsChecked;
         App.Current.Properties[Const.OptionsUseForeignKeyLookup] = blnIsChecked;
         this.rcbOptionUseForeignKeyLookup.IsChecked = blnIsChecked;
         if (this.MainFrame.Content != null)
            this.RefreshActualTable();
         _blnCodeIsRunning = false;
      }


      private void rbtCyclistLayout_Click(object sender, RoutedEventArgs e)
      {
         App.Current.Properties[Const.OptionsCyclistLayout] = ((ButtonTool)sender).Id;
      }


      private void RefreshActualTable()
      {
         //((Pages.Table)this.MainFrame.Content).UpdateFilter();
         string strTable = this.cboTableSelection.Value.ToString();
         this.SaveTableBeforeClosing(true);
         this.cboTableSelection.Value = null;
         _blnCodeIsRunning = false;
         this.cboTableSelection.Value = strTable;
      }


      private void StartLongJob(string strJob, string strValue)
      {
         _blnCodeIsRunning = true;
         ProgressWindow w = new ProgressWindow();
         w.StartLongJob(strJob, strValue);
         _blnCodeIsRunning = false;
         w = null;
      }


      private void Menu_SetAvailableLanguages()
      {
         foreach (string s in LanguageOptions.AvailableLanguages)
         {
            RadioButtonTool rbt = new RadioButtonTool();
            rbt.Click += new RoutedEventHandler(this.rbtLanguage_OnClick);
            rbt.IsChecked = (s == LanguageOptions.SelectedLanguage);
            rbt.Caption = s;
            rbt.Tag = s;
            try { rbt.LargeImage = BitmapFrame.Create(new Uri(string.Concat(LanguageOptions.LanguageFolder, s, ".jpg"))); }
            catch { }
            this.rmtLanguage.Items.Add(rbt);
         }
      }

      private void SetLanguage(string strLanguage)
      {
         if (strLanguage != null)
            LanguageOptions.SelectedLanguage = strLanguage;
         ((XmlDataProvider)(this.FindResource("Lang"))).Document = LanguageOptions.XmlLanguage;
         this.rcbOptionUseTranslatedFields.Caption = LanguageOptions.Text("MainWindow/ApplicationMenu/Options/UseTranslatedFields");
         this.rcbOptionUseForeignKeyLookup.Caption = LanguageOptions.Text("MainWindow/ApplicationMenu/Options/UseForeignKeyLookup");
         this.rcbOptionUseTeamFilter.Caption = LanguageOptions.Text("MainWindow/ApplicationMenu/Options/UseTeamFilter");
      }


      private bool SecurityCheckUnsavedData()
      {
         return SecurityCheckUnsavedData(false);
      }

      private bool SecurityCheckUnsavedData(bool blnCloseTable)
      {
         if (DBLoader.DataIsLoaded)
         {
            if (this.SaveTableBeforeClosing(blnCloseTable))
            {
               switch (LanguageOptions.ShowMessage("MainWindow/Messages/SecurityCheckUnsavedData", MessageBoxButton.YesNoCancel))
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


      private bool SaveTableBeforeClosing(bool blnCloseTable)
      {
         if (this.MainFrame.Content != null)
         {
            bool blnSaveOK = ((Pages.Table)this.MainFrame.Content).SaveBeforeClosing();
            if (blnCloseTable)
               this.MainFrame.Content = null;
            return blnSaveOK;
         }
         return true;
      }

   }
}
