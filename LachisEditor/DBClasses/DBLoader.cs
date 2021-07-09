using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Collections;
using System.Diagnostics;
using System.Windows;
using System.Text;
using System.Data;
using System.Xml;
using System.IO;
using System;
using Syncfusion.Windows.Tools.Controls;

namespace LachisEditor
{
    public class DBLoader : IProgressOperation
    {
        private int _intProgressMaximum;
        private int _intProgressCurrent;
        private string _strProgressStatus;
        private bool _blnProgressCancelRequested = false;

        private static DBDataset _dsCyanideDB;
        private static bool _blnDataIsLoaded = false;

        private static string _strApplicationPath;
        private static string _strForeignKeysPath;
        private static string _strFieldLayoutsPath;

        private static DatabaseInfo _ActualDatabase = new DatabaseInfo();
        private static List<DatabaseInfo> _Databases = new List<DatabaseInfo>();

        private static NumberFormatInfo _nfiFloat = new NumberFormatInfo();


        public DBLoader()
        {
            _intProgressMaximum = 0;
            _intProgressCurrent = 0;
            _blnProgressCancelRequested = false;
            _nfiFloat.NumberDecimalSeparator = ".";
            _nfiFloat.NumberGroupSeparator = ",";
        }


        public static bool DataIsLoaded
        {
            get { return _blnDataIsLoaded; }
        }

        public static string LoadedDatabaseName
        {
            get { return _ActualDatabase.Name; }
        }


        public static string ApplicationPath
        {
            get { return _strApplicationPath; }
            set
            {
                _strApplicationPath = value + "\\";
                _strForeignKeysPath = _strApplicationPath + Const.ForeignKeysPath;
                _strFieldLayoutsPath = _strApplicationPath + Const.FieldLayoutsPath;
            }
        }

        public static string FieldLayoutsPath
        {
            get { return _strFieldLayoutsPath; }
        }

        public static string ForeignKeysPath
        {
            get { return _strForeignKeysPath; }
        }


        //TODO: Replace ComboEditor Tool
        //TODO: Replace ComboBoxItemsProvider
        public static void Databases_FillList( /*ComboEditorTool cbo,*/ bool blnSelectActiveDatabase)
        {
            if (_Databases.Count == 0)
            {
                string strDatabaseFolder = string.Concat(_strApplicationPath, "Data\\");
                DirectoryInfo[] diList = new DirectoryInfo(strDatabaseFolder).GetDirectories();
                if (diList.Length > 0)
                {
                    ArrayList alDBs = new ArrayList(diList);
                    alDBs.Sort(new LatestUsageComparer());
                    foreach (DirectoryInfo di in alDBs)
                    {
                        string strPathSettings = string.Concat(di.FullName, "\\", Const.SettingsFileName);
                        if (File.Exists(strPathSettings))
                        {
                            XmlDocument xdocSettings = new XmlDocument();
                            xdocSettings.Load(strPathSettings);
                            XmlNode xRoot = xdocSettings.SelectSingleNode(string.Concat("/", Const.TopNode));
                            string strDatabasePath = xRoot.SelectSingleNode(Const.SettingsDatabasePath).InnerText;
                            AddDatabaseToList(new DatabaseInfo(di.Name, strDatabasePath));
                        }
                    }
                }
            }
            /*ComboBoxItemsProvider provider = new ComboBoxItemsProvider();
            foreach (DatabaseInfo dbi in _Databases)
               provider.Items.Add(new ComboBoxDataItem(dbi.Name, dbi.Name));
            cbo.ValueType = typeof(string);
            cbo.ItemsProvider = provider;
            if (blnSelectActiveDatabase)
               cbo.Value = _ActualDatabase.Name;*/
        }

        public static void Tables_FillList(ComboBoxAdv cbo)
        {
            cbo.Items.Clear();
            foreach (DBTable t in _dsCyanideDB.Tables)
            {
                if (t != null && t.TableName != Const.CountryRegionTable)
                    cbo.Items.Add(t.TableName);
            }
        }

        public static List<string> GetExistingTables()
        {
            var result = new List<string>();
            
            foreach (DBTable t in _dsCyanideDB.Tables)
            {
                if (t != null && t.TableName != Const.CountryRegionTable)
                    result.Add(t.TableName);
            }

            return result;
        }


