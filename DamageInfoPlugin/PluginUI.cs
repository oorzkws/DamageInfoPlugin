using ImGuiNET;
using System;
using System.Numerics;

namespace NeonCastbarPlugin
{
    // ReSharper disable once InconsistentNaming
    class PluginUI : IDisposable
    {
        private readonly Configuration _configuration;
        private NeonCastbarPlugin _neonCastbarPlugin;

        public bool Visible { get; set; } = false;

        private bool _settingsVisible;
        public bool SettingsVisible
        {
            get => _settingsVisible;
            set => _settingsVisible = value;
        }

        public PluginUI(Configuration configuration, NeonCastbarPlugin neonCastbarPlugin)
        {
            this._configuration = configuration;
            this._neonCastbarPlugin = neonCastbarPlugin;
        }

        public void Dispose()
        {

        }

        public void Draw()
        {
	        DrawSettingsWindow();
        }

        private void DrawSettingsWindow()
        {
	        if (!SettingsVisible) return;

            ImGui.SetNextWindowSize(new Vector2(250, 150), ImGuiCond.FirstUseEver);
            if (ImGui.Begin("Neon Castbar Config", ref _settingsVisible, ImGuiWindowFlags.AlwaysAutoResize))
            {
                // local copies of config properties
                var castBarConfigValue = _configuration.MainTargetCastBarColorEnabled;
                var ftCastBarConfigValue = _configuration.FocusTargetCastBarColorEnabled;
                ImGui.Text("Target Castbar");
                ImGui.SameLine();
                if (ImGui.Checkbox("##maintargetcheck", ref castBarConfigValue))
                {
                    _configuration.MainTargetCastBarColorEnabled = castBarConfigValue;
                    _configuration.Save();
                }

                ImGui.Text("Focus Target Castbar");
                ImGui.SameLine();
                if (ImGui.Checkbox("##focustargetcheck", ref ftCastBarConfigValue))
                {
                    _configuration.FocusTargetCastBarColorEnabled = ftCastBarConfigValue;
                    _configuration.Save();
                }
            }

            ImGui.End();
        }
    }
}
