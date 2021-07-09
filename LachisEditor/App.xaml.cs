using System.Windows.Threading;
using System.Windows;
using System.IO;
using System;
using System.Xml;

namespace LachisEditor
{
   /// <summary>
   /// Interaktionslogik für "App.xaml"
   /// </summary>
   public partial class App : Application
   {
      public static string _strRecentDatabase;
      public static string _strInitialDirectoryFolder;

      protected override void OnStartup(StartupEventArgs e)
      {
         base.OnStartup(e);

         Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(
            "NDcxMTA0QDMxMzkyZTMyMmUzMFFJUkZCcTErL2ZMSng1dm5QSHoxUlRmK0htRStpQ0NucHlWR3A2Yk1xTU09;NDcxMTA1QDMxMzkyZTMyMmUzMFFJb3VRZEdtazhUelpkWW5UaXdpS28zMnExR2FybTVtaW5QZTdKcEtsRVU9");
      }

      void InitAppSettings()
      {
         DBLoader.ApplicationPath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly()?.Location);
         XmlDocument xDocOptions = new XmlDocument();
         xDocOptions.Load(DBLoader.ApplicationPath + Const.ApplicationOptionFile);
         XmlNode xRoot = xDocOptions.SelectSingleNode(string.Concat("/", Const.TopNode));
         _strRecentDatabase = xRoot.SelectSingleNode(Const.OptionsRecentDatabase).InnerText;
         _strInitialDirectoryFolder = xRoot.SelectSingleNode(Const.OptionsFolderSaveGames)?.InnerText;
         App.Current.Properties[Const.OptionsUseTeamFilter] = xRoot.SelectSingleNode(Const.OptionsUseTeamFilter)?.InnerText == "1";
         App.Current.Properties[Const.OptionsUseTranslatedFields] = xRoot.SelectSingleNode(Const.OptionsUseTranslatedFields)?.InnerText == "1";
         App.Current.Properties[Const.OptionsUseForeignKeyLookup] = xRoot.SelectSingleNode(Const.OptionsUseForeignKeyLookup)?.InnerText == "1";
         App.Current.Properties[Const.TableFilterColumn] = "";
         App.Current.Properties[Const.TableFilterValue] = "";
         App.Current.Properties[Const.TableFilterType] = "";
         App.Current.Properties[Const.SelectedTable] = "";
      }
      
      void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
      {
         // new line: \r\n or  Environment.NewLine
         string strLastExceptionMessage = "";
         string strExceptionStackTrace = e.Exception.StackTrace;
         Exception exception = e.Exception.InnerException;
         System.Text.StringBuilder msg = new System.Text.StringBuilder("----An error occured----\r\n");
         msg.Append(e.Exception.Message);
         strLastExceptionMessage = e.Exception.Message;
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
         StreamWriter sw = new StreamWriter(DBLoader.ApplicationPath + "Error.txt");
         sw.Write(msg.ToString());
         sw.Close();
         sw.Dispose();
         Application.Current.MainWindow.Close();
         // Prevent default unhandled exception processing
         e.Handled = true;
      }
   }
}
