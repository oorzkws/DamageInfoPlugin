using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace NeonCastbarPlugin
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        private bool _mainTargetCastBarColorEnabled = true;
        private bool _focusTargetCastBarColorEnabled;

        public bool MainTargetCastBarColorEnabled
        {
	        get => _mainTargetCastBarColorEnabled;
	        set
	        {
		        _neonPlugin?.ResetMainTargetCastBar();
		        _mainTargetCastBarColorEnabled = value;
	        }
        }
        public bool FocusTargetCastBarColorEnabled
        {
	        get => _focusTargetCastBarColorEnabled;
	        set
	        {
		        _neonPlugin?.ResetFocusTargetCastBar();
		        _focusTargetCastBarColorEnabled = value;
	        }
        }

        [NonSerialized]
        private DalamudPluginInterface _pluginInterface;

        [NonSerialized]
        private NeonCastbarPlugin _neonPlugin;

        public void Initialize(DalamudPluginInterface pluginInterface, NeonCastbarPlugin neonPlugin)
        {
            _pluginInterface = pluginInterface;
            _neonPlugin = neonPlugin;
        }

        public void Save()
        {
            _pluginInterface.SavePluginConfig(this);
        }
    }
}
