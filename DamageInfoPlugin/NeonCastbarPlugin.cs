using Dalamud.Game.Command;
using Dalamud.Plugin;
using System;
using System.Numerics;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Gui;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace NeonCastbarPlugin
{
    public unsafe class NeonCastbarPlugin : IDalamudPlugin
    {
        // when a flytext 
        private const int CleanupInterval = 10000;

        private const int TargetInfoGaugeBgNodeIndex = 41;
        private const int TargetInfoGaugeNodeIndex = 43;

        private const int TargetInfoSplitGaugeBgNodeIndex = 2;
        private const int TargetInfoSplitGaugeNodeIndex = 4;
        
        private const int FocusTargetInfoGaugeBgNodeIndex = 13;
        private const int FocusTargetInfoGaugeNodeIndex = 15;

        public string Name => "Neon Castbar";

        private const string CommandName = "/neoncastbar";

        private Configuration configuration;
        private PluginUI ui;

        private GameGui gameGui;
        private DalamudPluginInterface pi;
        private CommandManager cmdMgr;
        private TargetManager targetManager;

        private Hook<SetCastBarDelegate> setCastBarHook;
        private Hook<SetCastBarDelegate> setFocusTargetCastBarHook;

        private CastbarInfo _nullCastbarInfo;


        public NeonCastbarPlugin(
            [RequiredVersion("1.0")] GameGui gameGui,
            [RequiredVersion("1.0")] DalamudPluginInterface pi,
            [RequiredVersion("1.0")] CommandManager cmdMgr,
            [RequiredVersion("1.0")] DataManager dataMgr,
            [RequiredVersion("1.0")] TargetManager targetManager,
            [RequiredVersion("1.0")] SigScanner scanner)
        {
            this.gameGui = gameGui;
            this.pi = pi;
            this.cmdMgr = cmdMgr;
            this.targetManager = targetManager;

            configuration = pi.GetPluginConfig() as Configuration ?? new Configuration();
            configuration.Initialize(pi, this);
            ui = new PluginUI(configuration, this);

            cmdMgr.AddHandler(CommandName, new CommandInfo(OnCommand)
                {HelpMessage = "Display the Neon Castbar configuration interface."});

            _nullCastbarInfo = new CastbarInfo {unitBase = null, gauge = null, bg = null};

            try
            {
                IntPtr setCastBarFuncPtr = scanner.ScanText(
                    "48 89 5C 24 ?? 48 89 6C 24 ?? 56 48 83 EC 20 80 7C 24 ?? ?? 49 8B D9 49 8B E8 48 8B F2 74 22 49 8B 09 66 41 C7 41 ?? ?? ?? E8 ?? ?? ?? ?? 66 83 F8 69 75 0D 48 8B 0B BA ?? ?? ?? ?? E8 ?? ?? ?? ??");
                setCastBarHook = new Hook<SetCastBarDelegate>(setCastBarFuncPtr, (SetCastBarDelegate) SetCastBarDetour);
                
                IntPtr setFocusTargetCastBarFuncPtr = scanner.ScanText("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 41 0F B6 F9 49 8B E8 48 8B F2 48 8B D9");
                setFocusTargetCastBarHook = new Hook<SetCastBarDelegate>(setFocusTargetCastBarFuncPtr, (SetCastBarDelegate) SetFocusTargetCastBarDetour);
            }
            catch (Exception ex)
            {
                PluginLog.Information($"Encountered an error loading NeonCastbarPlugin: {ex.Message}");
                PluginLog.Information("Plugin will not be loaded.");
                
                setCastBarHook?.Disable();
                setCastBarHook?.Dispose();
                setFocusTargetCastBarHook?.Disable();
                setFocusTargetCastBarHook?.Dispose();
                cmdMgr.RemoveHandler(CommandName);

                throw;
            }
            
            setCastBarHook.Enable();
            setFocusTargetCastBarHook.Enable();

            pi.UiBuilder.Draw += DrawUI;
            pi.UiBuilder.OpenConfigUi += DrawConfigUI;
        }

        public void Dispose()
        {
            ResetMainTargetCastBar();
            ResetFocusTargetCastBar();
            setCastBarHook?.Disable();
            setCastBarHook?.Dispose();
            setFocusTargetCastBarHook?.Disable();
            setFocusTargetCastBarHook?.Dispose();

            ui.Dispose();
            cmdMgr.RemoveHandler(CommandName);
            pi.Dispose();
        }

        private void OnCommand(string command, string args)
        {
            ui.SettingsVisible = true;
        }

        private void DrawUI()
        {
            ui.Draw();
        }

        private void DrawConfigUI()
        {
            ui.SettingsVisible = true;
        }
        
        private CastbarInfo GetTargetInfoUiElements()
        {
            AtkUnitBase* unitbase = (AtkUnitBase*) gameGui.GetAddonByName("_TargetInfo", 1).ToPointer();

            if (unitbase == null) return _nullCastbarInfo;
            
            return new CastbarInfo
            {
                unitBase = unitbase,
                gauge = (AtkImageNode*) unitbase->UldManager.NodeList[TargetInfoGaugeNodeIndex],
                bg = (AtkImageNode*) unitbase->UldManager.NodeList[TargetInfoGaugeBgNodeIndex]
            };
        }

        private CastbarInfo GetTargetInfoSplitUiElements()
        {
            AtkUnitBase* unitbase = (AtkUnitBase*) gameGui.GetAddonByName("_TargetInfoCastBar", 1).ToPointer();
            
            if (unitbase == null) return _nullCastbarInfo;
            
            return new CastbarInfo
            {
                unitBase = unitbase,
                gauge = (AtkImageNode*) unitbase->UldManager.NodeList[TargetInfoSplitGaugeNodeIndex],
                bg = (AtkImageNode*) unitbase->UldManager.NodeList[TargetInfoSplitGaugeBgNodeIndex]
            };
        }
        
        private CastbarInfo GetFocusTargetUiElements()
        {
            AtkUnitBase* unitbase = (AtkUnitBase*) gameGui.GetAddonByName("_FocusTargetInfo", 1).ToPointer();
            
            if (unitbase == null) return _nullCastbarInfo;
            
            return new CastbarInfo
            {
                unitBase = unitbase,
                gauge = (AtkImageNode*) unitbase->UldManager.NodeList[FocusTargetInfoGaugeNodeIndex],
                bg = (AtkImageNode*) unitbase->UldManager.NodeList[FocusTargetInfoGaugeBgNodeIndex]
            };
        }

        public void ResetMainTargetCastBar()
        {
            var targetInfo = GetTargetInfoUiElements();
            var splitInfo = GetTargetInfoSplitUiElements();

            if (targetInfo.unitBase != null && targetInfo.gauge != null && targetInfo.bg != null)
            {
                targetInfo.gauge->AtkResNode.Color.R = 0xFF;
                targetInfo.gauge->AtkResNode.Color.G = 0xFF;
                targetInfo.gauge->AtkResNode.Color.B = 0xFF;
                targetInfo.gauge->AtkResNode.Color.A = 0xFF;
                
                targetInfo.bg->AtkResNode.Color.R = 0xFF;
                targetInfo.bg->AtkResNode.Color.G = 0xFF;
                targetInfo.bg->AtkResNode.Color.B = 0xFF;
                targetInfo.bg->AtkResNode.Color.A = 0xFF;
            }

            if (splitInfo.unitBase != null && splitInfo.gauge != null && splitInfo.bg != null)
            {
                splitInfo.gauge->AtkResNode.Color.R = 0xFF;
                splitInfo.gauge->AtkResNode.Color.G = 0xFF;
                splitInfo.gauge->AtkResNode.Color.B = 0xFF;
                splitInfo.gauge->AtkResNode.Color.A = 0xFF;
                
                splitInfo.bg->AtkResNode.Color.R = 0xFF;
                splitInfo.bg->AtkResNode.Color.G = 0xFF;
                splitInfo.bg->AtkResNode.Color.B = 0xFF;
                splitInfo.bg->AtkResNode.Color.A = 0xFF;
            }
        }

        public void ResetFocusTargetCastBar()
        {
            var ftInfo = GetFocusTargetUiElements();

            if (ftInfo.unitBase != null && ftInfo.gauge != null && ftInfo.bg != null)
            {
                ftInfo.gauge->AtkResNode.Color.R = 0xFF;
                ftInfo.gauge->AtkResNode.Color.G = 0xFF;
                ftInfo.gauge->AtkResNode.Color.B = 0xFF;
                ftInfo.gauge->AtkResNode.Color.A = 0xFF;
                
                ftInfo.bg->AtkResNode.Color.R = 0xFF;
                ftInfo.bg->AtkResNode.Color.G = 0xFF;
                ftInfo.bg->AtkResNode.Color.B = 0xFF;
                ftInfo.bg->AtkResNode.Color.A = 0xFF;
            }
        }

        private delegate void SetCastBarDelegate(IntPtr thisPtr, IntPtr a2, IntPtr a3, IntPtr a4, char a5);

        private void SetCastBarDetour(IntPtr thisPtr, IntPtr a2, IntPtr a3, IntPtr a4, char a5)
        {
            if (!configuration.MainTargetCastBarColorEnabled)
            {
                setCastBarHook.Original(thisPtr, a2, a3, a4, a5);
                return;
            }

            var targetInfo = GetTargetInfoUiElements();
            var splitInfo = GetTargetInfoSplitUiElements();

            bool combinedInvalid = targetInfo.unitBase == null || targetInfo.gauge == null || targetInfo.bg == null;
            bool splitInvalid = splitInfo.unitBase == null || splitInfo.gauge == null || splitInfo.bg == null;
            
            if (combinedInvalid && splitInvalid)
            {
                setCastBarHook.Original(thisPtr, a2, a3, a4, a5);
                return;
            }

            if (thisPtr.ToPointer() == targetInfo.unitBase && !combinedInvalid)
            {
                var mainTarget = targetManager.Target;
                ColorCastBar(mainTarget, targetInfo, setCastBarHook, thisPtr, a2, a3, a4, a5);
            }
            else if (thisPtr.ToPointer() == splitInfo.unitBase && !splitInvalid)
            {
                var mainTarget = targetManager.Target;
                ColorCastBar(mainTarget, splitInfo, setCastBarHook, thisPtr, a2, a3, a4, a5);
            }
        }

        private void SetFocusTargetCastBarDetour(IntPtr thisPtr, IntPtr a2, IntPtr a3, IntPtr a4, char a5)
        {
            if (!configuration.FocusTargetCastBarColorEnabled)
            {
                setFocusTargetCastBarHook.Original(thisPtr, a2, a3, a4, a5);
                return;
            }
            
            var ftInfo = GetFocusTargetUiElements();
            
            bool focusTargetInvalid = ftInfo.unitBase == null || ftInfo.gauge == null || ftInfo.bg == null; 
            
            if (thisPtr.ToPointer() == ftInfo.unitBase && !focusTargetInvalid)
            {
                GameObject focusTarget = targetManager.FocusTarget;
                ColorCastBar(focusTarget, ftInfo, setFocusTargetCastBarHook, thisPtr, a2, a3, a4, a5);
            }
        }
        
        private void ColorCastBar(GameObject target, CastbarInfo info, Hook<SetCastBarDelegate> hook,
            IntPtr thisPtr, IntPtr a2, IntPtr a3, IntPtr a4, char a5)
        {
            if (target == null)
            {
                hook.Original(thisPtr, a2, a3, a4, a5);
                return;
            }

            // Assuming scale is 0 - 1
            var scaleX = info.gauge->AtkResNode.GetScaleX();
            var castColor = new Vector4(scaleX, 1.0f-scaleX, 1.0f-scaleX, 1.0f);

            info.gauge->AtkResNode.Color.R = (byte) (castColor.X * 255);
            info.gauge->AtkResNode.Color.G = (byte) (castColor.Y * 255);
            info.gauge->AtkResNode.Color.B = (byte) (castColor.Z * 255);
            info.gauge->AtkResNode.Color.A = (byte) (castColor.W * 255);

            hook.Original(thisPtr, a2, a3, a4, a5);
        }
    }
}