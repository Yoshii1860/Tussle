using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(SortingGroup))]
public class DynamicSorting : MonoBehaviour
{
    [SerializeField] private Transform targetTransform; // The transform to follow for sorting order
    private SortingGroup sortingGroup;

    void Start()
    {
        sortingGroup = GetComponent<SortingGroup>();
    }

    void Update()
    {
        float yPos = targetTransform.position.y;
        sortingGroup.sortingOrder = -Mathf.RoundToInt(yPos * 100) + 1;
    }
}