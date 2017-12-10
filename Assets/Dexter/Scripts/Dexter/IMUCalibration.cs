using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IMUCalibration : MonoBehaviour {
  public Dexter dexter;
  public KeyCode key1;
  public KeyCode key2;

  int rotatorRotation = 0, endRotation = 0;

  void Start () {}
	
	// Update is called once per frame
	void Update () {
    if (Input.GetKeyDown(key1) || Input.GetKeyDown(key2)) {
      if (Input.GetKeyDown(key1)) {
        dexter.lastRotatorRotTime = Time.time;

        if (rotatorRotation == 0) {
          rotatorRotation = -90 * 3600;
        } else {
          rotatorRotation = 0;
        }
      } else if (Input.GetKeyDown(key2)) {
        dexter.lastEndRotTime = Time.time;

        if (endRotation == 0) {
          endRotation = -90 * 3600;
        } else {
          endRotation = 0;
        }
      }

      dexter.sendStringToDexter(dexter.dexterSocket, "a 0 0 0 " + rotatorRotation + " " + endRotation + "; ");
    }
  }
}
