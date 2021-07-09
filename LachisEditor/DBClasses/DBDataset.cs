using System.Collections.Generic;
using System.Data;
using System.Xml;
using System.IO;
using System;

namespace LachisEditor
{
   public class DBDataset : DataSet
   {

      #region DECLARATION

      [Flags]
      private enum ForeignKeyAction 
      {
         Nothing      = 0x0, 
         AddRelation  = 0x1,
         SearchLookup = 0x2,
         RemoveLookup = 0x4
      };

      private enum BuildRelationResult
      {
         DatatypeMismatch   = 0,
         HasConstraints     = 1,
         WithoutConstraints = 2,
         NoIntegrity        = 9
      }

      private int _intIDteam = 0;
      private int _intPCMVersion = -1;
      private bool _blnGridLayoutIsLoaded = false;
      private List<string> _strTableList = new List<string>();
      private DBTable _tblHlpLayout = null;
      private string _strHlpTable = null;

      #endregion


      #region CONSTRUCTORS

      public DBDataset(DBTable[] dbtTables, bool blnLinkTablesAutomatically)
      {

         // Alle geladenen Tabellen ins Dataset übernehmen
         List<string> strTables = new List<string>();
         foreach (DBTable t in dbtTables)
            if (t != null)
               strTables.Add(t.TableName);
         strTables.Sort();
         foreach (string strTable in strTables)
         {
            foreach (DBTable t in dbtTables)
               if (t.TableName == strTable)
                  base.Tables.Add(t);
         }
         
         // Prüfen, ob DB aus PCM15, PCM14, PCM13, PCM12, PCM11, PCM10, PCM09 oder PCM08 (Information wird momentan nur hier verwendet)
         this.CheckDatabaseVersion();

         // Team vom Spieler auslesen
         this.GetManagerTeamId();

         // Tabellen verknüpfen, damit diese auf dem Interface angezeigt werden -> Untertabelle im DataGrid
         if (blnLinkTablesAutomatically)
            this.LinkTablesAutomatically();
         else
            this.LinkTablesFromSettings();

         // Hilfstabelle für Country/Region Lookup
         if (_intPCMVersion != -1)
         {
            DBTable tblCRLookup = new DBTable(Const.CountryRegionTable, new DBColumn[2] { new DBColumn("IDregion", "Int32"), new DBColumn("CONSTANT", "String") });
            foreach (DataRow r in base.Tables["STA_country"].Rows)
            {
               DataRow drNew = tblCRLookup.NewRow();
               drNew["IDregion"] = r["IDcountry"];
               drNew["CONSTANT"] = r["CONSTANT"];
               tblCRLookup.Rows.Add(drNew);
            }
            foreach (DataRow r in base.Tables["STA_region"].Rows)
            {
               DataRow drNew = tblCRLookup.NewRow();
               drNew["IDregion"] = r["IDregion"];
               drNew["CONSTANT"] = r["CONSTANT"];
               tblCRLookup.Rows.Add(drNew);
            }
            base.Tables.Add(tblCRLookup);
         }

      }

      #endregion


      #region PUBLIC METHODS

      public string GetTableName(string strPrimaryKey, int intFieldCount)
      {
         foreach (DBTable t in base.Tables)
            if (t.PrimaryKeyName == strPrimaryKey)
               if (t.FieldCountIsMatching(intFieldCount))
                  return t.TableName;
         return null;
      }


