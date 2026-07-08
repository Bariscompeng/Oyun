using UnityEngine;
using System.Collections;

public class SteerLogger : MonoBehaviour
{
    public static string LogResult = "";
    
    private IEnumerator Start()
    {
        LogResult = "Logger started\n";
        var car = GameObject.Find("Car");
        if (car == null)
        {
            LogResult += "Car not found\n";
            yield break;
        }
        var controller = car.GetComponent<CarController>();
        var type = typeof(CarController);
        var wheelFL = (WheelCollider)type.GetField("wheelFL", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(controller);
        var visualFL = (Transform)type.GetField("visualFL", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(controller);
        
        float elapsed = 0f;
        while (elapsed < 0.5f)
        {
            // Force steer target
            type.GetField("currentSteer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(controller, 1.0f);
            wheelFL.steerAngle = 30f;
            
            wheelFL.GetWorldPose(out Vector3 pos, out Quaternion rot);
            LogResult += $"Time: {Time.time:F2} | steerAngle: {wheelFL.steerAngle} | poseRot: {rot.eulerAngles} | visualLocalRot: {visualFL.localRotation.eulerAngles}\n";
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        LogResult += "Logger finished\n";
    }
}
