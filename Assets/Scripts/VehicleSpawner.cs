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

        // Find a valid position for the car
        Vector2 carPos = GetNonOverlappingPosition(usedPositions);
        GameObject car = Instantiate(carPrefabs[prefabIndex], carSpawnParent);
        car.GetComponent<RectTransform>().anchoredPosition = carPos;
        usedPositions.Add(carPos);

        var drag = car.GetComponent<DragAndDropScript>();
        if (drag != null && drag.screenBou == null)
            drag.screenBou = FindObjectOfType<ScreenBoundriesScript>();

        objectScript.vehicles[i] = car;
        objectScript.startCoordinates[i] = car.GetComponent<RectTransform>().anchoredPosition;

        // Find a valid position for the place
        Vector2 placePos = GetNonOverlappingPosition(usedPositions);
        GameObject place = Instantiate(placePrefabs[prefabIndex], placeSpawnParent);
        place.GetComponent<RectTransform>().anchoredPosition = placePos;
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
        foreach (var pos in usedPositions)
        {
            if (Vector2.Distance(candidate, pos) < minSpawnDistance)
            {
                overlaps = true;
                break;
            }
        }
        if (!overlaps)
            return candidate;
    }
    // If we can't find a non-overlapping position, just return a random one
    return new Vector2(
        Random.Range(-width / 2 + paddingX, width / 2 - paddingX),
        Random.Range(-height / 2 + paddingY, height / 2 - paddingY)
    );
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
    float paddingX = 650f; // adjust as needed for your car/place size
    float paddingY = 650f;

    // anchoredPosition is centered at (0,0)
    float x = Random.Range(-width / 2 + paddingX, width / 2 - paddingX);
    float y = Random.Range(-height / 2 + paddingY, height / 2 - paddingY);

    return new Vector2(x, y);
}
}
