using UnityEngine;

public class FlyingObjectsManager : MonoBehaviour
{
    public void DestroyAllFlyingObjects()
    {
    // Find all flying object controllers in the scene (include inactive).
    // Resources.FindObjectsOfTypeAll returns all loaded instances including inactive.
    FlyingObjectsControllerScript[] flyingObjects = Resources.FindObjectsOfTypeAll<FlyingObjectsControllerScript>();

        foreach (FlyingObjectsControllerScript obj in flyingObjects)
        {
            if (obj == null)
                continue;

            if (obj.CompareTag("Bomb"))
            {
                obj.TriggerExplosion();
            }
            else
            {
                obj.StartToDestroy(Color.cyan);
            }
        }
    }
}
