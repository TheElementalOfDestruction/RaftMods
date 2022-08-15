using HarmonyLib;
using System;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;


public class ConfigMachines : Mod
{
    public static JsonModInfo modInfo;
    public static List<(SettingType, string)> modSettings = new List<(SettingType, string)>();
    public static Settings ExtraSettingsAPI_Settings;

    private Harmony harmony;
    private static bool debugging = false;
    private static Dictionary<CookingSlot, (string, float)> cookingSlotMults = new Dictionary<CookingSlot, (string, float)>();
    private static Dictionary<Electric_Purifier, PurifierData> elecPurifiers = new Dictionary<Electric_Purifier, PurifierData>();
    private static Dictionary<MotorWheel, EngineData> engines = new Dictionary<MotorWheel, EngineData>();

    public void Start()
    {
        ConfigMachines.modInfo = modlistEntry.jsonmodinfo;
        this.harmony = new Harmony("com.destruction.ConfigurableMachines");
        this.harmony.PatchAll(Assembly.GetExecutingAssembly());
        // Just in case things change and this might not be null during the
        // start method, only set it to something if it is currently null.
        if (ConfigMachines.ExtraSettingsAPI_Settings == null)
        {
            ConfigMachines.ExtraSettingsAPI_Settings = new Settings();
        }

        // Place a default instance of the Settings class in our variable.
        // Extra settings api should override this anyways.

        // Find all the slots and add them to our update list.
        foreach (var slot in FindObjectsOfType<CookingSlot>())
        {
            // Only add if they are awake.
            if (slot.gameObject.activeInHierarchy)
            {
                ConfigMachines.AddCookingSlot(slot);
            }
        }

        // Find all electric purifiers and add them.
        foreach (var pur in FindObjectsOfType<Electric_Purifier>())
        {
            // Only add if they are awake.
            if (pur.gameObject.activeInHierarchy)
            {
                ConfigMachines.AddElectricPurifier(pur);
            }
        }

        // Find all engines and add them.
        foreach (var eng in FindObjectsOfType<MotorWheel>())
        {
            if (eng.gameObject.activeInHierarchy)
            {
                ConfigMachines.AddEngine(eng);
            }
        }

        // Load the json data so we can iterate over settings.
        var jObject = new JSONObject(Encoding.Default.GetString(GetEmbeddedFileBytes("modinfo.json")));
        jObject = jObject.GetField("modSettings");
        foreach (var x in jObject.list)
        {
            var name = x.GetField("name").str;
            SettingType type = (SettingType)Enum.Parse(typeof(SettingType), x.GetField("type").str);
            ConfigMachines.modSettings.Add(ValueTuple.Create<SettingType, string>(type, name));
        }

        ConfigMachines.Log("Mod has been loaded.");
    }

    public void OnModUnload()
    {
        ConfigMachines.RestoreMultipliers();
        this.harmony.UnpatchAll("com.destruction.ConfigurableMachines");
        ConfigMachines.Log("Mod has been unloaded.");
    }

    public static void Log(object message)
    {
        Debug.Log("[" + modInfo.name + "]: " + message.ToString());
    }

    public static void DebugLog(object message)
    {
        Debug.Log("[" + modInfo.name + "][DEBUG]: " + message.ToString());
    }

