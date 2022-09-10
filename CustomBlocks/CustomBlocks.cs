using HarmonyLib;
using I2.Loc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEngine;


namespace DestinyCustomBlocks
{
    public class CustomBlocks : Mod
    {
        // Original id and new id.
        private static readonly Dictionary<BlockType, (int, int)> IDS = new Dictionary<BlockType, (int, int)>()
        {
            // In little endian this would represent the string "De";
            { BlockType.FLAG, (478, 25924) },
            { BlockType.SAIL, (126, 25925) },
            { BlockType.BED, (448, 25926) },
            { BlockType.CURTAIN_H, (447, 25927) },
            { BlockType.CURTAIN_V, (446, 25928) },
            { BlockType.RUG_BIG, (158, 25929) },
            { BlockType.RUG_SMALL, (439, 25930) },
            { BlockType.POSTER_H_16_9, (352, 25931) },
            { BlockType.POSTER_V_9_16, (352, 25932) },
            { BlockType.POSTER_H_4_3, (352, 25933) },
            { BlockType.POSTER_V_3_4, (352, 25934) },
            { BlockType.POSTER_H_3_2, (352, 25935) },
            { BlockType.POSTER_V_2_3, (352, 25936) },
            { BlockType.POSTER_H_2_1, (352, 25939) },
            { BlockType.POSTER_V_1_2, (352, 25940) },
            { BlockType.POSTER_H_5_3, (352, 25941) },
            { BlockType.POSTER_V_3_5, (352, 25942) },
            { BlockType.POSTER_1_1, (352, 25943) },
        };
        private static readonly Dictionary<BlockType, string> FOLDER_NAMES = new Dictionary<BlockType, string>()
        {
            // In little endian this would represent the string "De";
            { BlockType.FLAG, "flag" },
            { BlockType.SAIL, "sail" },
            { BlockType.BED, "bed" },
            { BlockType.CURTAIN_H, "curtain_h" },
            { BlockType.CURTAIN_V, "curtain_v" },
            { BlockType.RUG_BIG, "rug_big" },
            { BlockType.RUG_SMALL, "rug_small" },
            { BlockType.POSTER_H_16_9, "poster_h" },
            { BlockType.POSTER_V_9_16, "poster_v" },
            { BlockType.POSTER_H_5_3, "poster_h" },
            { BlockType.POSTER_V_3_5, "poster_v" },
            { BlockType.POSTER_H_4_3, "poster_h" },
            { BlockType.POSTER_V_3_4, "poster_v" },
            { BlockType.POSTER_H_3_2, "poster_h" },
            { BlockType.POSTER_V_2_3, "poster_v" },
            { BlockType.POSTER_H_2_1, "poster_h" },
            { BlockType.POSTER_V_1_2, "poster_v" },
            { BlockType.POSTER_1_1, "poster_h" },
        };

        public static readonly int CUSTOM_BLOCK_ID_MIN = 25924;
        public static readonly int CUSTOM_BLOCK_ID_MAX = 25943;

        public static readonly Dictionary<BlockType, (int, int)> LOCATIONS = new Dictionary<BlockType, (int, int)>()
        {
            { BlockType.NONE, (0, 0) },
            { BlockType.BED, (5, 5) },
            { BlockType.CURTAIN_H, (-1, -1) },
            { BlockType.CURTAIN_V, (3, 3) },
            { BlockType.FLAG, (256, 770) },
            { BlockType.RUG_BIG, (7, 7) },
            { BlockType.RUG_SMALL, (632, 712) },
            { BlockType.SAIL, (3, 132) },
            { BlockType.POSTER_H_16_9, (-2, -2) },
            { BlockType.POSTER_V_9_16, (-2, -2) },
            { BlockType.POSTER_H_5_3, (-2, -2) },
            { BlockType.POSTER_V_3_5, (-2, -2) },
            { BlockType.POSTER_H_4_3, (-2, -2) },
            { BlockType.POSTER_V_3_4, (-2, -2) },
            { BlockType.POSTER_H_3_2, (-2, -2) },
            { BlockType.POSTER_V_2_3, (-2, -2) },
            { BlockType.POSTER_H_2_1, (-2, -2) },
            { BlockType.POSTER_V_1_2, (-2, -2) },
            { BlockType.POSTER_1_1, (-2, -2) },
        };
        public static readonly Dictionary<BlockType, (int, int)> SIZES = new Dictionary<BlockType, (int, int)>()
        {
            { BlockType.NONE, (0, 0) },
            { BlockType.ICON, (512, 512) },
            { BlockType.BED, (959, 682) },
            { BlockType.CURTAIN_H, (612, 706) },
            { BlockType.CURTAIN_V, (525, 496) },
            { BlockType.FLAG, (377, 252) },
            { BlockType.RUG_BIG, (627, 330) },
            { BlockType.RUG_SMALL, (385, 253) },
            { BlockType.SAIL, (794, 674) },
            { BlockType.POSTER_H_16_9, (1920, 1080) },
            { BlockType.POSTER_V_9_16, (1080, 1920) },
            { BlockType.POSTER_H_5_3, (1800, 1080) },
            { BlockType.POSTER_V_3_5, (1080, 1800) },
            { BlockType.POSTER_H_4_3, (1440, 1080) },
            { BlockType.POSTER_V_3_4, (1080, 1440) },
            { BlockType.POSTER_H_3_2, (1620, 1080) },
            { BlockType.POSTER_V_2_3, (1080, 1620) },
            { BlockType.POSTER_H_2_1, (2160, 1080) },
            { BlockType.POSTER_V_1_2, (1080, 2160) },
            { BlockType.POSTER_1_1, (1080, 1080) },
        };
        // Dictionary to tell what axis to mirror images on. Result is a tuple
        // of whether to mirror the x and whether to mirror the y.
        public static readonly Dictionary<BlockType, (bool, bool)> MIRROR = new Dictionary<BlockType, (bool, bool)>()
        {
            { BlockType.NONE, (false, false) },
            { BlockType.BED, (true, true) },
            { BlockType.CURTAIN_H, (false, false) },
            { BlockType.CURTAIN_V, (false, false) },
            { BlockType.FLAG, (false, false) },
            { BlockType.RUG_BIG, (false, false) },
            { BlockType.RUG_SMALL, (false, false) },
            { BlockType.SAIL, (true, false) },
            { BlockType.POSTER_H_16_9, (false, false) },
            { BlockType.POSTER_V_9_16, (false, false) },
            { BlockType.POSTER_H_5_3, (false, false) },
            { BlockType.POSTER_V_3_5, (false, false) },
            { BlockType.POSTER_H_4_3, (false, false) },
            { BlockType.POSTER_V_3_4, (false, false) },
            { BlockType.POSTER_H_3_2, (false, false) },
            { BlockType.POSTER_V_2_3, (false, false) },
            { BlockType.POSTER_H_2_1, (false, false) },
            { BlockType.POSTER_V_1_2, (false, false) },
            { BlockType.POSTER_1_1, (false, false) },
        };

