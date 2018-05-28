using UnityEngine;
public class MoveToIK : MonoBehaviour {
  public Dexter dexter;
  public CCDIK ik;
  public KeyCode moveKey = KeyCode.Space;
  public KeyCode resetKey = KeyCode.R;
  void Update () {
    if (Input.GetKeyDown(moveKey)) {
      ik.moveDexterToIK(dexter);
    }
    if (Input.GetKeyDown(resetKey)) {
      dexter.sendStringToDexter("a 0 0 0 0 0;");
    }
  }
}
