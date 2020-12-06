using System.Collections.Generic;
using System.Globalization;
using System.Data;
using System.Xml;
using System;

namespace LachisEditor
{
   public class DBTable : DataTable
   {

      #region DECLARATION

      private const Single C_MAX_FLOAT = 9999999.9f;

      private DBColumn[] _dbcColumns;
      private XmlNode _xLayout = null;
      private string _strPrimaryKey = null;
      private List<string> _strLargeSingleList = new List<string>();
      private List<ForeignKey> _ForeignKeyList = new List<ForeignKey>();
      private int _intRelationCount = 0;

      #endregion


      #region CONSTRUCTORS


      public DBTable(string strTableName, string strPrimaryKey, List<ForeignKey> lfkForeignKeyList, List<string> strLargeSingleList) : base(strTableName)
      {
         _strLargeSingleList = strLargeSingleList;
         _ForeignKeyList = lfkForeignKeyList;
         _strPrimaryKey = strPrimaryKey;
      }


      public DBTable(string strTableName, DBColumn[] dbcColumns, string[,] strCells, int intTableId, int intNumOrigCols, int intNumCols, int intNumRows) : base(strTableName)
      {
         // Alle Spalten anlegen gemäss Definition
         this.DefineColumns(dbcColumns);

         // Daten einlesen
         NumberStyles styles;
         styles = NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent | NumberStyles.AllowLeadingSign;
         NumberFormatInfo nfiFloat = new NumberFormatInfo();
         nfiFloat.NumberDecimalSeparator = ".";
         nfiFloat.NumberGroupSeparator = ",";

         for (int r = 0; r < intNumRows; ++r)
         {
            DataRow dr = this.NewRow();
            for (int c = 0; c < intNumCols; ++c)
            {
               switch (_dbcColumns[c].DataType)
               {
                  case "Bool":
                     dr[c] = (strCells[r, c] == "1");
                     break;
                  case "Float":
                     Single f = 0; // Bisher Decimal
                     try { f = Single.Parse(strCells[r, c], styles, nfiFloat); }
                     catch { LanguageOptions.ShowMessage("DBLoader/ExtractDB_NumberInvalid", System.Windows.MessageBoxButton.OK, new string[] { this.TableName, _dbcColumns[0].ColumnName, strCells[r, 0], _dbcColumns[c].ColumnName, strCells[r, c] }); }
                     dr[c] = f;
                     break;
                  case "String":
                     dr[c] = strCells[r, c];
                     break;
                  default:
                     dr[c] = strCells[r, c];
                     break;
               }
            }
            this.Rows.Add(dr);
         }

         // Daten prüfen (grosse Zahl) + Dummy-Zeile einfügen für referenzielle Integrität
         object[] objRowValues = new object[this.Columns.Count];
         foreach (DataColumn c in this.Columns)
         {
            if (c.DataType == Type.GetType("System.Single"))
            {
               object oMax = this.Compute("MAX(" + c.ColumnName + ")", "");
               if (oMax.GetType() == Type.GetType("System.Single"))
                  if ((Single)oMax > C_MAX_FLOAT)
                     _strLargeSingleList.Add(c.ColumnName);
            }
            if (c.ColumnName == this.PrimaryKeyName)
               objRowValues[c.Ordinal] = 0;
            else if (c.ColumnName == Const.ColumnName_IsHelpRow)
               objRowValues[c.Ordinal] = true;
            else if (c.ColumnName.Length > 4 && c.ColumnName.Substring(0, 4) == "fkID")
               objRowValues[c.Ordinal] = DBNull.Value;
            else
               switch (c.DataType.ToString())
               {
                  case "System.String":
                     objRowValues[c.Ordinal] = "-----";//"DUMMY - DO NOT EDIT OR DELETE THIS ROW";
                     break;
                  case "System.Boolean":
                     objRowValues[c.Ordinal] = false;
                     break;
                  case "Bool":
                     break;
                  default:
                     objRowValues[c.Ordinal] = 0;
                     break;
               }
         }
         try { this.Rows.Add(objRowValues); }
         catch { }
      }


      public DBTable(string strTableName, DBColumn[] dbcColumns) : base(strTableName)
      {
         // Alle Spalten anlegen gemäss Definition
         this.DefineColumns(dbcColumns);
      }

      #endregion


      #region PUBLIC PROPERTIES


      new public DataColumn PrimaryKey
      {
         get { return base.Columns[_strPrimaryKey]; }
      }


      public string PrimaryKeyName
      {
         get { return _strPrimaryKey; }
      }


      public List<ForeignKey> ForeignKeyList
      {
         get { return _ForeignKeyList; }
      }


      public List<string> LargeSingleList
      {
         get { return _strLargeSingleList; }
      }


      public XmlNode FieldLayout
      {
         get { return _xLayout; }
         set { _xLayout = value; }
      }

      #endregion


      #region PUBLIC METHODS


      public string DataType(int intCol)
      {
         return _dbcColumns[intCol].DataType;
      }


      public bool SetValue(int intRow, int intCol, string strValue)
      {
         base.Rows[intRow][intCol] = strValue;
         return true;
      }
      
      public bool SetValue(int intRow, int intCol, decimal dcmValue)
      {
         base.Rows[intRow][intCol] = dcmValue;
         return true;
      }

      public bool SetValue(int intRow, int intCol, float fltValue)
      {
         base.Rows[intRow][intCol] = fltValue;
         return true;
      }

