using HarmonyLib;

namespace DestinyConfigMachines
{
    [HarmonyPatch(typeof(CookingSlot), "Awake")]
    public class Patch_CookingSlotAwake
    {
        public static void Postfix(ref CookingSlot __instance)
        {
            // On awake, add to the dictionary. This will update it.
            ConfigMachines.AddCookingSlot(__instance);
        }
    }

    // Electric purifier doesn't have an awake, unfortunately.
    [HarmonyPatch(typeof(Electric_Purifier), "IBlockPlaced.OnBlockPlacedInterface")]
    public class Patch_ElectricPurifierAwake
    {
        public static void Postfix(ref Electric_Purifier __instance)
        {
            // On awake, add to the dictionary. This will update it.
            ConfigMachines.AddElectricPurifier(__instance);
        }
    }

    [HarmonyPatch(typeof(MotorWheel), "Awake")]
    public class Patch_EngineAwake
    {
        public static void Postfix(ref MotorWheel __instance)
        {
            // On awake, add to the dictionary. This will update it.
            ConfigMachines.AddEngine(__instance);
        }
    }

    // This patch is how we handle not using fuel.
    [HarmonyPatch(typeof(Tank), "ModifyTank")]
    public class Patch_EngineConsumeFuel
    {
        public static bool Prefix(ref Tank __instance, ref float amount)
        {
            // Check to see if it is the right tank.
            if (__instance is MotorwheelFuelTank)
            {
                if (amount < 0f && ConfigMachines.ExtraSettingsAPI_Settings.GetCheckbox("engineDontUseFuel"))
                {
                    return false;
                }
            }
            return true;
        }
    }

    // For handling portions and time for the cooking pot and juicer, we can
    // actually just straight up patch the recipe class.
    [HarmonyPatch(typeof(SO_CookingTable_Recipe), "get_Portions")]
    public class Patch_RecipePortions
    {
        public static void Postfix(ref SO_CookingTable_Recipe __instance, ref uint __result)
        {
            float mult = 1.0f;
            // Get the right multiplier. Protect against new recipe types.
            switch(__instance.RecipeType)
            {
                case CookingRecipeType.CookingPot:
                    mult = ConfigMachines.ExtraSettingsAPI_Settings.GetFloatInput("cookPotPortionsMult");
                    break;
                case CookingRecipeType.Juicer:
                    mult = ConfigMachines.ExtraSettingsAPI_Settings.GetFloatInput("juicerPortionsMult");
                    break;
            }

            if (ConfigMachines.Debugging)
            {
                ConfigMachines.DebugLog(mult);
                ConfigMachines.DebugLog(__result * mult);
            }

            __result = (uint)(__result * mult);
            // Ensure we don't have 0 (or negative) portions.
            if (__result <= 0)
            {
                __result = 1;
            }
        }
    }

    [HarmonyPatch(typeof(SO_CookingTable_Recipe), "get_CookTime")]
    public class Patch_RecipeTime
    {
        public static void Postfix(ref SO_CookingTable_Recipe __instance, ref float __result)
        {
            float mult = 1.0f;
            // Get the right multiplier. Protect against new recipe types.
            switch(__instance.RecipeType)
            {
                case CookingRecipeType.CookingPot:
                    mult = ConfigMachines.ExtraSettingsAPI_Settings.GetFloatInput("cookPotTimeMult");
                    break;
                case CookingRecipeType.Juicer:
                    mult = ConfigMachines.ExtraSettingsAPI_Settings.GetFloatInput("juicerTimeMult");
                    break;
            }

            __result = __result * mult;
        }
    }
}