        //TODO: Replace ComboEditor Tool
        public static void Columns_FillList( /*ComboEditorTool cbo,*/ string strTable)
        {
            /*cbo.Value = null;
            ComboBoxItemsProvider provider = new ComboBoxItemsProvider();
            foreach (DataColumn c in _dsCyanideDB.Tables[strTable].Columns)
               if (c.ColumnName != Const.ColumnName_IsHelpRow)
                  provider.Items.Add(new ComboBoxDataItem(c.ColumnName, c.Caption));
            //provider.Items.SortDescriptions.Add(new SortDescription("DisplayText", ListSortDirection.Ascending));
            cbo.ValueType = typeof(string);
            cbo.ItemsProvider = provider;*/
        }

        //TODO: Replace ComboEditor Tool
        public static void Columns_FillList( /*ComboEditorTool cbo,*/ string strTable, bool blnNumericOnly)
        {
            /*cbo.Value = null;
            ComboBoxItemsProvider provider = new ComboBoxItemsProvider();
            foreach (DataColumn c in _dsCyanideDB.Tables[strTable].Columns)
            {
               switch(c.DataType.Name)
               {
                  case "Single":
                  case "Int16":
                  case "Int32":
                     if (c.ColumnName != Const.ColumnName_IsHelpRow)
                        provider.Items.Add(new ComboBoxDataItem(c.ColumnName, c.Caption));
                     break;
               }
            }
            //provider.Items.SortDescriptions.Add(new SortDescription("DisplayText", ListSortDirection.Ascending));
            cbo.ValueType = typeof(string);
            cbo.ItemsProvider = provider;*/
        }


        public static DataView GetDataView(string strTableName, string strFilterColumn, string strFilterType,
            string strFilterValue, bool blnUseTeamFilter)
        {
            return _dsCyanideDB.GetDataView(strTableName, strFilterColumn, strFilterType, strFilterValue,
                blnUseTeamFilter);
        }

        public static DataView GetDataView(string strTableName, bool blnUseTeamFilter)
        {
            return _dsCyanideDB.GetDataView(strTableName, blnUseTeamFilter);
        }

        public static DataView GetDataviewLayoutDefinition(string strTableName)
        {
            return _dsCyanideDB.GetDataViewFieldLayout(strTableName);
        }

        public static void ApplyLayoutDefinition()
        {
            _dsCyanideDB.ApplyFieldLayoutDefinition();
        }


        public static DataView GetLookup(string strTableName, string strFilter, string strSortBy)
        {
            return _dsCyanideDB.GetLookup(strTableName, strFilter, strSortBy);
        }

        public static List<ForeignKey> GetTableForeignKeyList(string strTableName)
        {
            try
            {
                return ((DBTable) _dsCyanideDB.Tables[strTableName]).ForeignKeyList;
            }
            catch
            {
                return null;
            }
        }

        public static List<string> GetTableLargeSingleList(string strTableName)
        {
            try
            {
                return ((DBTable) _dsCyanideDB.Tables[strTableName]).LargeSingleList;
            }
            catch
            {
                return null;
            }
        }


        public static string GetTableName(string strPrimaryKey, int intFieldCount)
        {
            return _dsCyanideDB.GetTableName(strPrimaryKey, intFieldCount);
        }

        public static string GetFieldLayouts()
        {
            return _dsCyanideDB.BuildFieldLayouts(null);
        }

        public static string GetFieldLayouts(List<string> strTableList)
        {
            return _dsCyanideDB.BuildFieldLayouts(strTableList);
        }

        public static void SetFieldLayouts(string strFieldLayouts)
        {
            _dsCyanideDB.SplitFieldLayouts(strFieldLayouts, null);
        }

        public static void UpdateFieldLayout(string strTable, string strFieldLayouts)
        {
            _dsCyanideDB.SplitFieldLayouts(strFieldLayouts, strTable);
        }


        public static void CloseDatabase()
        {
            if (_blnDataIsLoaded)
            {
                _dsCyanideDB.SaveFieldLayouts();
                _blnDataIsLoaded = false;
            }
        }


