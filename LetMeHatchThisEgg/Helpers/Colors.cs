using System.Text.RegularExpressions;
using LetMeHatchThisEgg.Config;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mikew.BirdsOfAFeather.Helpers;
using StardewModdingAPI;
using StardewValley;
using static LetMeHatchThisEgg.EnumsAndStrings.Strings;

using static LetMeHatchThisEgg.ModEntry;

namespace LetMeHatchThisEgg.Helpers
{
    public class Colors
    {
        private readonly Dictionary<string, Dictionary<string, string>> _colorDictionary;
        private readonly Dictionary<string, string> _colorRelations;
        private readonly Random _random;

        private List<string> ColorsNames { get; }
        private List<string> ColorsHuesNames { get; }
        private List<string> ColorRelations { get; }
        private Dictionary<string, Dictionary<string, string>> GalaxyColors { get; }
        private Dictionary<string, string> AnimalEggColors { get; }

        //  private Dictionary<string, Dictionary<string, bool>> EggPalettes { get; }

        public Colors(ModConfig modConfig, SavedColors colors)
        {
            _colorDictionary = colors._colorDictionary;
            _colorRelations = colors._colorRelations;
            GalaxyColors = colors.GalaxyColors;
            AnimalEggColors = colors.ItemColors;
            ColorsNames = new List<string>(_colorDictionary.Keys) { "black", "white" };
            ColorsHuesNames = _colorDictionary.SelectMany(c => c.Value).Select(c => c.Key).ToList();
            ColorRelations = new List<string>(_colorRelations.Keys);
            _random = new Random();
        }

        public static string? GetColorFromTags(
            List<string> contextTags,
            float desaturateFactor = 0.2f
        )
        {
            var colorTag = contextTags.Find(ct => ct.StartsWith("color_"));
            if (colorTag == null)
            {
                return null;
            }
            return colorTag switch
            {
                "color_black" => Desaturate(new Color(45, 45, 45), desaturateFactor),
                "color_gray" => Desaturate(Color.Gray, desaturateFactor),
                "color_white" => Desaturate(Color.White, desaturateFactor),
                "color_pink" => Desaturate(new Color(255, 163, 186), desaturateFactor),
                "color_red" => Desaturate(new Color(220, 0, 0), desaturateFactor),
                "color_orange" => Desaturate(new Color(255, 128, 0), desaturateFactor),
                "color_yellow" => Desaturate(new Color(255, 230, 0), desaturateFactor),
                "color_green" => Desaturate(new Color(10, 143, 0), desaturateFactor),
                "color_blue" => Desaturate(new Color(46, 85, 183), desaturateFactor),
                "color_purple" => Desaturate(new Color(115, 41, 181), desaturateFactor),
                "color_brown" => Desaturate(new Color(130, 73, 37), desaturateFactor),
                "color_light_cyan" => Desaturate(new Color(180, 255, 255), desaturateFactor),
                "color_cyan" => Desaturate(Color.Cyan, desaturateFactor),
                "color_aquamarine" => Desaturate(Color.Aquamarine, desaturateFactor),
                "color_sea_green" => Desaturate(Color.SeaGreen, desaturateFactor),
                "color_lime" => Desaturate(Color.Lime, desaturateFactor),
                "color_yellow_green" => Desaturate(Color.GreenYellow, desaturateFactor),
                "color_pale_violet_red" => Desaturate(Color.PaleVioletRed, desaturateFactor),
                "color_salmon" => Desaturate(new Color(255, 85, 95), desaturateFactor),
                "color_jade" => Desaturate(new Color(130, 158, 93), desaturateFactor),
                "color_sand" => Desaturate(Color.NavajoWhite, desaturateFactor),
                "color_poppyseed" => Desaturate(new Color(82, 47, 153), desaturateFactor),
                "color_dark_red" => Desaturate(Color.DarkRed, desaturateFactor),
                "color_dark_orange" => Desaturate(Color.DarkOrange, desaturateFactor),
                "color_dark_yellow" => Desaturate(Color.DarkGoldenrod, desaturateFactor),
                "color_dark_green" => Desaturate(Color.DarkGreen, desaturateFactor),
                "color_dark_blue" => Desaturate(Color.DarkBlue, desaturateFactor),
                "color_dark_purple" => Desaturate(Color.DarkViolet, desaturateFactor),
                "color_dark_pink" => Desaturate(Color.DeepPink, desaturateFactor),
                "color_dark_cyan" => Desaturate(Color.DarkCyan, desaturateFactor),
                "color_dark_gray" => Desaturate(Color.DarkGray, desaturateFactor),
                "color_dark_brown" => Desaturate(Color.SaddleBrown, desaturateFactor),
                "color_gold" => Desaturate(Color.Gold, desaturateFactor),
                "color_copper" => Desaturate(new Color(179, 85, 0), desaturateFactor),
                "color_iron" => Desaturate(new Color(197, 213, 224), desaturateFactor),
                "color_iridium" => Desaturate(new Color(105, 15, 255), desaturateFactor),
                _ => null,
            };
        }