      public DataView GetDataView(string strTableName, string strFilterColumn, string strFilterType, string strFilterValue, bool blnUseTeamFilter)
      {
         string strWhere = "";
         DataView dv = new DataView(base.Tables[strTableName]);
         if (!string.IsNullOrEmpty(strFilterColumn))
         {
            if (base.Tables[strTableName].Columns[strFilterColumn].DataType == Type.GetType("System.String"))
            {
               switch (strFilterType)
               {
                  case "1":
                     strWhere = " AND " + strFilterColumn + "='" + strFilterValue.Replace((char)39, '_') + "'";
                     break;
                  case "2":
                     strWhere = " AND " + strFilterColumn + "<'" + strFilterValue.Replace((char)39, '_') + "'";
                     break;
                  case "3":
                     strWhere = " AND " + strFilterColumn + "<='" + strFilterValue.Replace((char)39, '_') + "'";
                     break;
                  case "4":
                     strWhere = " AND " + strFilterColumn + ">'" + strFilterValue.Replace((char)39, '_') + "'";
                     break;
                  case "5":
                     strWhere = " AND " + strFilterColumn + ">='" + strFilterValue.Replace((char)39, '_') + "'";
                     break;
                  case "6":
                     strWhere = " AND " + strFilterColumn + " LIKE '%" + strFilterValue.Replace((char)39, '_') + "%'";
                     break;
                  case "7":
                     strWhere = " AND " + strFilterColumn + " IN ('" + strFilterValue.Replace((char)39, '_').Replace(", ", ",").Replace(", ", ",").Replace(",", "','") + "')";
                     break;
               }
            }
            else
            {
               switch (strFilterType)
               {
                  case "1":
                     strWhere = " AND " + strFilterColumn + "=" + strFilterValue;
                     break;
                  case "2":
                     strWhere = " AND " + strFilterColumn + "<" + strFilterValue;
                     break;
                  case "3":
                     strWhere = " AND " + strFilterColumn + "<=" + strFilterValue;
                     break;
                  case "4":
                     strWhere = " AND " + strFilterColumn + ">" + strFilterValue;
                     break;
                  case "5":
                     strWhere = " AND " + strFilterColumn + ">=" + strFilterValue;
                     break;
                  case "6":
                     strWhere = " AND " + strFilterColumn + " LIKE %" + strFilterValue + "%";
                     break;
                  case "7":
                     strWhere = " AND " + strFilterColumn + " IN (" + strFilterValue + ")";
                     break;
               }
            }
         }
         else if (!string.IsNullOrEmpty(strFilterValue))
         {
            strWhere = " AND " + strFilterValue;
         }

         if (blnUseTeamFilter && (_intIDteam > 0))
         {
            try { dv.RowFilter = string.Format("{0}={1} AND {2}=false", Const.ColumnName_TeamFK, _intIDteam, Const.ColumnName_IsHelpRow) + strWhere; }
            catch
            {
               try { dv.RowFilter = string.Format("{0}=false", Const.ColumnName_IsHelpRow) + strWhere; }
               catch { dv.RowFilter = string.Format("{0}=false", Const.ColumnName_IsHelpRow); }
            }
         }
         else
         {
            try { dv.RowFilter = string.Format("{0}=false", Const.ColumnName_IsHelpRow) + strWhere; }
            catch { dv.RowFilter = string.Format("{0}=false", Const.ColumnName_IsHelpRow); }
         }
         return dv;
      }

      public DataView GetDataView(string strTableName, bool blnUseTeamFilter)
      {
         DataView dv = new DataView(base.Tables[strTableName]);
         if (blnUseTeamFilter && (_intIDteam > 0))
         {
            try { dv.RowFilter = string.Format("{0}={1} AND {2}=false", Const.ColumnName_TeamFK, _intIDteam, Const.ColumnName_IsHelpRow); }
            catch { dv.RowFilter = string.Format("{0}=false", Const.ColumnName_IsHelpRow); }
         }
         else
            dv.RowFilter = string.Format("{0}=false", Const.ColumnName_IsHelpRow);
         return dv;
      }


      public DataView GetLookup(string strTableName, string strFilter, string strSortBy)
      {
         if (!string.IsNullOrEmpty(strFilter)) 
         {
            if (strFilter == "REGION")
            {
               return new DataView(base.Tables[Const.CountryRegionTable], "", "CONSTANT", DataViewRowState.CurrentRows);
            }
            else
               strFilter = strFilter + " or " + Const.ColumnName_IsHelpRow + "=true";
         }
         try { return new DataView(base.Tables[strTableName], strFilter, strSortBy, DataViewRowState.CurrentRows); }
         catch { return new DataView(); }
         
      }

      #endregion


      #region PRIVATE METHODS


