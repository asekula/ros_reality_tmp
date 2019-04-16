using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RosSharp.RosBridgeClient {

    //    [RequireComponent(typeof(MeshRenderer))]
    public class OdometryReceiver : MessageReceiver {

        public override Type MessageType { get { return (typeof(NavigationOdometry)); } }

        private NavigationOdometry odometryData;
        private MovoPosition init;

        public RenderPointCloud pointCloudRenderer;

        private void Awake() {
            MessageReception += ReceiveMessage;
        }
        private void Start() {
            init = null;
            Debug.Log("Start OdometryReceiver");
        }

        private void ReceiveMessage(object sender, MessageEventArgs e) {
            //Debug.Log("ReceiveMessage LidarPointCloudReceiver");
            odometryData = ((NavigationOdometry)e.Message);
            //Debug.Log("Odometry timestamp: " + odometryData.header.stamp.secs + ", " + odometryData.header.stamp.nsecs);
            Debug.Log("Odometry x: " + odometryData.pose.pose.position.x + ", y: " + odometryData.pose.pose.position.y);

            // This is where the offset calculations come in. Only offsets get queued.
            if (init != null) {
                pointCloudRenderer.movoPositions.Enqueue(new MovoPosition(
                    odometryData.header.stamp.secs, odometryData.header.stamp.nsecs,
                    odometryData.pose.pose.position.x - init.x, odometryData.pose.pose.position.y - init.y,
                    odometryData.pose.pose.orientation.z - init.angle));
            } else {
                init = new MovoPosition(
                    odometryData.header.stamp.secs, odometryData.header.stamp.nsecs,
                    odometryData.pose.pose.position.x, odometryData.pose.pose.position.y,
                    odometryData.pose.pose.orientation.z);
                pointCloudRenderer.movoPositions.Enqueue(new MovoPosition(odometryData.header.stamp.secs, odometryData.header.stamp.nsecs, 0,0,0));
            }
        }
    }
}