        // Helper function to desaturate a color
        public static string Desaturate(Color color, float desaturateFactor)
        {
            // Calculate the grayscale value of the color
            float gray = (color.R * 0.3f + color.G * 0.59f + color.B * 0.11f);

            // Desaturate the color by moving it towards the grayscale value
            byte r = (byte)(color.R + (gray - color.R) * desaturateFactor);
            byte g = (byte)(color.G + (gray - color.G) * desaturateFactor);
            byte b = (byte)(color.B + (gray - color.B) * desaturateFactor);

            // Create a new Color object with the desaturated values
            Color desaturatedColor = new Color(r, g, b);

            // Convert the desaturated Color to a hex code
            string hexCode =
                "#"
                + desaturatedColor.R.ToString("X2")
                + desaturatedColor.G.ToString("X2")
                + desaturatedColor.B.ToString("X2");

            return hexCode;
        }

        public static string GenerateGlowingColorGradient(string hexColor, int steps)
        {
            if (hexColor.StartsWith('#'))
            {
                hexColor = hexColor.Substring(1);
            }

            if (hexColor.Length != 6)
            {
                throw new ArgumentException($"Hex color must be a six-digit code: {hexColor}");
            }

            // Parse the input color
            int r = int.Parse(
                hexColor.Substring(0, 2),
                System.Globalization.NumberStyles.HexNumber
            );
            int g = int.Parse(
                hexColor.Substring(2, 2),
                System.Globalization.NumberStyles.HexNumber
            );
            int b = int.Parse(
                hexColor.Substring(4, 2),
                System.Globalization.NumberStyles.HexNumber
            );

            // Calculate the maximum possible brightness increase
            int maxDeltaR = 255 - r;
            int maxDeltaG = 255 - g;
            int maxDeltaB = 255 - b;

            // Create a gradient of 'teps' colors
            Color[] gradient = new Color[steps + 1];
            for (int i = 0; i <= steps; i++)
            {
                // Calculate the brightness factor
                float t = i / (float)steps;

                // If the color is close to black, desaturate it instead of increasing brightness
                if (r < 15 && g < 15 && b < 15)
                {
                    int rDesaturate = (int)(r / (1 - t * 0.2f));
                    int gDesaturate = (int)(g / (1 - t * 0.2f));
                    int bDesaturate = (int)(b / (1 - t * 0.2f));
                    gradient[i] = new Color(rDesaturate, gDesaturate, bDesaturate);
                }
                // If the color is close to white, saturate it instead of increasing brightness
                else if (r > 240 && g > 240 && b > 240)
                {
                    int rSaturate = (int)(r * (1 + t * 0.2f));
                    int gSaturate = (int)(g * (1 + t * 0.2f));
                    int bSaturate = (int)(b * (1 + t * 0.2f));
                    gradient[i] = new Color(rSaturate, gSaturate, bSaturate);
                }
                else
                {
                    int rGlow = (int)Math.Min(r + maxDeltaR * t, 255);
                    int gGlow = (int)Math.Min(g + maxDeltaG * t, 255);
                    int bGlow = (int)Math.Min(b + maxDeltaB * t, 255);
                    gradient[i] = new Color(rGlow, gGlow, bGlow);
                }
            }

            // Convert the gradient to a comma-delimited string
            string result = string.Join(",", gradient.Select(c => ColorToHex(c)));
            return result;
        } // Extension method to convert a Color to a hex string

        public string GetColor(
            string eggID,
            string stringToExtractColor,
            bool linkAnimalColorToEgg,
            Dictionary<string, bool> colors
        )
        {
            var color = GetHexCode(stringToExtractColor, linkAnimalColorToEgg, colors, eggID);
            return color;
        }