      private void CheckDatabaseVersion()
      {
         _intPCMVersion = -1;
         try { base.Tables["GAM_slot"].Columns["IDslot"].ToString(); } // Prüfen, ob Local.cdb geöffnet wurde
         catch 
         {
            try
            {  // Tabelle existiert erst in PCM2018
               base.Tables["STA_skilltree_branch"].Columns["IDskilltree_branch"].ToString();
               _intPCMVersion = 19;
            }
            catch
            {
               try
               {  // Tabelle existiert erst in PCM2018
                  base.Tables["STA_race_pack"].Columns["IDrace_pack"].ToString();
                  _intPCMVersion = 18;
               }
               catch
               {
                  try
                  {  // Tabelle existiert erst in PCM2017
                     base.Tables["DYN_cyclist_training_plan"].Columns["IDcyclist_training_plan"].ToString();
                     _intPCMVersion = 17;
                  }
                  catch
                  {
                     try
                     {  // Tabelle existiert erst in PCM2016
                        base.Tables["DYN_cyclist_planning"].Columns["IDcyclist_planning"].ToString();
                        _intPCMVersion = 16;
                     }
                     catch
                     {
                        try
                        {  // Tabelle existiert erst in PCM2015
                           base.Tables["DYN_coach_relation"].Columns["IDcoach_relation"].ToString();
                           _intPCMVersion = 15;
                        }
                        catch
                        {
                           try
                           {  // Tabelle existiert erst in PCM2014
                              base.Tables["DYN_brand_contract"].Columns["IDbrand_contract"].ToString();
                              _intPCMVersion = 14;
                           }
                           catch
                           {
                              try
                              {  // Tabelle existiert erst in PCM2013
                                 base.Tables["DYN_cyclist_fitness"].Columns["IDcyclist"].ToString();
                                 _intPCMVersion = 13;
                              }
                              catch
                              {
                                 try
                                 {  // Tabelle existiert erst in PCM2012
                                    base.Tables["DYN_agenda"].Columns["IDagenda"].ToString();
                                    _intPCMVersion = 12;
                                 }
                                 catch
                                 {
                                    try
                                    {  // Tabelle existiert erst in PCM2011
                                       base.Tables["DYN_businessman"].Columns["IDbusinessman"].ToString();
                                       _intPCMVersion = 11;
                                    }
                                    catch
                                    {
                                       try { base.Tables["DYN_contract_manager"].Columns["IDxchange_manager"].ToString(); } // Tabelle existiert nicht mehr in PCM2010
                                       catch { _intPCMVersion = 10; }
                                       if (_intPCMVersion == -1)
                                       {
                                          _intPCMVersion = 9;
                                          try { base.Tables["STA_type_rider"].Columns["f_acceleration_ratio"].ToString(); } // Feld existiert noch nicht in PCM2008
                                          catch { _intPCMVersion = 8; }
                                       }
                                    }
                                 }
                              }
                           }
                        }
                     }
                  }
               }
            }
         }
      }


      private void GetManagerTeamId()
      {
         DataView dv;
         if (_intPCMVersion >= 16)
         {
            try 
            {
               dv = new DataView(base.Tables["GAM_user"]);
               dv.RowFilter = "fkIDteam_duplicate > 0";
               try { _intIDteam = int.Parse(dv[0]["fkIDteam_duplicate"].ToString()); }
               catch { }
            }
            catch { }
         }
         else if (_intPCMVersion >= 13)
         {
            dv = new DataView(base.Tables["GAM_user"]);
            dv.RowFilter = "gene_b_host=true";
            try { _intIDteam = int.Parse(dv[0]["fkIDteam_duplicate"].ToString()); }
            catch { }
         }
         else 
         {
            switch (_intPCMVersion)
            {
               case 10:
               case 11:
               case 12:
                  dv = new DataView(base.Tables["DYN_manager"]);
                  break;
               default:
                  dv = new DataView(base.Tables["DYN_contract_manager"]);
                  break;
            }
            try { _intIDteam = int.Parse(dv[0][Const.ColumnName_TeamFK].ToString()); }
            catch { }
         }
      }


      private void LinkTablesFromSettings()
      {
         foreach (DBTable t in base.Tables)
         {
            foreach (ForeignKey fkKey in t.ForeignKeyList)
            {
               if (!string.IsNullOrEmpty(fkKey.ItemsSource))
               {
                  DBTable tblMaster = (DBTable)base.Tables[fkKey.ItemsSource];
                  DataColumn dcParent = tblMaster.PrimaryKey;
                  DataColumn dcChild = t.Columns[fkKey.FieldName];
                  try
                  {
                     switch (BuildRelation(dcParent, dcChild))
                     {
                        case BuildRelationResult.HasConstraints:
                        case BuildRelationResult.WithoutConstraints: // Eventuell falsch verknüpft
                           tblMaster.RelationAdded();
                           break;
                        case BuildRelationResult.DatatypeMismatch:   // Falscher Datentyp >> Lookup muss entfernt werden (falls bereits definiert)
                        case BuildRelationResult.NoIntegrity:        // Vermutlich unreferenzierte Daten vorhanden >> Lookup muss entfernt werden (falls bereits definiert)
                           break;
                     }
                  }
                  catch { }
               }
            }
         }
      }