      public bool SetValue(int intRow, int intCol, bool blnValue)
      {
         base.Rows[intRow][intCol] = blnValue;
         return true;
      }


      public void RelationAdded()
      {
         _intRelationCount++;
      }


      public bool FieldCountIsMatching(int intFieldCount)
      {
         return (intFieldCount == base.Columns.Count + _intRelationCount);
      }

       #endregion


      #region PRIVATE METHODS


      private void DefineColumns(DBColumn[] dbcColumns)
      {
         _dbcColumns = dbcColumns;
         for (int intColIndex = 0; intColIndex < dbcColumns.Length; ++intColIndex)
            this.AddDataColumnFromDBColumn(intColIndex);
      }

      private bool AddDataColumnFromDBColumn(int intColIndex)
      {
         bool blnIsPrimary = false;
         string strDataType = _dbcColumns[intColIndex].DataType;
         string strColumnName = _dbcColumns[intColIndex].ColumnName;
         if (strColumnName.StartsWith("fkID"))
         {
            _ForeignKeyList.Add(new ForeignKey(strColumnName));
            strDataType = "Int32";
         }
         else if (strColumnName.StartsWith("ID"))
         {  
            // Einige Tabellen haben eine 1:1 Verknüpfung zu DYN_cyclist >> Verknüpfung ist in PCMnn_FixedForeignKeys.xml hinterlegt
            if (this.TableName == "DYN_cyclist_fitness" && strColumnName == "IDcyclist")
               _ForeignKeyList.Add(new ForeignKey(strColumnName));
            else if (this.TableName == "DYN_cyclist_objective" && strColumnName == "IDcyclist_objective")
               _ForeignKeyList.Add(new ForeignKey(strColumnName));
            else if (this.TableName == "DYN_cyclist_satisfaction" && strColumnName == "IDcyclist_satisfaction")
               _ForeignKeyList.Add(new ForeignKey(strColumnName));
            else if (this.TableName == "DYN_cyclist_season" && strColumnName == "IDcyclist_season")
               _ForeignKeyList.Add(new ForeignKey(strColumnName));
            else if (this.TableName == "DYN_cyclist_peak_detail" && strColumnName == "IDcyclist")
               _ForeignKeyList.Add(new ForeignKey(strColumnName));
            else if (this.TableName == "DYN_cyclist_training_plan" && strColumnName == "IDcyclist_training_plan")
               _ForeignKeyList.Add(new ForeignKey(strColumnName));

            if (intColIndex == 0)
               _strPrimaryKey = strColumnName;
            else
            {  // ID-Feld ist nicht das erste Feld der Tabelle >> Prüfen, ob Tabelel nicht den Konventionen entspricht
               if (string.IsNullOrEmpty(_strPrimaryKey))
               {  // PK wurde noch nicht gefunden >> Annahme: Es handelt sich um den PK, allerdings ist die Position komisch
                  _strPrimaryKey = strColumnName;
#if DEBUG
                  _ForeignKeyList.Add(new ForeignKey(strColumnName, "ERROR", "ColIndex > 0 | Is PK", "ERROR", "ERROR"));
#endif
               }
#if DEBUG
               else
               {  // PK wurde bereits gefunden >> Feld-Name entspricht nicht den Konventionen
                  _ForeignKeyList.Add(new ForeignKey(strColumnName, "ERROR", "ColIndex > 0 | Is not PK", "ERROR", "ERROR"));
               }
#endif
            }
            strDataType = "Int32";
            blnIsPrimary = true;
         }
         else if (this.TableName == "DYN_season_targets")
         {
            switch (strColumnName)
            {
               case "SeasonTarget":
               case "AltObjective1":
               case "AltObjective2":
               case "ManagerP1":
               case "ManagerP2":
               case "ManagerP3":
                  _ForeignKeyList.Add(new ForeignKey(strColumnName));
                  strDataType = "Int32";
                  break;
            }
         }
         DataColumn dc = new DataColumn(strColumnName, GetXMLType(strDataType));
         //dc.AllowDBNull = false;
         if (blnIsPrimary)
         {
            dc.AutoIncrement = true;
            dc.AutoIncrementStep = 1;
         }
         else
         {
            switch (strDataType)
            {
               case "Float":
               case "Int32":
               case "Int16":
               case "Int8":
                  dc.DefaultValue = 0;
                  break;
               case "Bool":
                  dc.DefaultValue = false;
                  break;
               case "String":
               case "ListInt":
               case "ListFloat":
               case "Date": //Wird nicht in der DB verwendet, sondern nur für Hilfsfelder
               default:
                  // kein Default-Wert
                  break;
            }
         }
         base.Columns.Add(dc);
         return true;
      }


      private Type GetXMLType(string strDataType)
      {
         switch (strDataType)
         { 
            case "Float":
               return Type.GetType("System.Single"); // Bisher Decimal
            case "Int32":
               return Type.GetType("System.Int32");
            case "Int16":
               return Type.GetType("System.Int16");
            case "Int8":
               return Type.GetType("System.Int16"); //Byte
            case "Bool":
               return Type.GetType("System.Boolean");
            case "String":
            case "ListInt":
            case "ListFloat":
               return Type.GetType("System.String");
            case "Date": //Wird nicht in der DB verwendet, sondern nur für Hilfsfelder
               return Type.GetType("System.DateTime");
            default:
               return Type.GetType("System.String");
         }
      }

      #endregion

   }
}
