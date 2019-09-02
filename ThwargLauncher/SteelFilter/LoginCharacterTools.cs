using System;
using System.Collections.Generic;

using Decal.Adapter;
using System.Drawing;

namespace SteelFilter
{
    class LoginCharacterTools
    {
        private string zonename;
        int characterSlots;
        private bool written;
        private string characterName = null;

        public List<Character> characters = null;

        internal void FilterCore_ServerDispatch(object sender, NetworkMessageEventArgs e)
        {
            log.WriteDebug("Account123:" + GameRepo.Game.Account);            
            if (e.Message.Type == 0xF658) // Zone Name
            {
                zonename = Convert.ToString(e.Message["zonename"]);
                log.WriteInfo("zonename123: '{0}'", zonename);
                GameRepo.Game.SetAccount(zonename);                
            }

            if (e.Message.Type == 0xF7E1) // Server Name
            {
                //Server Name retrieved from the server message, not used (unreliable in EMU)
                var server = Convert.ToString(e.Message["server"]);
                log.WriteInfo("server: '{0}'", server);
                GameRepo.Game.SetServer(server);
            }

            if (e.Message.Type == 0xF658) // Character List
            {
                characterSlots = Convert.ToInt32(e.Message["slotCount"]);

                characters = new List<Character>();

                MessageStruct charactersStruct = e.Message.Struct("characters");

                bool all_chars = true;
                for (int i = 0; i < charactersStruct.Count; i++)
                {
                    int character = Convert.ToInt32(charactersStruct.Struct(i)["character"]);
                    string name = Convert.ToString(charactersStruct.Struct(i)["name"]);
                    int deleteTimeout = Convert.ToInt32(charactersStruct.Struct(i)["deleteTimeout"]);

                    characters.Add(new Character(character, name, deleteTimeout));
                    log.WriteInfo(character.ToString() + " " + name + " " + deleteTimeout.ToString());
                    if(!name.StartsWith(base_name))
                    {
                        all_chars = false;
                    }
                }

                if(characters.Count < 10 && all_chars)
                {
                    log.WriteDebug("Found less than 10 Ztiel characters, creating!");
                    CreateCharacter();
                }

                characters.Sort((a, b) => String.Compare(a.Name, b.Name, StringComparison.Ordinal));
            }
            if (!written)
            {
                if (GameRepo.Game.Server != "" && zonename != null && characters != null)
                {
                    CharacterBook.WriteCharacters(ServerName: GameRepo.Game.Server, zonename: zonename, characters: characters);
                    Heartbeat.LaunchHeartbeat();
                    written = true;
                }
            }
            if (CoreManager.Current.CharacterFilter.Name != characterName)
            {
                GameRepo.Game.SetCharacter(CoreManager.Current.CharacterFilter.Name);
                characterName = CoreManager.Current.CharacterFilter.Name;
            }
        }

        public bool LoginCharacter(int id)
        {
            for (int i = 0; i < characters.Count; i++)
            {
                if (characters[i].Id == id)
                    return LoginByIndex(i);
            }

            return false;
        }

        private string base_name = "Zteil ";
        Random rand = new Random();

        private string randomLetter()
        {
            return char.ConvertFromUtf32('a' + rand.Next(0, 25));
        }

        public string randomName()
        {            
            string random_string = "Z";
            random_string += randomLetter();
            random_string += "q";
            random_string += randomLetter();
            random_string += "z";
            random_string += randomLetter();
            random_string += "z";
            random_string += randomLetter();
            random_string += "j";
            return base_name + random_string;
        }

        Point[] make_holt =
        {
            new Point(330,  233),
            new Point(50,  75),
            new Point(97,  575),
            new Point(42,  271),           
            new Point(94,  567),
            new Point(94,  567),
            new Point(94,  567),
            new Point(94,  567)
        };

        static Point arrow_next = new Point(94, 567);

