using System.Collections.Generic;


namespace DestinyConfigMachines
{
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



    public class CMSettings
    {
        private Dictionary<string, bool> checkboxCache;
        private Dictionary<string, string> inputCache;
        private Dictionary<string, float> sliderCache;
        public bool ExtraSettingsAPI_Loaded = false;

        public CMSettings()
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
}
