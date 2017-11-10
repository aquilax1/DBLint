using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.IO;
using System.Diagnostics;
using NVelocity;
using NVelocity.App;
using CommandLine;
using System.Runtime.InteropServices;

namespace DBLint.DBLintGui
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            //If arguments a given as input, trigger console line execution
            if (e.Args.Count() > 0)
            {
                //Create object holding command line arguments
                CommandLineOptions options = new CommandLineOptions();
                //Parse arguments and check if ExecuteCommandline flag is set to true
                if (CommandLine.Parser.Default.ParseArguments(e.Args, options))
                {
                    Console.WriteLine("DBLint: Starting command line execution...");

                    //Execute as command line
                    var lintCommandline = new CommandLineExecutor(options);
                    lintCommandline.Execute();
                    System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
                }
                else
                {
                    //Not able to parse arguments, the user is doing it wrong
                    Environment.Exit(0);
                }
            }
            else
            {
                //Project is a console application in order to support command line execution. But we need to close the console window behind the GUI.
                Win32.HideConsoleWindow();

                //Start windows GUI
                base.OnStartup(e);
            }
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            AppDomain currentDomain = default(AppDomain);
            currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += UnhandledExceptionHandler;
#if !DEBUG
            DBLint.DBLintGui.MainWindow.StartupScreen.Show();
#endif
            Velocity.Init();
        }

        private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = default(Exception);
            ex = (Exception)e.ExceptionObject;

            System.Text.StringBuilder logString = new System.Text.StringBuilder();
            logString.AppendLine("Message: " + ex.Message);
            logString.AppendLine("Stacktrace: " + ex.StackTrace);
            
            if (ex.InnerException != null)
            {
                logString.AppendLine("Message: " + ex.InnerException.Message);
                logString.AppendLine("Stacktrace: " + ex.InnerException.StackTrace);
            }

            File.WriteAllText("errorlog.txt", logString.ToString());
        }
    }

    //http://stackoverflow.com/questions/10415807
    internal sealed class Win32
    {
        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public static void HideConsoleWindow()
        {
            IntPtr hWnd = Process.GetCurrentProcess().MainWindowHandle;

            if (hWnd != IntPtr.Zero)
            {
                ShowWindow(hWnd, 0); // 0 = SW_HIDE
            }
        }
    }
}