        Point[] make_shoushi =
        {
            new Point(0,  0),
            new Point(0,  0),
            new Point(0,  0),
            new Point(330,  233),
            new Point(0,  0),
            new Point(0,  0),
            new Point(0,  0),
            new Point(50,  150),
            arrow_next,
            new Point(42,  271),
            arrow_next,
            arrow_next,
            arrow_next,
            arrow_next
        };

        Point delete_char = new Point(126, 571);
        Point delete_char_done = new Point(312, 361);

        int indexCreateCharClick = 0;

        readonly System.Windows.Forms.Timer createCharacterTimer = new System.Windows.Forms.Timer();
        readonly System.Windows.Forms.Timer deleteCharacterTimer = new System.Windows.Forms.Timer();

        string randomCharacterName = "";

        bool deleteing_character = true;
        int delete_cmds_index = 0;

        public void DeleteCharacter_tick(object sender, EventArgs e)
        {
            switch (delete_cmds_index) {
                case 0:
                    Filter.Shared.PostMessageTools.SendMouseClick(delete_char.X, delete_char.Y);
                    break;
                case 1:
                    Filter.Shared.PostMessageTools.SendCharString("delete");
                    break;
                case 2:
                    Filter.Shared.PostMessageTools.SendMouseClick(delete_char_done.X, delete_char_done.Y);
                    break;
                case 3:
                    break;
                default:
                    deleteCharacterTimer.Stop();
                    deleteing_character = false;
                    break;
            }
            delete_cmds_index++;
        }

        private void uninit_timers()
        {
            deleteCharacterTimer.Tick -= new EventHandler(DeleteCharacter_tick);
            createCharacterTimer.Tick -= new EventHandler(createChacter_tick);
            indexCreateCharClick = 0;
            delete_cmds_index = 0;
            deleteing_character = true;
        }

        public bool CreateCharacter()
        {
            string name = randomName();
            randomCharacterName = name;

            indexCreateCharClick = 0;
            deleteing_character = true;
            deleteCharacterTimer.Tick += new EventHandler(DeleteCharacter_tick);
            deleteCharacterTimer.Interval = 900;
            deleteCharacterTimer.Start();

            createCharacterTimer.Tick += new EventHandler(createChacter_tick);
            createCharacterTimer.Interval = 900;
            createCharacterTimer.Start();

            return true;
        }

        private void createChacter_tick(object sender, EventArgs e)
        {

            if (deleteing_character)
            {
                return;
            }
            if (indexCreateCharClick < make_shoushi.Length)
            {
                var x = make_shoushi[indexCreateCharClick].X;
                var y = make_shoushi[indexCreateCharClick].Y;
                Filter.Shared.PostMessageTools.SendMouseClick(x, y);                
            }
            else if (indexCreateCharClick == make_shoushi.Length)
            {
                log.WriteDebug("Creating character with name: " + randomCharacterName);
                Filter.Shared.PostMessageTools.SendCharString(randomCharacterName);
            }
            else
            {
                Filter.Shared.PostMessageTools.SendMouseClick(708, 583);
                createCharacterTimer.Stop();
                log.WriteDebug("Done.");
                uninit_timers();
            }
            indexCreateCharClick++;
        }

        public bool LoginCharacter(string name)
        {
            for (int i = 0; i < characters.Count; i++)
            {
                if (String.Compare(characters[i].Name, name, StringComparison.OrdinalIgnoreCase) == 0)
                    return LoginByIndex(i);
            }

            return false;
        }

        private const int XPixelOffset = 121;
        private const int YTopOfBox = 209;
        private const int YBottomOfBox = 532;

        public bool LoginByIndex(int index)
        {
            if (index >= characters.Count)
                return false;

            float characterNameSize = (YBottomOfBox - YTopOfBox) / (float)characterSlots;

            int yOffset = (int)(YTopOfBox + (characterNameSize / 2) + (characterNameSize * index));

            // Select the character
            Filter.Shared.PostMessageTools.SendMouseClick(XPixelOffset, yOffset);

            // Click the Enter button
            Filter.Shared.PostMessageTools.SendMouseClick(0x015C, 0x0185);

            log.WriteInfo("LoginCharacterTools.LoginByIndex");

            return true;
        }
    }
}
