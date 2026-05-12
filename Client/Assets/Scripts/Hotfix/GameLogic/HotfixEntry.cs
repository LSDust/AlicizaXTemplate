using System.Collections.Generic;
using System.Reflection;
using AlicizaX;
using AlicizaX.Localization;
using GameLogic.Player;
using GameLogic.UI;

namespace GameLogic
{
    public static class HotfixEntry
    {
        private static List<Assembly> _hotfixAssembly;
        public static List<Assembly> HotfixAssembly => _hotfixAssembly;

        public static void Entrance(object[] objects)
        {
            Log.Info("HotFix Logic Entry!");
            _hotfixAssembly = (List<Assembly>)objects[0];
            GameLocaizationTable locaizationTable = GameApp.Resource.LoadAsset<GameLocaizationTable>("LocalizationTable");
            GameApp.Localization.IncreAddLocalizationConfig(locaizationTable);
            if (!AppServices.App.TryGet<IFakePlayerDataService>(out _))
            {
                AppServices.App.Register<IFakePlayerDataService>(new FakePlayerDataService());
            }

            GameApp.UI.ShowUI<UIHomeWindow>();
        }
    }
}
