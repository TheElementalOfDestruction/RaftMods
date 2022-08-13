using System;
using UnityEngine;


namespace DestinyCustomBlocks
{
    public interface ICustomBlock
    {
        byte[] GetImageData();
        void SetImageData(byte[] data);
        CustomBlocks.BlockType GetBlockType();
        void SetSendUpdates(bool state);
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

        protected byte[] imageData;
        protected bool rendererPatched = false;
        protected bool showingText;

        public bool sendUpdates = true;

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

        public abstract CustomBlocks.BlockType CustomBlockType { get; }

        protected override void Start()
        {
            base.Start();
            if (!this.rendererPatched)
            {
                this.ImageData = new byte[0];
            }
        }

        /*
         * Attempt to load the new data into the renderer, returning whether it
         * succeeded.
         */
        protected virtual bool PatchRenderer()
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
                Material mat = CustomBlocks.CreateMaterialFromImageData(this.imageData, this.CustomBlockType);
                // If the creation fails, return false to signify.
                if (!mat)
                {
                    return false;
                }

                // Replace the material.
                this.occupyingComponent.renderers[0].material = mat;
            }
            else
            {
                // If we are here, use the default flag material.

                // Replace the material.
                this.occupyingComponent.renderers[0].material = CustomBlocks.instance.defaultMaterials[this.CustomBlockType];
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

        protected byte[] imageData;
        protected bool rendererPatched = false;
        protected bool showingText;

        public bool sendUpdates = true;

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

        public abstract CustomBlocks.BlockType CustomBlockType { get; }

        protected override void Start()
        {
            base.Start();
            if (!this.rendererPatched)
            {
                this.ImageData = new byte[0];
            }
        }

        /*
         * Attempt to load the new data into the renderer, returning whether it
         * succeeded.
         */
        protected virtual bool PatchRenderer()
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
                Material mat = CustomBlocks.CreateMaterialFromImageData(this.imageData, this.CustomBlockType);
                // If the creation fails, return false to signify.
                if (!mat)
                {
                    return false;
                }

                // Replace the material.
                this.occupyingComponent.renderers[0].material = mat;
            }
            else
            {
                // If we are here, use the default flag material.

                // Replace the material.
                this.occupyingComponent.renderers[0].material = CustomBlocks.instance.defaultMaterials[this.CustomBlockType];
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

        protected byte[] imageData;
        protected bool rendererPatched = false;
        protected bool showingText;

        public bool sendUpdates = true;

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

        public CustomBlocks.BlockType CustomBlockType
        {
            get
            {
                return CustomBlocks.BlockType.BED;
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
         * Attempt to load the new data into the renderer, returning whether it
         * succeeded.
         */
        protected virtual bool PatchRenderer()
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
                Material mat = CustomBlocks.CreateMaterialFromImageData(this.imageData, this.CustomBlockType);
                // If the creation fails, return false to signify.
                if (!mat)
                {
                    return false;
                }

                // Replace the material.
                this.occupyingComponent.renderers[1].material = mat;
                this.occupyingComponent.renderers[2].material = mat;
            }
            else
            {
                // If we are here, use the default flag material.

                // Replace the material.
                this.occupyingComponent.renderers[1].material = CustomBlocks.instance.defaultMaterials[this.CustomBlockType];
                this.occupyingComponent.renderers[2].material = CustomBlocks.instance.defaultMaterials[this.CustomBlockType];
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

        /*
         * Attempt to load the new data into the renderer, returning whether it
         * succeeded.
         */
        protected override bool PatchRenderer()
        {
            if (!base.PatchRenderer())
            {
                return false;
            }
            var mat = this.occupyingComponent.renderers[0].material;
            this.occupyingComponent.renderers[1].material = mat;
            this.occupyingComponent.renderers[2].material = mat;
            return true;
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

        /*
         * Attempt to load the new data into the renderer, returning whether it
         * succeeded.
         */
        protected override bool PatchRenderer()
        {
            if (!base.PatchRenderer())
            {
                return false;
            }
            var mat = this.occupyingComponent.renderers[0].material;
            Array.ForEach(this.occupyingComponent.renderers, x => x.material = mat);
            return true;
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
                Material mat = CustomBlocks.CreateMaterialFromImageData(this.imageData, this.CustomBlockType);
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
                    this.occupyingComponent.renderers[0].material = CustomBlocks.instance.defaultMaterials[this.CustomBlockType];
                }
                else
                {
                    this.occupyingComponent.renderers[1].material = CustomBlocks.instance.defaultMaterials[this.CustomBlockType];
                }
            }

            this.rendererPatched = true;
            if (this.sendUpdates)
            {
                this.GetComponent<CustomBlock_Network>()?.BroadcastChange(this.imageData);
            }
            return true;
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
                Material mat = CustomBlocks.CreateMaterialFromImageData(this.imageData, this.CustomBlockType);
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
                this.occupyingComponent.renderers[1].material = CustomBlocks.instance.defaultMaterials[this.CustomBlockType];
            }

            this.rendererPatched = true;
            if (this.sendUpdates)
            {
                this.GetComponent<CustomSail_Network>()?.BroadcastChange(this.imageData);
            }
            return true;
        }
    }
}
