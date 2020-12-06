using System;

namespace LachisEditor
{
   /// <summary>
   /// Provides an interface for WPF to display the progress of an operation
   /// </summary>
   public interface IProgressOperation
   {
      /// <summary>
      /// Starts the progress operation
      /// </summary>
      void ProgressStart(string strAction, string strDBPath);

      /// <summary>
      /// Requests cancellation of the progress operation
      /// </summary>
      void ProgressCancelAsync();

      /// <summary>
      /// Gets the progress status 
      /// </summary>
      string ProgressStatus { get; }

      /// <summary>
      /// Gets the progress maximum (the number that represents the progress end)
      /// </summary>
      int ProgressMaximum { get; }

      /// <summary>
      /// The current progress
      /// </summary>
      int ProgressCurrent { get; }

      /// <summary>
      /// Occurs when the progress of the operation has changed
      /// </summary>
      event EventHandler ProgressStatusChanged;

      /// <summary>
      /// Occurs when the maximum of the progress operation has changed (the number that represents the progress end)
      /// </summary>
      event EventHandler ProgressMaximumChanged;

      /// <summary>
      /// Occurs when the progress of the operation has changed
      /// </summary>
      event EventHandler ProgressCurrentChanged;

      /// <summary>
      /// Occurs when the progress operation is complete
      /// </summary>
      event EventHandler ProgressCompleted;
   }
}