        private void LoadDatabase(object sender, DoWorkEventArgs e)
        {
            // FieldLayouts speichern, falls vorher schon eine DB offen war (Fieldlayouts sind PCM-Versions-abhängig)
            if (_blnDataIsLoaded) _dsCyanideDB.SaveFieldLayouts();
            // Prüfen, ob DB Informationen noch vorhanden sind
            string strPathSettings = string.Concat(DatabaseFolder, Const.SettingsFileName);
            if (!File.Exists(strPathSettings))
            {
                LanguageOptions.ShowMessage("DBLoader/LoadDatabaseNoData", MessageBoxButton.OK);
                RemoveDatabaseFromList(_ActualDatabase.Name);
                _blnDataIsLoaded = false;
                return;
            }

            // DB Informationen einlesen
            this.ProgressStatus = "Load data";
            this.ProgressMaximum = 1000000;
            XmlDocument xdocSettings = new XmlDocument();
            xdocSettings.Load(strPathSettings);
            XmlNode xnRoot = xdocSettings.SelectSingleNode(string.Concat("/", Const.TopNode));
            _ActualDatabase.Path = xnRoot.SelectSingleNode(Const.SettingsDatabasePath).InnerText;
            XmlNode xnTables = xnRoot.SelectSingleNode(Const.SettingsTables);
            int intNumTables = int.Parse(xnTables.Attributes[Const.db_NumTables].Value);
            this.ProgressMaximum = intNumTables;
            DBTable[] dbtTables = new DBTable[intNumTables];
            intNumTables = -1;
            foreach (XmlNode xnTable in xnTables.ChildNodes)
            {
                intNumTables++;
                List<string> strLargeSingleList = new List<string>();
                List<ForeignKey> ForeignKeyList = new List<ForeignKey>();
                string strTableName = xnTable.Attributes[Const.SettingsTableName].Value;
                string strPrimarKey = xnTable.Attributes[Const.SettingsTablePrimaryKey].Value;
                XmlNode xnList = xnTable.SelectSingleNode(Const.SettingsTableForeignKeyList);
                if (xnList != null)
                    foreach (XmlNode x in xnList.ChildNodes)
                    {
                        string strFieldName = x.Attributes[Const.SettingsTableFKFieldName].Value;
                        string strItemsSource = x.Attributes[Const.SettingsTableFKItemsSource].Value;
                        if (string.IsNullOrEmpty(strItemsSource))
                            ForeignKeyList.Add(new ForeignKey(strFieldName));
                        else
                        {
                            string strFilter = x.Attributes[Const.SettingsTableFKFilter].Value;
                            string strSelectedValuePath = x.Attributes[Const.SettingsTableFKSelectedValuePath].Value;
                            string strDisplayMemberPath = x.Attributes[Const.SettingsTableFKDisplayMemberPath].Value;
                            ForeignKeyList.Add(new ForeignKey(strFieldName, strItemsSource, strSelectedValuePath,
                                strDisplayMemberPath, strFilter));
                        }
                    }

                xnList = xnTable.SelectSingleNode(Const.SettingsTableLargeSingleList);
                if (xnList != null)
                    foreach (XmlNode x in xnList.ChildNodes)
                        strLargeSingleList.Add(x.InnerText);
                dbtTables[intNumTables] = new DBTable(strTableName, strPrimarKey, ForeignKeyList, strLargeSingleList);
                dbtTables[intNumTables].ReadXmlSchema(string.Concat(DatabaseFolder, strTableName, ".xsd"));
                dbtTables[intNumTables].ReadXml(string.Concat(DatabaseFolder, strTableName, ".xml"));
                // Es sollte zwar nie vorkommen, aber falls im Schema eine Relation definiert ist, wird automatisch ein Dataset erstellt. >> Fehler in späterem Code, weil Tabelle nur in einem Dataset vorkommen darf
                if (dbtTables[intNumTables].DataSet != null)
                {
                    DataSet ds = dbtTables[intNumTables].DataSet;
                    while (ds.Relations.Count > 0)
                        ds.Relations.Remove(ds.Relations[0]);
                    ds.Tables.Remove(dbtTables[intNumTables]);
                    ds = null;
                }

                this.ProgressCurrent = intNumTables;
            }

            _dsCyanideDB = new DBDataset(dbtTables, false);
            _blnDataIsLoaded = true;
        }


