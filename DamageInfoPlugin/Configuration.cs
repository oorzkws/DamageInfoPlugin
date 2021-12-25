using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Numerics;

namespace NeonCastbarPlugin
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        private bool _mainTargetCastBarColorEnabled = false;
        private bool _focusTargetCastBarColorEnabled = false;

        public bool MainTargetCastBarColorEnabled
        {
	        get => _mainTargetCastBarColorEnabled;
	        set
	        {
		        neonPlugin?.ResetMainTargetCastBar();
		        _mainTargetCastBarColorEnabled = value;
	        }
        }
        public bool FocusTargetCastBarColorEnabled
        {
	        get => _focusTargetCastBarColorEnabled;
	        set
	        {
		        neonPlugin?.ResetFocusTargetCastBar();
		        _focusTargetCastBarColorEnabled = value;
	        }
        }

        [NonSerialized]
        private DalamudPluginInterface pluginInterface;

        [NonSerialized]
        private NeonCastbarPlugin neonPlugin;

        public void Initialize(DalamudPluginInterface pluginInterface, NeonCastbarPlugin neonPlugin)
        {
            this.pluginInterface = pluginInterface;
            this.neonPlugin = neonPlugin;
        }

        public void Save()
        {
            pluginInterface.SavePluginConfig(this);
        }
    }
}
