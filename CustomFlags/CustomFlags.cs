using HarmonyLib;
using I2.Loc;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
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
        //TODO: Raycast combine for sail;

        private const int FLAG_ID = 478;
        private const int SAIL_ID = 126;

        // In little endian this would represent the string "De";
        public static readonly int CUSTOM_FLAG_ITEM_ID = 25924;
        public static readonly int CUSTOM_SAIL_ITEM_ID = 25925;
        public static readonly int CUSTOM_BLOCK_ID_MIN = CUSTOM_FLAG_ITEM_ID;
        public static readonly int CUSTOM_BLOCK_ID_MAX = CUSTOM_SAIL_ITEM_ID;
        public static readonly Dictionary<BlockType, (int, int)> LOCATIONS = new Dictionary<BlockType, (int, int)>()
        {
            { BlockType.FLAG, (253, 768) },
            { BlockType.SAIL, (4, 132) },
        };
        public static readonly Dictionary<BlockType, (int, int)> SIZES = new Dictionary<BlockType, (int, int)>()
        {
            { BlockType.FLAG, (381, 256) },
            { BlockType.SAIL, (792, 674) },
        };

        public static CustomFlags instance;
        public static JsonModInfo modInfo;

        private static GameObject menu;
        private static GameObject menuAsset;
        private static CustomFlagsMenu cfMenu;

        // Dictionaries for storing the data for new materials.
        public Dictionary<BlockType, Texture2D> baseTextures;
        public Dictionary<BlockType, Texture2D> baseNormals;
        public Dictionary<BlockType, Texture2D> basePaints;
        // Dictionary for the default materials.
        public Dictionary<BlockType, Material> defaultMaterials;
        // Dictionary for the default sprites.
        public Dictionary<BlockType, Sprite> defaultSprites;

        private Shader shader;
        private Item_Base[] customItems;
        private Harmony harmony;
        private Transform prefabHolder;
        private AssetBundle bundle;

        public static bool IgnoreFlagMessages
        {
            get
            {
                return CustomFlags.instance.ExtraSettingsAPI_GetCheckboxState("ignoreFlagMessages");
            }
        }

        public static KeyCode InteractKey
        {
            get
            {
                return CustomFlags.instance.ExtraSettingsAPI_GetKeybindMain("interactKey");
            }
        }

        public static bool EditorEnabled
        {
            get
            {
                return CustomFlags.instance.ExtraSettingsAPI_GetCheckboxState("editorEnabled");
            }
        }

        public static bool PreventChanges
        {
            get
            {
                return CustomFlags.instance.ExtraSettingsAPI_GetCheckboxState("preventChanges");
            }
        }

        // Just leaving the command here that was used in testing.
        // FindObjectOfType<Streamer>().GetComponent<Block>().occupyingComponent.renderers[1]; y.material = j;

        public IEnumerator Start()
        {
            CustomFlags.instance = this;
            this.baseTextures = new Dictionary<BlockType, Texture2D>();
            this.baseNormals = new Dictionary<BlockType, Texture2D>();
            this.basePaints = new Dictionary<BlockType, Texture2D>();
            this.defaultMaterials = new Dictionary<BlockType, Material>();
            this.defaultSprites = new Dictionary<BlockType, Sprite>();
            HNotification notification = HNotify.instance.AddNotification(HNotify.NotificationType.spinning, "Loading CustomFlags...");

            CustomFlags.modInfo = modlistEntry.jsonmodinfo;
            this.harmony = new Harmony("com.destruction.CustomFlags");
            this.harmony.PatchAll(Assembly.GetExecutingAssembly());
            this.prefabHolder = new GameObject("prefabHolder").transform;
            this.prefabHolder.gameObject.SetActive(false);
            DontDestroyOnLoad(this.prefabHolder.gameObject);

            // First thing is first, let's fetch our shader from the game.
            this.shader = Shader.Find(" BlockPaint");

            // Second, setup the data for each block. First is the flag.
            var originalMat = ItemManager.GetItemByIndex(FLAG_ID).settings_buildable.GetBlockPrefab(DPS.Floor).GetComponent<OccupyingComponent>().renderers[1].material;
            this.AddBaseTextures(BlockType.FLAG, originalMat, "flag/transparent.png", "flag/normal.png", "flag/transparent.png");
            // Setup the data for the sail.
            originalMat = ItemManager.GetItemByIndex(SAIL_ID).settings_buildable.GetBlockPrefab(DPS.Floor).GetComponentInChildren<MeshRenderer>().material;
            this.AddBaseTextures(BlockType.SAIL, originalMat, "sail/transparent.png", "sail/normal.png", "sail/transparent.png");

            // Create our default flag material.
            this.defaultMaterials[BlockType.FLAG] = CustomFlags.CreateMaterialFromImageData(GetEmbeddedFileBytes("flag/default.png").SanitizeImage(BlockType.FLAG), BlockType.FLAG);
            this.defaultMaterials[BlockType.SAIL] = CustomFlags.CreateMaterialFromImageData(GetEmbeddedFileBytes("sail/default.png").SanitizeImage(BlockType.SAIL), BlockType.SAIL);
            // Create the custom flag item base.
            this.customItems = new Item_Base[] {
                this.CreateCustomFlagItem(),
                this.CreateCustomSailItem(),
            };

            Array.ForEach(this.customItems, x => RAPI.RegisterItem(x));

            // Create the default flag sprite for the menu.
            this.defaultSprites[BlockType.FLAG] = CustomFlags.CreateSpriteFromBytes(GetEmbeddedFileBytes("flag/default.png").SanitizeImage(BlockType.FLAG), BlockType.FLAG);
            this.defaultSprites[BlockType.SAIL] = CustomFlags.CreateSpriteFromBytes(GetEmbeddedFileBytes("sail/default.png").SanitizeImage(BlockType.SAIL), BlockType.SAIL);

            // Now, load the menu from the asset bundle.
            var bundleLoadRequest = AssetBundle.LoadFromMemoryAsync(GetEmbeddedFileBytes("general_assets/customflags.assets"));
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
            this.harmony?.UnpatchAll("com.destruction.CustomFlags");
            this.bundle?.Unload(true);
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

        public static void OpenCustomFlagsMenu(Block_CustomBlock_Base cf)
        {
            CustomFlags.cfMenu.ShowMenu(cf);
        }

        /*
         * Creates the base texture, normal, and paint Texture2Ds for a
         * specified block type using original material and the internal files
         * specified. Then, adds them to the dictionary.
         */
        private void AddBaseTextures(BlockType bt, Material originalMat, string texture, string normal, string paint)
        {
            Texture2D insertTex = new Texture2D(SIZES[bt].Item1, SIZES[bt].Item2);

            Texture2D[] add = new Texture2D[3];

            // Get the diffuse portion of the texture.
            add[0] = (originalMat.GetTexture("_Diffuse") as Texture2D).CreateReadable();
            ImageConversion.LoadImage(insertTex, GetEmbeddedFileBytes(texture));
            CustomFlags.PlaceImageInTexture(add[0], insertTex, bt);

            // Now we need to generate our normal map.
            add[1] = (originalMat.GetTexture("_Normal") as Texture2D).CreateReadable();
            ImageConversion.LoadImage(insertTex, GetEmbeddedFileBytes(normal));
            CustomFlags.PlaceImageInTexture(add[1], insertTex, bt);

            // Now we need to generate our paint map.
            add[2] = (originalMat.GetTexture("_MetallicRPaintMaskGSmoothnessA") as Texture2D).CreateReadable();
            ImageConversion.LoadImage(insertTex, GetEmbeddedFileBytes(paint));
            CustomFlags.PlaceImageInTexture(add[2], insertTex, bt);

            this.baseTextures[bt] = add[0];
            this.baseNormals[bt] = add[1];
            this.basePaints[bt] = add[2];
        }

        /*
         * Returns a new material with the flag data inside of it. Returns null
         * if the data is bad.
         */
        public static Material CreateMaterialFromImageData(byte[] data, BlockType bt)
        {
            // Load the data into a texture.
            Texture2D tex = data.ToTexture2D(SIZES[bt].Item1, SIZES[bt].Item2);
            if (!tex)
            {
                Debug.Log("Could not create material from image data: data failed to convert.");
                return null;
            }

            // Create the material and put the flag inside of it.
            Material mat = CustomFlags.instance.PrepareMaterial(bt);
            CustomFlags.PlaceImageInMaterial(mat, tex, bt);

            // Return it.
            return mat;
        }

        /*
         * Uses the data to create a flag sprite. Returns null if flag is bad.
         * Returns the default flag texture if the data is 0 bytes.
         */
        public static Sprite CreateSpriteFromBytes(byte[] data, BlockType bt)
        {
            if (data == null)
            {
                return null;
            }
            if (data.Length == 0)
            {
                return CustomFlags.instance.defaultSprites[bt];
            }

            Vector2 pivot = new Vector2(0.5f, 0.5f);
            Rect rect = new Rect(0, 0, 1524, 1024);

            Texture2D container = new Texture2D(1524, 1024);
            Texture2D imageTex = data.ToTexture2D(SIZES[bt].Item1, SIZES[bt].Item2);
            (int, int, int, int) newSize = CustomFlags.ScaleToFit(SIZES[bt].Item1, SIZES[bt].Item2, 1524, 1024);
            container.Edit(imageTex, newSize.Item1, newSize.Item2, newSize.Item3, newSize.Item4);

            return Sprite.Create(container, rect, pivot);
        }

        /*
         * Prepares a new material for flags to be placed into.
         */
        private Material PrepareMaterial(BlockType bt)
        {
            // Load the textures.
            Texture2D diffuse = new Texture2D(1024, 1024);
            Texture2D paint = new Texture2D(1024, 1024);
            Texture2D normal = new Texture2D(1024, 1024);

            Graphics.CopyTexture(this.baseTextures[bt], diffuse);
            Graphics.CopyTexture(this.baseNormals[bt], normal);
            Graphics.CopyTexture(this.basePaints[bt], paint);

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
                    cf.networkedBehaviour = cf.gameObject.AddComponent<CustomBlock_Network>();
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
         * Finds the base sail item and uses it to create a new custom sail
         * item.
         */
        private Item_Base CreateCustomSailItem()
        {
            // Create a clone of the regular flag.
            Item_Base originalItem = ItemManager.GetItemByIndex(SAIL_ID);
            Item_Base customSail = ScriptableObject.CreateInstance<Item_Base>();
            customSail.Initialize(CUSTOM_SAIL_ITEM_ID, "destiny_CustomSail", 1);
            customSail.settings_buildable = originalItem.settings_buildable.Clone();
            customSail.settings_consumeable = originalItem.settings_consumeable.Clone();
            customSail.settings_cookable = originalItem.settings_cookable.Clone();
            customSail.settings_equipment = originalItem.settings_equipment.Clone();
            customSail.settings_Inventory = originalItem.settings_Inventory.Clone();
            customSail.settings_recipe = originalItem.settings_recipe.Clone();
            customSail.settings_usable = originalItem.settings_usable.Clone();

            Block[] blocks = customSail.settings_buildable.GetBlockPrefabs().Clone() as Block[];

            // Set the block to not be paintable.
            Traverse.Create(customSail.settings_buildable).Field("primaryPaintAxis").SetValue(Axis.None);

            // Setup the recipe.
            customSail.SetRecipe(new[]
                {
                    new CostMultiple(new[] { ItemManager.GetItemByIndex(21) }, 4),
                    new CostMultiple(new[] { ItemManager.GetItemByIndex(23) }, 2),
                    new CostMultiple(new[] { ItemManager.GetItemByIndex(25) }, 6)
                }, CraftingCategory.Navigation, 1, false, "CustomSail", 1);
            Traverse.Create(customSail.settings_recipe).Field("_hiddenInResearchTable").SetValue(false);

            // Localization stuff.
            customSail.settings_Inventory.LocalizationTerm = "Item/destiny_CustomSail";
            var language = new LanguageSourceData()
            {
                mDictionary = new Dictionary<string, TermData>
                {
                    ["Item/destiny_CustomSail"] = new TermData() { Languages = new[] { "Custom Sail@A customizable sail." } },
                    ["CraftingSub/CustomSail"] = new TermData() { Languages = new[] { "Custom Sail" } }
                },
                mLanguages = new List<LanguageData> { new LanguageData() { Code = "en", Name = "English" } }
            };
            LocalizationManager.Sources.Add(language);
            Traverse.Create(typeof(LocalizationManager)).Field("OnLocalizeEvent").GetValue<LocalizationManager.OnLocalizeCallback>().Invoke();


            // Now, we need to replace Block with Block_CustomSail;
            for (int i = 0; i < blocks.Length; ++i)
            {
                if (blocks[i])
                {
                    // Based on some of Aidan's code.
                    var sailPrefab = Instantiate(blocks[i], this.prefabHolder, false);
                    sailPrefab.name = $"Block_CustomSail_{i}";
                    var cs = sailPrefab.gameObject.AddComponent<Block_CustomSail>();
                    cs.CopyFieldsOf(sailPrefab);
                    cs.ReplaceValues(sailPrefab, cs);
                    sailPrefab.ReplaceValues(originalItem, customSail);
                    blocks[i] = cs;
                    DestroyImmediate(sailPrefab);

                    // Finally, we need to remove the Sail instance and add our
                    // own class instead.
                    var sail = cs.gameObject.AddComponent<CustomSail_Network>();
                    sail.CopyFieldsOf(cs.GetComponent<Sail>());
                    sail.ReplaceValues(cs.GetComponent<Sail>(), sail);
                    DestroyImmediate(cs.GetComponent<Sail>());
                    cs.networkedBehaviour = sail;

                    cs.networkType = NetworkType.NetworkBehaviour;
                    cs.ImageData = new byte[0];
                }
            }

            Traverse.Create(customSail.settings_buildable).Field("blockPrefabs").SetValue(blocks);

            foreach (var q in Resources.FindObjectsOfTypeAll<SO_BlockQuadType>())
                if (q.AcceptsBlock(originalItem))
                    Traverse.Create(q).Field("acceptableBlockTypes").GetValue<List<Item_Base>>().Add(customSail);

            return customSail;
        }

        /*
         * Overwrites the data in the specified area with the specified texture.
         * This simplifies the calls to work with a material instead of a
         * Texture2D for the destination.
         *
         * :param dest: The material for the image to go into.
         * :param src: The source texture to add.
         * :param bt: The type of block the texture is. This tells where to
         *     place the texture and what size it will be.
         */
        private static void PlaceImageInMaterial(Material dest, Texture2D src, BlockType bt)
        {
            CustomFlags.PlaceImageInTexture(dest.GetTexture("_Diffuse") as Texture2D, src, bt);
        }

        /*
         * Overwrites the data in the specified area with the specified texture.
         *
         * :param dest: The texture for the flags to go into.
         * :param src: The flag texture to add.
         * :param bt: The type of block the texture is. This tells where to
         *     place the texture and what size it will be.
         */
        private static void PlaceImageInTexture(Texture2D dest, Texture2D src, BlockType bt)
        {
            dest.Edit(src, LOCATIONS[bt].Item1, LOCATIONS[bt].Item2, SIZES[bt].Item1, SIZES[bt].Item2);
        }

        /*
         * Function to retun offset to place at as well as the new size for an
         * image being placed in the specified box.
         *
         * :returns: 4 tuple of x offset, y offset, newWidth, newHeight.
         */
        public static (int, int, int, int) ScaleToFit(int srcWidth, int srcHeight, int destWidth, int destHeight)
        {
            double scaleW = destWidth / (double)srcWidth;
            double scaleH = destHeight / (double)srcHeight;
            double scaleRatio = Math.Min(scaleW, scaleH);
            int newWidth = Math.Min(destWidth, (int)(scaleRatio * srcWidth));
            int newHeight = Math.Min(destHeight, (int)(scaleRatio * srcHeight));
            int xOffset = (destWidth - newWidth) / 2;
            int yOffset = (destHeight - newHeight) / 2;


            return (xOffset, yOffset, newWidth, newHeight);
        }

        public static T CreateObject<T>() => (T)FormatterServices.GetUninitializedObject(typeof(T));
        public bool ExtraSettingsAPI_GetCheckboxState(string name) => true;
        public KeyCode ExtraSettingsAPI_GetKeybindMain(string SettingName) => KeyCode.None;

        public static void LogStack()
        {
            var trace = new System.Diagnostics.StackTrace();
            var message = "";
            foreach (var frame in trace.GetFrames())
            {
                var method = frame.GetMethod();
                if (method.Name.Equals("LogStack")) continue;
                message += string.Format("{0}::{1}",
                    method.ReflectedType != null ? method.ReflectedType.Name : string.Empty,
                    method.Name) + "\n";
            }
            CustomFlags.DebugLog(message);
        }

        public enum BlockType
        {
            FLAG,
            SAIL
        }
    }



    public class CustomFlagsMenu : MonoBehaviour
    {
        private CanvasGroup cg;
        private UnityEngine.UI.Image preview;
        private Block_CustomBlock_Base currentBlock;
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
                else if (button.gameObject.name.StartsWith("DefaultButton"))
                {
                    button.onClick.AddListener(this.SetBlockDefault);
                }
                else if (button.gameObject.name.StartsWith("LoadButton"))
                {
                    button.onClick.AddListener(this.LoadPreviewStart);
                }
                else if (button.gameObject.name.StartsWith("UpdateButton"))
                {
                    button.onClick.AddListener(this.UpdateBlock);
                }
            }

            // Get the components.
            this.cg = this.GetComponent<CanvasGroup>();
            this.preview = this.GetComponentsInChildren<UnityEngine.UI.Image>().First(x => x.gameObject.name.StartsWith("Preview"));
            this.inputField = this.GetComponentInChildren<TMPro.TMP_InputField>();

            // Setup the text entry.
            this.inputField.onSubmit.AddListener(this.LoadPreviewStartText);

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
            this.currentBlock = null;
            this.cg.alpha = 0;
            this.cg.interactable = false;
            this.cg.blocksRaycasts = false;
            this.shown = false;
            RAPI.ToggleCursor(false);
        }

        public IEnumerator LoadPreview()
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
                    this.imageData = handler.data.SanitizeImage(this.currentBlock.CustomBlockType);
                    this.preview.overrideSprite = CustomFlags.CreateSpriteFromBytes(this.imageData, this.currentBlock.CustomBlockType);
                }
            }
            else
            {
                if (File.Exists(path))
                {
                    temp = File.ReadAllBytes(path);
                    byte[] temp2 = temp.Length > 0 ? temp.SanitizeImage(this.currentBlock.CustomBlockType) : temp;
                    Sprite s = CustomFlags.CreateSpriteFromBytes(temp2, this.currentBlock.CustomBlockType);
                    if (temp2.Length == 0 || s == null)
                    {
                        this.HandleError("File does not contain a valid flag. Valid flag must be a PNG or JPG file.");
                    }
                    else
                    {
                        this.imageData = temp2;
                        this.preview.overrideSprite = s;
                    }
                }
                else
                {
                    this.HandleError("Path could not be found.");
                }
            }
        }

        public void LoadPreviewStart()
        {
            StartCoroutine(this.LoadPreview());
        }

        public void LoadPreviewStartText(string _ = "")
        {
            StartCoroutine(this.LoadPreview());
        }

        public void SetBlockDefault()
        {
            this.imageData = new byte[0];
            this.preview.overrideSprite = CustomFlags.instance.defaultSprites[this.currentBlock.CustomBlockType];
        }

        public void ShowMenu(Block_CustomBlock_Base cb)
        {
            if (cb == null)
            {
                return;
            }
            this.cg.alpha = 1;
            this.cg.interactable = true;
            this.cg.blocksRaycasts = true;
            this.shown = true;
            this.currentBlock = cb;
            this.imageData = cb.ImageData;
            this.preview.overrideSprite = CustomFlags.CreateSpriteFromBytes(this.imageData, cb.CustomBlockType);
            RAPI.ToggleCursor(true);
        }

        public void UpdateBlock()
        {
            if (this.currentBlock)
            {
                this.currentBlock.ImageData = this.imageData;
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


    // Base class for custom block types' network behaviour.
    public class CustomBlock_Network : MonoBehaviour_Network
    {
        public virtual void OnBlockPlaced()
        {
            NetworkIDManager.AddNetworkID(this);
        }

        protected override void OnDestroy()
        {
            NetworkIDManager.RemoveNetworkID(this);
            base.OnDestroy();
        }

        public override bool Deserialize(Message_NetworkBehaviour msg, CSteamID remoteID)
        {
            var message = msg as Message_Animal_AnimTriggers;
            if ((int)msg.Type == -75 && message != null && message.anim_triggers.Length == 1)
            {
                if (!CustomFlags.IgnoreFlagMessages && !(Raft_Network.IsHost && CustomFlags.PreventChanges))
                {
                    byte[] data = Convert.FromBase64String(message.anim_triggers[0]);
                    if (data != null)
                    {
                        this.GetComponent<Block_CustomBlock_Base>().ImageData = data;
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

        public virtual void BroadcastChange(byte[] data)
        {
            if (LoadSceneManager.IsGameSceneLoaded && !CustomFlags.IgnoreFlagMessages)
            {
                var msg = new Message_Animal_AnimTriggers((Messages)(-75), ComponentManager<Raft_Network>.Value.NetworkIDManager, this.ObjectIndex, new string[] { Convert.ToBase64String(data) });
                ComponentManager<Raft_Network>.Value.RPC(msg, Target.All, EP2PSend.k_EP2PSendReliable, (NetworkChannel)17732);
            }
        }
    }

    public class CustomSail_Network : Sail, IRaycastable
    {
        public virtual void OnBlockPlaced()
        {
            // Need to access the lower sail OnBlockPlace, but it is private.
            //Traverse.Create((Sail)this).Method("OnBlockPlaced").GetValue();
            typeof(Sail).GetMethod("OnBlockPlaced", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(this, null);
            NetworkIDManager.AddNetworkID(this);
        }

        protected override void OnDestroy()
        {
            NetworkIDManager.RemoveNetworkID(this);
            base.OnDestroy();
        }

        /*void IRaycastable.OnIsRayed()
        {
            this.OnIsRayed();
        }

        void IRaycastable.OnRayEnter()
        {
            this.OnRayEnter();
        }

        void IRaycastable.OnRayExit()
        {
            this.OnRayExit();
        }*/

        public override RGD Serialize_Save()
        {
            // Override so that the block class handles the save.
            return null;
        }

        public override bool Deserialize(Message_NetworkBehaviour msg, CSteamID remoteID)
        {
            var message = msg as Message_Animal_AnimTriggers;
            if ((int)msg.Type == -75 && message != null && message.anim_triggers.Length == 1)
            {
                if (!CustomFlags.IgnoreFlagMessages)
                {
                    byte[] data = Convert.FromBase64String(message.anim_triggers[0]);
                    if (data != null)
                    {
                        this.GetComponent<Block_CustomBlock_Base>().ImageData = data;
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

        public virtual void BroadcastChange(byte[] data)
        {
            if (LoadSceneManager.IsGameSceneLoaded && !CustomFlags.IgnoreFlagMessages)
            {
                var msg = new Message_Animal_AnimTriggers((Messages)(-75), ComponentManager<Raft_Network>.Value.NetworkIDManager, this.ObjectIndex, new string[] { Convert.ToBase64String(data) });
                ComponentManager<Raft_Network>.Value.RPC(msg, Target.All, EP2PSend.k_EP2PSendReliable, (NetworkChannel)17732);
            }
        }
    }

    // Class for handling how to display a custom item.
    public abstract class Block_CustomBlock_Base : Block, IRaycastable
    {
        protected byte[] imageData;
        protected bool rendererPatched = false;
        protected bool showingText;

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

        public abstract CustomFlags.BlockType CustomBlockType { get; }

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
        protected abstract bool PatchRenderer();

        public override RGD Serialize_Save()
        {
            var r = CustomFlags.CreateObject<RGD_Storage>();
            r.CopyFieldsOf(new RGD_Block(RGDType.Block, this));
            r.slots = new RGD_Slot[] { CustomFlags.CreateObject<RGD_Slot>() };
            r.slots[0].exclusiveString = Convert.ToBase64String(this.ImageData);
            r.slots[0].itemAmount = 1;
            return r;
        }

        public override RGD_Block GetBlockCreationData()
        {
            return this.Serialize_Save() as RGD_Block;
        }

        void IRaycastable.OnIsRayed()
        {
            if (!this.hasBeenPlaced || !CustomFlags.EditorEnabled)
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
                canvas.displayTextManager.ShowText($"Press {CustomFlags.InteractKey} to open the edit menu.", CustomFlags.InteractKey, 0, 0, false);
                this.showingText = true;
                if (Input.GetKeyDown(CustomFlags.InteractKey))
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

    public class Block_CustomFlag : Block_CustomBlock_Base
    {
        public override CustomFlags.BlockType CustomBlockType
        {
            get
            {
                return CustomFlags.BlockType.FLAG;
            }
        }

        /*
         * Attempt to load the new data into the renderer, returning whether it
         * succeeded.
         */
        protected override bool PatchRenderer()
        {
            if (!this.occupyingComponent)
            {
                this.occupyingComponent = this.GetComponent<OccupyingComponent>();
                this.occupyingComponent.FindRenderers();
            }
            if (this.imageData == null)
            {
                return false;
            }
            if (this.imageData.Length != 0)
            {
                // Create a new material from our data.
                Material mat = CustomFlags.CreateMaterialFromImageData(this.imageData, this.CustomBlockType);
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
                    this.occupyingComponent.renderers[0].material = CustomFlags.instance.defaultMaterials[this.CustomBlockType];
                }
                else
                {
                    this.occupyingComponent.renderers[1].material = CustomFlags.instance.defaultMaterials[this.CustomBlockType];
                }
            }

            this.rendererPatched = true;
            this.GetComponent<CustomBlock_Network>()?.BroadcastChange(this.imageData);
            return true;
        }
    }

    public class Block_CustomSail : Block_CustomBlock_Base
    {
        public override CustomFlags.BlockType CustomBlockType
        {
            get
            {
                return CustomFlags.BlockType.SAIL;
            }
        }

        // We need to add sail data to this RGD.
        public override RGD Serialize_Save()
        {
            RGD_Storage rgd = base.Serialize_Save() as RGD_Storage;
            // Attach our data to the existing save data.
            rgd.isOpen = this.GetComponent<Sail>().open;
            // Doing a little data abusing here.
            rgd.storageObjectIndex = BitConverter.ToUInt32(BitConverter.GetBytes(this.GetComponent<Sail>().LocalRotation), 0);

            return rgd;
        }

        /*
         * Attempt to load the new data into the renderer, returning whether it
         * succeeded.
         */
        protected override bool PatchRenderer()
        {
            if (!this.occupyingComponent)
            {
                this.occupyingComponent = this.GetComponent<OccupyingComponent>();
                this.occupyingComponent.FindRenderers();
            }
            if (this.imageData == null)
            {
                return false;
            }
            if (this.imageData.Length != 0)
            {
                // Create a new material from our data.
                Material mat = CustomFlags.CreateMaterialFromImageData(this.imageData, this.CustomBlockType);
                // If the creation fails, return false to signify.
                if (!mat)
                {
                    return false;
                }

                // Replace the material.
                this.occupyingComponent.renderers[1].material = mat;
            }
            else
            {
                // If we are here, use the default sail material.
                this.occupyingComponent.renderers[1].material = CustomFlags.instance.defaultMaterials[this.CustomBlockType];
            }

            this.rendererPatched = true;
            this.GetComponent<CustomSail_Network>()?.BroadcastChange(this.imageData);
            return true;
        }
    }

    [HarmonyPatch(typeof(RGD_Block), "RestoreBlock")]
    public class Patch_RestoreBlock
    {
        public static void Postfix(ref RGD_Block __instance, Block block)
        {
            // Make sure it is one of our blocks.
            if (__instance.BlockIndex >= CustomFlags.CUSTOM_BLOCK_ID_MIN && __instance.BlockIndex <= CustomFlags.CUSTOM_BLOCK_ID_MAX)
            {
                Block_CustomBlock_Base cb = block as Block_CustomBlock_Base;
                RGD_Storage rgd = __instance as RGD_Storage;
                if (cb != null && rgd != null)
                {
                    if (Raft_Network.IsHost || !CustomFlags.IgnoreFlagMessages)
                    {
                        byte[] imageData = Convert.FromBase64String(rgd.slots[0].exclusiveString);
                        if (rgd.slots[0].itemAmount == 0)
                        {
                            CustomFlags.DebugLog("Found flag with old save data. Updating to new save system.");
                            // Handle older saves with a different form of image
                            // save data.
                            if (Raft_Network.IsHost)
                            {
                                imageData = imageData.SanitizeImage(cb.CustomBlockType);
                            }
                            else
                            {
                                // Small protection against unsafe save data
                                // from remote host.
                                imageData = new byte[0];
                            }
                        }
                        if (imageData != null)
                        {
                            cb.ImageData = imageData;
                        }
                    }

                    // Handle the custom sail.
                    if (cb is Block_CustomSail)
                    {
                        Sail sail = block.GetComponent<Sail>();
                        if (rgd.isOpen)
                        {
                            sail?.Open();
                        }
                        sail?.SetRotation(BitConverter.ToSingle(BitConverter.GetBytes(rgd.storageObjectIndex), 0));
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

        public static byte[] SanitizeImage(this byte[] arr, CustomFlags.BlockType bt)
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

            (int, int) size = CustomFlags.SIZES[bt];

            // Resize the image before saving the color data.
            Texture2D tex = new Texture2D(size.Item1, size.Item2);
            tex.Edit(texOriginal, 0, 0, size.Item1, size.Item2);

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
                CustomFlags.DebugLog("Failed to convert byte array to Texture2D: array was null.");
                return null;
            }
            if ((arr.Length & 3) != 0 || arr.Length != (width * height * 4))
            {
                CustomFlags.DebugLog($"Failed to convert byte array to Texture2D: array was wrong length (expected {width * height * 4}, got {arr.Length}).");
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
