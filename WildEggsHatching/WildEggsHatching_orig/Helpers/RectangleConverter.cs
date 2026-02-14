using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

public class RectangleConverter
{
    public static HashSet<Rectangle> ConvertToRectangleHashSet(HashSet<string> stringSet)
    {
        HashSet<Rectangle> rectangleSet = new HashSet<Rectangle>();

        foreach (var str in stringSet)
        {
            // Split the string by the semicolon
            var coordinates = str.Split(';');
            if (coordinates.Length == 2)
            {
                try
                {
                    // Parse the top-left coordinates
                    var topLeft = ParseVector2(coordinates[0]);
                    // Parse the bottom-right coordinates
                    var bottomRight = ParseVector2(coordinates[1]);

                    // Create a Rectangle from the parsed coordinates
                    Rectangle rectangle = new Rectangle(
                        (int)topLeft.X,
                        (int)topLeft.Y,
                        (int)(bottomRight.X - topLeft.X),
                        (int)(bottomRight.Y - topLeft.Y)
                    );

                    rectangleSet.Add(rectangle);
                }
                catch (FormatException)
                {
                    // Handle parsing errors (e.g., log or ignore)
                    Console.WriteLine($"Invalid format for coordinates: {str}");
                }
            }
            else
            {
                Console.WriteLine($"Invalid coordinate pair: {str}");
            }
        }

        return rectangleSet;
    }

    private static Vector2 ParseVector2(string vectorString)
    {
        var components = vectorString.Trim().Split(',');
        if (components.Length != 2)
        {
            throw new FormatException("Vector2 must have two components.");
        }

        float x = float.Parse(components[0]);
        float y = float.Parse(components[1]);

        return new Vector2(x, y);
    }
}