        public static readonly Dictionary<BlockType, SplitImageData[]> SPLIT_IMAGES = new Dictionary<BlockType, SplitImageData[]>()
        {
            {
                BlockType.CURTAIN_H,
                new SplitImageData[] {
                    new SplitImageData((0, 0), (307, 706), (3, 505), Rotation.LEFT),
                    new SplitImageData((307, 0), (306, 706), (715, 106), Rotation.FLIP),
                }
            },
        };

        public static readonly Dictionary<BlockType, PosterData> POSTER_DATA = new Dictionary<BlockType, PosterData>()
        {
            { BlockType.POSTER_H_16_9, new PosterData("16:9", 1920, 1080, 2f, -0.036f) },
            { BlockType.POSTER_V_9_16, new PosterData("9:16", 1080, 1920, 1.125f, 0.4f) },
            { BlockType.POSTER_H_5_3, new PosterData("5:3", 1800, 1080, 1.5f, -0.036f) },
            { BlockType.POSTER_V_3_5, new PosterData("3:5", 1080, 1800, 1.125f, 0f) },
            { BlockType.POSTER_H_4_3, new PosterData("4:3", 1440, 1080, 1.5f, -0.036f) },
            { BlockType.POSTER_V_3_4, new PosterData("3:4", 1080, 1440, 1.125f, 0.15f) },
            { BlockType.POSTER_H_3_2, new PosterData("3:2", 1620, 1080, 1.6875f, -0.036f) },
            { BlockType.POSTER_V_2_3, new PosterData("2:3", 1080, 1620, 1.125f, 0.245f) },
            { BlockType.POSTER_H_2_1, new PosterData("2:1", 2160, 1080, 2.25f, 0f) },
            { BlockType.POSTER_V_1_2, new PosterData("1:2", 1080, 2160, 1.125f, -0.036f) },
            { BlockType.POSTER_1_1, new PosterData("1:1", 1080, 1080, 1.125f, -0.036f) },
        };
        public static readonly Dictionary<BlockType, string[]> POSTER_STRINGS = new Dictionary<BlockType, string[]>()
        {
            {
                BlockType.POSTER_H_16_9, new string[]
                {
                    "destiny_CustomPoster_h_16_9",
                    "Custom Poster (16:9)",
                    "CustomPosters",
                    "Custom Posters",
                    "A customizable poster.",
                }
            },
            {
                BlockType.POSTER_V_9_16, new string[]
                {
                    "destiny_CustomPoster_v_9_16",
                    "Custom Poster (9:16)",
                    "CustomPosters",
                    "Custom Posters",
                    "A customizable poster.",
                }
            },
            {
                BlockType.POSTER_H_4_3, new string[]
                {
                    "destiny_CustomPoster_h_4_3",
                    "Custom Poster (4:3)",
                    "CustomPosters",
                    "Custom Posters",
                    "A customizable poster.",
                }
            },
            {
                BlockType.POSTER_V_3_4, new string[]
                {
                    "destiny_CustomPoster_v_3_4",
                    "Custom Poster (3:4)",
                    "CustomPosters",
                    "Custom Posters",
                    "A customizable poster.",
                }
            },
            {
                BlockType.POSTER_H_3_2, new string[]
                {
                    "destiny_CustomPoster_h_3_2",
                    "Custom Poster (3:2)",
                    "CustomPosters",
                    "Custom Posters",
                    "A customizable poster.",
                }
            },
            {
                BlockType.POSTER_V_2_3, new string[]
                {
                    "destiny_CustomPoster_v_2_3",
                    "Custom Poster (2:3)",
                    "CustomPosters",
                    "Custom Posters",
                    "A customizable poster.",
                }
            },
        };

        public static Dictionary<int, BlockType> ID_TO_BLOCKTYPE = new Dictionary<int, BlockType>();

        public static CustomBlocks instance;
        public static JsonModInfo modInfo;
        public static Shader shader;
        public static Camera iconRenderer;

        private static GameObject menu;
        private static GameObject menuAsset;
        private static GameObject camera;
        private static GameObject cameraAsset;
        private static CustomBlocksMenu cfMenu;

        // Dictionaries for storing the data for new materials.
        public Dictionary<BlockType, Texture2D> baseTextures;
        public Dictionary<BlockType, Texture2D> baseNormals;
        public Dictionary<BlockType, Texture2D> basePaints;
        // Dictionary for the default materials.
        public Dictionary<BlockType, Material> defaultMaterials;
        // Dictionary for the default sprites.
        public Dictionary<BlockType, Sprite> defaultSprites;

        private List<Item_Base> customItems;
        private Harmony harmony;
        private Transform prefabHolder;
        private AssetBundle bundle;
        // Store the first poster here so we can do skins.
        private Item_Base posterBase;

        public static bool IgnoreFlagMessages
        {
            get
            {
                return CustomBlocks.instance.ExtraSettingsAPI_GetCheckboxState("ignoreFlagMessages");
            }
        }

        public static KeyCode InteractKey
        {
            get
            {
                return CustomBlocks.instance.ExtraSettingsAPI_GetKeybindMain("interactKey");
            }
        }

