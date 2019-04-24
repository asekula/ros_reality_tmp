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
        }

        private void Update() {
            if (isMessageReceived)
                ProcessMessage();
        }
        private void ReceiveMessage(object sender, MessageEventArgs e) {
            pointCloudData = ((SensorPointCloud2)e.Message);
            isMessageReceived = true;
        }

        private void ProcessMessage() {
            pointCloud = new PointCloud(pointCloudData);
            pointCloudRenderer.renderCloudAndMovo(pointCloud, pointCloudData.header.stamp.secs,
                                                  pointCloudData.header.stamp.nsecs);
            isMessageReceived = false;

            Debug.Log("\t\tvelo secs: " + pointCloudData.header.stamp.secs + ", " + pointCloudData.header.stamp.nsecs);
        }
    }
}
