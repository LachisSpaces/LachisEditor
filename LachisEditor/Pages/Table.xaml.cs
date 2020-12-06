using Infragistics.Windows.DataPresenter;
using Infragistics.Windows.Editors;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Data;
using System.Diagnostics;
using System.Windows;
using System.Data;
using System;

namespace LachisEditor.Pages
{
   /// <summary>
   /// Interaktionslogik für TeamOverview.xaml
   /// </summary>
   public partial class Table : Page
   {
      DataView _Table;
      string _strTableName;
      string _strCustomLayout;
      bool _blnUseTranslatedFields = false;
      bool _blnUseForeignKeyLookup = false;
      bool _blnControlIsLoading = false;

      public Table(string strTable)
      {
         // Tabellennamen speichern für weitere Verwendung
         _strTableName = strTable;
         Application.Current.MainWindow.Title = LanguageOptions.Text("MainWindow") + " - " + _strTableName;
         // Option abfragen: Spaltenüberschriften übersetzen / Eigenes Layout verwenden (noch nicht implementiert)
         _blnUseTranslatedFields = (bool)App.Current.Properties[Const.OptionsUseTranslatedFields];
         _blnUseForeignKeyLookup = (bool)App.Current.Properties[Const.OptionsUseForeignKeyLookup];
         _strCustomLayout = (string)App.Current.Properties[Const.OptionsCyclistLayout];
         // Das Datagrid hat 2 Styles für Zahlenfelder, aber die Felder mit Datentyp Single verwenden nicht automatisch das richtige
         ValueEditor.RegisterDefaultEditorForType(typeof(Single), typeof(XamCurrencyEditor), true);
         // Generische Funktion         
         InitializeComponent();
         // Daten laden (Triggert dann xdgGeneric_FieldLayoutInitialized)
         this.SetDataSource();
      }

      // Funktion aus dem Ribbon vom Hauptformular
      public void ResetLayout(CustomizationType ctType)
      {
         DBLoader.SetFieldLayouts(this.xdgGeneric.SaveCustomizations());
         this.xdgGeneric.ClearCustomizations(ctType);
         DBLoader.UpdateFieldLayout(_strTableName, this.xdgGeneric.SaveCustomizations());
         this.ReapplyFieldLayouts();
      }

      // Funktion aus dem Ribbon vom Hauptformular
      public void UpdateFilter()
      {
         this.SaveBeforeClosing();
         DBLoader.SetFieldLayouts(this.xdgGeneric.SaveCustomizations());
         this.xdgGeneric.ClearCustomizations(CustomizationType.GroupingAndSorting);
         this.SetDataSource();
         this.ReapplyFieldLayouts();
      }

      // Funktion aus dem Ribbon vom Hauptformular
      public void ChangeLayoutDefinition()
      {
         this.SaveBeforeClosing();
         DBLoader.SetFieldLayouts(this.xdgGeneric.SaveCustomizations());
         Window w = new LayoutDefinition(_strTableName);
         Nullable<bool> dialogResult = w.ShowDialog();
         if ((bool)dialogResult)
         {
            DBLoader.ApplyLayoutDefinition();
            this.ReapplyFieldLayouts();
         }
      }

      // Funktion aus dem Ribbon vom Hauptformular
      public void SetCustomLayout()
      {
         string s = (string)App.Current.Properties[Const.OptionsCyclistLayout];
         if (s != _strCustomLayout)
         {
            _strCustomLayout = s;
         }
      }

      // Funktion aus dem Ribbon vom Hauptformular
      public void ApplyMassEdit()
      { 
         this.IterateRecords(this.xdgGeneric.Records);
      }
      public void IterateRecords(RecordCollectionBase records)
      {
         Single sValue = 0;
         string strColumn = "", strType = "";
         try { strType = App.Current.Properties[Const.MasseditType].ToString(); }
         catch { return; }
         try { sValue = Single.Parse(App.Current.Properties[Const.MasseditValue].ToString()); }
         catch { return; }
         try { strColumn = App.Current.Properties[Const.MasseditColumn].ToString(); }
         catch { return; }
         foreach (Record rec in records)
         {
            if (rec.RecordType == RecordType.DataRecord)
            {
               try 
               {
                  DataRecord dataRecord = (DataRecord)rec;
                  switch (strType)
                  {
                     case "1":
                        dataRecord.Cells[strColumn].Value = sValue;
                        break;
                     case "2":
                        dataRecord.Cells[strColumn].Value = Single.Parse(dataRecord.Cells[strColumn].Value.ToString()) + sValue;
                        break;
                     case "3":
                        dataRecord.Cells[strColumn].Value = Single.Parse(dataRecord.Cells[strColumn].Value.ToString()) - sValue;
                        break;
                     case "4":
                        dataRecord.Cells[strColumn].Value = Single.Parse(dataRecord.Cells[strColumn].Value.ToString()) * (100 + sValue) / 100;
                        break;
                     case "5":
                        dataRecord.Cells[strColumn].Value = Single.Parse(dataRecord.Cells[strColumn].Value.ToString()) * (100 - sValue) / 100;
                        break;
                  }
               }
               catch 
               { 
               }
               //if (dataRecord.HasChildren)
               //   IterateRecords(dataRecord.ChildRecords);
            }
         }
      }

