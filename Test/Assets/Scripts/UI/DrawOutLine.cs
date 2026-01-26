using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class DrawOutLine : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Material glowMaterial; // 폰트 머터리얼 프리셋 할당

    private TextMeshProUGUI textMesh;
    private Material defaultMaterial;

    private void Awake()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
    }

    private void Start()
    {
        defaultMaterial = textMesh.fontSharedMaterial; // 기본 머터리얼 저장
    }

    public void OnPointerEnter(PointerEventData eventData) // 마우스 오버 시 호출
    {
        textMesh.fontSharedMaterial = glowMaterial;
    }

    public void OnPointerExit(PointerEventData eventData) // 마우스 아웃 시 호출
    {
        textMesh.fontSharedMaterial = defaultMaterial;
    }
}
