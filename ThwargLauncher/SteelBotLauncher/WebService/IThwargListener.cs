using System;
using System.Collections.Generic;
using System.Text;
using System.ServiceModel;


namespace SteelBotLauncher.WebService
{
    [ServiceContract(Namespace="http://www.thwargle.com/SteelBotLauncher/WebService")]
    public interface IThwargListener
    {
        [OperationContract]
        List<GameSetting> GetConfigurationSettings(string Account, string Server);
        [OperationContract]
        List<GameCommand> CheckIn(string Account, string Server);
    }
}
