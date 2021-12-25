using ImGuiNET;
using System;
using System.Numerics;

namespace NeonCastbarPlugin
{
    class PluginUI : IDisposable
    {
        private Configuration configuration;
        private DamageInfoPlugin damageInfoPlugin;

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

        public PluginUI(Configuration configuration, DamageInfoPlugin damageInfoPlugin)
        {
            this.configuration = configuration;
            this.damageInfoPlugin = damageInfoPlugin;
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

	            if (ImGui.CollapsingHeader("Damage type information"))
	            {
		            ImGui.TextWrapped(
			        "Each attack in the game has a specific damage type, such as blunt, piercing, magic, " +
			        "limit break, \"breath\", and more. The only important damage types for mitigation are " +
			        "physical (encompassing slashing, blunt, and piercing), magic, and breath (referred to the " +
			        "community as \"darkness\" damage).");
		            ImGui.TextWrapped(
			        "Physical damage can be mitigated by reducing an enemy's strength stat, or with moves " +
			        "that specifically mention physical damage reduction. Magic damage can be mitigated by " +
			        "reducing an enemy's intelligence stat, or with moves that specifically mention magic damage " +
			        "reduction. Darkness damage cannot be mitigated by reducing an enemy's stats or mitigating " +
			        "against physical or magic damage - only moves that \"reduce a target's damage dealt\" will " +
			        "affect darkness damage.");
	            }

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
