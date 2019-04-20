using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RosSharp.RosBridgeClient {

    public class OdometryReceiver : MessageReceiver {

        public override Type MessageType { get { return (typeof(NavigationOdometry)); } }

        private NavigationOdometry odometryData;
        private MovoPosition initialPosition;

        public RenderPointCloud pointCloudRenderer;

        private void Awake() {
            MessageReception += ReceiveMessage;
        }
        private void Start() {
            initialPosition = null;
            Debug.Log("Start OdometryReceiver");
        }

        // Important: This message zeroes out the translation/rotation data such that
        // the very first position it gets is zero, and every other position is relative
        // to the initial position.
        private void ReceiveMessage(object sender, MessageEventArgs e) {
            odometryData = ((NavigationOdometry)e.Message);
            GeometryQuaternion q = odometryData.pose.pose.orientation;
            Debug.Log("Odometry angle: " + q.z);

            // This is where the offset calculations come in. Only offsets get queued.
            if (initialPosition != null) {
                pointCloudRenderer.movoPositions.Enqueue(new MovoPosition(
                    odometryData.header.stamp.secs, odometryData.header.stamp.nsecs,
                    new Vector2(odometryData.pose.pose.position.y - initialPosition.position.x,
                    odometryData.pose.pose.position.x - initialPosition.position.y),
                    new Quaternion(q.x, q.y, q.z, q.w) * Quaternion.Inverse(initialPosition.rotation)));
            } else {
                initialPosition = new MovoPosition(
                    odometryData.header.stamp.secs, odometryData.header.stamp.nsecs,
                    new Vector2(odometryData.pose.pose.position.y, odometryData.pose.pose.position.x),
                    new Quaternion(q.x, q.y, q.z, q.w));
                pointCloudRenderer.movoPositions.Enqueue(new MovoPosition(odometryData.header.stamp.secs, odometryData.header.stamp.nsecs,
                                                                          new Vector2(0,0),
                                                                          new Quaternion(0,0,0,1)));
            }
        }
    }
}
