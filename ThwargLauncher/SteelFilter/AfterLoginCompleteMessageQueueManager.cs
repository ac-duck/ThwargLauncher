using System;
using System.Collections.Generic;

using Filter.Shared;

using Decal.Adapter;
using Decal.Adapter.Wrappers;
using System.Threading;

namespace SteelFilter
{
    class AfterLoginCompleteMessageQueueManager
    {
        private string STEELBOT_MANAGER = "Steelhead Trout";
        private string STEELBOT_MANAGER_2 = "Steelhead Trout II";
        private string STEELBOT_MANAGER_3 = "Steelhead Trout III";
        private string STEELBOT_MANAGER_4 = "Steelhead Trout IV";

        bool freshLogin;

        enum STEELBOT_STATE
        {
            CREATING_CHARACTER = 0,
            LOGGING_IN,
            HELLO,
            CHECKING_DOOR,
            DELAY_DOOR_ID,
            OPENING_DOOR,
            APPROACHING_SAM,
            TALKING_TO_JONATHAN,
            WAITING_FOR_EXIT_TOKEN,
            ESCAPING,
            DELAY1,
            PORTALING,
            MOVING_TO_CHEST,
            TURNING_IN_PATHWARDEN_TOKEN,
            WAITING_FOR_KEY,
            APPROACHING_CHEST,
            UNLOCKING_CHEST,
            WAITING_FOR_CHEST_TO_OPEN,
            CHEST_UNLOCKED,
            OPENING_CHEST,
            LOOTING_CHEST,
            CLOSING_CHEST,
            TURNING_IN_PATHWARDEN_ARMOR,
            GIVING_STEEL,
            LOGGING_OUT,
            DELETING_CHARACTER,
            ACCOUNT_FULL_WAITING_FOR_DELETE
        }

        STEELBOT_STATE current_state = STEELBOT_STATE.HELLO;
        STEELBOT_STATE next_state = STEELBOT_STATE.HELLO;
        bool action_completed = true;

        DateTime loginCompleteTime = DateTime.MaxValue;

        int aco_greeter = 0;
        int aco_door = 0;
        int aco_sign = 0;
        int aco_jonathan = 0;
        int aco_pathwarden_chest = 0;
        int aco_chest_container;
        int aco_exit_token = 0;
        int aco_pathwarden_token = 0;
        int aco_steelbot_manager = 0;
        int aco_pathwarden_key = 0;
        int aco_pathwarden = 0;

        bool is_door_open = false;

        void clearObjects()
        {
            aco_greeter = 0;
            aco_door = 0;
            aco_sign = 0;
            aco_jonathan = 0;
            aco_pathwarden_chest = 0;
            aco_chest_container = 0;
            aco_exit_token = 0;
            aco_pathwarden_token = 0;
            aco_pathwarden = 0;
            aco_steelbot_manager = 0;
            aco_pathwarden_key = 0;
        }

        void initHandlers()
        {
            clearObjects();
            Debug.LogText("Starting Steel Macro!");
            CoreManager.Current.ContainerOpened += Current_ContainerOpened;
            CoreManager.Current.CharacterFilter.ActionComplete += CharacterFilter_ActionComplete;
            CoreManager.Current.WorldFilter.ChangeObject += WorldFilter_ChangeObject;
            CoreManager.Current.WorldFilter.CreateObject += WorldFilter_CreateObject;
        }

        void unregisterHandlers()
        {
            clearObjects();
            Debug.LogText("Logging out!");
            CoreManager.Current.ContainerOpened -= Current_ContainerOpened;
            CoreManager.Current.CharacterFilter.ActionComplete -= CharacterFilter_ActionComplete;
            CoreManager.Current.WorldFilter.ChangeObject -= WorldFilter_ChangeObject;
            CoreManager.Current.WorldFilter.CreateObject -= WorldFilter_CreateObject;
            steelRunTimer.Tick -= SteelRunTimer_Tick;
        }

        private void WorldFilter_CreateObject(object sender, CreateObjectEventArgs e)
        {            
            if (e.New.Name == "Pathwarden Supply Key")
            {
                current_state = STEELBOT_STATE.UNLOCKING_CHEST;
                action_completed = true;
            }

            if (e.New.Name == "Academy Exit Token")
            {
                current_state = STEELBOT_STATE.ESCAPING;
                action_completed = true;
            }
        }

