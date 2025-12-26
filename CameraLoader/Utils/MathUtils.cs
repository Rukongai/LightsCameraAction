using System;
using System.Numerics;

namespace CameraLoader.Utils;

public static class MathUtils
{
    public static float RadToDeg(float radians)
    {
        return (float)(radians * (180 / Math.PI));
    }

    public static float DegToRad(float degrees)
    {
        return (float)(degrees * (Math.PI / 180));
    }

    public static float GetHorizontalRotation(Vector3 position)
    {
        return (float)Math.Atan2(position.Z, -position.X);
    }

    public static float ConvertToRelative(float objRot, float playerRot)
    {
        objRot -= playerRot;

        while (objRot > Math.PI) { objRot -= (float)Math.Tau; }
        while (objRot < -Math.PI) { objRot += (float)Math.Tau; }

        return objRot;
    }

    public static float ConvertFromRelative(float relRot, float playerRot)
    {
        relRot += playerRot;

        while (relRot > Math.PI) { relRot -= (float)Math.Tau; }
        while (relRot < -Math.PI) { relRot += (float)Math.Tau; }

        return relRot;
    }

    public static (float, float) RotatePoint2D((float, float) position, float radians)
    {
        var posX = position.Item1;
        var posY = position.Item2;
        float sin = (float)Math.Sin(radians);
        float cos = (float)Math.Cos(radians);
        // X and Y are swapped because of the way this game handles coordinates
        position.Item2 = (posX * cos) - (posY * sin);
        position.Item1 = (posX * sin) + (posY * cos);
        return position;
    }

    public static float AddPiRad(float angle)
    {
        angle += (float)Math.PI;
        while (angle > Math.PI) { angle -= (float)Math.Tau; }
        return angle;
    }

    public static float SubPiRad(float angle)
    {
        angle -= (float)Math.PI;
        while (angle < Math.PI) { angle += (float)Math.Tau; }
        return angle;
    }

    /// <summary>
    /// Rotates a 3D vector around the Y axis (horizontal rotation in game coordinates)
    /// </summary>
    public static Vector3 RotateVectorHorizontal(Vector3 vector, float radians)
    {
        float sin = (float)Math.Sin(radians);
        float cos = (float)Math.Cos(radians);

        // Rotate around Y axis (game uses swapped X/Z coordinates)
        return new Vector3(
            (vector.X * cos) + (vector.Z * sin),
            vector.Y, // Y stays the same for horizontal rotation
            (vector.Z * cos) - (vector.X * sin)
        );
    }

    /// <summary>
    /// Converts a world-space position offset to a character-relative offset
    /// </summary>
    public static Vector3 ConvertPositionToRelative(Vector3 cameraPos, Vector3 characterPos, float characterRotation)
    {
        // Get the offset in world space
        Vector3 worldOffset = new Vector3(
            cameraPos.X - characterPos.X,
            cameraPos.Y - characterPos.Y,
            cameraPos.Z - characterPos.Z
        );

        // Rotate the offset by negative character rotation to get local space
        return RotateVectorHorizontal(worldOffset, -characterRotation);
    }

    /// <summary>
    /// Converts a character-relative offset to a world-space position
    /// </summary>
    public static Vector3 ConvertPositionFromRelative(Vector3 relativeOffset, Vector3 characterPos, float characterRotation)
    {
        // Rotate the local offset to world space
        Vector3 worldOffset = RotateVectorHorizontal(relativeOffset, characterRotation);

        // Add character position to get final world position
        return new Vector3(
            characterPos.X + worldOffset.X,
            characterPos.Y + worldOffset.Y,
            characterPos.Z + worldOffset.Z
        );
    }

    // Converting from values stored in DrawObjects to the ones displayed to the user
    public static Vector3 ConvertFloatsTo24BitColor(Vector3 color)
    {
        color = Vector3.SquareRoot(color) * 64f;
        for (int i = 0; i < 3; i++)
        {
            if (color[i] >= 128) { color[i]--; }
        }
        return color;
    }
}