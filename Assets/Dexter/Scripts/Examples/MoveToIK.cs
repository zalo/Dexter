using UnityEngine;

public class MoveToIK : MonoBehaviour {
  public Dexter dexter;
  public CCDIK ik;
  public KeyCode key;

	// Use this for initialization
	void Start () {}
	
	// Update is called once per frame
	void Update () {
    int baseRotation = (int)(getAngle(ik.joints[4].transform.localRotation, Vector3.up) * 3600f);
    int pivotRotation = (int)(getAngle(ik.joints[3].transform.localRotation, Vector3.left) * 3600f);
    int elbowRotation = (int)(getAngle(ik.joints[2].transform.localRotation, Vector3.left) * 3600f);
    int rotatorRotation = (int)(getAngle(ik.joints[1].transform.localRotation, Vector3.left) * 3600f);
    int endRotation = (int)(getAngle(ik.joints[0].transform.localRotation, Vector3.up) * 3600f);
    if (Input.GetKeyDown(key)) {
      dexter.sendStringToDexter(dexter.dexterSocket, "a " + baseRotation + " " + pivotRotation + " " + elbowRotation + " " + rotatorRotation + " " + endRotation + "; ");
    }
  }

  public static float getAngle(Quaternion quat, Vector3 axis) {
    Quaternion normalized = Quaternion.Lerp(quat, quat, 0f);
    float quatAngle; Vector3 quatAxis;
    normalized.ToAngleAxis(out quatAngle, out quatAxis);
    if(Vector3.Dot(axis, quatAxis) < 0f) { quatAngle *= -1f; }
    return quatAngle;
  }
}
