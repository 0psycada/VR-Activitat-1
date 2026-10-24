using System.Collections.Generic;
using UnityEngine;

public class Garden : MonoBehaviour
{
    public Plant plantPrefab;
    public Transform[] plotPoints;
    public int plotCount = 3;
    public float plotSpacing = 0.13f;

    public bool buildSoilBed = true;
    public Color soilColor = new Color(0.36f, 0.25f, 0.17f);
    public float soilHeight = 0.015f;
    public float soilDepth = 0.12f;

    readonly List<Plant> plants = new List<Plant>();
    static readonly List<Plant> candidates = new List<Plant>();
    Vector2 bedHalfSize;

    public IReadOnlyList<Plant> Plants => plants;

    void Awake()
    {
        Build();
    }

    void Build()
    {
        bool customPlots = plotPoints != null && plotPoints.Length > 0;
        int count = customPlots ? plotPoints.Length : plotCount;

        if (buildSoilBed)
        {
            var bed = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bed.name = "SoilBed";
            Destroy(bed.GetComponent<Collider>());
            bed.transform.SetParent(transform, false);
            float width = count * plotSpacing + 0.06f;
            bed.transform.localScale = new Vector3(width, soilHeight, soilDepth);
            bed.transform.localPosition = new Vector3(0f, soilHeight * 0.5f, 0f);
            bed.GetComponent<Renderer>().material.color = soilColor;
            bedHalfSize = new Vector2(width * 0.5f, soilDepth * 0.5f);
        }

        float plantY = buildSoilBed ? soilHeight : 0f;

        for (int i = 0; i < count; i++)
        {
            Vector3 localPos = customPlots
                ? plotPoints[i].localPosition
                : new Vector3((i - (count - 1) * 0.5f) * plotSpacing, plantY, 0f);

            Plant plant = plantPrefab
                ? Instantiate(plantPrefab, transform)
                : new GameObject("Plant").AddComponent<Plant>();

            plant.transform.SetParent(transform, false);
            plant.transform.localPosition = localPos;
            plant.transform.localRotation = Quaternion.identity;
            plant.name = "Plant " + (i + 1);
            plants.Add(plant);
        }
    }

    public Plant GetRandomTargetPlant()
    {
        candidates.Clear();
        foreach (var p in plants)
            if (p != null && p.IsAlive && !p.IsBloomed) candidates.Add(p);
        return candidates.Count == 0 ? null : candidates[Random.Range(0, candidates.Count)];
    }

    public float SurfaceHeightAt(Vector3 localPos)
    {
        if (!buildSoilBed) return 0f;
        bool inside = Mathf.Abs(localPos.x) <= bedHalfSize.x && Mathf.Abs(localPos.z) <= bedHalfSize.y;
        return inside ? soilHeight : 0f;
    }
}
