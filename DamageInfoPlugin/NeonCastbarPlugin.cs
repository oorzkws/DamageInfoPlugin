using Dalamud.Game.Command;
using Dalamud.Plugin;
using System;
using System.Numerics;
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
    // ReSharper disable once ClassNeverInstantiated.Global
    public unsafe class NeonCastbarPlugin : IDalamudPlugin
    {

        private const int TargetInfoGaugeBgNodeIndex = 41;
        private const int TargetInfoGaugeNodeIndex = 43;

        private const int TargetInfoSplitGaugeBgNodeIndex = 2;
        private const int TargetInfoSplitGaugeNodeIndex = 4;
        
        private const int FocusTargetInfoGaugeBgNodeIndex = 13;
        private const int FocusTargetInfoGaugeNodeIndex = 15;

        public string Name => "Neon Castbar";

        private const string CommandName = "/neoncastbar";

        private readonly Configuration _configuration;
        private readonly PluginUI _ui;

        private readonly GameGui _gameGui;
        private DalamudPluginInterface _pi;
        private readonly CommandManager _cmdMgr;
        private readonly TargetManager _targetManager;

        private readonly Hook<SetCastBarDelegate> _setCastBarHook;
        private readonly Hook<SetCastBarDelegate> _setFocusTargetCastBarHook;

        private readonly CastbarInfo _nullCastbarInfo;


        public NeonCastbarPlugin(
            [RequiredVersion("1.0")] GameGui gameGui,
            [RequiredVersion("1.0")] DalamudPluginInterface pi,
            [RequiredVersion("1.0")] CommandManager cmdMgr,
            [RequiredVersion("1.0")] TargetManager targetManager,
            [RequiredVersion("1.0")] SigScanner scanner)
        {
            _gameGui = gameGui;
            _pi = pi;
            _cmdMgr = cmdMgr;
            _targetManager = targetManager;

            _configuration = pi.GetPluginConfig() as Configuration ?? new Configuration();
            _configuration.Initialize(pi, this);
            _ui = new PluginUI(_configuration, this);

            cmdMgr.AddHandler(CommandName, new CommandInfo(OnCommand)
                {HelpMessage = "Display the Neon Castbar configuration interface."});

            _nullCastbarInfo = new CastbarInfo {UnitBase = null, Gauge = null, Bg = null};

            try
            {
                var setCastBarFuncPtr = scanner.ScanText(
                    "48 89 5C 24 ?? 48 89 6C 24 ?? 56 48 83 EC 20 80 7C 24 ?? ?? 49 8B D9 49 8B E8 48 8B F2 74 22 49 8B 09 66 41 C7 41 ?? ?? ?? E8 ?? ?? ?? ?? 66 83 F8 69 75 0D 48 8B 0B BA ?? ?? ?? ?? E8 ?? ?? ?? ??");
                _setCastBarHook = new Hook<SetCastBarDelegate>(setCastBarFuncPtr, (SetCastBarDelegate) SetCastBarDetour);
                
                var setFocusTargetCastBarFuncPtr = scanner.ScanText("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 41 0F B6 F9 49 8B E8 48 8B F2 48 8B D9");
                _setFocusTargetCastBarHook = new Hook<SetCastBarDelegate>(setFocusTargetCastBarFuncPtr, (SetCastBarDelegate) SetFocusTargetCastBarDetour);
            }
            catch (Exception ex)
            {
                PluginLog.Information($"Encountered an error loading NeonCastbarPlugin: {ex.Message}");
                PluginLog.Information("Plugin will not be loaded.");
                
                _setCastBarHook?.Disable();
                _setCastBarHook?.Dispose();
                _setFocusTargetCastBarHook?.Disable();
                _setFocusTargetCastBarHook?.Dispose();
                cmdMgr.RemoveHandler(CommandName);

                throw;
            }
            
            _setCastBarHook.Enable();
            _setFocusTargetCastBarHook.Enable();

            pi.UiBuilder.Draw += DrawUI;
            pi.UiBuilder.OpenConfigUi += DrawConfigUI;
        }

        public void Dispose()
        {
            ResetMainTargetCastBar();
            ResetFocusTargetCastBar();
            _setCastBarHook?.Disable();
            _setCastBarHook?.Dispose();
            _setFocusTargetCastBarHook?.Disable();
            _setFocusTargetCastBarHook?.Dispose();

            _ui.Dispose();
            _cmdMgr.RemoveHandler(CommandName);
            GC.SuppressFinalize(this);
        }

        private void OnCommand(string command, string args)
        {
            _ui.SettingsVisible = true;
        }

        // ReSharper disable once InconsistentNaming
        private void DrawUI()
        {
            _ui.Draw();
        }

        // ReSharper disable once InconsistentNaming
        private void DrawConfigUI()
        {
            _ui.SettingsVisible = true;
        }
        
        private CastbarInfo GetTargetInfoUiElements()
        {
            var unitbase = (AtkUnitBase*) _gameGui.GetAddonByName("_TargetInfo", 1).ToPointer();

            if (unitbase == null) return _nullCastbarInfo;
            
            return new CastbarInfo
            {
                UnitBase = unitbase,
                Gauge = (AtkImageNode*) unitbase->UldManager.NodeList[TargetInfoGaugeNodeIndex],
                Bg = (AtkImageNode*) unitbase->UldManager.NodeList[TargetInfoGaugeBgNodeIndex]
            };
        }

        private CastbarInfo GetTargetInfoSplitUiElements()
        {
            var unitbase = (AtkUnitBase*) _gameGui.GetAddonByName("_TargetInfoCastBar", 1).ToPointer();
            
            if (unitbase == null) return _nullCastbarInfo;
            
            return new CastbarInfo
            {
                UnitBase = unitbase,
                Gauge = (AtkImageNode*) unitbase->UldManager.NodeList[TargetInfoSplitGaugeNodeIndex],
                Bg = (AtkImageNode*) unitbase->UldManager.NodeList[TargetInfoSplitGaugeBgNodeIndex]
            };
        }
        
        private CastbarInfo GetFocusTargetUiElements()
        {
            var unitbase = (AtkUnitBase*) _gameGui.GetAddonByName("_FocusTargetInfo", 1).ToPointer();
            
            if (unitbase == null) return _nullCastbarInfo;
            
            return new CastbarInfo
            {
                UnitBase = unitbase,
                Gauge = (AtkImageNode*) unitbase->UldManager.NodeList[FocusTargetInfoGaugeNodeIndex],
                Bg = (AtkImageNode*) unitbase->UldManager.NodeList[FocusTargetInfoGaugeBgNodeIndex]
            };
        }

        public void ResetMainTargetCastBar()
        {
            var targetInfo = GetTargetInfoUiElements();
            var splitInfo = GetTargetInfoSplitUiElements();

            if (targetInfo.UnitBase != null && targetInfo.Gauge != null && targetInfo.Bg != null)
            {
                targetInfo.Gauge->AtkResNode.Color.R = 0xFF;
                targetInfo.Gauge->AtkResNode.Color.G = 0xFF;
                targetInfo.Gauge->AtkResNode.Color.B = 0xFF;
                targetInfo.Gauge->AtkResNode.Color.A = 0xFF;
                
                targetInfo.Bg->AtkResNode.Color.R = 0xFF;
                targetInfo.Bg->AtkResNode.Color.G = 0xFF;
                targetInfo.Bg->AtkResNode.Color.B = 0xFF;
                targetInfo.Bg->AtkResNode.Color.A = 0xFF;
            }

            if (splitInfo.UnitBase == null || splitInfo.Gauge == null || splitInfo.Bg == null) return;
            splitInfo.Gauge->AtkResNode.Color.R = 0xFF;
            splitInfo.Gauge->AtkResNode.Color.G = 0xFF;
            splitInfo.Gauge->AtkResNode.Color.B = 0xFF;
            splitInfo.Gauge->AtkResNode.Color.A = 0xFF;
                
            splitInfo.Bg->AtkResNode.Color.R = 0xFF;
            splitInfo.Bg->AtkResNode.Color.G = 0xFF;
            splitInfo.Bg->AtkResNode.Color.B = 0xFF;
            splitInfo.Bg->AtkResNode.Color.A = 0xFF;
        }

        public void ResetFocusTargetCastBar()
        {
            var ftInfo = GetFocusTargetUiElements();

            if (ftInfo.UnitBase == null || ftInfo.Gauge == null || ftInfo.Bg == null) return;
            ftInfo.Gauge->AtkResNode.Color.R = 0xFF;
            ftInfo.Gauge->AtkResNode.Color.G = 0xFF;
            ftInfo.Gauge->AtkResNode.Color.B = 0xFF;
            ftInfo.Gauge->AtkResNode.Color.A = 0xFF;
                
            ftInfo.Bg->AtkResNode.Color.R = 0xFF;
            ftInfo.Bg->AtkResNode.Color.G = 0xFF;
            ftInfo.Bg->AtkResNode.Color.B = 0xFF;
            ftInfo.Bg->AtkResNode.Color.A = 0xFF;
        }

        private delegate void SetCastBarDelegate(IntPtr thisPtr, IntPtr a2, IntPtr a3, IntPtr a4, char a5);

        private void SetCastBarDetour(IntPtr thisPtr, IntPtr a2, IntPtr a3, IntPtr a4, char a5)
        {
            if (!_configuration.MainTargetCastBarColorEnabled)
            {
                _setCastBarHook.Original(thisPtr, a2, a3, a4, a5);
                return;
            }

            var targetInfo = GetTargetInfoUiElements();
            var splitInfo = GetTargetInfoSplitUiElements();

            var combinedInvalid = targetInfo.UnitBase == null || targetInfo.Gauge == null || targetInfo.Bg == null;
            var splitInvalid = splitInfo.UnitBase == null || splitInfo.Gauge == null || splitInfo.Bg == null;
            
            if (combinedInvalid && splitInvalid)
            {
                _setCastBarHook.Original(thisPtr, a2, a3, a4, a5);
                return;
            }

            if (thisPtr.ToPointer() == targetInfo.UnitBase && !combinedInvalid)
            {
                var mainTarget = _targetManager.Target;
                ColorCastBar(mainTarget, targetInfo, _setCastBarHook, thisPtr, a2, a3, a4, a5);
            }
            else if (thisPtr.ToPointer() == splitInfo.UnitBase && !splitInvalid)
            {
                var mainTarget = _targetManager.Target;
                ColorCastBar(mainTarget, splitInfo, _setCastBarHook, thisPtr, a2, a3, a4, a5);
            }
        }

        private void SetFocusTargetCastBarDetour(IntPtr thisPtr, IntPtr a2, IntPtr a3, IntPtr a4, char a5)
        {
            if (!_configuration.FocusTargetCastBarColorEnabled)
            {
                _setFocusTargetCastBarHook.Original(thisPtr, a2, a3, a4, a5);
                return;
            }
            
            var ftInfo = GetFocusTargetUiElements();
            
            var focusTargetInvalid = ftInfo.UnitBase == null || ftInfo.Gauge == null || ftInfo.Bg == null;

            if (thisPtr.ToPointer() != ftInfo.UnitBase || focusTargetInvalid) return;
            var focusTarget = _targetManager.FocusTarget;
            ColorCastBar(focusTarget, ftInfo, _setFocusTargetCastBarHook, thisPtr, a2, a3, a4, a5);
        }
        
        private static void ColorCastBar(GameObject target, CastbarInfo info, Hook<SetCastBarDelegate> hook,
            IntPtr thisPtr, IntPtr a2, IntPtr a3, IntPtr a4, char a5)
        {
            if (target == null)
            {
                hook.Original(thisPtr, a2, a3, a4, a5);
                return;
            }

            // Assuming scale is 0 - 1
            var scaleX = info.Gauge->AtkResNode.GetScaleX();
            var castColor = new Vector4(scaleX, 1.0f-scaleX, 1.0f-scaleX, 1.0f);

            info.Gauge->AtkResNode.Color.R = (byte) (castColor.X * 255);
            info.Gauge->AtkResNode.Color.G = (byte) (castColor.Y * 255);
            info.Gauge->AtkResNode.Color.B = (byte) (castColor.Z * 255);
            info.Gauge->AtkResNode.Color.A = (byte) (castColor.W * 255);

            hook.Original(thisPtr, a2, a3, a4, a5);
        }
    }
}