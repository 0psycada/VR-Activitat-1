using System.Collections;
using UnityEngine;

public class Bug : MonoBehaviour
{
    public float speed = 0.03f;
    public float reachDistance = 0.03f;
    public float damagePerSecond = 0.6f;
    public Color bodyColor = new Color(0.6f, 0.1f, 0.1f);
    public Color headColor = new Color(0.1f, 0.08f, 0.08f);

    Garden garden;
    Plant target;
    bool squashed;
    Transform body;
    Vector3 bodyBaseScale;
    float wobble;

    public bool IsSquashed => squashed;

    void Awake()
    {
        if (transform.childCount == 0) BuildPlaceholder();
        body = transform.childCount > 0 ? transform.GetChild(0) : transform;
        bodyBaseScale = body.localScale;

        if (!GetComponent<Collider>())
        {
            var col = gameObject.AddComponent<SphereCollider>();
            col.radius = 0.03f;
            col.center = new Vector3(0f, 0.01f, 0f);
        }
    }

    public void Init(Garden owner, Plant firstTarget)
    {
        garden = owner;
        target = firstTarget;
    }

    void Update()
    {
        if (squashed) return;
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing) return;

        if (target == null || !target.IsAlive || target.IsBloomed)
        {
            target = garden ? garden.GetRandomTargetPlant() : null;
            if (target == null) return;
        }

        Vector3 pos = transform.localPosition;
        Vector3 goal = target.transform.localPosition;
        goal.y = pos.y;
        Vector3 toGoal = goal - pos;
        float dist = toGoal.magnitude;

        if (dist > reachDistance)
        {
            Vector3 dir = toGoal / dist;
            pos += dir * Mathf.Min(speed * Time.deltaTime, dist);
            if (garden) pos.y = garden.SurfaceHeightAt(pos);
            transform.localPosition = pos;
            transform.localRotation = Quaternion.LookRotation(dir, Vector3.up);

            wobble += Time.deltaTime * 20f;
            body.localRotation = Quaternion.Euler(0f, Mathf.Sin(wobble) * 15f, 0f);
        }
        else
        {
            target.TakeDamage(damagePerSecond * Time.deltaTime);
            wobble += Time.deltaTime * 30f;
            body.localScale = bodyBaseScale * (1f + 0.1f * Mathf.Sin(wobble));
        }
    }

    public void Squash()
    {
        if (squashed) return;
        squashed = true;
        var col = GetComponent<Collider>();
        if (col) col.enabled = false;
        if (GameManager.Instance != null) GameManager.Instance.RegisterBugSquashed();
        StartCoroutine(SquashRoutine());
    }

    IEnumerator SquashRoutine()
    {
        Vector3 start = body.localScale;
        Vector3 flat = new Vector3(start.x * 1.4f, start.y * 0.1f, start.z * 1.4f);
        const float duration = 0.15f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            body.localScale = Vector3.Lerp(start, flat, t / duration);
            yield return null;
        }
        yield return new WaitForSeconds(0.4f);
        Destroy(gameObject);
    }

    void BuildPlaceholder()
    {
        var container = new GameObject("Body").transform;
        container.SetParent(transform, false);
        MakePart(container, new Vector3(0f, 0.01f, 0f), new Vector3(0.03f, 0.018f, 0.04f), bodyColor);
        MakePart(container, new Vector3(0f, 0.012f, 0.022f), new Vector3(0.016f, 0.014f, 0.016f), headColor);
    }

    static void MakePart(Transform parent, Vector3 localPos, Vector3 localScale, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = localScale;
        go.GetComponent<Renderer>().material.color = color;
    }
}
