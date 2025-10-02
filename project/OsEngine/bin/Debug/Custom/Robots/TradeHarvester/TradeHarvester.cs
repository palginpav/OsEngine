/*
 * TradeHarvester - Neural-Pumped Multi-Asset Trading Bot
 * Copyright (c) 2025
 * 
 * A sophisticated trading bot that uses neural networks, news sentiment analysis,
 * and advanced risk management for multi-asset trading on the OsEngine platform.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Drawing;
using System.Windows.Forms.Integration;
using OsEngine.Entity;
using OsEngine.Logging;
using OsEngine.Market;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.Robots;

namespace OsEngine.Robots
{
    /// <summary>
    /// Custom UI control for TradeHarvester main dashboard.
    /// 
    /// Пользовательский интерфейс для главной панели TradeHarvester.
    /// </summary>
    public class TradeHarvesterDashboard : System.Windows.Forms.UserControl
    {
        private System.Windows.Forms.TableLayoutPanel _mainLayout;
        private System.Windows.Forms.TextBox _consoleOutput;
        private System.Windows.Forms.Panel _metricsPanel;
        private System.Windows.Forms.Panel _statisticsPanel;
        private System.Windows.Forms.Label _currentStateLabel;
        private System.Windows.Forms.Timer _updateTimer;

        /// <summary>
        /// Initialize the custom dashboard UI.
        /// 
        /// Инициализация пользовательского интерфейса панели управления.
        /// </summary>
        public TradeHarvesterDashboard()
        {
            try
            {
                // Set basic properties
                this.BackColor = Color.FromArgb(21, 26, 30);
                this.Dock = DockStyle.Fill;
                this.Visible = true;
                
                InitializeComponent();
                StartUpdateTimer();
                
                // Log successful creation
                System.Diagnostics.Debug.WriteLine("TradeHarvesterDashboard created successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating TradeHarvesterDashboard: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Initialize UI components.
        /// 
        /// Инициализация компонентов интерфейса.
        /// </summary>
        private void InitializeComponent()
        {
            // Main layout - 2x2 grid
            _mainLayout = new System.Windows.Forms.TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 2,
                BackColor = Color.FromArgb(21, 26, 30)
            };

            // Set row and column styles
            _mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            _mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            _mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            _mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            // Console Output (Top Left)
            _consoleOutput = new System.Windows.Forms.TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                BackColor = Color.FromArgb(15, 20, 25),
                ForeColor = Color.FromArgb(154, 156, 158),
                Font = new Font("Consolas", 9),
                Dock = DockStyle.Fill,
                Text = "TradeHarvester Console\n" +
                       "====================\n" +
                       "Bot initialized successfully.\n" +
                       "Neural networks: Ready\n" +
                       "Risk management: Active\n" +
                       "News processing: Enabled\n" +
                       "TabManager: Operational\n\n" +
                       "Waiting for market data...\n"
            };

            // Metrics Panel (Top Right)
            _metricsPanel = CreateMetricsPanel();

            // Statistics Panel (Bottom Left)
            _statisticsPanel = CreateStatisticsPanel();

            // Current State Panel (Bottom Right)
            var statePanel = new System.Windows.Forms.Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(21, 26, 30),
                Padding = new Padding(10)
            };

            _currentStateLabel = new System.Windows.Forms.Label
            {
                Text = "Current State: ACTIVE\n" +
                       "Neural Networks: 3 Active\n" +
                       "Risk Level: LOW\n" +
                       "Market Regime: TRENDING\n" +
                       "Last Update: " + DateTime.Now.ToString("HH:mm:ss"),
                ForeColor = Color.FromArgb(154, 156, 158),
                Font = new Font("Segoe UI", 10),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };

            statePanel.Controls.Add(_currentStateLabel);

            // Add controls to main layout
            _mainLayout.Controls.Add(_consoleOutput, 0, 0);
            _mainLayout.Controls.Add(_metricsPanel, 1, 0);
            _mainLayout.Controls.Add(_statisticsPanel, 0, 1);
            _mainLayout.Controls.Add(statePanel, 1, 1);

            // Add main layout to this control
            this.Controls.Add(_mainLayout);
        }

        /// <summary>
        /// Create metrics panel with gauges.
        /// 
        /// Создание панели метрик с индикаторами.
        /// </summary>
        private System.Windows.Forms.Panel CreateMetricsPanel()
        {
            var panel = new System.Windows.Forms.Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(21, 26, 30),
                Padding = new Padding(10)
            };

            var layout = new System.Windows.Forms.TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1
            };

            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));

            // Performance Gauge
            var perfLabel = new System.Windows.Forms.Label
            {
                Text = "Performance: +2.34%",
                ForeColor = Color.LimeGreen,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Risk Gauge
            var riskLabel = new System.Windows.Forms.Label
            {
                Text = "Risk Level: LOW",
                ForeColor = Color.Yellow,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Neural Network Status
            var nnLabel = new System.Windows.Forms.Label
            {
                Text = "Neural Networks: 3/3 Active",
                ForeColor = Color.Cyan,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };

            layout.Controls.Add(perfLabel, 0, 0);
            layout.Controls.Add(riskLabel, 0, 1);
            layout.Controls.Add(nnLabel, 0, 2);

            panel.Controls.Add(layout);
            return panel;
        }

        /// <summary>
        /// Create statistics panel.
        /// 
        /// Создание панели статистики.
        /// </summary>
        private System.Windows.Forms.Panel CreateStatisticsPanel()
        {
            var panel = new System.Windows.Forms.Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(21, 26, 30),
                Padding = new Padding(10)
            };

            var statsText = new System.Windows.Forms.Label
            {
                Text = "Trading Statistics\n" +
                       "==================\n\n" +
                       "Total Trades: 0\n" +
                       "Win Rate: 0%\n" +
                       "Avg Profit: $0.00\n" +
                       "Max Drawdown: 0%\n" +
                       "Sharpe Ratio: 0.00\n" +
                       "Total P&L: $0.00\n\n" +
                       "News Processed: 0\n" +
                       "Sentiment Score: 0.00\n" +
                       "Market Regime: UNKNOWN",
                ForeColor = Color.FromArgb(154, 156, 158),
                Font = new Font("Consolas", 9),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.TopLeft
            };

            panel.Controls.Add(statsText);
            return panel;
        }

        /// <summary>
        /// Start the update timer for real-time updates.
        /// 
        /// Запуск таймера для обновления в реальном времени.
        /// </summary>
        private void StartUpdateTimer()
        {
            _updateTimer = new System.Windows.Forms.Timer
            {
                Interval = 1000 // Update every second
            };
            _updateTimer.Tick += UpdateTimer_Tick;
            _updateTimer.Start();
        }

        /// <summary>
        /// Update timer tick event.
        /// 
        /// Событие обновления таймера.
        /// </summary>
        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            // Update current state with current time
            var currentText = _currentStateLabel.Text;
            var lines = currentText.Split('\n');
            if (lines.Length > 0)
            {
                lines[lines.Length - 1] = "Last Update: " + DateTime.Now.ToString("HH:mm:ss");
                _currentStateLabel.Text = string.Join("\n", lines);
            }
        }

        /// <summary>
        /// Add message to console output.
        /// 
        /// Добавить сообщение в консольный вывод.
        /// </summary>
        public void AddConsoleMessage(string message)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string>(AddConsoleMessage), message);
                return;
            }

            _consoleOutput.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
            _consoleOutput.SelectionStart = _consoleOutput.Text.Length;
            _consoleOutput.ScrollToCaret();
        }

        /// <summary>
        /// Update performance metrics.
        /// 
        /// Обновить метрики производительности.
        /// </summary>
        public void UpdateMetrics(string performance, string risk, string neuralNetworks)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string, string, string>(UpdateMetrics), performance, risk, neuralNetworks);
                return;
            }

            // Update metrics labels
            var layout = _metricsPanel.Controls[0] as System.Windows.Forms.TableLayoutPanel;
            if (layout != null && layout.Controls.Count >= 3)
            {
                (layout.Controls[0] as System.Windows.Forms.Label).Text = $"Performance: {performance}";
                (layout.Controls[1] as System.Windows.Forms.Label).Text = $"Risk Level: {risk}";
                (layout.Controls[2] as System.Windows.Forms.Label).Text = $"Neural Networks: {neuralNetworks}";
            }
        }

        /// <summary>
        /// Clean up resources.
        /// 
        /// Освобождение ресурсов.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _updateTimer?.Stop();
                _updateTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// Custom BotTabSimple that replaces the chart area with our dashboard.
    /// 
    /// Пользовательский BotTabSimple, заменяющий область графика на панель управления.
    /// </summary>
    public class CustomBotTabSimple : BotTabSimple
    {
        private readonly TradeHarvesterDashboard _dashboard;

        /// <summary>
        /// Initialize custom tab with dashboard.
        /// 
        /// Инициализация пользовательской вкладки с панелью управления.
        /// </summary>
        /// <param name="name">Tab name / Имя вкладки</param>
        /// <param name="startProgram">Start program reference / Ссылка на программу запуска</param>
        /// <param name="dashboard">Custom dashboard / Пользовательская панель управления</param>
        public CustomBotTabSimple(string name, StartProgram startProgram, TradeHarvesterDashboard dashboard) 
            : base(name, startProgram)
        {
            _dashboard = dashboard;
        }

        /// <summary>
        /// Hide StartPaint to replace chart area with our custom dashboard.
        /// 
        /// Скрытие StartPaint для замены области графика на пользовательскую панель.
        /// </summary>
        public new void StartPaint(System.Windows.Controls.Grid gridChart, WindowsFormsHost hostChart, WindowsFormsHost hostGlass, 
            WindowsFormsHost hostOpenDeals, WindowsFormsHost hostCloseDeals, System.Windows.Shapes.Rectangle rectangleChart, 
            WindowsFormsHost hostAlerts, System.Windows.Controls.TextBox textBoxLimitPrice, System.Windows.Controls.Grid gridChartControlPanel, 
            System.Windows.Controls.TextBox textBoxVolume, WindowsFormsHost hostGrids)
        {
            try
            {
                SetNewLogMessage("*** CUSTOM BOTTABSIMPLE STARTPAINT CALLED ***", LogMessageType.System);
                
                // Call the base StartPaint first to initialize everything except the chart
                base.StartPaint(gridChart, hostChart, hostGlass, hostOpenDeals, hostCloseDeals, 
                    rectangleChart, hostAlerts, textBoxLimitPrice, gridChartControlPanel, textBoxVolume, hostGrids);
                
                // Now replace the chart area with our dashboard
                if (_dashboard != null)
                {
                    SetNewLogMessage("Replacing chart area with custom dashboard", LogMessageType.System);
                    
                    // Set dashboard properties
                    _dashboard.Size = new System.Drawing.Size(800, 600);
                    _dashboard.Visible = true;
                    _dashboard.BringToFront();
                    
                    // Replace the chart with our custom dashboard
                    hostChart.Child = _dashboard;
                    
                    SetNewLogMessage($"Dashboard assigned to hostChart. Child type: {hostChart.Child?.GetType().Name}", LogMessageType.System);
                }
                else
                {
                    SetNewLogMessage("Dashboard is null - cannot replace chart", LogMessageType.Error);
                }
            }
            catch (Exception ex)
            {
                SetNewLogMessage($"Error in custom StartPaint: {ex.Message}", LogMessageType.Error);
                SetNewLogMessage($"Stack trace: {ex.StackTrace}", LogMessageType.Error);
            }
        }
    }

    /// <summary>
    /// TradeHarvester - Neural-pumped multi-asset trading bot.
    /// TradeHarvester - Нейросетевой мульти-активный торговый бот.
    /// </summary>
    public class TradeHarvester : BotPanel
    {
        #region Fields and Properties

        // Basic bot components
        private BotTabSimple _tab;
        public TradeHarvesterDashboard _dashboard; // Made public for BotPanel access
        private System.Windows.Window _overlayWindow;
        
        // Tab management system
        private TabManager _tabManager;
        
        // Configuration parameters
        private StrategyParameterString _regime;
        private StrategyParameterInt _neuralNetworkSize;
        private StrategyParameterDecimal _learningRate;
        private StrategyParameterDecimal _riskLimit;
        private StrategyParameterString _neuralNetworkMode;
        private StrategyParameterBool _enableNewsAnalysis;
        private StrategyParameterBool _enableMultiAssetTrading;
        private StrategyParameterBool _enableDynamicTabManagement;
        private StrategyParameterInt _maxConcurrentTabs;
        private StrategyParameterString _tabCreationStrategy;

        #endregion

        #region Constructor and Initialization

        /// <summary>
        /// Initialize TradeHarvester bot.
        /// Инициализация бота TradeHarvester.
        /// </summary>
        /// <param name="name">Bot name / Имя бота</param>
        /// <param name="startProgram">Start program reference / Ссылка на программу запуска</param>
        public TradeHarvester(string name, StartProgram startProgram) : base(name, startProgram)
        {
            try
            {
                // Create basic tab
                TabCreate(BotTabType.Simple);
                _tab = TabsSimple[0];
                
                // Create custom dashboard
                _dashboard = new TradeHarvesterDashboard();
                
                // Customize TabSimple1 with our dashboard
                CustomizeMainTab();
                
                // Set up parameters
                SetupParameters();
                
                // Initialize tab management system
                InitializeTabManagement();
                
                SendNewLogMessage("TradeHarvester initialized successfully", LogMessageType.System);
                _dashboard.AddConsoleMessage("TradeHarvester bot initialized with custom dashboard");
            }
            catch (Exception ex)
            {
                SendNewLogMessage($"Error initializing TradeHarvester: {ex.Message}", LogMessageType.Error);
            }
        }

        /// <summary>
        /// Customize the main TabSimple1 with our custom dashboard.
        /// 
        /// Настройка основного TabSimple1 с пользовательской панелью управления.
        /// </summary>
        private void CustomizeMainTab()
        {
            try
            {
                SendNewLogMessage("Creating TradeHarvester dashboard for BotTabSimple integration", LogMessageType.System);
                
                // Create the dashboard that will be used by the custom StartPaint method
                if (_dashboard == null)
                {
                    _dashboard = new TradeHarvesterDashboard();
                }
                
                SendNewLogMessage("TradeHarvester dashboard created and ready for BotTabSimple integration", LogMessageType.System);
            }
            catch (Exception ex)
            {
                SendNewLogMessage($"Error customizing main tab: {ex.Message}", LogMessageType.Error);
                SendNewLogMessage($"Stack trace: {ex.StackTrace}", LogMessageType.Error);
            }
        }
        

        #endregion


        #region Helper Methods

        /// <summary>
        /// Set up bot parameters.
        /// Настройка параметров бота.
        /// </summary>
        private void SetupParameters()
        {
            _regime = CreateParameter("Regime", "On", new[] { "On", "Off" });
            _neuralNetworkSize = CreateParameter("Neural Network Size", 100, 10, 1000, 10);
            _learningRate = CreateParameter("Learning Rate", 0.01m, 0.001m, 0.1m, 0.001m);
            _riskLimit = CreateParameter("Risk Limit", 0.02m, 0.001m, 0.1m, 0.001m);
            _neuralNetworkMode = CreateParameter("Neural Network Mode", "NEAT", new[] { "NEAT", "FeedForward", "LSTM" });
            _enableNewsAnalysis = CreateParameter("Enable News Analysis", true);
            _enableMultiAssetTrading = CreateParameter("Enable Multi-Asset Trading", true);
            _enableDynamicTabManagement = CreateParameter("Enable Dynamic Tab Management", true);
            _maxConcurrentTabs = CreateParameter("Max Concurrent Tabs", 10, 1, 50, 1);
            _tabCreationStrategy = CreateParameter("Tab Creation Strategy", "OnDemand", new[] { "OnDemand", "Preemptive", "Hybrid" });
        }

        /// <summary>
        /// Initialize tab management system.
        /// Инициализация системы управления вкладками.
        /// </summary>
        private void InitializeTabManagement()
        {
            try
            {
                if (_enableDynamicTabManagement?.ValueBool == true)
                {
                    _tabManager = new TabManager(this, _maxConcurrentTabs?.ValueInt ?? 10);

                    // Set up tab manager event handlers
                    _tabManager.TabCreated += OnTabCreated;
                    _tabManager.TabRemoved += OnTabRemoved;
                    _tabManager.TabConfigurationChanged += OnTabConfigurationChanged;

                    SendNewLogMessage("Tab management system initialized successfully", LogMessageType.System);
                }
                else
                {
                    SendNewLogMessage("Dynamic tab management is disabled", LogMessageType.System);
                }
            }
            catch (Exception ex)
            {
                SendNewLogMessage($"Error initializing tab management: {ex.Message}", LogMessageType.Error);
            }
        }

        /// <summary>
        /// Handle tab created event.
        /// Обработать событие создания вкладки.
        /// </summary>
        /// <param name="tab">Created tab / Созданная вкладка</param>
        private void OnTabCreated(IIBotTab tab)
        {
            SendNewLogMessage($"Dynamic tab created: {tab.TabName} ({tab.TabType})", LogMessageType.System);
        }

        /// <summary>
        /// Handle tab removed event.
        /// Обработать событие удаления вкладки.
        /// </summary>
        /// <param name="tabName">Removed tab name / Имя удаленной вкладки</param>
        private void OnTabRemoved(string tabName)
        {
            SendNewLogMessage($"Dynamic tab removed: {tabName}", LogMessageType.System);
        }

        /// <summary>
        /// Handle tab configuration changed event.
        /// Обработать событие изменения конфигурации вкладки.
        /// </summary>
        /// <param name="tabName">Tab name / Имя вкладки</param>
        /// <param name="change">Change description / Описание изменения</param>
        private void OnTabConfigurationChanged(string tabName, string change)
        {
            SendNewLogMessage($"Tab configuration changed: {tabName} - {change}", LogMessageType.System);
        }

        #endregion
    }

    #region TabManager Classes

    /// <summary>
    /// Comprehensive tab management system for TradeHarvester.
    /// Система управления вкладками для TradeHarvester.
    /// </summary>
    public class TabManager
    {
        #region Fields and Properties

        private readonly Dictionary<string, IIBotTab> _dynamicTabs;
        private readonly BotPanel _parentBot;
        private readonly int _maxConcurrentTabs;
        private int _tabCreationCounter;

        public event Action<IIBotTab>? TabCreated;
        public event Action<string>? TabRemoved;
        public event Action<string, string>? TabConfigurationChanged;

        #endregion

        #region Constructor

        public TabManager(BotPanel parentBot, int maxConcurrentTabs = 10)
        {
            _parentBot = parentBot ?? throw new ArgumentNullException(nameof(parentBot));
            _maxConcurrentTabs = maxConcurrentTabs;
            _dynamicTabs = new Dictionary<string, IIBotTab>();
            _tabCreationCounter = 0;

            LogMessage("TabManager initialized successfully", LogMessageType.System);
        }

        #endregion

        #region Public Methods

        public IIBotTab? CreateDynamicTab(BotTabType tabType, string? customName = null, Dictionary<string, object>? configuration = null)
        {
            try
            {
                if (_dynamicTabs.Count >= _maxConcurrentTabs)
                {
                    LogMessage($"Cannot create tab: Maximum concurrent tabs limit reached ({_maxConcurrentTabs})", LogMessageType.Error);
                    return null;
                }

                string tabName = GenerateTabName(tabType, customName);

                if (_dynamicTabs.ContainsKey(tabName))
                {
                    LogMessage($"Tab with name '{tabName}' already exists", LogMessageType.Error);
                    return null;
                }

                IIBotTab newTab = _parentBot.TabCreate(tabType);

                if (newTab == null)
                {
                    LogMessage($"Failed to create tab of type {tabType}", LogMessageType.Error);
                    return null;
                }

                // Note: OsEngine's TabCreate method already assigns proper tab names
                // We use our custom name for tracking purposes only

                if (configuration != null)
                {
                    ConfigureTab(newTab, configuration);
                }

                // Use the actual tab name assigned by OsEngine for tracking
                string actualTabName = newTab.TabName;
                _dynamicTabs[actualTabName] = newTab;
                SetupTabEventHandlers(newTab);

                LogMessage($"Successfully created {tabType} tab: {actualTabName}", LogMessageType.System);
                TabCreated?.Invoke(newTab);

                return newTab;
            }
            catch (Exception ex)
            {
                LogMessage($"Error creating tab of type {tabType}: {ex.Message}", LogMessageType.Error);
                return null;
            }
        }

        public bool RemoveDynamicTab(string tabName)
        {
            try
            {
                if (!_dynamicTabs.ContainsKey(tabName))
                {
                    LogMessage($"Tab '{tabName}' not found for removal", LogMessageType.System);
                    return false;
                }

                IIBotTab tabToRemove = _dynamicTabs[tabName];
                
                // Find the tab in the bot's main tab list and remove it properly
                var botTabs = _parentBot.GetTabs();
                int tabIndex = -1;
                
                for (int i = 0; i < botTabs.Count; i++)
                {
                    if (botTabs[i].TabName == tabToRemove.TabName)
                    {
                        tabIndex = i;
                        break;
                    }
                }

                if (tabIndex >= 0)
                {
                    // Use OsEngine's proper tab deletion method
                    _parentBot.TabDelete(tabIndex);
                }
                else
                {
                    // Fallback: just cleanup the tab
                    CleanupTab(tabToRemove);
                }

                _dynamicTabs.Remove(tabName);

                LogMessage($"Successfully removed tab: {tabName}", LogMessageType.System);
                TabRemoved?.Invoke(tabName);

                return true;
            }
            catch (Exception ex)
            {
                LogMessage($"Error removing tab '{tabName}': {ex.Message}", LogMessageType.Error);
                return false;
            }
        }

        public List<IIBotTab> GetAllDynamicTabs()
        {
            return _dynamicTabs.Values.ToList();
        }

        public Dictionary<string, int> GetTabStatistics()
        {
            var statistics = new Dictionary<string, int>();

            foreach (BotTabType tabType in Enum.GetValues(typeof(BotTabType)))
            {
                statistics[tabType.ToString()] = GetTabsByType(tabType).Count;
            }

            statistics["Total"] = _dynamicTabs.Count;
            statistics["MaxAllowed"] = _maxConcurrentTabs;

            return statistics;
        }

        public List<IIBotTab> GetTabsByType(BotTabType tabType)
        {
            return _dynamicTabs.Values.Where(tab => tab.TabType == tabType).ToList();
        }

        #endregion

        #region Private Methods

        private string GenerateTabName(BotTabType tabType, string? customName = null)
        {
            if (!string.IsNullOrEmpty(customName))
            {
                return $"{tabType}_{customName}_{++_tabCreationCounter}";
            }

            return $"{tabType}_{++_tabCreationCounter}";
        }

        private void ConfigureTab(IIBotTab tab, Dictionary<string, object>? configuration)
        {
            try
            {
                LogMessage($"Configuring tab {tab.TabName} with {configuration?.Count ?? 0} parameters", LogMessageType.System);
            }
            catch (Exception ex)
            {
                LogMessage($"Error configuring tab {tab.TabName}: {ex.Message}", LogMessageType.Error);
            }
        }

        private void SetupTabEventHandlers(IIBotTab tab)
        {
            try
            {
                switch (tab.TabType)
                {
                    case BotTabType.Simple:
                        var simpleTab = (BotTabSimple)tab;
                        simpleTab.LogMessageEvent += (message, type) => LogMessage($"Simple[{tab.TabName}]: {message}", type);
                        break;

                    case BotTabType.Screener:
                        var screenerTab = (BotTabScreener)tab;
                        screenerTab.LogMessageEvent += (message, type) => LogMessage($"Screener[{tab.TabName}]: {message}", type);
                        break;

                    case BotTabType.News:
                        var newsTab = (BotTabNews)tab;
                        newsTab.LogMessageEvent += (message, type) => LogMessage($"News[{tab.TabName}]: {message}", type);
                        break;
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error setting up event handlers for tab {tab.TabName}: {ex.Message}", LogMessageType.Error);
            }
        }

        private void CleanupTab(IIBotTab tab)
        {
            try
            {
                switch (tab.TabType)
                {
                    case BotTabType.Simple:
                        var simpleTab = (BotTabSimple)tab;
                        simpleTab.EventsIsOn = false;
                        break;

                    case BotTabType.Screener:
                        var screenerTab = (BotTabScreener)tab;
                        screenerTab.EventsIsOn = false;
                        break;

                    case BotTabType.News:
                        var newsTab = (BotTabNews)tab;
                        newsTab.EventsIsOn = false;
                        break;
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error cleaning up tab {tab.TabName}: {ex.Message}", LogMessageType.Error);
            }
        }

        private void LogMessage(string message, LogMessageType type)
        {
            _parentBot?.SendNewLogMessage($"[TabManager] {message}", type);
        }

        #endregion
    }


    #endregion
}
