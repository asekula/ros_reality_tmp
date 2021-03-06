﻿/*
© Siemens AG, 2017-2018
Author: Dr. Martin Bischoff (martin.bischoff@siemens.com)

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at
<http://www.apache.org/licenses/LICENSE-2.0>.
Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using System;
using UnityEngine;

namespace RosSharp.RosBridgeClient {
    [RequireComponent(typeof(MeshRenderer))]
    public class KinectDepthImageReceiver : MessageReceiver {
        public override Type MessageType { get { return (typeof(SensorImage)); } }

        public Material Material;

        private byte[] depthData;
        private bool isMessageReceived;

        private MeshRenderer meshRenderer;
        private Texture2D depthTexture;

        private int width = 512;
        private int height = 424;

        public float scale = 1.0f;
        Matrix4x4 m;

        private void Awake() {
            MessageReception += ReceiveMessage;
        }
        private void Start() {
            depthTexture = new Texture2D(width, height, TextureFormat.R16, false);

        }
        private void Update() {
            if (isMessageReceived)
                ProcessMessage();
            //gameObject.transform.localScale = new Vector3(16f * scale, scale, 9f * scale);
        }
        private void ReceiveMessage(object sender, MessageEventArgs e) {
            depthData = ((SensorImage)e.Message).data;
            isMessageReceived = true;
        }

        private void ProcessMessage() {
            depthTexture.LoadRawTextureData(depthData);
            //Debug.Log(depthData[1341]);
            isMessageReceived = false;
        }

        void OnRenderObject() {

            Material.SetTexture("_MainTex", depthTexture);
            Material.SetPass(0);

            m = Matrix4x4.TRS(this.transform.position, this.transform.rotation, this.transform.localScale);
            Material.SetMatrix("transformationMatrix", m);

            Graphics.DrawProcedural(MeshTopology.Points, 512 * 424, 1);
        }
    }
}