        private string GetHexCode(
            string stringToExtractColor,
            bool linkAnimalColorToEgg,
            Dictionary<string, bool> colors,
            string eggID
        )
        {
            if (eggID == "305")
            {
                return GetRandomHexCodeGalaxy();
            }
            if (linkAnimalColorToEgg)
            {
                // Try to extract a color name from the input string
                var colorName = ExtractColorName(stringToExtractColor, ColorsNames);
                if (colorName != null)
                {
                    return GetRandomHexCodeForColor(colorName);
                }

                // Try to extract a color hue from the input string
                colorName = ExtractColorHue(stringToExtractColor, ColorsHuesNames);
                if (colorName != null)
                {
                    string hexCode = string.Empty;
                    foreach (var color in _colorDictionary.Values)
                    {
                        foreach (var colorGroup in color)
                        {
                            if (colorGroup.Key.ToLower() == colorName.ToLower())
                            {
                                hexCode = colorGroup.Value;
                                break; // exit the inner loop
                            }
                        }
                        if (!string.IsNullOrEmpty(hexCode))
                        {
                            break; // exit the outer loop
                        }
                    }
                    return hexCode;
                }

                colorName = ExtractColorRelation(stringToExtractColor, ColorRelations);
                if (colorName != null)
                {
                    return GetRandomHexCodeForColor(_colorRelations[colorName]);
                }
            }
            return GetRandomHexCodeFromColors(colors);
        }

        private string? ExtractColorName(string input, IEnumerable<string> colorNames)
        {
            string pattern =
                @"\b(" + string.Join("|", colorNames.Select(Regex.Escape).Reverse()) + @")\b";
            Match match = Regex.Match(
                input,
                pattern,
                RegexOptions.IgnoreCase | RegexOptions.RightToLeft
            );
            var colorName = "";
            if (match.Success)
            {
                colorName = match.Value.ToLowerInvariant();
                return colorName == ColorNames.Black ? ColorNames.Gray : colorName;
            }
            else
            {
                return null;
            }
        }

        private string? ExtractColorHue(string input, IEnumerable<string> colorHues)
        {
            return colorHues.FirstOrDefault(color =>
                input.ToLowerInvariant().Contains(color.ToLowerInvariant())
            );
        }

        private string? ExtractColorRelation(string input, IEnumerable<string> colorRelations)
        {
            string pattern = string.Join("|", colorRelations.Select(Regex.Escape));
            Match match = Regex.Match(
                input,
                pattern,
                RegexOptions.IgnoreCase | RegexOptions.RightToLeft
            );
            return match.Success ? match.Value.ToLower() : null;
        }

        private string GetRandomHexCodeForColor(string colorName, bool repeatColor = false)
        {
            var availableHexCodes = _colorDictionary[colorName]
                .Select(x => x.Value)
                .Where(hex => repeatColor || !AnimalEggColors.ContainsValue(hex))
                .ToList();

            if (availableHexCodes.Any())
            {
                return availableHexCodes[_random.Next(availableHexCodes.Count)];
            }
            else
            {
                return GetRandomHexCodeForColor(colorName, true);
            }
        }

        public string GetRandomHexCodeFromColors(
            Dictionary<string, bool> colors,
            bool repeatColor = false,
            bool allColors = false
        )
        {
            var availableHexCodesPerColor = _colorDictionary
                .Where(x =>
                    (colors.ContainsKey(x.Key) && colors[x.Key] || allColors)
                    && x.Key != ColorNames.Gold
                )
                .ToDictionary(x => x.Key, x => x.Value);

            if (availableHexCodesPerColor.Count < 1)
            {
                return GetRandomHexCodeFromColors(colors, allColors: true);
            }
            var availableHexCodes = availableHexCodesPerColor
                .SelectMany(x => x.Value.Values)
                .Where(hex => !AnimalEggColors.ContainsValue(hex) || repeatColor)
                .ToList();

            if (availableHexCodes.Any())
            {
                return availableHexCodes[_random.Next(availableHexCodes.Count)];
            }
            else
            {
                return GetRandomHexCodeFromColors(colors, repeatColor: true);
            }
        }

        private string GetRandomHexCodeGalaxy(bool repeatColor = false)
        {
            var availableHexCodesDic = GalaxyColors
                .SelectMany(x =>
                    x.Value.Select(y => new
                    {
                        Category = x.Key,
                        Color = y.Key,
                        HexCode = y.Value
                    })
                )
                .GroupBy(x => $"{x.Category} {x.Color}")
                .Select(g => g.First())
                .ToDictionary(x => $"{x.Category} {x.Color}", x => x.HexCode);
            var availableHexCodes = availableHexCodesDic
                .Select(x => x.Value)
                .Where(hex => !AnimalEggColors.ContainsValue(hex) || repeatColor)
                .ToList();

            if (availableHexCodes.Any())
            {
                return availableHexCodes[_random.Next(availableHexCodes.Count)];
            }
            else
            {
                return GetRandomHexCodeGalaxy(true);
            }
        }

