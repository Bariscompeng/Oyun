using UnityEngine;

public class TestSteer : MonoBehaviour
{
    private int frames = 0;
    private void Update()
    {
        var ctrl = GetComponent<CarController>();
        if (ctrl != null)
        {
            var type = typeof(CarController);
            var steerField = type.GetField("currentSteer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (steerField != null)
            {
                steerField.SetValue(ctrl, 1.0f);
            }
        }
        
        frames++;
        if (frames == 20)
        {
            if (ctrl != null)
            {
                var flField = typeof(CarController).GetField("wheelFL", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var fl = (WheelCollider)flField.GetValue(ctrl);
                var visField = typeof(CarController).GetField("visualFL", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var vis = (Transform)visField.GetValue(ctrl);
                
                fl.GetWorldPose(out Vector3 pos, out Quaternion rot);
                Debug.Log($"[TestSteerLog] steerAngle={fl.steerAngle}, wheelWorldRot={rot.eulerAngles}, visualLocalRot={vis.localRotation.eulerAngles}, visualWorldRot={vis.rotation.eulerAngles}");
            }
            Destroy(this);
        }
    }
}
