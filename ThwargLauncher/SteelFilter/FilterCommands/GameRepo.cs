﻿using System;
using System.Text;

namespace SteelFilter
{
    /// <summary>
    /// Implement singleton storage of game info
    /// Currently has no thread locks
    /// </summary>
    class GameRepo
    {
        private static GameRepo Instance = new GameRepo();
        public static GameRepo Game { get { return Instance; } }
        private string _server = "";
        private string _account = "";
        private string _character = "";
        public string Server { get { return _server; } }
        public string Account { get { return _account; } }
        public string Character { get { return _character; } }
        public void SetAccount(string account)
        {
            _account = account;
        }
        public void SetServer(string server)
        {
            _server = server;
        }
        public void SetServerAccount(string server, string account)
        {
            // Probably not needed 2018-12-18
            _server = server;
            _account = account;
        }
        public void SetCharacter(string character)
        {
            _character = character;
        }
    }
}
