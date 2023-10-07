using HarmonyLib;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace DestinyBedTime
{
    public class BedTime : Mod
    {
        private Harmony harmony;
        public static BedTime instance;
        private BedTimeSetting bedSetting = BedTimeSetting.FULL;

        public static BedTimeSetting BedTimeSetting
        {
            get
            {
                return BedTime.instance.bedSetting;
            }
            private set
            {
                BedTime.instance.bedSetting = value;
                BedTime.instance.ExtraSettingsAPI_SetComboboxSelectedIndex("bedSetting", (int)value);
                BedTime.Log($"Set sleep type to {value}");
            }
        }

        public void Start()
        {
            BedTime.instance = this;
            this.harmony = new Harmony("com.destruction.BedTime");
            this.harmony.PatchAll(Assembly.GetExecutingAssembly());
            if (RAPI.IsDedicatedServer())
            {
                BedTime.Log("Dedicated server detected. Activating additional patch.");
                var method = AccessTools.Method("RaftDedicatedServer.HarmonyPatches+HarmonyPatch_PlayerSleep:Prefix");
                var prefix = new HarmonyMethod(AccessTools.Method(typeof(BedTime_Patch_DedicatedServer), "Prefix"));
                this.harmony.Patch(method, prefix);
            }

            BedTime.Log("Mod has been loaded.");
        }

        public void OnModUnload()
        {
            this.harmony?.UnpatchAll("com.destruction.BedTime");
            BedTime.Log("Mod has been unloaded.");
        }

        public static void Log(object message)
        {
            Debug.Log($"[{instance.modlistEntry.jsonmodinfo.name}]: {message}");
        }

        // Extra Settings Api connection stuff.

        public virtual void ExtraSettingsAPI_Load()
        {
            this.bedSetting = (BedTimeSetting)ExtraSettingsAPI_GetComboboxSelectedIndex("bedSetting");
        }

        public virtual void ExtraSettingsAPI_SettingsClose()
        {
            this.bedSetting = (BedTimeSetting)ExtraSettingsAPI_GetComboboxSelectedIndex("bedSetting");
        }

        public virtual int ExtraSettingsAPI_GetComboboxSelectedIndex(string settingName) => 0;

        public virtual void ExtraSettingsAPI_SetComboboxSelectedIndex(string settingName, int value) {}

        [ConsoleCommand(name: "SetBedTimeFull", docs: "Sets the player requirement for the BedTime mode to all players.")]
        public static void SetBedTimeFull()
        {
            BedTime.BedTimeSetting = BedTimeSetting.FULL;
        }

        [ConsoleCommand(name: "SetBedTimeHalf", docs: "Sets the player requirement for the BedTime mode to half of the players.")]
        public static void SetBedTimeHalf()
        {
            BedTime.BedTimeSetting = BedTimeSetting.HALF;
        }

        [ConsoleCommand(name: "SetBedTimeSingle", docs: "Sets the player requirement for the BedTime mode to a single player.")]
        public static void SetBedTimeSingle()
        {
            BedTime.BedTimeSetting = BedTimeSetting.SINGLE;
        }
    }



    public enum BedTimeSetting
    {
        FULL = 0,
        HALF,
        SINGLE
    }



    [HarmonyPatch(typeof(BedManager), "AllPlayersSleeping")]
    public class BedTime_Patch_BedManager_AllPlayersSleeping
    {
        public static bool Prefix(ref bool __result)
        {
            int count = 1;
            Dictionary<CSteamID, Network_Player> dict = ComponentManager<Raft_Network>.Value.remoteUsers;
            switch (BedTime.BedTimeSetting)
            {
                case BedTimeSetting.FULL:
                    return true;
                case BedTimeSetting.HALF:
                    count = (dict.Count - (RAPI.IsDedicatedServer() ? 1 : 0)) / 2;
                    break;
            }

            __result = false;

            int numSleeping = 0;
            // Iterate over the dictionary to see if we have enough players.
            foreach (var (_, player) in dict)
            {
                if (player.BedComponent.Sleeping)
                {
                    if ((++numSleeping) >= count)
                    {
                        __result = true;
                        break;
                    }
                }
            }

            return false;
        }
    }



    [HarmonyPatch()]
    public class BedTime_Patch_BedManager_Slumber_Coro
    {
        static MethodBase TargetMethod() => typeof(BedManager).Assembly.GetTypes().First(x => x.Name.StartsWith("<Slumber>")).GetMethod("MoveNext", ~BindingFlags.Default);

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var i in instructions)
            {
                if (i.operand is MethodInfo method && method.Name == "StartCoroutine")
                {
                    i.opcode = OpCodes.Call;
                    i.operand = AccessTools.Method(typeof(BedTime_Patch_BedManager_Slumber_Coro), "StartCoroutine");
                }
            }
            return instructions;
        }

        private static Coroutine StartCoroutine(MonoBehaviour obj, IEnumerator coroutine) {
            if (ComponentManager<Raft_Network>.Value.GetLocalPlayer().BedComponent.Sleeping)
            {
                return obj.StartCoroutine(coroutine);
            }
            else
            {
                return obj.StartCoroutine(test());
            }
        }

        private static IEnumerator test() {
            yield break;
        }
    }

    // Patch to be compaitible with dedicated servers.
    public class BedTime_Patch_DedicatedServer
    {
        public static bool Prefix(ref bool __result)
        {
            if (BedTime.BedTimeSetting == BedTimeSetting.FULL)
            {
                return true;
            }
            else
            {
                __result = true;
                return false;
            }
        }
    }
}