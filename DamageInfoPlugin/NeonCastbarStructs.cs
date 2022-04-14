using FFXIVClientStructs.FFXIV.Component.GUI;

namespace NeonCastbarPlugin
{
    public unsafe struct CastbarInfo
    {
        public AtkUnitBase* UnitBase;
        public AtkImageNode* Gauge;
        public AtkImageNode* Bg;
    }
    
}