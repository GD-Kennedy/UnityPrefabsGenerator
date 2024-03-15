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

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

public class PrefabsSpawner : MonoBehaviour
{
    public GameObject ActiveParent;
    public List<GameObject> PrefabList = new List<GameObject>();
    public List<int> WeightTable = new List<int>();
    public bool UseWeightTable;

    public Color SpawnRadiusColor = Color.magenta;
    public float SpawnRadius = 100f;
    public float MinDistance = 20f;
    public LayerMask LayerMask;

    public Vector3 MinScale = Vector3.one;
    public Vector3 MaxScale = Vector3.one;

    [Range(0f, 360f)] public float MinRotationY = 0f;
    [Range(0f, 360f)] public float MaxRotationY = 360f;
    
    private Camera mainCamera;
    private float groundCheckOffset; // Could be exposed if needed

    public bool TrySpawnAtMousePosition(Vector3 point)
    {
        if (mainCamera == null) 
            mainCamera = Camera.main;

        if (PrefabList.Count == 0)
            return false;
        
        Vector3 randomPoint = GetRandomPointAroundMousePos(point);
        if (CanSpawnAtPoint(randomPoint))
        {
            SpawnAtPoint(randomPoint);
            return true;
        }

        return false;
    }

    private Vector3 GetRandomPointAroundMousePos(Vector3 point)
    {
        Vector3 randomizedPoint = point + new Vector3(Random.Range(-SpawnRadius, SpawnRadius), point.y,
            Random.Range(-SpawnRadius, SpawnRadius));
        Ray ray = new Ray(randomizedPoint + Vector3.up * groundCheckOffset, Vector3.down);
        return Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, LayerMask)
            ? hit.point
            : default;
    }

    private bool CanSpawnAtPoint(Vector3 point)
    {
        if (ActiveParent == null || point == default)
            return false;

        Transform parentTransform = ActiveParent.transform; 
        foreach (Transform child in parentTransform)
        {
            float distance = Vector3.Distance(child.position, point);
            if (distance < MinDistance)
            {
                return false;
            }
        }

        return true;
    }

    public void SpawnAtPoint(Vector3 position)
    {
        if (ActiveParent == null)
        {
            Debug.LogError("Parent not assigned.");
            return;
        }

        GameObject prefab = null;
        if (UseWeightTable)
        {
            float totalWeight = 0f;
            for (int i = 0; i < WeightTable.Count; i++)
            {
                totalWeight += WeightTable[i];
            }

            float randomNum = Random.Range(0f, totalWeight);

            float cumulativeWeight = 0f;
            for (int i = 0; i < PrefabList.Count; i++)
            {
                cumulativeWeight += WeightTable[i];
                if (randomNum <= cumulativeWeight)
                {
                    prefab = PrefabList[i];
                    break;
                }
            }
        }
        else
        {
            prefab = PrefabList[Random.Range(0, PrefabList.Count)];
        }
        
        if (prefab == null)
            return;

        Vector3 scale = new Vector3(
            Random.Range(MinScale.x, MaxScale.x),
            Random.Range(MinScale.y, MaxScale.y),
            Random.Range(MinScale.z, MaxScale.z)
        );

        Quaternion rotation = Quaternion.Euler(0f, Random.Range(MinRotationY, MaxRotationY), 0f);
        GameObject instance = Instantiate(prefab, position, rotation, ActiveParent.transform);
        instance.transform.localScale = scale;
        Undo.RegisterCreatedObjectUndo(instance, "Instantiate Prefab");
    }

    public void CreateAndAssignNewParent()
    {
        GameObject parentObject = new GameObject("NewPrefabsParent");
        parentObject.transform.parent = transform;
        ActiveParent = parentObject;
        Undo.RegisterCreatedObjectUndo(parentObject, "Create new parent");
    }

    public void RemoveAllObjectsFromParent()
    {
        GameObject groupParent = ActiveParent;

        if (groupParent != null)
        {
            Undo.RecordObject(groupParent, "Remove all spawned objects");

            int childCount = groupParent.transform.childCount;
            for (int i = childCount - 1; i >= 0; i--)
            {
                Undo.DestroyObjectImmediate(groupParent.transform.GetChild(i).gameObject);
            }
        }
        else
        {
            Debug.LogWarning("Group parent is not assigned.");
        }
    }

    public void RemoveAllOfThisType(GameObject prefab)
    {
        if (ActiveParent == null || prefab == null)
            return;

        Undo.RecordObject(ActiveParent, "Remove all of type");

        Transform parentTransform = ActiveParent.transform;
        for (int i = parentTransform.childCount - 1; i >= 0; i--)
        {
            GameObject childObject = parentTransform.GetChild(i).gameObject;
            if (childObject != null && childObject.name == prefab.name + "(Clone)")
            {
                Undo.DestroyObjectImmediate(childObject);
            }
        }
    }
}