        public static void SaveDatabase()
        {
            if (!_blnDataIsLoaded) return;
            // Layout Einstellungen speichern
            _dsCyanideDB.SaveFieldLayouts();
            // Datenbank-Informationen und Daten speichern
            int intNumTables = 0;
            XmlDocument xdocSettings = new XmlDocument();
            XmlNode xRoot = xdocSettings.CreateElement(Const.TopNode);
            XmlNode xTables = xdocSettings.CreateElement(Const.SettingsTables);
            //Alle Tabellen speichern
            foreach (DBTable t in _dsCyanideDB.Tables)
            {
                if (t != null)
                {
                    if (t.TableName != Const.CountryRegionTable)
                    {
                        intNumTables++;
                        t.WriteXml(string.Concat(DatabaseFolder, t.TableName, ".xml"));
                        t.WriteXmlSchema(string.Concat(DatabaseFolder, t.TableName, ".xsd"));
                        XmlNode xnTable = xdocSettings.CreateElement(Const.SettingsTable);
                        xnTable.Attributes.Append(xdocSettings.CreateAttribute(Const.SettingsTableName)).Value =
                            t.TableName;
                        xnTable.Attributes.Append(xdocSettings.CreateAttribute(Const.SettingsTablePrimaryKey)).Value =
                            t.PrimaryKeyName;
                        if (t.ForeignKeyList.Count > 0)
                        {
                            XmlNode xnList = xdocSettings.CreateElement(Const.SettingsTableForeignKeyList);
                            foreach (ForeignKey fk in t.ForeignKeyList)
                            {
                                XmlNode xnFK = xdocSettings.CreateElement(Const.SettingsTableForeignKey);
                                xnFK.Attributes.Append(xdocSettings.CreateAttribute(Const.SettingsTableFKFieldName))
                                    .Value = fk.FieldName;
                                xnFK.Attributes.Append(xdocSettings.CreateAttribute(Const.SettingsTableFKItemsSource))
                                    .Value = fk.ItemsSource;
                                xnFK.Attributes
                                    .Append(xdocSettings.CreateAttribute(Const.SettingsTableFKSelectedValuePath))
                                    .Value = fk.SelectedValuePath;
                                xnFK.Attributes
                                    .Append(xdocSettings.CreateAttribute(Const.SettingsTableFKDisplayMemberPath))
                                    .Value = fk.DisplayMemberPath;
                                xnFK.Attributes.Append(xdocSettings.CreateAttribute(Const.SettingsTableFKFilter))
                                    .Value = fk.Filter;
                                xnList.AppendChild(xnFK);
                            }

                            xnTable.AppendChild(xnList);
                        }

                        if (t.LargeSingleList.Count > 0)
                        {
                            XmlNode xnList = xdocSettings.CreateElement(Const.SettingsTableLargeSingleList);
                            foreach (string s in t.LargeSingleList)
                                xnList.AppendChild(xdocSettings.CreateElement(Const.SettingsTableLargeSingle))
                                    .InnerText = s;
                            xnTable.AppendChild(xnList);
                        }

                        xTables.AppendChild(xnTable);
                    }
                }
            }

            // Anzahl der Tabellen speichern
            xTables.Attributes.Append(xdocSettings.CreateAttribute(Const.db_NumTables)).Value = intNumTables.ToString();
            xRoot.AppendChild(xTables);
            // Speicherort der zuletzt geladenen DB
            xRoot.AppendChild(xdocSettings.CreateElement(Const.SettingsDatabasePath)).InnerText = _ActualDatabase.Path;
            // Speichern in Settings.xml
            xdocSettings.AppendChild(xRoot);
            xdocSettings.Save(string.Concat(DatabaseFolder, Const.SettingsFileName));
        }