        public static string AdjustColor(
            string hex,
            float luminanceAdjustment,
            float saturationAdjustment
        )
        {
            Color color = HexToColor(hex);
            Vector3 hsv = RgbToHsv(color.R, color.G, color.B);

            // Adjust luminance (value)
            hsv.Z += luminanceAdjustment;

            // Adjust saturation
            hsv.Y *= saturationAdjustment;

            Color adjustedColor = HsvToRgb(hsv.X, hsv.Y, hsv.Z);
            return ColorToHex(adjustedColor);
        }

        // Helper methods
        public static Color HexToColor(string hex)
        {
            hex = hex.TrimStart('#');
            byte r = Convert.ToByte(hex.Substring(0, 2), 16);
            byte g = Convert.ToByte(hex.Substring(2, 2), 16);
            byte b = Convert.ToByte(hex.Substring(4, 2), 16);
            return new Color(r, g, b);
        }

        public static Vector3 RgbToHsv(byte r, byte g, byte b)
        {
            Color color = new Color(r, g, b);
            return ColorToHsv(color);
        }

        public static Vector3 ColorToHsv(Color color)
        {
            float r = color.R / 255f;
            float g = color.G / 255f;
            float b = color.B / 255f;

            float max = Math.Max(r, Math.Max(g, b));
            float min = Math.Min(r, Math.Min(g, b));
            float delta = max - min;

            float h = 0;
            if (delta != 0)
            {
                if (r == max)
                    h = (g - b) / delta;
                else if (g == max)
                    h = 2 + (b - r) / delta;
                else
                    h = 4 + (r - g) / delta;
                h *= 60;
                if (h < 0)
                    h += 360;
            }

            float s = max != 0 ? delta / max : 0;
            float v = max;

            return new Vector3(h, s, v);
        }

        public static Color HsvToRgb(float h, float s, float v)
        {
            int hi = (int)Math.Floor(h / 60) % 6;
            float f = h / 60 - hi;
            float p = v * (1 - s);
            float q = v * (1 - f * s);
            float t = v * (1 - (1 - f) * s);

            return hi switch
            {
                0 => new Color((int)(v * 255), (int)(t * 255), (int)(p * 255)),
                1 => new Color((int)(q * 255), (int)(v * 255), (int)(p * 255)),
                2 => new Color((int)(p * 255), (int)(v * 255), (int)(t * 255)),
                3 => new Color((int)(p * 255), (int)(q * 255), (int)(v * 255)),
                4 => new Color((int)(t * 255), (int)(p * 255), (int)(v * 255)),
                _ => new Color((int)(v * 255), (int)(p * 255), (int)(q * 255)),
            };
        }

        public static string ColorToHex(Color color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}".ToUpper();
        }

        private const float R_WEIGHT = 0.299f;

        private const float G_WEIGHT = 0.587f;

        private const float B_WEIGHT = 0.114f;

        public static Texture2D ConvertToGrayscale(Texture2D texture)
        {
            return ConvertToGrayscale(texture, null, 0, 1);
        }

        public static Texture2D ConvertToGrayscale(
            Texture2D texture,
            IMonitor monitor,
            float minLuminance,
            float maxLuminance
        )
        {
            Texture2D grayscaleTexture = new Texture2D(
                texture.GraphicsDevice,
                texture.Width,
                texture.Height,
                false,
                texture.Format
            );

            byte[] pixelData = new byte[
                texture.Width * texture.Height * GetBytesPerPixel(texture.Format)
            ];

            texture.GetData(pixelData);

            ConvertPixelDataToGrayscale(pixelData, texture.Format);

            if (monitor != null)
            {
                AdjustLuminance(pixelData, texture.Format, minLuminance, maxLuminance);
            }
            if (monitor != null)
            {
                AdjustLuminance(pixelData, texture.Format, minLuminance, maxLuminance);
            }

            grayscaleTexture.SetData(pixelData);

            return grayscaleTexture;
        }

        private static void ConvertPixelDataToGrayscale(byte[] pixelData, SurfaceFormat format)
        {
            int bytesPerPixel = GetBytesPerPixel(format);

            for (int i = 0; i < pixelData.Length; i += bytesPerPixel)
            {
                byte blue = pixelData[i];
                byte green = pixelData[i + 1];
                byte red = pixelData[i + 2];

                byte gray = (byte)(blue * B_WEIGHT + green * G_WEIGHT + red * R_WEIGHT);

                pixelData[i] = gray;
                pixelData[i + 1] = gray;
                pixelData[i + 2] = gray;
            }
        }