      private void LinkTablesAutomatically()
      {
         string strDebug_Info = string.Empty;
         string strDebug_Error_Data = string.Empty;
         string strDebug_Error_Database = string.Empty;
         string strDebug_Error_Datatype = string.Empty;
         string strDebug_Error_FixedForeignKeys = string.Empty;
#if DEBUG
         // Hilfstabelle für Datenfehler (und Andere)
         DBTable tblError = new DBTable("DatabaseErrors", new DBColumn[4] { new DBColumn("IDerror", "Int32"), new DBColumn("Type", "String"), new DBColumn("Error", "String"), new DBColumn(Const.ColumnName_IsHelpRow, "Bool") });
         base.Tables.Add(tblError);
         int intRowIndex = -1;
#endif
         // FK-Definitionen aus Datei laden
         string strFile = string.Format("PCM{0:00}_FixedForeignKeys.xml", _intPCMVersion);
         string strPath = DBLoader.ForeignKeysPath + strFile;
         if (File.Exists(strPath))
         {
            XmlDocument xdoc = new XmlDocument();
            xdoc.Load(strPath);
            foreach (XmlNode xnTable in xdoc.SelectSingleNode(string.Concat('/', Const.TopNode, '/', Const.SettingsTables)).ChildNodes)
            {
               string strTableName = xnTable.Attributes[Const.SettingsTableName].Value;
               XmlNode xnList = xnTable.SelectSingleNode(Const.SettingsTableForeignKeyList);
               if (xnList != null)
               {
                  try
                  {
                     strDebug_Info = strTableName;
                     if (base.Tables.Contains(strTableName))
                     {  // Tabelle existiert >> FK-Felderr suchen und Definition eintragen
                        DBTable t = (DBTable)base.Tables[strTableName];
                        foreach (XmlNode x in xnList.ChildNodes)
                        {
                           string strFieldName = x.Attributes[Const.SettingsTableFKFieldName].Value;
                           string strItemsSource = x.Attributes[Const.SettingsTableFKItemsSource].Value;
                           string strSelectedValuePath = x.Attributes[Const.SettingsTableFKSelectedValuePath].Value;
                           string strDisplayMemberPath = x.Attributes[Const.SettingsTableFKDisplayMemberPath].Value;
                           XmlAttribute xaFilter = x.Attributes[Const.SettingsTableFKFilter];
                           string strFilter = string.Empty;
                           if (xaFilter != null)
                              strFilter = xaFilter.Value;
                           foreach (ForeignKey fkKey in t.ForeignKeyList)
                              if (fkKey.FieldName == strFieldName)
                              {
                                 fkKey.ItemsSource = strItemsSource;
                                 fkKey.SelectedValuePath = strSelectedValuePath;
                                 fkKey.DisplayMemberPath = strDisplayMemberPath;
                                 fkKey.Filter = strFilter;
                                 break;
                              }
#if DEBUG
                           if (t.Columns.Contains(strFieldName))
                           {  // FK-Feld existiert >> Auch dazugehörigen PK prüfen
                              if (base.Tables.Contains(strItemsSource))
                              {  
                                 DBTable t2 = (DBTable)base.Tables[strItemsSource];
                                 if (!string.IsNullOrEmpty(strSelectedValuePath) && !t2.Columns.Contains(strSelectedValuePath))
                                    strDebug_Error_FixedForeignKeys = strDebug_Error_FixedForeignKeys + Environment.NewLine + "Column missing: " + strItemsSource + "." + strSelectedValuePath;
                                 if (!string.IsNullOrEmpty(strDisplayMemberPath) && !t2.Columns.Contains(strDisplayMemberPath))
                                    strDebug_Error_FixedForeignKeys = strDebug_Error_FixedForeignKeys + Environment.NewLine + "Column missing: " + strItemsSource + "." + strDisplayMemberPath;
                              }
                              else
                                 strDebug_Error_FixedForeignKeys = strDebug_Error_FixedForeignKeys + Environment.NewLine + "Table missing: " + strItemsSource;
                           }
                           else
                              strDebug_Error_FixedForeignKeys = strDebug_Error_FixedForeignKeys + Environment.NewLine + "Column missing: " + strTableName + "." + strFieldName;
#endif
                        }
                     }
                     else
                     {
                        strDebug_Error_FixedForeignKeys = strDebug_Error_FixedForeignKeys + Environment.NewLine + "Table missing: " + strDebug_Info;
                     }
                  }
                  catch (Exception e)
                  {
#if DEBUG
                     strDebug_Error_FixedForeignKeys = strDebug_Error_FixedForeignKeys + Environment.NewLine + strDebug_Info + ": " + e.Message;
                     tblError.Rows.Add();
                     tblError.SetValue(++intRowIndex, 0, (decimal)intRowIndex);
                     tblError.SetValue(intRowIndex, 1, e.Message);
                     tblError.SetValue(intRowIndex, 2, strDebug_Info);
#else
                     System.Windows.MessageBox.Show("Error in " + strPath + Environment.NewLine + e.Message + Environment.NewLine + strDebug_Info);
#endif
                  }
               }
            }
         }

#if DEBUG
         // Primärschlüssel überprüfen
         foreach (DBTable t in base.Tables)
            foreach (DBTable hlp in base.Tables)
               if (hlp != t && !string.IsNullOrEmpty(hlp.PrimaryKeyName) && hlp.PrimaryKeyName == t.PrimaryKeyName)
                  strDebug_Error_Database = strDebug_Error_Database + Environment.NewLine + t.TableName + "." + t.PrimaryKeyName + ": Same PK in table " + hlp.TableName;
#endif

         // Tabellen-Verknüpfungen via FK Felder anlegen 
         foreach (DBTable t in base.Tables)
         {
            // Verknüpfung via FK erstellen
            foreach (ForeignKey fkKey in t.ForeignKeyList)
            {
               DBTable tblMaster = null;
               ForeignKeyAction vForeignKeyAction = ForeignKeyAction.Nothing;
               strDebug_Info = t.TableName + "." + fkKey.FieldName;
               try
               {
                  if (!string.IsNullOrEmpty(fkKey.ItemsSource))
                  {  // Eine FK-Definition existiert
                     strDebug_Info = t.TableName + "." + fkKey.FieldName + " >> " + fkKey.ItemsSource;
                     if (fkKey.ItemsSource == "ERROR")
                     {  // Diese FK-Definition wurde nur fürs Debugging hinzugefügt
                        strDebug_Error_Database = strDebug_Error_Database + Environment.NewLine + "Error in " + t.TableName + ": " + fkKey.FieldName + " = " + fkKey.SelectedValuePath;
                        vForeignKeyAction = ForeignKeyAction.RemoveLookup;
                     }
                     else if (fkKey.ItemsSource != t.TableName)
                     {  // FK referenziert NICHT auf eigene Tabelle 
                        if (base.Tables.Contains(fkKey.ItemsSource))
                        {  // Tabelle existiert
                           tblMaster = (DBTable)base.Tables[fkKey.ItemsSource];
                           vForeignKeyAction = ForeignKeyAction.AddRelation;
                        }
                        else
                        {  // Tabelle existiert nicht
                           strDebug_Error_Database = strDebug_Error_Database + Environment.NewLine + "Table " + fkKey.ItemsSource + " does not exist (Linked to " + t.TableName + "." + fkKey.FieldName + ")";
                           vForeignKeyAction = ForeignKeyAction.RemoveLookup;
                        }
                     }
                  }
                  else
                  {  // Keine FK-Definition vorhanden >> Generisch verknüpfen
                     string strFKFieldNameWithoutFK = fkKey.FieldName.Substring(2);
                     strDebug_Info = t.TableName + "." + fkKey.FieldName + " >> (" + strFKFieldNameWithoutFK + ")";
                     switch (strFKFieldNameWithoutFK)
                     {
                        case "IDinjury":
                           switch (t.TableName)
                           {
                              case "DYN_cyclist":
                                 strDebug_Info = t.TableName + "." + fkKey.FieldName + " >> DYN_injury";
                                 tblMaster = (DBTable)base.Tables["DYN_injury"];
                                 break;
                              case "DYN_injury":
                                 strDebug_Info = t.TableName + "." + fkKey.FieldName + " >> STA_injury";
                                 tblMaster = (DBTable)base.Tables["STA_injury"];
                                 break;
                              default:
                                 tblMaster = this.GetTable(strFKFieldNameWithoutFK);
                                 break;
                           }
                           break;
                        default:
                           tblMaster = this.GetTable(strFKFieldNameWithoutFK);
                           break;
                     }
                     if (tblMaster != null)
                     {  // Tabelle existiert
                        if (tblMaster.TableName != t.TableName) // FK referenziert NICHT auf eigene Tabelle 
                           vForeignKeyAction = ForeignKeyAction.AddRelation | ForeignKeyAction.SearchLookup;
                     }
                     else
                     {  // Tabelle existiert (vermutlich) nicht
                        strDebug_Error_Database = strDebug_Error_Database + Environment.NewLine + "No table found with primary key " + strFKFieldNameWithoutFK + " (Linked to " + t.TableName + "." + fkKey.FieldName + ")";
                     }
                  }
                  if ((vForeignKeyAction & ForeignKeyAction.AddRelation) == ForeignKeyAction.AddRelation)
                  {
                     DataColumn dcParent = tblMaster.PrimaryKey;
                     DataColumn dcChild = t.Columns[fkKey.FieldName];
                     strDebug_Info = t.TableName + "." + dcChild.ColumnName + " > " + tblMaster.TableName + "." + dcParent.ColumnName;
                     try
                     {
                        switch (BuildRelation(dcParent, dcChild))
                        {
                           case BuildRelationResult.HasConstraints:
                              tblMaster.RelationAdded();
                              break;
                           case BuildRelationResult.WithoutConstraints: // Referenzielle Integrität kann nicht hergestellt werden >> Falsche Daten oder falsch verknüpft
                              tblMaster.RelationAdded();
                              strDebug_Error_Data = strDebug_Error_Data + Environment.NewLine + "Link without constraint: " + t.TableName + "." + fkKey.FieldName + " > " + tblMaster.TableName + "." + tblMaster.PrimaryKeyName;
                              break;
                           case BuildRelationResult.DatatypeMismatch:   // Falscher Datentyp >> Lookup muss entfernt werden (falls bereits definiert)
                              vForeignKeyAction = ForeignKeyAction.RemoveLookup;
                              strDebug_Error_Datatype = strDebug_Error_Datatype + Environment.NewLine + "Link not possible because of wrong datatype: " + t.TableName + "." + fkKey.FieldName + " > " + tblMaster.TableName + "." + tblMaster.PrimaryKeyName;
                              break;
                           case BuildRelationResult.NoIntegrity:        // Tritt dieser Fall überhaupt ein? >> Lookup muss entfernt werden (falls bereits definiert)
                              vForeignKeyAction = ForeignKeyAction.RemoveLookup;
                              strDebug_Error_Data = strDebug_Error_Data + Environment.NewLine + "Link not possible because of wrong data: " + t.TableName + "." + fkKey.FieldName + " > " + tblMaster.TableName + "." + tblMaster.PrimaryKeyName;
                              break;
                        }
                     }
                     catch (Exception e)
                     {  // Anderer, unerwarteter Fehler >> Lookup muss entfernt werden (falls bereits definiert)
#if DEBUG
                        strDebug_Error_Data = strDebug_Error_Data + Environment.NewLine + e.Message + ": Linking " + t.TableName + "." + fkKey.FieldName + " > " + tblMaster.TableName + "." + tblMaster.PrimaryKeyName;
                        tblError.Rows.Add();
                        tblError.SetValue(++intRowIndex, 0, (decimal)intRowIndex);
                        tblError.SetValue(intRowIndex, 1, e.Message);
                        tblError.SetValue(intRowIndex, 2, strDebug_Info);
#endif
                        vForeignKeyAction = ForeignKeyAction.RemoveLookup;
                     }
                  }
               }
               catch (Exception e)
               {  // "Unerklärlicher" Fehler >> Kein Lookup verwenden
                  vForeignKeyAction = ForeignKeyAction.RemoveLookup;
#if DEBUG
                  strDebug_Error_Data = strDebug_Error_Data + Environment.NewLine + e.Message + ": " + strDebug_Info;
                  tblError.Rows.Add();
                  tblError.SetValue(++intRowIndex, 0, (decimal)intRowIndex);
                  tblError.SetValue(intRowIndex, 1, e.Message);
                  tblError.SetValue(intRowIndex, 2, strDebug_Info);
#endif
               }

               // Angaben für Lookup speichern
               if ((vForeignKeyAction & ForeignKeyAction.RemoveLookup) == ForeignKeyAction.RemoveLookup)
               {
                  fkKey.SelectedValuePath = string.Empty;
                  fkKey.DisplayMemberPath = string.Empty;
                  fkKey.ItemsSource = string.Empty;
               }
               else if ((vForeignKeyAction & ForeignKeyAction.SearchLookup) == ForeignKeyAction.SearchLookup)
               {
                  fkKey.ItemsSource = tblMaster.TableName;
                  fkKey.SelectedValuePath = tblMaster.PrimaryKey.ColumnName;
                  if (tblMaster.DataType(1) == "String")
                     fkKey.DisplayMemberPath = tblMaster.Columns[1].ColumnName;
                  else
                  {
                     foreach (DataColumn c in tblMaster.Columns)
                        if (c.ColumnName == "CONSTANT")
                           fkKey.DisplayMemberPath = c.ColumnName;
                  }
               }
            }
         }
#if DEBUG
         StreamWriter sw;
         if (!string.IsNullOrEmpty(strDebug_Error_FixedForeignKeys))
         {
            sw = new StreamWriter(DBLoader.ApplicationPath + "Error_FixedForeignKeys.txt");
            sw.Write("Error in " + strPath + Environment.NewLine + strDebug_Error_FixedForeignKeys);
            sw.Close();
            sw.Dispose();
         }
         if (!string.IsNullOrEmpty(strDebug_Error_Database) || !string.IsNullOrEmpty(strDebug_Error_Datatype))
         {
            sw = new StreamWriter(DBLoader.ApplicationPath + "Error_Database.txt");
            sw.Write(strDebug_Error_Database + strDebug_Error_Datatype);
            sw.Close();
            sw.Dispose();
         }
         if (!string.IsNullOrEmpty(strDebug_Error_Data))
         {
            sw = new StreamWriter(DBLoader.ApplicationPath + "Error_Data.txt");
            sw.Write(DBLoader.LoadedDatabaseName + Environment.NewLine + strDebug_Error_Data);
            sw.Close();
            sw.Dispose();
         }
#endif
      }


