using System.Windows.Controls;
using System.Windows;
using System.Data;
using Infragistics.Windows.DataPresenter;
using System;
using System.Collections.Generic;

namespace LachisEditor
{
   /// <summary>
   /// Interaktionslogik für FieldLayout.xaml
   /// </summary>
   public partial class LayoutDefinition : Window
   {
      DataView _Table;
      string _strTableName;
      private enum eMoveDirection { Up = -1, Down = 1 };

      public LayoutDefinition(string strTable)
      {
         _strTableName = strTable;
         InitializeComponent();
         this.Title = LanguageOptions.Text("LayoutDefinition");
         this.SetDataSource();
      }

      private void SetDataSource()
      {
         _Table = DBLoader.GetDataviewLayoutDefinition(_strTableName);
         this.xdgLayout.DataSource = _Table;
      }

      private void MoveRows(object sender, RoutedEventArgs e)
      {
         Int32 intMoveFactor = 1;
         eMoveDirection eDirection = 0;
         switch (((Button)sender).Name)
         {
            case "cmdMoveUp":
               eDirection = eMoveDirection.Up;
               break;
            case "cmdMoveDown":
               eDirection = eMoveDirection.Down;
               break;
            case "cmdMoveUp5":
               eDirection = eMoveDirection.Up;
               intMoveFactor = 5;
               break;
            case "cmdMoveDown5":
               eDirection = eMoveDirection.Down;
               intMoveFactor = 5;
               break;
            case "cmdMoveUp10":
               eDirection = eMoveDirection.Up;
               intMoveFactor = 10;
               break;
            case "cmdMoveDown10":
               eDirection = eMoveDirection.Down;
               intMoveFactor = 10;
               break;
            case "cmdMoveFirst":
               eDirection = eMoveDirection.Up;
               intMoveFactor = 200;
               break;
            case "cmdMoveLast":
               eDirection = eMoveDirection.Down;
               intMoveFactor = 200;
               break;
         }

         Int32 intHlp =0;
         Int32 intIndex = 0;
         Int32 intOtherRowsFound = 0;
         Int32 intOriginalPosition = -1;
         bool blnSelectionFound = false;
         List<DataRecord> drMovingRows = new List<DataRecord>();
         List<DataRecord> drOtherRows = new List<DataRecord>();
         switch (eDirection)
         {
            case eMoveDirection.Up:
               for (int i = this.xdgLayout.Records.Count - 1; i >= 0; i--)
               {
                  if (this.xdgLayout.Records[i].IsSelected)
                  {
                     drMovingRows.Add((DataRecord)this.xdgLayout.Records[i]);
                     if (!blnSelectionFound)
                        intOriginalPosition = i;
                     blnSelectionFound = true;
                  }
                  else if (blnSelectionFound)
                     if (intOtherRowsFound < intMoveFactor)
                     {
                        drOtherRows.Add((DataRecord)this.xdgLayout.Records[i]);
                        intOtherRowsFound++;
                     }
               }
               intIndex = 0;
               foreach (DataRecord dr in drOtherRows)
                  dr.SetCellValue(dr.FieldLayout.Fields["Column"], intOriginalPosition - intIndex++);
               intHlp = intOriginalPosition - (intMoveFactor + drMovingRows.Count - 1);
               if (intHlp < 0)
                  intOriginalPosition = intOriginalPosition - intHlp;
               intIndex = 0;
               foreach (DataRecord dr in drMovingRows)
                  dr.SetCellValue(dr.FieldLayout.Fields["Column"], intOriginalPosition - (intMoveFactor + intIndex++));
               break;
            case eMoveDirection.Down:
               for (int i = 0; i < this.xdgLayout.Records.Count; i++)
               {
                  if (this.xdgLayout.Records[i].IsSelected)
                  {
                     drMovingRows.Add((DataRecord)this.xdgLayout.Records[i]);
                     if (!blnSelectionFound) 
                        intOriginalPosition = i;
                     blnSelectionFound = true;
                  }
                  else if (blnSelectionFound)
                     if (intOtherRowsFound < intMoveFactor)
                     {
                        drOtherRows.Add((DataRecord)this.xdgLayout.Records[i]);
                        intOtherRowsFound++;
                     }
               }
               intIndex = 0;
               foreach (DataRecord dr in drOtherRows)
                  dr.SetCellValue(dr.FieldLayout.Fields["Column"], intOriginalPosition + intIndex++);
               intHlp = intOriginalPosition + (intMoveFactor + drMovingRows.Count - 1);
               if (intHlp > (this.xdgLayout.Records.Count - 1))
                  intOriginalPosition = intOriginalPosition - (intHlp - (this.xdgLayout.Records.Count - 1));
               intIndex = 0;
               foreach (DataRecord dr in drMovingRows)
                  dr.SetCellValue(dr.FieldLayout.Fields["Column"], intOriginalPosition + (intMoveFactor + intIndex++));
               break;
         }

         this.xdgLayout.ExecuteCommand(DataPresenterCommands.CommitChangesToAllRecords);
         this.xdgLayout.FieldLayouts[0].SortedFields.Clear();
         FieldSortDescription sort1 = new FieldSortDescription();
         sort1.Direction = System.ComponentModel.ListSortDirection.Ascending;
         sort1.FieldName = "Column";
         this.xdgLayout.FieldLayouts[0].SortedFields.Add(sort1);
      }


      private void cmdApply_Click(object sender, RoutedEventArgs e)
      {
         this.DialogResult = true;
         //this.Close();
      }

      private void cmdCancel_Click(object sender, RoutedEventArgs e)
      {
         this.DialogResult = false;
         //this.Close();
      }

      private void xdgLayout_FieldLayoutInitialized(object sender, Infragistics.Windows.DataPresenter.Events.FieldLayoutInitializedEventArgs e)
      {
         foreach (Field f in this.xdgLayout.FieldLayouts[0].Fields)
         {
            string strLabel = LanguageOptions.Text(string.Concat("LayoutDefinition/Columns/", f.Name));
            if (!string.IsNullOrEmpty(strLabel))
               f.Label = strLabel;
         }
      }


      private void xdgLayout_Sorting(object sender, Infragistics.Windows.DataPresenter.Events.SortingEventArgs e)
      {
         e.Cancel = true;
      }
   }
}