      // Funktion aus Hauptformular 
      public bool SaveBeforeClosing()
      {
         // Speichern (falls Focus noch auf Feld der letzten Änderung war)
         this.xdgGeneric.ExecuteCommand(DataPresenterCommands.EndEditModeAndAcceptChanges);
         DBLoader.SetFieldLayouts(this.xdgGeneric.SaveCustomizations());
         return true;
      }



      private void Page_Unloaded(object sender, RoutedEventArgs e)
      {
         // Speichern (falls Focus noch auf Feld der letzten Änderung war)
         this.xdgGeneric.ExecuteCommand(DataPresenterCommands.EndEditModeAndAcceptChanges);
      }


      private void xdgGeneric_FieldLayoutInitialized(object sender, Infragistics.Windows.DataPresenter.Events.FieldLayoutInitializedEventArgs dpe)
      {
         List<string> strTableList = new List<string>();
         for (int intIndex = 0; intIndex < this.xdgGeneric.FieldLayouts.Count; intIndex++)
         {
            FieldLayout layout = this.xdgGeneric.FieldLayouts[intIndex];
            string strLayoutDescription = "";
            if (layout.Description.Length < 7)
               strLayoutDescription = layout.Description;
            else
               strLayoutDescription = layout.Description.Substring(0, 7);
            switch (strLayoutDescription) //layout.Key.ToString().Substring(0, 4)
            {
               case "Updated":
               //case "DUMMY":
               //case "DYN_":
               //case "STA_":
                  break;
               default:
                  string strTable = DBLoader.GetTableName(layout.PrimaryField.Name, layout.Fields.Count);
                  this.UpdateFieldLayout(intIndex);
                  if (strTable != null)
                  {
//                     layout.Key = strTable;
                     layout.Description = "Updated - " + layout.Description;
                     strTableList.Add(strTable);
                     foreach (string s in DBLoader.GetTableLargeSingleList(strTable))
                        try { layout.Fields[s].Settings.EditorType = typeof(XamTextEditor); }
                        catch { }
                     if (_blnUseForeignKeyLookup)
                        foreach (ForeignKey fk in DBLoader.GetTableForeignKeyList(strTable))
                           if (!string.IsNullOrEmpty(fk.DisplayMemberPath))
                           {
                              try
                              {
                                 this.ApplyCombobox(layout.Fields[fk.FieldName], fk.ItemsSource, fk.SelectedValuePath, fk.DisplayMemberPath, fk.Filter);
                              }
                              catch (Exception e)
                              {
                                 string s = fk.FieldName + ": " + e.Message;
                              }
                           }
                  }
                  else
  //                   layout.Key = "DUMMY";
                     layout.Description = "Updated - " + layout.Description;
                  break;
            }
         }
         this.xdgGeneric.LoadCustomizations(DBLoader.GetFieldLayouts(strTableList));
      }

