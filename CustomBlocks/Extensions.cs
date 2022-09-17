using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;


namespace DestinyCustomBlocks
{
    static class ArrayExtension
    {
        public static T[] Extend<T>(this T[] arr, T newElement)
        {
            List<T> l = new List<T>(arr);
            l.Add(newElement);
            return l.ToArray();
        }
    }



    static class MaterialExtension
    {
        public static readonly string[] ShaderPropNames = new String[]
        {
            "_Diffuse",
            "_MetallicRPaintMaskGSmoothnessA",
            "_Normal"
        };

        /*
         * Takes a material without mip maps and creates one with mip maps.
         */
        public static Material CreateMipMapEnabled(this Material mat)
        {
            Material newMat = new Material(mat.shader);

            // Iterate the list of known shader properties to copy.
            foreach (string prop in ShaderPropNames)
            {
                // Get the original texture and create a new texture with mip
                // maps enabled.
                Texture2D originalTex = mat.GetTexture(prop) as Texture2D;
                Texture2D newTex = originalTex.CreateReadable(true);
                newTex.Apply(true, true);

                // Add it to the material.
                newMat.SetTexture(prop, newTex);
            }

            return newMat;
        }

        public static void FullDestroy(this Material mat)
        {
            if (mat != null)
            {
                UnityEngine.Object.DestroyImmediate(mat.GetTexture("_Diffuse"));
                UnityEngine.Object.DestroyImmediate(mat.GetTexture("_MetallicRPaintMaskGSmoothnessA"));
                UnityEngine.Object.DestroyImmediate(mat.GetTexture("_Normal"));
                UnityEngine.Object.DestroyImmediate(mat);
            }
        }
    }



    static class Texture2DExtension
    {
        public static void AddText(this Texture2D source, string text)
        {
            var temp = RenderTexture.GetTemporary(source.width, source.height, 0);
            temp.filterMode = FilterMode.Point;

            Camera cam = CustomBlocks.iconRenderer;
            cam.targetTexture = temp;
            cam.GetComponentInChildren<TMPro.TMP_Text>().text = text;
            cam.GetComponentInChildren<MeshRenderer>(true).material.SetTexture("_MainTex", source);
            cam.gameObject.SetActiveSafe(true);
            cam.Render();
            cam.gameObject.SetActiveSafe(false);

            var prev = RenderTexture.active;
            RenderTexture.active = temp;
            var area = new Rect(0, 0, temp.width, temp.height);
            area.y = temp.height - area.y - area.height;
            source.ReadPixels(area, 0, 0);
            source.Apply();
            RenderTexture.active = prev;

            cam.targetTexture = null;

            RenderTexture.ReleaseTemporary(temp);
        }

        // How is Aidan so amazing?
        public static Texture2D CreateReadable(this Texture2D source, bool mipChain = false, Rect? copyArea = null, RenderTextureFormat format = RenderTextureFormat.Default, RenderTextureReadWrite readWrite = RenderTextureReadWrite.Default, TextureFormat? targetFormat = null)
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
        public static void Edit(this Texture2D baseImg, Texture2D overlay, int xOffset, int yOffset, int targetX, int targetY, BlockType bt = BlockType.NONE, bool extend = false)
        {
            var w = targetX;
            var h = targetY;
            var mirrorX = CustomBlocks.MIRROR[bt].Item1 ? w - 1 : 0;
            var mirrorY = CustomBlocks.MIRROR[bt].Item2 ? h - 1 : 0;
            var baseWidth = baseImg.width;

            var pixels = baseImg.GetPixels();

            for (var x = 0; x < w; x++)
            {
                for (var y = 0; y < h; y++)
                {
                    pixels[xOffset + x + (yOffset + y) * baseWidth] = overlay.GetPixelBilinear(Math.Abs((float)(mirrorX - x) / w), Math.Abs((float)(mirrorY - y) / h));
                }
            }

            // This code only runs if we are extending the texture outwards.
            if (extend)
            {
                // This code extends the final pixel border outwards to help
                // with mip maps.
                // `i < borderSize` is the format.
                // Additionally, it is in a try block to stop it if it tries to
                // overflow the texture.
                for (int i = 0; i < 3; ++i)
                {
                    // Given that *a lot* of values are used over and over again
                    // and things get recalculated a lot, I'm storing a few of
                    // them here to try and save time.
                    var x1i = Math.Max(xOffset - (1 + i), 0);
                    var xti = Math.Min(xOffset + targetX + i, baseImg.width - 1);
                    var xt1 = Math.Min(xOffset + targetX - 1, baseImg.width - 1);

                    var y1i = Math.Max((yOffset - (1 + i)) * baseWidth, 0);
                    var yti = Math.Min((yOffset + targetY + i) * baseWidth, baseImg.height - 1);
                    var yt1 = Math.Min((yOffset + targetY - 1) * baseWidth, baseImg.height - 1);
                    var yw = Math.Min(yOffset * baseWidth, baseImg.height - 1);
                    var yt = Math.Min((yOffset + targetY) * baseWidth, baseImg.height - 1);

                    // Do the 4 corners.
                    pixels[x1i + y1i] = pixels[xOffset + yw];
                    pixels[xti + y1i] = pixels[xt1 + yw];
                    pixels[x1i + yt] = pixels[xOffset + yt1];
                    pixels[xti + yti] = pixels[xt1 + yt1];

                    for (int x = xOffset; x < xOffset + targetX; ++x)
                    {
                        pixels[x + y1i] = pixels[x + yw];
                        pixels[x + yti] = pixels[x + yt1];
                    }

                    for (int y = yOffset; y < yOffset + targetY; ++y)
                    {
                        int yy = y * baseWidth;
                        pixels[x1i + yy] = pixels[xOffset + yy];
                        pixels[xti + yy] = pixels[xt1 + yy];
                    }
                }
            }

            baseImg.SetPixels(pixels);
            baseImg.Apply();
        }