        private static void AdjustLuminance(
            byte[] pixelData,
            SurfaceFormat format,
            float minLuminance,
            float maxLuminance
        )
        {
            int bytesPerPixel = GetBytesPerPixel(format);
            float darkestPixel = float.MaxValue;
            float brightestPixel = float.MinValue;

            for (int i = 0; i < pixelData.Length; i += bytesPerPixel)
            {
                byte gray = pixelData[i];
                float luminance = gray / 255f;

                if (luminance < darkestPixel)
                    darkestPixel = luminance;

                if (luminance > brightestPixel)
                    brightestPixel = luminance;
            }

            if (darkestPixel < minLuminance)
            {
                // Brighten the image using gamma adjustment

                float gamma = (float)Math.Pow(minLuminance / darkestPixel, 1.2f);

                if (gamma > 2.0f)
                    gamma = 2.0f; // Cap gamma at 2.0 to avoid over-brightening

                for (int i = 0; i < pixelData.Length; i += bytesPerPixel)
                {
                    byte gray = pixelData[i];
                    float luminance = gray / 255f;

                    luminance = (float)Math.Pow(luminance, gamma);
                    gray = (byte)(luminance * 255);

                    pixelData[i] = gray;
                    pixelData[i + 1] = gray;
                    pixelData[i + 2] = gray;
                }
            }
            else if (brightestPixel > maxLuminance)
            {
                // Darken the image using gamma adjustment

                float gamma = (float)Math.Pow(maxLuminance / brightestPixel, 0.8f);

                if (gamma < 0.5f)
                    gamma = 0.5f; // Cap gamma at 0.5 to avoid over-darkening

                for (int i = 0; i < pixelData.Length; i += bytesPerPixel)
                {
                    byte gray = pixelData[i];
                    float luminance = gray / 255f;

                    luminance = (float)Math.Pow(luminance, 1 / gamma);
                    gray = (byte)(luminance * 255);

                    pixelData[i] = gray;
                    pixelData[i + 1] = gray;
                    pixelData[i + 2] = gray;
                }
            }
        }

        private static int GetBytesPerPixel(SurfaceFormat format)
        {
            switch (format)
            {
                case SurfaceFormat.Bgra5551:

                    return 2;

                case SurfaceFormat.Color:

                    return 4;

                default:

                    throw new ArgumentException("Unsupported pixel format");
            }
        }

        public static Texture2D CropTexture(
            Texture2D texture,
            Microsoft.Xna.Framework.Rectangle rectangle
        )
        {
            // Get the pixel format of the original texture
            SurfaceFormat format = texture.Format;

            // Create a new Texture2D with the same pixel format
            Texture2D croppedTexture;
            if (format == SurfaceFormat.Bgra5551 || format == SurfaceFormat.Color)
            {
                croppedTexture = new Texture2D(
                    texture.GraphicsDevice,
                    rectangle.Width,
                    rectangle.Height,
                    false, // mipMap
                    format // use the same pixel format as the original texture
                );

                // Get the pixel data from the original texture
                int dataSize = rectangle.Width * rectangle.Height;
                byte[] data;
                if (format == SurfaceFormat.Bgra5551)
                {
                    data = new byte[dataSize * 2]; // 16-bit format, 2 bytes per pixel
                    texture.GetData(0, rectangle, data, 0, dataSize * 2);
                }
                else if (format == SurfaceFormat.Color)
                {
                    data = new byte[dataSize * 4]; // 32-bit format, 4 bytes per pixel
                    texture.GetData(0, rectangle, data, 0, dataSize * 4);
                }
                else
                {
                    //LoggerHelper.VerboseLog($"Unsupported pixel format: {format}", LogLevel.Error);
                    throw new ArgumentException("Unsupported pixel format");
                }

                // Set the pixel data on the new texture
                croppedTexture.SetData(data);
            }
            else
            {
                //LoggerHelper.VerboseLog($"Unsupported pixel format: {format}", LogLevel.Error);
                throw new ArgumentException("Unsupported pixel format");
            }

            return croppedTexture;
        }

