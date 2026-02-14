using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RoosterAddon.Framework
{
    public static class ColorGradientGenerator
    {
        public static string GenerateGlowingColorGradient(string hexColor, int steps)
        {
            if (hexColor.StartsWith('#'))
            {
                hexColor = hexColor.Substring(1);
            }

            if (hexColor.Length != 6)
            {
                throw new ArgumentException("Hex color must be a six-digit code.");
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
            string result = string.Join(",", gradient.Select(c => c.ToHex()));
            return result;
        } // Extension method to convert a Color to a hex string

        public static string ToHex(this Color color)
        {
            return "#" + color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2");
        }
    }
}
