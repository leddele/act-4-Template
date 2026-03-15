using HarmonyLib;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Models;
using System.Linq;
using System.Collections.Generic;

namespace Sts2Act4Mod.Patches;


/// 存档安全防护补丁 (Save Data Safety Patch)


[HarmonyPatch(typeof(RoomSet), nameof(RoomSet.FromSave))]
public static class RoomSetSavePatch
{

    /// 在 RoomSet 尝试从序列化数据恢复之前执行清理 (Pre-deserialization cleanup)
   
    /// <param name="save">磁盘上读取的原始存档模型 (The raw save model from disk)</param>
    [HarmonyPrefix]
    public static void Prefix(SerializableRoomSet save)
    {
      
        //  防御性初始化 (Defensive Initialization)
        // ==========================================
        // 如果存档由于某种原因（如版本更新或 Mod 冲突）导致列表为 null，
        // 后续的 LINQ 操作（.Where）会抛出 ArgumentNullException。
        // 在此强制确保所有列表实例存在，即便它们是空的。
        if (save.NormalEncounterIds == null) save.NormalEncounterIds = new List<ModelId>();
        if (save.EliteEncounterIds == null) save.EliteEncounterIds = new List<ModelId>();
        if (save.EventIds == null) save.EventIds = new List<ModelId>();

     

        // 过滤普通敌人队列 
        save.NormalEncounterIds = save.NormalEncounterIds
            .Where(id => ModelDb.GetByIdOrNull<EncounterModel>(id) != null)
            .ToList();
            
        // 过滤精英敌人队列
        save.EliteEncounterIds = save.EliteEncounterIds
            .Where(id => ModelDb.GetByIdOrNull<EncounterModel>(id) != null)
            .ToList();
            
        // 过滤事件队列 
        save.EventIds = save.EventIds
            .Where(id => ModelDb.GetByIdOrNull<EventModel>(id) != null)
            .ToList();

    }
}