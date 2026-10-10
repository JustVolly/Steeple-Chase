#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Rebuilds the separate vertical climbing layout using project prefab assets.
/// Keeps root instance scale exactly (1,1,1). Does not modify Main or other scenes.
/// </summary>
public static class VerticalClimbSceneGenerator
{
    private const string ScenePath = "Assets/Scenes/VerticalClimb.unity";
    private const int Seed = 20261011;
    private const float Radius = 75f;
    private const float RouteHeightStep = 1.38f;
    private const int RouteCount = 78;
    private const int ScatterCount = 56;
    private const int HazardCount = 12;

    // Existing prefabs have their own mesh colliders.
    private static readonly string[] PlatformPaths =
    {
        "Assets/Platform/Platformer_9_Heaven/Prefabs/GOLD/platform_002.prefab",
        "Assets/Platform/Platformer_9_Heaven/Prefabs/GOLD/platform_003.prefab",
        "Assets/Platform/Platformer Deathrun/Prefabs/Landscape/land_001.prefab",
        "Assets/Platform/Platformer Deathrun/Prefabs/Landscape/land_002.prefab"
    };

    private const string HazardPath = "Assets/Obstacle12.prefab";

    [MenuItem("Tools/Steeple Chase/Rebuild VerticalClimb Scene")]
    public static void RebuildScene()
    {
        if (!EditorUtility.DisplayDialog(
                "Rebuild VerticalClimb?",
                "This replaces Assets/Scenes/VerticalClimb.unity with a freshly generated layout. " +
                "Other scenes are not touched. Continue?",
                "Rebuild", "Cancel"))
            return;

        var platforms = new GameObject[PlatformPaths.Length];
        for (int i = 0; i < platforms.Length; i++)
        {
            platforms[i] = AssetDatabase.LoadAssetAtPath<GameObject>(PlatformPaths[i]);
            if (platforms[i] == null)
            {
                Debug.LogError("Missing platform prefab: " + PlatformPaths[i]);
                return;
            }
        }

        var hazardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HazardPath);
        if (hazardPrefab == null)
        {
            Debug.LogError("Missing hazard prefab: " + HazardPath);
            return;
        }

        Scene oldActiveScene = SceneManager.GetActiveScene();
        Scene scene = default;
        try
        {
            scene = EditorSceneManager.NewScene(
                NewSceneSetup.DefaultGameObjects, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);

            var root = MakeGroup("VerticalClimb - 150m diameter", scene, null);
            var routeRoot = MakeGroup("Main Ascent Route (78)", scene, root);
            var scatterRoot = MakeGroup("Scattered Platforms (56)", scene, root);
            var hazardsRoot = MakeGroup("Hazards (12)", scene, root);

            var random = new System.Random(Seed);
            var positions = new List<Vector3>(ScatterCount);

            Vector2 routePoint = Vector2.zero;
            Vector3 lastRoutePosition = Vector3.zero;

            for (int i = 0; i < RouteCount; i++)
            {
                if (i != 0)
                {
                    float angle = RandomRange(random, 0f, Mathf.PI * 2f);
                    float distance = RandomRange(random, 1.15f, 3.5f);
                    routePoint += new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
                    routePoint = Vector2.ClampMagnitude(routePoint, 34f);
                }

                lastRoutePosition =
                    new Vector3(routePoint.x, i * RouteHeightStep, routePoint.y);

                MakeInstance(platforms[random.Next(platforms.Length)], scene,
                    routeRoot, "RoutePlatform_" + (i + 1).ToString("D3"),
                    lastRoutePosition, Quaternion.identity);
            }

            for (int i = 0; i < ScatterCount; i++)
            {
                Vector3 position = Vector3.zero;

                // Attempts to keep loosely scattered items from occupying the same space.
                for (int attempt = 0; attempt < 20; attempt++)
                {
                    Vector2 disk = RandomPointInDisk(random, Radius);
                    position = new Vector3(disk.x,
                        RandomRange(random, 4f, RouteCount * RouteHeightStep + 12f), disk.y);

                    bool tooClose = false;
                    foreach (Vector3 previous in positions)
                    {
                        Vector3 separation = position - previous;
                        separation.y *= 1.2f;
                        if (separation.magnitude < 4f)
                        {
                            tooClose = true;
                            break;
                        }
                    }

                    if (!tooClose)
                        break;
                }

                positions.Add(position);
                MakeInstance(platforms[random.Next(platforms.Length)], scene,
                    scatterRoot, "ScatteredPlatform_" + (i + 1).ToString("D3"),
                    position, Quaternion.Euler(0f, RandomRange(random, 0f, 360f), 0f));
            }

            for (int i = 0; i < HazardCount; i++)
            {
                float angle = RandomRange(random, 0f, 2f * Mathf.PI);
                float distance = RandomRange(random, 18f, 56f);
                var position = new Vector3(Mathf.Cos(angle) * distance,
                    RandomRange(random, 5f, 110f), Mathf.Sin(angle) * distance);

                MakeInstance(hazardPrefab, scene, hazardsRoot,
                    "VerticalHazard_" + (i + 1).ToString("D2"),
                    position, Quaternion.identity);
            }

            MakeMarker("Route Start", scene, root, Vector3.zero);
            MakeMarker("Route Finish", scene, root,
                lastRoutePosition + Vector3.up * RouteHeightStep);

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("Scene could not be saved: " + ScenePath);

            Debug.Log(
                "VerticalClimb saved with 134 platforms, 12 hazards, " +
                "75m radius and root scale (1,1,1): " + ScenePath);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
        finally
        {
            if (oldActiveScene.IsValid() && oldActiveScene.isLoaded)
                SceneManager.SetActiveScene(oldActiveScene);

            if (scene.IsValid() && scene.isLoaded)
                EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static Transform MakeGroup(string name, Scene scene, Transform parent)
    {
        var go = new GameObject(name);
        SceneManager.MoveGameObjectToScene(go, scene);
        go.transform.SetParent(parent, false);
        go.transform.localScale = Vector3.one;
        return go.transform;
    }

    private static void MakeMarker(string name, Scene scene,
        Transform parent, Vector3 position)
    {
        Transform marker = MakeGroup(name, scene, parent);
        marker.position = position;
    }

    private static void MakeInstance(GameObject asset, Scene scene, Transform parent,
        string objectName, Vector3 position, Quaternion rotation)
    {
        var instance = PrefabUtility.InstantiatePrefab(asset, scene) as GameObject;
        if (instance == null)
            throw new InvalidOperationException("Cannot instantiate: " + asset.name);

        instance.name = objectName;
        instance.transform.SetParent(parent, true);
        instance.transform.position = position;
        instance.transform.rotation = rotation;
        instance.transform.localScale = Vector3.one;
    }

    private static Vector2 RandomPointInDisk(System.Random random, float radius)
    {
        float angle = RandomRange(random, 0f, Mathf.PI * 2f);
        float distance = Mathf.Sqrt((float)random.NextDouble()) * radius;
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
    }

    private static float RandomRange(System.Random random, float min, float max)
    {
        return min + (float)random.NextDouble() * (max - min);
    }
}
#endif
