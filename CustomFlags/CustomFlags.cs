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


namespace DestinyCustomFlags
{
    public class CustomFlags : Mod
    {
        //TODO: Raycast combine for interactable blocks.
        // I want to implement all of the following blocks, but some may have
        // difficulties that will need to be resolved. For example, the bed
        // is going to need a custom class that extends the Bed class, cause
        // the Bed class derives from Block.

        private const int BED_ID = 448; // Renderer for sheet flat is 1, occupied is 2.
        private const int CURTAIN_HORIZONTAL_ID = 447;
        private const int CURTAIN_VERTICAL_ID = 446;
        private const int FLAG_ID = 478;
        private const int RUG_BIG_ID = 158;
        private const int RUG_SMALL_ID = 439;
        private const int SAIL_ID = 126;

        // In little endian this would represent the string "De";
        public static readonly int CUSTOM_FLAG_ITEM_ID = 25924;
        public static readonly int CUSTOM_SAIL_ITEM_ID = 25925;
        public static readonly int CUSTOM_BED_ITEM_ID = 25926;
        public static readonly int CUSTOM_CURTAIN_H_ITEM_ID = 25927;
        public static readonly int CUSTOM_CURTAIN_V_ITEM_ID = 25928;
        public static readonly int CUSTOM_RUG_BIG_ITEM_ID = 25929;
        public static readonly int CUSTOM_RUG_SMALL_ITEM_ID = 25930;
        public static readonly int CUSTOM_BLOCK_ID_MIN = CUSTOM_FLAG_ITEM_ID;
        public static readonly int CUSTOM_BLOCK_ID_MAX = CUSTOM_RUG_SMALL_ITEM_ID;
        public static readonly Dictionary<BlockType, (int, int)> LOCATIONS = new Dictionary<BlockType, (int, int)>()
        {
            { BlockType.NONE, (0, 0) },
            { BlockType.BED, (5, 5) },
            { BlockType.CURTAIN_H, (4, 132) },//
            { BlockType.CURTAIN_V, (4, 132) },//
            { BlockType.FLAG, (256, 770) },
            { BlockType.RUG_BIG, (7, 7) },
            { BlockType.RUG_SMALL, (632, 712) },
            { BlockType.SAIL, (3, 132) },
        };
        public static readonly Dictionary<BlockType, (int, int)> SIZES = new Dictionary<BlockType, (int, int)>()
        {
            { BlockType.NONE, (0, 0) },
            { BlockType.BED, (959, 682) },
            { BlockType.CURTAIN_H, (4, 132) },//
            { BlockType.CURTAIN_V, (4, 132) },//
            { BlockType.FLAG, (377, 252) },
            { BlockType.RUG_BIG, (627, 330) },
            { BlockType.RUG_SMALL, (385, 253) },
            { BlockType.SAIL, (794, 674) },
        };
        // Dictionary to tell what axis to mirror images on. Result is a tuple
        // of whether to mirror the x and whether to mirror the y.
        public static readonly Dictionary<BlockType, (bool, bool)> MIRROR = new Dictionary<BlockType, (bool, bool)>()
        {
            { BlockType.NONE, (false, false) },
            { BlockType.BED, (true, true) },
            { BlockType.CURTAIN_H, (false, false) },//
            { BlockType.CURTAIN_V, (false, false) },//
            { BlockType.FLAG, (false, false) },
            { BlockType.RUG_BIG, (false, false) },
            { BlockType.RUG_SMALL, (false, false) },
            { BlockType.SAIL, (true, false) },
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

            // Second, setup most of the materials using the basic methods.
            this.SetupBasicBlockData(BlockType.BED, "bed", this.GetOriginalMaterial(BED_ID));
            this.SetupBasicBlockData(BlockType.CURTAIN_V, "curtain_v", this.GetOriginalMaterial(CURTAIN_VERTICAL_ID));
            this.SetupBasicBlockData(BlockType.FLAG, "flag", this.GetOriginalMaterial(FLAG_ID));
            this.SetupBasicBlockData(BlockType.RUG_BIG, "rug_big", this.GetOriginalMaterial(RUG_BIG_ID));
            this.SetupBasicBlockData(BlockType.RUG_SMALL, "rug_small", this.GetOriginalMaterial(RUG_SMALL_ID));
            this.SetupBasicBlockData(BlockType.SAIL, "sail", this.GetOriginalMaterial(SAIL_ID));

            // Create the custom block item bases.
            this.customItems = new Item_Base[] {
                this.CreateCustomBedItem(),
                //this.CreateCustomCurtainHItem(),
                //this.CreateCustomCurtainVItem(),
                this.CreateCustomFlagItem(),
                this.CreateCustomRugBigItem(),
                this.CreateCustomRugSmallItem(),
                this.CreateCustomSailItem(),
            };

            // Register the items.
            Array.ForEach(this.customItems, x => RAPI.RegisterItem(x));

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

        public static void OpenCustomFlagsMenu(ICustomBlock cf)
        {
            CustomFlags.cfMenu.ShowMenu(cf);
        }

        /*
         * Performs serveral tasks, including the textures, material, and the
         * sprite.
         */
        private void SetupBasicBlockData(BlockType bt, string imgDir, Material originalMat)
        {
            this.AddBaseTextures(bt, originalMat, $"{imgDir}/transparent.png", $"{imgDir}/normal.png", $"{imgDir}/transparent.png");
            this.defaultMaterials[bt] = CustomFlags.CreateMaterialFromImageData(GetEmbeddedFileBytes($"{imgDir}/default.png").SanitizeImage(bt), bt);
            this.defaultSprites[bt] = CustomFlags.CreateSpriteFromBytes(GetEmbeddedFileBytes($"{imgDir}/default.png").SanitizeImage(bt), bt);
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
            Texture2D diffuse = new Texture2D(this.baseTextures[bt].width, this.baseTextures[bt].height, this.baseTextures[bt].format, false);
            Texture2D paint = new Texture2D(this.basePaints[bt].width, this.basePaints[bt].height, this.basePaints[bt].format, false);
            Texture2D normal = new Texture2D(this.baseNormals[bt].width, this.baseNormals[bt].height, this.baseNormals[bt].format, false);

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
        private Item_Base CreateGenericCustomItem<BlockClass, NetworkClass>(int originalID, int newID, string[] data, Vector3 bbSize, Vector3 bbCenter, CostMultiple[] recipe, CraftingCategory craftCat) where BlockClass : Block where NetworkClass : MonoBehaviour_Network
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
            customBlock.settings_Inventory.LocalizationTerm = "Item/destiny_CustomFlag";
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
                    cb.gameObject.AddComponent<RaycastInteractable>();
                    if (bbSize != Vector3.zero)
                    {
                        var c = cb.gameObject.AddComponent<BoxCollider>();
                        c.size = bbSize;
                        c.center = bbCenter;

                        c.isTrigger = true;
                        c.enabled = true;
                        c.gameObject.layer = 10;
                    }

                    cb.networkedBehaviour = cb.gameObject.AddComponent<NetworkClass>();
                    cb.networkType = NetworkType.NetworkBehaviour;

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
            return this.CreateGenericCustomItem<Block_CustomBed, CustomBlock_Network>(BED_ID, CUSTOM_BED_ITEM_ID, data, Vector3.zero, Vector3.zero, recipe, CraftingCategory.Other);
        }

        private Item_Base CreateCustomCurtainHItem()
        {
            var recipe = new[] {
                new CostMultiple(new[] { ItemManager.GetItemByIndex(21) }, 10),
                new CostMultiple(new[] { ItemManager.GetItemByIndex(25) }, 20),
                new CostMultiple(new[] { ItemManager.GetItemByIndex(95) }, 14),
                new CostMultiple(new[] { ItemManager.GetItemByIndex(20) }, 9)
            };
            var data = new[] {
                "destiny_CustomCurtainH",
                "Custom Curtain (Horizontal)",
                "CustomCurtains",
                "Custom Curtains",
                "A customizable curtain."
            };
            return this.CreateGenericCustomItem<Block_CustomCurtainH, CustomBlock_Network>(CURTAIN_HORIZONTAL_ID, CUSTOM_CURTAIN_H_ITEM_ID, data, Vector3.zero, Vector3.zero, recipe, CraftingCategory.Decorations);
        }

        private Item_Base CreateCustomCurtainVItem()
        {
            var recipe = new[] {
                new CostMultiple(new[] { ItemManager.GetItemByIndex(21) }, 10),
                new CostMultiple(new[] { ItemManager.GetItemByIndex(25) }, 20),
                new CostMultiple(new[] { ItemManager.GetItemByIndex(95) }, 14),
                new CostMultiple(new[] { ItemManager.GetItemByIndex(20) }, 9)
            };
            var data = new[] {
                "destiny_CustomCurtainV",
                "Custom Curtain (Vertical)",
                "CustomCurtains",
                "Custom Curtains",
                "A customizable curtain."
            };
            return this.CreateGenericCustomItem<Block_CustomCurtainV, CustomBlock_Network>(CURTAIN_VERTICAL_ID, CUSTOM_CURTAIN_V_ITEM_ID, data, Vector3.zero, Vector3.zero, recipe, CraftingCategory.Decorations);
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

        private Item_Base CreateCustomRugBigItem()
        {
            var recipe = new[] {
                new CostMultiple(new[] { ItemManager.GetItemByIndex(25) }, 2),
                new CostMultiple(new[] { ItemManager.GetItemByIndex(22) }, 1),
            };
            var data = new[] {
                "destiny_CustomRugBig",
                "Custom Rug (Big)",
                "CustomRugs",
                "Custom Rugs",
                "A customizable rug."
            };
            return this.CreateGenericCustomItem<Block_CustomRugBig, CustomBlock_Network>(RUG_BIG_ID, CUSTOM_RUG_BIG_ITEM_ID, data, new Vector3(1.4797308f, 0.14399316f, 2.736644f), new Vector3(0, 0.005513929f, 0), recipe, CraftingCategory.Other);
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
            return this.CreateGenericCustomItem<Block_CustomRugSmall, CustomBlock_Network>(RUG_SMALL_ID, CUSTOM_RUG_SMALL_ITEM_ID, data, new Vector3(0.8902238f, 0.12493733f, 1.32783f), new Vector3(0, 0.005513929f, 0), recipe, CraftingCategory.Other);
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
            dest.Edit(src, LOCATIONS[bt].Item1, LOCATIONS[bt].Item2, SIZES[bt].Item1, SIZES[bt].Item2, bt);
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
            BED,
            CURTAIN_H,
            CURTAIN_V,
            FLAG,
            RUG_BIG,
            RUG_SMALL,
            SAIL,
            // Special value used for the edit function to not mirror.
            NONE
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
                RGD_Storage rgd = __instance as RGD_Storage;
                if (rgd != null)
                {
                    ICustomBlock cb = block as ICustomBlock;
                    if (cb != null)
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
                                    imageData = imageData.SanitizeImage(cb.GetBlockType());
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
                                cb.SetSendUpdates(false);
                                cb.SetImageData(imageData);
                                cb.SetSendUpdates(true);
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
    }

    /*
    [HarmonyPatch(typeof(SteamNetworking), "SendP2PPacket")]
    public class Patch_Steam
    {
        public static void Prefix(CSteamID steamIDRemote, uint cubData)
        {
            Debug.Log($"Got packet of length {cubData}");
            P2PSessionState_t state = new P2PSessionState_t();
            SteamNetworking.GetP2PSessionState(steamIDRemote, out state);
            Debug.Log($"Bytes to send: {state.m_nBytesQueuedForSend}");
        }

        public static void Postfix(CSteamID steamIDRemote)
        {
            P2PSessionState_t state = new P2PSessionState_t();
            SteamNetworking.GetP2PSessionState(steamIDRemote, out state);
            Debug.Log($"Bytes to send: {state.m_nBytesQueuedForSend}");
        }
    }*/

    static class Texture2DExtension
    {
        // How is Aidan so amazing?
        public static Texture2D CreateReadable(this Texture2D source, Rect? copyArea = null, RenderTextureFormat format = RenderTextureFormat.Default, RenderTextureReadWrite readWrite = RenderTextureReadWrite.Default, TextureFormat? targetFormat = null, bool mipChain = false)
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
        public static void Edit(this Texture2D baseImg, Texture2D overlay, int xOffset, int yOffset, int targetX, int targetY, CustomFlags.BlockType bt = CustomFlags.BlockType.NONE)
        {
            var w = targetX;
            var h = targetY;
            var mirrorX = CustomFlags.MIRROR[bt].Item1 ? 1 : 0;
            var mirrorY = CustomFlags.MIRROR[bt].Item2 ? 1 : 0;
            for (var x = 0; x < w; x++)
            {
                for (var y = 0; y < h; y++)
                {
                    baseImg.SetPixel(xOffset + x, yOffset + y, baseImg.GetPixel(x, y).Overlay(overlay.GetPixelBilinear(Math.Abs(mirrorX - (float)x / w), Math.Abs(mirrorY - (float)y / h))));
                }
            }
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
            texOriginal.wrapMode = TextureWrapMode.Clamp;
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
            tex.wrapMode = TextureWrapMode.Clamp;
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
