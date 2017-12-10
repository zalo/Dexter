using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour {
  public Vector3 start;
	// Use this for initialization
	void Start () {
    start = transform.position;

  }
	
	// Update is called once per frame
	void Update () {
    transform.position = start + Quaternion.Euler(Time.time*-200f, 0f, 0f) * Vector3.forward*0.2f;
	}
}
