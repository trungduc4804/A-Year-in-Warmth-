using UnityEngine;

/// <summary>
/// Công cụ tạo môi trường cảnh quan mẫu (Test Scenic Park) nhanh chóng trong Unity.
/// Giúp bạn kiểm tra ngay cảm giác đi bộ thong thả, ngắm cảnh quanh các hàng cây,
/// ghế đá, đèn đường và lối đi mà không cần chờ vẽ xong toàn bộ Sprite.
/// </summary>
[ExecuteInEditMode]
public class ScenicEnvironmentGenerator : MonoBehaviour
{
    [Header("=== CÀI ĐẶT CẢNH QUAN MẪU ===")]
    [Tooltip("Bán kính khu vực dạo chơi ngắm cảnh (units).")]
    [Range(10, 40)]
    public int parkRadius = 20;

    [Tooltip("Khoảng cách giữa các mốc đá lát đường.")]
    public float pathTileSpacing = 1.5f;

    [Tooltip("Tự động tạo khung cảnh khi bắt đầu Play Mode nếu chưa có.")]
    public bool autoGenerateOnPlay = true;

    [Header("=== MÀU SẮC ẤM ÁP (COZY PALETTE) ===")]
    public Color pathColor = new Color(0.82f, 0.76f, 0.65f, 0.9f);       // Màu sỏi ấm
    public Color grassPatchColor = new Color(0.45f, 0.68f, 0.42f, 0.6f); // Thảm cỏ xanh mát
    public Color treeTrunkColor = new Color(0.48f, 0.32f, 0.22f, 1f);     // Thân cây gỗ
    public Color treeCanopyColor = new Color(0.35f, 0.65f, 0.35f, 0.95f); // Tán lá
    public Color autumnCanopyColor = new Color(0.85f, 0.52f, 0.25f, 0.95f); // Tán lá mùa thu
    public Color lanternColor = new Color(1.0f, 0.85f, 0.45f, 1f);       // Đèn lồng vàng ấm

    private const string ContainerName = "_ScenicPark_Environment";

    private void Start()
    {
        if (Application.isPlaying && autoGenerateOnPlay)
        {
            Transform existing = transform.Find(ContainerName);
            if (existing == null)
            {
                GeneratePark();
            }
        }
    }

    [ContextMenu("🌿 Tạo khu dạo cảnh mẫu (Generate Scenic Park)")]
    public void GeneratePark()
    {
        ClearPark();

        GameObject container = new GameObject(ContainerName);
        container.transform.SetParent(transform);
        container.transform.localPosition = Vector3.zero;

        // Tạo Sprite chuẩn mặc định từ Texture2D runtime
        Sprite defaultSprite = CreateSoftSquareSprite();

        // 1. Tạo lối đi dạo chữ thập & đường vòng tròn ngắm cảnh
        for (int x = -parkRadius; x <= parkRadius; x += 2)
        {
            CreateMarker(container.transform, new Vector3(x, 0, 0), new Vector3(1.2f, 1.2f, 1), pathColor, -10, "Path_H_" + x, defaultSprite);
        }
        for (int y = -parkRadius; y <= parkRadius; y += 2)
        {
            CreateMarker(container.transform, new Vector3(0, y, 0), new Vector3(1.2f, 1.2f, 1), pathColor, -10, "Path_V_" + y, defaultSprite);
        }

        // 2. Tạo các hàng cây và cụm cảnh quan hai bên lối đi
        Random.InitState(42); // Seed cố định để cảnh quan ổn định

        for (int i = 0; i < 36; i++)
        {
            float posX = Random.Range(-parkRadius + 2f, parkRadius - 2f);
            float posY = Random.Range(-parkRadius + 2f, parkRadius - 2f);

            // Tránh đặt đè lên lối đi chính
            if (Mathf.Abs(posX) < 2.0f && Mathf.Abs(posY) < 2.0f) continue;
            if (Mathf.Abs(posX) < 1.2f || Mathf.Abs(posY) < 1.2f) continue;

            Vector3 treePos = new Vector3(posX, posY, 0);
            Color canopyColor = (i % 3 == 0) ? autumnCanopyColor : treeCanopyColor;

            CreateTree(container.transform, treePos, canopyColor, defaultSprite, i);
        }

        // 3. Tạo một số cột đèn ấm áp dọc lối đi
        for (int x = -parkRadius + 4; x <= parkRadius - 4; x += 6)
        {
            if (x == 0) continue;
            CreateLantern(container.transform, new Vector3(x, 1.4f, 0), lanternColor, defaultSprite);
            CreateLantern(container.transform, new Vector3(x, -1.4f, 0), lanternColor, defaultSprite);
        }

        Debug.Log("[ScenicEnvironmentGenerator] Đã tạo công viên dạo bộ mẫu thành công! Hãy nhấn Play để thử cảm giác đi dạo thư thả.");
    }

