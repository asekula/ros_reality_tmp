using System;
using System.Collections;
using System.Collections.Concurrent;
using UnityEngine;

public class MovoPosition {
    public int secs, nsecs;
    public Vector2 translation;
    public float angle;

    public MovoPosition(int secs, int nsecs, Vector2 translation, float angle) {
        this.secs = secs;
        this.nsecs = nsecs;
        this.translation = translation;
        this.angle = angle;
    }
}

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class RenderPointCloud : MonoBehaviour {

    public ConcurrentQueue<MovoPosition> movoPositions;
    private MovoPosition lastPosition;
    private int numMissingPositions;
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
        lastPosition = new MovoPosition(0, 0, new Vector2(0, 0), 0);
        numMissingPositions = 0;

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
        MovoPosition currentPosition = null, savedLastPosition = lastPosition;

        // Step 1a: Find the latest movoPosition from the Queue that is before the input secs/nsecs.
        if (!movoPositions.IsEmpty) {
            MovoPosition next;
            while (movoPositions.TryPeek(out next)) {
                //Debug.Log("current secs: " + secs + ", next secs: " + next.secs);
                if (next.secs < secs || (next.secs == secs && next.nsecs <= nsecs)) { // next is before the input time
                    bool success = movoPositions.TryDequeue(out currentPosition);
                    //Debug.Log("dequeued: " + success);
                    //Debug.Log("length of queue: " + movoPositions.Count);
                }
                else {
                    break;
                }
            }
        } else {
            //Debug.Log("Queue empty.");
        }

        if (currentPosition != null) { // We found a time for the next movo position.
            numMissingPositions = 0;
            lastPosition = currentPosition;
        }
        else {
            //Debug.Log("No SLAM data: " + numMissingPositions);
            currentPosition = lastPosition;
            numMissingPositions += 1;
            System.Threading.Thread.Sleep(10);
        }

        // Step 2: Rotate and translate the point cloud.
        // TODO: try removing this -- we wouldn't need calibration (in theory)
        Quaternion calibrationRotation = Quaternion.Euler(-95, 0, 95);
        Vector3 calibrationTranslation = new Vector3(0.4f, 0.49f, 0.35f);
        Vector3 fixedMovoPosition = new Vector3(currentPosition.translation.x, 0, currentPosition.translation.y);

        for (int i = 0; i < numPoints; ++i) { // TODO: make this more efficient -- maybe do this via matrix multiplication.
            points[i] = new Vector3(-cloud.Points[i].x, cloud.Points[i].y, cloud.Points[i].z);

            // Calibration -- after these two lines the point cloud is correct relative to the Movo
            points[i] = calibrationRotation * points[i];
            points[i] = points[i] + calibrationTranslation;

            // Translating and rotating according to the Movo's new position
            points[i] = points[i] + fixedMovoPosition;
            points[i] = Quaternion.Euler(new Vector3(0, currentPosition.angle, 0)) * points[i];

            indices[i] = i;
            colors[i] = g.Evaluate(cloud.Points[i].rgb[2] / 255.0f);
        }

        mesh.vertices = points;
        mesh.colors = colors;
        mesh.SetIndices(indices, MeshTopology.Points, 0);

        // Step 4: Move the movo to its new position.
        float angle = (360 + currentPosition.angle - savedLastPosition.angle) % 360;
        movo.transform.Rotate(Vector3.up, angle);
        movo.transform.position = fixedMovoPosition;
    }
}