        private void Current_ContainerOpened(object sender, ContainerOpenedEventArgs e)
        {
            if (e.ItemGuid != 0)
            {
                aco_chest_container = e.ItemGuid;
                action_completed = true;
                current_state = STEELBOT_STATE.DELAY1;
                CoreManager.Current.Actions.AddChatText("Container opened: " + aco_chest_container.ToString(), 5);
            }
        }

        private void WorldFilter_ChangeObject(object sender, ChangeObjectEventArgs e)
        {            
            try
            {
                if (e.Change == WorldChangeType.IdentReceived && e.Changed.Id == aco_door)
                {
                    is_door_open = e.Changed.Values(BoolValueKey.Open);
                    action_completed = true;
                }
                if(e.Changed.Name == "Pathwarden Supply Key")
                {
                    current_state = STEELBOT_STATE.UNLOCKING_CHEST;
                    action_completed = true;
                }                    
                if(e.Changed.Name == "Academy Exit Token")
                {
                    current_state = STEELBOT_STATE.ESCAPING;
                    action_completed = true;
                }  
            }
            catch (Exception ex) { }
        }

        private void CharacterFilter_ActionComplete(object sender, EventArgs e)
        {
            action_completed = true;
        }

        double door_dist = 1000;
        void updateWorldAcos()
        {            
            var ac_objs = CoreManager.Current.WorldFilter.GetAll();
            foreach(var aco in ac_objs)
            {
                switch (aco.Name)
                {
                    case "Jonathan":
                        aco_jonathan = aco.Id;
                        log.WriteDebug("Found Jonathan");
                        break;
                    case "Society Greeter":
                        aco_greeter = aco.Id;
                        log.WriteDebug("Found Greeter");
                        break;
                    case "Door":                        
                        var dist = aco.Coordinates().DistanceToCoords(CoreManager.Current.WorldFilter.GetInventory().First.Coordinates());
                        log.WriteDebug("dist:" + dist);
                        if (dist < door_dist)
                        {
                            door_dist = dist;
                            aco_door = aco.Id;
                            log.WriteDebug("Found Door");
                        }
                        break;
                    case "Samuel":
                        aco_sign = aco.Id;
                        log.WriteDebug("Found Sam");
                        break;
                    case "Gharu'ndim Pathwarden Chest":
                    case "Sho Pathwarden Chest":
                    case "Aluvian Pathwarden Chest":
                        aco_pathwarden_chest = aco.Id;
                        log.WriteDebug("Found Pathwarden Chest");
                        break;
                    case "Pathwarden Koro Ijida":
                    case "Pathwarden Thorolf":
                        aco_pathwarden = aco.Id;
                        log.WriteDebug("Found Pathwarden");
                        break;
                    default:
                        break;
                }
            }
        }

        void findSteelBotManagerAco()
        {
            var ac_objs = CoreManager.Current.WorldFilter.GetAll();
            foreach (var aco in ac_objs)
            {
                if (aco.Name == STEELBOT_MANAGER || aco.Name == STEELBOT_MANAGER_2 || aco.Name == STEELBOT_MANAGER_3 || aco.Name == STEELBOT_MANAGER_4)
                {
                    aco_steelbot_manager = aco.Id;
                    log.WriteDebug("Found Steelbot Manager.");
                    break;
                }
            }
        }       

        const string PATHWARDEN_KEY = "Pathwarden Supply Key";
        const string EXIT_TOKEN = "Academy Exit Token";
        const string PATHWARDEN_TOKEN = "Pathwarden Token";
        const string SALVAGED_STEEL = "Salvaged Steel";

        void updateInventoryAcos()
        {
            var inventory = CoreManager.Current.WorldFilter.GetInventory();
            foreach (var item in inventory)
            {
                switch (item.Name)
                {
                    case EXIT_TOKEN:
                        log.WriteDebug("Found Academy Exit Token");
                        aco_exit_token = item.Id;
                        break;                    
                    case PATHWARDEN_TOKEN:
                        log.WriteDebug("Found Pathwarden Token");
                        aco_pathwarden_token = item.Id;
                        break;
                    case PATHWARDEN_KEY:
                        log.WriteDebug("Found Pathwarden Key");
                        aco_pathwarden_key = item.Id;
                        break;
                    default:
                        break;
                }
                if (waiting_for_item != "" && item.Name == waiting_for_item)
                {
                    action_completed = true;
                    waiting_for_item = "";
                }
            }
        }

