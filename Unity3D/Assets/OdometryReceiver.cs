using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RosSharp.RosBridgeClient {

    public class OdometryReceiver : MessageReceiver {


        public override Type MessageType { get { return (typeof(GeometryTransform)); } }

        private GeometryTransform odometryData;
        private MovoPosition initialPosition;
        private bool isMessageReceived;

        public RenderPointCloud pointCloudRenderer;

        private void Awake() {
            MessageReception += ReceiveMessage;
        }
        private void Start() {
            initialPosition = null;
            //Debug.Log("Start tf listener");
        }

        private void Update() {
            if (isMessageReceived)
                ProcessMessage();
        }
        private void ReceiveMessage(object sender, MessageEventArgs e) {
            odometryData = ((GeometryTransform)e.Message);
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

            int secs = (int)odometryData.rotation.x;
            int nsecs = (int)odometryData.rotation.y;
            Quaternion rotation = Quaternion.Inverse(new Quaternion(0, 0, odometryData.rotation.z, odometryData.rotation.w));
            rotation = new Quaternion(0.701f, 0, 0, 0.701f) * rotation * Quaternion.Inverse(new Quaternion(0.701f, 0, 0, 0.701f));
            Vector2 translation = new Vector2(-odometryData.translation.y, -odometryData.translation.x); // translation.z is always zero.
            //Debug.Log("X: " + translation.x);
            //Debug.Log("Y: " + translation.y);
            //Debug.Log("z: " + rotation.z);
            Debug.Log("slam secs: " + secs + ", " + nsecs);


            // This is where the offset calculations come in. Only offsets get queued.
            if (initialPosition != null) {
                pointCloudRenderer.movoPositions.Enqueue(new MovoPosition(
                    secs, nsecs, 
                    translation - initialPosition.translation, 
                    rotation * Quaternion.Inverse(initialPosition.rotation)));
            } else {
                initialPosition = new MovoPosition(secs, nsecs, translation, rotation);
                pointCloudRenderer.movoPositions.Enqueue(new MovoPosition(secs, nsecs, new Vector2(0,0), new Quaternion(0,0,0,1)));
            }
            isMessageReceived = false;
        }
    }
}
