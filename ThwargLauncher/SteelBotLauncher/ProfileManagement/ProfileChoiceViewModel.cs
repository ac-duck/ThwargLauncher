﻿using System;
using System.ComponentModel;

namespace SteelBotLauncher
{
    class ProfileChoiceViewModel : INotifyPropertyChanged
    {
        private readonly Profile _profile;
        public ProfileChoiceViewModel(Profile profile)
        {
            _profile = profile;
        }
        public string Name
        {
            get { return _profile.Name; }
            set
            {
                var profileManager = new ProfileManager();
                // profile manager renames the actual profile on the disk
                bool renamed = profileManager.RenameProfile(_profile.Name, value);
                if (renamed)
                {
                    _profile.Name = value;
                    OnPropertyChanged("CurrentProfileName");
                }
            }
        }
        public string Description
        {
            get { return _profile.Description; }
            set
            {
                _profile.Description = value;
                var profileManager = new ProfileManager();
                profileManager.Save(_profile);

            }
        }
        public int ActiveAccounts { get { return _profile.ActiveAccountCount; } }
        public int ActiveServers { get { return _profile.ActiveServerCount; } }
        //public DateTime LastLaunch { get { return PopulateDate(_profile.LastLaunchedDate, DateTime.MinValue); } }
        public string LastLaunch { get { return (_profile.LastLaunchedDate.HasValue ? _profile.LastLaunchedDate.ToString() : "(never)"); } }

        private DateTime PopulateDate(DateTime? date, DateTime defval)
        {
            return (date.HasValue ? date.Value : defval);
        }
        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
