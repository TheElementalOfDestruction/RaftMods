using System;
using System.Collections;
using UnityEngine;


namespace DestinyCustomBlocks
{
    public interface ICustomBlock
    {
        /*
         * Retrieves the member of the BlockType enum that identifies this
         * class.
         */
        BlockType GetBlockType();

        /*
         * Get the color array that will make up the custom image in the
         * texture.
         */
        byte[] GetImageData();

        /*
         * Set the color array that will make up the custom image in the
         * texture.
         */
        void SetImageData(byte[] data);

        /*
         * Coroutine for setting image data. Typically used only for block
         * creation.
         */
        IEnumerator SetImageDataCo(byte[] data, bool sendUpdates);

        /*
         * Tells the block whether it should be broadcasting updates at all.
         */
        void SetSendUpdates(bool state);

        /*
         * Gets the sprite to use for the current texture in the block editor.
         */
        Sprite GetSprite();

        /*
         * Tells the block to switch the quality for the current custom image.
         * A value of true means that it should follow the game quality and use
         * mip maps, while a value of false tells it to always use full
         * resolution.
         */
        void SwitchMipMapState(bool state);
    }



    // Class for handling how to display a custom item.
    public abstract class Block_CustomBlock_Base : Block, ICustomBlock, IRaycastable
    {
        byte[] ICustomBlock.GetImageData()
        {
            return this.ImageData;
        }

        void ICustomBlock.SetImageData(byte[] data)
        {
            this.ImageData = data;
        }

        public IEnumerator SetImageDataCo(byte[] data, bool sendUpdates)
        {
            object lockObj = null;
            if (this.coLock)
            {
                yield return this.coLock.Lock(x => lockObj = x);
            }
            // Backup our data incase the patch function fails.
            byte[] oldData = this.imageData;
            this.imageData = data;
            bool success = false;

            yield return this.PatchRenderer(sendUpdates, x => success = x);

            // If we fail, reset the image data back to it's previous state.
            if (!success)
            {
                this.imageData = oldData;
            }

            if (lockObj != null)
            {
                this.coLock.Unlock(lockObj);
            }
        }

        BlockType ICustomBlock.GetBlockType()
        {
            return this.CustomBlockType;
        }

        void ICustomBlock.SetSendUpdates(bool state)
        {
            this.sendUpdates = state;
        }

        Sprite ICustomBlock.GetSprite()
        {
            return this.sprite;
        }

        void ICustomBlock.SwitchMipMapState(bool state)
        {
            if (this.occupyingComponent)
            {
                Material mat = CustomBlocks.UseMipMaps ? this.autoResolutionMat : this.fullResolutionMat;
                foreach(int i in this.RendererIndicies)
                {
                    this.occupyingComponent.renderers[i].material = mat;
                }
            }
        }

        private static readonly int[] rendererIndicies = { 0 };

        [SerializeField]
        protected byte[] imageData;
        [SerializeField]
        protected bool rendererPatched = false;
        protected bool showingText;
        [SerializeField]
        protected Sprite sprite;
        [SerializeField]
        protected Material fullResolutionMat;
        [SerializeField]
        protected Material autoResolutionMat;
        protected CoroutineLock coLock;

        public bool sendUpdates = true;

        protected virtual int[] RendererIndicies
        {
            get
            {
                return rendererIndicies;
            }
        }

        public abstract BlockType CustomBlockType { get; }

        public byte[] ImageData
        {
            get
            {
                return this.imageData;
            }
            set
            {
                if (this.imageData?.Length == 0 && value?.Length == 0)
                {
                    return;
                }
                StartCoroutine(this.SetImageDataCo(value, this.sendUpdates));
            }
        }

        protected override void Start()
        {
            base.Start();
            this.coLock = new CoroutineLock();
            if (!this.rendererPatched)
            {
                this.rendererPatched = true;
                this.imageData = new byte[0];
                if (!RAPI.IsDedicatedServer())
                {
                    this.autoResolutionMat = CustomBlocks.instance.defaultMaterialsMipEnabled[this.CustomBlockType];
                    this.fullResolutionMat = CustomBlocks.instance.defaultMaterials[this.CustomBlockType];
                    this.sprite = CustomBlocks.instance.defaultSprites[this.CustomBlockType];
                }
            }
        }

