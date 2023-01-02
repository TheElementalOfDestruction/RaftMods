using System;
using System.Collections;
using System.Collections.Generic;
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
        public string ratio;
        public bool horizontal;
        // How many points should be between the corners.
        public int divisions;

        /*
         * Height offset is for fixing the box collider being vertically.
         */
        public PosterData(string ratio, int widthPixels, int heightPixels, float widthBlock, float heightOffset, int divisions = 8)
        {
            this.widthPixels = widthPixels;
            this.heightPixels = heightPixels;
            this.widthBlock = widthBlock;
            this.ratio = ratio;
            this.heightOffset = heightOffset;
            this.divisions = divisions;

            // Calculations.
            this.meshWidth = widthBlock;
            this.meshHeight =  heightPixels * (widthBlock / (widthPixels));
            this.horizontal = widthPixels >= heightPixels;

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

        public int[] CalculateTriangles()
        {
            List<int> points = new List<int>();
            int squaresLength = this.divisions + 1;
            int squares = (this.divisions + 1) * (this.divisions + 1);
            // Use the following code in a loop to figure out the triangle
            // points.
            for (int i = 0; i < squares; ++i)
            {
                int topLeft = (i / squaresLength) + i;
                int topRight = topLeft + 1;
                int bottomLeft = topLeft + squaresLength + 1;
                int bottomRight = bottomLeft + 1;
                // First triangle.
                points.Add(topLeft);
                points.Add(topRight);
                points.Add(bottomRight);
                // Second triangle.
                points.Add(topLeft);
                points.Add(bottomRight);
                points.Add(bottomLeft);
            }

            int numPoints = squares * 6;
            int backPointOffset = (this.divisions + 2) * (this.divisions + 2);

            // Use the original points list to create the points for the back.
            for (int i = 0; i < numPoints; i += 3)
            {
                // Add the triangle with the indicies reversed, using the back
                // points.
                points.Add(backPointOffset + points[i + 2]);
                points.Add(backPointOffset + points[i + 1]);
                points.Add(backPointOffset + points[i]);
            }

            return points.ToArray();
        }

        public Mesh CreateMesh()
        {
            float uvTop = 1f;
            float uvBottom = 0;
            float uvLeft = 0;
            float uvRight = 1f;

            // Prepare to generate the UVs and vertices.

            int numPointsSide = this.divisions + 2;
            List<float> locationsV = new List<float>() { this.meshTop };
            List<float> locationsH = new List<float>() { this.meshLeft };
            List<float> uvV = new List<float>() { uvTop };
            List<float> uvH = new List<float>() { uvLeft };

            int divisor = numPointsSide - 1;

            // Create the lines.
            for (int i = 1; i < numPointsSide - 1; ++i)
            {
                float ratio = i / (float)divisor;
                locationsV.Add(this.meshTop - (ratio * this.meshHeight));
                uvV.Add(uvTop - (ratio * uvTop));
                locationsH.Add(this.meshLeft + (ratio * this.meshWidth));
                uvH.Add(ratio);
            }

            // Add the bottom line.
            locationsV.Add(this.meshBottom);
            uvV.Add(uvBottom);
            // Add the right line.
            locationsH.Add(this.meshRight);
            uvH.Add(uvRight);

            List<Vector2> uvsList = new List<Vector2>();
            List<Vector3> verticesList = new List<Vector3>();

            // Generate the vertices and UVs at the same time.
            for (int i = 0; i < 2; ++i)
            {
                // Run this loop twice to create the front and back.
                for (int x = 0; x < numPointsSide; ++x)
                {
                    for (int y = 0; y < numPointsSide; ++y)
                    {
                        uvsList.Add(new Vector2(uvH[x], uvV[y]));
                        verticesList.Add(new Vector3(locationsH[x], locationsV[y], 0));
                    }
                }
            }

            Mesh mesh = new Mesh();

            mesh.vertices = verticesList.ToArray();
            mesh.triangles = this.CalculateTriangles();

            mesh.uv = uvsList.ToArray();

            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();

            // Make the mesh non-readable so it stops taking up extra memory.
            mesh.UploadMeshData(true);

            return mesh;
        }

        public Material CreateMaterial()
        {
            Material ret = new Material(CustomBlocks.standardShader);
            Texture2D temp = new Texture2D(this.widthPixels, this.heightPixels);
            ret.SetTexture("_BumpMap", temp);
            ret.SetTexture("_MainTex", temp);
            ret.SetTexture("_MetallicGlossMap", temp);

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
