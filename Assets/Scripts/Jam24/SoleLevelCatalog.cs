using System;
using UnityEngine;

namespace Jam24
{
    public enum MechanismType
    {
        FixedCurrent, RotatingJet, WaterValve, BubbleColumn, RockDiverter,
        BounceShell, SeaweedGate, FlowDivider, PulseCurrent, PressureSwitch
    }

    [Serializable]
    public sealed class SoleLevel
    {
        public string title;
        public string chapter;
        public string tutorial;
        public string slipperName;
        public Vector2 start;
        public Vector2 nest;
        public Vector2[] route;
        public MechanismSpec[] mechanisms;
        public int parActions;
        public bool hasPearl;
    }

    [Serializable]
    public struct MechanismSpec
    {
        public MechanismType type;
        public Vector2 position;
        public int initialState;
        public int requiredState;
        public int stateCount;
        public bool timingOnly;

        public MechanismSpec(MechanismType type, float x, float y, int initial, int required, int states = 2, bool timing = false)
        {
            this.type = type;
            position = new Vector2(x, y);
            initialState = initial;
            requiredState = required;
            stateCount = Mathf.Max(1, states);
            timingOnly = timing;
        }
    }

    public static class SoleLevelCatalog
    {
        public const int Count = 10;

        public static SoleLevel Get(int index)
        {
            index = Mathf.Clamp(index, 0, Count - 1);
            return index switch
            {
                0 => L("Một cú đẩy nhẹ", "SHALLOW WATER", "Dép tự trôi! Bật dòng nước trước khi nó đi tới vòi.", "Dép Cam Bãi Biển", 1,
                    V(-6,-2), V(6,-1.8f), R((-4,-2),(-1,-1.8f),(2,-1.7f),(5,-1.8f)), M(MechanismType.FixedCurrent,-4,-2,0,1)),
                1 => L("Quay đúng hướng", "SHALLOW WATER", "Chạm vòi phun để xoay theo các góc cố định.", "Dép Sọc Thủy Thủ", 1,
                    V(-5,-2.2f), V(5.7f,1.5f), R((-3,-2),(-.5,-1),(2,.2f),(4,1.2f)), M(MechanismType.RotatingJet,-3.8f,-2.4f,0,1,4)),
                2 => L("Chặn dòng sai", "SHALLOW WATER", "Di chuyển đá vào nhánh nguy hiểm để đổi hướng dòng.", "Dép Đá San Hô", 1,
                    V(-5.5f,1.7f), V(5.5f,-2), R((-3,1.4f),(-1,.5f),(1,-.5f),(4,-1.6f)), M(MechanismType.RockDiverter,0,1.7f,0,1)),
                3 => L("Đi lên bằng bong bóng", "SHALLOW WATER", "Bong bóng nâng dép; dòng nước phía trên đưa dép về tổ.", "Dép Bong Bóng", 1,
                    V(-4.8f,-3), V(5.3f,2.2f), R((-3,-2.7f),(-1.5,-.5f),(-1,1.8f),(2,2.2f),(4.5,2.2f)), M(MechanismType.BubbleColumn,-2,-1,0,1)),
                4 => L2("Cú bật của vỏ sò", "CORAL GARDEN", "Chuẩn bị vòi nước và mở vỏ sò trước khi thả dép.", "Dép Ngọc Trai", 2,
                    V(-5,-2.6f), V(5,1.8f), R((-3,-2.4f),(-1.4,-2),(0,0),(2.4,1.2f),(4.4,1.7f)),
                    M(MechanismType.RotatingJet,-4,-2.7f,0,1,4), M(MechanismType.BounceShell,-1.2f,-2.4f,0,1)),
                5 => L2("Cánh cửa rong biển", "CORAL GARDEN", "Mở rong và bật bong bóng đúng lúc khi dép đang trôi.", "Dép Lá Rong", 2,
                    V(-5.4f,-2), V(5,1.8f), R((-3,-1.8f),(-1,-1.6f),(0,.2f),(1,1.6f),(4.5,1.8f)),
                    M(MechanismType.SeaweedGate,-1.5f,-1.7f,0,1), M(MechanismType.BubbleColumn,0,-.6f,0,1)),
                6 => LP("Ngã ba san hô", "CORAL GARDEN", "Xoay bộ chia qua nhánh trên để lấy ngọc trai trước khi về tổ.", "Dép San Hô Vàng", 1,
                    V(-5.5f,-1.8f), V(5.4f,-1), R((-3,-1.5f),(-1,0),(0,2.2f),(2,1),(4,-.6f)), M(MechanismType.FlowDivider,-1.5f,-.5f,0,1)),
                7 => L3("Đẩy, giữ, nâng", "CORAL GARDEN", "Tạo chuỗi ba bước: đẩy → mở rong → nâng.", "Dép Ba Dòng", 3,
                    V(-5.5f,-2.5f), V(5.3f,2.3f), R((-3.5f,-2.3f),(-1.5f,-2),(0,-.2f),(1,1.8f),(4.5f,2.2f)),
                    M(MechanismType.WaterValve,-4,-2.8f,0,1), M(MechanismType.SeaweedGate,-1.5f,-2,0,1), M(MechanismType.BubbleColumn,0,-.7f,0,1)),
                8 => L("Dòng nước theo nhịp", "BUBBLE CAVE", "Chạm dòng nhịp đúng lúc để giữ lực đẩy khi dép đi qua.", "Dép Nhịp Sóng", 0,
                    V(-5.6f,-2), V(5.5f,1.7f), R((-3,-1.8f),(-.5,-.4f),(1.5,.8f),(4.5,1.5f)), M(MechanismType.PulseCurrent,-1,0,0,1,2,true)),
                _ => L3("Hòn đá trên công tắc", "BUBBLE CAVE", "Đặt đá lên công tắc để mở cổng, rồi xoay vòi.", "Dép Công Tắc", 2,
                    V(-5.5f,-2.4f), V(5.5f,1.6f), R((-3.5f,-2.1f),(-1,-1),(1,.2f),(3,1.2f),(5,1.5f)),
                    M(MechanismType.RockDiverter,-3,1.8f,0,1), M(MechanismType.RotatingJet,-4,-2.5f,0,1,4),
                    M(MechanismType.PressureSwitch,.5f,1.8f,0,1,2,true)),
            };
        }

