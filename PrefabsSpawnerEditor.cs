/* By GD-Kennedy // 2024
 
This is free and unencumbered software released into the public domain.

Anyone is free to copy, modify, publish, use, compile, sell, or
distribute this software, either in source code form or as a compiled
binary, for any purpose, commercial or non-commercial, and by any
means.

In jurisdictions that recognize copyright laws, the author or authors
of this software dedicate any and all copyright interest in the
software to the public domain. We make this dedication for the benefit
of the public at large and to the detriment of our heirs and
successors. We intend this dedication to be an overt act of
relinquishment in perpetuity of all present and future rights to this
software under copyright law.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY CLAIM, DAMAGES OR
OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
OTHER DEALINGS IN THE SOFTWARE.

For more information, please refer to <https://unlicense.org>
*/

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PrefabsSpawner))]
public class PrefabsSpawnerEditor : Editor
{
    private PrefabsSpawner _prefabsSpawner;
    private SerializedProperty prefabList;
    private SerializedProperty spawnRadius;
    private SerializedProperty minDistance;
    private SerializedProperty layerMask;
    private SerializedProperty activeParent;
    private SerializedProperty minScale;
    private SerializedProperty maxScale;
    private SerializedProperty minRotationY;
    private SerializedProperty maxRotationY;
    private SerializedProperty spawnRadiusColor;
    private SerializedProperty weightTable;
    private SerializedProperty useWeightTable;

    private bool isMouseDown = false;

    private void OnEnable()
    {
        _prefabsSpawner = (PrefabsSpawner)target;
        prefabList = serializedObject.FindProperty(nameof(_prefabsSpawner.PrefabList));
        spawnRadius = serializedObject.FindProperty(nameof(_prefabsSpawner.SpawnRadius));
        minDistance = serializedObject.FindProperty(nameof(_prefabsSpawner.MinDistance));
        layerMask = serializedObject.FindProperty(nameof(_prefabsSpawner.LayerMask));
        activeParent = serializedObject.FindProperty(nameof(_prefabsSpawner.ActiveParent));
        minScale = serializedObject.FindProperty(nameof(_prefabsSpawner.MinScale));
        maxScale = serializedObject.FindProperty(nameof(_prefabsSpawner.MaxScale));
        minRotationY = serializedObject.FindProperty(nameof(_prefabsSpawner.MinRotationY));
        maxRotationY = serializedObject.FindProperty(nameof(_prefabsSpawner.MaxRotationY));
        spawnRadiusColor = serializedObject.FindProperty(nameof(_prefabsSpawner.SpawnRadiusColor));
        weightTable = serializedObject.FindProperty(nameof(_prefabsSpawner.WeightTable));
        useWeightTable = serializedObject.FindProperty(nameof(_prefabsSpawner.UseWeightTable));
    }

    private void OnDisable()
    {
        isMouseDown = false;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        EditorGUILayout.LabelField("Parent Section", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(activeParent);
        if (GUILayout.Button("Create and assign parent"))
        {
            _prefabsSpawner.CreateAndAssignNewParent();
        }
        if (GUILayout.Button("Remove all from parent"))
        {
            _prefabsSpawner.RemoveAllObjectsFromParent();
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Generation Settings", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(minDistance);
        EditorGUILayout.PropertyField(spawnRadius);
        EditorGUILayout.PropertyField(spawnRadiusColor);
        EditorGUILayout.PropertyField(layerMask);
        EditorGUI.indentLevel--;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Scale Settings", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(minScale, new GUIContent("Min Scale"));
        EditorGUILayout.PropertyField(maxScale, new GUIContent("Max Scale"));
        EditorGUI.indentLevel--;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Rotation Settings", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.Slider(minRotationY, 0f, 360f, new GUIContent("Min Rotation Y"));
        EditorGUILayout.Slider(maxRotationY, 0f, 360f, new GUIContent("Max Rotation Y"));
        EditorGUI.indentLevel--;
        
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(useWeightTable);
        
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(prefabList);
        for (int i = 0; i < prefabList.arraySize; i++)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            weightTable.arraySize = prefabList.arraySize;
            GameObject prefab = (GameObject)prefabList.GetArrayElementAtIndex(i).objectReferenceValue;
            if (prefab != null)
            {
                Texture2D preview = AssetPreview.GetAssetPreview(prefab);
                if (preview != null)
                {
                    GUILayout.Label(preview, GUILayout.Width(64), GUILayout.Height(64));
                }
                else
                {
                    GUILayout.Label("No preview", GUILayout.Width(64), GUILayout.Height(64));
                }
            }
            EditorGUILayout.BeginVertical();
            EditorGUILayout.PropertyField(prefabList.GetArrayElementAtIndex(i), GUIContent.none);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Remove from list", GUILayout.Width(150)))
            {
                prefabList.DeleteArrayElementAtIndex(i);
                serializedObject.ApplyModifiedProperties();
                break;
            }
            if (GUILayout.Button("Delete all copies from parent", GUILayout.Width(200)))
            {
                _prefabsSpawner.RemoveAllOfThisType(prefab);
            }

            if (useWeightTable.boolValue)
            {
                EditorGUILayout.PropertyField(weightTable.GetArrayElementAtIndex(i), GUIContent.none);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }
        serializedObject.ApplyModifiedProperties();
    }

    private void OnSceneGUI()
    {
        Event guiEvent = Event.current;

        if (guiEvent.type == EventType.Layout)
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        else if (guiEvent.type == EventType.MouseMove || guiEvent.type == EventType.MouseDrag)
            SceneView.RepaintAll();

        if (guiEvent.type == EventType.Repaint)
        {
            Ray mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            if (Physics.Raycast(mouseRay, out RaycastHit hit, Mathf.Infinity, layerMask.intValue))
            {
                Handles.color = spawnRadiusColor.colorValue;
                Handles.DrawWireDisc(hit.point, Vector3.up, _prefabsSpawner.SpawnRadius);
            }
        }

        if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0)
        {
            isMouseDown = true;
            Undo.RecordObject(_prefabsSpawner.ActiveParent, "Create new parent");
        }
        else if (guiEvent.type == EventType.MouseUp && guiEvent.button == 0)
        {
            isMouseDown = false;
        }

        if (isMouseDown)
        {
            Ray mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            if (Physics.Raycast(mouseRay, out RaycastHit hit, Mathf.Infinity, layerMask.intValue))
            {
                _prefabsSpawner.TrySpawnAtMousePosition(hit.point);
            }
        }
    }
}