      private DBTable GetTable(string strPrimaryKey)
      {  // Tabelle suchen anhand des Primärschlüssels >> Es werden alle Tabellen geprüft, d.h. falls es mehrere Tabellen mit gleichem PK gibt, dann wird NULL zurückgegeben
         DBTable dbtReturn = null;
         foreach (DBTable t in base.Tables)
            if (t.PrimaryKeyName == strPrimaryKey)
            {
               if (dbtReturn == null)
               {
                  dbtReturn = t;
                  // Ausnahmen hier implementieren
                  if (t.TableName == "DYN_cyclist" && strPrimaryKey == "IDcyclist")
                     return dbtReturn;
               }
               else
               {
                  return null;
               }
            }
         return dbtReturn;
      }


      private BuildRelationResult BuildRelation(DataColumn dcParent, DataColumn dcChild)
      {
         BuildRelationResult rtReturn;
         if (dcParent.DataType == dcChild.DataType)
         {
            DataRelation dRelation = null;
            string strRelationName = string.Concat(dcChild.Table.TableName, '.', dcChild.ColumnName, " linked to ", dcParent.ColumnName);
            try
            {
               // Folgendes Try wird verwendet, weil Fehlerhandling einfacher ist, als Daten auf Konsistenz zu prüfen
               try
               { // Versuche Relation mit Constraint zu erstellen
                  dRelation = new DataRelation(strRelationName, dcParent, dcChild, true);
                  base.Relations.Add(dRelation);
                  rtReturn = BuildRelationResult.HasConstraints;
               }
               catch (ArgumentException)
               {  // Diese Spalten haben momentan keine eindeutigen Werte >> Relation ohne Constraint erstellen
                  base.Relations.Remove(dRelation);
                  dRelation = new DataRelation(strRelationName, dcParent, dcChild, false);
                  base.Relations.Add(dRelation);
                  rtReturn = BuildRelationResult.WithoutConstraints;
               }
            }
            catch (ArgumentException)
            {  // Vermutlich unreferenzierte Daten vorhanden >> Lookup muss entfernt werden (falls bereits definiert)
               rtReturn = BuildRelationResult.NoIntegrity;
            }
         }
         else
            return BuildRelationResult.DatatypeMismatch;

         return rtReturn;
      }

#endregion

      
      #region ** FieldLayouts **