        public static bool EditorEnabled
        {
            get
            {
                return CustomBlocks.instance.ExtraSettingsAPI_GetCheckboxState("editorEnabled");
            }
        }

        public static bool PreventChanges
        {
            get
            {
                return CustomBlocks.instance.ExtraSettingsAPI_GetCheckboxState("preventChanges");
            }
        }

        public static bool UseMipMaps
        {
            get
            {
                return CustomBlocks.instance.ExtraSettingsAPI_GetCheckboxState("useMipMaps");
            }
        }

        public IEnumerator Start()
        {
            CustomBlocks.instance = this;
            this.baseTextures = new Dictionary<BlockType, Texture2D>();
            this.baseNormals = new Dictionary<BlockType, Texture2D>();
            this.basePaints = new Dictionary<BlockType, Texture2D>();
            this.defaultMaterials = new Dictionary<BlockType, Material>();
            this.defaultSprites = new Dictionary<BlockType, Sprite>();
            HNotification notification = HNotify.instance.AddNotification(HNotify.NotificationType.spinning, "Loading CustomBlocks...");

            // Now, load the menu from the asset bundle.
            var bundleLoadRequest = AssetBundle.LoadFromMemoryAsync(GetEmbeddedFileBytes("general_assets/customblocks.assets"));
            yield return bundleLoadRequest;
            this.bundle = bundleLoadRequest.assetBundle;

            var request = this.bundle.LoadAssetAsync<GameObject>("CustomBlocksMenu");
            yield return request;

            try
            {
                CustomBlocks.menuAsset = request.asset as GameObject;
                CustomBlocks.menu = Instantiate(CustomBlocks.menuAsset, this.transform);
                CustomBlocks.cfMenu = CustomBlocks.menu.AddComponent<CustomBlocksMenu>();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                notification.Close();
                yield break;
            }

            request = this.bundle.LoadAssetAsync<GameObject>("IconRenderer");
            yield return request;

            try
            {
                CustomBlocks.cameraAsset = request.asset as GameObject;
                CustomBlocks.camera = Instantiate(CustomBlocks.cameraAsset, this.transform);
                CustomBlocks.camera.SetActiveSafe(false);
                CustomBlocks.iconRenderer = CustomBlocks.camera.GetComponentInChildren<Camera>(true);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                notification.Close();
                yield break;
            }

            yield return new WaitForEndOfFrame();

            foreach (BlockType bt in IDS.Keys)
            {
                ID_TO_BLOCKTYPE[IDS[bt].Item2] = bt;
            }

            try
            {
                CustomBlocks.modInfo = modlistEntry.jsonmodinfo;
                this.harmony = new Harmony("com.destruction.CustomBlocks");
                this.harmony.PatchAll(Assembly.GetExecutingAssembly());
                this.prefabHolder = new GameObject("prefabHolder").transform;
                this.prefabHolder.gameObject.SetActive(false);
                DontDestroyOnLoad(this.prefabHolder.gameObject);

                // First thing is first, let's fetch our shader from the game.
                CustomBlocks.shader = Shader.Find(" BlockPaint");

                // Second, setup most of the materials using the basic methods.
                this.SetupBasicBlockData(BlockType.BED);
                this.SetupBasicBlockData(BlockType.CURTAIN_V);
                this.SetupBasicBlockData(BlockType.CURTAIN_H);
                this.SetupBasicBlockData(BlockType.FLAG);
                this.SetupBasicBlockData(BlockType.RUG_BIG);
                this.SetupBasicBlockData(BlockType.RUG_SMALL);
                this.SetupBasicBlockData(BlockType.SAIL);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                notification.Close();
                yield break;
            }

            yield return new WaitForEndOfFrame();

            try
            {
                // Create the custom block item bases.
                this.customItems = new List<Item_Base>()
                {
                    this.CreateCustomBedItem(),
                    this.CreateCustomCurtainHItem(),
                    this.CreateCustomCurtainVItem(),
                    this.CreateCustomFlagItem(),
                    this.CreateCustomRugBigItem(),
                    this.CreateCustomRugSmallItem(),
                    this.CreateCustomSailItem(),
                };

            }
            catch (Exception e)
            {
                Debug.LogError(e);
                notification.Close();
                yield break;
            }

            yield return new WaitForEndOfFrame();

            try
            {
                this.SetupPosters();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                notification.Close();
                yield break;
            }

            yield return new WaitForEndOfFrame();

            try
            {
                // Register the items.
                Array.ForEach(this.customItems.ToArray(), x => RAPI.RegisterItem(x));
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                notification.Close();
                yield break;
            }

            CustomBlocks.Log("Mod has been loaded.");
            notification?.Close();
        }

        public void OnModUnload()
        {
            this.harmony?.UnpatchAll("com.destruction.CustomBlocks");
            this.bundle?.Unload(true);
            Destroy(this.prefabHolder?.gameObject);
            if (CustomBlocks.menu)
            {
                Destroy(CustomBlocks.menu);
            }
            if (CustomBlocks.menuAsset)
            {
                Destroy(CustomBlocks.menuAsset);
            }
            if (CustomBlocks.camera)
            {
                Destroy(CustomBlocks.menu);
            }
            if (CustomBlocks.cameraAsset)
            {
                Destroy(CustomBlocks.menuAsset);
            }
            CustomBlocks.Log("Mod has been unloaded.");
        }

        public static void Log(object message)
        {
            Debug.Log($"[{modInfo.name}]: {message}");
        }

        public static void DebugLog(object message)
        {
            Debug.Log($"[{modInfo.name}][DEBUG]: {message}");
        }

        public static void OpenCustomBlocksMenu(ICustomBlock cf)
        {
            CustomBlocks.cfMenu.ShowMenu(cf);
        }

        /*
         * Performs serveral tasks, including the textures, material, and the
         * sprite.
         */
        private void SetupBasicBlockData(BlockType bt)
        {
            string imgDir = FOLDER_NAMES[bt];
            this.AddBaseTextures(bt, this.GetOriginalMaterial(IDS[bt].Item1), $"{imgDir}/transparent.png", $"{imgDir}/normal.png", $"{imgDir}/transparent.png");
            this.defaultMaterials[bt] = CustomBlocks.CreateMaterialFromImageData(GetEmbeddedFileBytes($"{imgDir}/default.png").SanitizeImage(bt), bt);
            this.defaultSprites[bt] = CustomBlocks.CreateSpriteFromBytes(GetEmbeddedFileBytes($"{imgDir}/default.png").SanitizeImage(bt), bt);
        }