        private void ImportDatabase(object sender, DoWorkEventArgs e)
        {
            // FieldLayouts speichern, falls vorher schon eine DB offen war (Fieldlayouts sind PCM-Versions-abhängig)
            if (_blnDataIsLoaded) _dsCyanideDB.SaveFieldLayouts();

            // Export aus CDB
            int intProgressStep = 2;
            int intProgressCurrent = 20;
            this.ProgressMaximum = 1000000;
            this.ProgressStatus = "Extracting data";
            this.ProgressCurrent = intProgressCurrent;
            string strExportPath = string.Concat(DatabaseFolder, Const.DumpOutFileName);
            ProcessStartInfo pci = new ProcessStartInfo(string.Concat(_strApplicationPath, Const.ExporterApplication));
            pci.Arguments = string.Format(" -input \"{0}\" -output \"{1}\" -ToXML", _ActualDatabase.Path,
                strExportPath);
            pci.WindowStyle = ProcessWindowStyle.Hidden; //hide console 
            Process proc = Process.Start(pci);
            while (!proc.HasExited)
            {
                this.ProgressCurrent = intProgressCurrent++;
                if (intProgressCurrent > 9000000)
                    _blnProgressCancelRequested = true;
            }

            //Daten laden
            this.ProgressStatus = "Reading data";
            intProgressStep = 1000000 - intProgressCurrent;
            if (intProgressStep < 0)
                intProgressStep = 0;

            StringBuilder strData = new StringBuilder(100);
            XmlDocument xdocSource = new XmlDocument();
            xdocSource.Load(strExportPath);
            //testen, ob das File korrekt ist
            XmlNodeList xnlAllNodes = xdocSource.SelectNodes(Const.db);
            if (xnlAllNodes.Count != 1)
            {
                if (xnlAllNodes.Count == 0)
                    MessageBox.Show("Wrong file");
                return;
            }

            //Basic-Infos laden
            XmlNode xDatabase = xnlAllNodes.Item(0);
            int intNumOrigTables = int.Parse(xDatabase.Attributes[Const.db_NumOTables].Value);
            int intNumTables = int.Parse(xDatabase.Attributes[Const.db_NumTables].Value);
            intProgressStep = intProgressStep / intNumTables;
            //Tabellen anlegen
            DBTable[] dbtTables;
            dbtTables = new DBTable[intNumTables];
            //Loop über alle Tabellen
            int intTableIndex = -1;
            foreach (XmlNode xnTable in xDatabase.ChildNodes)
            {
                intTableIndex++;
                string strTableName = xnTable.Attributes[Const.table_name].Value;
                this.ProgressStatus = string.Concat("Reading ", strTableName);
                int intTableId = int.Parse(xnTable.Attributes[Const.table_id].Value);
                int intNumRows = int.Parse(xnTable.Attributes[Const.table_NumRows].Value);
                int intNumCols = int.Parse(xnTable.Attributes[Const.table_NumCols].Value);
                int intNumOrigCols = int.Parse(xnTable.Attributes[Const.table_NumOCols].Value);
                string[,] strCells = new String[intNumRows, intNumCols];
                DBColumn[] dbcColumns = new DBColumn[intNumCols + 1];
                //preprocess the columns, to know the DB structure 
                int intColIndex = -1;
                foreach (XmlNode xnColumn in xnTable.ChildNodes)
                {
                    intColIndex++;
                    string strDataType = xnColumn.Attributes[Const.column_type].Value;
                    string strColumnName = xnColumn.Attributes[Const.column_name].Value;
                    int intColumnId = int.Parse(xnColumn.Attributes[Const.column_ID].Value);
                    dbcColumns[intColIndex] = new DBColumn(strColumnName, strDataType);
                    int intRowIndex = -1;
                    switch (strDataType)
                    {
                        case "ListInt":
                        case "ListFloat":
                            foreach (XmlNode xnList in xnColumn.ChildNodes)
                            {
                                intRowIndex++;
                                if (int.Parse(xnList.Attributes[Const.list_size].Value) == 0)
                                    strCells[intRowIndex, intColIndex] = "()";
                                else
                                {
                                    strData.Length = 0;
                                    foreach (XmlNode xItem in xnList.ChildNodes)
                                        strData.Append("," + xItem.InnerText);
                                    if (strData.Length > 0)
                                        strCells[intRowIndex, intColIndex] =
                                            '(' + strData.Remove(0, 1).ToString() + ')';
                                    else
                                        strCells[intRowIndex, intColIndex] = "()";
                                }
                            }

                            break;
                        default:
                            foreach (XmlNode xnCell in xnColumn.ChildNodes)
                                strCells[++intRowIndex, intColIndex] = xnCell.InnerText;
                            break;
                    }
                }

                // Tabelle mit allen Daten erstellen (inkl. Hilfsspalte)
                dbcColumns[++intColIndex] = new DBColumn(Const.ColumnName_IsHelpRow, "Bool");
                dbtTables[intTableIndex] = new DBTable(strTableName, dbcColumns, strCells, intTableId, intNumOrigCols,
                    intNumCols, intNumRows);
                intProgressCurrent += intProgressStep;
                this.ProgressCurrent = intProgressCurrent;
            }

            // Dataset erstellen
            _dsCyanideDB = new DBDataset(dbtTables, true);
            AddDatabaseToList(_ActualDatabase);
            _blnDataIsLoaded = true;
        }


