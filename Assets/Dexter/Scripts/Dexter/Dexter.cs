using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class Dexter : MonoBehaviour {
  [NonSerialized]
  public TcpClient dexterSocket;
  public Transform[] joints;
  public byte[] ip = { 192, 168, 1, 250 };
  float sendTimer = 0;
  ProduceConsumeBuffer<Quaternion>[] jointBuffer = new ProduceConsumeBuffer<Quaternion>[5];
  ProduceConsumeBuffer<string> stringBuffer = new ProduceConsumeBuffer<string>(10);
  Quaternion[] jointAngles = new Quaternion[5];

  public TextMesh text;
  float lastEndRot = 0f;
  public float lastEndRotTime;
  float lastRotatorRot = 0f;
  public float lastRotatorRotTime;
  public float lastRotUpdateTime;

  void Start() {
    for(int i = 0; i<jointBuffer.Length; i++) {
      jointBuffer[i] = new ProduceConsumeBuffer<Quaternion>(10);
    }

    dexterSocket = new TcpClient();
    dexterSocket.SendBufferSize = 128;
    dexterSocket.ReceiveBufferSize = 240;
    try {
      dexterSocket.Connect(new IPAddress(ip), 50000);
      if (dexterSocket.Connected) {
        Debug.Log("Connected to " + dexterSocket.Client.RemoteEndPoint);
        //sendStringToDexter(dexterSocket, "S StartSpeed 10000;");
        //sendStringToDexter(dexterSocket, "S MaxSpeed 250000;");
        sendStringToDexter(dexterSocket, "a 0 0 0 0 0;");
      }
    } catch (Exception e) {
      Debug.Log(e.ToString());
    }
  }

  int count = 0;
  void Update() {
    //Heartbeat the Dexter to query its current pose
    if (dexterSocket != null && dexterSocket.Connected && sendTimer < Time.time) {
      sendStringToDexter(dexterSocket, "g;");
      count++;
      sendTimer += 0.016f;
    }

    //Dequeue the joint angles that were enqueued from the other (socket) thread
    //This is necessary since you cannot set joint angles directly from another thread
    for(int i=0; i<5; i++) {
      Quaternion jointRot = Quaternion.identity;
      if (jointBuffer[i].TryDequeue(out jointRot)) {
        jointAngles[i] = Quaternion.Slerp(joints[i].localRotation, jointRot, 1f);
        joints[i].localRotation = jointAngles[i];

        if (i == 3) {
          float angle = MoveToIK.getAngle(joints[i].localRotation, Vector3.right) / 90f;
          Debug.DrawLine(((lastRotUpdateTime - lastRotatorRotTime) * Vector3.right) + (Vector3.up * lastRotatorRot), ((Time.time - lastRotatorRotTime) * Vector3.right) + (Vector3.up * angle), Color.green, 5f);
          lastRotatorRot = angle;
        } else if (i == 4) {
          float angle = MoveToIK.getAngle(joints[i].localRotation, Vector3.right) / 90f;
          Debug.DrawLine(((lastRotUpdateTime - lastEndRotTime) * Vector3.right) + (Vector3.up * lastEndRot), ((Time.time - lastEndRotTime) * Vector3.right) + (Vector3.up * angle), Color.red, 5f);
          lastEndRot = angle;
          lastRotUpdateTime = Time.time;
        }
      }

    }

    string toDisplay;
    while(stringBuffer.TryDequeue(out toDisplay)) {
      text.text = toDisplay;
    }
  }

  public void sendStringToDexter(TcpClient socket, string command) {
    byte[] toSend = new byte[128];
    //The first four values don't do anything yet
    byte[] asciiBytes = Encoding.ASCII.GetBytes("xxx xxx xxx xxx "+ command);
    Array.Copy(asciiBytes, toSend, asciiBytes.Length);
    //dexterSocket.Client.Send(toSend);
    SocketAsyncEventArgs sendArgs = new SocketAsyncEventArgs();
    sendArgs.SetBuffer(toSend, 0, 128);
    sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(completeSendingCommand);
    dexterSocket.Client.SendAsync(sendArgs);
  }

  void completeSendingCommand(object sender = null, SocketAsyncEventArgs e = null) {
    byte[] receiveArray = new byte[240];
    SocketAsyncEventArgs receiveArgs = new SocketAsyncEventArgs();
    receiveArgs.SetBuffer(receiveArray, 0, 240);
    receiveArgs.Completed += new EventHandler<SocketAsyncEventArgs>(receiveUpdatedState);
    dexterSocket.Client.ReceiveAsync(receiveArgs);
  }

  void receiveUpdatedState(object sender, SocketAsyncEventArgs e) {
    int[] state = new int[60];
    Buffer.BlockCopy(e.Buffer, 0, state, 0, 240);
    string stateString = "";
    //Enumerates through the state on each joint individually
    //Unclear what each of these mean exactly, but good to have
    int jointNumber = 0;
    for (int i = 10; i < 60; i += 10) {
      Quaternion jointRot;
      float posState = state[i];// * (jointNumber > 2 ? 0.1f : 1f);
      float deltaState = state[i + 1] * (jointNumber > 2 ? 0.125f : 1f);// * (jointNumber == 1 ? -1f : 1f);
      float forceState = state[i + 3];// * (jointNumber == 1 ? -1f : 1f);
      float totalRotation = (posState + deltaState + forceState) / -3600f; ;// state[i + 2] + forceState + (state[i + 6]*-4f)) / -3600f;
      if (jointNumber == 0 || jointNumber == 4) {
        jointRot = Quaternion.Euler(0f, totalRotation * ((jointNumber == 4)|| (jointNumber == 0) ? -1f : 1f), 0f);
      } else {
        jointRot = Quaternion.Euler(totalRotation, 0f, 0f);
      }

      jointBuffer[jointNumber].TryEnqueue(ref jointRot);

      stateString += ("Pos: " + state[i] + ", ");
      stateString += ("Delta: " + state[i + 1] + ", ");
      stateString += ("PID Delta: " + state[i + 2] + ", ");
      stateString += ("Force Delta: " + state[i + 3] + ", ");
      stateString += ("Sin: " + state[i + 4] + ", ");
      stateString += ("Cos: " + state[i + 5] + ", ");
      //stateString += ("Sent Position: " + state[i + 6]*4 + ", ");
      stateString += "\n";

      //if(jointNumber == 4) { Debug.DrawLine(Vector3.up * state[i], (Vector3.up * state[i]) + Vector3.right); }

      jointNumber++;
    }
    //text.text = stateString;
    stringBuffer.TryEnqueue(ref stateString);
    //Debug.Log(stateString);
  }

  void OnDestroy () {
    try {
      dexterSocket.Close();
    } catch (Exception e) {
      Debug.Log(e.ToString());
    }
  }
}