        private void SetupPosters()
        {
            // This is the recipe for the posters.
            var recipe = new CostMultiple[]
            {
                new CostMultiple(new[] { ItemManager.GetItemByIndex(25) }, 5),
                /*new CostMultiple(new[]
                                 {
                                    ItemManager.GetItemByIndex(104),
                                    ItemManager.GetItemByIndex(105),
                                    ItemManager.GetItemByIndex(106),
                                    ItemManager.GetItemByIndex(107),
                                    ItemManager.GetItemByIndex(108),
                                 }, 5),*/
            };

            List<Item_Base> posters = new List<Item_Base>();

            foreach (BlockType bt in POSTER_DATA.Keys)
            {
                PosterData pd = POSTER_DATA[bt];
                string imgDir = FOLDER_NAMES[bt];
                this.AddBaseTextures(bt, pd.CreateMaterial(), $"{imgDir}/transparent.png", $"{imgDir}/normal.png", $"{imgDir}/transparent.png");
                this.defaultMaterials[bt] = CustomBlocks.CreateMaterialFromImageData(GetEmbeddedFileBytes($"{imgDir}/default.png").SanitizeImage(bt), bt);
                this.defaultSprites[bt] = CustomBlocks.CreateSpriteFromBytes(GetEmbeddedFileBytes($"{imgDir}/default.png").SanitizeImage(bt), bt);
                Item_Base poster = this.CreateGenericCustomPoster<Block_CustomPoster, CustomBlock_Network>(IDS[bt].Item1, IDS[bt].Item2, POSTER_STRINGS[bt], pd, recipe, CraftingCategory.Decorations);
                this.customItems.Add(poster);
                if (!this.posterBase)
                {
                    this.posterBase = poster;
                }
                posters.Add(poster);
                poster.settings_recipe.baseSkinItem = this.posterBase;
            }

            Traverse.Create(this.posterBase).Field("skins").SetValue(posters.ToArray());
        }

        /*
         * Creates the base texture, normal, and paint Texture2Ds for a
         * specified block type using original material and the internal files
         * specified. Then, adds them to the dictionary.
         */
        private void AddBaseTextures(BlockType bt, Material originalMat, string texture, string normal, string paint)
        {
            Texture2D insertTex = new Texture2D(SIZES[bt].Item1, SIZES[bt].Item2);
            insertTex.wrapMode = TextureWrapMode.Clamp;

            Texture2D[] add = new Texture2D[3];

            // Get the diffuse portion of the texture.
            add[0] = (originalMat.GetTexture("_Diffuse") as Texture2D).CreateReadable();
            ImageConversion.LoadImage(insertTex, GetEmbeddedFileBytes(texture));
            CustomBlocks.PlaceImageInTexture(add[0], insertTex, bt);

            // Now we need to generate our normal map.
            add[1] = (originalMat.GetTexture("_Normal") as Texture2D).CreateReadable();
            ImageConversion.LoadImage(insertTex, GetEmbeddedFileBytes(normal));
            CustomBlocks.PlaceImageInTexture(add[1], insertTex, bt);

            // Now we need to generate our paint map.
            add[2] = (originalMat.GetTexture("_MetallicRPaintMaskGSmoothnessA") as Texture2D).CreateReadable();
            ImageConversion.LoadImage(insertTex, GetEmbeddedFileBytes(paint));
            CustomBlocks.PlaceImageInTexture(add[2], insertTex, bt);

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
                CustomBlocks.DebugLog("Could not create material from image data: data failed to convert.");
                return null;
            }

            // Create the material and put the flag inside of it.
            Material mat = CustomBlocks.instance.PrepareMaterial(bt);
            CustomBlocks.PlaceImageInMaterial(mat, tex, bt);

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
                return CustomBlocks.instance.defaultSprites[bt];
            }

            Vector2 pivot = new Vector2(0.5f, 0.5f);
            Rect rect = new Rect(0, 0, 1524, 1024);

            Texture2D container = new Texture2D(1524, 1024);
            Texture2D imageTex = data.ToTexture2D(SIZES[bt].Item1, SIZES[bt].Item2);
            (int, int, int, int) newSize = CustomBlocks.ScaleToFit(SIZES[bt].Item1, SIZES[bt].Item2, 1524, 1024);
            container.Edit(imageTex, newSize.Item1, newSize.Item2, newSize.Item3, newSize.Item4);

