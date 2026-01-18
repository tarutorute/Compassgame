using UnityEngine;

public class NeedleSpinner : MonoBehaviour
{
    [Header("Assign")]
    public Transform needlePivot;   // NeedlePivot Çì¸ÇÍÇÈ

    [Header("Spin")]
    public float spinSpeed = 240f;  // ìx/ïb
    public bool isSpinning;

    void Update()
    {
        if (isSpinning && needlePivot != null)
        {
            needlePivot.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.Self);
        }
    }

    public float CurrentAngleY()
    {
        // 0Å`360 ÇÃäpìx
        return needlePivot.eulerAngles.y;
    }
}
