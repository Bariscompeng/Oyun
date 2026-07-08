using UnityEngine;

public class TempWheelLogger : MonoBehaviour
{
    private int state = 0;
    private CarController cc;
    private WheelCollider col;
    private Transform visual;

    private void Start()
    {
        cc = GetComponent<CarController>();
        col = transform.Find("WheelColliders/FL")?.GetComponent<WheelCollider>();
        visual = transform.Find("RealCarVisual/WheelFrontL");
        
        if (cc != null) cc.enabled = false;
    }

    private void Update()
    {
        if (col == null || visual == null) return;

        if (state == 0)
        {
            col.steerAngle = 30f;
            Debug.Log($"[TempWheelLogger] Set steerAngle=30. col.steerAngle={col.steerAngle}");
            state = 1;
        }
        else if (state == 1)
        {
            // Physics has run 1 step now
            col.GetWorldPose(out Vector3 pos, out Quaternion rot);
            Debug.Log($"[TempWheelLogger] Physics updated: steer={col.steerAngle}, colRot={rot.eulerAngles}, visWorld={visual.rotation.eulerAngles}, visLocal={visual.localRotation.eulerAngles}");
            
            // Re-enable controller
            if (cc != null) cc.enabled = true;
            Destroy(this);
        }
    }
}
