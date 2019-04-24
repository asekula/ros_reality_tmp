using UnityEngine;
using System.Collections;
using System;

/*
 * This script collects color and depth data from the Kinect2
 * and uses this to display a 3D Point Cloud where the robot is 
 * looking. This is done by updating the texture of a material 
 * that has a custom shader.
 * 
 * This code and the shader used here were written by David F. Whitney.
 */

public class TfReceiver : MonoBehaviour {

    public WebsocketClient wsc;
    string tfTopic;
    int framerate = 100;
    public string compression = "none"; //"png" is the other option, haven't tried it yet though
    string tfMessage;

    // Use this for initialization
    void Start() {
        tfTopic = "tf";
        wsc.Subscribe(tfTopic, "tf2_msgs/TfMessage", compression, framerate);
        InvokeRepeating("UpdateTf", 0.1f, 0.1f);
    }

    // Update is called once per frame
    void UpdateTf() {
        Debug.Log("UpdateTf");
        try {
            tfMessage = wsc.messages[tfTopic];
            Debug.Log("Read message in TfReceiver.");
        }
        catch (Exception e) {
            foreach (string s in wsc.messages.Keys) {
                Debug.Log(s);
            }
            Debug.Log(e.ToString());
        }
    }
}