using Xamarin.Forms;

namespace SmartPower.Resources.FontAwesome
{


    // HELPFUL SECTION WITH USEFUL TIPS

    // To see contents of font awesome file...
    // Use tool below and just drag your fontawesome file into the browser and it will generate your c# code.
    // https://andreinitescu.github.io/IconFont2Code/

    // CHEAT SHEET
    //https://fontawesome.com/v4.7.0/cheatsheet/

    // TIPS
    //  1. iOS font awesome files should be.otf files(not.ttf)
    //  2. All font awesome files should be referenced in plist
    //  3. iOS font awesome name references in renderer should be TYPE FACE references
    //  4. Use FontBook to validate and inspect font awesome files
    //  5. Font awesome files should be set to bundle resource and copy always
    //  6. Solid icons should be made bold in ios(some may not show otherwise)
    //  7. iOS font awesome files should be copied to Resources folder
    //  8. To review all fonts implemented into app, copy following code into AppDelegate.cs
    //
    // foreach (var familyNames in UIFont.FamilyNames.OrderBy(c => c).ToList())
    // {
    //    Console.WriteLine(" * " + familyNames);
    //    foreach (var familyName in UIFont.FontNamesForFamilyName(familyNames).OrderBy(c => c).ToList())
    //    {
    //        Console.WriteLine(" *Font family =-- " + familyName);
    //    }
    // }

    /// <summary>
    /// If referencing FontAwesome from C#, then this class can be used to reference the FontAwesome resources.
    ///
    /// e.g. Application.Current.Resources[FAResourceKeys.FABrands]
    /// 
    /// </summary>
    public static class FAResourceKeys
    {
        public static readonly string FARegular = "FontAwesomeRegular";
        public static readonly string FASolid = "FontAwesomeSolid";
        public static readonly string FABrands = "FontAwesomeBrands";
    }

    public static partial class Icon
    {
        // FA Regular
        public const string FASearch = "\uf002";

        public const string FABars = "\uf0c9";

        public const string FAFAngleLeft = "\uf104";

        public const string FAUSD = "\uf155";

        public const string FAUserCircle = "\uf2bd";

        public const string FAUser = "\uf007";

        public const string FADollar = "\uf155";

        public const string FAChevronDown = "\uf078";

        public const string FAChevronRight = "\uf054";

        public const string FAChevronLeft = "\uf053";

        public const string FAChevronUp = "\uf077";

        public const string FACircleO = "\uf192";

        public const string FAArrowLeft = "\uf060";

        public const string FACircle = "\uf111";

        public const string FACaretDown = "\uf150";

        public const string FAChevronRightDble = "\uf101";

        public const string FACheckBoxCircle = "\uf058";

        public const string FACross = "\uf00d";

        public const string FAEdit = "\uf044";

        public const string FASave = "\uf0c7";

        public const string FATimesCircle = "\uf057";

        public const string FAExclamationCircle = "\uf06a";

        public const string FACalender = "\uf073";

        public const string FAStar = "\uf005";

        public const string FAHelp = "\uf059";

        public const string FARadioButtonON = "\uf192";

        public const string FALine = "\uf2d1";

        public const string FADollarCircle = "\uf2e8";

        public const string FASignOut = "\uf08b";

        public const string FAClose = "\uf00d";

        public const string FAPencil = "\uf040";

        public const string FAPhone = "\uf095";

        public const string FALock = "\uf023";

        public const string FAUnlock = "\uf09c";

        public const string FABullet = "\u2022";


        //FA Solid
        public const string FABattery = "\uf5df";

        public const string FAWeather = "\uf6c4";

        public const string FACog = "\uf013";

        public const string FAPlus = "\uf067";

        public const string FAAlert = "\uf071";

        public const string FASlash = "\uf715";

        public const string FAMinus = "\uf068";

        public const string FATimes = "\uf00d";

        public const string FACloud = "\uf0c2";

        public const string FAWiFi = "\uf1eb";

        public const string FABell = "\uf0f3";

        public const string FABellSlash = "\uf1f6";

        public const string FAGripLines = "\uf7a4";

        public const string FADeleteLeft = "\uf55a";

        public const string FACircleExclamation = "\uf06a";


        //FA Brands
        public const string FABluetooth = "\uf294";

        //OCM Icons
        public const string UnLockIcon = "\ue900";

        public const string LockIcon = "\ue901";

        public const string RetractIcon = "\ue903";

        public const string ExtendIcon = "\ue902";

        public const string NoConnection = "\ue904";

        public const string Info = "\ue905";

        public const string ClosedHeart = "\ue906";

        public const string OpenHeart = "\ue907";
    }
}