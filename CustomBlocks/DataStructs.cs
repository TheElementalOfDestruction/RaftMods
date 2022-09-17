using System;
using UnityEngine;


namespace DestinyCustomBlocks
{
    // Class for storing data about a split image.
    public struct SplitImageData
    {
        // The location in the source image of the bottom right pixel.
        public ValueTuple<int, int> srcXY;
        // The width and height of the split.
        public ValueTuple<int, int> widthHeight;
        // The location in the destination for the bottom right pixel.
        public ValueTuple<int, int> destXY;
        // The rotation to use when placing it in the destination.
        public Rotation rotation;

        public SplitImageData((int, int) srcXY, (int, int) widthHeight, (int, int) destXY, Rotation rotation)
        {
            this.srcXY = srcXY;
            this.widthHeight = widthHeight;
            this.destXY = destXY;
            this.rotation = rotation;
        }
    }

    public struct PosterData
    {
        public int widthPixels;
        public int heightPixels;
        public float widthBlock;
        public float meshWidth;
        public float meshHeight;
        public float meshTop;
        public float meshBottom;
        public float meshRight;
        public float meshLeft;
        public float heightOffset;
        public Vector2[] uvs;
        public string ratio;
        public bool horizontal;

        /*
         * Height offset is for fixing the box collider being vertically.
         */
        public PosterData(string ratio, int widthPixels, int heightPixels, float widthBlock, float heightOffset)
        {
            this.widthPixels = widthPixels;
            this.heightPixels = heightPixels;
            this.widthBlock = widthBlock;
            this.ratio = ratio;
            this.heightOffset = heightOffset;

            // Calculations.
            this.meshWidth = widthBlock;
            this.meshHeight =  heightPixels * (widthBlock / (widthPixels));
            this.horizontal = widthPixels >= heightPixels;

            float uvTop = 1f;
            float uvBottom = 0;
            float uvLeft = 0;
            float uvRight = 1f;

            this.uvs = new Vector2[]
            {
                new Vector2(uvLeft, uvTop),
                new Vector2(uvRight, uvTop),
                new Vector2(uvRight, uvBottom),
                new Vector2(uvLeft, uvBottom),
                new Vector2(uvRight, uvTop),
                new Vector2(uvLeft, uvTop),
                new Vector2(uvLeft, uvBottom),
                new Vector2(uvRight, uvBottom)
            };

            this.meshTop = this.meshHeight;
            this.meshBottom = 0;
            this.meshRight = this.meshWidth / 2;
            this.meshLeft = -1 * this.meshRight;
        }

        public void AdjustBoxCollider(BoxCollider collider)
        {
            collider.size = new Vector3(0.01f, this.meshHeight, this.meshWidth);
            collider.center = new Vector3(0, this.heightOffset, 0);
        }

        public Mesh CreateMesh()
        {
            Mesh mesh = new Mesh();

            mesh.vertices = new Vector3[]
            {
                new Vector3(this.meshLeft, this.meshTop, 0),
                new Vector3(this.meshRight, this.meshTop, 0),
                new Vector3(this.meshRight, this.meshBottom, 0),
                new Vector3(this.meshLeft, this.meshBottom, 0),
                new Vector3(this.meshRight, this.meshTop, 0),
                new Vector3(this.meshLeft, this.meshTop, 0),
                new Vector3(this.meshLeft, this.meshBottom, 0),
                new Vector3(this.meshRight, this.meshBottom, 0),
            };
            mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3, 4, 5, 6, 4, 6, 7 };
            mesh.uv = this.uvs;

            return mesh;
        }

        public Material CreateMaterial()
        {
            Material ret = new Material(CustomBlocks.shader);
            Texture2D temp = new Texture2D(this.widthPixels, this.heightPixels);
            ret.SetTexture("_Diffuse", temp);
            ret.SetTexture("_MetallicRPaintMaskGSmoothnessA", temp);
            ret.SetTexture("_Normal", temp);

            return ret;
        }

        public Sprite CreateIcon()
        {
            // Load the base texture.
            Texture2D iconTex = new Texture2D(512, 512, TextureFormat.RGBA32, false);
            ImageConversion.LoadImage(iconTex, CustomBlocks.instance.GetEmbeddedFileBytes($"general_assets/{(this.horizontal ? "poster_icon_base_h.png" : "poster_icon_base_v.png")}"));

            // Place the text in the icon.
            iconTex.AddText(this.ratio);
            // Make unreadable.
            iconTex.Apply(true, true);

            // Create and return the sprite.
            return Sprite.Create(iconTex, new Rect(0, 0, 512, 512), new Vector2(0.5f, 0.5f));
        }
    }
}
