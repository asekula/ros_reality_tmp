using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RosSharp.RosBridgeClient {

    public class OdometryReceiver : MessageReceiver {


        public override Type MessageType { get { return (typeof(GeometryTransformStamped)); } }

        private GeometryTransformStamped odometryData;
        private MovoPosition initialPosition;
        private bool isMessageReceived;

        public RenderPointCloud pointCloudRenderer;

        private void Awake() {
            MessageReception += ReceiveMessage;
        }
        private void Start() {
            initialPosition = null;
            Debug.Log("Start tf listener");
        }

        private void Update() {
            if (isMessageReceived)
                ProcessMessage();
        }
        private void ReceiveMessage(object sender, MessageEventArgs e) {
            odometryData = ((GeometryTransformStamped)e.Message);
            isMessageReceived = true;
        }

        // Important: This message zeroes out the translation/rotation data such that
        // the very first position it gets is zero, and every other position is relative
        // to the initial position.
        private void ProcessMessage() {
            //Debug.Log("tf rotation: " + odometryData.rotation);

            // Important: As an initial hack just to get things working, 
            // the x and y coordinates for the quaternion will be the seconds and 
            // nanoseconds of the transform. This will be replaced later with 
            // a GeometryTransformStamped message type.

            int secs = odometryData.header.stamp.secs;
            int nsecs = odometryData.header.stamp.nsecs;
            Quaternion rotation = new Quaternion(0, 0, odometryData.transform.rotation.z, odometryData.transform.rotation.w);
            Vector2 translation = new Vector2(odometryData.transform.translation.y, odometryData.transform.translation.x);

            // This is where the offset calculations come in. Only offsets get queued.
            if (initialPosition != null) {
                pointCloudRenderer.movoPositions.Enqueue(new MovoPosition(
                    secs, nsecs, 
                    translation - initialPosition.translation,
                    (360 + (360-rotation.eulerAngles.z) - initialPosition.angle) % 360));
            } else {
                initialPosition = new MovoPosition(secs, nsecs, translation, 360-rotation.eulerAngles.z);
                pointCloudRenderer.movoPositions.Enqueue(new MovoPosition(secs, nsecs, new Vector2(0,0), 0));
            }
            isMessageReceived = false;
        }
    }
}
