namespace XCom2ModTool
{
    internal class XCom2
    {
        private static readonly string BaseDisplayName = "XCOM 2";
        private static readonly string WotcDisplayName = "War of the Chosen";

        private static readonly string SteamAppName = "XCOM 2";
        private static readonly string WotcFolderName = "XCom2-WarOfTheChosen";
        private static readonly string SdkSteamAppName = "XCOM 2 SDK";
        private static readonly string WotcSdkSteamAppName = "XCOM 2 War of the Chosen SDK";

        private static readonly string UserGameFolderName = "XCOM2";
        private static readonly string WotcUserGameFolderName = "XCOM2 War of the Chosen";

        public static readonly XCom2Edition Base = new XCom2Edition(BaseDisplayName, SteamAppName, null, SdkSteamAppName, UserGameFolderName);
        public static readonly XCom2Edition Wotc = new XCom2Edition(WotcDisplayName, SteamAppName, WotcFolderName, WotcSdkSteamAppName, WotcUserGameFolderName, isExpansion: true);
    }
}