    public static void AddCookingSlot(CookingSlot slot)
    {
        // Get the original multiplier.
        float originalMult = (float)Traverse.Create(slot).Field("cookTimeMultiplier").GetValue();

        string name = slot.gameObject.name;


        ValueTuple<string, float> valueTup = ("", originalMult);
        bool valueTupSet = false;

        // Now figure out where to put it.
        if (name.StartsWith("CookingSlot_Smelter_Electric"))
        {
            // Electric smelter slot.
            valueTup.Item1 = "electricSmelterMultiplier";
            valueTupSet = true;
        }
        else if (name.StartsWith("CookingSlot_Color"))
        {
            // Paint mill slot.
            valueTup.Item1 = "paintMillMultiplier";
            valueTupSet = true;
        }
        else if (name.StartsWith("CookingSlot_Food_BigGrill"))
        {
            if (slot.GetComponentInParent<CookingStand_Electric>())
            {
                // Electric grill slot.
                valueTup.Item1 = "electricGrillMultiplier";
                valueTupSet = true;
            }
            else
            {
                // Advanced grill slot.
                valueTup.Item1 = "advancedGrillMultiplier";
                valueTupSet = true;
            }
        }
        else if (name.StartsWith("CookingSlot_Food"))
        {
            // Basic grill slot.
            valueTup.Item1 = "basicGrillMultiplier";
            valueTupSet = true;
        }
        else if (name.StartsWith("CookingSlot_Smelter"))
        {
            // Basic smelter.
            valueTup.Item1 = "basicSmelterMultiplier";
            valueTupSet = true;
        }
        else if (slot.gameObject.GetComponentInParent<Block_CookingStand_Purifier>())
        {
            name = slot.gameObject.GetComponentInParent<Block_CookingStand_Purifier>().gameObject.name;
            if (name.StartsWith("Placeable_CookingStand_Purifier_One"))
            {
                // Basic purifier.
                valueTup.Item1 = "basicPurifierMultiplier";
                valueTupSet = true;
            }
            else if (name.StartsWith("Placeable_CookingStand_Purifier_Two"))
            {
                // Advanced purifier.
                valueTup.Item1 = "advancedPurifierMultiplier";
                valueTupSet = true;
            }
        }

        if (valueTupSet)
        {
            cookingSlotMults.TryAdd(slot, valueTup);
            ConfigMachines.UpdateCookingSlot(slot);
        }
        else if (ConfigMachines.debugging)
        {
            ConfigMachines.DebugLog("Found cooking slot with name \"" + name + "\" but could not identify it.");
        }
    }

    public static void AddElectricPurifier(Electric_Purifier machine)
    {
        if (ConfigMachines.debugging)
        {
            ConfigMachines.DebugLog($"Adding Electric Purifier \"{machine}\"");
        }
        if (ConfigMachines.elecPurifiers.TryAdd(machine, ConfigMachines.CreatePurifierData(machine)))
        {
            ConfigMachines.UpdatePurifier(machine);
        }
    }

    public static void AddEngine(MotorWheel machine)
    {
        if (ConfigMachines.debugging)
        {
            ConfigMachines.DebugLog($"Adding Engine \"{machine}\"");
        }
        if (ConfigMachines.engines.TryAdd(machine, ConfigMachines.CreateEngineData(machine)))
        {
            ConfigMachines.UpdateEngine(machine);
        }
    }

    public static EngineData CreateEngineData(MotorWheel machine)
    {
        EngineData data = new EngineData();
        var traverse = Traverse.Create(machine);
        data.timePerFuel = (float)traverse.Field("timePerFuel").GetValue();
        return data;
    }

    public static PurifierData CreatePurifierData(Electric_Purifier machine)
    {
        PurifierData data = new PurifierData();
        var traverse = Traverse.Create(machine);
        data.tankFillPerPump = (int)traverse.Field("tankFillPerPump").GetValue();
        data.batteryDrainPerPump = (int)traverse.Field("batteryDrainPerPump").GetValue();
        return data;
    }

    public static void RestoreMultipliers()
    {
        // Restore all multipliers to their original values.
        cookingSlotMults.Clean();
        foreach (var slot in ConfigMachines.cookingSlotMults.Keys)
        {
            Traverse.Create(slot).Field("cookTimeMultiplier").SetValue(cookingSlotMults[slot].Item2);
        }

        foreach (var slot in ConfigMachines.elecPurifiers.Keys)
        {
            var traverse = Traverse.Create(slot);
            var data = ConfigMachines.elecPurifiers[slot];

            traverse.Field("tankFillPerPump").SetValue(data.tankFillPerPump);
            traverse.Field("batteryDrainPerPump").SetValue(data.batteryDrainPerPump);
        }

        foreach (var slot in ConfigMachines.engines.Keys)
        {
            var traverse = Traverse.Create(slot);
            var data = ConfigMachines.engines[slot];

            traverse.Field("timePerFuel").SetValue(data.timePerFuel);
        }
    }

