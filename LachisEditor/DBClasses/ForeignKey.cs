namespace LachisEditor
{
   public class ForeignKey
   {

      public ForeignKey(string strFieldName)
      {
         _strFieldName = strFieldName;
      }

      public ForeignKey(string strFieldName, string strItemsSource, string strSelectedValuePath, string strDisplayMemberPath, string strFilter)
      {
         _strDisplayMemberPath = strDisplayMemberPath;
         _strSelectedValuePath = strSelectedValuePath;
         _strItemsSource = strItemsSource;
         _strFieldName = strFieldName;
         _strFilter = strFilter;
      }

      string _strFieldName = string.Empty;
      public string FieldName
      {
         get { return _strFieldName; }
         set { _strFieldName = value; }
      }

      string _strItemsSource = string.Empty;
      public string ItemsSource
      {
         get { return _strItemsSource; }
         set { _strItemsSource = value; }
      }

      string _strDisplayMemberPath = string.Empty;
      public string DisplayMemberPath
      {
         get { return _strDisplayMemberPath; }
         set { _strDisplayMemberPath = value; }
      }

      string _strSelectedValuePath = string.Empty;
      public string SelectedValuePath
      {
         get { return _strSelectedValuePath; }
         set { _strSelectedValuePath = value; } 
      }

      string _strFilter = string.Empty;
      public string Filter
      {
         get { return _strFilter; }
         set { _strFilter = value; }
      }

   }
}
