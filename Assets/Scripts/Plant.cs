using System.Collections;
using UnityEngine;

public enum PlantStage { Seed, Sprout, Young, Bloom, Dead }

public class Plant : MonoBehaviour
{
    public float secondsToBloom = 60f;
    [Range(0f, 1f)] public float sproutAt = 0.15f;
    [Range(0f, 1f)] public float youngAt = 0.5f;

    [Range(0f, 1f)] public float startWater = 0.6f;
    public float waterDrainPerSecond = 0.05f;
    public float waterPerTap = 0.35f;
    [Range(0f, 1f)] public float overwaterThreshold = 0.9f;
    public float overwaterDamage = 0.1f;

    public float thirstDamagePerSecond = 0.08f;
    public float healthRegenPerSecond = 0.02f;

    public GameObject[] stageModels;

    public PlantStage Stage { get; private set; }
    public float Growth { get; private set; }
    public float Water { get; private set; }
    public float Health { get; private set; }
    public bool IsAlive => Stage != PlantStage.Dead;
    public bool IsBloomed => Stage == PlantStage.Bloom;

    static readonly Color healthyGreen = new Color(0.2f, 0.7f, 0.25f);
    static readonly Color sickBrown = new Color(0.45f, 0.35f, 0.15f);
    static readonly Color flowerColor = new Color(1f, 0.45f, 0.7f);
    static readonly Color seedColor = new Color(0.3f, 0.2f, 0.12f);
    static readonly Color waterFull = new Color(0.2f, 0.55f, 1f);
    static readonly Color waterEmpty = new Color(1f, 0.25f, 0.2f);

    bool useCustomModels;
    Transform visualRoot;
    Transform popTarget;
    Vector3 popBaseScale;
    Coroutine popRoutine;

    Transform stem, leafLeft, leafRight, flower, seedMound, waterBar;
    Renderer stemR, leafLeftR, leafRightR, waterBarR;

    void Awake()
    {
        useCustomModels = stageModels != null && stageModels.Length >= 5;
        if (!useCustomModels) BuildPlaceholder();
        BuildWaterBar();
        EnsureCollider();

        popTarget = useCustomModels ? transform : visualRoot;
        popBaseScale = popTarget.localScale;

        ResetPlant();
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing) return;
        if (!IsAlive || IsBloomed) return;

        float dt = Time.deltaTime;
        Water = Mathf.Max(0f, Water - waterDrainPerSecond * dt);

        if (Water > 0f)
        {
            Growth = Mathf.Min(1f, Growth + dt / secondsToBloom);
            Health = Mathf.Min(1f, Health + healthRegenPerSecond * dt);
        }
        else
        {
            Health -= thirstDamagePerSecond * dt;
        }

        if (Health <= 0f) { Die(); return; }

