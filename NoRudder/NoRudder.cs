using HarmonyLib;
using System.Reflection;
using UnityEngine;

public class NoRudder : Mod
{
    static public JsonModInfo modInfo;
    Harmony harmony;

    public void Start()
    {
        NoRudder.modInfo = modlistEntry.jsonmodinfo;
        this.harmony = new Harmony("com.destruction.NoRudder");
        this.harmony.PatchAll(Assembly.GetExecutingAssembly());
        NoRudder.Log("NoRudder has been loaded.");
    }

    public void OnModUnload()
    {
        this.harmony.UnpatchAll("com.destruction.NoRudder");
        NoRudder.Log("NoRudder has been unloaded.");
    }

    public static void Log(object message)
    {
        Debug.Log("[" + modInfo.name + "]: " + message.ToString());
    }

    [HarmonyPatch(typeof(SteeringWheelRudderAttachment), "BuildRudderPipesAttachment")]
    public class Patch_BuildRudderPipesAttachment
    {
        // Just for you, TeK. Eat me.
        static bool Prefix() => false;
    }
}
