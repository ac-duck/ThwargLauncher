using System;

using Filter.Shared;

namespace SteelFilter
{
    class SteelFilterCommandExecutor
    {
        public void ExecuteCommand(string command)
        {
            if (!string.IsNullOrEmpty(command))
            {
                DecalProxy.DispatchChatToBoxWithPluginIntercept(command);
            }
        }
    }
}