      private void ApplyCombobox(Field f, string strItemsSource, string strSelectedValuePath, string strDisplayMemberPath, string strFilter)
      {
         //create style
         Style sComboBox = new Style(typeof(CellValuePresenter));
         //create controltemplate 
         ControlTemplate xctComboBox = new ControlTemplate(typeof(CellValuePresenter));
         //create element of type combobox
         FrameworkElementFactory fefComboBox = new FrameworkElementFactory(typeof(ComboBox), "cmbAddStCode");
         //set the combobox binding to combobox
         Binding bSource = new Binding();
         bSource.Source = DBLoader.GetLookup(strItemsSource, strFilter, strDisplayMemberPath);
         fefComboBox.SetBinding(ComboBox.ItemsSourceProperty, bSource);
         Binding bText = new Binding("Value");
         bText.RelativeSource = RelativeSource.TemplatedParent;
         bText.Mode = BindingMode.TwoWay;
         fefComboBox.SetBinding(ComboBox.SelectedValueProperty, bText);
         fefComboBox.SetValue(ComboBox.SelectedValuePathProperty, strSelectedValuePath);
         //fefComboBox.SetValue(ComboBox.DisplayMemberPathProperty, strDisplayMemberPath);
         //create the data template
         DataTemplate dtMultiColumn = new DataTemplate();
         //set up the stack panel
         FrameworkElementFactory fefStackPanel = new FrameworkElementFactory(typeof(StackPanel));
         //fefStackPanel.Name = "myComboFactory";
         fefStackPanel.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
         //set up the column 1
         FrameworkElementFactory fefColumn1 = new FrameworkElementFactory(typeof(TextBlock));
         fefColumn1.SetValue(TextBlock.BackgroundProperty, this.FindResource("scbGray"));
         fefColumn1.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Right);
         fefColumn1.SetValue(TextBlock.PaddingProperty, new Thickness(0, 0, 2, 0));
         fefColumn1.SetValue(TextBlock.WidthProperty, 28.0);
         Binding bColumn1 = new Binding("Column1");
         bColumn1.Path = new PropertyPath(strSelectedValuePath);
         fefColumn1.SetBinding(TextBlock.TextProperty, bColumn1);
         fefStackPanel.AppendChild(fefColumn1);
         //set up the column 2
         FrameworkElementFactory fefColumn2 = new FrameworkElementFactory(typeof(TextBlock));
         fefColumn2.SetValue(TextBlock.PaddingProperty, new Thickness(2, 0, 0, 0));
         Binding bColumn2 = new Binding("Column2");
         bColumn2.Path = new PropertyPath(strDisplayMemberPath);
         fefColumn2.SetBinding(TextBlock.TextProperty, bColumn2);
         fefStackPanel.AppendChild(fefColumn2);
         //set up the column 3 if Person
         if (strDisplayMemberPath == "gene_sz_lastname")
         {
            FrameworkElementFactory fefColumn3 = new FrameworkElementFactory(typeof(TextBlock));
            fefColumn3.SetValue(TextBlock.PaddingProperty, new Thickness(4, 0, 0, 0));
            Binding bColumn3 = new Binding("Column3");
            bColumn3.Path = new PropertyPath("gene_sz_firstname");
            fefColumn3.SetBinding(TextBlock.TextProperty, bColumn3);
            fefStackPanel.AppendChild(fefColumn3);
         }
         //set the visual tree of the data template
         dtMultiColumn.VisualTree = fefStackPanel;
         //set the item template to be our shiny new data template
         fefComboBox.SetValue(ComboBox.ItemTemplateProperty, dtMultiColumn);
         //and UI element to visual tree
         xctComboBox.VisualTree = fefComboBox;
         //Set setter
         Setter setter = new Setter(CellValuePresenter.TemplateProperty, xctComboBox);
         sComboBox.Setters.Add(setter);
         //
         fefComboBox.AddHandler(ComboBox.LoadedEvent, new RoutedEventHandler(ComboBox_Loaded));
         fefComboBox.AddHandler(ComboBox.SelectionChangedEvent, new SelectionChangedEventHandler(ComboBox_SelectionChanged));
         //
         f.Settings.CellValuePresenterStyle = sComboBox;
      }

      private void ComboBox_Loaded(object sender, RoutedEventArgs e)
      {
         _blnControlIsLoading = true; // Set Flag to prevent Code in ComboBox_SelectionChanged to be executed
         ((ComboBox)sender).InvalidateProperty(ComboBox.SelectedValueProperty);
         _blnControlIsLoading = false;
      }
      private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         if (!_blnControlIsLoading)
            this.xdgGeneric.ExecuteCommand(DataPresenterCommands.EndEditModeAndAcceptChanges);
      }


      private void SetDataSource()
      {
         string strColumn = "", strType = "", strValue = "";
         try { strType = App.Current.Properties[Const.TableFilterType].ToString(); }
         catch {}
         try { strValue = App.Current.Properties[Const.TableFilterValue].ToString(); }
         catch {}
         try { strColumn = App.Current.Properties[Const.TableFilterColumn].ToString(); }
         catch { }
         _Table = DBLoader.GetDataView(_strTableName, strColumn, strType, strValue, (bool)App.Current.Properties[Const.OptionsUseTeamFilter]);
         this.xdgGeneric.DataSource = _Table;
      }


      private void UpdateFieldLayout(int intFieldLayout)
      {
         foreach (Field f in this.xdgGeneric.FieldLayouts[intFieldLayout].Fields)
         {
            string strLabel = "";
            if (f.Name == Const.ColumnName_IsHelpRow)
               f.Visibility = Visibility.Collapsed;
            else
            {
               if (_blnUseTranslatedFields)
               {
                  strLabel = LanguageOptions.Text(string.Concat(_strTableName, "/", f.Name));
                  if (!string.IsNullOrEmpty(strLabel))
                     f.Label = strLabel;
               }
               if (string.IsNullOrEmpty(strLabel))
                  f.Tag = f.Name;
               else
                  f.Tag = string.Concat(strLabel, System.Environment.NewLine, f.Name);
            }
         }
      }

      private void ReapplyFieldLayouts()
      {
         List<string> strTableList = new List<string>();
         for (int intIndex = 0; intIndex < this.xdgGeneric.FieldLayouts.Count; intIndex++)
         {
            FieldLayout layout = this.xdgGeneric.FieldLayouts[intIndex];
            switch (layout.Key.ToString().Substring(0, 4))
            {
               case "DUMMY":
                  break;
               case "DYN_":
               case "STA_":
                  strTableList.Add(layout.Key.ToString());
                  break;
               default:
                  string strTable = DBLoader.GetTableName(layout.PrimaryField.Name, layout.Fields.Count);
                  if (strTable != null)
                     strTableList.Add(strTable);
                  break;
            }
         }
         this.xdgGeneric.LoadCustomizations(DBLoader.GetFieldLayouts(strTableList));
      }

   }
}
