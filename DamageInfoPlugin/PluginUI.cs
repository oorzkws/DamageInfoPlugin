using ImGuiNET;
using System;
using System.Numerics;

namespace NeonCastbarPlugin
{
    class PluginUI : IDisposable
    {
        private Configuration configuration;
        private NeonCastbarPlugin neonCastbarPlugin;

		private bool visible = false;
        public bool Visible
        {
            get => visible;
            set => visible = value;
        }

        private bool settingsVisible = false;
        public bool SettingsVisible
        {
            get => settingsVisible;
            set => settingsVisible = value;
        }

        public PluginUI(Configuration configuration, NeonCastbarPlugin neonCastbarPlugin)
        {
            this.configuration = configuration;
            this.neonCastbarPlugin = neonCastbarPlugin;
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
            if (ImGui.Begin("Neon Castbar Config", ref settingsVisible, ImGuiWindowFlags.AlwaysAutoResize))
            {
                // local copies of config properties
                var castBarConfigValue = configuration.MainTargetCastBarColorEnabled;
                var ftCastBarConfigValue = configuration.FocusTargetCastBarColorEnabled;
                ImGui.Text("Target Castbar");
                ImGui.SameLine();
                if (ImGui.Checkbox("##maintargetcheck", ref castBarConfigValue))
                {
                    configuration.MainTargetCastBarColorEnabled = castBarConfigValue;
                    configuration.Save();
                }

                ImGui.Text("Focus Target Castbar");
                ImGui.SameLine();
                if (ImGui.Checkbox("##focustargetcheck", ref ftCastBarConfigValue))
                {
                    configuration.FocusTargetCastBarColorEnabled = ftCastBarConfigValue;
                    configuration.Save();
                }
            }

            ImGui.End();
        }
    }
}
