using UnityEngine;

public class CCDIK : MonoBehaviour {
  public Transform Tooltip;
  public Transform Target;
  public CCDIKJoint[] joints;

  public void Update() {
    for (int i = 0; i < 100; i++) {
      for (int j = 0; j < joints.Length; j++) {
        joints[j].Evaluate(Tooltip, Target, j < 2);
      }
    }
  }

  public void moveDexterToIK(Dexter dexter) {
    int baseRotation = (int)(getAngle(joints[4].transform.localRotation, Vector3.up) * 3600f);
    int pivotRotation = (int)(getAngle(joints[3].transform.localRotation, Vector3.left) * 3600f);
    int elbowRotation = (int)(getAngle(joints[2].transform.localRotation, Vector3.left) * 3600f);
    int rotatorRotation = (int)(getAngle(joints[1].transform.localRotation, Vector3.left) * 3600f);
    int endRotation = (int)(getAngle(joints[0].transform.localRotation, Vector3.up) * 3600f);
    dexter.sendStringToDexter("a " + baseRotation + " " + pivotRotation + " " + elbowRotation + " " + rotatorRotation + " " + endRotation + "; ");
  }

  public static float getAngle(Quaternion quat, Vector3 axis) {
    Quaternion normalized = Quaternion.Lerp(quat, quat, 0f);
    float quatAngle; Vector3 quatAxis;
    normalized.ToAngleAxis(out quatAngle, out quatAxis);
    if (Vector3.Dot(axis, quatAxis) < 0f) { quatAngle *= -1f; }
    return quatAngle;
  }
}
