﻿using System;
using UnityEngine;

[Serializable]
public struct HexCoordinates
{
    // If you add all three coordinates together you will always get zero. 
    // If you increment one coordinate, you have to decrement another. 
    // Indeed, this produces six possible directions of movement. 
    // These coordinates are typically known as cube coordinates, as they are three-dimensional and the topology resembles a cube.
    public int X
    {
        get
        {
            return _x;
        }
    }

    public int Z
    {
        get
        {
            return _z;
        }
    }

    // As we already store the X and Z coordinates, we don't need to store the Y coordinate. 
    // We can include a property that computes it on demand.
    public int Y
    {
        get
        {
            return -X - Z;
        }
    }

    [SerializeField]
    int _x, _z;

    public HexCoordinates(int x, int z)
    {
        _x = x;
        _z = z;
    }

    // Let's fix out those X coordinates so they are aligned along a straight axis. 
    // We can do this by undoing the horizontal shift.
    // The result is typically know as axial coordinates.
    public static HexCoordinates FromOffsetCoordinates(int x, int z) => new HexCoordinates(x - z / 2, z);

    public override string ToString() => "(" + X.ToString() + ", " + Y.ToString() + ", " + Z.ToString() + ")";

    public string ToStringOnSeparateLines() => X.ToString() + "\n" + Y.ToString() + "\n" + Z.ToString();

    public static HexCoordinates FromPosition(Vector3 position)
    {
        float x = position.x / (HexMetrics.InnerRadius * 2f);
        float y = -x;

        // we have to shift as we move along Z. Every two rows we should shift an entire unit to the left
        float offset = position.z / (HexMetrics.OuterRadius * 3f);
        x -= offset;
        y -= offset;

        // Rounding them to integers we should get the coordinates. We derive Z as well and then construct the final coordinates.
        int iX = Mathf.RoundToInt(x);
        int iY = Mathf.RoundToInt(y);
        int iZ = Mathf.RoundToInt(-x - y);

        // rounding error may sometimes occur
        if (iX + iY + iZ != 0)
        {
            // The solution then becomes to discard the coordinate with the largest rounding delta, and reconstruct it from the other two. 
            // But as we only need X and Z, we don't need to bother with reconstructing Y.
            float dX = Mathf.Abs(x - iX);
            float dY = Mathf.Abs(y - iY);
            float dZ = Mathf.Abs(-x - y - iZ);

            if (dX > dY && dX > dZ)
                iX = -iY - iZ;
            else if (dZ > dY)
                iZ = -iX - iY;
        }

        return new HexCoordinates(iX, iZ);
    }
}