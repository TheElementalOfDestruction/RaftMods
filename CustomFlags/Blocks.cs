using System;
using UnityEngine;


namespace DestinyCustomBlocks
{
    public interface ICustomBlock
    {
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
         * Retrieves the member of the BlockType enum that identifies this
         * class.
         */
        CustomBlocks.BlockType GetBlockType();

        /*
         * Tells the block whether it should be broadcasting updates at all.
         */
        void SetSendUpdates(bool state);

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

        CustomBlocks.BlockType ICustomBlock.GetBlockType()
        {
            return this.CustomBlockType;
        }

        void ICustomBlock.SetSendUpdates(bool state)
        {
            this.sendUpdates = state;
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

        protected byte[] imageData;
        protected bool rendererPatched = false;
        protected bool showingText;
        protected Material fullResolutionMat;
        protected Material autoResolutionMat;

        public bool sendUpdates = true;

        protected virtual int[] RendererIndicies
        {
            get
            {
                return rendererIndicies;
            }
        }

        public abstract CustomBlocks.BlockType CustomBlockType { get; }

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

        protected override void Start()
        {
            base.Start();
            if (!this.rendererPatched)
            {
                this.ImageData = new byte[0];
            }
        }

        /*
         * Attempt to load the new data into the renderer(s), returning whether
         * it succeeded.
         */
        protected virtual bool PatchRenderer()
        {
            // A null value is completely invalid.
            try
            {
                if (this.imageData == null)
                {
                    return false;
                }

                // Make sure the OccupyingComponent is good.
                if (!this.occupyingComponent)
                {
                    this.occupyingComponent = this.GetComponent<OccupyingComponent>();
                    this.occupyingComponent.FindRenderers();
                }

                // Setup our new materials and figure out which to use.
                Material mat;

                if (this.imageData.Length != 0)
                {
                    // Create a new material from our data.
                    mat = CustomBlocks.CreateMaterialFromImageData(this.imageData, this.CustomBlockType);
                    // If the creation fails, return false to signify.
                    if (!mat)
                    {
                        return false;
                    }
                }
                else
                {
                    // If we are here, use the default flag material.
                    mat = CustomBlocks.instance.defaultMaterials[this.CustomBlockType];
                }

                // Setup the automatic resolution version and determine which to
                // use.
                this.fullResolutionMat = mat;
                this.autoResolutionMat = mat.CreateMipMapEnabled();
                if (CustomBlocks.UseMipMaps)
                {
                    mat = this.autoResolutionMat;
                }

                // Replace the material(s).
                foreach(int i in this.RendererIndicies)
                {
                    this.occupyingComponent.renderers[i].material = mat;
                }

                this.rendererPatched = true;
                if (this.sendUpdates)
                {
                    this.GetComponent<CustomBlock_Network>()?.BroadcastChange(this.imageData);
                }
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                throw e;
            }
        }

        public override RGD Serialize_Save()
        {
            var r = CustomBlocks.CreateObject<RGD_Storage>();
            r.CopyFieldsOf(new RGD_Block(RGDType.Block, this));
            r.slots = new RGD_Slot[] { CustomBlocks.CreateObject<RGD_Slot>() };
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

        CustomBlocks.BlockType ICustomBlock.GetBlockType()
        {
            return this.CustomBlockType;
        }

        void ICustomBlock.SetSendUpdates(bool state)
        {
            this.sendUpdates = state;
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

        protected byte[] imageData;
        protected bool rendererPatched = false;
        protected bool showingText;
        protected Material fullResolutionMat;
        protected Material autoResolutionMat;

        public bool sendUpdates = true;

        protected virtual int[] RendererIndicies
        {
            get
            {
                return rendererIndicies;
            }
        }

        public abstract CustomBlocks.BlockType CustomBlockType { get; }

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

        protected override void Start()
        {
            base.Start();
            if (!this.rendererPatched)
            {
                this.ImageData = new byte[0];
            }
        }

        /*
         * Attempt to load the new data into the renderer(s), returning whether
         * it succeeded.
         */
        protected virtual bool PatchRenderer()
        {
            // A null value is completely invalid.
            if (this.imageData == null)
            {
                return false;
            }

            // Make sure the OccupyingComponent is good.
            if (!this.occupyingComponent)
            {
                this.occupyingComponent = this.GetComponent<OccupyingComponent>();
                this.occupyingComponent.FindRenderers();
            }

            // Setup our new materials and figure out which to use.
            Material mat;

            if (this.imageData.Length != 0)
            {
                // Create a new material from our data.
                mat = CustomBlocks.CreateMaterialFromImageData(this.imageData, this.CustomBlockType);
                // If the creation fails, return false to signify.
                if (!mat)
                {
                    return false;
                }
            }
            else
            {
                // If we are here, use the default flag material.
                mat = CustomBlocks.instance.defaultMaterials[this.CustomBlockType];
            }

            // Setup the automatic resolution version and determine which to
            // use.
            this.fullResolutionMat = mat;
            this.autoResolutionMat = mat.CreateMipMapEnabled();
            if (CustomBlocks.UseMipMaps)
            {
                mat = this.autoResolutionMat;
            }

            // Replace the material(s).
            foreach(int i in this.RendererIndicies)
            {
                this.occupyingComponent.renderers[i].material = mat;
            }

            this.rendererPatched = true;
            if (this.sendUpdates)
            {
                this.GetComponent<CustomBlock_Network>()?.BroadcastChange(this.imageData);
            }
            return true;
        }

        public override RGD Serialize_Save()
        {
            var r = CustomBlocks.CreateObject<RGD_Storage>();
            r.CopyFieldsOf(new RGD_Block(RGDType.Block, this));
            r.slots = new RGD_Slot[] { CustomBlocks.CreateObject<RGD_Slot>() };
            r.slots[0].exclusiveString = Convert.ToBase64String(this.ImageData);
            r.slots[0].itemAmount = 1;
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

        CustomBlocks.BlockType ICustomBlock.GetBlockType()
        {
            return this.CustomBlockType;
        }

        void ICustomBlock.SetSendUpdates(bool state)
        {
            this.sendUpdates = state;
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

        protected byte[] imageData;
        protected bool rendererPatched = false;
        protected bool showingText;
        protected Material fullResolutionMat;
        protected Material autoResolutionMat;

        public bool sendUpdates = true;

        protected virtual int[] RendererIndicies
        {
            get
            {
                return rendererIndicies;
            }
        }

        public CustomBlocks.BlockType CustomBlockType
        {
            get
            {
                return CustomBlocks.BlockType.BED;
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
                // Backup our data incase the patch function fails.
                byte[] oldData = this.imageData;
                this.imageData = value;
                if (!this.PatchRenderer())
                {
                    this.imageData = oldData;
                }
            }
        }

        protected override void Start()
        {
            base.Start();
            if (!this.rendererPatched)
            {
                this.ImageData = new byte[0];
            }
        }

        /*
         * Attempt to load the new data into the renderer(s), returning whether
         * it succeeded.
         */
        protected virtual bool PatchRenderer()
        {
            // A null value is completely invalid.
            if (this.imageData == null)
            {
                return false;
            }

            // Make sure the OccupyingComponent is good.
            if (!this.occupyingComponent)
            {
                this.occupyingComponent = this.GetComponent<OccupyingComponent>();
                this.occupyingComponent.FindRenderers();
            }

            // Setup our new materials and figure out which to use.
            Material mat;

            if (this.imageData.Length != 0)
            {
                // Create a new material from our data.
                mat = CustomBlocks.CreateMaterialFromImageData(this.imageData, this.CustomBlockType);
                // If the creation fails, return false to signify.
                if (!mat)
                {
                    return false;
                }
            }
            else
            {
                // If we are here, use the default flag material.
                mat = CustomBlocks.instance.defaultMaterials[this.CustomBlockType];
            }

            // Setup the automatic resolution version and determine which to
            // use.
            this.fullResolutionMat = mat;
            this.autoResolutionMat = mat.CreateMipMapEnabled();
            if (CustomBlocks.UseMipMaps)
            {
                mat = this.autoResolutionMat;
            }

            // Replace the material(s).
            foreach(int i in this.RendererIndicies)
            {
                this.occupyingComponent.renderers[i].material = mat;
            }

            this.rendererPatched = true;
            if (this.sendUpdates)
            {
                this.GetComponent<CustomBlock_Network>()?.BroadcastChange(this.imageData);
            }
            return true;
        }

        public override RGD Serialize_Save()
        {
            var r = CustomBlocks.CreateObject<RGD_Storage>();
            r.CopyFieldsOf(new RGD_Block(RGDType.Block, this));
            r.slots = new RGD_Slot[] { CustomBlocks.CreateObject<RGD_Slot>() };
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
        public override CustomBlocks.BlockType CustomBlockType
        {
            get
            {
                return CustomBlocks.BlockType.CURTAIN_H;
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
        public override CustomBlocks.BlockType CustomBlockType
        {
            get
            {
                return CustomBlocks.BlockType.CURTAIN_V;
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
        public override CustomBlocks.BlockType CustomBlockType
        {
            get
            {
                return CustomBlocks.BlockType.FLAG;
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
        protected CustomBlocks.BlockType bt = CustomBlocks.BlockType.NONE;

        public override CustomBlocks.BlockType CustomBlockType
        {
            get
            {
                return this.bt;
            }
        }

        public void SetCustomBlockType(CustomBlocks.BlockType value)
        {
            if (!this.blockTypeSet)
            {
                this.bt = value;
                this.blockTypeSet = true;
            }
        }
    }



    public class Block_CustomRugBig : Block_CustomBlock_Base
    {
        public override CustomBlocks.BlockType CustomBlockType
        {
            get
            {
                return CustomBlocks.BlockType.RUG_BIG;
            }
        }
    }



    public class Block_CustomRugSmall : Block_CustomBlock_Base
    {
        public override CustomBlocks.BlockType CustomBlockType
        {
            get
            {
                return CustomBlocks.BlockType.RUG_SMALL;
            }
        }
    }



    public class Block_CustomSail : Block_CustomBlock_Base
    {
        public override CustomBlocks.BlockType CustomBlockType
        {
            get
            {
                return CustomBlocks.BlockType.SAIL;
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
    }
}
