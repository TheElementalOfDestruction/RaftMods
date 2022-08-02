using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class CustomMessages : Mod
{
    private static CustomMessages instance;
    public static string STRINGS_FILE_NAME = "custom_messages.txt";
    public static string MOD_DATA_FOLDER = Path.Combine(HMLLibrary.HLib.path_modsFolder, "ModData");
    public static JsonModInfo modInfo;
    private string[] originalList;
    private string[] userList;
    private static string[] defaultList = { $"{STRINGS_FILE_NAME} has no valid messages." };

    public void Start()
    {
        CustomMessages.instance = this;
        CustomMessages.modInfo = modlistEntry.jsonmodinfo;
        this.originalList = RaftModLoader.HomePage.randomTexts;

        this.ReloadList();

        CustomMessages.Log("Mod has been loaded.");
    }

    public void ReloadList()
    {
        try
        {
            string filePath = Path.Combine(MOD_DATA_FOLDER, STRINGS_FILE_NAME);

            // Make sure the ModData folder exists.
            if (!Directory.Exists(MOD_DATA_FOLDER))
            {
                CustomMessages.Log("ModData folder not found. Creating it manually.");
                Directory.CreateDirectory(MOD_DATA_FOLDER);
            }

            // Try to open our file. If this fails, create the file.
            if (!File.Exists(filePath))
            {
                CustomMessages.Log($"\"{STRINGS_FILE_NAME}\"not found. Creating it manually.");
                byte[] data = GetEmbeddedFileBytes("default_strings.txt");
                using (FileStream fs = File.Create(filePath))
                {
                    fs.Write(data, 0, data.Length);
                }
            }

            string[] list = File.ReadAllLines(filePath);

            List<string> valid = new List<string>();

            for (int i = 0; i < list.Length; ++i)
            {
                string entry = list[i].Trim();
                if (entry.Length > 77)
                {
                    CustomMessages.Log($"Entry on line {i + 1} is greater than 77 characters and will be skipped.");
                }
                else
                {
                    valid.Add(entry);
                }
            }

            if (valid.Count == 0)
            {
                CustomMessages.LogError("Could not read any valid entries from the custom messages file.");
                this.userList = CustomMessages.defaultList;
            }
            else
            {
                this.userList = valid.ToArray();
            }

            RaftModLoader.HomePage.randomTexts = this.userList;
            RaftModLoader.HomePage.RandomText.text = this.userList[UnityEngine.Random.Range(0, this.userList.Length)];
        }
        catch (Exception e)
        {
            Debug.LogError(e.ToString());
            throw e;
        }
    }

    public void OnModUnload()
    {
        // Undo the changes.
        RaftModLoader.HomePage.randomTexts = this.originalList;
        RaftModLoader.HomePage.RandomText.text = this.originalList[UnityEngine.Random.Range(0, this.originalList.Length)];
        CustomMessages.Log("Mod has been unloaded.");
    }

    public static void Log(object message)
    {
        Debug.Log($"[{modInfo.name}]: {message}");
    }

    public static void LogError(object message)
    {
        Debug.LogError($"[{modInfo.name}]: {message}");
    }

    // Command for reloading.
    [ConsoleCommand(name: "reloadCustomHomepageMessages", docs: "Reloads the list of home page messages from the custom messages file.")]
    public static void ReloadCommand()
    {
        CustomMessages.instance.ReloadList();
        CustomMessages.Log("List reloaded.");
    }

    // Extra Settings Api connection stuff.

    public void ExtraSettingsAPI_ButtonPress(string name)
    {
        if (name == "reloadList")
        {
            CustomMessages.Log("Reloading message list...");
            this.ReloadList();
            CustomMessages.Log("Message list reloaded.");
        }
    }
}
