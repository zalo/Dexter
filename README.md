Dexter
=====================
A quick GUI and example code for visualizing and controlling the Dexter robot arm from [Haddington Dynamics](http://hdrobotic.com/) with Unity.

At the moment, it includes support for visualizing the arm's current pose and a simplified interface for for sending arbitrary commandlets (move to pose, change mode, change PID strength, etc.)


The included example scene demonstrates using an IK solver to reposition the arm by dragging the end effector around. (Use the mouse to drag each side of the ghost-effector and press ‘Space’ to send a motion command to the arm.)

NOTE: You will need to ensure that the IP address on the “Dexter” object matches the IP of the Dexter on your network in order for it to connect (if it can't connect, the application will freeze for 20 seconds upon bootup while it times out).


## [Gif of Effector-Mounted Leap Tracking (not included)](https://i.imgur.com/NDUfEzk.gifv)
## [Gif of Effector Motion Curves](https://i.imgur.com/E9hRJSi.gifv)