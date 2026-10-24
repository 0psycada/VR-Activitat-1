using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// Handles everything related to putting the garden in the real world:
/// - Scanning: shows a reticle where the screen centre hits a detected plane; tap to place.
/// - Placing: one finger drags the garden, two fingers pinch to scale and twist to rotate.
/// - Lock: adds an ARAnchor and hides the planes.
/// Put this component on the XR Origin (next to ARRaycastManager / ARPlaneManager / ARAnchorManager).
/// </summary>
public class ARPlacementController : MonoBehaviour
{
    [Header("AR references (auto-filled if on the XR Origin)")]
    public ARRaycastManager raycastManager;
    public ARPlaneManager planeManager;
    public ARAnchorManager anchorManager;
    public ARSession arSession;
    public Camera arCamera;

    [Header("Content")]
    public Garden gardenPrefab;
    [Tooltip("Optional. If empty, a simple flat disc is generated.")]
    public GameObject reticlePrefab;

    [Header("Placement gestures")]
    public float minScale = 0.5f;
    public float maxScale = 2.5f;

    [Header("Reset")]
    [Tooltip("Also reset the whole AR session (forgets all planes) when the player presses Reset.")]
    public bool resetSessionOnReset = false;

    static readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();

    GameObject reticle;
    Garden garden;
    GameObject anchorObject;
    bool planesVisible = true;
    float currentScale = 1f;
    bool dragging;
    bool twoFingerActive;
    float prevPinchDistance;
    float prevTwistAngle;

    public Garden CurrentGarden => garden;

    void Awake()
    {
        if (!raycastManager) raycastManager = GetComponent<ARRaycastManager>();
        if (!planeManager) planeManager = GetComponent<ARPlaneManager>();
        if (!anchorManager) anchorManager = GetComponent<ARAnchorManager>();
        if (!arCamera) arCamera = Camera.main;

        if (!raycastManager) Debug.LogError("ARPlacementController: no ARRaycastManager found.");
        if (!gardenPrefab) Debug.LogError("ARPlacementController: 'Garden Prefab' is not assigned.");
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        if (gm.State == GameState.Scanning)
        {
            UpdateScanning();
        }
        else
        {
            SetReticleVisible(false);
            if (gm.State == GameState.Placing) UpdatePlacing();
        }

        // Planes detected while they should be hidden (during play) are hidden too.
        if (!planesVisible) SetPlanesVisible(false);
    }

    // ---------- Public API used by GameManager ----------

    public void BeginScanning()
    {
        EnsureReticle();
        SetPlanesVisible(true);
    }

    public void LockPlacement()
    {
        if (!garden) return;

        // Hide planes first, so the game can continue even if anchoring fails.
        SetPlanesVisible(false);

        if (anchorManager == null || !anchorManager.enabled)
        {
            Debug.LogWarning("ARPlacementController: no ARAnchorManager, garden will not be anchored (may drift).");
            return;
        }

        // The anchor lives on a separate object and the garden becomes its child.
        // AR Foundation moves the anchor, and the garden follows it.
        anchorObject = new GameObject("GardenAnchor");
        anchorObject.transform.SetPositionAndRotation(garden.transform.position, garden.transform.rotation);

        try
        {
            anchorObject.AddComponent<ARAnchor>();
            garden.transform.SetParent(anchorObject.transform, true);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("ARPlacementController: could not create the anchor, playing without it. " + e.Message);
            Destroy(anchorObject);
            anchorObject = null;
        }
    }

    public void ResetPlacement()
    {
        if (garden) Destroy(garden.gameObject);
        garden = null;
        if (anchorObject) Destroy(anchorObject);
        anchorObject = null;
        dragging = false;
        twoFingerActive = false;
        SetReticleVisible(false);
        SetPlanesVisible(true);
        if (resetSessionOnReset && arSession) arSession.Reset();
    }

    void UpdateScanning()
    {
        EnsureReticle();

        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        if (!raycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon))
        {
            SetReticleVisible(false);
            return;
        }

        Pose pose = hits[0].pose;
        Quaternion facing = FacingRotation();
        reticle.transform.SetPositionAndRotation(pose.position, facing);
        SetReticleVisible(true);

        if (TouchInput.Count > 0 && TouchInput.Began(0) && !TouchInput.IsOverUI(TouchInput.Position(0)))
            PlaceGarden(pose.position, facing);
    }

    void PlaceGarden(Vector3 position, Quaternion rotation)
    {
        if (garden) Destroy(garden.gameObject);

        garden = Instantiate(gardenPrefab, position, rotation);
        currentScale = 1f;
        garden.transform.localScale = Vector3.one;
        dragging = false;
        twoFingerActive = false;

        SetReticleVisible(false);
        GameManager.Instance.OnGardenPlaced(garden);
    }

    Quaternion FacingRotation()
    {
        Vector3 forward = Vector3.ProjectOnPlane(arCamera.transform.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
        return Quaternion.LookRotation(forward.normalized, Vector3.up);
    }

    void UpdatePlacing()
    {
        if (!garden) return;

        int count = TouchInput.Count;

        if (count >= 2)
        {
            dragging = false;
            Vector2 p0 = TouchInput.Position(0);
            Vector2 p1 = TouchInput.Position(1);
            float distance = Vector2.Distance(p0, p1);
            Vector2 d = p1 - p0;
            float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;

            if (twoFingerActive && prevPinchDistance > 1f)
            {
                // Pinch to scale
                currentScale = Mathf.Clamp(currentScale * (distance / prevPinchDistance), minScale, maxScale);
                garden.transform.localScale = Vector3.one * currentScale;

                // Twist to rotate 
                float deltaAngle = Mathf.DeltaAngle(prevTwistAngle, angle);
                garden.transform.Rotate(Vector3.up, -deltaAngle, Space.World);
            }

            prevPinchDistance = distance;
            prevTwistAngle = angle;
            twoFingerActive = true;
            return;
        }

        twoFingerActive = false;

        if (count == 1)
        {
            Vector2 pos = TouchInput.Position(0);
            if (TouchInput.Began(0)) dragging = !TouchInput.IsOverUI(pos);

            if (dragging && raycastManager.Raycast(pos, hits, TrackableType.PlaneWithinPolygon))
                garden.transform.position = hits[0].pose.position;
        }
        else
        {
            dragging = false;
        }
    }

    void SetPlanesVisible(bool visible)
    {
        planesVisible = visible;
        if (!planeManager) return;
        if (!planeManager.enabled) planeManager.enabled = true;
        foreach (var plane in planeManager.trackables)
            if (plane.gameObject.activeSelf != visible) plane.gameObject.SetActive(visible);
    }

    void EnsureReticle()
    {
        if (reticle) return;

        if (reticlePrefab)
        {
            reticle = Instantiate(reticlePrefab);
        }
        else
        {
            reticle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            reticle.name = "Reticle";
            Destroy(reticle.GetComponent<Collider>());
            reticle.transform.localScale = new Vector3(0.15f, 0.002f, 0.15f);
            reticle.GetComponent<Renderer>().material.color = new Color(0.9f, 1f, 0.9f);
        }
        reticle.SetActive(false);
    }

    void SetReticleVisible(bool visible)
    {
        if (reticle && reticle.activeSelf != visible) reticle.SetActive(visible);
    }
}
