using UnityEngine;
using UnityEngine;
using System.Collections.Generic;

public class VehicleSpawner : MonoBehaviour
{
    public GameObject[] carPrefabs;        // UI car prefabs
    public GameObject[] placePrefabs;      // UI drop zone prefabs
    public Transform carSpawnParent;       // Car UI parent (inside Canvas)
    public Transform placeSpawnParent;     // Drop place UI parent (inside Canvas)
    public ObjectScript objectScript;      // Reference to ObjectScript
    public RectTransform timerRect; // Assign this in the Inspector
    public float timerPadding = 40f; // Extra padding around the timer

    public int numberToSpawn = 5;

    public Vector2 spawnMin = new Vector2(-600f, -300f);  // UI space min
    public Vector2 spawnMax = new Vector2(600f, 300f);    // UI space max

    void Start()
    {
        SpawnCarsAndPlaces();
    }


public float minSpawnDistance = 120f;

void SpawnCarsAndPlaces()
{
    int spawnCount = Mathf.Min(numberToSpawn, carPrefabs.Length, placePrefabs.Length);
    if (spawnCount == 0)
    {
        Debug.LogError("No car or place prefabs assigned!");
        return;
    }

    objectScript.vehicles = new GameObject[spawnCount];
    objectScript.startCoordinates = new Vector2[spawnCount];

    List<int> indices = new List<int>();
    for (int i = 0; i < spawnCount; i++) indices.Add(i);
    for (int i = 0; i < indices.Count; i++)
    {
        int swap = Random.Range(i, indices.Count);
        int temp = indices[i];
        indices[i] = indices[swap];
        indices[swap] = temp;
    }

    // Keep track of all used positions
    List<Vector2> usedPositions = new List<Vector2>();

for (int i = 0; i < spawnCount; i++)
{
    int prefabIndex = indices[i];

    // --- Spawn Car ---
    Vector2 carPos = GetNonOverlappingPosition(usedPositions);
    GameObject car = Instantiate(carPrefabs[prefabIndex], carSpawnParent);
    RectTransform carRT = car.GetComponent<RectTransform>();
    carRT.anchoredPosition = carPos;

    // ✅ Random rotation between -30° and +30°
    float carRotation = Random.Range(-30f, 30f);
    carRT.localRotation = Quaternion.Euler(0f, 0f, carRotation);

    // Keep the prefab’s original scale (don’t change)
    // carRT.localScale = carRT.localScale;

    usedPositions.Add(carPos);

    var drag = car.GetComponent<DragAndDropScript>();
    if (drag != null && drag.screenBou == null)
        drag.screenBou = FindObjectOfType<ScreenBoundriesScript>();

    objectScript.vehicles[i] = car;
    objectScript.startCoordinates[i] = carRT.anchoredPosition;

    // --- Spawn Place ---
    Vector2 placePos = GetNonOverlappingPosition(usedPositions);
    GameObject place = Instantiate(placePrefabs[prefabIndex], placeSpawnParent);
    RectTransform placeRT = place.GetComponent<RectTransform>();
    placeRT.anchoredPosition = placePos;

    // ✅ Random rotation for place (-30° to +30°)
    float placeRotation = Random.Range(-30f, 30f);
    placeRT.localRotation = Quaternion.Euler(0f, 0f, placeRotation);

    // Keep original prefab scale (no change)
    // placeRT.localScale = placeRT.localScale;

    usedPositions.Add(placePos);
}

objectScript.Initialize();

}

// Helper to get a random position not too close to any in usedPositions
Vector2 GetNonOverlappingPosition(List<Vector2> usedPositions)
{
    RectTransform parentRect = carSpawnParent as RectTransform;
    if (parentRect == null)
        parentRect = placeSpawnParent as RectTransform;
    if (parentRect == null)
        return Vector2.zero;

    float width = parentRect.rect.width;
    float height = parentRect.rect.height;

    float paddingX = 60f;
    float paddingY = 60f;

    int maxAttempts = 100;
    for (int attempt = 0; attempt < maxAttempts; attempt++)
    {
        float x = Random.Range(-width / 2 + paddingX, width / 2 - paddingX);
        float y = Random.Range(-height / 2 + paddingY, height / 2 - paddingY);
        Vector2 candidate = new Vector2(x, y);

        bool overlaps = false;

        // Check overlap with other spawned objects
        foreach (var pos in usedPositions)
        {
            if (Vector2.Distance(candidate, pos) < minSpawnDistance)
            {
                overlaps = true;
                break;
            }
        }

        // Check overlap with timer
        if (!overlaps && timerRect != null)
        {
            Vector2 timerCenter = timerRect.anchoredPosition;
            Vector2 timerSize = timerRect.rect.size;
            Rect timerArea = new Rect(
                timerCenter - timerSize / 2f - Vector2.one * timerPadding,
                timerSize + Vector2.one * timerPadding * 2f
            );

            if (timerArea.Contains(candidate))
            {
                overlaps = true;
            }
        }

        if (!overlaps)
            return candidate;
    }

    // fallback: just return a random position (may overlap)
    float fallbackX = Random.Range(-width / 2 + paddingX, width / 2 - paddingX);
    float fallbackY = Random.Range(-height / 2 + paddingY, height / 2 - paddingY);
    return new Vector2(fallbackX, fallbackY);
}


Vector2 GetRandomPosition()
{
    RectTransform parentRect = carSpawnParent as RectTransform;
    if (parentRect == null)
        parentRect = placeSpawnParent as RectTransform;
    if (parentRect == null)
        return Vector2.zero;

    float width = parentRect.rect.width;
    float height = parentRect.rect.height;

    // Padding to keep objects inside the map
    float paddingX = 60f; // adjust as needed for your car/place size
    float paddingY = 60f;

    // anchoredPosition is centered at (0,0)
    float x = Random.Range(-width / 2 + paddingX, width / 2 - paddingX);
    float y = Random.Range(-height / 2 + paddingY, height / 2 - paddingY);

    return new Vector2(x, y);
}
}
