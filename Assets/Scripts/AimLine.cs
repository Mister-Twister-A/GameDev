using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class AimLine : MonoBehaviour
{
    public static AimLine Instance { get; private set; }

    [SerializeField] private Color color = Color.red;
    [SerializeField] private float width = 0.04f;
    [SerializeField] private Material material;

    private LineRenderer line;

    private void Awake()
    {
        Instance = this;

        line = GetComponent<LineRenderer>();
        line.positionCount = 2;
        line.useWorldSpace = true;
        line.startWidth = width;
        line.endWidth = width;
        line.startColor = color;
        line.endColor = color;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.sharedMaterial = material != null ? material : CreateDefaultMaterial();
        line.enabled = false;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private static Material CreateDefaultMaterial()
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        return new Material(shader);
    }

    public static void Show(Vector3 from, Vector3 to)
    {
        if (Instance == null) return;
        Instance.line.enabled = true;
        Instance.line.SetPosition(0, from);
        Instance.line.SetPosition(1, to);
    }

    public static void Hide()
    {
        if (Instance != null) Instance.line.enabled = false;
    }
}