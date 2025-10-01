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
            
        }

        private string _botLocker = "botLocker";

        public BotPanel GetBot(string botType, string botName)
        {
            BotPanel bot = null;
            int waitCount = 0;
            DateTime startTime = DateTime.Now;
            const int timeoutSeconds = 30; // 30 second timeout

            while (true)
            {
                waitCount++;
                
                // Check for timeout
                if (startTime.AddSeconds(timeoutSeconds) < DateTime.Now)
                {
                    SendLogMessage($"GetBot: Timeout waiting for bot {botName} after {timeoutSeconds} seconds", LogMessageType.Error);
                    return null;
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

                        return bot;
                    }
                }
                
                // Increase sleep time progressively to reduce CPU usage
                if (waitCount < 100)
                {
                    Thread.Sleep(1);
                }
                else if (waitCount < 1000)
                {
                    Thread.Sleep(10);
                }
                else
                {
                    Thread.Sleep(100);
                }
            }
        }

        public void CreateNewBots(List<string> botsName, string botType, bool isScript, StartProgram startProgram)
        {
            _botType = botType;
            _isActivate = false;
            
            // Clear existing bot names to prevent conflicts
            lock (_botLocker)
            {
                for (int i = 0; i < _botsToStart.Count; i++)
                {
                    _botsToStart[i].Clear();
                }
            }
            
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
        }

        /// <summary>
        /// Create a single bot synchronously for genetic algorithm use.
        /// Создать одного бота синхронно для использования в генетическом алгоритме.
        /// </summary>
        /// <param name="botName">Bot name / Имя бота</param>
        /// <param name="botType">Bot type / Тип бота</param>
        /// <param name="isScript">Is script / Является ли скриптом</param>
        /// <param name="startProgram">Start program / Программа запуска</param>
        /// <returns>Created bot panel / Созданная панель бота</returns>
        public BotPanel CreateSingleBot(string botName, string botType, bool isScript, StartProgram startProgram)
        {
            try
            {
                BotPanel bot = BotFactory.GetStrategyForName(botType, botName, startProgram, isScript);
                
                if (bot != null)
                {
                    lock (_botLocker)
                    {
                        _bots.Add(bot);
                    }
                }
                
                return bot;
            }
            catch (Exception ex)
            {
                SendLogMessage($"CreateSingleBot: Exception creating bot {botName}: {ex.Message}", LogMessageType.Error);
                return null;
            }
        }

        /// <summary>
        /// Get a bot directly by name without waiting (for synchronous creation).
        /// Получить бота напрямую по имени без ожидания (для синхронного создания).
        /// </summary>
        /// <param name="botType">Bot type / Тип бота</param>
        /// <param name="botName">Bot name / Имя бота</param>
        /// <returns>Bot panel or null / Панель бота или null</returns>
        public BotPanel GetBotDirect(string botType, string botName)
        {
            lock (_botLocker)
            {
                for (int i = 0; i < _bots.Count; i++)
                {
                    if (_bots[i] == null)
                    {
                        continue;
                    }

                    if (_bots[i].NameStrategyUniq == botName &&
                        _bots[i].GetNameStrategyType() == botType)
                    {
                        BotPanel bot = _bots[i];
                        _bots.RemoveAt(i);
                        return bot;
                    }
                }
            }
            return null;
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
            while (true)
            {
                string botName = null;
                
                // Thread-safe removal of first element
                lock (_botLocker)
                {
                    if (names.Count == 0)
                    {
                        break;
                    }
                    botName = names[0];
                    names.RemoveAt(0);
                }
                
                if (string.IsNullOrEmpty(botName))
                {
                    continue;
                }
                
                BotPanel bot = null;

                try
                {
                    bot = BotFactory.GetStrategyForName(_botType, botName, _startProgram, _isScript);
                    
                    if (bot == null)
                    {
                        SendLogMessage($"Load: Failed to create bot {botName}", LogMessageType.Error);
                    }
                }
                catch (Exception ex)
                {
                    SendLogMessage($"Load: Exception creating bot {botName}: {ex.Message}", LogMessageType.Error);
                }

                if (bot != null)
                {
                    lock (_botLocker)
                    {
                        _bots.Add(bot);
                    }
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