        /*
         * Returns a new Texture2D containing a portion of the original image.
         */
        public static Texture2D Cut(this Texture2D baseImg, int xOffset, int yOffset, int width, int height)
        {
            Texture2D tex = new Texture2D(width, height, baseImg.format, false);
            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    tex.SetPixel(x, y, baseImg.GetPixel(x + xOffset, y + yOffset));
                }
            }
            tex.Apply();
            return tex;
        }

        public static Texture2D Cut(this Texture2D baseImg, (int, int) xyOffset, (int, int) widthHeight)
        {
            return baseImg.Cut(xyOffset.Item1, xyOffset.Item2, widthHeight.Item1, widthHeight.Item2);
        }

        public static void Rotate(this Texture2D img, Rotation rot)
        {
            if (rot == Rotation.NONE)
            {
                // Don't change it.
                return;
            }
            Color32[] source = img.GetPixels32();
            Color32[] dest = new Color32[source.Length];
            int height = img.height;
            int width = img.width;
            switch(rot)
            {
                case Rotation.LEFT:
                    for (int y = 0; y < height; ++y)
                    {
                        for (int x = 0; x < width; ++x)
                        {
                            dest[(height - 1) - y + (x * height)] = source[x + (y * width)];
                        }
                    }
                    img.Resize(img.height, img.width);
                    break;
                case Rotation.RIGHT:
                    for (int y = 0; y < height; ++y)
                    {
                        for (int x = 0; x < width; ++x)
                        {
                            dest[y + ((width - 1 - x) * height)] = source[x + (y * width)];
                        }
                    }
                    img.Resize(img.height, img.width);
                    break;
                case Rotation.FLIP:
                    for (int y = 0; y < height; ++y)
                    {
                        for (int x = 0; x < width; ++x)
                        {
                            dest[((height - 1 - y) * width) + (width - 1 - x)] = source[x + (y * width)];
                        }
                    }
                    break;
            }
            img.SetPixels32(dest);
            img.Apply();
        }

        public static byte[] SanitizeImage(this byte[] arr, BlockType bt)
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

            (int, int) size = CustomBlocks.SIZES[bt];

            // Resize the image before saving the color data.
            Texture2D tex = new Texture2D(size.Item1, size.Item2);
            tex.Edit(texOriginal, 0, 0, size.Item1, size.Item2);

            byte[] bytes = tex.GetPixels32().ToByteArray();

            // Clean up our textures.
            UnityEngine.Object.DestroyImmediate(tex);
            UnityEngine.Object.DestroyImmediate(texOriginal);

            return bytes;
        }

        public static Texture2D ToTexture2D(this byte[] arr, int width, int height)
        {
            if (arr == null)
            {
                CustomBlocks.DebugLog("Failed to convert byte array to Texture2D: array was null.");
                return null;
            }
            if ((arr.Length & 3) != 0 || arr.Length != (width * height * 4))
            {
                CustomBlocks.DebugLog($"Failed to convert byte array to Texture2D: array was wrong length (expected {width * height * 4}, got {arr.Length}).");
                return null;
            }

            var tex = new Texture2D(width, height);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.SetPixels32(arr.ToColor32Array());
            return tex;
        }

        public static Color32[] ToColor32Array(this byte[] data)
        {
            if (data == null)
            {
                return null;
            }

            if ((data.Length & 3) != 0)
            {
                // If not a multiple of 4.
                return null;
            }

            Color32[] colors = new Color32[data.Length / 4];

            for (int i = 0; i < data.Length; ++i)
            {
                // Shortcut for i % 4;
                switch (i & 3)
                {
                    case 0:
                        colors[i >> 2].r = data[i];
                        break;
                    case 1:
                        colors[i >> 2].g = data[i];
                        break;
                    case 2:
                        colors[i >> 2].b = data[i];
                        break;
                    case 3:
                        colors[i >> 2].a = data[i];
                        break;
                }
            }
            return colors;
        }

        public static byte[] ToByteArray(this Color32[] colors)
        {
            byte[] bytes = new byte[colors.Length * 4];

            for (int i = 0; i < colors.Length; ++i)
            {
                bytes[i << 2] = colors[i].r;
                bytes[(i << 2) + 1] = colors[i].g;
                bytes[(i << 2) + 2] = colors[i].b;
                bytes[(i << 2) + 3] = colors[i].a;
            }

            return bytes;
        }

        public static byte[] FixPoster(this byte[] data, BlockType bt)
        {
            if (data == null)
            {
                return null;
            }

            if (data.Length == 0)
            {
                return data;
            }

            // Convert to an array of colors.
            Color32[] colors = data.ToColor32Array();

            if (colors == null)
            {
                return null;
            }

            // Create a texture which we will then resize.
            var size = CustomBlocks.SIZES[bt];
            Texture2D oldPoster = new Texture2D(size.Item1 * 2, size.Item2 * 2);
            oldPoster.SetPixels32(colors);
            Texture2D newPoster = new Texture2D(size.Item1, size.Item2);
            newPoster.Edit(oldPoster, 0, 0, size.Item1, size.Item2, BlockType.NONE, false);

            byte[] bytes = newPoster.GetPixels32().ToByteArray();

            UnityEngine.Object.DestroyImmediate(oldPoster);
            UnityEngine.Object.DestroyImmediate(newPoster);

            return bytes;
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
