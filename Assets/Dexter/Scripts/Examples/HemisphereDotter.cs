using UnityEngine;
public class HemisphereDotter : MonoBehaviour {
  public Dexter dexterArm;
  public Transform Tooltip;
  public CCDIK ghostIK;
  public int numberOfPoints = 1000;
  public float steppingInterval = 2f;
  public bool isStepping = false;

  Vector3[] points = new Vector3[100];
  int currentDot = 0;
  float steppingTimer = 0f;
  Vector3 originalPosition;
  Quaternion originalRotation;

  void Start() {
    originalPosition = Tooltip.position;
    originalRotation = Tooltip.rotation;
    GeneratePointPattern(numberOfPoints);
    if (isStepping) {
      SetEffectorPositionRotation(transform.position + (transform.up*transform.lossyScale.y*0.5f), Quaternion.Euler(180f, 0f, 0f));
      SetPosition(currentDot);
    }
  }

  void Update() {
    if (isStepping) {
      if (steppingTimer > steppingInterval) {
        if (currentDot < points.Length - 1) {
          currentDot++;
          SetPosition(currentDot);
          steppingTimer -= steppingInterval;
        } else {
          currentDot = 0;
          Tooltip.position = originalPosition;
          Tooltip.rotation = originalRotation;
          isStepping = false;
        }
      }
      steppingTimer += Time.deltaTime;
    }
  }

  public void SetPosition(int currentDot) {
    Quaternion toolRotation = Quaternion.LookRotation(points[currentDot] - transform.position, Vector3.down);
    Vector3 normalOffset = (transform.position - points[currentDot]).normalized * 0.05f;

    SetEffectorPositionRotation(points[currentDot] + normalOffset, toolRotation);
    SetEffectorPositionRotation(points[currentDot], toolRotation);
    SetEffectorPositionRotation(points[currentDot] + normalOffset, toolRotation);
  }

  void SetEffectorPositionRotation(Vector3 position, Quaternion rotation) {
    Tooltip.position = position;
    Tooltip.rotation = rotation;
    ghostIK.Update();
    ghostIK.moveDexterToIK(dexterArm);
  }

  public void OnDrawGizmos() {
    if (!Application.isPlaying || points == null) { GeneratePointPattern(numberOfPoints); }
    foreach(Vector3 dot in points) {
      Gizmos.DrawSphere(dot, 0.005f);
    }
    for (int i = currentDot; i < Mathf.Min(currentDot+3, points.Length-1); i++) {
      Gizmos.DrawLine(points[i], points[i + 1]);
    }
  }

  //Adapted from: https://stackoverflow.com/a/26127012
  public void GeneratePointPattern(int numPoints) {
    if(points != null || points.Length != numPoints) { points = new Vector3[numPoints]; }
    float offset = 1f / numPoints;
    float increment = Mathf.PI * (3f - Mathf.Sqrt(5f));

    int counter = 0;
    for (int i = 0; i < numPoints; i++) {
      float y = ((i * offset) - 1f) + (offset / 2f);
        float r = Mathf.Sqrt(1f - Mathf.Pow(y, 2f));
        float phi = (i % numPoints) * increment;

        points[counter++] = transform.TransformPoint(new Vector3(Mathf.Cos(phi) * r, y,
                                                                 Mathf.Sin(phi) * r) * 0.5f);
    }
  }
}