        List<string> pathwarden_armor_names = new List<string> {
            "Pathwarden Gauntlets",
            "Pathwarden Plate Leggings",
            "Pathwarden Yoroi Leggings",
            "Pathwarden Helm",
            "Pathwarden Sollerets",
            "Pathwarden Plate Hauberk",
            "Pathwarden Yoroi Hauberk"
        };

        //uint currentStateTimeout = 5000;
        System.Threading.Timer timer = null;

        string waiting_for_item = "";

        void doStateAction(STEELBOT_STATE state)
        {
            CoreManager.Current.Actions.AddChatText("Doing action for state: " + state.ToString() , 5);
            log.WriteDebug("Doing action for state: "+ state.ToString());
            action_completed = false;
            switch (state)
            {
                case STEELBOT_STATE.HELLO:
                    if (aco_greeter == 0)
                    {
                        next_state = STEELBOT_STATE.LOGGING_OUT;
                        action_completed = true;
                    }
                    else
                    {
                        CoreManager.Current.Actions.UseItem(aco_greeter, 0);
                        next_state = STEELBOT_STATE.CHECKING_DOOR;
                    }
                    break;
                case STEELBOT_STATE.CHECKING_DOOR:
                    CoreManager.Current.Actions.RequestId(aco_door);                    
                    next_state = STEELBOT_STATE.DELAY_DOOR_ID;
                    action_completed = true;
                    break;
                case STEELBOT_STATE.DELAY_DOOR_ID:
                    next_state = STEELBOT_STATE.OPENING_DOOR;
                    action_completed = true;
                    break;
                case STEELBOT_STATE.OPENING_DOOR:                    
                    if (is_door_open){
                        action_completed = true;
                    } else {
                        CoreManager.Current.Actions.UseItem(aco_door, 0);
                    }
                    next_state = STEELBOT_STATE.APPROACHING_SAM;
                    break;
                case STEELBOT_STATE.APPROACHING_SAM:
                    CoreManager.Current.Actions.UseItem(aco_sign, 0);
                    next_state = STEELBOT_STATE.TALKING_TO_JONATHAN;
                    break;
                case STEELBOT_STATE.TALKING_TO_JONATHAN:
                    CoreManager.Current.Actions.UseItem(aco_jonathan, 0);
                    next_state = STEELBOT_STATE.ESCAPING;
                    break;
                case STEELBOT_STATE.ESCAPING:
                    Thread.Sleep(100);
                    var exit_token = FindItemInInventoryByName(EXIT_TOKEN);
                    if (exit_token != 0)
                    {
                        CoreManager.Current.Actions.GiveItem(exit_token, aco_jonathan);
                        waitMsAndGoToState(1000, STEELBOT_STATE.ESCAPING);
                    }
                    break;
                case STEELBOT_STATE.PORTALING:
                    break;
                case STEELBOT_STATE.MOVING_TO_CHEST:
                    CoreManager.Current.Actions.UseItem(aco_pathwarden_chest, 0);
                    next_state = STEELBOT_STATE.TURNING_IN_PATHWARDEN_TOKEN;
                    break;
                case STEELBOT_STATE.TURNING_IN_PATHWARDEN_TOKEN:
                    Thread.Sleep(100);
                    var token = FindItemInInventoryByName(PATHWARDEN_TOKEN);
                    if (token != 0)
                    {
                        CoreManager.Current.Actions.GiveItem(token, aco_pathwarden);
                        waitMsAndGoToState(2000, STEELBOT_STATE.TURNING_IN_PATHWARDEN_TOKEN);
                    }
                    else
                    {
                        next_state = STEELBOT_STATE.WAITING_FOR_KEY;
                        action_completed = true;
                    }
                    break;
                case STEELBOT_STATE.WAITING_FOR_KEY:
                    var key = FindItemInInventoryByName(PATHWARDEN_KEY);                    
                    if (key != 0)
                    {
                        next_state = STEELBOT_STATE.UNLOCKING_CHEST;
                    }
                    action_completed = true;
                    break;                
                case STEELBOT_STATE.UNLOCKING_CHEST:
                    CoreManager.Current.Actions.ApplyItem(aco_pathwarden_key, aco_pathwarden_chest);
                    next_state = STEELBOT_STATE.OPENING_CHEST;
                    action_completed = true;
                    break;
                case STEELBOT_STATE.OPENING_CHEST:
                    if (aco_chest_container == 0)
                    {
                        var retry_key = FindItemInInventoryByName(PATHWARDEN_KEY);
                        if (retry_key != 0)
                        {
                            next_state = STEELBOT_STATE.UNLOCKING_CHEST;
                            action_completed = true;
                        }
                        else
                        {
                            CoreManager.Current.Actions.UseItem(aco_pathwarden_chest, 0);
                            next_state = STEELBOT_STATE.OPENING_CHEST;
                        }
                    }
                    else
                    {
                        CoreManager.Current.Actions.AddChatText("OPENING_CHEST_NONZERO_CONTAINER" + state.ToString(), 5);
                        next_state = STEELBOT_STATE.LOOTING_CHEST;
                        action_completed = true;
                    }
                    break;
                case STEELBOT_STATE.DELAY1:
                    next_state = STEELBOT_STATE.LOOTING_CHEST;
                    action_completed = true;
                    break;
                case STEELBOT_STATE.LOOTING_CHEST:
                    var chest = CoreManager.Current.WorldFilter.GetByContainer(aco_chest_container);
                    if (aco_chest_container != 0)
                    {
                        if (chest.Count > 0)
                        {
                            CoreManager.Current.Actions.MoveItem(chest.First.Id, CoreManager.Current.CharacterFilter.Id);
                            next_state = STEELBOT_STATE.LOOTING_CHEST;
                            action_completed = true;
                        }
                        else
                        {
                            next_state = STEELBOT_STATE.CLOSING_CHEST;
                            action_completed = true;
                        }
                    }
                    else
                    {
                        CoreManager.Current.Actions.AddChatText("LOOTING_CHEST__0_CONTAINER" + state.ToString(), 5);
                    }
                    break;
                case STEELBOT_STATE.CLOSING_CHEST:
                    CoreManager.Current.Actions.UseItem(aco_pathwarden_chest, 0);
                    waitMsAndGoToState(1000, STEELBOT_STATE.TURNING_IN_PATHWARDEN_ARMOR);
                    break;
                case STEELBOT_STATE.TURNING_IN_PATHWARDEN_ARMOR:
                    GiveArmorToPathWarden(current_state);
                    break;                
                case STEELBOT_STATE.GIVING_STEEL:
                    var steel = FindItemInInventoryByName(SALVAGED_STEEL);
                    if (steel != 0)
                    {
                        CoreManager.Current.Actions.GiveItem(steel, aco_steelbot_manager);
                        waitMsAndGoToState(300, STEELBOT_STATE.GIVING_STEEL);
                    }
                    else
                    {
                        next_state = STEELBOT_STATE.LOGGING_OUT;
                    }
                    break;
                case STEELBOT_STATE.LOGGING_OUT:
                    CoreManager.Current.Actions.AddChatText("DONE.", 5);
                    break;
            }            
        }