      #region PUBLIC METHODS

      public void SplitFieldLayouts(string strFieldLayouts, string strTable)
      {
         XmlDocument xdoc = new XmlDocument();
         xdoc.LoadXml(strFieldLayouts);
         XmlNode xLayouts = xdoc.SelectSingleNode("/xamDataPresenter/fieldLayouts");
         XmlNodeList xLayoutList = xLayouts.ChildNodes;
         if (string.IsNullOrEmpty(strTable))
            foreach (XmlNode xLayout in xLayoutList)
            {
               strTable = xLayout.Attributes["key"].InnerXml;
               try
               {
                  DBTable t = (DBTable)base.Tables[strTable];
                  if (t != null)
                     t.FieldLayout = xLayout;
               }
               catch { }
            }
         else
            foreach (XmlNode xLayout in xLayoutList)
               if (strTable == xLayout.Attributes["key"].InnerXml)
               {
                  try
                  {
                     DBTable t = (DBTable)base.Tables[strTable];
                     if (t != null)
                        t.FieldLayout = xLayout;
                  }
                  catch { }
                  return;
               }
      }


      public string BuildFieldLayouts(List<string> strTableList)
      {
         _strTableList = strTableList;
         if (!_blnGridLayoutIsLoaded)
            this.LoadFieldLayouts();
         return this.BuildFieldLayouts();
      }


