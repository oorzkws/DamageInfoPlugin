using System;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace NeonCastbarPlugin
{
    public unsafe struct CastbarInfo
    {
        public AtkUnitBase* unitBase;
        public AtkImageNode* gauge;
        public AtkImageNode* bg;
    }
    
    public struct HijackStruct
    {
        public uint kind;
        public uint val1;
        public uint val2;
        public uint icon;
        public uint color;
        public IntPtr text1;
        public IntPtr text2;
        public float unk3;
    }
}