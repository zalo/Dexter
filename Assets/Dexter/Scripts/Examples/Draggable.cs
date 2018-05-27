using UnityEngine;

public class Draggable : MonoBehaviour {
  public Material targetMaterial;

  Color targetColor = Color.yellow;
  Vector3 hitPoint = Vector3.zero;
  Plane draggingPlane;

  private void OnMouseEnter() {
    targetColor.a = 0.5f;
    targetMaterial.color = targetColor;
  }

  private void OnMouseExit() {
    targetColor.a = 0.1f;
    targetMaterial.color = targetColor;
  }

  void OnMouseDown() {
    RaycastHit hit;
    Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit);
    hitPoint = hit.point;
    draggingPlane = new Plane(hit.normal.normalized, hitPoint);
  }

  void OnMouseDrag() {
    if (hitPoint != Vector3.zero) {
      float timeToHit = 0f; Ray cameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);
      if (draggingPlane.Raycast(cameraRay, out timeToHit)) {
        Vector3 planeHit = cameraRay.origin + (cameraRay.direction.normalized * timeToHit);
        transform.parent.position += planeHit - hitPoint;
        hitPoint = planeHit;
      } else {
        Debug.Log("GRAH.");
      }
    }
  }
}
