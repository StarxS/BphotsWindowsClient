﻿using System;
using System.Collections.Generic;

namespace HotsBpHelper.Settings
{
    public class CustomConfigurationSettings
    {
        public bool AutoShowHideHelper { get; set; }

        public bool AutoDetectHeroAndMap { get; set; }

        public bool AutoShowMMR { get; set; } = true;

        public bool AutoUploadReplayToHotslogs { get; set; } 

        public bool AutoUploadReplayToHotsweek { get; set; } 

        public UploadStrategy UploadStrategy { get; set; }

        public string LanguageForBphots { get; set; }

        public string LanguageForMessage { get; set; }

        public string LanguageForGameClient { get; set; }

        public int MMRAutoCloseTime { get; set; }

        public bool UploadBanSample { get; set; }
    }

    public class UserDataSettings
    {
        public List<string> PlayerTags { get; set; }

        public string HotsweekPlayerId { get; set; }

        public DateTime LastClientVisit { get; set; }
    }

    public enum UploadStrategy
    {
        UploadAll = 2,
        UploadNew = 1,
        None = 0
    }
}
