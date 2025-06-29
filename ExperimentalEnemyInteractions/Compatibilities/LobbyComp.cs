using System;
using LobbyCompatibility.Enums;
using LobbyCompatibility.Features;

namespace NaturalSelection.Compatibility
{
    internal class LobbyCompCompatibility
    {
        public static void RegisterLobbyComp(string GUID, Version version)
        {
            PluginHelper.RegisterPlugin(GUID,version, CompatibilityLevel.Everyone, VersionStrictness.Minor);
        }
    }
}