    public static void UpdateAll()
    {
        if (debugging)
        {
            ConfigMachines.DebugLog("Updating all registered machines.");
        }

        ConfigMachines.cookingSlotMults.Clean();
        foreach (CookingSlot k in ConfigMachines.cookingSlotMults.Keys)
        {
            ConfigMachines.UpdateCookingSlot(k);
        }

        ConfigMachines.elecPurifiers.Clean();
        foreach (Electric_Purifier k in ConfigMachines.elecPurifiers.Keys)
        {
            ConfigMachines.UpdatePurifier(k);
        }

        ConfigMachines.engines.Clean();
        foreach (MotorWheel k in ConfigMachines.engines.Keys)
        {
            ConfigMachines.UpdateEngine(k);
        }
    }

    private static void UpdateCookingSlot(CookingSlot slot)
    {
        try
        {
            var field = Traverse.Create(slot).Field("cookTimeMultiplier");
            // Get the tuple with the setting name and
            var valueTup = ConfigMachines.cookingSlotMults[slot];
            var mult = ConfigMachines.ExtraSettingsAPI_Settings.GetSlider(valueTup.Item1.ToString());
            if (debugging)
            {
                ConfigMachines.DebugLog($"Fetched value of {mult} for setting \"{valueTup.Item1}\"");
            }

            field.SetValue(mult * valueTup.Item2);

            if (debugging)
            {
                ConfigMachines.DebugLog($"Updated CookingSlot \"{slot}\" to new multiplier of {mult * valueTup.Item2}.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.ToString());
            throw e;
        }
    }

    public static void UpdateEngine(MotorWheel machine)
    {
        var traverse = Traverse.Create(machine);
        var data = ConfigMachines.engines[machine];
        var mult = ConfigMachines.ExtraSettingsAPI_Settings.GetSlider("engineTimePerFuelMult");
        if (debugging)
        {
            ConfigMachines.DebugLog($"Fetched value of {mult} for setting \"engineTimePerFuelMult\"");
        }

        traverse.Field("timePerFuel").SetValue(data.timePerFuel * mult);

        if (debugging)
        {
            ConfigMachines.DebugLog($"Updated Engine \"{machine}\" property \"timePerFuel\" to new value of ${data.timePerFuel * mult}.");
        }
    }

    public static void UpdatePurifier(Electric_Purifier machine)
    {
        var traverse = Traverse.Create(machine);
        var data = ConfigMachines.elecPurifiers[machine];

        var mult1 = ConfigMachines.ExtraSettingsAPI_Settings.GetSlider("elecPurifTankFill");
        var mult2 = ConfigMachines.ExtraSettingsAPI_Settings.GetSlider("elecPurifBatteryDrain");

        if (debugging)
        {
            ConfigMachines.DebugLog($"Fetched value of {mult1} for setting \"elecPurifTankFill\"");
            ConfigMachines.DebugLog($"Fetched value of {mult2} for setting \"elecPurifBatteryDrain\"");
        }

        traverse.Field("tankFillPerPump").SetValue(Math.Max(1, (int)(data.tankFillPerPump * mult1)));
        traverse.Field("batteryDrainPerPump").SetValue((int)(data.batteryDrainPerPump * mult2));

        if (debugging)
        {
            ConfigMachines.DebugLog($"Updated Electric Purifier \"{machine}\" property \"elecPurifTankFill\" to new value of ${Math.Max(1, (int)(data.tankFillPerPump * mult1))}.");
            ConfigMachines.DebugLog($"Updated Electric Purifier \"{machine}\" property \"elecPurifBatteryDrain\" to new value of ${(int)(data.batteryDrainPerPump * mult2)}.");
        }
    }

    // Debug console commands.
    [ConsoleCommand(name: "ConfigMachines_ToggleDebug", docs: "Toggles debug logging.")]
    public static void ToggleDebug()
    {
        ConfigMachines.debugging = !ConfigMachines.debugging;
        var enabled = ConfigMachines.debugging ? "enabled" : "disabled";
        ConfigMachines.DebugLog($"Debug messages have been {enabled}.");
    }

    [ConsoleCommand(name: "ConfigMachines_DumpSettings", docs: "Dumps a list of setting for debug testing.")]
    public static void DumpSettings()
    {
        string debugMessage = "\nExtra Settings Loaded: " + (ConfigMachines.ExtraSettingsAPI_Settings.ExtraSettingsAPI_Loaded ? "True" : "False") + "\n";
        debugMessage += "Aquired Settings Values:\n";

        foreach (var valueTup in ConfigMachines.modSettings)
        {
            string line = "";
            switch (valueTup.Item1)
            {
                case SettingType.slider:
                    line = $"    {valueTup.Item2}: {ConfigMachines.ExtraSettingsAPI_Settings.GetSlider(valueTup.Item2)}\n";
                    break;
                case SettingType.checkbox:
                    line = $"    {valueTup.Item2}: {ConfigMachines.ExtraSettingsAPI_Settings.GetCheckbox(valueTup.Item2)}\n";
                    break;
            }
            debugMessage += line;
        }

        ConfigMachines.DebugLog(debugMessage);
    }

    [ConsoleCommand(name: "ConfigMachines_DumpAll", docs: "Creates an extensive dump of debugging information.")]
    public static void DumpAll()
    {
        // Start with the settings.
        string debugMessage = "\nExtra Settings Loaded: " + (ConfigMachines.ExtraSettingsAPI_Settings.ExtraSettingsAPI_Loaded ? "True" : "False") + "\n";
        debugMessage += "Aquired Settings Values:\n";

        foreach (var valueTup in ConfigMachines.modSettings)
        {
            string line = "";
            switch (valueTup.Item1)
            {
                case SettingType.slider:
                    line = $"    {valueTup.Item2}: {ConfigMachines.ExtraSettingsAPI_Settings.GetSlider(valueTup.Item2)}\n";
                    break;
                case SettingType.checkbox:
                    line = $"    {valueTup.Item2}: {ConfigMachines.ExtraSettingsAPI_Settings.GetCheckbox(valueTup.Item2)}\n";
                    break;
            }
            debugMessage += line;
        }

        // Now we want to look at the saved cooking slots.
        debugMessage += "\nSaved Cooking Slot States:\n";
        foreach (CookingSlot slot in ConfigMachines.cookingSlotMults.Keys)
        {
            debugMessage += $"    Slot {slot}:\n";
            if (slot)
            {
                // We only want to get a bunch of data if the object is good.
                // Get the saved data into short variables.
                ValueTuple<string, float> valueTup = ConfigMachines.cookingSlotMults[slot];
                var currentMult = Traverse.Create(slot).Field("cookTimeMultiplier").GetValue();
                debugMessage += "        Is Valid: True\n";
                debugMessage += $"        Original Mutiplier: {valueTup.Item2}\n";
                debugMessage += $"        Current Multiplier: {currentMult}\n";
                debugMessage += $"        Setting Name: {valueTup.Item1}\n";
            }
            else
            {
                debugMessage += "        Is Valid: False\n";
            }
        }

        debugMessage += "\nSaved Electric Purifier States:\n";
        foreach (Electric_Purifier slot in ConfigMachines.elecPurifiers.Keys)
        {
            debugMessage += $"    Slot {slot}:\n";
            if (slot)
            {
                // We only want to get a bunch of data if the object is good.
                // Get the saved data into short variables.
                PurifierData data = ConfigMachines.elecPurifiers[slot];
                var trav = Traverse.Create(slot);
                debugMessage += "        Is Valid: True\n";
                var currentMult = trav.Field("tankFillPerPump").GetValue();
                debugMessage += $"        Original Tank Fill Per Pump: {data.tankFillPerPump}\n";
                debugMessage += $"        Current Value: {currentMult}\n";
                debugMessage += $"        Setting Name: elecPurifTankFill\n";
                currentMult = trav.Field("batteryDrainPerPump").GetValue();
                debugMessage += $"        Original Battery Drain Per Pump: {data.batteryDrainPerPump}\n";
                debugMessage += $"        Current Value: {currentMult}\n";
                debugMessage += $"        Setting Name: elecPurifBatteryDrain\n";
            }
            else
            {
                debugMessage += "        Is Valid: False\n";
            }
        }

        debugMessage += "\nSaved Engine States:\n";
        foreach (MotorWheel slot in ConfigMachines.engines.Keys)
        {
            debugMessage += $"    Slot {slot}:\n";
            if (slot)
            {
                // We only want to get a bunch of data if the object is good.
                // Get the saved data into short variables.
                EngineData data = ConfigMachines.engines[slot];
                var trav = Traverse.Create(slot);
                debugMessage += "        Is Valid: True\n";
                var currentMult = trav.Field("timePerFuel").GetValue();
                debugMessage += $"        Original Time Per Fuel: {data.timePerFuel}\n";
                debugMessage += $"        Current Value: {currentMult}\n";
                debugMessage += $"        Setting Name: engineTimePerFuelMult\n";
            }
            else
            {
                debugMessage += "        Is Valid: False\n";
            }
        }

        ConfigMachines.DebugLog(debugMessage);
    }

    [ConsoleCommand(name: "ConfigMachines_ForceUpdate", docs: "Attempts to forcibly update all values.")]
    public static void ForceUpdate()
    {
        ConfigMachines.UpdateAll();
    }
}



// Class for storing electric purifier data.
public class PurifierData
{
    public int tankFillPerPump;
    public int batteryDrainPerPump;
}



public class EngineData
{
    public float timePerFuel;
}



public class Settings
{
    private Dictionary<string, bool> checkboxCache;
    private Dictionary<string, string> inputCache;
    private Dictionary<string, float> sliderCache;

    public Settings()
    {
        this.checkboxCache = new Dictionary<string, bool>();
        this.inputCache = new Dictionary<string, string>();
        this.sliderCache = new Dictionary<string, float>();
    }

    public float GetSlider(string name)
    {
        return this.sliderCache.Get(name, 1f);
    }

    public bool GetCheckbox(string name)
    {
        return this.checkboxCache.Get(name, false);
    }

    public string GetInput(string name)
    {
        return this.inputCache.Get(name, "");
    }

    // Convenience function. This will take a text input field and parse it as a
    // float.
    public float GetFloatInput(string name)
    {
        // This commented section is the new code that will be switched to. It
        // actually needs to be fixed to handle locale though.
        //return float.Parse(this.GetInput(name));
        return this.GetSlider(name);
    }

    public void ExtraSettingsAPI_Load() // Occurs when the API loads the mod's settings
    {
        ConfigMachines.ExtraSettingsAPI_Settings = this;
        this.ReloadSettingsCache();
        ConfigMachines.UpdateAll();
    }

    public void ExtraSettingsAPI_SettingsClose() // Occurs when the API loads the mod's settings
    {
        this.ReloadSettingsCache();
        ConfigMachines.UpdateAll();
    }

    // Updates the cache and returns if any values changed.
    public bool ReloadSettingsCache()
    {
        bool settingChanged = false;
        foreach ((SettingType, string) setting in ConfigMachines.modSettings)
        {
            float fValue;
            bool bValue;
            switch (setting.Item1)
            {
                case SettingType.slider:
                    fValue = ExtraSettingsAPI_GetSliderValue(setting.Item2);
                    settingChanged = settingChanged || (!this.sliderCache.ContainsKey(setting.Item2)) || fValue == this.sliderCache[setting.Item2];
                    this.sliderCache[setting.Item2] = fValue;
                    break;
                case SettingType.checkbox:
                    bValue = ExtraSettingsAPI_GetCheckboxState(setting.Item2);
                    settingChanged = settingChanged || (!this.checkboxCache.ContainsKey(setting.Item2)) || bValue == this.checkboxCache[setting.Item2];
                    this.checkboxCache[setting.Item2] = bValue;
                    break;
            }
        }

        return settingChanged;
    }

    public float ExtraSettingsAPI_GetSliderValue(string SettingName) => 1f;
    public bool ExtraSettingsAPI_GetCheckboxState(string SettingName) => false;
    public bool ExtraSettingsAPI_Loaded = false;
}



public enum SettingType
{
    checkbox,
    slider,
    combobox,
    keybind,
    button,
    text,
    data,
    input
}



static class DictionaryExtension
{
    // Thanks Aidan.
    public static void Clean<X,Y>(this Dictionary<X,Y> c) where X : MonoBehaviour
    {
        var l = new List<X>();
        foreach (var k in c.Keys)
        {
            if (!k)
            {
                l.Add(k);
            }
        }
        foreach (var i in l)
        {
            c.Remove(i);
        }
    }

    // Tries to get a value based on the key, returning the default if not
    // found.
    public static Y Get<X,Y>(this Dictionary<X,Y> d, X key, Y _default)
    {
        return d.ContainsKey(key) ? d[key] : _default;
    }
}