            return Sprite.Create(container, rect, pivot);
        }

        /*
         * Prepares a new material for flags to be placed into.
         */
        private Material PrepareMaterial(BlockType bt)
        {
            // Load the textures.
            Texture2D diffuse = new Texture2D(this.baseTextures[bt].width, this.baseTextures[bt].height, this.baseTextures[bt].format, false);
            Texture2D paint = new Texture2D(this.basePaints[bt].width, this.basePaints[bt].height, this.basePaints[bt].format, false);
            Texture2D normal = new Texture2D(this.baseNormals[bt].width, this.baseNormals[bt].height, this.baseNormals[bt].format, false);

            Graphics.CopyTexture(this.baseTextures[bt], diffuse);
            Graphics.CopyTexture(this.baseNormals[bt], normal);
            Graphics.CopyTexture(this.basePaints[bt], paint);

            // Create and setup our material.
            Material mat = new Material(CustomBlocks.shader);
            mat.SetTexture("_Diffuse", diffuse);
            mat.SetTexture("_MetallicRPaintMaskGSmoothnessA", paint);
            mat.SetTexture("_Normal", normal);

            return mat;
        }

        /*
         * Gets the original material from the item with the specified ID,
         * assuming that any of the MeshRenderers will have the correct
         * material, and that any of the DPS types is acceptable.
         */
        private Material GetOriginalMaterial(int id)
        {
            return ItemManager.GetItemByIndex(id).settings_buildable.GetBlockPrefabs()[0].GetComponentInChildren<MeshRenderer>().material;
        }

        /*
         * A generic method for making new custom blocks. For blocks that can
         * only be placed on one type of surface. The data strings are used for
         * setting the various strings, and should be provided in the following
         * order:
         *  * Unique Name
         *  * Display Name
         *  * Crafting Sub
         *  * Crafting Sub Name
         *  * Description
         */
        private Item_Base CreateGenericCustomItem<BlockClass, NetworkClass>(int originalID, int newID, string[] data, Vector3 bbSize, Vector3 bbCenter, CostMultiple[] recipe, CraftingCategory craftCat) where BlockClass : Block, ICustomBlock where NetworkClass : MonoBehaviour_ID_Network
        {
            // Create a clone of the regular flag.
            Item_Base originalItem = ItemManager.GetItemByIndex(originalID);
            Item_Base customBlock = ScriptableObject.CreateInstance<Item_Base>();
            customBlock.Initialize(newID, data[0], 1);
            customBlock.settings_buildable = originalItem.settings_buildable.Clone();
            customBlock.settings_consumeable = originalItem.settings_consumeable.Clone();
            customBlock.settings_cookable = originalItem.settings_cookable.Clone();
            customBlock.settings_equipment = originalItem.settings_equipment.Clone();
            customBlock.settings_Inventory = originalItem.settings_Inventory.Clone();
            customBlock.settings_recipe = originalItem.settings_recipe.Clone();
            customBlock.settings_usable = originalItem.settings_usable.Clone();

            Block[] blocks = customBlock.settings_buildable.GetBlockPrefabs().Clone() as Block[];

            // Set the block to not be paintable.
            Traverse.Create(customBlock.settings_buildable).Field("primaryPaintAxis").SetValue(Axis.None);

            // Setup the recipe.
            customBlock.SetRecipe(recipe, craftCat, 1, false, data[2], 1);
            var trav = Traverse.Create(customBlock.settings_recipe);
            trav.Field("_hiddenInResearchTable").SetValue(false);
            trav.Field("skins").SetValue(new Item_Base[0]);
            trav.Field("baseSkinItem").SetValue(null);

            // Set the display stuff.
            customBlock.settings_Inventory.DisplayName = data[1];
            customBlock.settings_Inventory.Description = data[4];

            // Localization stuff.
            customBlock.settings_Inventory.LocalizationTerm = $"Item/{data[0]}";
            var language = new LanguageSourceData()
            {
                mDictionary = new Dictionary<string, TermData>
                {
                    [$"Item/{data[0]}"] = new TermData() { Languages = new[] { $"{data[1]}@{data[4]}" } },
                    [$"CraftingSub/{data[2]}"] = new TermData() { Languages = new[] { data[3] } }
                },
                mLanguages = new List<LanguageData> { new LanguageData() { Code = "en", Name = "English" } }
            };
            LocalizationManager.Sources.Add(language);
            Traverse.Create(typeof(LocalizationManager)).Field("OnLocalizeEvent").GetValue<LocalizationManager.OnLocalizeCallback>().Invoke();


            // Now, we need to replace Block with BlockType;
            for (int i = 0; i < blocks.Length; ++i)
            {
                if (blocks[i])
                {
                    // Based on some of Aidan's code.
                    var blockPrefab = Instantiate(blocks[i], this.prefabHolder, false);
                    blockPrefab.name = $"Block_CustomFlag_{i}";
                    var cb = blockPrefab.gameObject.AddComponent<BlockClass>();
                    cb.CopyFieldsOf(blockPrefab);
                    cb.ReplaceValues(blockPrefab, cb);
                    blockPrefab.ReplaceValues(originalItem, customBlock);
                    blocks[i] = cb;
                    DestroyImmediate(blockPrefab);
                    if (bbSize != Vector3.zero)
                    {
                        cb.gameObject.AddComponent<RaycastInteractable>();
                        var c = cb.gameObject.AddComponent<BoxCollider>();
                        c.size = bbSize;
                        c.center = bbCenter;

                        c.isTrigger = true;
                        c.enabled = false;
                        c.gameObject.layer = 10;
                        cb.onoffColliders = cb.onoffColliders.Extend(c);
                    }

                    if (cb.networkedBehaviour != null)
                    {
                        var originalNb = cb.GetComponent<MonoBehaviour_Network>();
                        var nb = cb.gameObject.AddComponent<NetworkClass>();
                        nb.CopyFieldsOf(originalNb);
                        nb.ReplaceValues(originalNb, nb);
                        DestroyImmediate(originalNb);
                    }
                    else if (cb.networkedIDBehaviour != null)
                    {
                        var originalNb = cb.GetComponent<MonoBehaviour_ID_Network>();
                        var nb = cb.gameObject.AddComponent<NetworkClass>();
                        nb.CopyFieldsOf(originalNb);
                        nb.ReplaceValues(originalNb, nb);
                        DestroyImmediate(originalNb);
                    }
                    else
                    {
                        var nb = cb.gameObject.AddComponent<NetworkClass>();
                        if (nb is MonoBehaviour_Network)
                        {
                            cb.networkedBehaviour = nb as MonoBehaviour_Network;
                            cb.networkType = NetworkType.NetworkBehaviour;
                        }
                        else
                        {
                            cb.networkedIDBehaviour = nb as MonoBehaviour_ID_Network;
                            cb.networkType = NetworkType.NetworkIDBehaviour;
                        }
                    }

                    // Use traverse to get around generic BS.
                    Traverse.Create(cb).Method("set_ImageData", new byte[0]).GetValue();
                }
            }

            Traverse.Create(customBlock.settings_buildable).Field("blockPrefabs").SetValue(blocks);

            foreach (var q in Resources.FindObjectsOfTypeAll<SO_BlockQuadType>())
                if (q.AcceptsBlock(originalItem))
                    Traverse.Create(q).Field("acceptableBlockTypes").GetValue<List<Item_Base>>().Add(customBlock);

            return customBlock;
        }

        private Item_Base CreateGenericCustomItemInteractable<BlockClass, NetworkClass>(int originalID, int newID, string[] data, CostMultiple[] recipe, CraftingCategory craftCat) where BlockClass : Block_Interactable, ICustomBlock where NetworkClass : Placeable_Interactable
        {
            // Create a clone of the regular flag.
            Item_Base originalItem = ItemManager.GetItemByIndex(originalID);
            Item_Base customBlock = ScriptableObject.CreateInstance<Item_Base>();
            customBlock.Initialize(newID, data[0], 1);
            customBlock.settings_buildable = originalItem.settings_buildable.Clone();
            customBlock.settings_consumeable = originalItem.settings_consumeable.Clone();
            customBlock.settings_cookable = originalItem.settings_cookable.Clone();
            customBlock.settings_equipment = originalItem.settings_equipment.Clone();
            customBlock.settings_Inventory = originalItem.settings_Inventory.Clone();
            customBlock.settings_recipe = originalItem.settings_recipe.Clone();
            customBlock.settings_usable = originalItem.settings_usable.Clone();

            Block[] blocks = customBlock.settings_buildable.GetBlockPrefabs().Clone() as Block[];

            // Set the block to not be paintable.
            Traverse.Create(customBlock.settings_buildable).Field("primaryPaintAxis").SetValue(Axis.None);

            // Setup the recipe.
            customBlock.SetRecipe(recipe, craftCat, 1, false, data[2], 1);
            var trav = Traverse.Create(customBlock.settings_recipe);
            trav.Field("_hiddenInResearchTable").SetValue(false);
            trav.Field("skins").SetValue(new Item_Base[0]);
            trav.Field("baseSkinItem").SetValue(null);

            // Set the display stuff.
            customBlock.settings_Inventory.DisplayName = data[1];
            customBlock.settings_Inventory.Description = data[4];

            // Localization stuff.
            customBlock.settings_Inventory.LocalizationTerm = $"Item/{data[0]}";
            var language = new LanguageSourceData()
            {
                mDictionary = new Dictionary<string, TermData>
                {
                    [$"Item/{data[0]}"] = new TermData() { Languages = new[] { $"{data[1]}@{data[4]}" } },
                    [$"CraftingSub/{data[2]}"] = new TermData() { Languages = new[] { data[3] } }
                },
                mLanguages = new List<LanguageData> { new LanguageData() { Code = "en", Name = "English" } }
            };
            LocalizationManager.Sources.Add(language);
            Traverse.Create(typeof(LocalizationManager)).Field("OnLocalizeEvent").GetValue<LocalizationManager.OnLocalizeCallback>().Invoke();


            // Now, we need to replace Block with BlockType;
            for (int i = 0; i < blocks.Length; ++i)
            {
                if (blocks[i])
                {
                    // Based on some of Aidan's code.
                    var blockPrefab = Instantiate(blocks[i], this.prefabHolder, false);
                    blockPrefab.name = $"Block_CustomFlag_{i}";
                    var cb = blockPrefab.gameObject.AddComponent<BlockClass>();
                    cb.CopyFieldsOf(blockPrefab);
                    cb.ReplaceValues(blockPrefab, cb);
                    blockPrefab.ReplaceValues(originalItem, customBlock);
                    blocks[i] = cb;
                    DestroyImmediate(blockPrefab);

                    var originalNb = cb.gameObject.GetComponent<Placeable_Interactable>();
                    var nb = cb.gameObject.AddComponent<NetworkClass>();
                    nb.CopyFieldsOf(originalNb);
                    nb.ReplaceValues(originalNb, nb);
                    DestroyImmediate(originalNb);

                    cb.SetImageData(new byte[0]);
                }
            }

            Traverse.Create(customBlock.settings_buildable).Field("blockPrefabs").SetValue(blocks);

            foreach (var q in Resources.FindObjectsOfTypeAll<SO_BlockQuadType>())
                if (q.AcceptsBlock(originalItem))
                    Traverse.Create(q).Field("acceptableBlockTypes").GetValue<List<Item_Base>>().Add(customBlock);

            return customBlock;
        }

        private Item_Base CreateGenericCustomPoster<BlockClass, NetworkClass>(int originalID, int newID, string[] data, PosterData pd, CostMultiple[] recipe, CraftingCategory craftCat) where BlockClass : Block_CustomPoster where NetworkClass : MonoBehaviour_Network
        {
            // Create a clone of the regular flag.
            Item_Base originalItem = ItemManager.GetItemByIndex(originalID);
            Item_Base customBlock = ScriptableObject.CreateInstance<Item_Base>();
            customBlock.Initialize(newID, data[0], 1);
            customBlock.settings_buildable = originalItem.settings_buildable.Clone();
            customBlock.settings_consumeable = originalItem.settings_consumeable.Clone();
            customBlock.settings_cookable = originalItem.settings_cookable.Clone();
            customBlock.settings_equipment = originalItem.settings_equipment.Clone();
            customBlock.settings_Inventory = originalItem.settings_Inventory.Clone();
            customBlock.settings_recipe = originalItem.settings_recipe.Clone();
            customBlock.settings_usable = originalItem.settings_usable.Clone();

            Block[] blocks = customBlock.settings_buildable.GetBlockPrefabs().Clone() as Block[];

            // Set the block to not be paintable.
            Traverse.Create(customBlock.settings_buildable).Field("primaryPaintAxis").SetValue(Axis.None);

            // Setup the recipe.
            customBlock.SetRecipe(recipe, craftCat, 1, false, data[2], 1);
            var trav = Traverse.Create(customBlock.settings_recipe);
            trav.Field("_hiddenInResearchTable").SetValue(false);
            trav.Field("skins").SetValue(new Item_Base[0]);
            trav.Field("baseSkinItem").SetValue(null);

            // Set the display stuff.
            customBlock.settings_Inventory.DisplayName = data[1];
            customBlock.settings_Inventory.Description = data[4];

            // Set the icon.
            customBlock.settings_Inventory.Sprite = pd.CreateIcon();

            // Localization stuff.
            customBlock.settings_Inventory.LocalizationTerm = $"Item/{data[0]}";
            var language = new LanguageSourceData()
            {
                mDictionary = new Dictionary<string, TermData>
                {
                    [$"Item/{data[0]}"] = new TermData() { Languages = new[] { $"{data[1]}@{data[4]}" } },
                    [$"CraftingSub/{data[2]}"] = new TermData() { Languages = new[] { data[3] } }
                },
                mLanguages = new List<LanguageData> { new LanguageData() { Code = "en", Name = "English" } }
            };
            LocalizationManager.Sources.Add(language);
            Traverse.Create(typeof(LocalizationManager)).Field("OnLocalizeEvent").GetValue<LocalizationManager.OnLocalizeCallback>().Invoke();


            // Now, we need to replace Block with BlockType;
            for (int i = 0; i < blocks.Length; ++i)
            {
                if (blocks[i])
                {
                    // Based on some of Aidan's code.
                    var blockPrefab = Instantiate(blocks[i], this.prefabHolder, false);
                    blockPrefab.name = $"Block_CustomFlag_{i}";
                    var cb = blockPrefab.gameObject.AddComponent<BlockClass>();
                    cb.CopyFieldsOf(blockPrefab);
                    cb.ReplaceValues(blockPrefab, cb);
                    blockPrefab.ReplaceValues(originalItem, customBlock);
                    blocks[i] = cb;
                    DestroyImmediate(blockPrefab);
                    DestroyImmediate(cb.GetComponentsInChildren<Component>(true).First(x => x.gameObject.name.Contains("painting")).gameObject);

                    cb.transform.localScale = Vector3.one;

                    var c = cb.GetComponentInChildren<BoxCollider>();
                    pd.AdjustBoxCollider(c);
                    var raycast = c.gameObject.AddComponent<RaycastInteractable>();

                    var nb = cb.gameObject.AddComponent<NetworkClass>();
                    cb.networkedBehaviour = nb;
                    cb.networkType = NetworkType.NetworkBehaviour;

                    MeshFilter filter = cb.GetComponentInChildren<MeshFilter>(true);

                    filter.mesh = pd.CreateMesh();

                    cb.ImageData = new byte[0];
                }
            }

            Traverse.Create(customBlock.settings_buildable).Field("blockPrefabs").SetValue(blocks);

            foreach (var q in Resources.FindObjectsOfTypeAll<SO_BlockQuadType>())
                if (q.AcceptsBlock(originalItem))
                    Traverse.Create(q).Field("acceptableBlockTypes").GetValue<List<Item_Base>>().Add(customBlock);

            return customBlock;
        }

        private Item_Base CreateCustomBedItem()
        {
            var recipe = new[] {
                new CostMultiple(new[] { ItemManager.GetItemByIndex(21) }, 10),
                new CostMultiple(new[] { ItemManager.GetItemByIndex(25) }, 20),
                new CostMultiple(new[] { ItemManager.GetItemByIndex(95) }, 14),
                new CostMultiple(new[] { ItemManager.GetItemByIndex(20) }, 9)
            };
            var data = new[] {
                "destiny_CustomBed",
                "Custom Bed",
                "CustomBed",
                "Custom Bed",
                "A customizable bed."
            };
            return this.CreateGenericCustomItem<Block_CustomBed, CustomBlock_Network>(IDS[BlockType.BED].Item1, IDS[BlockType.BED].Item2, data, Vector3.zero, Vector3.zero, recipe, CraftingCategory.Other);
        }

        private Item_Base CreateCustomCurtainHItem()
        {
            var recipe = new[] {
                new CostMultiple(new[] { ItemManager.GetItemByIndex(25) }, 8),
                new CostMultiple(new[] { ItemManager.GetItemByIndex(22) }, 2),
            };
            var data = new[] {
                "destiny_CustomCurtainH",
                "Custom Curtain (Horizontal)",
                "CustomCurtains",
                "Custom Curtains",
                "A customizable curtain."
            };
            return this.CreateGenericCustomItemInteractable<Block_CustomCurtainH, CustomInteractableOC_Network>(IDS[BlockType.CURTAIN_H].Item1, IDS[BlockType.CURTAIN_H].Item2, data, recipe, CraftingCategory.Decorations);
        }

        private Item_Base CreateCustomCurtainVItem()
        {
            var recipe = new[] {
                new CostMultiple(new[] { ItemManager.GetItemByIndex(25) }, 8),
                new CostMultiple(new[] { ItemManager.GetItemByIndex(22) }, 2),
            };
            var data = new[] {
                "destiny_CustomCurtainV",
                "Custom Curtain (Vertical)",
                "CustomCurtains",
                "Custom Curtains",
                "A customizable curtain."
            };
            return this.CreateGenericCustomItemInteractable<Block_CustomCurtainV, CustomInteractableOC_Network>(IDS[BlockType.CURTAIN_V].Item1, IDS[BlockType.CURTAIN_V].Item2, data, recipe, CraftingCategory.Decorations);
        }

        /*
         * Finds the base flag we will be using and creates a new item for our
         * custom flag.
         */
        private Item_Base CreateCustomFlagItem()
        {
            // Create a clone of the regular flag.
            Item_Base originalItem = ItemManager.GetItemByIndex(IDS[BlockType.FLAG].Item1);
            Item_Base customFlag = ScriptableObject.CreateInstance<Item_Base>();
            customFlag.Initialize(IDS[BlockType.FLAG].Item2, "destiny_CustomFlag", 1);
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

            // Set the display stuff.
            customFlag.settings_Inventory.DisplayName = "Custom Flag";
            customFlag.settings_Inventory.Description = "A customizable flag.";

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
                    c.enabled = false;
                    c.gameObject.layer = 10;
                    cf.onoffColliders = cf.onoffColliders.Extend(c);
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

        private Item_Base CreateCustomRugBigItem()
        {
            var recipe = new[] {
                new CostMultiple(new[] { ItemManager.GetItemByIndex(25) }, 4),
                new CostMultiple(new[] { ItemManager.GetItemByIndex(22) }, 2),
            };
            var data = new[] {
                "destiny_CustomRugBig",
                "Custom Rug (Big)",
                "CustomRugs",
                "Custom Rugs",
                "A customizable rug."
            };
            return this.CreateGenericCustomItem<Block_CustomRugBig, CustomBlock_Network>(IDS[BlockType.RUG_BIG].Item1, IDS[BlockType.RUG_BIG].Item2, data, new Vector3(1.4797308f, 0.14399316f, 2.736644f), new Vector3(0, 0.005513929f, 0), recipe, CraftingCategory.Decorations);
        }

        private Item_Base CreateCustomRugSmallItem()
        {
            var recipe = new[] {
                new CostMultiple(new[] { ItemManager.GetItemByIndex(25) }, 2),
                new CostMultiple(new[] { ItemManager.GetItemByIndex(22) }, 1),
            };
            var data = new[] {
                "destiny_CustomRugSmall",
                "Custom Rug (Small)",
                "CustomRugs",
                "Custom Rugs",
                "A customizable rug."
            };
            return this.CreateGenericCustomItem<Block_CustomRugSmall, CustomBlock_Network>(IDS[BlockType.RUG_SMALL].Item1, IDS[BlockType.RUG_SMALL].Item2, data, new Vector3(0.8902238f, 0.12493733f, 1.32783f), new Vector3(0, 0.005513929f, 0), recipe, CraftingCategory.Decorations);
        }

        /*
         * Finds the base sail item and uses it to create a new custom sail
         * item.
         */
        private Item_Base CreateCustomSailItem()
        {
            // Create a clone of the regular flag.
            Item_Base originalItem = ItemManager.GetItemByIndex(IDS[BlockType.SAIL].Item1);
            Item_Base customSail = ScriptableObject.CreateInstance<Item_Base>();
            customSail.Initialize(IDS[BlockType.SAIL].Item2, "destiny_CustomSail", 1);
            customSail.settings_buildable = originalItem.settings_buildable.Clone();
            customSail.settings_consumeable = originalItem.settings_consumeable.Clone();
            customSail.settings_cookable = originalItem.settings_cookable.Clone();
            customSail.settings_equipment = originalItem.settings_equipment.Clone();
            customSail.settings_Inventory = originalItem.settings_Inventory.Clone();
            customSail.settings_recipe = originalItem.settings_recipe.Clone();
            customSail.settings_usable = originalItem.settings_usable.Clone();

            Block[] blocks = customSail.settings_buildable.GetBlockPrefabs().Clone() as Block[];

            // Set the display stuff.
            customSail.settings_Inventory.DisplayName = "Custom Sail";
            customSail.settings_Inventory.Description = "A customizable sail.";

            // Setup the recipe.
            customSail.SetRecipe(new[]
                {
                    new CostMultiple(new[] { ItemManager.GetItemByIndex(21) }, 10),
                    new CostMultiple(new[] { ItemManager.GetItemByIndex(25) }, 20),
                    new CostMultiple(new[] { ItemManager.GetItemByIndex(23) }, 3)
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

            // Now, we need to replace Block with Block_CustomSail.
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
                    var originalNb = cs.GetComponent<Sail>();
                    var sail = cs.gameObject.AddComponent<CustomSail_Network>();
                    sail.CopyFieldsOf(originalNb);
                    sail.ReplaceValues(originalNb, sail);
                    DestroyImmediate(originalNb);

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
            CustomBlocks.PlaceImageInTexture(dest.GetTexture("_Diffuse") as Texture2D, src, bt);
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
            // A negative position means it's a split image.
            switch(LOCATIONS[bt].Item1)
            {
                case -1:
                    CustomBlocks.PlaceSplitImageInTexture(dest, src, bt);
                    break;
                case -2:
                    PosterData pd = POSTER_DATA[bt];
                    dest.Edit(src, 0, 0, pd.widthPixels, pd.heightPixels, bt, true);
                    break;
                default:
                    dest.Edit(src, LOCATIONS[bt].Item1, LOCATIONS[bt].Item2, SIZES[bt].Item1, SIZES[bt].Item2, bt, true);
                    break;
            }
        }

        /*
         * Special function for determining how to deal with a split image to
         * place it into the texture.
         */
        private static void PlaceSplitImageInTexture(Texture2D dest, Texture2D src, BlockType bt)
        {
            foreach (SplitImageData sid in SPLIT_IMAGES[bt])
            {
                Texture2D slice = src.Cut(sid.srcXY, sid.widthHeight);
                slice.Rotate(sid.rotation);
                int height;
                int width;
                if (sid.rotation == Rotation.LEFT || sid.rotation == Rotation.RIGHT)
                {
                    height = sid.widthHeight.Item1;
                    width = sid.widthHeight.Item2;
                }
                else
                {
                    width = sid.widthHeight.Item1;
                    height = sid.widthHeight.Item2;
                }
                dest.Edit(slice, sid.destXY.Item1, sid.destXY.Item2, width, height, bt, true);
            }
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
        public virtual bool ExtraSettingsAPI_GetCheckboxState(string name) => true;
        public KeyCode ExtraSettingsAPI_GetKeybindMain(string SettingName) => KeyCode.None;
        public void ExtraSettingsAPI_Load()
        {
            this.SwitchMipMapUsage(CustomBlocks.UseMipMaps);
        }

        public void ExtraSettingsAPI_SettingsClose()
        {
            this.SwitchMipMapUsage(CustomBlocks.UseMipMaps);
        }

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
            CustomBlocks.DebugLog(message);
        }

        /*
         * Finds all existing custom blocks and instructs them to switch their
         * textures to the specified mip map state. If the state is true, the
         * blocks will use the game's texture quality setting for the blocks,
         * otherwise they will use full resolution.
         *
         * This exists so that blocks don't need to be manually updated. Manual
         * updates will always check the current texture settings to determine
         * what to actually do.
         */
        public void SwitchMipMapUsage(bool state)
        {
            foreach (Block block in BlockCreator.GetPlacedBlocks())
            {
                // Only do something for custom blocks.
                block.GetComponent<ICustomBlock>()?.SwitchMipMapState(state);
            }
        }
    }
}