      public void SaveFieldLayouts()
      {
         if (!_blnGridLayoutIsLoaded) return;
         XmlDocument xdoc = new XmlDocument();
         XmlNode xRoot = xdoc.CreateElement("xamDataPresenter");
         xRoot.Attributes.Append(xdoc.CreateAttribute("version")).InnerText = "11.2.20112.2076";// "8.2.20082.2001";
         XmlNode xLayouts = xdoc.CreateElement("fieldLayouts");
         foreach (DBTable t in base.Tables)
            if (t.FieldLayout != null)
               xLayouts.AppendChild(xdoc.ImportNode(t.FieldLayout, true));
         xRoot.AppendChild(xLayouts);
         xdoc.AppendChild(xRoot);
         xdoc.Save(DBLoader.FieldLayoutsPath + string.Format("PCM{0:00}_GridLayout.xml", _intPCMVersion));
      }


      public DataView GetDataViewFieldLayout(string strTable)
      {
         DBTable t = (DBTable)base.Tables[strTable];
         if (t.FieldLayout == null)
            return null;

         _strHlpTable = strTable;
         _tblHlpLayout = new DBTable(Const.TableName_FieldLayout,
            new DBColumn[5] { new DBColumn("Idxno", "Int32"), new DBColumn("FieldName", "String"), new DBColumn("Width", "Int32"), new DBColumn("Column", "Int32"), new DBColumn("LayoutGroups", "String") });

         XmlNodeList xFieldList = t.FieldLayout.SelectSingleNode("fields").ChildNodes;
         int intColIndex = 0;
         bool blnLastFieldReached = false;
         foreach (XmlNode xField in xFieldList)
         {
            string strFieldName = xField.Attributes["name"].InnerXml;
            if (strFieldName == Const.ColumnName_IsHelpRow)
               blnLastFieldReached = true;
            if (!blnLastFieldReached)
            {
               object[] objRowValues = new object[5];
               objRowValues[0] = intColIndex;
               objRowValues[1] = strFieldName;
               objRowValues[2] = this.ValidatedAttributeValue(xField, "cellWidth");
               objRowValues[3] = this.ValidatedAttributeValue(xField, "column", intColIndex);
               objRowValues[4] = "0";
               _tblHlpLayout.Rows.Add(objRowValues);
            }
            intColIndex++;
         }
         return new DataView(_tblHlpLayout);
      }