        /*
         * Attempt to load the new data into the renderer(s), returning whether
         * it succeeded.
         */
        protected virtual IEnumerator PatchRenderer(bool sendUpdates, Action<bool> callback)
        {
            // A null value is completely invalid.
            if (this.imageData == null)
            {
                callback(false);
                yield break;
            }

            if(!RAPI.IsDedicatedServer())
            {
                // Make sure the OccupyingComponent is good.
                if (!this.occupyingComponent)
                {
                    this.occupyingComponent = this.GetComponent<OccupyingComponent>();
                    this.occupyingComponent.FindRenderers();
                }


                // Setup our new materials and figure out which to use.
                Material mat = null;

                if (this.imageData.Length != 0)
                {
                    // Create a new material from our data.
                    yield return CustomBlocks.CreateMaterialFromImageData(this.imageData, this.CustomBlockType, x => mat = x);
                    // If the creation fails, return false to signify.
                    if (!mat)
                    {
                        callback(false);
                        yield break;
                    }

                    // Create the mipmap enabled version.
                    this.autoResolutionMat = mat.CreateMipMapEnabled(this.CustomBlockType);
                }
                else
                {
                    // If we are here, use the default flag material.
                    mat = CustomBlocks.instance.defaultMaterials[this.CustomBlockType];
                    // Get the mipmap enabled version.
                    this.autoResolutionMat = CustomBlocks.instance.defaultMaterialsMipEnabled[this.CustomBlockType];
                }

                // Setup the automatic resolution version and determine which to
                // use.
                this.fullResolutionMat = mat;
                if (CustomBlocks.UseMipMaps)
                {
                    mat = this.autoResolutionMat;
                }

                // Replace the material(s).
                foreach(int i in this.RendererIndicies)
                {
                    this.occupyingComponent.renderers[i].material = mat;
                }
            }
            else
            {
                if (this.imageData.Length != 0)
                {
                    var size = CustomBlocks.SIZES[this.CustomBlockType].Item1;
                    size *= CustomBlocks.SIZES[this.CustomBlockType].Item2 * 4;
                    if (this.imageData.Length != size)
                    {
                        callback(false);
                        yield break;
                    }
                }
            }
            this.rendererPatched = true;
            if (this.hasBeenPlaced && sendUpdates)
            {
                this.GetComponent<ICustomBlockNetwork>()?.BroadcastChange(this.imageData);
            }

            // See if we need to cleanup the old sprite.
            if (this.sprite != null && this.sprite != CustomBlocks.instance.defaultSprites[this.CustomBlockType])
            {
                DestroyImmediate(this.sprite.texture);
                DestroyImmediate(this.sprite);
            }

            // Before we trash any data we might have, we need to create our
            // sprite.
            yield return CustomBlocks.CreateSpriteFromBytes(this.imageData, this.CustomBlockType, x => this.sprite = x);

            if (!Raft_Network.IsHost && this.imageData.Length > 0)
            {
                // If we are not the host, set the image data to a single byte,
                // But *only* if it is not an empty array already. This tells
                // the code that it has image data without actually storing any.
                this.imageData = new byte[1];
            }

            callback(true);
        }

        public override RGD Serialize_Save()
        {
            var r = CustomBlocks.CreateObject<RGD_Storage>();
            r.CopyFieldsOf(new RGD_Block(RGDType.Block, this));
            r.slots = new RGD_Slot[] { CustomBlocks.CreateObject<RGD_Slot>() };
            r.slots[0].exclusiveString = Convert.ToBase64String(this.ImageData);
            r.slots[0].itemAmount = 2;
            return r;
        }

        public override RGD_Block GetBlockCreationData()
        {
            return this.Serialize_Save() as RGD_Block;
        }

