using System;
using System.Runtime.Serialization;

namespace SteelBotLauncher.WebService
{
    [DataContract]
    public class GameCommand
    {
        [DataMember]
        public string Command { get; set; }
    }
}
