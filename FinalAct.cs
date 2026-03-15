using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Map;

namespace Sts2Act4Mod;

public sealed class FinalAct : ActModel
{
   protected override int BaseNumberOfRooms => 7; 

    public new LocString Title => new LocString("acts", "FINAL_ACT.title");


    // --- 怪物与事件配置 ---
    public override IEnumerable<EncounterModel> GenerateAllEncounters()
    {
        return new List<EncounterModel> {
            ModelDb.Encounter<QueenBoss>(),
            ModelDb.Encounter<ByrdonisElite>()
        };
    }

    public override IEnumerable<AncientEventModel> AllAncients 
{
    get 
    {
        var neow = ModelDb.AncientEvent<Neow>();
        if (neow != null) return new List<AncientEventModel> { neow };
        return new List<AncientEventModel>();
    }
}

    public override IEnumerable<AncientEventModel> GetUnlockedAncients(UnlockState state) => AllAncients;

    // --- 视听配置 (借用 Act 3) ---
    public override string[] BgMusicOptions => new string[] { "event:/music/act3_boss_queen", "event:/music/act3_boss_queen" };
    public override string[] MusicBankPaths => new string[] { "res://banks/desktop/act3_a1.bank", "res://banks/desktop/act3_a2.bank" };
    public override string AmbientSfx => "event:/sfx/ambience/act3_ambience";
     // 地图背景底色
public override Color MapBgColor => new Color(0f, 0f, 0f, 0f); 
    
    // 已走过的路径连线颜色
    public override Color MapTraveledColor => new Color("ffffff"); 
    
    //  未走过的路径连线颜色（改为亮灰色，别太黑）
    public override Color MapUntraveledColor => new Color("b0b0b0"); 
    public override string ChestSpineSkinNameNormal => "act3";
    public override string ChestSpineSkinNameStroke => "act3_stroke";
    public override string ChestOpenSfx => "event:/sfx/ui/treasure/treasure_act3";
    public override IEnumerable<EncounterModel> BossDiscoveryOrder => new List<EncounterModel> { ModelDb.Encounter<QueenBoss>() };
    public override IEnumerable<EventModel> AllEvents => new List<EventModel>();
    protected override void ApplyActDiscoveryOrderModifications(UnlockState unlockState) { }
    public override MapPointTypeCounts GetMapPointTypes(Rng mapRng) => new MapPointTypeCounts(mapRng);
}