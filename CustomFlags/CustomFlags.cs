using HarmonyLib;
using I2.Loc;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace DestinyCustomFlags
{
    public class CustomFlags : Mod
    {
        private const int FLAG_ID = 478;

        // In little endian this would represent the string "De";
        public static int CUSTOM_FLAG_ITEM_ID = 25924;
        public static CustomFlags instance;
        public static JsonModInfo modInfo;

        private static ValueTuple<int, int> FLAG_LOCATION = (253, 768);
        private static Block_CustomFlag currentFlag;
        private static GameObject menu;
        private static GameObject menuAsset;
        private static CustomFlagsMenu cfMenu;

        public Texture2D baseFlagTexture;
        public Texture2D baseFlagNormal;
        public Texture2D baseFlagPaint;
        public Material defaultFlag;
        public Sprite defaultFlagSprite;

        private Shader shader;
        private Item_Base customFlagItem;
        private Harmony harmony;
        private Transform prefabHolder;
        private AssetBundle bundle;

        public static bool IgnoreFlagMessages
        {
            get
            {
                return instance.ExtraSettingsAPI_GetCheckboxState("ignoreFlagMessages");
            }
        }

        // Just leaving the command here that was used in testing.
        // FindObjectOfType<Streamer>().GetComponent<Block>().occupyingComponent.renderers[1]; y.material = j;

        public IEnumerator Start()
        {
            CustomFlags.instance = this;

            HNotification notification = HNotify.instance.AddNotification(HNotify.NotificationType.spinning, "Loading CustomFlags...");

            CustomFlags.modInfo = modlistEntry.jsonmodinfo;
            this.harmony = new Harmony("com.destruction.CustomFlags");
            this.harmony.PatchAll(Assembly.GetExecutingAssembly());
            this.prefabHolder = new GameObject("prefabHolder").transform;
            this.prefabHolder.gameObject.SetActive(false);
            DontDestroyOnLoad(this.prefabHolder.gameObject);

            // First thing is first, let's fetch our shader from the game.
            this.shader = Shader.Find(" BlockPaint");

            // Now since we need this for every flag, get the base texture for
            // flags.
            var originalMat = ItemManager.GetItemByIndex(FLAG_ID).settings_buildable.GetBlockPrefab(DPS.Floor).GetComponent<OccupyingComponent>().renderers[1].material;
            this.baseFlagTexture = (originalMat.GetTexture("_Diffuse") as Texture2D).CreateReadable();
            Texture2D flagTexture = new Texture2D(381, 256);
            ImageConversion.LoadImage(flagTexture, GetEmbeddedFileBytes("transparent_flag.png"));
            CustomFlags.PlaceFlagInTexture(this.baseFlagTexture, flagTexture);

            // Now we need to generate our paint map.
            this.baseFlagPaint = (originalMat.GetTexture("_MetallicRPaintMaskGSmoothnessA") as Texture2D).CreateReadable();
            // We already have the transparent flag loaded, so just use it.
            CustomFlags.PlaceFlagInTexture(this.baseFlagPaint, flagTexture);

            // Now we need to generate our normal map.
            this.baseFlagNormal = (originalMat.GetTexture("_Normal") as Texture2D).CreateReadable();
            ImageConversion.LoadImage(flagTexture, GetEmbeddedFileBytes("normal.png"));
            CustomFlags.PlaceFlagInTexture(this.baseFlagNormal, flagTexture);

            // Create our default flag material.
            this.defaultFlag = this.PrepareMaterial();
            flagTexture = new Texture2D(381, 256);
            ImageConversion.LoadImage(flagTexture, GetEmbeddedFileBytes("default.png"));
            CustomFlags.PlaceFlagInMaterial(this.defaultFlag, flagTexture);

            // Create the custom flag item base.
            this.customFlagItem = this.CreateCustomFlagItem();

            RAPI.RegisterItem(this.customFlagItem);

            // Create the default flag sprite for the menu.
            this.defaultFlagSprite = CustomFlags.CreateFlagSpriteFromBytes(GetEmbeddedFileBytes("default.png"));

            // Now, load the menu from the asset bundle.
            var bundleLoadRequest = AssetBundle.LoadFromMemoryAsync(GetEmbeddedFileBytes("customflags.assets"));
            yield return bundleLoadRequest;
            this.bundle = bundleLoadRequest.assetBundle;

            var request = this.bundle.LoadAssetAsync<GameObject>("CustomFlagsMenu");
            yield return request;

            CustomFlags.menuAsset = request.asset as GameObject;
            CustomFlags.menu = Instantiate(CustomFlags.menuAsset, this.transform);
            CustomFlags.cfMenu = CustomFlags.menu.AddComponent<CustomFlagsMenu>();

            CustomFlags.Log("Mod has been loaded.");
            notification.Close();
        }

        public void OnModUnload()
        {
            this.harmony.UnpatchAll("com.destruction.CustomFlags");
            this.bundle.Unload(true);
            Destroy(this.prefabHolder?.gameObject);
            if (CustomFlags.menu)
            {
                Destroy(CustomFlags.menu);
            }
            if (CustomFlags.menuAsset)
            {
                Destroy(CustomFlags.menuAsset);
            }
            CustomFlags.Log("Mod has been unloaded.");
        }

        public static void Log(object message)
        {
            Debug.Log($"[{modInfo.name}]: {message}");
        }

        public static void DebugLog(object message)
        {
            Debug.Log($"[{modInfo.name}][DEBUG]: {message}");
        }

        public static void OpenCustomFlagsMenu(Block_CustomFlag cf)
        {
            CustomFlags.cfMenu.ShowMenu(cf);
        }

        /*
         * Returns a new material with the flag data inside of it. Returns null
         * if the data is bad.
         */
        public static Material CreateMaterialFromImageData(byte[] flagData)
        {
            // Copy instance to local variable.
            CustomFlags inst = CustomFlags.instance;

            // Load the data into a texture.
            Texture2D flagTex = new Texture2D(381, 256);
            if (!ImageConversion.LoadImage(flagTex, flagData))
            {
                return null;
            }

            // Create the material and put the flag inside of it.
            Material mat = inst.PrepareMaterial();
            CustomFlags.PlaceFlagInMaterial(mat, flagTex);

            // Return it.
            return mat;
        }

        /*
         * Uses the data to create a flag sprite. Returns null if flag is bad.
         * Returns the default flag texture if the data is 0 bytes.
         */
        public static Sprite CreateFlagSpriteFromBytes(byte[] data)
        {
            if (data == null)
            {
                return null;
            }
            if (data.Length == 0)
            {
                return CustomFlags.instance.defaultFlagSprite;
            }

            Vector2 pivot = new Vector2(0.5f, 0.5f);
            Rect rect = new Rect(0, 0, 381, 256);

            Texture2D flag = new Texture2D(381, 256);
            Texture2D flagData = new Texture2D(1, 1);
            if (!ImageConversion.LoadImage(flagData, data))
            {
                return null;
            }
            flag.Edit(flagData, 0, 0, 381, 256);

            return Sprite.Create(flag, rect, pivot);
        }

        /*
         * Prepares a new material for flags to be placed into.
         */
        private Material PrepareMaterial()
        {
            // Load the textures.
            Texture2D diffuse = new Texture2D(1024, 1024);
            Texture2D paint = new Texture2D(1024, 1024);
            Texture2D normal = new Texture2D(1024, 1024);

            Graphics.CopyTexture(this.baseFlagTexture, diffuse);
            Graphics.CopyTexture(this.baseFlagNormal, normal);
            Graphics.CopyTexture(this.baseFlagPaint, paint);

            // Create and setup our material.
            Material mat = new Material(this.shader);
            mat.SetTexture("_Diffuse", diffuse);
            mat.SetTexture("_MetallicRPaintMaskGSmoothnessA", paint);
            mat.SetTexture("_Normal", normal);

            return mat;
        }

        /*
         * Finds the base flag we will be using and creates a new item for our
         * custom flag.
         */
        private Item_Base CreateCustomFlagItem()
        {
            // Create a clone of the regular flag.
            Item_Base originalItem = ItemManager.GetItemByIndex(FLAG_ID);
            Item_Base customFlag = ScriptableObject.CreateInstance<Item_Base>();
            customFlag.Initialize(CUSTOM_FLAG_ITEM_ID, "destiny_CustomFlag", 1);
            customFlag.settings_buildable = originalItem.settings_buildable.Clone();
            customFlag.settings_consumeable = originalItem.settings_consumeable.Clone();
            customFlag.settings_cookable = originalItem.settings_cookable.Clone();
            customFlag.settings_equipment = originalItem.settings_equipment.Clone();
            customFlag.settings_Inventory = originalItem.settings_Inventory.Clone();
            customFlag.settings_recipe = originalItem.settings_recipe.Clone();
            customFlag.settings_usable = originalItem.settings_usable.Clone();

            Block[] blocks = customFlag.settings_buildable.GetBlockPrefabs().Clone() as Block[];

            // Set the block to not be paintable.
            Traverse.Create(customFlag.settings_buildable).Field("primaryPaintAxis").SetValue(Axis.None);

            // Setup the recipe.
            customFlag.SetRecipe(new[]
                {
                    new CostMultiple(new[] { ItemManager.GetItemByIndex(21) }, 4),
                    new CostMultiple(new[] { ItemManager.GetItemByIndex(23) }, 2),
                    new CostMultiple(new[] { ItemManager.GetItemByIndex(25) }, 6)
                }, CraftingCategory.Decorations, 1, false, "CustomFlag", 1);
            Traverse.Create(customFlag.settings_recipe).Field("_hiddenInResearchTable").SetValue(false);

            // Localization stuff.
            customFlag.settings_Inventory.LocalizationTerm = "Item/destiny_CustomFlag";
            var language = new LanguageSourceData()
            {
                mDictionary = new Dictionary<string, TermData>
                {
                    ["Item/destiny_CustomFlag"] = new TermData() { Languages = new[] { "Custom Flag@A customizable flag." } },
                    ["CraftingSub/CustomFlag"] = new TermData() { Languages = new[] { "Custom Flag" } }
                },
                mLanguages = new List<LanguageData> { new LanguageData() { Code = "en", Name = "English" } }
            };
            LocalizationManager.Sources.Add(language);
            Traverse.Create(typeof(LocalizationManager)).Field("OnLocalizeEvent").GetValue<LocalizationManager.OnLocalizeCallback>().Invoke();


            // Now, we need to replace Block with Block_CustomFlag;
            for (int i = 0; i < blocks.Length; ++i)
            {
                if (blocks[i])
                {
                    // Based on some of Aidan's code.
                    var flagPrefab = Instantiate(blocks[i], this.prefabHolder, false);
                    flagPrefab.name = $"Block_CustomFlag_{i}";
                    var cf = flagPrefab.gameObject.AddComponent<Block_CustomFlag>();
                    cf.CopyFieldsOf(flagPrefab);
                    cf.ReplaceValues(flagPrefab, cf);
                    flagPrefab.ReplaceValues(originalItem, customFlag);
                    blocks[i] = cf;
                    DestroyImmediate(flagPrefab);
                    cf.gameObject.AddComponent<RaycastInteractable>();
                    var c = cf.gameObject.AddComponent<BoxCollider>();
                    switch (cf.dpsType)
                    {
                        case DPS.Floor:
                            c.size = new Vector3(0.292296f, 3.260565f, 0.292296f);
                            c.center = new Vector3(0, 1.372016f, 0);
                            break;
                        case DPS.Ceiling:
                            c.size = new Vector3(1.204021f, 1.826855f, 0.2007985f);
                            c.center = new Vector3(0, -1.012313f, 0);
                            break;
                        case DPS.Wall:
                            c.size = new Vector3(0.214351f, 1.739691f, 1.0067628f);
                            c.center = new Vector3(0, 0, 0.5f);
                            break;
                        default:
                            c.size = Vector3.zero;
                            c.center = Vector3.zero;
                            break;
                    }

                    c.isTrigger = true;
                    c.enabled = true;
                    c.gameObject.layer = 10;
                    cf.networkedBehaviour = cf.gameObject.AddComponent<CF_Network>();
                    cf.networkType = NetworkType.NetworkBehaviour;

                    cf.ImageData = new byte[0];
                }
            }

            Traverse.Create(customFlag.settings_buildable).Field("blockPrefabs").SetValue(blocks);

            foreach (var q in Resources.FindObjectsOfTypeAll<SO_BlockQuadType>())
                if (q.AcceptsBlock(originalItem))
                    Traverse.Create(q).Field("acceptableBlockTypes").GetValue<List<Item_Base>>().Add(customFlag);

            return customFlag;
        }

        /*
         * Overwrites the data in the specified area with the specified flag
         * texture. Returns true if success, false if the texture was the wrong
         * size. This simplifies the calls to work with a material instead of a
         * Texture2D for the destination.
         *
         * :param dest: The texture for the flags to go into.
         * :param flag: The flag texture to add.
         */
        private static void PlaceFlagInMaterial(Material dest, Texture2D flag)
        {
            CustomFlags.PlaceFlagInTexture(dest.GetTexture("_Diffuse") as Texture2D, flag);
        }

        /*
         * Overwrites the data in the specified area with the specified flag
         * texture. Returns true if success, false if the texture was the wrong
         * size.
         *
         * :param dest: The texture for the flags to go into.
         * :param flag: The flag texture to add.
         */
        private static void PlaceFlagInTexture(Texture2D dest, Texture2D flag)
        {
            dest.Edit(flag, FLAG_LOCATION.Item1, FLAG_LOCATION.Item2, 381, 256);
        }

        public static T CreateObject<T>() => (T)FormatterServices.GetUninitializedObject(typeof(T));
        public bool ExtraSettingsAPI_GetCheckboxState(string name) => true;

        [ConsoleCommand(name: "CustomFlagsDebug_ReplaceMaterial", docs: "Test to replace the material of all custom flags.")]
        public static string ReplaceMaterials(string[] args)
        {
            string arg = string.Join(" ", args);

            Texture2D tex = new Texture2D(1024, 1024);
            ImageConversion.LoadImage(tex, System.IO.File.ReadAllBytes(args[0]));
            Material mat = CustomFlags.instance.PrepareMaterial();
            mat.SetTexture("_Diffuse", tex);

            Array.ForEach(FindObjectsOfType<Block_CustomFlag>(), x => {
                if (x.occupyingComponent)
                {
                    if (x.occupyingComponent.renderers.Length == 1)
                    {
                        x.occupyingComponent.renderers[0].material = mat;
                    }
                    else
                    {
                        x.occupyingComponent.renderers[1].material = mat;
                    }
                }
            });

            return "Replaced.";
        }

        public static void LogStack()
        {
            var trace = new System.Diagnostics.StackTrace();
            foreach (var frame in trace.GetFrames())
            {
                var method = frame.GetMethod();
                if (method.Name.Equals("LogStack")) continue;
                Debug.Log(string.Format("{0}::{1}",
                    method.ReflectedType != null ? method.ReflectedType.Name : string.Empty,
                    method.Name));
          }
        }
    }



    public class CustomFlagsMenu : MonoBehaviour
    {
        private CanvasGroup cg;
        private UnityEngine.UI.Image flagPreview;
        private Block_CustomFlag currentFlag;
        private TMPro.TMP_InputField inputField;
        private byte[] imageData;
        private bool shown;

        void Start()
        {
            // Setup the button events.
            foreach (var button in this.GetComponentsInChildren<UnityEngine.UI.Button>())
            {
                if (button.gameObject.name.StartsWith("CloseButton"))
                {
                    button.onClick.AddListener(this.HideMenu);
                }
                else if (button.gameObject.name.StartsWith("DefaultFlagButton"))
                {
                    button.onClick.AddListener(this.SetFlagDefault);
                }
                else if (button.gameObject.name.StartsWith("LoadButton"))
                {
                    button.onClick.AddListener(this.LoadFlagPreviewStart);
                }
                else if (button.gameObject.name.StartsWith("UpdateButton"))
                {
                    button.onClick.AddListener(this.UpdateFlag);
                }
            }

            // Get the components.
            this.cg = this.GetComponent<CanvasGroup>();
            this.flagPreview = this.GetComponentsInChildren<UnityEngine.UI.Image>().First(x => x.gameObject.name.StartsWith("FlagPreview"));
            this.inputField = this.GetComponentInChildren<TMPro.TMP_InputField>();

            // Setup the text entry.
            this.inputField.onSubmit.AddListener(this.LoadFlagPreviewStartText);

            this.HideMenu();
        }

        void Update()
        {
            if (this.shown && Input.GetKeyDown("escape"))
            {
                this.HideMenu();
            }
        }

        private void HandleError(ErrorType e)
        {
            CustomFlags.Log(e);
        }

        private void HandleError(string e)
        {
            CustomFlags.Log(e);
        }

        public void HideMenu()
        {
            this.currentFlag = null;
            this.cg.alpha = 0;
            this.cg.interactable = false;
            this.cg.blocksRaycasts = false;
            this.shown = false;
            RAPI.ToggleCursor(false);
        }

        public IEnumerator LoadFlagPreview()
        {
            byte[] temp = null;

            string path = this.inputField.text;

            if (path.ToLower().StartsWith("http://") || path.ToLower().StartsWith("https://"))
            {
                UnityWebRequest www = UnityWebRequest.Get(path);
                var handler = new DownloadHandlerTexture();
                www.downloadHandler = handler;

                yield return www.SendWebRequest();

                if (www.responseCode > 500)
                {
                    this.HandleError("A server error occured while getting the url.");
                }
                else if (www.responseCode > 400)
                {
                    this.HandleError("The url could not be accessed.");
                }
                else if (www.responseCode != 200)
                {
                    this.HandleError($"Request had response code of {www.responseCode}");
                }
                else if (www.error != null)
                {
                    this.HandleError(www.error);
                }
                else if (handler.texture == null)
                {
                    this.HandleError("Remote file was not a valid texture.");
                }
                else
                {
                    // Success!
                    this.imageData = handler.data;
                    this.flagPreview.overrideSprite = CustomFlags.CreateFlagSpriteFromBytes(handler.data);
                }
            }
            else
            {
                if (File.Exists(path))
                {
                    temp = File.ReadAllBytes(path);
                    Sprite s = CustomFlags.CreateFlagSpriteFromBytes(temp);
                    if (temp.Length == 0 || s == null)
                    {
                        this.HandleError("File does not contain a valid flag. Valid flag must be a PNG or JPG file.");
                    }
                    else
                    {
                        this.imageData = temp;
                        this.flagPreview.overrideSprite = s;
                    }
                }
                else
                {
                    this.HandleError("Path could not be found.");
                }
            }
        }

        public void LoadFlagPreviewStart()
        {
            StartCoroutine(this.LoadFlagPreview());
        }

        public void LoadFlagPreviewStartText(string _ = "")
        {
            StartCoroutine(this.LoadFlagPreview());
        }

        public void SetFlagDefault()
        {
            this.imageData = new byte[0];
            this.flagPreview.overrideSprite = CustomFlags.instance.defaultFlagSprite;
        }

        public void ShowMenu(Block_CustomFlag cf)
        {
            if (cf == null)
            {
                return;
            }
            this.cg.alpha = 1;
            this.cg.interactable = true;
            this.cg.blocksRaycasts = true;
            this.shown = true;
            this.currentFlag = cf;
            this.imageData = cf.ImageData;
            this.flagPreview.overrideSprite = CustomFlags.CreateFlagSpriteFromBytes(this.imageData);
            RAPI.ToggleCursor(true);
        }

        public void UpdateFlag()
        {
            if (this.currentFlag)
            {
                this.currentFlag.ImageData = this.imageData;
            }
            this.HideMenu();
        }

        enum ErrorType
        {
            PATH_NOT_FOUND,
            URL_NOT_FOUND,
            URL_ERROR,
            BAD_DATA
        }
    }


    public class CF_Network : MonoBehaviour_Network
    {
        public void OnBlockPlaced()
        {
            NetworkIDManager.AddNetworkID(this);
        }

        void OnDestroy()
        {
            NetworkIDManager.RemoveNetworkID(this);
        }

        public override bool Deserialize(Message_NetworkBehaviour msg, CSteamID remoteID)
        {
            var message = msg as Message_Animal_AnimTriggers;
            if ((int)msg.Type == -75 && message != null && message.anim_triggers.Length == 3)
            {
                if (!CustomFlags.IgnoreFlagMessages)
                {
                    int width = Int32.Parse(message.anim_triggers[1]);
                    int height = Int32.Parse(message.anim_triggers[2]);
                    byte[] data = Convert.FromBase64String(message.anim_triggers[0]).ToTexture2D(width, height)?.EncodeToPNG();
                    if (data != null)
                    {
                        this.GetComponent<Block_CustomFlag>().ImageData = data;
                        return true;
                    }
                }
                else
                {
                    return false;
                }
            }
            return base.Deserialize(msg, remoteID);
        }

        public void BroadcastFlag(byte[] data)
        {
            if (LoadSceneManager.IsGameSceneLoaded && !CustomFlags.IgnoreFlagMessages)
            {
                Texture2D tex = new Texture2D(1, 1);
                ImageConversion.LoadImage(tex, data);
                byte[] newData = data.SanitizeImage();
                var msg = new Message_Animal_AnimTriggers((Messages)(-75), ComponentManager<Raft_Network>.Value.NetworkIDManager, this.ObjectIndex, new string[] { Convert.ToBase64String(newData), tex.width.ToString(), tex.height.ToString() });
                ComponentManager<Raft_Network>.Value.RPC(msg, Target.All, EP2PSend.k_EP2PSendReliable, (NetworkChannel)17732);
            }
        }
    }

    // Class for handling how to display a custom flag.
    public class Block_CustomFlag : Block, IRaycastable
    {
        private byte[] imageData;
        private bool rendererPatched = false;
        private bool showingText;

        public byte[] ImageData
        {
            get
            {
                return this.imageData;
            }
            set
            {
                // Backup our data incase the patch function fails.
                byte[] oldData = this.imageData;
                this.imageData = value;
                if (!this.PatchRenderer())
                {
                    this.imageData = oldData;
                }
            }
        }

        void Start()
        {
            if (!this.rendererPatched)
            {
                this.ImageData = new byte[0];
            }
        }

        /*
         * Attempt to load the new data into the renderer, returning whether it
         * succeeded.
         */
        private bool PatchRenderer()
        {
            if (!this.occupyingComponent)
            {
                this.occupyingComponent = this.GetComponent<OccupyingComponent>();
            }
            if (this.imageData == null)
            {
                return false;
            }
            if (this.imageData.Length != 0)
            {
                // Create a new material from our data.
                Material mat = CustomFlags.CreateMaterialFromImageData(this.imageData);
                // If the creation fails, return false to signify.
                if (!mat)
                {
                    return false;
                }

                // Replace the material for the correct renderer.
                if (this.occupyingComponent.renderers.Length == 1)
                {
                    this.occupyingComponent.renderers[0].material = mat;
                }
                else
                {
                    this.occupyingComponent.renderers[1].material = mat;
                }
            }
            else
            {
                // If we are here, use the default flag material.

                // Replace the material for the correct renderer.
                if (this.occupyingComponent.renderers.Length == 1)
                {
                    this.occupyingComponent.renderers[0].material = CustomFlags.instance.defaultFlag;
                }
                else
                {
                    this.occupyingComponent.renderers[1].material = CustomFlags.instance.defaultFlag;
                }
            }

            this.rendererPatched = true;
            this.GetComponent<CF_Network>()?.BroadcastFlag(this.imageData);
            return true;
        }

        public override RGD Serialize_Save()
        {
            var r = CustomFlags.CreateObject<RGD_Storage>();
            r.CopyFieldsOf(new RGD_Block(RGDType.Block, this));
            r.slots = new RGD_Slot[] { CustomFlags.CreateObject<RGD_Slot>() };
            r.slots[0].exclusiveString = Convert.ToBase64String(this.ImageData);
            return r;
        }

        void IRaycastable.OnIsRayed()
        {
            if (!this.hasBeenPlaced)
            {
                return;
            }
            CanvasHelper canvas = ComponentManager<CanvasHelper>.Value;
            if (CanvasHelper.ActiveMenu != MenuType.None || !Helper.LocalPlayerIsWithinDistance(transform.position, Player.UseDistance))
            {
                if (this.showingText)
                {
                    canvas.displayTextManager.HideDisplayTexts();
                    this.showingText = false;
                }
            }
            else
            {
                canvas.displayTextManager.ShowText("Press E to open the flag selection menu.", 0, true, 0);
                this.showingText = true;
                if (Input.GetKeyDown(KeyCode.E))
                {
                    CustomFlags.OpenCustomFlagsMenu(this);
                }
            }
        }

        void IRaycastable.OnRayEnter() {}

        void IRaycastable.OnRayExit()
        {
            if (this.showingText)
            {
                ComponentManager<CanvasHelper>.Value.displayTextManager.HideDisplayTexts();
                this.showingText = false;
            }
        }
    }

    [HarmonyPatch(typeof(RGD_Block), "RestoreBlock")]
    public class Patch_RestoreBlock
    {
        public static void Postfix(ref RGD_Block __instance, Block block)
        {
            if (__instance.BlockIndex == CustomFlags.CUSTOM_FLAG_ITEM_ID)
            {
                Block_CustomFlag cf = block as Block_CustomFlag;
                RGD_Storage rgd = __instance as RGD_Storage;
                if (cf != null && rgd != null)
                {
                    byte[] imageData = Convert.FromBase64String(rgd.slots[0].exclusiveString);
                    if (imageData != null)
                    {
                        cf.ImageData = imageData;
                    }
                }
            }
        }
    }

    static class Texture2DExtension
    {
        // How is Aidan so amazing?
        public static Texture2D CreateReadable(this Texture2D source, Rect? copyArea = null, RenderTextureFormat format = RenderTextureFormat.Default, RenderTextureReadWrite readWrite = RenderTextureReadWrite.Default, TextureFormat? targetFormat = null, bool mipChain = true)
        {
            var temp = RenderTexture.GetTemporary(source.width, source.height, 0, format, readWrite);
            Graphics.Blit(source, temp);
            temp.filterMode = FilterMode.Point;
            var prev = RenderTexture.active;
            RenderTexture.active = temp;
            var area = copyArea ?? new Rect(0, 0, temp.width, temp.height);
            area.y = temp.height - area.y - area.height;
            var texture = new Texture2D((int)area.width, (int)area.height, targetFormat ?? TextureFormat.RGBA32, mipChain);
            texture.ReadPixels(area, 0, 0);
            texture.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(temp);
            return texture;
        }

        // Aidan is a god.
        public static void Edit(this Texture2D baseImg, Texture2D overlay, int xOffset, int yOffset, int targetX, int targetY)
        {
            var w = targetX - 1;
            var h = targetY - 1;
            for (var x = 0; x <= w; x++)
                 for (var y = 0; y <= h; y++)
                      baseImg.SetPixel(xOffset + x, yOffset + y, baseImg.GetPixel(x, y).Overlay(overlay.GetPixelBilinear((float)x / w, (float)y / h)));
            baseImg.Apply();
        }

        public static byte[] SanitizeImage(this byte[] arr)
        {
            if (arr == null)
            {
                return new byte[0];
            }
            if (arr.Length == 0)
            {
                return arr;
            }
            // Load our plain data.
            Texture2D texOriginal = new Texture2D(1, 1);
            if (!ImageConversion.LoadImage(texOriginal, arr))
            {
                return new byte[0];
            }

            // Resize the image before saving the color data.
            Texture2D tex = new Texture2D(381, 256);
            tex.Edit(texOriginal, 0, 0, 381, 256);

            Color32[] colors = tex.GetPixels32();
            List<byte> bytes = new List<byte>();
            foreach(var c in colors)
            {
                bytes.Add(c.r);
                bytes.Add(c.g);
                bytes.Add(c.b);
                bytes.Add(c.a);
            }

            return bytes.ToArray();
        }

        public static Texture2D ToTexture2D(this byte[] arr, int width, int height)
        {
            if (arr == null)
            {
                return null;
            }
            if ((arr.Length & 3) != 0 || arr.Length != (width * height * 4))
            {
                return null;
            }

            List<Color32> colors = new List<Color32>();
            Color32 currentColor = new Color32(0, 0, 0, 0);

            for (int i = 0; i < arr.Length; ++i)
            {
                // Shortcut for i % 4;
                switch (i & 3)
                {
                    case 0:
                        if (i != 0)
                        {
                            colors.Add(currentColor);
                            currentColor = new Color32(0, 0, 0, 0);
                        }
                        currentColor.r = arr[i];
                        break;
                    case 1:
                        currentColor.g = arr[i];
                        break;
                    case 2:
                        currentColor.b = arr[i];
                        break;
                    case 3:
                        currentColor.a = arr[i];
                        break;
                }
            }
            colors.Add(currentColor);

            var tex = new Texture2D(width, height);
            tex.SetPixels32(colors.ToArray());
            return tex;
        }

        public static Color Overlay(this Color a, Color b)
        {
            if (a.a <= 0)
                return b;
            if (b.a <= 0)
                return a;
            var r = b.a / (b.a + a.a * (1 - b.a));
            float Ratio(float aV, float bV) => bV * r + aV * (1 - r);
            return new Color(Ratio(a.r, b.r), Ratio(a.g, b.g), Ratio(a.b, b.b), b.a + a.a * (1 - b.a));
        }
    }

    // Thank you Aidan for these methods.
    static class ExtensionMethods
    {
        public static void CopyFieldsOf(this object value, object source)
        {
            var t1 = value.GetType();
            var t2 = source.GetType();
            while (!t1.IsAssignableFrom(t2))
                t1 = t1.BaseType;
            while (t1 != typeof(UnityEngine.Object) && t1 != typeof(object))
            {
                foreach (var f in t1.GetFields(~BindingFlags.Default))
                    if (!f.IsStatic)
                        f.SetValue(value, f.GetValue(source));
                t1 = t1.BaseType;
            }
        }

        public static void ReplaceValues(this Component value, object original, object replacement, int serializableLayers = 0)
        {
            foreach (var c in value.GetComponentsInChildren<Component>())
                (c as object).ReplaceValues(original, replacement, serializableLayers);
        }
        public static void ReplaceValues(this GameObject value, object original, object replacement, int serializableLayers = 0)
        {
            foreach (var c in value.GetComponentsInChildren<Component>())
                (c as object).ReplaceValues(original, replacement, serializableLayers);
        }

        public static void ReplaceValues(this object value, object original, object replacement, int serializableLayers = 0)
        {
            if (value == null)
                return;
            var t = value.GetType();
            while (t != typeof(UnityEngine.Object) && t != typeof(object))
            {
                foreach (var f in t.GetFields(~BindingFlags.Default))
                    if (!f.IsStatic)
                    {
                        if (f.GetValue(value) == original || (f.GetValue(value)?.Equals(original) ?? false))
                            try
                            {
                                f.SetValue(value, replacement);
                            } catch { }
                        else if (f.GetValue(value) is IList)
                        {
                            var l = f.GetValue(value) as IList;
                            for (int i = 0; i < l.Count; i++)
                                if (l[i] == original || (l[i]?.Equals(original) ?? false))
                                    try
                                    {
                                        l[i] = replacement;
                                    } catch { }

                        }
                        else if (serializableLayers > 0 && (f.GetValue(value)?.GetType()?.IsSerializable ?? false))
                            f.GetValue(value).ReplaceValues(original, replacement, serializableLayers - 1);
                    }
                t = t.BaseType;
            }
        }

        public static void SetRecipe(this Item_Base item, CostMultiple[] cost, CraftingCategory category = CraftingCategory.Resources, int amountToCraft = 1, bool learnedFromBeginning = false, string subCategory = null, int subCatergoryOrder = 0)
        {
            Traverse recipe = Traverse.Create(item.settings_recipe);
            recipe.Field("craftingCategory").SetValue(category);
            recipe.Field("amountToCraft").SetValue(amountToCraft);
            recipe.Field("learnedFromBeginning").SetValue(learnedFromBeginning);
            recipe.Field("subCategory").SetValue(subCategory);
            recipe.Field("subCatergoryOrder").SetValue(subCatergoryOrder);
            item.settings_recipe.NewCost = cost;
        }
    }
}
