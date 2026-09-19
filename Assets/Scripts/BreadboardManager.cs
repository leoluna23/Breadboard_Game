using System.Collections.Generic;
using UnityEngine;

// ------------------------------------------------------------
// BreadboardManager.cs
//
// Purely visual grid generator. Lives on its own empty GameObject,
// separate from the breadboard background sprite and separate from
// the electrical model (Breadboard.cs / Circuit.cs) — this script
// doesn't know or care what a "node" is, it just places dots.
//
// The board is made of 3 independently configurable grid regions,
// each with its own origin, row/column counts, row/column spacing,
// and an optional center gap that splits the region in half along
// either axis:
//   - Positive Rails  (the + power rail lines, split left/right)
//   - Negative Rails  (the - power rail lines, split left/right)
//   - Center Rails    (the big terminal-strip block in the middle)
//
// Tune each region in the Inspector (even live, in Play mode) until
// the generated dots land on your groupmate's sprite.
// ------------------------------------------------------------

public enum GapAxis { Row, Column }

[System.Serializable]
public class GridRegion {
    public string name = "Region";

    [Tooltip("World position of this region's TOP-LEFT corner. Each region is positioned independently.")]
    public Vector2 origin = Vector2.zero;

    [Tooltip("Number of hole rows in this region.")]
    public int rows = 2;

    [Tooltip("Number of hole columns in this region.")]
    public int columns = 30;

    [Tooltip("Distance between adjacent rows.")]
    public float rowSpacing = 0.25f;

    [Tooltip("Distance between adjacent columns.")]
    public float columnSpacing = 0.25f;

    [Tooltip("Extra gap inserted once, at the halfway point along Center Gap Axis, splitting this region's dots into two halves. Set to 0 for no gap.")]
    public float centerGapSpacing = 0f;

    [Tooltip("Which axis the center gap splits: Column for a left/right split (e.g. rail segments), Row for a top/bottom split (e.g. the bank trench).")]
    public GapAxis centerGapAxis = GapAxis.Column;

    public IEnumerable<(int row, int col, Vector3 localPosition)> GenerateSlots() {
        for (int row = 0; row < rows; row++) {
            float x = row * rowSpacing;
            if (centerGapAxis == GapAxis.Row && row >= rows / 2) x += centerGapSpacing;

            for (int col = 0; col < columns; col++) {
                float y = col * columnSpacing;
                if (centerGapAxis == GapAxis.Column && col >= columns / 2) y += centerGapSpacing;

                yield return (row, col, new Vector3(origin.x + x, origin.y - y, 0f));
            }
        }
    }
}

public class HoleView : MonoBehaviour {
    public string RegionName { get; private set; }
    public int Row { get; private set; }
    public int Column { get; private set; }

    public void Init(string regionName, int row, int col) {
        RegionName = regionName;
        Row = row;
        Column = col;
    }
}

public class BreadboardManager : MonoBehaviour {
    [Header("Hole prefab")]
    public GameObject holePrefab;

    [Header("Grid Regions")]
    public GridRegion positiveRails = new GridRegion { name = "PositiveRails", rows = 2, centerGapAxis = GapAxis.Column };
    public GridRegion negativeRails = new GridRegion { name = "NegativeRails", rows = 2, centerGapAxis = GapAxis.Column };
    public GridRegion centerRails = new GridRegion { name = "CenterRails", rows = 10, centerGapAxis = GapAxis.Row };

    private readonly List<HoleView> _holeViews = new();

    private IEnumerable<GridRegion> Regions {
        get {
            yield return positiveRails;
            yield return negativeRails;
            yield return centerRails;
        }
    }

    void Awake() {
        GenerateGrid();
    }

    private void GenerateGrid() {
        if (holePrefab == null) {
            Debug.LogError("BreadboardManager: holePrefab not assigned.");
            return;
        }

        foreach (var region in Regions) {
            foreach (var (row, col, localPosition) in region.GenerateSlots()) {
                SpawnHole(region, row, col, localPosition);
            }
        }
    }

    private void SpawnHole(GridRegion region, int row, int col, Vector3 localPosition) {
        var go = Instantiate(holePrefab, localPosition, Quaternion.identity, transform);
        go.name = $"{region.name}_{row}_{col}";
        var view = go.GetComponent<HoleView>();
        if (view == null) view = go.AddComponent<HoleView>();
        view.Init(region.name, row, col);
        _holeViews.Add(view);
    }

    // Query used by drag-and-drop / snapping code: given a world
    // position, find the nearest hole to snap to.
    public bool TryGetNearestHole(Vector3 worldPos, out HoleView hole, float maxSnapDistance = 0.15f) {
        hole = null;
        float best = maxSnapDistance;
        bool found = false;
        foreach (var view in _holeViews) {
            float d = Vector3.Distance(worldPos, view.transform.position);
            if (d < best) {
                best = d;
                hole = view;
                found = true;
            }
        }
        return found;
    }
}
