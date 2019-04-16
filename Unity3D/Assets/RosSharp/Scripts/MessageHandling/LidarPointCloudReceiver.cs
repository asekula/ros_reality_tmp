using System;
using System.Collections;
using System.Collections.Concurrent;
using UnityEngine;

namespace RosSharp.RosBridgeClient {

//    [RequireComponent(typeof(MeshRenderer))]
    public class LidarPointCloudReceiver : MessageReceiver {
   
        public override Type MessageType { get { return (typeof(SensorPointCloud2)); } }

        private SensorPointCloud2 pointCloudData;
        private PointCloud pointCloud;
        private bool isMessageReceived;
        private bool receivedSinglePointCloud;

        public RenderPointCloud pointCloudRenderer;

        private void Awake() {
            MessageReception += ReceiveMessage;
        }
        private void Start() {
            Debug.Log("Start LidarPointCloudReceiver");
            receivedSinglePointCloud = false;
        }

        private void Update() {
            if (isMessageReceived)
                ProcessMessage();
            //Debug.Log("Update LidarPointCloudReceiver");
        }
        private void ReceiveMessage(object sender, MessageEventArgs e) {
            //Debug.Log("ReceiveMessage LidarPointCloudReceiver");
            pointCloudData = ((SensorPointCloud2)e.Message);
            //Debug.Log("Lidar timestamp: " + pointCloudData.header.stamp.secs + ", " + pointCloudData.header.stamp.nsecs);
            isMessageReceived = true;
        }

        private void ProcessMessage() {
            if (!receivedSinglePointCloud) {
                //Debug.Log("Received point cloud. Rendering.");
                pointCloud = new PointCloud(pointCloudData);
                pointCloudRenderer.renderCloudAndMovo(pointCloud, pointCloudData.header.stamp.secs, pointCloudData.header.stamp.nsecs);
                receivedSinglePointCloud = false;
            }
            isMessageReceived = false;
        }
    }
}

