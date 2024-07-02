using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ams.2d
{
public class GridEditorWindow : EditorWindow
{
    Grid grid;
    Tilemap tilemap;
    
    Vector2 scrollPos;
    
    Dictionary<string, List<int>> namedTileIndices = new();
    Dictionary<string, TileBase> namedTiles = new();
    
    [MenuItem("Tools/AMS/Grid Editor")]
    static void ShowWindow()
    {
        var window = GetWindow<GridEditorWindow>();
        window.titleContent = new GUIContent("Grid Editor");
        window.Show();
    }
    
    void WatchUndoHistory()
    {
        Undo.undoRedoPerformed += OnUndoRedo;
        Undo.undoRedoEvent += OnUndoRedo;
    }


    void OnUndoRedo(in UndoRedoInfo undo)
    {
        OnUndoRedo();
    }


    void OnUndoRedo()
    {
        UpdateInfo();
    }


    void OnGUI()
    {
        tilemap = (Tilemap)EditorGUILayout.ObjectField("Tilemap", tilemap, typeof(Tilemap), true);
        if (tilemap == null)
        {
            EditorGUILayout.HelpBox("Please select a Tilemap.", MessageType.Warning);
            return;
        }
        grid = tilemap.layoutGrid;
        if (grid == null)
        {
            EditorGUILayout.HelpBox("Tilemap does not have a Grid.", MessageType.Warning);
            return;
        }
        EditorGUILayout.LabelField("Batch Change Tiles", EditorStyles.boldLabel);
        if (GUILayout.Button("Update Grid Info"))
        {
            UpdateInfo();
        }
        if (namedTileIndices.Count > 0)
        {
            PaintTileList();
        }
        else
        {
            EditorGUILayout.HelpBox("No tiles found in Tilemap.", MessageType.Info);
        }
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Grid Operations", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Subdivide Grid"))
        {
            SubdivideGrid();
        }
        if (GUILayout.Button("Compress Grid"))
        {
            UnsubdivideGrid();
        }
        EditorGUILayout.EndHorizontal();
    }


    void PaintTileList()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        foreach (var kvp in namedTileIndices)
        {
            EditorGUILayout.LabelField($"Tile Name: {kvp.Key}", $"Count: {kvp.Value.Count.ToString()}");
            namedTiles.TryGetValue(kvp.Key, out var tile);
            tile = (TileBase)EditorGUILayout.ObjectField(tile, typeof(TileBase), false);
            if (tile != null)
            {
                namedTiles[kvp.Key] = tile;
            }

            if (GUILayout.Button("Delete Tiles"))
            {
                DeleteTiles(tilemap, kvp.Value);
            }
        }
        EditorGUILayout.EndScrollView();
        if (GUILayout.Button("Write Changes to Tilemap"))
        {
            WriteTileMapUpdates();
            UpdateInfo();
        }
    }


    void DeleteTiles(Tilemap tilemap, IEnumerable<int> indices)
    {
        if (tilemap == null) return;
        var bounds = tilemap.cellBounds;
        var tiles = tilemap.GetTilesBlock(bounds);
        foreach (var index in indices)
        {
            tiles[index] = null;
        }
        Undo.RecordObject(tilemap, "Delete Tiles");
        tilemap.SetTilesBlock(bounds, tiles);
    }


    void WriteTileMapUpdates()
    {
        if (tilemap == null) return;
        var bounds = tilemap.cellBounds;
        var tiles = tilemap.GetTilesBlock(bounds);
        foreach (var kvp in namedTiles)
        {
            namedTileIndices.TryGetValue(kvp.Key, out var indices);
            if (indices == null) continue;
            foreach (var index in indices)
            {
                tiles[index] = kvp.Value;
            }
        }
        Undo.RecordObject(tilemap, "Update Tilemap");
        tilemap.SetTilesBlock(bounds, tiles);
    }


    void UpdateInfo()
    {
        namedTileIndices.Clear();
        if (tilemap == null) return;
        var bounds = tilemap.cellBounds;
        var tiles = tilemap.GetTilesBlock(bounds);
        
        for (int x = 0; x < bounds.size.x; x++)
        {
            for (int y = 0; y < bounds.size.y; y++)
            {
                var tile = tiles[x + y * bounds.size.x];
                if (tile == null) continue;
                namedTiles[tile.name] = tile;
                if (!namedTileIndices.ContainsKey(tile.name))
                {
                    namedTileIndices[tile.name] = new List<int>();
                }
                namedTileIndices[tile.name].Add(x + y * bounds.size.x);
            }
        }
    }


    void SubdivideGrid()
    {
        if (grid == null) return;
        var bounds = tilemap.cellBounds;
        var cellSize = grid.cellSize;
        var cellGap = grid.cellGap;

        var newCellSize = cellSize * 0.5f;
        var newCellGap = cellGap * 0.5f;

        var newGrid = Instantiate(grid.gameObject, grid.transform.parent, true).GetComponent<Grid>();
        newGrid.name = $"{grid.name} - Subdivided";
        newGrid.cellSize = newCellSize;
        newGrid.cellGap = newCellGap;
        newGrid.cellLayout = grid.cellLayout;
        newGrid.cellSwizzle = grid.cellSwizzle;
        
        foreach (Transform child in newGrid.transform)
        {
            DestroyImmediate(child.gameObject);
        }

        var newTilemapGO = Instantiate(tilemap.gameObject, newGrid.transform, true);
        newTilemapGO.name = $"{tilemap.name} - Subdivided";
        newTilemapGO.transform.position = tilemap.transform.position;
        var newTilemap = newTilemapGO.GetComponent<Tilemap>();
        newTilemap.ClearAllTiles();

        for (int x = 0; x < bounds.size.x; x++)
        {
            for (int y = 0; y < bounds.size.y; y++)
            {
                var px = x + bounds.x;
                var py = y + bounds.y;
                var tile = tilemap.GetTile(new Vector3Int(px, py, 0));
                newTilemap.SetTile(new Vector3Int(px * 2, py * 2, 0), tile);
                newTilemap.SetTile(new Vector3Int(px * 2 + 1, py * 2, 0), tile);
                newTilemap.SetTile(new Vector3Int(px * 2, py * 2 + 1, 0), tile);
                newTilemap.SetTile(new Vector3Int(px * 2 + 1, py * 2 + 1, 0), tile);
            }
        }
    }


    void UnsubdivideGrid()
    {
        if (grid == null) return;
        var bounds = tilemap.cellBounds;
        var cellSize = grid.cellSize;
        var cellGap = grid.cellGap;
        var cellLayout = grid.cellLayout;
        var cellSwizzle = grid.cellSwizzle;
        
        var newCellSize = cellSize * 2;
        var newCellGap = cellGap * 2;

        var newGrid = Instantiate(grid.gameObject, grid.transform.parent, true).GetComponent<Grid>();
        newGrid.name = $"{grid.name} - Compressed";
        newGrid.cellSize = newCellSize;
        newGrid.cellGap = newCellGap;
        newGrid.cellLayout = cellLayout;
        newGrid.cellSwizzle = cellSwizzle;
        
        foreach (Transform child in newGrid.transform)
        {
            DestroyImmediate(child.gameObject);
        }

        var newTilemapGO = Instantiate(tilemap.gameObject, newGrid.transform, true);
        newTilemapGO.name = $"{tilemap.name} - Compressed";
        newTilemapGO.transform.position = tilemap.transform.position;
        var newTilemap = newTilemapGO.GetComponent<Tilemap>();
        newTilemap.ClearAllTiles();

        for (int x = 0; x < bounds.size.x; x += 2)
        {
            for (int y = 0; y < bounds.size.y; y += 2)
            {
                var px = x + bounds.x;
                var py = y + bounds.y;
                var tile = tilemap.GetTile(new Vector3Int(px, py, 0));
                newTilemap.SetTile(new Vector3Int(px / 2, py / 2, 0), tile);
            }
        }
    }
}
}