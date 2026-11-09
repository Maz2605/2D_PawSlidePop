#if UNITY_EDITOR
using System;
using System.Reflection;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;
using UnityEditor;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Utils.Editor
{
    [CustomEditor(typeof(Match3TileDatabaseSO))]
    public class Match3TileDatabaseEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);

            Match3TileDatabaseSO database = (Match3TileDatabaseSO)target;
            EditorGUILayout.HelpBox(
                $"Entries: {database.TileCount}\nTile Definitions: {database.TileDefinitionCount}\nOverlay Definitions: {database.OverlayDefinitionCount}\nUnderlay Definitions: {database.UnderlayDefinitionCount}\nNormal Tiles: {database.NormalTileCount}\nBooster Tiles: {database.BoosterTileCount}\nValid Unique Tiles: {database.CachedTileCount}\nSpawnable Normal Tiles: {database.SpawnableTileCount}",
                MessageType.Info);

            if (GUILayout.Button("Clear Console + Validate"))
            {
                ClearConsole();
                database.ValidateAndLogSummary();
                EditorUtility.SetDirty(database);
            }

            if (GUILayout.Button("Validate Only"))
            {
                database.ValidateAndLogSummary();
                EditorUtility.SetDirty(database);
            }
        }

        private static void ClearConsole()
        {
            Type logEntriesType = Type.GetType("UnityEditor.LogEntries, UnityEditor.dll");
            MethodInfo clearMethod = logEntriesType?.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public);
            clearMethod?.Invoke(null, null);
        }
    }
}
#endif