    [ContextMenu("🗑️ Xoá khu dạo cảnh mẫu (Clear Scenic Park)")]
    public void ClearPark()
    {
        Transform existing = transform.Find(ContainerName);
        while (existing != null)
        {
            if (Application.isPlaying)
                Destroy(existing.gameObject);
            else
                DestroyImmediate(existing.gameObject);

            existing = transform.Find(ContainerName);
        }
    }

    private void CreateTree(Transform parent, Vector3 pos, Color canopyColor, Sprite sprite, int index)
    {
        GameObject tree = new GameObject($"Tree_{index}");
        tree.transform.SetParent(parent);
        tree.transform.position = pos;

        // Thân cây
        GameObject trunk = new GameObject("Trunk");
        trunk.transform.SetParent(tree.transform);
        trunk.transform.localPosition = new Vector3(0, -0.3f, 0);
        trunk.transform.localScale = new Vector3(0.35f, 0.8f, 1f);
        SpriteRenderer trunkSr = trunk.AddComponent<SpriteRenderer>();
        trunkSr.sprite = sprite;
        trunkSr.color = treeTrunkColor;
        trunkSr.sortingOrder = 1;

        // Tán cây
        GameObject canopy = new GameObject("Canopy");
        canopy.transform.SetParent(tree.transform);
        canopy.transform.localPosition = new Vector3(0, 0.4f, 0);
        canopy.transform.localScale = new Vector3(1.6f, 1.6f, 1f);
        SpriteRenderer canopySr = canopy.AddComponent<SpriteRenderer>();
        canopySr.sprite = sprite;
        canopySr.color = canopyColor;
        canopySr.sortingOrder = 2;
    }

    private void CreateLantern(Transform parent, Vector3 pos, Color lightColor, Sprite sprite)
    {
        GameObject lantern = new GameObject("Lantern");
        lantern.transform.SetParent(parent);
        lantern.transform.position = pos;

        // Cột đèn
        GameObject post = new GameObject("Post");
        post.transform.SetParent(lantern.transform);
        post.transform.localPosition = new Vector3(0, 0, 0);
        post.transform.localScale = new Vector3(0.15f, 0.9f, 1f);
        SpriteRenderer postSr = post.AddComponent<SpriteRenderer>();
        postSr.sprite = sprite;
        postSr.color = new Color(0.2f, 0.2f, 0.25f, 1f);
        postSr.sortingOrder = 3;

        // Chụp đèn ấm
        GameObject bulb = new GameObject("Bulb");
        bulb.transform.SetParent(lantern.transform);
        bulb.transform.localPosition = new Vector3(0, 0.5f, 0);
        bulb.transform.localScale = new Vector3(0.4f, 0.4f, 1f);
        SpriteRenderer bulbSr = bulb.AddComponent<SpriteRenderer>();
        bulbSr.sprite = sprite;
        bulbSr.color = lightColor;
        bulbSr.sortingOrder = 4;
    }

    private void CreateMarker(Transform parent, Vector3 pos, Vector3 scale, Color color, int order, string name, Sprite sprite)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent);
        obj.transform.position = pos;
        obj.transform.localScale = scale;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = order;
    }

    private Sprite CreateSoftSquareSprite()
    {
        Texture2D texture = new Texture2D(32, 32);
        Color[] colors = new Color[32 * 32];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.white;
        }
        texture.SetPixels(colors);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);
    }
}
