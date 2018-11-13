﻿using UnityEngine;

public class HexCell : MonoBehaviour
{
    public int Elevation
    {
        get
        {
            return _elevation;
        }
        set
        {
            _elevation = value;
            Vector3 position = transform.localPosition;
            position.y = value * HexMetrics.ElevationStep;
            position.y += (HexMetrics.SampleNoise(position).y * 2f - 1f) * HexMetrics.ElevationPerturbStrength;

            transform.localPosition = position;

            Vector3 uiPosition = uiRect.localPosition;
            uiPosition.z = -position.y;
            uiRect.localPosition = uiPosition;
        }
    }
    int _elevation;

    public Vector3 Position
    {
        get
        {
            return transform.localPosition;
        }
    }

    public Color Color;
    public HexCoordinates Coordinates;

    [SerializeField]
    HexCell[] _neighbors;

    public RectTransform uiRect;

    public HexCell GetNeighbor(HexDirection direction) => _neighbors[(int)direction];

    public void SetNeighbor(HexDirection direction, HexCell cell)
    {
        _neighbors[(int)direction] = cell;
        cell._neighbors[(int)direction.Opposite()] = this;
    }

    public HexEdgeType GetEdgeType(HexDirection direction) 
        => HexMetrics.GetEdgeType(Elevation, _neighbors[(int)direction].Elevation);

    public HexEdgeType GetEdgeType(HexCell otherCell) 
        => HexMetrics.GetEdgeType(Elevation, otherCell.Elevation);
}