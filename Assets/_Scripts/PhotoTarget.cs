using UnityEngine;

/// <summary>
/// Gắn lên bất kỳ đối tượng cảnh quan nào là mục tiêu cần chụp ảnh (Ví dụ: Cây Đào Phai, Đèn Lồng, Ghế Đá...).
/// Cung cấp thuật toán kiểm tra xem đối tượng có nằm trọn trong khung ngắm của máy ảnh Viewfinder hay không.
/// </summary>
[DisallowMultipleComponent]
public class PhotoTarget : MonoBehaviour
{
    [Header("=== THÔNG TIN MỤC TIÊU ẢNH (PHOTO TARGET) ===")]
    [Tooltip("Mã định danh của mục tiêu để so khớp với nhiệm vụ (ví dụ: PeachBlossom).")]
    public string targetId = "PeachBlossom";

    [Tooltip("Tên hiển thị của mục tiêu.")]
    public string targetDisplayName = "Cành Đào Phai";

    [Tooltip("Khoảng cách tối đa từ người chơi đến mục tiêu để bức ảnh được tính là hợp lệ (units).")]
    public float maxCaptureDistance = 14.0f;

    [Tooltip("Bán kính vùng chủ thể để vẽ Gizmo trong Editor.")]
    public float targetSubjectRadius = 1.2f;

    /// <summary>
    /// Kiểm tra xem đối tượng này có đang nằm bên trong khung chữ nhật của máy ảnh Viewfinder hay không
    /// </summary>
    /// <param name="cam">Camera đang thực hiện chụp</param>
    /// <param name="viewfinderRect">Khung chữ nhật của Viewfinder trên màn hình (GUI Rect, gốc trên-trái)</param>
    /// <param name="playerPosition">Vị trí hiện tại của người chơi Arthur</param>
    /// <returns>True nếu đối tượng được căn trọn trong khung ngắm và ở cự ly hợp lệ</returns>
    public bool IsInViewfinder(Camera cam, Rect viewfinderRect, Vector2 playerPosition)
    {
        if (cam == null) return false;

        // 1. Kiểm tra khoảng cách từ người chơi đến mục tiêu (không được đứng quá xa)
        float distToPlayer = Vector2.Distance(playerPosition, transform.position);
        if (distToPlayer > maxCaptureDistance)
        {
            return false;
        }

        // 2. Chuyển toạ độ 3D thế giới của mục tiêu sang toạ độ màn hình (Screen Space)
        Vector3 screenPos = cam.WorldToScreenPoint(transform.position);

        // Nếu đối tượng nằm sau lưng camera
        if (screenPos.z <= 0)
        {
            return false;
        }

        // 3. Đổi trục toạ độ Y từ Screen Space (gốc dưới-trái) sang GUI Rect (gốc trên-trái)
        float guiX = screenPos.x;
        float guiY = Screen.height - screenPos.y;
        Vector2 targetGuiPos = new Vector2(guiX, guiY);

        // 4. Kiểm tra xem điểm tâm mục tiêu có nằm gọn trong khung ngắm không
        return viewfinderRect.Contains(targetGuiPos);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1.0f, 0.65f, 0.75f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, targetSubjectRadius);
    }
}