        //This function simply rewrites the export.xml file 
        //It loads the former export.xml to copy the structure and add the data 
        private void ExportDatabase(object sender, DoWorkEventArgs e)
        {
            _blnDataIsLoaded = false;
            this.ProgressCurrent = 0;
            char[] chrTrim = {'(', ')'};
            this.ProgressStatus = "Prepare file";
            XmlDocument xdocDumpOut = new XmlDocument();
            xdocDumpOut.Load(string.Concat(DatabaseFolder, Const.DumpOutFileName));
            XmlDocument xdocDumpIn = new XmlDocument();
            xdocDumpIn.AppendChild(xdocDumpIn.ImportNode(xdocDumpOut.FirstChild, false));
            XmlNode xDumpOutRoot = xdocDumpOut.FirstChild;
            XmlNode DBdest = xdocDumpIn.FirstChild;

            this.ProgressMaximum = xDumpOutRoot.ChildNodes.Count;

            for (int intTableIndex = 0; intTableIndex < xDumpOutRoot.ChildNodes.Count; ++intTableIndex)
            {
                //
                XmlNode xDumpOutTable = xDumpOutRoot.ChildNodes.Item(intTableIndex);
                string strTableName = xDumpOutTable.Attributes[Const.table_name].Value;
                //
                XmlElement xDumpInTable;
                // DataView filtert den Dummy-Eintrag raus (ID = 0), welcher angelegt wurde, damit die ref. Integrität erstellt werden kann
                DataView dvTable = GetDataView(strTableName, false);
                int intNumRows = dvTable.Count;
                xDumpInTable = xdocDumpIn.CreateElement(Const.table);
                XmlAttribute xAttribute;
                xAttribute = xdocDumpIn.CreateAttribute(Const.table_name);
                xAttribute.Value = strTableName;
                xDumpInTable.SetAttributeNode(xAttribute);
                xAttribute = xdocDumpIn.CreateAttribute(Const.table_id);
                xAttribute.Value = xDumpOutTable.Attributes[Const.table_id].Value;
                xDumpInTable.SetAttributeNode(xAttribute);
                xAttribute = xdocDumpIn.CreateAttribute(Const.table_NumOCols);
                xAttribute.Value = xDumpOutTable.Attributes[Const.table_NumOCols].Value;
                xDumpInTable.SetAttributeNode(xAttribute);
                xAttribute = xdocDumpIn.CreateAttribute(Const.table_NumCols);
                xAttribute.Value = xDumpOutTable.Attributes[Const.table_NumCols].Value;
                xDumpInTable.SetAttributeNode(xAttribute);
                xAttribute = xdocDumpIn.CreateAttribute(Const.table_NumRows);
                xAttribute.Value = intNumRows.ToString();
                xDumpInTable.SetAttributeNode(xAttribute);

                foreach (XmlNode xDumpOutColumn in xDumpOutTable.ChildNodes)
                {
                    XmlElement xDumpInColumn = xdocDumpIn.CreateElement(Const.column);
                    string strColumnName = xDumpOutColumn.Attributes[Const.column_name].Value;
                    string strColumnType = xDumpOutColumn.Attributes[Const.column_type].Value;
                    xAttribute = xdocDumpIn.CreateAttribute(Const.column_name);
                    xAttribute.Value = strColumnName;
                    xDumpInColumn.SetAttributeNode(xAttribute);
                    xAttribute = xdocDumpIn.CreateAttribute(Const.column_ID);
                    xAttribute.Value = xDumpOutColumn.Attributes[Const.column_ID].Value;
                    xDumpInColumn.SetAttributeNode(xAttribute);
                    xAttribute = xdocDumpIn.CreateAttribute(Const.column_type);
                    xAttribute.Value = strColumnType;
                    xDumpInColumn.SetAttributeNode(xAttribute);

                    this.ProgressStatus = string.Format("{0}_{1}", strTableName, strColumnName);

                    for (int i = 0; i < intNumRows; ++i)
                    {
                        XmlElement xnCell;
                        string strValue = dvTable[i][strColumnName].ToString();
                        switch (strColumnType)
                        {
                            case "Bool":
                                xnCell = xdocDumpIn.CreateElement(Const.cell);
                                if (string.IsNullOrEmpty(strValue))
                                    xnCell.InnerText =
                                        "0"; // Wert ist NULL, d.h. noch gar kein Wert eingegeben --> False
                                else
                                    xnCell.InnerText = Convert.ToInt16(dvTable[i][strColumnName]).ToString();
                                break;
                            case "Float":
                                xnCell = xdocDumpIn.CreateElement(Const.cell);
                                if (string.IsNullOrEmpty(strValue))
                                    xnCell.InnerText = "0"; // Wert ist NULL, d.h. noch gar kein Wert eingegeben --> 0
                                else
                                    xnCell.InnerText = XmlConvert.ToString((Single) dvTable[i][strColumnName]);
                                //xnCell.InnerText = GetFloatValue((Decimal)dvTable[i][strColumnName]);//this function converts the data to float 
                                break;
                            case "ListInt":
                            case "ListFloat":
                                xnCell = xdocDumpIn.CreateElement(Const.list);
                                strValue = strValue
                                    .Trim(chrTrim); // Dieser Code ist besser, denn es kann ja sein, dass gar keine Klammern vorhanden sind
                                if (string.IsNullOrEmpty(strValue))
                                {
                                    XmlAttribute xListSize = xdocDumpIn.CreateAttribute(Const.list_size);
                                    xListSize.Value = "0";
                                    xnCell.SetAttributeNode(xListSize);
                                }
                                else
                                {
                                    string[] items = strValue.Split(',');
                                    foreach (string item in items)
                                    {
                                        XmlElement xItem = xdocDumpIn.CreateElement(Const.cell);
                                        if (!string.IsNullOrEmpty(item))
                                            xItem.InnerText = item;
                                        xnCell.InsertAfter(xItem, xnCell.LastChild);
                                    }

                                    XmlAttribute xListSize = xdocDumpIn.CreateAttribute(Const.list_size);
                                    xListSize.Value = items.Length.ToString();
                                    xnCell.SetAttributeNode(xListSize);
                                }

                                break;
                            default: // normal cell, just copy the data 
                                xnCell = xdocDumpIn.CreateElement(Const.cell);
                                if (!string.IsNullOrEmpty(strValue))
                                    xnCell.InnerText = strValue; //SecurityElement.Escape(strValue)
                                break;
                        }

                        xDumpInColumn.InsertAfter(xnCell, xDumpInColumn.LastChild);
                    }

                    xDumpInTable.InsertAfter(xDumpInColumn, xDumpInTable.LastChild);
                }

                DBdest.InsertAfter(xDumpInTable, DBdest.LastChild);
                this.ProgressCurrent = intTableIndex;
            }

            string strExportPath = string.Concat(DatabaseFolder, Const.DumpInFileName);
            xdocDumpIn.Save(strExportPath);
            // Import in die CDB
            this.ProgressStatus = "Building database";
            ProcessStartInfo pci = new ProcessStartInfo(string.Concat(_strApplicationPath, Const.ExporterApplication));
            pci.Arguments = string.Format(" -input \"{0}\" -output \"{1}\" -FromXML", strExportPath,
                _ActualDatabase.Path);
            pci.WindowStyle = ProcessWindowStyle.Hidden; //hide console 
            this.ProgressMaximum = 1000000;
            this.ProgressCurrent = 0;
            int intHlp = 0;
            Process proc = Process.Start(pci);
            while (!proc.HasExited)
            {
                this.ProgressCurrent = intHlp++;
            }
        }


