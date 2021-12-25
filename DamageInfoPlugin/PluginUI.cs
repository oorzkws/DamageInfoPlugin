using ImGuiNET;
using System;
using System.Numerics;

namespace NeonCastbarPlugin
{
    class PluginUI : IDisposable
    {
        private Configuration configuration;
        private NeonCastbarPlugin _neonCastbarPlugin;

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

            ImGui.SetNextWindowSize(new Vector2(400, 500), ImGuiCond.FirstUseEver);
            if (ImGui.Begin("Damage Info Config", ref settingsVisible, ImGuiWindowFlags.AlwaysVerticalScrollbar)) {

                // local copies of config properties
                var castBarConfigValue = configuration.MainTargetCastBarColorEnabled;
	            var ftCastBarConfigValue = configuration.FocusTargetCastBarColorEnabled;

	            if (ImGui.CollapsingHeader("Castbars"))
	            {
		            ImGui.Text("Main target");
		            ImGui.SameLine();
		            if (ImGui.Checkbox("##maintargetcheck", ref castBarConfigValue))
		            {
			            configuration.MainTargetCastBarColorEnabled = castBarConfigValue;
			            configuration.Save();
		            }
		            
		            ImGui.Text("Focus target");
		            ImGui.SameLine();
		            if (ImGui.Checkbox("##focustargetcheck", ref ftCastBarConfigValue))
		            {
			            configuration.FocusTargetCastBarColorEnabled = ftCastBarConfigValue;
			            configuration.Save();
		            }
		            
	            }
            }
            ImGui.End();
        }
    }
}