        void IRaycastable.OnIsRayed()
        {
            if (!this.hasBeenPlaced || !CustomBlocks.EditorEnabled || CustomBlocks.InteractKey == KeyCode.None)
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
                canvas.displayTextManager.ShowText($"Press {CustomBlocks.InteractKey} to open the edit menu.", CustomBlocks.InteractKey, 0, 0, false);
                this.showingText = true;
                if (Input.GetKeyDown(CustomBlocks.InteractKey))
                {
                    CustomBlocks.OpenCustomBlocksMenu(this);
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



    public abstract class Block_CustomBlock_Interactable : Block_Interactable, ICustomBlock, IRaycastable
    {
        byte[] ICustomBlock.GetImageData()
        {
            return this.ImageData;
        }

        void ICustomBlock.SetImageData(byte[] data)
        {
            this.ImageData = data;
        }

        public IEnumerator SetImageDataCo(byte[] data, bool sendUpdates)
        {
            object lockObj = null;
            if (this.coLock)
            {
                yield return this.coLock.Lock(x => lockObj = x);
            }
            // Backup our data incase the patch function fails.
            byte[] oldData = this.imageData;
            this.imageData = data;
            bool success = false;

            yield return this.PatchRenderer(sendUpdates, x => success = x);

            // If we fail, reset the image data back to it's previous state.
            if (!success)
            {
                this.imageData = oldData;
            }

            if (lockObj != null)
            {
                this.coLock.Unlock(lockObj);
            }
        }

        BlockType ICustomBlock.GetBlockType()
        {
            return this.CustomBlockType;
        }

        void ICustomBlock.SetSendUpdates(bool state)
        {
            this.sendUpdates = state;
        }

        Sprite ICustomBlock.GetSprite()
        {
            return this.sprite;
        }

        void ICustomBlock.SwitchMipMapState(bool state)
        {
            if (this.occupyingComponent)
            {
                Material mat = CustomBlocks.UseMipMaps ? this.autoResolutionMat : this.fullResolutionMat;
                foreach(int i in this.RendererIndicies)
                {
                    this.occupyingComponent.renderers[i].material = mat;
                }
            }
        }

        private static readonly int[] rendererIndicies = { 0 };

        [SerializeField]
        protected byte[] imageData;
        [SerializeField]
        protected bool rendererPatched = false;
        protected bool showingText;
        protected Sprite sprite;
        [SerializeField]
        protected Material fullResolutionMat;
        [SerializeField]
        protected Material autoResolutionMat;
        protected CoroutineLock coLock;

        public bool sendUpdates = true;

        protected virtual int[] RendererIndicies
        {
            get
            {
                return rendererIndicies;
            }
        }

        public abstract BlockType CustomBlockType { get; }

        public byte[] ImageData
        {
            get
            {
                return this.imageData;
            }
            set
            {
                if (this.imageData?.Length == 0 && value?.Length == 0)
                {
                    return;
                }
                StartCoroutine(this.SetImageDataCo(value, this.sendUpdates));
            }
        }

        protected override void Start()
        {
            base.Start();
            this.coLock = new CoroutineLock();
            if (!this.rendererPatched)
            {
                this.rendererPatched = true;
                this.imageData = new byte[0];
                if(!RAPI.IsDedicatedServer())
                {
                    this.autoResolutionMat = CustomBlocks.instance.defaultMaterialsMipEnabled[this.CustomBlockType];
                    this.fullResolutionMat = CustomBlocks.instance.defaultMaterials[this.CustomBlockType];
                    this.sprite = CustomBlocks.instance.defaultSprites[this.CustomBlockType];
                }
            }
        }

        /*
         * Attempt to load the new data into the renderer(s), returning whether
         * it succeeded.
         */
        protected virtual IEnumerator PatchRenderer(bool sendUpdates, Action<bool> callback)
        {
            // A null value is completely invalid.
            if (this.imageData == null)
            {
                callback(false);
                yield break;
            }

            if(!RAPI.IsDedicatedServer())
            {
                // Make sure the OccupyingComponent is good.
                if (!this.occupyingComponent)
                {
                    this.occupyingComponent = this.GetComponent<OccupyingComponent>();
                    this.occupyingComponent.FindRenderers();
                }


                // Setup our new materials and figure out which to use.
                Material mat = null;

                if (this.imageData.Length != 0)
                {
                    // Create a new material from our data.
                    yield return CustomBlocks.CreateMaterialFromImageData(this.imageData, this.CustomBlockType, x => mat = x);
                    // If the creation fails, return false to signify.
                    if (!mat)
                    {
                        callback(false);
                        yield break;
                    }

                    // Create the mipmap enabled version.
                    this.autoResolutionMat = mat.CreateMipMapEnabled(this.CustomBlockType);
                }
                else
                {
                    // If we are here, use the default flag material.
                    mat = CustomBlocks.instance.defaultMaterials[this.CustomBlockType];
                    // Get the mipmap enabled version.
                    this.autoResolutionMat = CustomBlocks.instance.defaultMaterialsMipEnabled[this.CustomBlockType];
                }

                // Setup the automatic resolution version and determine which to
                // use.
                this.fullResolutionMat = mat;
                if (CustomBlocks.UseMipMaps)
                {
                    mat = this.autoResolutionMat;
                }

                // Replace the material(s).
                foreach(int i in this.RendererIndicies)
                {
                    this.occupyingComponent.renderers[i].material = mat;
                }
            }
            else
            {
                if (this.imageData.Length != 0)
                {
                    var size = CustomBlocks.SIZES[this.CustomBlockType].Item1;
                    size *= CustomBlocks.SIZES[this.CustomBlockType].Item2 * 4;
                    if (this.imageData.Length != size)
                    {
                        callback(false);
                        yield break;
                    }
                }
            }
            this.rendererPatched = true;
            if (this.hasBeenPlaced && sendUpdates)
            {
                this.GetComponent<ICustomBlockNetwork>()?.BroadcastChange(this.imageData);
            }

            // See if we need to cleanup the old sprite.
            if (this.sprite != null && this.sprite != CustomBlocks.instance.defaultSprites[this.CustomBlockType])
            {
                DestroyImmediate(this.sprite.texture);
                DestroyImmediate(this.sprite);
            }

            // Before we trash any data we might have, we need to create our
            // sprite.
            yield return CustomBlocks.CreateSpriteFromBytes(this.imageData, this.CustomBlockType, x => this.sprite = x);

            if (!Raft_Network.IsHost && this.imageData.Length > 0)
            {
                // If we are not the host, set the image data to a single byte,
                // But *only* if it is not an empty array already. This tells
                // the code that it has image data without actually storing any.
                this.imageData = new byte[1];
            }

            callback(true);
        }

        public override RGD Serialize_Save()
        {
            var r = CustomBlocks.CreateObject<RGD_Storage>();
            r.CopyFieldsOf(new RGD_Block(RGDType.Block, this));
            r.slots = new RGD_Slot[] { CustomBlocks.CreateObject<RGD_Slot>() };
            r.slots[0].exclusiveString = Convert.ToBase64String(this.ImageData);
            r.slots[0].itemAmount = 2;
            r.storageObjectIndex = BitConverter.ToUInt32(BitConverter.GetBytes(this.GetComponent<Placeable_Interactable>().CurrentIndex), 0);

            return r;
        }

        public override RGD_Block GetBlockCreationData()
        {
            return this.Serialize_Save() as RGD_Block;
        }

        void IRaycastable.OnIsRayed()
        {
            if (!this.hasBeenPlaced || !CustomBlocks.EditorEnabled || CustomBlocks.InteractKey == KeyCode.None)
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
                canvas.displayTextManager.ShowText($"Press {CustomBlocks.InteractKey} to open the edit menu.", CustomBlocks.InteractKey, 0, 0, false);
                this.showingText = true;
                if (Input.GetKeyDown(CustomBlocks.InteractKey))
                {
                    CustomBlocks.OpenCustomBlocksMenu(this);
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



    public class Block_CustomBed : Bed, ICustomBlock, IRaycastable
    {
        byte[] ICustomBlock.GetImageData()
        {
            return this.ImageData;
        }

        void ICustomBlock.SetImageData(byte[] data)
        {
            this.ImageData = data;
        }

        public IEnumerator SetImageDataCo(byte[] data, bool sendUpdates)
        {
            object lockObj = null;
            if (this.coLock)
            {
                yield return this.coLock.Lock(x => lockObj = x);
            }
            // Backup our data incase the patch function fails.
            byte[] oldData = this.imageData;
            this.imageData = data;
            bool success = false;

            yield return this.PatchRenderer(sendUpdates, x => success = x);

            // If we fail, reset the image data back to it's previous state.
            if (!success)
            {
                this.imageData = oldData;
            }

            if (lockObj != null)
            {
                this.coLock.Unlock(lockObj);
            }
        }

        BlockType ICustomBlock.GetBlockType()
        {
            return this.CustomBlockType;
        }

        void ICustomBlock.SetSendUpdates(bool state)
        {
            this.sendUpdates = state;
        }

        Sprite ICustomBlock.GetSprite()
        {
            return this.sprite;
        }

        void ICustomBlock.SwitchMipMapState(bool state)
        {
            if (this.occupyingComponent)
            {
                Material mat = CustomBlocks.UseMipMaps ? this.autoResolutionMat : this.fullResolutionMat;
                foreach(int i in this.RendererIndicies)
                {
                    this.occupyingComponent.renderers[i].material = mat;
                }
            }
        }

        private static readonly int[] rendererIndicies = { 1, 2 };

        [SerializeField]
        protected byte[] imageData;
        [SerializeField]
        protected bool rendererPatched = false;
        protected bool showingText;
        protected Sprite sprite;
        [SerializeField]
        protected Material fullResolutionMat;
        [SerializeField]
        protected Material autoResolutionMat;
        protected CoroutineLock coLock;

        public bool sendUpdates = true;

        protected virtual int[] RendererIndicies
        {
            get
            {
                return rendererIndicies;
            }
        }

        public BlockType CustomBlockType
        {
            get
            {
                return BlockType.BED;
            }
        }

        public byte[] ImageData
        {
            get
            {
                return this.imageData;
            }
            set
            {
                if (this.imageData?.Length == 0 && value?.Length == 0)
                {
                    return;
                }
                StartCoroutine(this.SetImageDataCo(value, this.sendUpdates));
            }
        }

        protected override void Start()
        {
            base.Start();
            this.coLock = new CoroutineLock();
            if (!this.rendererPatched)
            {
                this.rendererPatched = true;
                this.imageData = new byte[0];
                if(!RAPI.IsDedicatedServer())
                {
                    this.autoResolutionMat = CustomBlocks.instance.defaultMaterialsMipEnabled[this.CustomBlockType];
                    this.fullResolutionMat = CustomBlocks.instance.defaultMaterials[this.CustomBlockType];
                    this.sprite = CustomBlocks.instance.defaultSprites[this.CustomBlockType];
                }
            }
        }

        /*
         * Attempt to load the new data into the renderer(s), returning whether
         * it succeeded.
         */
        protected virtual IEnumerator PatchRenderer(bool sendUpdates, Action<bool> callback)
        {
            // A null value is completely invalid.
            if (this.imageData == null)
            {
                callback(false);
                yield break;
            }

            if(!RAPI.IsDedicatedServer())
            {
                // Make sure the OccupyingComponent is good.
                if (!this.occupyingComponent)
                {
                    this.occupyingComponent = this.GetComponent<OccupyingComponent>();
                    this.occupyingComponent.FindRenderers();
                }


                // Setup our new materials and figure out which to use.
                Material mat = null;

                if (this.imageData.Length != 0)
                {
                    // Create a new material from our data.
                    yield return CustomBlocks.CreateMaterialFromImageData(this.imageData, this.CustomBlockType, x => mat = x);
                    // If the creation fails, return false to signify.
                    if (!mat)
                    {
                        callback(false);
                        yield break;
                    }

                    // Create the mipmap enabled version.
                    this.autoResolutionMat = mat.CreateMipMapEnabled(this.CustomBlockType);
                }
                else
                {
                    // If we are here, use the default flag material.
                    mat = CustomBlocks.instance.defaultMaterials[this.CustomBlockType];
                    // Get the mipmap enabled version.
                    this.autoResolutionMat = CustomBlocks.instance.defaultMaterialsMipEnabled[this.CustomBlockType];
                }

                // Setup the automatic resolution version and determine which to
                // use.
                this.fullResolutionMat = mat;
                if (CustomBlocks.UseMipMaps)
                {
                    mat = this.autoResolutionMat;
                }

                // Replace the material(s).
                foreach(int i in this.RendererIndicies)
                {
                    this.occupyingComponent.renderers[i].material = mat;
                }
            }
            else
            {
                if (this.imageData.Length != 0)
                {
                    var size = CustomBlocks.SIZES[this.CustomBlockType].Item1;
                    size *= CustomBlocks.SIZES[this.CustomBlockType].Item2 * 4;
                    if (this.imageData.Length != size)
                    {
                        callback(false);
                        yield break;
                    }
                }
            }
            this.rendererPatched = true;
            if (this.hasBeenPlaced && sendUpdates)
            {
                this.GetComponent<ICustomBlockNetwork>()?.BroadcastChange(this.imageData);
            }

            // See if we need to cleanup the old sprite.
            if (this.sprite != null && this.sprite != CustomBlocks.instance.defaultSprites[this.CustomBlockType])
            {
                DestroyImmediate(this.sprite.texture);
                DestroyImmediate(this.sprite);
            }

            // Before we trash any data we might have, we need to create our
            // sprite.
            yield return CustomBlocks.CreateSpriteFromBytes(this.imageData, this.CustomBlockType, x => this.sprite = x);

            if (!Raft_Network.IsHost && this.imageData.Length > 0)
            {
                // If we are not the host, set the image data to a single byte,
                // But *only* if it is not an empty array already. This tells
                // the code that it has image data without actually storing any.
                this.imageData = new byte[1];
            }

            callback(true);
        }

        public override RGD Serialize_Save()
        {
            var r = CustomBlocks.CreateObject<RGD_Storage>();
            r.CopyFieldsOf(new RGD_Block(RGDType.Block, this));
            r.slots = new RGD_Slot[] { CustomBlocks.CreateObject<RGD_Slot>() };
            r.slots[0].exclusiveString = Convert.ToBase64String(this.ImageData);
            r.slots[0].itemAmount = 2;
            return r;
        }

        public override RGD_Block GetBlockCreationData()
        {
            return this.Serialize_Save() as RGD_Block;
        }

        void IRaycastable.OnIsRayed()
        {
            base.OnIsRayed();
            if (!this.hasBeenPlaced || !CustomBlocks.EditorEnabled || CustomBlocks.InteractKey == KeyCode.None)
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
                canvas.displayTextManager.ShowText($"Press {CustomBlocks.InteractKey} to open the edit menu.", CustomBlocks.InteractKey, 0, 0, false);
                this.showingText = true;
                if (Input.GetKeyDown(CustomBlocks.InteractKey))
                {
                    CustomBlocks.OpenCustomBlocksMenu(this);
                }
            }
        }

        void IRaycastable.OnRayEnter() {}

        void IRaycastable.OnRayExit()
        {
            base.OnRayExit();
            if (this.showingText)
            {
                ComponentManager<CanvasHelper>.Value.displayTextManager.HideDisplayTexts();
                this.showingText = false;
            }
        }
    }



    public class Block_CustomCurtainH : Block_CustomBlock_Interactable
    {
        public override BlockType CustomBlockType
        {
            get
            {
                return BlockType.CURTAIN_H;
            }
        }

        private static readonly int[] rendererIndicies = { 0, 1, 2 };

        protected override int[] RendererIndicies
        {
            get
            {
                return rendererIndicies;
            }
        }
    }



    public class Block_CustomCurtainV : Block_CustomBlock_Interactable
    {
        public override BlockType CustomBlockType
        {
            get
            {
                return BlockType.CURTAIN_V;
            }
        }

        private static readonly int[] rendererIndicies = { 0, 1, 2 };

        protected override int[] RendererIndicies
        {
            get
            {
                return rendererIndicies;
            }
        }
    }



    public class Block_CustomFlag : Block_CustomBlock_Base
    {
        public override BlockType CustomBlockType
        {
            get
            {
                return BlockType.FLAG;
            }
        }

        protected override int[] RendererIndicies
        {
            get
            {
                // Result is different based on flag orientation.
                return new int[] { this.occupyingComponent.renderers.Length - 1 };
            }
        }
    }



    public class Block_CustomPoster : Block_CustomBlock_Base
    {
        protected bool blockTypeSet = false;

        private BlockType bt = BlockType.NONE;

        public override BlockType CustomBlockType
        {
            get
            {
                if (this.bt == BlockType.NONE)
                {
                    this.bt = CustomBlocks.ID_TO_BLOCKTYPE[this.buildableItem.UniqueIndex];
                }
                return bt;
            }
        }

        protected override void Start()
        {
            base.Start();

            Array.ForEach(this.GetComponentsInChildren<RaycastInteractable>(),
                          x => x.AddRaycastables(new IRaycastable[] { this }));
        }
    }



    public class Block_CustomRugBig : Block_CustomBlock_Base
    {
        public override BlockType CustomBlockType
        {
            get
            {
                return BlockType.RUG_BIG;
            }
        }
    }



    public class Block_CustomRugSmall : Block_CustomBlock_Base
    {
        public override BlockType CustomBlockType
        {
            get
            {
                return BlockType.RUG_SMALL;
            }
        }
    }



    public class Block_CustomSail : Block_CustomBlock_Base
    {
        public override BlockType CustomBlockType
        {
            get
            {
                return BlockType.SAIL;
            }
        }

        private static readonly int[] rendererIndicies = { 1 };

        protected override int[] RendererIndicies
        {
            get
            {
                return rendererIndicies;
            }
        }

        public override RGD Serialize_Save()
        {
            // For the network stuff, we need to do this on the network
            // component.
            return null;
        }

        public RGD_Storage GetSerialized()
        {
            RGD_Storage rgd = base.Serialize_Save() as RGD_Storage;
            // Attach our data to the existing save data.
            rgd.isOpen = this.GetComponent<Sail>().open;
            // Doing a little data abusing here.
            rgd.storageObjectIndex = BitConverter.ToUInt32(BitConverter.GetBytes(this.GetComponent<Sail>().LocalRotation), 0);

            return rgd;
        }

        public override RGD_Block GetBlockCreationData()
        {
            return this.GetSerialized();
        }
    }
}