        public static Texture2D ColorImage(Texture2D texture, string hexColor)
        {
            try
            {
                Color color = HexToColor(hexColor);
                Color[] data = new Color[texture.Width * texture.Height];
                texture.GetData(data);

                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = new Color(
                        (byte)(data[i].R * color.R / 255f),
                        (byte)(data[i].G * color.G / 255f),
                        (byte)(data[i].B * color.B / 255f),
                        data[i].A
                    );
                }

                Texture2D result =
                    new(Game1.graphics.GraphicsDevice, texture.Width, texture.Height);
                result.SetData(data);
                return result;
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as necessary
                LoggerHelper.VerboseLog($"Error in ColorImage: {ex.Message}");
                return texture; // Return null in case of an error
            }
        }

        public static Texture2D CloneTexture(Texture2D originalTexture)
        {
            // Create a new texture with the same dimensions as the original

            Texture2D clonedTexture = new Texture2D(
                originalTexture.GraphicsDevice,
                originalTexture.Width,
                originalTexture.Height
            );

            // Get the pixel data from the original texture

            Color[] pixels = new Color[originalTexture.Width * originalTexture.Height];

            originalTexture.GetData(pixels);

            // Set the pixel data on the new texture

            clonedTexture.SetData(pixels);

            return clonedTexture;
        }

        public static Texture2D PatchTexture(
            Texture2D target,
            Texture2D source,
            Rectangle? targetArea = null
        )
        {
            // If targetArea is not specified, use the entire target texture

            Rectangle targetRect = targetArea ?? new Rectangle(0, 0, target.Width, target.Height);

            // Get the pixel data from the source texture

            Color[] sourcePixels = new Color[source.Width * source.Height];

            source.GetData(sourcePixels);

            // Get the pixel data from the target texture

            Color[] targetPixels = new Color[target.Width * target.Height];

            target.GetData(targetPixels);

            // Calculate the source area to copy from

            Rectangle sourceRect = new Rectangle(0, 0, source.Width, source.Height);

            // Copy pixels from the source texture to the target texture

            for (int y = 0; y < targetRect.Height; y++)
            {
                for (int x = 0; x < targetRect.Width; x++)
                {
                    int sourceX = x - targetRect.X;

                    int sourceY = y - targetRect.Y;

                    if (
                        sourceX >= 0
                        && sourceX < sourceRect.Width
                        && sourceY >= 0
                        && sourceY < sourceRect.Height
                    )
                    {
                        int targetIndex = (y + targetRect.Y) * target.Width + x + targetRect.X;

                        int sourceIndex = sourceY * sourceRect.Width + sourceX;

                        targetPixels[targetIndex] = sourcePixels[sourceIndex];
                    }
                }
            }

            // Set the updated pixel data back to the target texture

            target.SetData(targetPixels);
            return target;
        }
    }

    public sealed class SavedColors
    {
        public Dictionary<string, string> ItemColors { get; set; } = new();
        public Dictionary<string, Dictionary<string, string>> _colorDictionary { get; set; }
        public Dictionary<string, string> _colorRelations { get; set; }
        public Dictionary<string, Dictionary<string, string>> GalaxyColors { get; set; }

        public SavedColors()
        {
            _colorRelations = new Dictionary<string, string>
            {
                { "albino", ColorNames.White },
                { "cream", ColorNames.White },
                { "pale", ColorNames.White },
                { "charcoal", ColorNames.Black },
                { "crimson", ColorNames.Red },
                { "sandstone", ColorNames.Yellow },
                { "clay", ColorNames.Brown },
                { "terracotta", ColorNames.Red },
                { "gypsum", ColorNames.White },
                { "alabaster", ColorNames.White },
                { "porcelain", ColorNames.White },
                { "bone", ColorNames.White },
                { "blush", ColorNames.Pink },
                { "rose", ColorNames.Red },
                { "coral", ColorNames.Orange },
                { "salmon", ColorNames.Orange },
                { "peach", ColorNames.Orange },
                { "cinnamon", ColorNames.Brown },
                { "tawny", ColorNames.Orange },
                { "sienna", ColorNames.Red },
                { "ochre", ColorNames.Brown },
                { "umber", ColorNames.Brown },
                { "cobalt", ColorNames.Blue },
                { "turquoise", ColorNames.Blue },
                { "emerald", ColorNames.Green },
                { "sage", ColorNames.Green },
                { "moss", ColorNames.Green },
                { "silver", ColorNames.Gray },
                { "gold", ColorNames.Yellow },
                { "copper", ColorNames.Orange },
                { "bronze", ColorNames.Brown },
                { "tan", ColorNames.Brown },
                { "beige", ColorNames.White },
                { "mocha", ColorNames.Brown },
                { "lavender", ColorNames.Purple },
                { "levender", ColorNames.Purple },
                { "lilac", ColorNames.Purple },
                { "mint", ColorNames.Green },
                { "teal", ColorNames.Blue },
                { "aqua", ColorNames.Blue },
                { "navy", ColorNames.Blue },
                { "forest", ColorNames.Green },
                { "olive", ColorNames.Green },
                { "camel", ColorNames.Brown },
                { "sand", ColorNames.Yellow },
            };
            _colorDictionary = new Dictionary<string, Dictionary<string, string>>
            {
                // red
                {
                    ColorNames.Red,
                    new Dictionary<string, string>()
                    {
                        { "burgundy", "#8B0A1A" },
                        { "crimson", "#DC143C" },
                        { "maroon", "#A02340" },
                        { "fire engine", "#FF0000" },
                        { "tomato red", "#FF6347" },
                        { "ruby", "#E0115F" },
                        { "muted", "#CB4C4E" },
                        { "russet", "#A64C4C" },
                        { "burnt red", "#E5763D" },
                        { "garnet", "#733635" }
                    }
                },
                // pink
                {
                    ColorNames.Pink,
                    new Dictionary<string, string>()
                    {
                        { "baby pink", "#FFC0CB" },
                        { "blush", "#FFB6C1" },
                        { "rose", "#FFC5C5" },
                        { "blushing bloom", "#FFC9C9" },
                        { "magenta", "#FF00FF" },
                        { "frosted fuchsia", "#C154C1" },
                        { "lilac", "#C8A2C8" },
                        { "orchid", "#DA70D6" },
                        { "blushy", "#DE98B2" },
                        { "rose petal", "#f0d4b9" },
                        { "muted fuchsia", "#dbacff" }
                    }
                },
                // brown
                {
                    ColorNames.Brown,
                    new Dictionary<string, string>()
                    {
                        { "chocolate", "#964B00" },
                        { "espresso", "#663300" },
                        { "walnut", "#A52A2A" },
                        { "pecan", "#C9A664" },
                        { "mushroom", "#786644" },
                        { "sandy shore", "#F5F5DC" },
                        { "sienna", "#A0522D" },
                        { "caramel", "#F5DEB3" },
                        { "honey", "#FFC394" },
                        { "golden brown", "#F2C464" }
                    }
                },
                // orange
                {
                    ColorNames.Orange,
                    new Dictionary<string, string>()
                    {
                        { "peachy keen", "#FFD7BE" },
                        { "apricot bliss", "#ffb16d" },
                        { "mandarim", "#f37a48" },
                        { "pumpkin", "#FF7F24" },
                        { "nectarine", "#FFC499" },
                        { "peach", "#FFD7BE" },
                        { "light salmon", "#FFA07A" },
                        { "marmalade", "#FFA57D" },
                        { "burnt ember", "#E2786F" },
                        { "spiced cranberry", "#E2553F" }
                    }
                },
                // green
                {
                    ColorNames.Green,
                    new Dictionary<string, string>()
                    {
                        { "pale pistachio", "#C7F464" },
                        { "jade oasis", "#BCE3C5" },
                        { "misty morning", "#C2E0B3" },
                        { "mint", "#ACFFAC" },
                        { "mint to be", "#B2FFFC" },
                        { "cyan green", "#45B370" },
                        { "olive branch", "#556B2F" },
                        { "sagebrush", "#BCE3C5" },
                        { "moss", "#8B9467" },
                        { "faded leaf", "#6A8F3A" },
                        { "faded fern", "#5C715E" },
                        { "seafoam mist", "#6C9E85" },
                        { "avocado", "#568203" },
                        { "lime", "#32CD32" },
                        { "jade", "#00A86B" },
                        { "forest", "#228B22" },
                        { "hunter", "#3F7F4E" },
                        { "pine", "#2F4F4F" }
                    }
                },
                // blue
                {
                    ColorNames.Blue,
                    new Dictionary<string, string>()
                    {
                        { "iceberg", "#44B9FF" },
                        { "navy frost", "#2F4F7F" },
                        { "cobalt", "#0047AB" },
                        { "navy", "#03055B" },
                        { "cosmic", "#2C3E50" },
                        { "shadow", "#212931" },
                        { "dusk", "#6A5ACD" },
                        { "riverbed", "#8497B0" },
                        { "dusk blue", "#87A9C7" },
                        { "royal", "#4169E1" },
                        { "cerulean", "#98B2D1" },
                        { "azure", "#007FFF" },
                        { "baby blue", "#A1C9F2" },
                        { "powder blue", "#7ee6ce" },
                        { "sky blue", "#87CEEB" }
                    }
                },
                // yellow
                {
                    ColorNames.Yellow,
                    new Dictionary<string, string>()
                    {
                        { "homebase warm", "#f6cd8b" },
                        { "custard", "#FFC68B" },
                        { "mossy stone", "#C9C464" },
                        { "cream", "#FFFDD0" },
                        { "goldenrod", "#DAA520" },
                        { "lemon zest", "#F2C070" },
                        { "daffodil", "#F7DC6F" },
                        { "beige", "#eddcb9" },
                        { "soft sulfur", "#F2F2C4" },
                        { "bourbon", "#FF9900" },
                        { "caramel apple", "#FFC27D" }
                    }
                },
                // gold
                {
                    ColorNames.Gold,
                    new Dictionary<string, string>()
                    {
                        { "saffron", "#F5C070" },
                        { "golden glow", "#FFD3B5" },
                        { "antique", "#C9B457" },
                        { "brass", "#C7B464" },
                        { "beige gold", "#F0E4B4" },
                        { "light bronze", "#F2F2A4" },
                        { "rich gold", "#FFD200" },
                        { "old gold", "#58493B" }
                    }
                },
                // purple
                {
                    ColorNames.Purple,
                    new Dictionary<string, string>()
                    {
                        { "muted mauve", "#BFB8E6" },
                        { "muted magenta", "#A8C3E6" },
                        { "lavender dream", "#C7B8EA" },
                        { "lavender", "#9A7EA5" },
                        { "grape", "#644164" },
                        { "periwinkle", "#C5BFFF" },
                        { "eggplant", "#3B3F4E" },
                        { "dark violet", "#3B0069" },
                        { "wine", "#722F37" },
                        { "plum", "#663e66" },
                        { "wine stain", "#60447d" },
                        { "grape harvest", "#3A2C5E" },
                        { "mulberry", "#C54B8C" },
                        { "pomegranate punch", "#C9483F" },
                        { "rich raspberry", "#E1474C" },
                        { "muted wine", "#7A6F8F" },
                        { "velvet night", "#4B2E83" },
                        { "dull eggplant", "#6F6C7B" }
                    }
                },
                // gray
                {
                    ColorNames.Gray,
                    new Dictionary<string, string>()
                    {
                        { "misty morning", "#E2E2E2" },
                        { "dove gray", "#B5B8CF" },
                        { "rustic gray", "#9A9A9A" },
                        { "ash", "#CCCCCC" },
                        { "muted clay", "#B2B2B2" },
                        { "dove", "#D3D3D3" },
                        { "rosewood", "#C9C4B5" },
                        { "warm slate", "#7A7A7A" },
                        { "taupe", "#b9a281" },
                        { "misty dawn", "#C5C5C5" },
                        { "sea salt", "#4E5154" },
                        { "frostbite", "#87A2B5" },
                        { "charcoal", "#333333" },
                        { "gunmetal", "#2C3539" },
                        { "slate", "#708090" }
                    }
                },
                // gray
                {
                    ColorNames.White,
                    new Dictionary<string, string>()
                    {
                        { "Pastel Pink", "#E6DAC3" },
                        { "Baby Blue", "#A1C9F2" },
                        { "Mint Green", "#B2FFFC" },
                        { "Powder Peach", "#FFD7BE" },
                        { "Soft Lavender", "#C7B8EA" },
                        { "Pale Yellow", "#F7DC6F" }
                    }
                }
            };
            GalaxyColors = new Dictionary<string, Dictionary<string, string>>
            {
                {
                    ColorCategories.Warm,
                    new Dictionary<string, string>
                    {
                        { "orange nebula", "#FFC080" },
                        { "red sunset", "#FFA07A" },
                        { "yellow golden", "#F8E231" },
                    }
                },
                {
                    ColorCategories.Cool,
                    new Dictionary<string, string>
                    {
                        { "blue galactic", "#3498DB" },
                        { "blue cosmic", "#2E4053" },
                        { "black stellar", "#1A1D23" },
                        { "blue green aurora", "#66CCCC" }
                    }
                },
                {
                    ColorCategories.Bright,
                    new Dictionary<string, string>
                    {
                        { "pink asteroid", "#FF69B4" },
                        { "orange comet", "#FFC67D" },
                        { "magenta nova", "#FF99FF" },
                        { "green quasar", "#33CC33" }
                    }
                }
            };
        }
    }
}
