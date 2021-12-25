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
		        dmgPlugin?.ResetMainTargetCastBar();
		        _mainTargetCastBarColorEnabled = value;
	        }
        }
        public bool FocusTargetCastBarColorEnabled
        {
	        get => _focusTargetCastBarColorEnabled;
	        set
	        {
		        dmgPlugin?.ResetFocusTargetCastBar();
		        _focusTargetCastBarColorEnabled = value;
	        }
        }

        [NonSerialized]
        private DalamudPluginInterface pluginInterface;

        [NonSerialized]
        private DamageInfoPlugin dmgPlugin;

        public void Initialize(DalamudPluginInterface pluginInterface, DamageInfoPlugin dmgPlugin)
        {
            this.pluginInterface = pluginInterface;
            this.dmgPlugin = dmgPlugin;
        }

        public void Save()
        {
            pluginInterface.SavePluginConfig(this);
        }
    }
}