        private static SoleLevel L(string title, string chapter, string tutorial, string slipper, int par, Vector2 start, Vector2 nest, Vector2[] route, params MechanismSpec[] mechanisms) =>
            new() { title=title, chapter=chapter, tutorial=tutorial, slipperName=slipper, parActions=par, start=start, nest=nest, route=route, mechanisms=mechanisms };
        private static SoleLevel L2(string a,string b,string c,string d,int e,Vector2 f,Vector2 g,Vector2[] h,MechanismSpec i,MechanismSpec j)=>L(a,b,c,d,e,f,g,h,i,j);
        private static SoleLevel L3(string a,string b,string c,string d,int e,Vector2 f,Vector2 g,Vector2[] h,MechanismSpec i,MechanismSpec j,MechanismSpec k)=>L(a,b,c,d,e,f,g,h,i,j,k);
        private static SoleLevel LP(string a,string b,string c,string d,int e,Vector2 f,Vector2 g,Vector2[] h,MechanismSpec i){var l=L(a,b,c,d,e,f,g,h,i);l.hasPearl=true;return l;}
        private static MechanismSpec M(MechanismType t,float x,float y,int initial,int required,int states=2,bool timing=false)=>new(t,x,y,initial,required,states,timing);
        private static Vector2 V(float x,float y)=>new(x,y);
        private static Vector2[] R(params (double x,double y)[] points){var r=new Vector2[points.Length];for(int i=0;i<points.Length;i++)r[i]=V((float)points[i].x,(float)points[i].y);return r;}
    }
}
