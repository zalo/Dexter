using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class Dexter : MonoBehaviour {
  [NonSerialized]
  public TcpClient dexterSocket;
  public Transform[] joints;
  public byte[] ip = { 192, 168, 1, 250 };
  public TextMesh diagnosticText;

  [NonSerialized]
  public Vector3Int[] rawJointAngles = new Vector3Int[5];

  float sendTimer = 0; bool hasStarted = false;
  ConcurrentQueue<Vector3Int[]> jointAngleBuffer = new ConcurrentQueue<Vector3Int[]>();
  ConcurrentQueue<string> diagnosticStringBuffer = new ConcurrentQueue<string>();

  void Start() {
    if (hasStarted) { return; }
    hasStarted = true;
    dexterSocket = new TcpClient();
    dexterSocket.SendBufferSize = 128;
    dexterSocket.ReceiveBufferSize = 240;
    try {
      IPAddress address = new IPAddress(ip);
      IAsyncResult result = dexterSocket.BeginConnect(address, 50000, null, null);
      bool success = result.AsyncWaitHandle.WaitOne(1000, true);

      if (success && dexterSocket.Connected) {
        dexterSocket.EndConnect(result);

        Debug.Log("Connected to " + dexterSocket.Client.RemoteEndPoint, this);
        //sendStringToDexter("S StartSpeed 0;"); //Good defaults
        //sendStringToDexter("S MaxSpeed 200000;"); //Good defaults
        //sendStringToDexter("a 0 0 0 0 0;"); //Good defaults
      } else {
        dexterSocket.Close();
        enabled = false;
        throw new TimeoutException("Connection attempt to Dexter timed out.  " +
                                   "Check that this is the correct IP Address: "+ address);
      }
    } catch (Exception e) {
      Debug.Log(e.ToString(), this);
    }
  }

  void Update() {
    if (sendTimer < Time.time) {
      sendStringToDexter("g;");
      sendTimer = Time.time + 0.016f; //+= 0.016f; //<- Use this instead if you want it to never skip a beat (potentially susceptible to death spirals)
    }

    //Dequeue the joint angles that were enqueued from the other (socket) thread
    //This is necessary since you cannot set joint angles directly from another thread
    Vector3Int[] jointRot;
    while (jointAngleBuffer.TryDequeue(out jointRot)) {
      rawJointAngles = jointRot;
    }

    //Decode the raw joint angles and apply them to the Visual Model
    for (int jointNumber = 0; jointNumber < 5; jointNumber++) {
      float totalRotation = (rawJointAngles[jointNumber][0] +
                            (rawJointAngles[jointNumber][1] / (jointNumber > 2 ? 8 : 1)) +
                             rawJointAngles[jointNumber][2]) / -3600f;
      bool baseOrYaw = (jointNumber == 4) || (jointNumber == 0);
      joints[jointNumber].localRotation = Quaternion.Euler(baseOrYaw ? 0f : totalRotation, 
                                                           baseOrYaw ? -totalRotation : 0f, 
                                                           0f);
    }

    //Also write this status text to a screen
    string toDisplay;
    while(diagnosticStringBuffer.TryDequeue(out toDisplay)) {
      if (diagnosticText != null) { diagnosticText.text = toDisplay; }
    }
  }

  public void sendStringToDexter(string command) {
    if (!hasStarted) { Start(); }
    try {
      if (dexterSocket != null && dexterSocket.Connected) {
        byte[] toSend = new byte[128];
        //The first four values don't do anything yet
        byte[] asciiBytes = Encoding.ASCII.GetBytes("xxx xxx xxx xxx " + command);
        Array.Copy(asciiBytes, toSend, asciiBytes.Length);
        SocketAsyncEventArgs sendArgs = new SocketAsyncEventArgs();
        sendArgs.SetBuffer(toSend, 0, toSend.Length);
        sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(completeSendingCommand);
        dexterSocket.Client.SendAsync(sendArgs);
      }
    } catch (NullReferenceException e) {
      Debug.LogWarning("Dexter was disconnected!\n" + e.StackTrace, this);//Not connected!
    }
  }

  void completeSendingCommand(object sender = null, SocketAsyncEventArgs e = null) {
    byte[] receiveArray = new byte[240];
    SocketAsyncEventArgs receiveArgs = new SocketAsyncEventArgs();
    receiveArgs.SetBuffer(receiveArray, 0, receiveArray.Length);
    receiveArgs.Completed += new EventHandler<SocketAsyncEventArgs>(receiveUpdatedState);
    dexterSocket.Client.ReceiveAsync(receiveArgs);
  }

  void receiveUpdatedState(object sender, SocketAsyncEventArgs e) {
    int[] state = new int[60];
    Buffer.BlockCopy(e.Buffer, 0, state, 0, 240);
    StringBuilder diagnosticStateString = new StringBuilder(7);
    Vector3Int[] jointAngles = new Vector3Int[5];

    //Enumerates through the state on each joint individually
    //Unclear what each of these mean exactly, but good to have
    //(Some may be incorrect unit conversions, be careful!)
    int jointNumber = 0;
    for (int i = 10; i < 60; i += 10) {
      jointAngles[jointNumber] = new Vector3Int(state[i], state[i + 1], state[i + 3]);

      diagnosticStateString.Append("Pos: " + state[i] + ", ");
      diagnosticStateString.Append("Delta: " + state[i + 1] + ", ");
      diagnosticStateString.Append("PID Delta: " + (state[i + 2] / (jointNumber > 2 ? 8 : 1)) + ", ");
      diagnosticStateString.Append("Force Delta: " + state[i + 3] + ", ");
      diagnosticStateString.Append("Sin: " + state[i + 4] + ", ");
      diagnosticStateString.Append("Cos: " + state[i + 5] + ", ");
      //stateString += ("Sent Position: " + state[i + 6]*4 + ", ");

      diagnosticStateString.AppendLine();
      jointNumber++;
    }
    jointAngleBuffer.Enqueue(jointAngles);
    diagnosticStringBuffer.Enqueue(diagnosticStateString.ToString());
  }

  void OnDestroy () {
    try {
      dexterSocket.Close();
    } catch (Exception e) {
      Debug.Log(e.ToString(), this);
    }
  }
}