        UpdateStageFromGrowth();
        RefreshVisuals();
    }

    public void OnTapped()
    {
        if (!IsAlive) { ResetPlant(); Pop(); return; }
        if (IsBloomed) { Pop(); return; }

        if (Water >= overwaterThreshold)
        {
            Health -= overwaterDamage;
            if (Health <= 0f) { Die(); return; }
        }

        Water = Mathf.Min(1f, Water + waterPerTap);
        Pop();
        RefreshVisuals();
    }

    public void TakeDamage(float amount)
    {
        if (!IsAlive || IsBloomed) return;
        Health -= amount;
        if (Health <= 0f) Die();
    }

    public void ResetPlant()
    {
        Stage = PlantStage.Seed;
        Growth = 0f;
        Water = startWater;
        Health = 1f;
        if (visualRoot) visualRoot.localRotation = Quaternion.identity;
        RefreshVisuals();
    }

    void UpdateStageFromGrowth()
    {
        PlantStage newStage =
            Growth >= 1f ? PlantStage.Bloom :
            Growth >= youngAt ? PlantStage.Young :
            Growth >= sproutAt ? PlantStage.Sprout :
            PlantStage.Seed;

        if (newStage == Stage) return;
        Stage = newStage;
        Pop();
        if (Stage == PlantStage.Bloom && GameManager.Instance != null)
            GameManager.Instance.RegisterBloom();
    }

    void Die()
    {
        Health = 0f;
        Stage = PlantStage.Dead;
        if (visualRoot) visualRoot.localRotation = Quaternion.Euler(0f, 0f, 40f);
        RefreshVisuals();
        if (GameManager.Instance != null) GameManager.Instance.RegisterDeath();
    }

    void RefreshVisuals()
    {
        if (useCustomModels)
        {
            for (int i = 0; i < stageModels.Length; i++)
                if (stageModels[i]) stageModels[i].SetActive(i == (int)Stage);
        }
        else
        {
            UpdatePlaceholder();
        }
        UpdateWaterBar();
    }

    void UpdatePlaceholder()
    {
        bool dead = Stage == PlantStage.Dead;
        bool showStem = Stage != PlantStage.Seed;

        seedMound.gameObject.SetActive(Stage == PlantStage.Seed);
        stem.gameObject.SetActive(showStem);
        leafLeft.gameObject.SetActive(showStem);
        leafRight.gameObject.SetActive(showStem);
        flower.gameObject.SetActive(Stage == PlantStage.Bloom);

        float height = Mathf.Lerp(0.015f, 0.11f, Growth);
        stem.localScale = new Vector3(0.01f, height * 0.5f, 0.01f);
        stem.localPosition = new Vector3(0f, height * 0.5f, 0f);

        float leaf = Mathf.Lerp(0.015f, 0.045f, Growth);
        leafLeft.localScale = new Vector3(leaf, leaf * 0.3f, leaf * 0.55f);
        leafLeft.localPosition = new Vector3(-leaf * 0.5f, height * 0.55f, 0f);
        leafLeft.localRotation = Quaternion.Euler(0f, 0f, 25f);
        leafRight.localScale = leafLeft.localScale;
        leafRight.localPosition = new Vector3(leaf * 0.5f, height * 0.45f, 0f);
        leafRight.localRotation = Quaternion.Euler(0f, 0f, -25f);

        flower.localScale = Vector3.one * 0.045f;
        flower.localPosition = new Vector3(0f, height + 0.015f, 0f);

        Color c = dead ? sickBrown : Color.Lerp(sickBrown, healthyGreen, Health);
        stemR.material.color = c;
        leafLeftR.material.color = c;
        leafRightR.material.color = c;
    }

    void UpdateWaterBar()
    {
        bool show = IsAlive && !IsBloomed;
        waterBar.gameObject.SetActive(show);
        if (!show) return;
        waterBar.localScale = new Vector3(Mathf.Max(0.003f, 0.07f * Water), 0.008f, 0.008f);
        waterBarR.material.color = Color.Lerp(waterEmpty, waterFull, Water);
    }

    void BuildPlaceholder()
    {
        visualRoot = new GameObject("Visual").transform;
        visualRoot.SetParent(transform, false);

        seedMound = CreatePart(PrimitiveType.Sphere, "Seed", seedColor, out _);
        seedMound.localScale = new Vector3(0.035f, 0.015f, 0.035f);
        stem = CreatePart(PrimitiveType.Cylinder, "Stem", healthyGreen, out stemR);
        leafLeft = CreatePart(PrimitiveType.Sphere, "LeafL", healthyGreen, out leafLeftR);
        leafRight = CreatePart(PrimitiveType.Sphere, "LeafR", healthyGreen, out leafRightR);
        flower = CreatePart(PrimitiveType.Sphere, "Flower", flowerColor, out _);
    }

    Transform CreatePart(PrimitiveType type, string partName, Color color, out Renderer r)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = partName;
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(visualRoot, false);
        r = go.GetComponent<Renderer>();
        r.material.color = color;
        return go.transform;
    }

    void BuildWaterBar()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "WaterBar";
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 0.17f, 0f);
        waterBar = go.transform;
        waterBarR = go.GetComponent<Renderer>();
    }

    void EnsureCollider()
    {
        if (GetComponent<Collider>()) return;
        var box = gameObject.AddComponent<BoxCollider>();
        box.size = new Vector3(0.08f, 0.16f, 0.08f);
        box.center = new Vector3(0f, 0.08f, 0f);
    }

    void Pop()
    {
        if (!isActiveAndEnabled || popTarget == null) return;
        if (popRoutine != null) StopCoroutine(popRoutine);
        popRoutine = StartCoroutine(PopRoutine());
    }

    IEnumerator PopRoutine()
    {
        const float duration = 0.18f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            popTarget.localScale = popBaseScale * (1f + 0.2f * Mathf.Sin(t / duration * Mathf.PI));
            yield return null;
        }
        popTarget.localScale = popBaseScale;
        popRoutine = null;
    }
}
