using UnityEngine;

public class ArenaFence : MonoBehaviour
{
    [SerializeField] float radius = 6f;        // радиус бортика (край площадки)
    [SerializeField] int segments = 24;        // сколько кусков в круге
    [SerializeField] float height = 0.5f;      // высота бортика
    [SerializeField] float thickness = 0.2f;   // толщина
    [SerializeField] Material material;        // цвет бортика

    void Start()
    {
        float segmentLength = 2f * Mathf.PI * radius / segments;  // длина окружности / кол-во кусков

        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            Vector3 dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "FenceSegment";
            wall.transform.SetParent(transform);
            wall.transform.position = transform.position + dir * radius + Vector3.up * height / 2f;
            wall.transform.rotation = Quaternion.LookRotation(-dir);   // лицом к центру
            wall.transform.localScale = new Vector3(segmentLength, height, thickness);

            Destroy(wall.GetComponent<Collider>());   // не мешает пулям и врагам
            if (material != null) wall.GetComponent<Renderer>().material = material;
        }
    }
}