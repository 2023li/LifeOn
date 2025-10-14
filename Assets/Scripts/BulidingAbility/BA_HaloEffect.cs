using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BA_HaloEffect : BulidingAbility
{
    private Vector3Int[] footprint = Array.Empty<Vector3Int>();
    private HaloEffectRangeValue[] appliedValues = Array.Empty<HaloEffectRangeValue>();
    private bool haloApplied;

    protected override void OnAdd()
    {
        haloApplied = false;
        footprint = Array.Empty<Vector3Int>();
        appliedValues = Array.Empty<HaloEffectRangeValue>();

        var def = building.Def;
        if (def == null)
        {
            return;
        }

        var values = def.HaloEffectValues;
        if (values == null || values.Length == 0)
        {
            return;
        }

        if (!TryCacheFootprint(out var cachedFootprint))
        {
            return;
        }

        if (GridSystem.Instance == null || GridSystem.Instance.Halo == null)
        {
            return;
        }

        footprint = cachedFootprint;
        appliedValues = (HaloEffectRangeValue[])values.Clone();

        foreach (var halo in appliedValues)
        {
            GridSystem.Instance.Halo.AddHaloEffect(footprint, halo);
        }

        haloApplied = true;
    }

    public override void Remove()
    {
        if (haloApplied && GridSystem.Instance != null && GridSystem.Instance.Halo != null)
        {
            foreach (var halo in appliedValues)
            {
                GridSystem.Instance.Halo.RemoveEffect(footprint, halo);
            }
        }

        haloApplied = false;
        footprint = Array.Empty<Vector3Int>();
        appliedValues = Array.Empty<HaloEffectRangeValue>();

        Destroy(this);
    }

    // Convert the building's world transform into occupied grid cells so we can apply halo effects.
    private bool TryCacheFootprint(out Vector3Int[] cachedFootprint)
    {
        cachedFootprint = Array.Empty<Vector3Int>();

        var gridSystem = GridSystem.Instance;
        if (gridSystem == null || gridSystem.mapGrid == null || building == null || building.Def == null)
        {
            return false;
        }

        var grid = gridSystem.mapGrid;

        Vector3 local = grid.WorldToLocal(building.transform.position);
        Vector3 center = grid.LocalToCellInterpolated(local);
        center.z = 0f;

        int size = Mathf.Max(1, building.Def.Size);
        List<Vector3Int> cells = CoordinateCalculator.GetBuildingCells(center, size);
        if (cells == null || cells.Count == 0)
        {
            return false;
        }

        cachedFootprint = cells.ToArray();
        return true;
    }
}
