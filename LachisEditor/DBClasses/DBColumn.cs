namespace LachisEditor
{
   public class DBColumn
   {

      public DBColumn(string strColumnName, string strDataType)
      {
         _strColumnName = strColumnName;
         _strDataType = strDataType;
      }

      private string _strColumnName = "";
      public string ColumnName
      {
         get { return _strColumnName; }
      }

      private string _strDataType = "";
      public string DataType
      {
         get { return _strDataType; }
      }

   }
}
