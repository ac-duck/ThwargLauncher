using System;
using System.Collections.Generic;
using System.Linq;

namespace SteelBotLauncher
{
    /// <summary>
    /// Historical record of game configurations seen in this session
    /// </summary>
    public class Configurator
    {
        public class GameConfig
        {
            public string SteelFilterPath;
            public string SteelFilterVersion;
            public override string ToString()
            {
                return string.Format("SteelFilterVersion={0}, SteelFilterPath={1}", this.SteelFilterVersion, this.SteelFilterPath);
            }
        }

        private Dictionary<string, GameConfig> _map = new Dictionary<string, GameConfig>();
        public bool ContainsSteelFilterPath(string path) { return _map.ContainsKey(GetPathConfigKey(path)); }
        public void AddGameConfig(GameConfig config)
        {
            string key = GetConfigKey(config);
            if (!_map.ContainsKey(key))
            {
                _map[key] = config;
            }
        }
        public IList<GameConfig> GetGameConfigs()
        {
            return _map.Values.ToList();
        }
        public int GetNumberGameConfigs()
        {
            return _map.Keys.Count;
        }
        private string GetConfigKey(GameConfig config)
        {
            return GetPathConfigKey(config.SteelFilterPath);
        }
        private string GetPathConfigKey(string path)
        {
            return path.ToUpperInvariant();
        }
    }
}