        void waitForItem(string item_name)
        {
            waiting_for_item = item_name;
        }

        void waitMsAndGoToState(int ms, STEELBOT_STATE state)
        {
            timer = new System.Threading.Timer((obj) =>
            {
                next_state = state;
                action_completed = true;
                timer.Dispose();
            }, null, ms, System.Threading.Timeout.Infinite);
        }

        int FindItemInInventoryByName(string name)
        {
            foreach (var item in CoreManager.Current.WorldFilter.GetInventory())
            {
                if (item.Name == name)
                {
                    return item.Id;
                }
            }
            return 0;
        }

        void GiveArmorToPathWarden(STEELBOT_STATE state)
        {
            foreach (var item in CoreManager.Current.WorldFilter.GetInventory())
            {
                if (pathwarden_armor_names.Contains(item.Name))
                {
                    CoreManager.Current.Actions.GiveItem(item.Id, aco_pathwarden);
                    timer = new System.Threading.Timer((obj) =>
                    {
                        action_completed = true;
                        timer.Dispose();
                    }, null, 500, System.Threading.Timeout.Infinite);

                    return;                    
                }
            }

            next_state = STEELBOT_STATE.GIVING_STEEL;
            action_completed = true;
        }

        readonly System.Windows.Forms.Timer steelRunTimer = new System.Windows.Forms.Timer();
        int state;

        bool steelRunnerTimer_initialized = false;
        public void Init_SteelRunner_Timer()
        {
            if (!steelRunnerTimer_initialized)
            {
                steelRunnerTimer_initialized = true;
                steelRunTimer.Tick += SteelRunTimer_Tick;
                steelRunTimer.Interval = 500;
                steelRunTimer.Start();
            }
        }

