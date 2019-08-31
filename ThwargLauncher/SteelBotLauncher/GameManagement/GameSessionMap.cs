﻿using System;
using System.Collections.Generic;
using System.Text;

namespace SteelBotLauncher
{
    /// <summary>
    /// The GameSessionMap contains a list of running games 
    /// Which can be searched by process id or by server+account or by character name
    /// </summary>
    class GameSessionMap
    {
        public delegate void CommandsReceivedHandler(GameSession session);
        public event CommandsReceivedHandler CommandsReceivedEvent;

        private static object _locker = new object();
        // Member data
        private Dictionary<string, GameSession> _sessionByProcessId = new Dictionary<string, GameSession>();
        private Dictionary<string, GameSession> _sessionByServerAccount = new Dictionary<string, GameSession>();
        private Dictionary<string, List<GameSession>> _sessionsByCharacterName = new Dictionary<string,List<GameSession>>();

        public static string GetProcessIdKey(int pid)
        {
            return string.Format("{0}", pid);
        }
        public void AddGameSession(GameSession gameSession)
        {
            lock (_locker)
            {
                // #1 By ProcessId
                string pidkey = gameSession.ProcessIdKey;
                if (_sessionByProcessId.ContainsKey(pidkey))
                {
                    // If called from TryToAddGameFromHeartbeatFile, then process id was just added by UpdateGameSessionFromHeartbeatStatus
                }
                else
                {
                    _sessionByProcessId.Add(pidkey, gameSession);
                }
                // #2 By Server/Account
                string key = GetServerAccountKey(gameSession);
                if (_sessionByServerAccount.ContainsKey(key))
                {
                    Logger.WriteError(string.Format("Duplicate server/account in AddGameSession: {0}", key));
                }
                else
                {
                    _sessionByServerAccount.Add(key, gameSession);
                }
                // #3 By character
                if (gameSession.CharacterName != null)
                {
                    List<GameSession> sessionList = null;
                    if (_sessionsByCharacterName.ContainsKey(gameSession.CharacterName))
                    {
                        sessionList = _sessionsByCharacterName[gameSession.CharacterName];
                    }
                    else
                    {
                        sessionList = new List<GameSession>();
                    }
                    sessionList.Add(gameSession);
                    _sessionsByCharacterName[gameSession.CharacterName] = sessionList;
                }
                StartSessionWatcher(gameSession);
            }
        }
        public void StartSessionWatcher(GameSession gameSession)
        {
            if (gameSession.GameChannel != null)
            {
                var writer = new SteelFilter.Channels.ChannelWriter();
                if (!writer.IsWatcherEnabled(gameSession.GameChannel))
                {
                    writer.StartWatcher(gameSession.GameChannel);
                    gameSession.GameChannel.FileWatcher.Changed += (sender, e) => OnChannelFileChanged(gameSession, sender, e);
                }
            }
        }