        public static void AddDatabaseToList(DatabaseInfo dbiAdd)
        {
            if (!_Databases.Exists(delegate(DatabaseInfo d) { return d.Name == dbiAdd.Name; }))
                _Databases.Add(dbiAdd);
        }

        private static void RemoveDatabaseFromList(string strName)
        {
            _Databases.RemoveAll(delegate(DatabaseInfo d) { return d.Name == strName; });
        }


        private static string DatabaseFolder
        {
            get
            {
                string strPath = string.Concat(_strApplicationPath, "Data\\", _ActualDatabase.Name, "\\");
                if (!Directory.Exists(strPath))
                    Directory.CreateDirectory(strPath);
                return strPath;
            }
        }


        private string GetFloatValue(Decimal dblValue)
        {
            try
            {
                if (dblValue != (Decimal) (Int32) dblValue)
                    return dblValue.ToString(_nfiFloat);
                string strValue = string.Format(_nfiFloat, "{0:0.0}", dblValue);
                return strValue.Substring(0, strValue.Length - 1);
            }
            catch
            {
                return "0.";
            }
        }


        #region IProgressOperation Members

        /// <summary>
        /// Starts the background operation that will export the event logs
        /// </summary>
        public void ProgressStart(string strAction, string strValue)
        {
            BackgroundWorker worker = new BackgroundWorker();
            switch (strAction)
            {
                case "ImportDatabase":
                    _ActualDatabase = new DatabaseInfo(Path.GetFileNameWithoutExtension(strValue), strValue);
                    worker.DoWork += new DoWorkEventHandler(ImportDatabase);
                    break;
                case "ExportDatabase":
                    _ActualDatabase.Path = strValue;
                    worker.DoWork += new DoWorkEventHandler(ExportDatabase);
                    break;
                case "LoadDatabase":
                    _ActualDatabase = new DatabaseInfo(strValue);
                    worker.DoWork += new DoWorkEventHandler(LoadDatabase);
                    break;
                default:
                    return;
            }

            this.ProgressCurrent = 0;
            this.ProgressMaximum = 1;
            this.ProgressStatus = "Please wait";
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
            worker.RunWorkerAsync();
        }

