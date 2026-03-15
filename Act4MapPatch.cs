using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Multiplayer.Game; 
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sts2Act4Mod.Patches;


[HarmonyPatch]
public static class Act4Logic
{
    
    // 状态同步修复 (State Synchronization)

    // 游戏使用同步器防止多端操作冲突。当进入第四层(Index 3)时，必须强行重置同步器，
    // 否则地图点击事件会被判定为非法
    [HarmonyPatch(typeof(RunManager), nameof(RunManager.SetActInternal))]
    [HarmonyPostfix]
    public static void Postfix_SyncAct(RunManager __instance, int actIndex)
    {
        // 仅在进入自定义的第四层（索引为 3）时触发
        if (actIndex == 3)
        {
            var state = AccessTools.Property(typeof(RunManager), "State").GetValue(__instance) as RunState;
            if (state == null) return;

            // 利用反射抓取私有的 MapSelectionSynchronizer 实例
            var syncField = AccessTools.GetDeclaredFields(typeof(RunManager))
                .FirstOrDefault(f => f.FieldType == typeof(MapSelectionSynchronizer));
            
            if (syncField != null)
            {
                var synchronizer = syncField.GetValue(__instance) as MapSelectionSynchronizer;
                if (synchronizer != null)
                {
                    // 强行调用“地图生成前”逻辑，以此重置同步器状态，使其接受新 Act 的点击
                    AccessTools.Method(typeof(MapSelectionSynchronizer), "BeforeMapGenerated")?.Invoke(synchronizer, null);
                    GD.Print("[Act 4 Mod] 同步器状态已强制更新至第四幕，解决地图点击无响应。");
                }
            }
        }
    }

 
    //  act4注入 (Sequence Injection)
    // 原版游戏在 Act 3 结束后会直接触发结局逻辑。
    // 在此截获跳转指令，向 Acts 列表中动态添加自定义的 FinalAct，并跳转过去。
    [HarmonyPatch(typeof(RunManager), nameof(RunManager.EnterNextAct))]
    [HarmonyPrefix]
    public static bool Prefix_Sequence(RunManager __instance, ref Task __result)
    {
        var state = AccessTools.Property(typeof(RunManager), "State").GetValue(__instance) as RunState;
        if (state == null) return true;

        // 当玩家完成 Act 3 (索引为 2) 准备前进时
        if (state.CurrentActIndex == 2)
        {
            var acts = state.Acts.ToList();
            // 确保不会重复添加
            if (acts.Count == 3)
            {
                var finalAct = ModelDb.Act<FinalAct>().ToMutable();
                // 必须在跳转前手动调用 GenerateRooms 初始化该层的 Boss、精英、事件等数据容器
                finalAct.GenerateRooms(state.Rng.UpFront, state.UnlockState, state.Players.Count > 1);
                
                acts.Add(finalAct);
                // 利用反射将修改后的列表写回只读属性 Acts
                AccessTools.Property(typeof(RunState), "Acts").SetValue(state, acts);
                GD.Print("[Act 4 Mod] 已在关卡序列末尾成功注入第四层。");
            }
            return true; // 让原版逻辑继续执行，它会自动识别到下一层并调用 EnterAct(3)
        }
        return true;
    }

   
    // 线性地图构建 (Linear Path Layout)
 
    // 将随机复杂的地图网格抹除，强行建立单一的、垂直的线性路径。
    [HarmonyPatch(typeof(StandardActMap), "AssignPointTypes")]
    [HarmonyPostfix]
    public static void Postfix_Map(StandardActMap __instance)
    {
        var rm = RunManager.Instance;
        var state = AccessTools.Property(typeof(RunManager), "State").GetValue(rm) as RunState;
        
      
        if (state != null && state.Act is FinalAct)
        {
            var grid = AccessTools.Property(typeof(StandardActMap), "Grid").GetValue(__instance) as MapPoint[,];
            if (grid == null) return;

            // A将网格内所有点设为 null，防止背景出现杂乱图标
            for (int r = 1; r < grid.GetLength(1); r++)
                for (int c = 0; c < 7; c++) grid[c, r] = null;

            // 在中间一列 (col 3) 按照顺序生成房间
            SetPoint(grid, 3, 1, MapPointType.RestSite); // 1: 篝火
            SetPoint(grid, 3, 2, MapPointType.Shop);     // 2: 商店
            SetPoint(grid, 3, 3, MapPointType.Elite);    // 3: 精英
            SetPoint(grid, 3, 4, MapPointType.RestSite); // 4: 篝火
            
            // 设定起点和 Boss 房类型
            __instance.StartingMapPoint.PointType = MapPointType.Ancient;
            __instance.BossMapPoint.PointType = MapPointType.Boss;

            // 建立唯一连线：Starting -> 1 -> 2 -> 3 -> 4 -> Boss
            __instance.StartingMapPoint.Children.Clear();
            __instance.StartingMapPoint.AddChildPoint(grid[3, 1]);
            
            grid[3, 1].Children.Clear(); grid[3, 1].AddChildPoint(grid[3, 2]);
            grid[3, 2].Children.Clear(); grid[3, 2].AddChildPoint(grid[3, 3]);
            grid[3, 3].Children.Clear(); grid[3, 3].AddChildPoint(grid[3, 4]);
            
            // 连向终点 Boss
            grid[3, 4].Children.Clear(); grid[3, 4].AddChildPoint(__instance.BossMapPoint);
        }
    }


