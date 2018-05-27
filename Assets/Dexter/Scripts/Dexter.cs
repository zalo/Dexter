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
  float sendTimer = 0;
  ConcurrentQueue<Quaternion>[] jointBuffer = new ConcurrentQueue<Quaternion>[5];
  ConcurrentQueue<string> stringBuffer = new ConcurrentQueue<string>();
  Quaternion[] jointAngles = new Quaternion[5];

  public TextMesh text;

  void Start() {
    for(int i = 0; i<jointBuffer.Length; i++) {
      jointBuffer[i] = new ConcurrentQueue<Quaternion>();
    }

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
        //sendStringToDexter(dexterSocket, "S StartSpeed 10000;");
        //sendStringToDexter(dexterSocket, "S MaxSpeed 250000;");
        sendStringToDexter(dexterSocket, "a 0 0 0 0 0;");
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
    try {
      //Heartbeat the Dexter to query its current pose
      if (dexterSocket != null && dexterSocket.Connected && sendTimer < Time.time) {
        sendStringToDexter(dexterSocket, "g;");
        sendTimer = Time.time + 0.016f; //+= 0.016f; //<- Use this instead if you want it to never skip a beat (susceptible to death spirals)
      }
    } catch (NullReferenceException e) {
      Debug.LogWarning("Dexter was disconnected!\n"+e.StackTrace, this);//Not connected!
    }

    //Dequeue the joint angles that were enqueued from the other (socket) thread
    //This is necessary since you cannot set joint angles directly from another thread
    for (int i=0; i<5; i++) {
      Quaternion jointRot = Quaternion.identity;
      if (jointBuffer[i].TryDequeue(out jointRot)) {
        jointAngles[i] = Quaternion.Slerp(joints[i].localRotation, jointRot, 1f);
        joints[i].localRotation = jointAngles[i];
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
    //(Some may be incorrect unit conversions, be careful!)
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

      jointBuffer[jointNumber].Enqueue(jointRot);

      stateString += ("Pos: " + state[i] + ", ");
      stateString += ("Delta: " + state[i + 1] + ", ");
      stateString += ("PID Delta: " + state[i + 2] + ", ");
      stateString += ("Force Delta: " + state[i + 3] + ", ");
      stateString += ("Sin: " + state[i + 4] + ", ");
      stateString += ("Cos: " + state[i + 5] + ", ");
      //stateString += ("Sent Position: " + state[i + 6]*4 + ", ");
      stateString += "\n";

      jointNumber++;
    }
    //text.text = stateString;
    stringBuffer.Enqueue(stateString);
    //Debug.Log(stateString, this);
  }

  void OnDestroy () {
    try {
      dexterSocket.Close();
    } catch (Exception e) {
      Debug.Log(e.ToString(), this);
    }
  }
}
