using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Turntable : MonoBehaviour {
  Vector3 prevPosition = Vector3.zero;
	void Update () {
    if (Input.GetMouseButton(0) && !Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition))) {
      if (Input.GetMouseButtonDown(0)) {
        prevPosition = Input.mousePosition;
      }
      Vector3 delta = Input.mousePosition - prevPosition;
      prevPosition = Input.mousePosition;

      transform.rotation *= Quaternion.Euler(0, delta.x, 0f);
    }
	}
}
