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
      MoveEffectorPositionRotation(transform.position + (transform.up*transform.lossyScale.y*0.5f), Quaternion.Euler(180f, 0f, 0f));
      PokePosition(currentDot);
    }
  }

  void Update() {
    if (isStepping) {
      if (steppingTimer > steppingInterval) {
        if (currentDot < points.Length - 1) {
          currentDot++;
          PokePosition(currentDot);
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

  public void PokePosition(int currentDot) {
    Quaternion toolRotation = Quaternion.LookRotation(points[currentDot] - transform.position, Vector3.down);
    Vector3 normalOffset = (transform.position - points[currentDot]).normalized * 0.05f;

    MoveEffectorPositionRotation(points[currentDot] + normalOffset, toolRotation);
    MoveEffectorPositionRotation(points[currentDot], toolRotation);
    MoveEffectorPositionRotation(points[currentDot] + normalOffset, toolRotation);
  }

  void MoveEffectorPositionRotation(Vector3 position, Quaternion rotation) {
    if (Tooltip != null) {
      Tooltip.position = position;
      Tooltip.rotation = rotation;
    }
    if (ghostIK != null) {
      ghostIK.Update();
      ghostIK.moveDexterToIK(dexterArm);
    }
  }

  //Draw the Dots to Poke
  public void OnDrawGizmos() {
    if (!Application.isPlaying || points == null) { GeneratePointPattern(numberOfPoints); }
    Gizmos.color = Color.green;
    for (int i = 0; i < points.Length; i++) {
      if(i == currentDot+1) { Gizmos.color = Color.white; }
      Gizmos.DrawSphere(points[i], 0.005f);
    }
    //for (int i = currentDot; i < Mathf.Min(currentDot+3, points.Length-1); i++) {
    //  Gizmos.DrawLine(points[i], points[i + 1]);
    //}
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

  //Reset when we're Done
  private void OnDestroy() {
    MoveEffectorPositionRotation(transform.position + (transform.up * transform.lossyScale.y * 0.5f), Quaternion.Euler(180f, 0f, 0f));
    dexterArm.sendStringToDexter("a 0 0 0 0 0;");
  }
}
