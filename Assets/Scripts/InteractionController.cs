using UnityEngine;

public class InteractionController : MonoBehaviour
{
    public Camera arCamera;
    public float tapRadius = 0.015f;
    public float maxDistance = 10f;

    void Awake()
    {
        if (!arCamera) arCamera = Camera.main;
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing) return;
        if (TouchInput.Count == 0 || !TouchInput.Began(0)) return;

        Vector2 screenPos = TouchInput.Position(0);
        if (TouchInput.IsOverUI(screenPos)) return;

        Ray ray = arCamera.ScreenPointToRay(screenPos);
        RaycastHit[] hits = Physics.SphereCastAll(ray, tapRadius, maxDistance);

        Bug bestBug = null;
        float bugDist = float.MaxValue;
        Plant bestPlant = null;
        float plantDist = float.MaxValue;

        foreach (var hit in hits)
        {
            var bug = hit.collider.GetComponentInParent<Bug>();
            if (bug != null)
            {
                if (!bug.IsSquashed && hit.distance < bugDist) { bestBug = bug; bugDist = hit.distance; }
                continue;
            }

            var plant = hit.collider.GetComponentInParent<Plant>();
            if (plant != null && hit.distance < plantDist) { bestPlant = plant; plantDist = hit.distance; }
        }

        if (bestBug != null) bestBug.Squash();
        else if (bestPlant != null) bestPlant.OnTapped();
    }
}
