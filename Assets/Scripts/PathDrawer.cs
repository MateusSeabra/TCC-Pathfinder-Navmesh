using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(LineRenderer))]
public class NavMeshPathDrawer : MonoBehaviour
{
    [Header("Component References")]
    [Tooltip("Referência automática ao LineRenderer.")]
    [SerializeField] private LineRenderer lRenderer;

    [Header("State")]
    [SerializeField] private bool active = true;
    private Color32 originalColor;
    private int id;
    private bool mode;

    private const float VerticalOffset = 0.1f;

    private readonly Color32 PartialPathColor = new Color32(255, 165, 0, 255); 
    private readonly Color32 InvalidPathColor = new Color32(255, 0, 0, 255);   

    private void Awake()
    {
        if (lRenderer == null)
        {
            lRenderer = GetComponent<LineRenderer>();
        }

        if (lRenderer == null)
        {
            Debug.LogError($"[NavMeshPathDrawer] ERRO CRÍTICO: LineRenderer ausente no objeto '{gameObject.name}'. Desativando script.");
            this.enabled = false;
            return;
        }

        lRenderer.useWorldSpace = true;
        lRenderer.positionCount = 0;

        lRenderer.startWidth = 0.2f;
        lRenderer.endWidth = 0.2f;
    }

    private void Start()
    {
        if (lRenderer.sharedMaterial == null || lRenderer.sharedMaterial.shader.name.Contains("Hidden/InternalErrorShader"))
        {
            lRenderer.material = new Material(Shader.Find("Sprites/Default"));
        }
    }

    private void Update()
    {
        if (lRenderer == null) return;

        if (!active && Input.anyKeyDown)
            ActivateColor();

        if (Input.GetKeyDown(KeyCode.Alpha0))
            mode = !mode;

        HandleNumericInput(KeyCode.Alpha1, 0);
        HandleNumericInput(KeyCode.Alpha2, 1);
        HandleNumericInput(KeyCode.Alpha3, 2);
        HandleNumericInput(KeyCode.Alpha4, 3);
        HandleNumericInput(KeyCode.Alpha5, 4);
        HandleNumericInput(KeyCode.Alpha6, 5);
        HandleNumericInput(KeyCode.Alpha7, 6);
        HandleNumericInput(KeyCode.Alpha8, 7);
        HandleNumericInput(KeyCode.Alpha9, 8);

        if (Input.GetKeyDown(KeyCode.A) && id > 9) DisableColor();
        if (Input.GetKeyDown(KeyCode.S) && id <= 9) DisableColor();
        if (Input.GetKeyDown(KeyCode.D) && (id % 2) == 0) DisableColor();
        if (Input.GetKeyDown(KeyCode.F) && (id % 2) == 1) DisableColor();
    }

    private void HandleNumericInput(KeyCode key, int baseId)
    {
        if (Input.GetKeyDown(key))
        {
            DisableColor();
            if (id == (baseId + (mode ? 9 : 0)))
                ActivateColor();
        }
    }

    public void SetPath(int id, Vector3[] pathCorners, NavMeshPathStatus status, Color32 successColor)
    {
        if (lRenderer == null) return;

        this.id = id;

        if (status == NavMeshPathStatus.PathInvalid)
        {
            Debug.LogWarning($"[NavMeshPathDrawer] Caminho ID {id} é INVÁLIDO/INACESSÍVEL.");
            this.originalColor = InvalidPathColor; 
            lRenderer.positionCount = 0;
            return;
        }

        if (pathCorners == null || pathCorners.Length < 2)
        {
            Debug.LogWarning($"[NavMeshPathDrawer] Caminho inválido recebido para ID {id}. O caminho precisa de pelo menos 2 pontos.");
            this.originalColor = InvalidPathColor; 
            lRenderer.positionCount = 0;
            return;
        }

        Vector3[] visualCorners = new Vector3[pathCorners.Length];
        for (int i = 0; i < pathCorners.Length; i++)
        {
            visualCorners[i] = pathCorners[i] + Vector3.up * VerticalOffset;
        }

        lRenderer.positionCount = visualCorners.Length;
        lRenderer.SetPositions(visualCorners);

        if (status == NavMeshPathStatus.PathPartial)
        {
            this.originalColor = PartialPathColor; 
        }
        else 
        {
            this.originalColor = successColor;
        }

        ActivateColor();
    }

    private void DisableColor()
    {
        if (lRenderer == null) return;
        Color32 transparent = new Color32(255, 255, 255, 0);
        lRenderer.startColor = transparent;
        lRenderer.endColor = transparent;
        active = false;
    }

    private void ActivateColor()
    {
        if (lRenderer == null) return;
        lRenderer.startColor = originalColor;
        lRenderer.endColor = originalColor;
        active = true;
    }
}