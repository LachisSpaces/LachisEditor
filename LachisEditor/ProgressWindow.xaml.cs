using System.ComponentModel;
using System.Windows;
using System;

namespace LachisEditor
{
   /// <summary>
   /// Interaktionslogik für ProgressWindow.xaml
   /// </summary>
   public partial class ProgressWindow : Window, INotifyPropertyChanged
   {
      private IProgressOperation _operation;

      public ProgressWindow()
      {
         InitializeComponent();
      }

      public void StartLongJob(string strJob, string strValue)
      {
         _operation = new DBLoader();
         _operation.ProgressStatusChanged += new EventHandler(Operation_ProgressStatusChanged);
         _operation.ProgressCurrentChanged += new EventHandler(Operation_ProgressCurrentChanged);
         _operation.ProgressMaximumChanged += new EventHandler(Operation_ProgressMaximumChanged);
         _operation.ProgressCompleted += new EventHandler(Operation_ProgressCompleted);
         _operation.ProgressStart(strJob, strValue);
         this.ShowDialog();
      }

      private void Operation_ProgressStatusChanged(object sender, EventArgs e)
      {
         this.OnPropertyChanged("Status");
      }

      private void Operation_ProgressCurrentChanged(object sender, EventArgs e)
      {
         this.OnPropertyChanged("Current");
      }

      private void Operation_ProgressMaximumChanged(object sender, EventArgs e)
      {
         this.OnPropertyChanged("Maximum");
      }

      private void Operation_ProgressCompleted(object sender, EventArgs e)
      {
         this.Close();
      }

      private void CancelClick(object sender, RoutedEventArgs e)
      {
         _operation.ProgressCancelAsync();
      }

      public string Status
      {
         get 
         {
            if (_operation == null)
               return "";
            return _operation.ProgressStatus; 
         }
      }

      public int Current
      {
         get 
         {
            if (_operation == null)
               return 0;
            return _operation.ProgressCurrent; 
         }
      }

      public int Maximum
      {
         get 
         {
            if (_operation == null)
               return 0;
            return _operation.ProgressMaximum; 
         }
      }

      #region INotifyPropertyChanged Members

      /// <summary>
      /// Notify property changed
      /// </summary>
      /// <param name="propertyName">Property name</param>
      protected void OnPropertyChanged(string propertyName)
      {
         if (PropertyChanged != null)
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
      }

      public event PropertyChangedEventHandler PropertyChanged;

      #endregion
   }
}
