using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CommNetConstellation.UI
{
    /// <summary>
    /// Swiss Army knife for interface use cases
    /// </summary>
    public static class UIUtils
    {
        private static string _TextureDirectory = string.Empty; // one-time calculation for performance reason

        private static string TextureDirectory
        {
            get
            {
                if (_TextureDirectory.Length <= 0)
                    _TextureDirectory = $"{CNCSettings.AssemblyDLL.url.Replace("Plugins", "Textures")}/";
                return _TextureDirectory;
            }
        }

        /// <summary>
        /// Read KSP's GameDatabase for the desired texture
        /// </summary>
        public static Texture2D loadImage(string fileName)
        {
            Texture2D image;
            string fullFilePath = TextureDirectory + fileName;

            if (GameDatabase.Instance.ExistsTexture(fullFilePath))
            {
                image = GameDatabase.Instance.GetTexture(fullFilePath, false);
            }
            else
            {
                CNCLog.Error($"Cannot find the texture '{fileName}': {fullFilePath}");
                image = Texture2D.blackTexture;
            }

            return image;
        }

        /// <summary>
        /// Convert the color to hex string (#RRGGBB)
        /// </summary>
        //http://answers.unity3d.com/questions/1102232/how-to-get-the-color-code-in-rgb-hex-from-rgba-uni.html
        public static string colorToHex(Color thisColor)
        {
            return string.Format("#{0:X2}{1:X2}{2:X2}", toByte(thisColor.r), toByte(thisColor.g), toByte(thisColor.b));
        }

        private static byte toByte(float f)
        {
            f = Mathf.Clamp01(f);
            return (byte)(f * 255);
        }

        /// <summary>
        /// Create new texture and fill up with given color
        /// </summary>
        //https://forum.unity3d.com/threads/best-easiest-way-to-change-color-of-certain-pixels-in-a-single-sprite.223030/
        public static Texture2D createAndColorize(int width, int height, Color thisColor)
        {
            var newTexture = new Texture2D(width, height)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            for (int y = 0; y < newTexture.height; y++)
            {
                for (int x = 0; x < newTexture.width; x++)
                {
                    newTexture.SetPixel(x, y, thisColor);
                }
            }

            newTexture.Apply();
            return newTexture;
        }

        /// <summary>
        /// Fill the existing texture with the given color
        /// </summary>
        public static void colorize(Texture2D existingTexture, Color thisColor)
        {
            for (int y = 0; y < existingTexture.height; y++)
            {
                for (int x = 0; x < existingTexture.width; x++)
                {
                    existingTexture.SetPixel(x, y, thisColor);
                }
            }

            existingTexture.Apply();
        }

        /// <summary>
        /// Overlay two base and topmost textures to create a new texture
        /// </summary>
        public static Texture2D createAndOverlay(Texture2D baseTexture, Texture2D frontTexture)
        {
            var newTexture = new Texture2D(baseTexture.width, baseTexture.height, TextureFormat.ARGB32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            for (int y = 0; y < newTexture.height; y++)
            {
                for (int x = 0; x < newTexture.width; x++)
                {
                    if (frontTexture.GetPixel(x, y).a <= 0f) // transparent
                        newTexture.SetPixel(x, y, baseTexture.GetPixel(x, y));
                    else
                        newTexture.SetPixel(x, y, frontTexture.GetPixel(x, y));
                }
            }

            newTexture.Compress(true);
            newTexture.Apply(false, true);
            return newTexture;
        }

        /// <summary>
        /// Cursor detection within the given window
        /// </summary>
        public static bool ContainsMouse(Rect window)
        {
            return window.Contains(new Vector2(Input.mousePosition.x,
                Screen.height - Input.mousePosition.y));
        }

        /// <summary>
        /// Easy method to convert list to string
        /// </summary>
        public static string Concatenate<T>(IEnumerable<T> source, string delimiter = ",")
        {
            var result = new StringBuilder();
            bool first = true;
            foreach (var item in source)
            {
                if (first) first = false;
                else result.Append(delimiter);
                result.Append(item);
            }

            return result.ToString();
        }

        /// <summary>
        /// Round a given number to nearest metric factor
        /// </summary>
        public static string RoundToNearestMetricFactor(double number, int decimalPlaces = 0)
        {
            string formatStr = "{0:0}";
            if (decimalPlaces >= 1)
            {
                formatStr = "{0:0.";
                for (int i = 0; i < decimalPlaces; i++)
                    formatStr += "0";
                formatStr += "}";
            }

            if (number > Math.Pow(10, 9))
                return string.Format(formatStr + " G", number / Math.Pow(10, 9));
            else if (number > Math.Pow(10, 6))
                return string.Format(formatStr + " M", number / Math.Pow(10, 6));
            else if (number > Math.Pow(10, 3))
                return string.Format(formatStr + " k", number / Math.Pow(10, 3));
            else
                return string.Format(formatStr, number);
        }

        /// <summary>
        /// Duplicate and make given texture readable
        /// </summary>
        public static Texture2D getReadableCopy(Texture2D source)
        {
            //source: https://stackoverflow.com/questions/44733841/how-to-make-texture2d-readable-via-script
            RenderTexture tmp = RenderTexture.GetTemporary(
                source.width, source.height,
                0,
                RenderTextureFormat.Default,
                RenderTextureReadWrite.Linear);
            Graphics.Blit(source, tmp);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = tmp;

            Texture2D readable = new Texture2D(source.width, source.height, TextureFormat.ARGB32, false);
            readable.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
            readable.Apply(false, false); //latter false to keep readable

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(tmp);

            return readable;
        }

        /// <summary>
        /// Make new unreadble texture on subregion of a bigger texture
        /// Eg make a bunch of UI textures out of 1 giant texture of bunch subregions
        /// </summary>
        public static Texture2D createSubregionTexture(Texture2D source, int startX, int startY, int subregionWidth,
            int subregionHeight, bool compress = false, bool readable = false)
        {
            Texture2D newTexture = new Texture2D(subregionWidth, subregionHeight, TextureFormat.ARGB32, false);
            Color[] c = source.GetPixels(startX, startY, subregionWidth, subregionHeight, 0);
            newTexture.SetPixels(c, 0);
            if (compress)
            {
                newTexture.Compress(true);
            }

            newTexture.Apply(false, !readable); //unreadable state has performance boost

            return newTexture;
        }
    }
}