    // 强制指定怪物 (Static Encounter Setup)


    [HarmonyPatch(typeof(ActModel), nameof(ActModel.GenerateRooms))]
    [HarmonyPostfix]
    public static void Postfix_Rooms(ActModel __instance)
    {
        if (__instance is FinalAct)
        {
            var rooms = AccessTools.Field(typeof(ActModel), "_rooms").GetValue(__instance) as RoomSet;
            if (rooms != null) {
                // 这里可以替换为自定义的 Encounter 类
                rooms.Boss = ModelDb.Encounter<QueenBoss>();
                rooms.eliteEncounters.Clear();
                rooms.eliteEncounters.Add(ModelDb.Encounter<ByrdonisElite>());
            }
        }
    }


    // 资源重定向 (Asset Redirector)
    // 杀戮尖塔2会根据类名自动找资源。
    // 拦截所有视觉请求，将其指向原版的资源。
  

    // 地图卷轴图片重定向
    [HarmonyPatch(typeof(ActModel), "get_MapTopBg")]
    [HarmonyPrefix]
    public static bool Prefix_MapTopBg(ActModel __instance, ref Texture2D __result) {
        if (__instance is FinalAct) {
            __result = MegaCrit.Sts2.Core.Assets.PreloadManager.Cache.GetCompressedTexture2D("res://images/packed/map/map_bgs/glory/map_top_glory.png");
            return false;
        }
        return true;
    }

    [HarmonyPatch(typeof(ActModel), "get_MapMidBg")]
    [HarmonyPrefix]
    public static bool Prefix_MapMidBg(ActModel __instance, ref Texture2D __result) {
        if (__instance is FinalAct) {
            __result = MegaCrit.Sts2.Core.Assets.PreloadManager.Cache.GetCompressedTexture2D("res://images/packed/map/map_bgs/glory/map_middle_glory.png");
            return false;
        }
        return true;
    }

    [HarmonyPatch(typeof(ActModel), "get_MapBotBg")]
    [HarmonyPrefix]
    public static bool Prefix_MapBotBg(ActModel __instance, ref Texture2D __result) {
        if (__instance is FinalAct) {
            __result = MegaCrit.Sts2.Core.Assets.PreloadManager.Cache.GetCompressedTexture2D("res://images/packed/map/map_bgs/glory/map_bottom_glory.png");
            return false;
        }
        return true;
    }

    // 篝火背景重定向
    [HarmonyPatch(typeof(ActModel), "get_RestSiteBackgroundPath")]
    [HarmonyPrefix] public static bool Prefix_Rest(ActModel __instance, ref string __result) {
        if (__instance is FinalAct) { __result = "res://scenes/rest_site/glory_rest_site.tscn"; return false; }
        return true;
    }

    // 地图大场景背景重定向
    [HarmonyPatch(typeof(ActModel), "get_BackgroundScenePath")]
    [HarmonyPrefix] public static bool Prefix_Bg(ActModel __instance, ref string __result) {
        if (__instance is FinalAct) { __result = "res://scenes/backgrounds/glory/glory_background.tscn"; return false; }
        return true;
    }

    // 战斗背景层资源重定向
    [HarmonyPatch(typeof(ActModel), nameof(ActModel.GenerateBackgroundAssets))]
    [HarmonyPrefix] public static bool Prefix_Assets(ActModel __instance, MegaCrit.Sts2.Core.Random.Rng rng, ref BackgroundAssets __result) {
        if (__instance is FinalAct) { __result = new BackgroundAssets("glory", rng); return false; }
        return true;
    }


    // 音频稳定性保护 (Audio Safety)
  
    // 音频控制器内部以 0-2 (Act 1-3) 为索引。当进入第 4 层时，更新音乐会报“索引超出界限”。
    // 直接拦截第四层的音乐自动更新逻辑。
    [HarmonyPatch(typeof(NRunMusicController), "UpdateMusic")]
    [HarmonyPrefix] 
    public static bool Prefix_Music() {
        var state = AccessTools.Property(typeof(RunManager), "State").GetValue(RunManager.Instance) as RunState;
        // 如果身处第四层，跳过原版音轨刷新
        if (state != null && state.Act is FinalAct) return false;
        return true;
    }

 
    private static void SetPoint(MapPoint[,] grid, int col, int row, MapPointType type) {
        MapPoint p = new MapPoint(col, row);
        p.PointType = type; 
        p.CanBeModified = false; 
        grid[col, row] = p;
    }
}