        private void SteelRunTimer_Tick(object sender, EventArgs e)
        {
            EscapeTheAcademy();
        }

        void EscapeTheAcademy()
        {
            if (action_completed)
            {
                updateWorldAcos();
                updateInventoryAcos();
                findSteelBotManagerAco();

                doStateAction(current_state);

                current_state = next_state;
            }
        }

        //void setStateTimeout(STEELBOT_STATE state)
        //{
        //    switch(state)
        //    {
        //        case STEELBOT_STATE.LOGGING_IN:
        //            currentStateTimeout = 20000;
        //            break;
        //        case STEELBOT_STATE.LOOTING_CHEST:
        //            currentStateTimeout = 20000;
        //            break;
        //        default:
        //            currentStateTimeout = 7000;
        //            break;
        //    }
        //}

        void Current_RenderFrame(object sender, EventArgs e)
        {
            try
            {
                if (DateTime.Now.Subtract(TimeSpan.FromMilliseconds(4000)) < loginCompleteTime)
                    return;

                if (CoreManager.Current.CharacterFilter.Name.ToLower().StartsWith("z"))
                {
                    Init_SteelRunner_Timer();
                }

                if (current_state == STEELBOT_STATE.LOGGING_OUT ||
                    DateTime.Now.Subtract(TimeSpan.FromMilliseconds(3*60*1000)) > loginCompleteTime )
                { 
                    CoreManager.Current.RenderFrame -= new EventHandler<EventArgs>(Current_RenderFrame);
                    CoreManager.Current.Actions.Logout();
                    unregisterHandlers();
                }

            }
            catch (Exception ex) { Debug.LogException(ex); }
        }

        public void FilterCore_ClientDispatch(object sender, NetworkMessageEventArgs e)
        {
            if (e.Message.Type == 0xF7C8)
            { // Enter Game 
                freshLogin = true;
                current_state = STEELBOT_STATE.HELLO;
                steelRunnerTimer_initialized = false;
            }

            if (e.Message.Type == 0xF7B1 && Convert.ToInt32(e.Message["action"]) == 0xA1) // Character Materialize (Any time is done portalling in, login or portal)
            {
                if (freshLogin)
                {
                    freshLogin = false;
                    current_state = STEELBOT_STATE.HELLO;
                    action_completed = true;

                    string characterName = GameRepo.Game.Character;
                    if (string.IsNullOrEmpty(characterName))
                    {
                        // Do not know why GameRepo.Game.Character is not yet populated, but it isn't
                        var launchInfo = LaunchControl.GetLaunchInfo();
                        if (launchInfo.IsValid)
                        {
                            characterName = launchInfo.CharacterName;
                        }
                    }

                    var persister = new LoginCommandPersister(GameRepo.Game.Account, GameRepo.Game.Server, characterName);

                    log.WriteDebug("FilterCore_ClientDispatch: Character: '{0}'", GameRepo.Game.Character);
                    
                    //_loginCmds = persister.ReadAndCombineQueues();

                    loginCompleteTime = DateTime.Now;

                    initHandlers();

                    CoreManager.Current.RenderFrame += new EventHandler<EventArgs>(Current_RenderFrame);
                }
                else
                {
                    // find the pathwarden
                    current_state = STEELBOT_STATE.MOVING_TO_CHEST;
                    action_completed = true;
                }
            }
        }

        private string TextRemainder(string text, string prefix)
        {
            if (text.Length <= prefix.Length) { return string.Empty; }
            return text.Substring(prefix.Length);
        }
        public void FilterCore_CommandLineText(object sender, ChatParserInterceptEventArgs e)
        {
            bool writeChanges = true;
            bool global = false;
            string cmdtext = e.Text;
            if (cmdtext.Contains("/tfglobal"))
            {
                cmdtext = cmdtext.Replace(" /tfglobal", " /tf");
                cmdtext = cmdtext.Replace("/tfglobal ", "/tf ");
                cmdtext = cmdtext.Replace("/tfglobal", "/tf");
                global = true;
            }
            if (cmdtext.StartsWith("/tf log "))
            {
                string logmsg = TextRemainder(cmdtext, "/tf log ");
                log.WriteInfo(logmsg);

                e.Eat = true;
            }
            if (e.Eat && writeChanges)
            {
                var persister = new LoginCommandPersister(GameRepo.Game.Account, GameRepo.Game.Server, GameRepo.Game.Character);
            }
        }
    }
}