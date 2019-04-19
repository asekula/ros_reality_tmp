﻿using System;
using System.Collections;
using System.Collections.Concurrent;
using UnityEngine;

public class MovoPosition {
    public int secs, nsecs;
    public Vector2 position;
    public Quaternion rotation;

    public MovoPosition(int secs, int nsecs, Vector2 position, Quaternion rotation) {
        this.secs = secs;
        this.nsecs = nsecs;
        this.position = position;
        this.rotation = rotation;
    }
}

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class RenderPointCloud : MonoBehaviour {

    public ConcurrentQueue<MovoPosition> movoPositions;
    private GameObject movo;

    private Mesh mesh;
    int maxPoints = 60000;
    float epsilon = 0.01f;

    Vector3[] points;
    int[] indices;
    Color[] colors;

    Gradient g;
    GradientColorKey[] gck;
    GradientAlphaKey[] gak;

    Color[] colorBuckets;

    // Use this for initialization
    void Start() {
        movoPositions = new ConcurrentQueue<MovoPosition>();
        movo = GameObject.Find("movo");

        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        points = new Vector3[maxPoints];
        indices = new int[maxPoints];
        colors = new Color[maxPoints];

        g = new Gradient();
        gck = new GradientColorKey[5];
        gak = new GradientAlphaKey[2];
        gck[0].color = Color.red;
        gck[0].time = 0.0F;
        gck[1].color = Color.yellow;
        gck[1].time = 0.15F;
        gck[2].color = Color.green;
        gck[2].time = 0.3F;
        gck[3].color = Color.blue;
        gck[3].time = 0.7F;
        gck[4].color = new Color(0.0f, 0.0f, 0.6f);
        gck[4].time = 1.0F;
        gak[0].alpha = 1.0F;
        gak[0].time = 0.0F;
        gak[1].alpha = 1.0F;
        gak[1].time = 1.0F;
        g.SetKeys(gck, gak);
    }

    public void renderCloudAndMovo(PointCloud cloud, int secs, int nsecs) {
        int numPoints = Mathf.Min(60000, cloud.Points.Length);

        // Step 1: Get the latest possible movo position
        MovoPosition currentPosition = null;

        // Step 1a: Find the latest movoPosition from the Queue that is before the input secs/nsecs.
        if (!movoPositions.IsEmpty) {
            MovoPosition next;
            while (movoPositions.TryPeek(out next)) {
                if (next.secs < secs || (next.secs == secs && next.nsecs <= nsecs)) { // next is before the input time
                    movoPositions.TryDequeue(out currentPosition);
                } else {
                    break;
                }
            }
        }

        if (currentPosition != null) { // We found a time for the next movo position.
            // Step 1b: Get the translation offset between the realLifeInitialMovoPosition and the latest movoPosition.
            movoTranslation = new Vector3(curr.x, 0, curr.y); // Note the 0 in the middle.

            // Step 1c: Get the angle offset between them
            movoRotation = curr.rotation;
        } else {
          // TODO: Figure this case out -- it's not trivial.
          Debug.Log("No odometry data.");
          return;
        }

        // Step 2: Rotate and translate the point cloud.
        // TODO: try removing this -- we wouldn't need calibration (in theory)
        Quaternion calibrationRotation = Quaternion.Euler(-95, 0, 95);
        Vector3 calibrationTranslation = new Vector3(0.4f, 0.49f, 0.35f);
        Vector3 fixedMovoPosition = new Vector3(currentPosition.position.x, 0, currentPosition.position.y);

        for (int i = 0; i < numPoints; ++i) { // TODO: make this more efficient -- maybe do this via matrix multiplication.
            points[i] = new Vector3(-cloud.Points[i].x, cloud.Points[i].y, cloud.Points[i].z);

            // Calibration -- after these two lines the point cloud is correct relative to the Movo
            points[i] = calibrationRotation * points[i];
            points[i] = points[i] + calibrationTranslation;

            // Translating and rotating according to the Movo's new position
            points[i] = points[i] + fixedMovoPosition;
            points[i] = currentPosition.rotation * points[i];

            indices[i] = i;
            colors[i] = g.Evaluate(cloud.Points[i].rgb[2] / 255.0f);
        }

        mesh.vertices = points;
        mesh.colors = colors;
        mesh.SetIndices(indices, MeshTopology.Points, 0);

        // Step 4: Move the movo to its new position.
        movo.transform.position = new Vector3(currentPosition.position.x, 0, currentPosition.position.y);
        movo.transform.rotation = currentPosition.rotation;
        Debug.Log("Movo rotation: " + movo.transform.rotation);
    }
}
