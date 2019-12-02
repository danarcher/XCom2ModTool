namespace XCom2ModTool
{
    internal class XCom2
    {
        private static readonly string BaseInternalName = "XCOM2";
        private static readonly string WotcInternalName = "WOTC";

        private static readonly string BaseDisplayName = "<cyan>XCOM 2 (Base/Legacy)</cyan>";
        private static readonly string WotcDisplayName = "<red>War of the Chosen</red>";

        private static readonly string SteamAppName = "XCOM 2";
        private static readonly string WotcFolderName = "XCom2-WarOfTheChosen";
        private static readonly string SdkSteamAppName = "XCOM 2 SDK";
        private static readonly string WotcSdkSteamAppName = "XCOM 2 War of the Chosen SDK";

        private static readonly string UserGameFolderName = "XCOM2";
        private static readonly string WotcUserGameFolderName = "XCOM2 War of the Chosen";

        private static readonly string BaseHighlanderName = "X2CommunityHighlander";
        private static readonly string WotcHighlanderName = "X2WOTCCommunityHighlander";

        public static readonly XCom2Edition Base = new XCom2Edition(BaseInternalName, BaseDisplayName, SteamAppName, null, SdkSteamAppName, UserGameFolderName, BaseHighlanderName);
        public static readonly XCom2Edition Wotc = new XCom2Edition(WotcInternalName, WotcDisplayName, SteamAppName, WotcFolderName, WotcSdkSteamAppName, WotcUserGameFolderName, WotcHighlanderName, isExpansion: true);

        public static readonly XCom2Edition[] Editions = { Base, Wotc };
    }
}
