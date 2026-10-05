namespace Siavosh.Localization
{
    /// <summary>The interface's words in Persian and English.</summary>
    public static class Ui
    {
        static LocText T(string fa, string en) { return new LocText(fa, en); }

        // Language screen
        public static readonly LocText ChooseLanguage = T("زبان را انتخاب کنید", "Choose your language");
        public static readonly LocText LanguageSaved = T("بعداً از تنظیمات قابل تغییر است", "You can change it any time in Settings");

        // Menu
        public static readonly LocText Continue = T("ادامهٔ داستان", "Continue the tale");
        public static readonly LocText Begin = T("آغاز داستان", "Begin the tale");
        public static readonly LocText Chapters = T("پردهٔ داستان", "The story curtain");
        public static readonly LocText Camp = T("اردوگاه", "Camp");
        public static readonly LocText StoryBook = T("کتاب داستان", "Story book");
        public static readonly LocText Settings = T("تنظیمات", "Settings");
        public static readonly LocText NextUp = T("فصل {0} · مرحلهٔ {1} · {2}", "Chapter {0} · Stage {1} · {2}");
        public static readonly LocText AllDone = T("فصل یکم به پایان رسید · فصل دوم به‌زودی", "Chapter one is complete · Chapter two is coming soon");
        public static readonly LocText Tagline = T("شاهزاده‌ای که از آتش گذشت و نسوخت", "The prince who rode through fire and did not burn");

        // Common
        public static readonly LocText Back = T("بازگشت", "Back");
        public static readonly LocText Close = T("بستن", "Close");
        public static readonly LocText Play = T("آغاز", "Play");
        public static readonly LocText Replay = T("دوباره", "Replay");
        public static readonly LocText Level = T("سطح {0}", "Level {0}");
        public static readonly LocText Dinars = T("{0} دینار", "{0} dinars");
        public static readonly LocText Leaves = T("برگ‌ها {0} از {1}", "Leaves {0} of {1}");
        public static readonly LocText Honor = T("نام نیک {0}", "Honour {0}");
        public static readonly LocText Stage = T("مرحلهٔ {0}", "Stage {0}");
        public static readonly LocText Boss = T("آزمون", "Trial");
        public static readonly LocText Locked = T("بسته", "Locked");
        public static readonly LocText ComingSoon = T("به‌زودی", "Coming soon");
        public static readonly LocText ChapterN = T("فصل {0}", "Chapter {0}");
        public static readonly LocText Yes = T("آری", "Yes");
        public static readonly LocText No = T("نه", "No");

        // Map
        public static readonly LocText MapTitle = T("پردهٔ داستان", "The Story Curtain");
        public static readonly LocText ChapterLocked = T("این فصل هنوز نقل نشده است. نقّال به‌زودی آن را می‌گوید.", "This chapter has not been told yet. The storyteller will tell it soon.");
        public static readonly LocText FinishPrevious = T("نخست مرحلهٔ پیشین را به پایان برسان.", "Finish the stage before this one first.");

        // Camp
        public static readonly LocText Skills = T("درخت مهارت", "Skills");
        public static readonly LocText Smith = T("آهنگر", "Smith");
        public static readonly LocText SkillPoints = T("امتیاز مهارت: {0}", "Skill points: {0}");
        public static readonly LocText Experience = T("{0} از {1} تجربه", "{0} / {1} XP");
        public static readonly LocText Learn = T("یادگیری · ۱ امتیاز", "Learn · 1 point");
        public static readonly LocText Learned = T("آموخته شد", "Learned");
        public static readonly LocText NeedsPrevious = T("نخست مهارت پیشین را بیاموز", "Learn the skill before it first");
        public static readonly LocText NeedsPoint = T("با بالا رفتن سطح، امتیاز می‌گیری", "You earn points by levelling up");
        public static readonly LocText StoryLocked = T("با پیشرفت داستان باز می‌شود", "Unlocked by the story");
        public static readonly LocText Champion = T("پهلوانی", "Champion");
        public static readonly LocText Archery = T("کمانداری", "Archery");
        public static readonly LocText Riding = T("سواری", "Riding");
        public static readonly LocText MasterRostam = T("استاد: رستم", "Master: Rostam");
        public static readonly LocText MasterZavareh = T("استاد: رستم و زواره", "Masters: Rostam and Zavareh");
        public static readonly LocText WithShabrang = T("با شبرنگ", "With Shabrang");
        public static readonly LocText FarrTitle = T("فرّ کیانی", "Royal Farr");
        public static readonly LocText Earned = T("به دست آمد", "Earned");
        public static readonly LocText Equipped = T("در دست", "Equipped");
        public static readonly LocText Equip = T("برگزیدن", "Equip");
        public static readonly LocText Buy = T("خرید · {0}", "Buy · {0}");
        public static readonly LocText NotEnough = T("دینار کافی نیست", "Not enough dinars");
        public static readonly LocText Damage = T("آسیب", "Damage");
        public static readonly LocText Health = T("سلامت", "Health");
        public static readonly LocText Sword = T("شمشیر", "Sword");
        public static readonly LocText Bow = T("کمان", "Bow");
        public static readonly LocText Armor = T("زره", "Armour");
        public static readonly LocText Stats = T("سلامت {0} · آسیب {1} · کمان {2}", "Health {0} · Sword {1} · Bow {2}");

        // Story book
        public static readonly LocText StoryBookTitle = T("کتاب داستان", "The Story Book");
        public static readonly LocText NotSeenYet = T("هنوز دیده نشده", "Not seen yet");
        public static readonly LocText LeavesFound = T("برگ‌های شاهنامه: {0} از {1}", "Leaves of the Shahnameh: {0} of {1}");

        // Settings
        public static readonly LocText Language = T("زبان", "Language");
        public static readonly LocText Sound = T("صدا", "Sound");
        public static readonly LocText Music = T("موسیقی", "Music");
        public static readonly LocText Vibration = T("لرزش", "Vibration");
        public static readonly LocText Difficulty = T("سختی", "Difficulty");
        public static readonly LocText Easy = T("آسان", "Easy");
        public static readonly LocText Normal = T("عادی", "Normal");
        public static readonly LocText Hard = T("سخت", "Hard");
        public static readonly LocText On = T("روشن", "On");
        public static readonly LocText Off = T("خاموش", "Off");
        public static readonly LocText ResetProgress = T("پاک کردن پیشرفت", "Reset progress");
        public static readonly LocText ResetConfirm = T("همهٔ پیشرفت پاک شود؟ این کار برگشت‌پذیر نیست.", "Erase all progress? This cannot be undone.");

        // Stage
        public static readonly LocText Paused = T("درنگ", "Paused");
        public static readonly LocText Resume = T("ادامه", "Resume");
        public static readonly LocText Restart = T("از نو", "Restart");
        public static readonly LocText LeaveStage = T("بازگشت به پرده", "Back to the curtain");
        public static readonly LocText Defeated = T("سیاوش از پا افتاد", "Siavosh has fallen");
        public static readonly LocText DefeatedRide = T("سیاوش از اسب افتاد", "Siavosh was thrown");
        public static readonly LocText DefeatHint = T("رستم می‌گوید: «پهلوان دوباره برمی‌خیزد.»", "Rostam says: “A champion rises again.”");
        public static readonly LocText HuntFailed = T("شب رسید و شکار کامل نشد", "Night fell before the hunt was done");
        public static readonly LocText TryAgain = T("دوباره بکوش", "Try again");
        public static readonly LocText GateOpen = T("دروازه گشوده شد!", "The gate is open!");
        public static readonly LocText LeafFound = T("برگ {0} از {1}", "Leaf {0} of {1}");
        public static readonly LocText Parry = T("دفع شد!", "Parried!");
        public static readonly LocText Yield = T("تسلیم!", "I yield!");
        public static readonly LocText HelpLamb = T("بره را برگردان", "Return the lamb");
        public static readonly LocText HelpBird = T("زخم هدهد را ببند", "Tend the hoopoe");
        public static readonly LocText HelpTrainee = T("دستش را بگیر", "Help him up");
        public static readonly LocText HonorPlus = T("+۱ نام نیک", "+1 Honour");
        public static readonly LocText HonorLamb = T("برهٔ گم‌شده را به چوپان بازگرداندی.", "You returned the lost lamb to its shepherd.");
        public static readonly LocText HonorBird = T("زخم هدهد را بستی و او پر کشید.", "You bound the hoopoe's wing and it flew away.");
        public static readonly LocText HonorTrainee = T("دست نوآموزِ شکست‌خورده را گرفتی.", "You helped the beaten trainee to his feet.");
        public static readonly LocText Hunted = T("شکار {0} از {1}", "Hunted {0} of {1}");

        // Result
        public static readonly LocText StageComplete = T("مرحله به پایان رسید", "Stage complete");
        public static readonly LocText Xp = T("تجربه", "Experience");
        public static readonly LocText DinarsLabel = T("دینار", "Dinars");
        public static readonly LocText LeavesLabel = T("برگ‌ها", "Leaves");
        public static readonly LocText HonorLabel = T("نام نیک", "Honour");
        public static readonly LocText LevelUp = T("سطح تازه! یک امتیاز مهارت گرفتی.", "Level up! You earned a skill point.");
        public static readonly LocText NewSkill = T("مهارت تازه: {0}", "New skill: {0}");
        public static readonly LocText NewFarr = T("فرّ تازه: {0}", "New Farr: {0}");
        public static readonly LocText Next = T("ادامه", "Continue");
        public static readonly LocText ToCamp = T("اردوگاه", "Camp");

        // Cutscenes
        public static readonly LocText Skip = T("رد کردن", "Skip");
        public static readonly LocText TapToContinue = T("برای ادامه لمس کنید", "Tap to continue");
        public static readonly LocText BackToChoice = T("بازگشت به انتخاب", "Back to the choice");
        public static readonly LocText YouChose = T("انتخاب تو:", "You chose:");
    }
}