        void OnChannelFileChanged(GameSession session, object sender, System.IO.FileSystemEventArgs e)
        {
            if (CommandsReceivedEvent != null)
            {
                CommandsReceivedEvent(session);
            }
        }
        public IEnumerable<string> GetAllProcessIdKeys()
        {
            var keys = this._sessionByProcessId.Keys;
            return keys;
        }
        public void SetGameSessionProcessId(GameSession gameSession, int processId)
        {
            lock (_locker)
            {
                if (gameSession.ProcessId == processId) { return; }
                // This should only occur with starting game sessions
                // when they find their process id after finding their heartbeat file
                if (gameSession.ProcessId != 0)
                {
                    // This should not occur
                    // ProcessId should only change when a starting game session with process id 0 (unknown)
                    // finds its heartbeat file the first time
                }
                string pidkey = gameSession.ProcessIdKey;
                if (pidkey == null)
                {
                    // this is a new gameSession just created to recieve this discovered game, and not yet in map
                }
                else
                {
                    _sessionByProcessId.Remove(pidkey);
                }
                gameSession.ProcessId = processId;
                pidkey = GetProcessIdKey(processId);
                gameSession.ProcessIdKey = pidkey;
                _sessionByProcessId.Add(pidkey, gameSession);
            }
        }
        public bool HasGameSessionByProcessId(int processId)
        {
            return GetGameSessionByProcessId(processId) != null;
        }
        public GameSession GetGameSessionByProcessIdKey(string pidkey)
        {
            return GetGameSessionByProcessIdImpl(pidkey);
        }
        public GameSession GetGameSessionByProcessId(int processId)
        {
            string pidkey = GetProcessIdKey(processId);
            return GetGameSessionByProcessIdImpl(pidkey);
        }
        private GameSession GetGameSessionByProcessIdImpl(string pidkey)
        {
            lock (_locker)
            {
                if (_sessionByProcessId.ContainsKey(pidkey))
                {
                    return _sessionByProcessId[pidkey];
                }
                else
                {
                    return null;
                }
            }
        }
        public ServerAccountStatusEnum GetGameSessionStateByServerAccount(string serverName, string accountName)
        {
            var gameSession = GetGameSessionByServerAccount(serverName, accountName);
            if (gameSession == null)
            {
                return ServerAccountStatusEnum.None;
            }
            else
            {
                return gameSession.Status;
            }
        }
        public GameSession GetGameSessionByServerAccount(string serverName, string accountName)
        {
            lock (_locker)
            {
                return GetGameSessionByServerAccountImplUnlocked(serverName, accountName);
            }
        }
        private GameSession GetGameSessionByServerAccountImplUnlocked(string serverName, string accountName)
        {
            string key = GetServerAccountKey(serverName, accountName);
            if (_sessionByServerAccount.ContainsKey(key))
            {
                return _sessionByServerAccount[key];
            }
            else
            {
                return null;
            }
        }
        public List<GameSession> GetGameSessionsByCharacterName(string characterName)
        {
            if (_sessionsByCharacterName.ContainsKey(characterName))
            {
                return _sessionsByCharacterName[characterName];
            }
            else
            {
                return new List<GameSession>();
            }
        }
        public List<GameSession> GetAllGameSessions()
        {
            var allStatuses = new List<GameSession>();
            lock (_locker)
            {
                allStatuses.AddRange(_sessionByProcessId.Values);
            }
            return allStatuses;
        }
        public void RemoveGameSessionByProcessId(int processId)
        {
            string pidkey = GetProcessIdKey(processId);
            RemoveGameSessionByProcessIdKey(pidkey);
        }
        public GameSession RemoveGameSessionByProcessIdKey(string pidkey)
        {
            lock (_locker)
            {
                if (!_sessionByProcessId.ContainsKey(pidkey))
                {
                    return null;
                }
                GameSession gameSession = _sessionByProcessId[pidkey];
                // #1 By ProcessId
                _sessionByProcessId.Remove(pidkey);
                // #2 By Server/Account
                _sessionByServerAccount.Remove(GetServerAccountKey(gameSession));
                // #3 By Character Name
                if (!string.IsNullOrEmpty(gameSession.CharacterName) && _sessionsByCharacterName.ContainsKey(gameSession.CharacterName))
                {
                    _sessionsByCharacterName[gameSession.CharacterName].Remove(gameSession);
                }
                return gameSession;
            }
        }
        public GameSession StartLaunchingSession(string serverName, string accountName)
        {
            lock (_locker)
            {
                var gameSession = GetGameSessionByServerAccountImplUnlocked(serverName, accountName);
                if (gameSession != null)
                {
                    gameSession.Status = ServerAccountStatusEnum.Starting;
                }
                else
                {
                    gameSession = new GameSession();
                    gameSession.ProcessId = 0;
                    gameSession.ProcessIdKey = Guid.NewGuid().ToString();
                    gameSession.ServerName = serverName;
                    gameSession.AccountName = accountName;
                    gameSession.Status = ServerAccountStatusEnum.Starting;
                    AddGameSession(gameSession);
                }
                return gameSession;
            }
        }
        public void EndLaunchingSession(string serverName, string accountName)
        {
            lock (_locker)
            {
                var gameSession = GetGameSessionByServerAccountImplUnlocked(serverName, accountName);
                if (gameSession != null)
                {
                    if (gameSession.Status == ServerAccountStatusEnum.Starting)
                    {
                        // If it never made it out of starting, then it should be set to warning
                        gameSession.Status = ServerAccountStatusEnum.Warning;
                    }
                    Logger.WriteDebug("Ended launching game {0} in status {1}", gameSession.Description, gameSession.Status);
                }
                else
                {
                    Logger.WriteDebug("Ended launching srv {0} acct {1} and cannot find game session", serverName, accountName);
                }
            }
        }
        public void EndAllLaunchingSessions()
        {
            lock (_locker)
            {
                foreach (var gameSession in _sessionByProcessId.Values)
                {
                    if (gameSession.Status == ServerAccountStatusEnum.Starting)
                    {
                        gameSession.Status = ServerAccountStatusEnum.Warning;
                    }
                }
            }
        }
        /// <summary>
        /// Return latest launch times for all accounts
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, DateTime> GetLaunchAccountTimes()
        {
            var accountLaunchTimes = new Dictionary<string, DateTime>();
            lock (_locker)
            {
                foreach (var gameSession in _sessionByProcessId.Values)
                {
                    if (gameSession.UptimeSeconds == -1) { continue; }
                    DateTime launch = DateTime.UtcNow - TimeSpan.FromSeconds(gameSession.UptimeSeconds);
                    if(gameSession.AccountName == null)
                    {
                        continue;
                    }
                    if (!accountLaunchTimes.ContainsKey(gameSession.AccountName)
                        || launch > accountLaunchTimes[gameSession.AccountName])
                    {
                        accountLaunchTimes[gameSession.AccountName] = launch;
                    }
                }

            }
            return accountLaunchTimes;
        }

        private string GetServerAccountKey(GameSession gameSession)
        {
            return GetServerAccountKey(gameSession.ServerName, gameSession.AccountName);
        }
        private string GetServerAccountKey(string serverName, string accountName)
        {
            string key = string.Format("{0}:{1}", serverName, accountName);
            return key;
        }
    }
}
