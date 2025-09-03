/*
 * Your rights to use code governed by this license https://github.com/AlexWan/OsEngine/blob/master/LICENSE
 * Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using OsEngine.Alerts;
using OsEngine.Language;
using OsEngine.Market;
using OsEngine.OsConverter;
using OsEngine.OsData;
using OsEngine.OsOptimizer;
using OsEngine.OsTrader.Gui;
using OsEngine.PrimeSettings;
using OsEngine.Layout;
using System.Collections.Generic;
using OsEngine.Entity;
using System.Net.Sockets;
using System.Text;
using OsEngine.OsTrader.Gui.BlockInterface;
using OsEngine.OsTrader.SystemAnalyze;
using System.Reflection;
using System.Linq;


namespace OsEngine
{

    /// <summary>
    /// Application start screen
    /// Стартовое окно приложения
    /// </summary>
    public partial class MainWindow
    {

        private static MainWindow _window;

        public static Dispatcher GetDispatcher
        {
            get { return _window.Dispatcher; }
        }

        public static bool DebuggerIsWork;

        /// <summary>
        ///  is application running
        /// работает ли приложение или закрывается
        /// </summary>
        public static bool ProccesIsWorked;

        public MainWindow()
        {
            Process ps = Process.GetCurrentProcess();
            ps.PriorityClass = ProcessPriorityClass.RealTime;

            InitializeComponent();
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            ImageAlor2.Visibility = Visibility.Collapsed;
            ImageAlor.Visibility = Visibility.Collapsed;

            this.Closing += MainWindow_Closing;

            try
            {
                int winVersion = Environment.OSVersion.Version.Major;
                if (winVersion < 6)
                {
                    MessageBox.Show(OsLocalization.MainWindow.Message1);
                    Close();
                }
                if (!CheckDotNetVersion())
                {
                    Close();
                }

                if (!CheckWorkWithDirectory())
                {
                    MessageBox.Show(OsLocalization.MainWindow.Message2);
                    Close();
                }

                if(!CheckOutSomeLibrariesNearby())
                {
                    MessageBox.Show(OsLocalization.MainWindow.Message6);
                    Close();
                }

                if (!CheckAlreadyWorkEngine())
                {
                    MessageBox.Show(OsLocalization.MainWindow.Message7);
                    Close();
                }

            }
            catch (Exception)
            {
                MessageBox.Show(OsLocalization.MainWindow.Message3);
                Close();
            }

            if(Debugger.IsAttached)
            {
                DebuggerIsWork = true;
            }

            AlertMessageManager.TextBoxFromStaThread = new TextBox();

            ProccesIsWorked = true;
            _window = this;

            ServerMaster.Activate();
            SystemUsageAnalyzeMaster.Activate();

            Thread.CurrentThread.CurrentCulture = OsLocalization.CurCulture;

            Task task = new Task(ThreadAreaGreeting);
            task.Start();

            ChangeText();
            OsLocalization.LocalizationTypeChangeEvent += ChangeText;

            CommandLineInterfaceProcess();

            Task.Run(ClearOptimizerWorkResults);

            this.Activate();
            this.Focus();

            GlobalGUILayout.Listen(this, "mainWindow");

            ImageAlor.MouseEnter += ImageAlor_MouseEnter;
            ImageAlor2.MouseLeave += ImageAlor_MouseLeave;
            ImageAlor2.MouseDown += ImageAlor2_MouseDown;

            if(BlockMaster.IsBlocked == true)
            {
                BlockInterface();
            }
            else
            {
                UnblockInterface();
            }

            ChangeText();

            ReloadFlagButton();

            // Wire up the ComboBox event handler
            // Подключаем обработчик событий ComboBox
            ModulesComboBox.SelectionChanged += ModulesComboBox_SelectionChanged;

            this.ContentRendered += MainWindow_ContentRendered;
        }

        #region Block and Unblock interface

        private void BlockInterface()
        {
            ImageData.Visibility = Visibility.Hidden;
            ImageTests.Visibility = Visibility.Hidden;
            ImageTrading.Visibility = Visibility.Hidden;
            ImageModules.Visibility = Visibility.Hidden;
            ImageFlag_Ru.Visibility = Visibility.Hidden;
            ImageFlag_Eng.Visibility = Visibility.Hidden;

            ImagePadlock.Visibility = Visibility.Visible;
            ImagePadlock.MouseEnter += ImagePadlock_MouseEnter;
            ImagePadlock.MouseLeave += ImagePadlock_MouseLeave;
            ImagePadlock.MouseDown += ImagePadlock_MouseDown;
            ButtonSettings.IsEnabled = false;
            ButtonRobot.IsEnabled = false;
            ButtonTester.IsEnabled = false;
            ButtonData.IsEnabled = false;
            ButtonCandleConverter.IsEnabled = false;
            ButtonConverter.IsEnabled = false;
            ButtonOptimizer.IsEnabled = false;
            ButtonTesterLight.IsEnabled = false;
            ButtonRobotLight.IsEnabled = false;
            ButtonLocal_Ru.IsEnabled = false;
            ButtonLocal_Eng.IsEnabled = false;
        }

        private void ImagePadlock_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            RobotsUiLightUnblock ui = new RobotsUiLightUnblock();

            ui.ShowDialog();

            if (ui.IsUnBlocked == true)
            {
                UnblockInterface();
            }
        }

        private void ImagePadlock_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            ImagePadlock.Cursor = System.Windows.Input.Cursors.Arrow;
        }

        private void ImagePadlock_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            ImagePadlock.Cursor = System.Windows.Input.Cursors.Hand;
        }

        private void UnblockInterface()
        {
            ImageData.Visibility = Visibility.Visible;
            ImageTests.Visibility = Visibility.Visible;
            ImageTrading.Visibility = Visibility.Visible;
            ImageModules.Visibility = Visibility.Visible;
            ImageFlag_Ru.Visibility = Visibility.Visible;
            ImageFlag_Eng.Visibility = Visibility.Visible;

            ImagePadlock.Visibility = Visibility.Hidden;
            ButtonSettings.IsEnabled = true;
            ButtonRobot.IsEnabled = true;
            ButtonTester.IsEnabled = true;
            ButtonData.IsEnabled = true;
            ButtonCandleConverter.IsEnabled = true;
            ButtonConverter.IsEnabled = true;
            ButtonOptimizer.IsEnabled = true;
            ButtonTesterLight.IsEnabled = true;
            ButtonRobotLight.IsEnabled = true;
            ButtonLocal_Ru.IsEnabled = true;
            ButtonLocal_Eng.IsEnabled = true;
        }

        #endregion

        private void ImageAlor2_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo("https://www.alorbroker.ru/open?pr=L0745") { UseShellExecute = true });
            }
            catch
            {
                // ignore
            }
        }

        private void ImageAlor_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            try
            {
                if (OsLocalization.CurLocalization == OsLocalization.OsLocalType.Ru)
                {
                    ImageAlor2.Visibility = Visibility.Collapsed;
                    ImageAlor.Visibility = Visibility.Visible;
                }
            }
            catch
            {
                // ignore
            }
        }

        private void ImageAlor_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            try
            {
                if (OsLocalization.CurLocalization == OsLocalization.OsLocalType.Ru)
                {
                    ImageAlor2.Visibility = Visibility.Visible;
                    ImageAlor.Visibility = Visibility.Collapsed;
                }
            }
            catch
            {
                // ignore
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            GlobalGUILayout.IsClosed = true;

            if (ProccesIsWorked == true)
            {
                ProccesIsWorked = false;

                if (this.IsVisible == false)
                {
                    _awaitUiBotsInfoLoading = new AwaitObject(OsLocalization.Trader.Label391, 100, 0, true);
                    AwaitUi ui = new AwaitUi(_awaitUiBotsInfoLoading);

                    Thread worker = new Thread(Await7Seconds);
                    worker.Start();

                    ui.ShowDialog();
                }
            }

            Thread.Sleep(500);

            Process.GetCurrentProcess().Kill();
        }

        AwaitObject _awaitUiBotsInfoLoading;

        private void Await7Seconds()
        {
            // Это нужно чтобы потоки сохраняющие данные в файловую систему штатно завершили свою работу
            // This is necessary for threads saving data to the file system to complete their work properly
            Thread.Sleep(7000);
            _awaitUiBotsInfoLoading.Dispose();
        }

        private void MainWindow_ContentRendered(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                Thread.Sleep(1000);
                try
                {
                    ChangeText();
                }
                catch
                {
                    // ignore
                }
            });

            // Populate the modules combo box after UI is fully rendered
            // Заполняем комбобокс модулей после полной отрисовки UI
            PopulateModulesComboBox();
        }

        private void ChangeText()
        {

            if (ImageGear.Dispatcher.CheckAccess() == false)
            {
                ImageGear.Dispatcher.Invoke(new Action(ChangeText));
                return;
            }

            Title = OsLocalization.MainWindow.Title;
            BlockDataLabel.Content = OsLocalization.MainWindow.BlockDataLabel;
            BlockTestingLabel.Content = OsLocalization.MainWindow.BlockTestingLabel;
            BlockTradingLabel.Content = OsLocalization.MainWindow.BlockTradingLabel;
            BlockModulesLabel.Content = OsLocalization.MainWindow.BlockModulesLabel;
            ButtonData.Content = OsLocalization.MainWindow.OsDataName;
            ButtonConverter.Content = OsLocalization.MainWindow.OsConverter;
            ButtonTester.Content = OsLocalization.MainWindow.OsTesterName;
            ButtonOptimizer.Content = OsLocalization.MainWindow.OsOptimizerName;

            ButtonRobot.Content = OsLocalization.MainWindow.OsBotStationName;
            ButtonCandleConverter.Content = OsLocalization.MainWindow.OsCandleConverter;

            ButtonTesterLight.Content = OsLocalization.MainWindow.OsTesterLightName;
            ButtonRobotLight.Content = OsLocalization.MainWindow.OsBotStationLightName;
            ModuleLoad.Content = OsLocalization.MainWindow.ModuleLoadButton;

           // if(OsLocalization.CurLocalization == OsLocalization.OsLocalType.Ru)
          //  {
          //      this.Height = 415;
           //     ImageAlor.Visibility = Visibility.Visible;
            //}
            //else
            //{
                this.Height = 315;
                ImageAlor.Visibility = Visibility.Collapsed;
                ImageAlor2.Visibility = Visibility.Collapsed;
           // }
        }

        /// <summary>
        /// check the version of dotnet
        /// проверить версию дотНет
        /// </summary>
        private bool CheckDotNetVersion()
        {
            using (RegistryKey ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey("SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v4\\Full\\"))
            {
                if (ndpKey == null)
                {
                    return false;
                }
                int releaseKey = Convert.ToInt32(ndpKey.GetValue("Release"));

                if (releaseKey >= 393295)
                {
                    //"4.6 or later";
                    return true;
                }
                if ((releaseKey >= 379893))
                {
                    //"4.5.2 or later";
                    return true;
                }
                if ((releaseKey >= 378675))
                {
                    //"4.5.1 or later";
                    return true;
                }
                if ((releaseKey >= 378389))
                {
                    MessageBox.Show(OsLocalization.MainWindow.Message4);
                    return false;
                }

                MessageBox.Show(OsLocalization.MainWindow.Message4);

                return false;
            }
        }

        private bool CheckOutSomeLibrariesNearby()
        {
            // проверяем чтобы пользователь не запустился с рабочего стола, но не ярлыком, а экзешником

            if(File.Exists("QuikSharp.dll") == false)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// check the permission of the program to create files in the directory
        /// проверяем разрешение программы создавать файлы в директории
        /// </summary>
        private bool CheckWorkWithDirectory()
        {
            try
            {

                if (!Directory.Exists("Engine"))
                {
                    Directory.CreateDirectory("Engine");
                }

                if (File.Exists("Engine\\checkFile.txt"))
                {
                    File.Delete("Engine\\checkFile.txt");
                }

                File.Create("Engine\\checkFile.txt");

                if (File.Exists("Engine\\checkFile.txt") == false)
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }


            return true;
        }

        private bool CheckAlreadyWorkEngine()
        {
            try
            {
                string myDirectory = Directory.GetCurrentDirectory();

                Process[] ps1 = System.Diagnostics.Process.GetProcesses();

                List<Process> process = new List<Process>();

                for (int i = 0; i < ps1.Length; i++)
                {
                    Process p = ps1[i];

                    try
                    {
                        string mainStr = p.MainWindowHandle.ToString();

                        if (mainStr == "0")
                        {
                            continue;
                        }

                        if (p.MainModule.FileName != ""
                            && p.Modules != null)
                        {
                            process.Add(p);
                        }
                    }
                    catch
                    {

                    }
                }

                int osEngineCount = 0;

                string myProgramPath = myDirectory + "\\OsEngine.exe";

                for (int i = 0; i < process.Count; i++)
                {
                    Process p = process[i];

                    for (int j = 0; p.Modules != null && j < p.Modules.Count; j++)
                    {
                        if (p.Modules[j].FileName == null)
                        {
                            continue;
                        }

                        if (p.Modules[j].FileName.EndsWith(myProgramPath))
                        {
                            osEngineCount++;
                        }
                    }
                }

                if (osEngineCount > 0)
                {
                    return false;
                }

                return true;
            }
            catch
            {
                return true;
            }
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            string message = OsLocalization.MainWindow.Message5 + " THREAD " + e.ExceptionObject;

            message = _startProgram + "  " + message;

            message = System.Reflection.Assembly.GetExecutingAssembly() + "\n" + message;

            _messageToCrashServer = "Crash% " + message;
            Thread worker = new Thread(SendMessageInCrashServer);
            worker.Start();

            if (PrimeSettingsMaster.RebootTradeUiLight == true &&
                RobotUiLight.IsRobotUiLightStart)
            {
                Reboot(message);
            }
            else
            {
                MessageBox.Show(message);
            }
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            string message = OsLocalization.MainWindow.Message5 + " TASK " + e.Exception.ToString();

            message = _startProgram + "  " + message;

            message = System.Reflection.Assembly.GetExecutingAssembly() + "\n" + message;

            _messageToCrashServer = "Crash% " + message;
            Thread worker = new Thread(SendMessageInCrashServer);
            worker.Start();

            if (PrimeSettingsMaster.RebootTradeUiLight == true &&
                RobotUiLight.IsRobotUiLightStart)
            {
                Reboot(message);
            }
            else
            {
                MessageBox.Show(message);
            }
        }

        private StartProgram _startProgram;

        private void Reboot(string message)
        {

            if (!CheckAccess())
            {
                Dispatcher.Invoke(() =>
                {
                    Reboot(message);
                });
                return;
            }

            App.app.Shutdown();
            Process process = new Process();
            process.StartInfo.FileName = Directory.GetCurrentDirectory() + "\\OsEngine.exe";
            process.StartInfo.Arguments = " -error " + message;
            process.Start();

            Process.GetCurrentProcess().Kill();
        }

        private void ButtonTesterCandleOne_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _startProgram = StartProgram.IsTester;
                Hide();
                TesterUi candleOneUi = new TesterUi();
                candleOneUi.ShowDialog();
                Close();
                ProccesIsWorked = false;
                Thread.Sleep(5000);
            }
            catch (Exception error)
            {
                MessageBox.Show(error.ToString());
            }
            Process.GetCurrentProcess().Kill();
        }

        private void ButtonTesterLight_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _startProgram = StartProgram.IsTester;
                Hide();
                TesterUiLight candleOneUi = new TesterUiLight();
                candleOneUi.ShowDialog();
                Close();
                ProccesIsWorked = false;
                Thread.Sleep(5000);
            }
            catch (Exception error)
            {
                MessageBox.Show(error.ToString());
            }
            Process.GetCurrentProcess().Kill();
        }

        private void ButtonRobotCandleOne_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _startProgram = StartProgram.IsOsTrader;
                Hide();
                RobotUi candleOneUi = new RobotUi();
                candleOneUi.ShowDialog();
                Close();
                ProccesIsWorked = false;
                Thread.Sleep(5000);
            }
            catch (Exception error)
            {
                MessageBox.Show(error.ToString());
            }
            Process.GetCurrentProcess().Kill();
        }

        private void ButtonRobotLight_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _startProgram = StartProgram.IsOsTrader;
                Hide();
                RobotUiLight candleOneUi = new RobotUiLight();
                candleOneUi.ShowDialog();
                Close();
                ProccesIsWorked = false;
                Thread.Sleep(5000);
            }
            catch (Exception error)
            {
                MessageBox.Show(error.ToString());
            }
            Process.GetCurrentProcess().Kill();
        }

        private void ButtonData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _startProgram = StartProgram.IsOsData;
                Hide();
                OsDataUi ui = new OsDataUi();
                ui.ShowDialog();
                Close();
                ProccesIsWorked = false;
                Thread.Sleep(5000);
            }
            catch (Exception error)
            {
                MessageBox.Show(error.ToString());
            }
            Process.GetCurrentProcess().Kill();
        }

        private void ButtonConverter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _startProgram = StartProgram.IsOsConverter;
                Hide();
                OsConverterUi ui = new OsConverterUi();
                ui.ShowDialog();
                Close();
                ProccesIsWorked = false;
                Thread.Sleep(10000);
            }
            catch (Exception error)
            {
                MessageBox.Show(error.ToString());
            }
            Process.GetCurrentProcess().Kill();
        }

        private void ButtonOptimizer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _startProgram = StartProgram.IsOsOptimizer;
                Hide();
                OptimizerUi ui = new OptimizerUi();
                ui.ShowDialog();
                Close();
                ProccesIsWorked = false;
                Thread.Sleep(10000);
            }
            catch (Exception error)
            {
                MessageBox.Show(error.ToString());
            }
            Process.GetCurrentProcess().Kill();
        }

        private async void ThreadAreaGreeting()
        {
            await Task.Delay(1000);
            double angle = 5;

            for (int i = 0; i < 7; i++)
            {
                RotatePic(angle);
                await Task.Delay(50);
                angle += 10;
            }

            for (int i = 0; i < 7; i++)
            {
                RotatePic(angle);
                await Task.Delay(100);
                angle += 10;
            }

            await Task.Delay(100);
            RotatePic(angle);

        }

        private void RotatePic(double angle)
        {
            if (ImageGear.Dispatcher.CheckAccess() == false)
            {
                ImageGear.Dispatcher.Invoke(new Action<double>(RotatePic), angle);
                return;
            }

            ImageGear.RenderTransform = new RotateTransform(angle, 12, 12);

        }

        private void ButtonSettings_Click(object sender, RoutedEventArgs e)
        {
            if (_settingsUi == null)
            {
                _settingsUi = new PrimeSettingsMasterUi();
                _settingsUi.Show();
                _settingsUi.Closing += delegate { _settingsUi = null; };
            }
            else
            {
                _settingsUi.Activate();
            }
        }

        private PrimeSettingsMasterUi _settingsUi;




        private void CandleConverter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Hide();
                OsCandleConverterUi ui = new OsCandleConverterUi();
                ui.ShowDialog();
                Close();
                ProccesIsWorked = false;
                Thread.Sleep(10000);
            }
            catch (Exception error)
            {
                MessageBox.Show(error.ToString());
            }
            Process.GetCurrentProcess().Kill();
        }

        private void CommandLineInterfaceProcess()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (Array.Exists(args, a => a.Equals("-robots")))
            {
                ButtonRobotCandleOne_Click(this, default);
            }
            else if (Array.Exists(args, a => a.Equals("-tester")))
            {
                ButtonTesterCandleOne_Click(this, default);
            }
            else if (Array.Exists(args, a => a.Equals("-robotslight")))
            {
                ButtonRobotLight_Click(this, default);
            }
            else if (Array.Exists(args, a => a.Equals("-error")) && PrimeSettingsMaster.RebootTradeUiLight)
            {

                CriticalErrorHandler.ErrorInStartUp = true;

                Array.ForEach(args, (a) => { CriticalErrorHandler.ErrorMessage += a; });

                new Task(() =>
                {
                    string messageError = String.Empty;

                    for (int i = 0; i < args.Length; i++)
                    {
                        messageError += args[i];
                    }

                    MessageBox.Show(messageError);

                }).Start();

                ButtonRobotLight_Click(this, default);
            }
        }

        private void ClearOptimizerWorkResults()
        {
            try
            {
                if (Directory.Exists("Engine") == false)
                {
                    return;
                }

                string[] files = Directory.GetFiles("Engine");

                for (int i = 0; i < files.Length; i++)
                {
                    try
                    {
                        if (files[i].Contains(" OpT "))
                        {
                            File.Delete(files[i]);
                        }
                    }
                    catch
                    {
                        // ignore
                    }

                }
            }
            catch
            {
                // ignore
            }
        }

        string _messageToCrashServer;

        private void SendMessageInCrashServer()
        {
            try
            {
                if(PrimeSettingsMaster.ReportCriticalErrors == false)
                {
                    return;
                }

                TcpClient newClient = new TcpClient();
                newClient.Connect("195.133.196.183", 11000);
                NetworkStream tcpStream = newClient.GetStream();
                byte[] sendBytes = Encoding.UTF8.GetBytes(_messageToCrashServer);
                tcpStream.Write(sendBytes, 0, sendBytes.Length);
                newClient.Close();
            }
            catch
            {
                // ignore
            }
        }

        private void ButtonLocal_Click(object sender, RoutedEventArgs e)
        {
            OsLocalization.OsLocalType newType;

            if (Enum.TryParse("Ru", out newType))
            {
                OsLocalization.CurLocalization = newType;
                Thread.CurrentThread.CurrentCulture = OsLocalization.CurCulture;

                ButtonLocal_Ru.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#ff5500");
                ButtonLocal_Eng.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF111217");
            }
        }

        private void ButtonLocal_Eng_Click(object sender, RoutedEventArgs e)
        {
            OsLocalization.OsLocalType newType;

            if (Enum.TryParse("Eng", out newType))
            {
                OsLocalization.CurLocalization = newType;
                Thread.CurrentThread.CurrentCulture = OsLocalization.CurCulture;

                ButtonLocal_Eng.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#ff5500");
                ButtonLocal_Ru.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF111217");
            }
        }

        private void ReloadFlagButton()
        {
            if (OsLocalization.CurLocalization == OsLocalization.OsLocalType.Ru)
            {
                ButtonLocal_Ru.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#ff5500");
            }
            else
            {
                ButtonLocal_Eng.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#ff5500");
            }
        }

        #region Modules ComboBox Management / Управление ComboBox Модулей

        private readonly Dictionary<string, ModuleInfo> _loadedModules = new Dictionary<string, ModuleInfo>();
        private readonly List<string> _moduleErrors = new List<string>();

        /// <summary>
        /// Information about a discovered module
        /// Информация об обнаруженном модуле
        /// </summary>
        private class ModuleInfo
        {
            public string Name { get; set; }
            public string ProjectPath { get; set; }
            public string AssemblyPath { get; set; }
            public bool IsBuilt { get; set; }
            public bool IsLoaded { get; set; }
            public string ErrorMessage { get; set; }
            public DateTime LastBuildTime { get; set; }
        }

        /// <summary>
        /// Populate the ModulesComboBox with available modules from the Modules directory
        /// Заполняет ModulesComboBox доступными модулями из директории Modules
        /// </summary>
        private void PopulateModulesComboBox()
        {
            try
            {
                if (ModulesComboBox == null)
                {
                    return;
                }

                ModulesComboBox.Items.Clear();
                _loadedModules.Clear();
                _moduleErrors.Clear();
                
                // Try multiple possible paths for the Modules directory
                // Пробуем несколько возможных путей для директории Modules
                string[] possiblePaths = {
                    // Current directory (when running from project folder)
                    // Текущая директория (при запуске из папки проекта)
                    Path.Combine(Directory.GetCurrentDirectory(), "OsEngine", "Modules"),
                    // Base directory (when running from bin/Debug)
                    // Базовая директория (при запуске из bin/Debug)
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OsEngine", "Modules"),
                    // Relative paths
                    // Относительные пути
                    Path.Combine(Directory.GetCurrentDirectory(), "Modules"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Modules"),
                    "Modules",
                    Path.Combine(Environment.CurrentDirectory, "Modules"),
                    // Look for Modules in the project root directory (two levels up from bin\Debug)
                    // Ищем Modules в корневой директории проекта (на два уровня выше от bin\Debug)
                    Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "Modules"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "Modules")
                };

                string modulesPath = null;
                Debug.WriteLine("Trying to find Modules directory...");
                foreach (string path in possiblePaths)
                {
                    Debug.WriteLine($"  Checking path: {path}");
                    if (Directory.Exists(path))
                    {
                        modulesPath = path;
                        Debug.WriteLine($"Found Modules directory at: {path}");
                        break;
                    }
                    else
                    {
                        Debug.WriteLine($"Path does not exist: {path}");
                    }
                }
                
                if (!string.IsNullOrEmpty(modulesPath) && Directory.Exists(modulesPath))
                {
                    Debug.WriteLine($"Scanning for modules in: {modulesPath}");
                    
                    string[] moduleFolders = Directory.GetDirectories(modulesPath);
                    Debug.WriteLine($"Found {moduleFolders.Length} directories in Modules folder");
                    
                    foreach (string folderPath in moduleFolders)
                    {
                        string folderName = Path.GetFileName(folderPath);
                        Debug.WriteLine($"  Examining folder: {folderName} at {folderPath}");
                        if (!string.IsNullOrEmpty(folderName))
                        {
                            var moduleInfo = DiscoverModule(folderPath, folderName);
                            if (moduleInfo != null)
                            {
                                _loadedModules[folderName] = moduleInfo;
                                ModulesComboBox.Items.Add(folderName);
                                Debug.WriteLine($"Discovered module: {folderName}");
                            }
                            else
                            {
                                Debug.WriteLine($"Failed to discover module: {folderName}");
                            }
                        }
                    }
                }
                
                // Set default selection if items exist
                // Устанавливаем выбор по умолчанию, если есть элементы
                if (ModulesComboBox.Items.Count > 0)
                {
                    ModulesComboBox.SelectedIndex = 0;
                    Debug.WriteLine($"Found {ModulesComboBox.Items.Count} modules");
                }
                else
                {
                    Debug.WriteLine("No modules found");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error populating modules: {ex.Message}");
            }
        }

        /// <summary>
        /// Discover a module and determine if it's a C# project
        /// Обнаруживает модуль и определяет, является ли он C# проектом
        /// </summary>
        private ModuleInfo DiscoverModule(string folderPath, string folderName)
        {
            try
            {
                // Look for .csproj files
                // Ищем файлы .csproj
                string[] csprojFiles = Directory.GetFiles(folderPath, "*.csproj", SearchOption.TopDirectoryOnly);
                
                if (csprojFiles.Length == 0)
                {
                    Debug.WriteLine($"{folderName}: No .csproj file found (folder only)");
                    return new ModuleInfo
                    {
                        Name = folderName,
                        ProjectPath = null,
                        AssemblyPath = null,
                        IsBuilt = false,
                        IsLoaded = false,
                        ErrorMessage = "No .csproj file found"
                    };
                }

                string projectPath = csprojFiles[0];
                Debug.WriteLine($"{folderName}: Found project file: {Path.GetFileName(projectPath)}");

                // Determine output assembly path
                // Определяем путь к выходной сборке
                string assemblyName = Path.GetFileNameWithoutExtension(projectPath);
                string assemblyPath = Path.Combine(folderPath, "bin", "Debug", "net9.0-windows", $"{assemblyName}.dll");
                
                // Check if assembly exists and is built
                // Проверяем, существует ли сборка и собрана ли она
                bool isBuilt = File.Exists(assemblyPath);
                DateTime lastBuildTime = isBuilt ? File.GetLastWriteTime(assemblyPath) : DateTime.MinValue;

                return new ModuleInfo
                {
                    Name = folderName,
                    ProjectPath = projectPath,
                    AssemblyPath = assemblyPath,
                    IsBuilt = isBuilt,
                    IsLoaded = false,
                    LastBuildTime = lastBuildTime
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error discovering module {folderName}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Build a specific module
        /// Собирает конкретный модуль
        /// </summary>
        private async Task<bool> BuildModuleAsync(string moduleName)
        {
            if (!_loadedModules.ContainsKey(moduleName))
            {
                Debug.WriteLine($"Module {moduleName} not found");
                return false;
            }

            var moduleInfo = _loadedModules[moduleName];
            if (string.IsNullOrEmpty(moduleInfo.ProjectPath))
            {
                Debug.WriteLine($"{moduleName}: No project file to build");
                return false;
            }

            try
            {
                Debug.WriteLine($"Building module: {moduleName}...");
                Debug.WriteLine($"  Project path: {moduleInfo.ProjectPath}");
                Debug.WriteLine($"  Working directory: {Path.GetDirectoryName(moduleInfo.ProjectPath)}");
                
                // Check if dotnet is available
                // Проверяем, доступен ли dotnet
                try
                {
                    var dotnetCheck = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = "--version",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    };
                    
                    using var checkProcess = new Process { StartInfo = dotnetCheck };
                    checkProcess.Start();
                    string version = checkProcess.StandardOutput.ReadToEnd().Trim();
                    checkProcess.WaitForExit();
                    
                    Debug.WriteLine($"  .NET SDK version: {version}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"  Warning: Could not check .NET SDK version: {ex.Message}");
                }
                
                // Check current environment
                // Проверяем текущую среду
                Debug.WriteLine($"  Current directory: {Directory.GetCurrentDirectory()}");
                Debug.WriteLine($"  Base directory: {AppDomain.CurrentDomain.BaseDirectory}");
                Debug.WriteLine($"  Environment current: {Environment.CurrentDirectory}");
                
                var startInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"build \"{moduleInfo.ProjectPath}\" --configuration Debug --verbosity normal",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.GetDirectoryName(moduleInfo.ProjectPath)
                };

                Debug.WriteLine($"  Command: {startInfo.FileName} {startInfo.Arguments}");

                using var process = new Process { StartInfo = startInfo };
                var output = new StringBuilder();
                var error = new StringBuilder();

                process.OutputDataReceived += (sender, e) => { 
                    if (e.Data != null) 
                    {
                        output.AppendLine(e.Data);
                        Debug.WriteLine($"  [BUILD OUTPUT] {e.Data}");
                    }
                };
                process.ErrorDataReceived += (sender, e) => { 
                    if (e.Data != null) 
                    {
                        error.AppendLine(e.Data);
                        Debug.WriteLine($"  [BUILD ERROR] {e.Data}");
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                
                await process.WaitForExitAsync();

                Debug.WriteLine($"  Build process exited with code: {process.ExitCode}");
                Debug.WriteLine($"  Output length: {output.Length}");
                Debug.WriteLine($"  Error length: {error.Length}");

                if (process.ExitCode == 0)
                {
                    // Check if assembly was created
                    // Проверяем, была ли создана сборка
                    if (File.Exists(moduleInfo.AssemblyPath))
                    {
                        moduleInfo.IsBuilt = true;
                        moduleInfo.LastBuildTime = File.GetLastWriteTime(moduleInfo.AssemblyPath);
                        moduleInfo.ErrorMessage = null;
                        Debug.WriteLine($"{moduleName}: Build successful");
                        return true;
                    }
                    else
                    {
                        moduleInfo.ErrorMessage = "Build succeeded but assembly not found";
                        Debug.WriteLine($"{moduleName}: {moduleInfo.ErrorMessage}");
                        Debug.WriteLine($"  Expected assembly path: {moduleInfo.AssemblyPath}");
                        return false;
                    }
                }
                else
                {
                    moduleInfo.ErrorMessage = $"Build failed with exit code {process.ExitCode}";
                    Debug.WriteLine($"{moduleName}: {moduleInfo.ErrorMessage}");
                    if (output.Length > 0)
                    {
                        Debug.WriteLine($"Build output: {output}");
                    }
                    if (error.Length > 0)
                    {
                        Debug.WriteLine($"Build errors: {error}");
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                moduleInfo.ErrorMessage = $"Build exception: {ex.Message}";
                Debug.WriteLine($"{moduleName}: {moduleInfo.ErrorMessage}");
                Debug.WriteLine($"  Exception details: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Load a module's assembly
        /// Загружает сборку модуля
        /// </summary>
        private bool LoadModuleAssembly(string moduleName)
        {
            if (!_loadedModules.ContainsKey(moduleName))
            {
                Debug.WriteLine($"Module {moduleName} not found");
                return false;
            }

            var moduleInfo = _loadedModules[moduleName];
            if (!moduleInfo.IsBuilt)
            {
                Debug.WriteLine($"{moduleName}: Module not built yet");
                return false;
            }

            try
            {
                Debug.WriteLine($"Loading assembly: {moduleName}...");
                
                // Load the assembly
                // Загружаем сборку
                var assembly = Assembly.LoadFrom(moduleInfo.AssemblyPath);
                
                // Look for module entry points (classes with specific attributes or names)
                // Ищем точки входа модуля (классы с определенными атрибутами или именами)
                var moduleTypes = assembly.GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract && 
                               (t.Name.EndsWith("Module") || t.Name.EndsWith("Plugin") || 
                                t.GetCustomAttributes().Any(a => a.GetType().Name.Contains("Module"))))
                    .ToList();

                if (moduleTypes.Any())
                {
                    Debug.WriteLine($"{moduleName}: Found {moduleTypes.Count} module types");
                    foreach (var type in moduleTypes)
                    {
                        Debug.WriteLine($"  - {type.FullName}");
                    }
                }
                else
                {
                    Debug.WriteLine($"{moduleName}: No specific module types found");
                }

                moduleInfo.IsLoaded = true;
                Debug.WriteLine($"{moduleName}: Assembly loaded successfully");
                return true;
            }
            catch (Exception ex)
            {
                moduleInfo.ErrorMessage = $"Load exception: {ex.Message}";
                Debug.WriteLine($"{moduleName}: {moduleInfo.ErrorMessage}");
                return false;
            }
        }

        /// <summary>
        /// Refresh the modules list (can be called when modules are added/removed)
        /// Обновляет список модулей (можно вызывать при добавлении/удалении модулей)
        /// </summary>
        public void RefreshModulesList()
        {
            if (ModulesComboBox.Dispatcher.CheckAccess() == false)
            {
                ModulesComboBox.Dispatcher.Invoke(new Action(RefreshModulesList));
                return;
            }
            
            PopulateModulesComboBox();
        }

        /// <summary>
        /// Handle module selection change
        /// Обрабатывает изменение выбора модуля
        /// </summary>
        private void ModulesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ModulesComboBox.SelectedItem != null)
            {
                string selectedModule = ModulesComboBox.SelectedItem.ToString();
                DisplayModuleInfo(selectedModule);
            }
        }

        /// <summary>
        /// Display information about the selected module
        /// Отображает информацию о выбранном модуле
        /// </summary>
        private void DisplayModuleInfo(string moduleName)
        {
            if (!_loadedModules.ContainsKey(moduleName))
            {
                Debug.WriteLine($"Module {moduleName} not found");
                return;
            }

            var moduleInfo = _loadedModules[moduleName];
            Debug.WriteLine($"Module Info: {moduleName}");
            Debug.WriteLine($"  Project: {moduleInfo.ProjectPath ?? "None"}");
            Debug.WriteLine($"  Assembly: {moduleInfo.AssemblyPath ?? "None"}");
            Debug.WriteLine($"  Built: {moduleInfo.IsBuilt}");
            Debug.WriteLine($"  Loaded: {moduleInfo.IsLoaded}");
            Debug.WriteLine($"  Last Build: {moduleInfo.LastBuildTime:yyyy-MM-dd HH:mm:ss}");
            
            if (!string.IsNullOrEmpty(moduleInfo.ErrorMessage))
            {
                Debug.WriteLine($"  Error: {moduleInfo.ErrorMessage}");
            }
        }

        /// <summary>
        /// Handle module load button click
        /// Обрабатывает нажатие кнопки загрузки модуля
        /// </summary>
        private async void ModuleLoad_Click(object sender, RoutedEventArgs e)
        {
            if (ModulesComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a module first.", "No Module Selected", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string selectedModule = ModulesComboBox.SelectedItem.ToString();
            await LoadSelectedModuleAsync(selectedModule);
        }

        /// <summary>
        /// Load the selected module based on its name
        /// Загружает выбранный модуль по его имени
        /// </summary>
        private async Task LoadSelectedModuleAsync(string moduleName)
        {
            try
            {
                Debug.WriteLine($"Loading module: {moduleName}...");
                
                // Step 1: Build the module if needed
                // Шаг 1: Собираем модуль, если нужно
                if (!_loadedModules[moduleName].IsBuilt)
                {
                    Debug.WriteLine($"Building {moduleName}...");
                    bool buildSuccess = await BuildModuleAsync(moduleName);
                    if (!buildSuccess)
                    {
                        Debug.WriteLine($"Failed to build {moduleName}");
                        return;
                    }
                }

                // Step 2: Load the assembly
                // Шаг 2: Загружаем сборку
                bool loadSuccess = LoadModuleAssembly(moduleName);
                if (loadSuccess)
                {
                    Debug.WriteLine($"Module {moduleName} loaded successfully!");
                    MessageBox.Show($"Module '{moduleName}' has been loaded successfully!", "Module Loaded", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    Debug.WriteLine($"Failed to load {moduleName}");
                    MessageBox.Show($"Failed to load module '{moduleName}'. Check the console for details.", "Load Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading module '{moduleName}': {ex.Message}");
                MessageBox.Show($"Error loading module '{moduleName}': {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        #endregion
    }


    public static class CriticalErrorHandler
    {
        public static string ErrorMessage = String.Empty;

        public static bool ErrorInStartUp = false;
    }

}