        /// <summary>
        /// Requests cancelation of the event log exporting
        /// </summary>
        public void ProgressCancelAsync()
        {
            this._blnProgressCancelRequested = true;
        }

        public string ProgressStatus
        {
            get { return _strProgressStatus; }
            private set
            {
                _strProgressStatus = value;
                OnProgressStatusChanged(EventArgs.Empty);
            }
        }

        public int ProgressMaximum
        {
            get { return _intProgressMaximum; }
            private set
            {
                _intProgressMaximum = value;
                OnProgressMaximumChanged(EventArgs.Empty);
            }
        }

        public int ProgressCurrent
        {
            get { return _intProgressCurrent; }
            private set
            {
                _intProgressCurrent = value;
                OnProgressCurrentChanged(EventArgs.Empty);
            }
        }

        public event EventHandler ProgressStatusChanged;
        public event EventHandler ProgressCurrentChanged;
        public event EventHandler ProgressMaximumChanged;
        public event EventHandler ProgressCompleted;

        #endregion


        #region IProgressOperation Helper

        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            OnComplete(e);
        }

        protected virtual void OnProgressStatusChanged(EventArgs e)
        {
            if (this.ProgressStatusChanged != null)
                this.ProgressStatusChanged(this, e);
        }

        protected virtual void OnProgressMaximumChanged(EventArgs e)
        {
            if (this.ProgressMaximumChanged != null)
                this.ProgressMaximumChanged(this, e);
        }

        protected virtual void OnProgressCurrentChanged(EventArgs e)
        {
            if (this.ProgressCurrentChanged != null)
                this.ProgressCurrentChanged(this, e);
        }

        protected virtual void OnComplete(RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
                ShowUnhandledException(e);
            if (this.ProgressCompleted != null)
                this.ProgressCompleted(this, e);
        }

        #endregion


        private void ShowUnhandledException(RunWorkerCompletedEventArgs e)
        {
            // new line: \r\n or  Environment.NewLine
            string strLastExceptionMessage = "";
            string strExceptionStackTrace = e.Error.StackTrace;
            Exception exception = e.Error.InnerException;
            System.Text.StringBuilder msg = new System.Text.StringBuilder("----An error occured----\r\n");
            msg.Append(e.Error.Message);
            strLastExceptionMessage = e.Error.Message;
            while (exception != null)
            {
                if (strLastExceptionMessage != exception.Message)
                {
                    msg.AppendFormat("\r\n\r\n----Inner error----\r\n{0}", exception.Message);
                    strLastExceptionMessage = exception.Message;
                }

                strExceptionStackTrace = exception.StackTrace;
                exception = exception.InnerException;
            }

            MessageBox.Show(msg.ToString(), "Error");
            msg.AppendFormat("\r\n\r\n----Stacktrace----\r\n{0}", strExceptionStackTrace);
            StreamWriter sw = new StreamWriter(_strApplicationPath + "DBLoader_Error.txt");
            sw.Write(msg.ToString());
            sw.Close();
            sw.Dispose();
        }
    }


    public class DatabaseInfo
    {
        public string Name;
        public string Path;

        public DatabaseInfo()
        {
        }

        public DatabaseInfo(string strName)
        {
            this.Name = strName;
        }

        public DatabaseInfo(string strName, string strPath)
        {
            this.Name = strName;
            this.Path = strPath;
        }
    }

    internal class LatestUsageComparer : IComparer
    {
        public int Compare(object x, object y)
        {
            FileInfo objX = null;
            FileInfo objY = null;
            try
            {
                objX = ((DirectoryInfo) x).GetFiles(Const.SettingsFileName)[0];
            }
            catch
            {
                return 1;
            }

            try
            {
                objY = ((DirectoryInfo) y).GetFiles(Const.SettingsFileName)[0];
            }
            catch
            {
                return -1;
            }

            return -1 * objX.LastWriteTime.CompareTo(objY.LastWriteTime);
        }
    }
}