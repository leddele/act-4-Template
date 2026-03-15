using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace Sts2Act4Mod; 

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    internal const string ModId = "Sts2Act4Mod"; 

    public static void Initialize()
    {
        Harmony harmony = new Harmony(ModId);
        harmony.PatchAll();
        GD.Print(">>> [Act 4 Mod] 已激活。");
    }
}