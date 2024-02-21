using UnityEngine;

public class StuckHelper {
    public float RayRadius { get; private set; }
    public int Mask { get; private set; }

    private RaycastHit hit;

    public bool IsHit { get; private set; }
    public Vector3 HitNormal { get { return hit.normal; } }
    public Vector3 HitPos { get { return hit.point; } }
    public float HitDistance { get { return hit.distance; } }



    public StuckHelper(float rayRadius, int mask) {
        RayRadius = rayRadius;
        Mask = mask;
    }

    #region Utility
    public void Raycast(Vector3 rayPos, Vector3 direction, float distance) {
        Vector3 p1 = rayPos;
        Vector3 p2 = p1 + Vector3.up * PlayerController.PlayerHeight; //임의로 player의 높이를 적용
        IsHit = Physics.CapsuleCast(p1, p2, RayRadius, direction, out hit, distance, Mask);
    }
    #endregion
}