      public void ApplyFieldLayoutDefinition()
      {
         DataView dv = new DataView(_tblHlpLayout);
         dv.Sort = "Idxno ASC";
         XmlDocument xdoc = new XmlDocument();
         DBTable t = (DBTable)base.Tables[_strHlpTable];
         XmlNode xLayout = t.FieldLayout;
         XmlNode xFields = xLayout.SelectSingleNode("fields");
         XmlNodeList xFieldList = xFields.ChildNodes;
         for (int i = 0; i < dv.Count; i++)
         {
            Int32 intValue = -1;
            try { intValue = (Int32)dv[i]["Width"]; }
            catch { }
            if (intValue >= 0)
            {
               ((XmlElement)xFieldList[i]).SetAttribute("cellWidth", intValue.ToString());
               ((XmlElement)xFieldList[i]).SetAttribute("labelWidth", intValue.ToString());
            }
            else
            {
               ((XmlElement)xFieldList[i]).RemoveAttribute("cellWidth");
               ((XmlElement)xFieldList[i]).RemoveAttribute("labelWidth");
            }
            string strValue = dv[i]["Column"].ToString();
            ((XmlElement)xFieldList[i]).SetAttribute("column", strValue);
            ((XmlElement)xFieldList[i]).SetAttribute("row", "0");
            ((XmlElement)xFieldList[i]).SetAttribute("rowSpan", "1");
            ((XmlElement)xFieldList[i]).SetAttribute("columnSpan", "1");
         }
      }

      #endregion

      #region PRIVATE METHODS

            private void LoadFieldLayouts()
            {
               _blnGridLayoutIsLoaded = true;
               string strPath = DBLoader.FieldLayoutsPath + string.Format("PCM{0:00}_GridLayout.xml", _intPCMVersion);
               if (File.Exists(strPath))
               {
                  XmlDocument xdoc = new XmlDocument();
                  xdoc.Load(strPath);
                  this.SplitFieldLayouts(xdoc.OuterXml, null);
               }
            }


            private string BuildFieldLayouts()
            {
               XmlDocument xdoc = new XmlDocument();
               XmlNode xRoot = xdoc.CreateElement("xamDataPresenter");
               xRoot.Attributes.Append(xdoc.CreateAttribute("version")).InnerText = "11.2.20112.2076";// "8.2.20082.2001";
               XmlNode xLayouts = xdoc.CreateElement("fieldLayouts");
               foreach (string s in _strTableList)
               {
                  DBTable t = (DBTable)base.Tables[s];
                  if (t.FieldLayout != null)
                     xLayouts.AppendChild(xdoc.ImportNode(t.FieldLayout, true));
               }
               xRoot.AppendChild(xLayouts);
               xdoc.AppendChild(xRoot);
               return xdoc.OuterXml;
            }


            private Int32 ValidatedAttributeValue(XmlNode xField, string strAttribute)
            {
               return this.ValidatedAttributeValue(xField, strAttribute, -1);
            }

            private Int32 ValidatedAttributeValue(XmlNode xField, string strAttribute, Int32 intDefault)
            {
               try { return Int32.Parse(xField.Attributes[strAttribute].InnerXml); }
               catch { return intDefault; }
            }

      #endregion

      #endregion

   }
}
