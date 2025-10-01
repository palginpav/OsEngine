/*
 * Your rights to use code governed by this license https://github.com/AlexWan/OsEngine/blob/master/LICENSE
 * Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using System.Collections.Generic;
using System.Threading;
using OsEngine.Entity;
using OsEngine.OsTrader.Panels;
using OsEngine.Robots;
using System;
using OsEngine.Logging;
using OsEngine.Performance;

namespace OsEngine.OsOptimizer.OptimizerEntity
{
    public class AsyncBotFactory
    {
        public AsyncBotFactory()
        {
            // Dynamic thread count based on processor count, with reasonable limits
            int optimalThreadCount = Math.Min(Environment.ProcessorCount * 2, 20); // Cap at 20 for safety
            if (optimalThreadCount <= 0) optimalThreadCount = 4; // Minimum of 4 threads
            
            for (int i = 0; i < optimalThreadCount; i++)
            {
                _botsToStart.Add(CollectionOptimizer.CreateListWithCapacity<string>(16));
                Thread worker = new Thread(WorkerArea);
                worker.Name = i.ToString();
                worker.IsBackground = true; // Mark as background thread
                worker.Start();
            }
            
            // Log thread configuration for performance monitoring
            SendLogMessage($"AsyncBotFactory thread configuration: {optimalThreadCount} threads (CPU cores: {Environment.ProcessorCount})", LogMessageType.System);
        }

        private string _botLocker = "botLocker";

        public BotPanel GetBot(string botType, string botName)
        {
            SendLogMessage($"AsyncBotFactory.GetBot: Starting with botType={botType}, botName={botName}", LogMessageType.System);
            BotPanel bot = null;
            int waitCount = 0;

            while (true)
            {
                waitCount++;
                
                // Log progress every 5 seconds
                if (waitCount % 5000 == 0)
                {
                    SendLogMessage($"AsyncBotFactory.GetBot: Still waiting for bot... ({waitCount}ms), _bots.Count={_bots.Count}, _isActivate={_isActivate}", LogMessageType.System);
                }
                
                for (int i = 0; i < _bots.Count; i++)
                {
                    if (_bots[i] == null)
                    {
                        continue;
                    }

                    if (_bots[i].NameStrategyUniq == botName &&
                        _bots[i].GetNameStrategyType() == botType)
                    {
                        lock (_botLocker)
                        {
                            bot = _bots[i];
                            _bots.RemoveAt(i);
                        }

                        SendLogMessage($"AsyncBotFactory.GetBot: Found and returning bot for {botName}", LogMessageType.System);
                        return bot;
                    }
                }
                Thread.Sleep(1);
            }
        }

        public void CreateNewBots(List<string> botsName, string botType, bool isScript, StartProgram startProgram)
        {
            SendLogMessage($"AsyncBotFactory.CreateNewBots: Starting with {botsName.Count} bots, botType={botType}", LogMessageType.System);
            _botType = botType;
            _isActivate = false;
            for (int i = 0; i < _botsToStart.Count; i++)
            {
                List<string> names = _botsToStart[i];

                for (int i2 = i; i2 < botsName.Count; i2 += _botsToStart.Count)
                {
                    names.Add(botsName[i2]);
                }
            }

            _isScript = isScript;
            _startProgram = startProgram;
            _isActivate = true;
            SendLogMessage($"AsyncBotFactory.CreateNewBots: Activated bot creation for {botsName.Count} bots", LogMessageType.System);
        }

        private bool _isActivate;

        private List<List<string>> _botsToStart = new List<List<string>>();

        public List<BotPanel> _bots = new List<BotPanel>();

        private string _botType;

        private bool _isScript;

        private StartProgram _startProgram;

        private void WorkerArea()
        {
            // Safely get thread index from thread name
            int num = 0;
            if (!int.TryParse(Thread.CurrentThread.Name, out num))
            {
                // Fallback: find thread index by matching thread reference
                for (int i = 0; i < _botsToStart.Count; i++)
                {
                    if (Thread.CurrentThread.Name == i.ToString())
                    {
                        num = i;
                        break;
                    }
                }
            }

            while (true)
            {
                try
                {
                    Thread.Sleep(10);
                    if (MainWindow.ProccesIsWorked == false)
                    {
                        return;
                    }

                    if (_isActivate == false)
                    {
                        continue;
                    }

                    if (_botsToStart[num].Count != 0)
                    {
                        Load(_botsToStart[num]);
                    }
                }
                catch (Exception e)
                {
                    SendLogMessage("Optimizer critical error. \n Can`t create bot. Error: " + e.ToString(), LogMessageType.Error);
                    Thread.Sleep(1000);
                }
            }
        }

        private void Load(List<string> names)
        {
            while (names.Count != 0)
            {

                BotPanel bot = BotFactory.GetStrategyForName(_botType, names[0], _startProgram, _isScript);

                try
                {
                    names.RemoveAt(0);
                }
                catch
                {
                    // ignore
                }

                lock (_botLocker)
                {
                    _bots.Add(bot);
                }
            }
        }

        public void SendLogMessage(string message, LogMessageType type)
        {
            if (LogMessageEvent != null)
            {
                LogMessageEvent(message, type);
            }
        }

        public event Action<string, LogMessageType> LogMessageEvent;
    }
}