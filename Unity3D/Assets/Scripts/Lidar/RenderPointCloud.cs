using System;
using System.Collections;
using System.Collections.Concurrent;
using UnityEngine;

public class MovoPosition {
    public int secs, nsecs;
    public float x, y, angle;

    public MovoPosition(int secs, int nsecs, float x, float y, float angle) {
        this.secs = secs;
        this.nsecs = nsecs;
        this.x = x;
        this.y = y;
        this.angle = angle;
    }
}

public enum CalibrationStage {
    BEFORE_FIRST_POINT,
    BEFORE_SECOND_POINT,
    CALIBRATED
}

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class RenderPointCloud : MonoBehaviour {

    public ConcurrentQueue<MovoPosition> movoPositions;
    private Vector3 lastMovoTranslation, lastMovoRotation;

    private Vector3 smoothedAveragePoint;
    private int numAverages;
    private CalibrationStage calibrationStage;

    private GameObject movo;
    private GameObject averagePoint, calibrationBalloon;
    private GameObject calibrationPoint1, calibrationPoint2;

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
        lastMovoRotation = new Vector3(0,0,0);
        lastMovoTranslation = new Vector3(0,0,0);
        movo = GameObject.Find("movo");

        averagePoint = GameObject.Find("AveragePoint");
        averagePoint.GetComponent<Renderer>().material.color = Color.black;
        averagePoint.SetActive(false);
        calibrationBalloon = GameObject.Find("CalibrationBalloon");
        calibrationBalloon.GetComponent<Renderer>().material.color = Color.red;
        calibrationPoint1 = GameObject.Find("CalibrationPoint1");
        calibrationPoint2 = GameObject.Find("CalibrationPoint2");
        calibrationPoint1.SetActive(false);
        calibrationPoint2.SetActive(false);

        smoothedAveragePoint = new Vector3(0, 0, 0);
        numAverages = 0;
        calibrationStage = CalibrationStage.BEFORE_FIRST_POINT;

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
        bool moveTheMovo = false;

        // Step 1: Get the latest possible movo position
        Vector3 movoTranslationOffset = new Vector3(0,0,0);
        float movoRotationOffsetAngle = 0;

        // Step 1a: Find the latest movoPosition from the Queue that is before the input secs/nsecs.
        if (!movoPositions.IsEmpty) {
            MovoPosition curr = null;
            MovoPosition next;
            while (movoPositions.TryPeek(out next)) {
                if (next.secs < secs || (next.secs == secs && next.nsecs <= nsecs)) { // next is before the input time
                    movoPositions.TryDequeue(out curr);
                }
                else {
                    break;
                }
            }

            if (curr != null) {
                // We found a time for the next movo position.
                moveTheMovo = true;

                // Step 1b: Get the translation offset between the realLifeInitialMovoPosition and the latest movoPosition.
                movoTranslationOffset = new Vector3(curr.x, 0, curr.y); // Note the 0 in the middle.

                // Step 1c: Get the angle offset between them
                movoRotationOffsetAngle = curr.angle;
            }
        }

      

        if (!moveTheMovo) {
            // TODO: Figure this case out -- it's not trivial.
            return;
        }

        // Step 2: Rotate and translate the point cloud.
        Quaternion pointCloudRotation = Quaternion.Euler(0, movoRotationOffsetAngle, 0);
        Quaternion calibrationRotation = Quaternion.Euler(-95, 0, 95);
        Vector3 calibrationTranslation = new Vector3(0.4f, 0.49f, 0.35f);
        Vector3 total = new Vector3(0, 0, 0);
    
        for (int i = 0; i < numPoints; ++i) { // TODO: make this more efficient -- maybe do this via matrix multiplication.
            points[i] = new Vector3(-cloud.Points[i].x, cloud.Points[i].y, cloud.Points[i].z);

            // Calibration -- after these two lines the point cloud is correct relative to the Movo
            points[i] = calibrationRotation * points[i];
            points[i] = points[i] + calibrationTranslation;

            total += points[i];
            // Translating and rotating according to the Movo's new position
            points[i] = points[i] + movoTranslationOffset;
            points[i] = pointCloudRotation * points[i];

            indices[i] = i;
            colors[i] = g.Evaluate(cloud.Points[i].rgb[2] / 255.0f);
        }

        mesh.vertices = points;
        mesh.colors = colors;
        mesh.SetIndices(indices, MeshTopology.Points, 0);

        // Sets the location of the calibration balloon (AveragePoint game object).
        //averagePoint.transform.position.Set(total.x / numPoints, total.y / numPoints, total.z / numPoints);
        if (false) {
            averagePoint.transform.position = total / numPoints;

            if (calibrationStage == CalibrationStage.BEFORE_FIRST_POINT) {
                Vector3 prevSmoothedAverage = smoothedAveragePoint;
                smoothedAveragePoint = (smoothedAveragePoint * numAverages + (total / numPoints)) / (numAverages + 1);
                numAverages += 1;
                calibrationBalloon.transform.position = smoothedAveragePoint;

                if (Vector3.Distance(prevSmoothedAverage, smoothedAveragePoint) < epsilon) {
                    calibrationPoint1.transform.position = smoothedAveragePoint;
                    calibrationPoint1.SetActive(true);
                    calibrationStage = CalibrationStage.BEFORE_SECOND_POINT;
                }
            }
            else {

            }
        }


        // Step 3: Un-rotate and un-translate the Movo.
        movo.transform.Rotate(-lastMovoRotation);
        movo.transform.Translate(-lastMovoTranslation);

        //// Step 4: Rotate and translate the Movo to its new position.
        lastMovoRotation = new Vector3(0, movoRotationOffsetAngle, 0);
        lastMovoTranslation = movoTranslationOffset;
        movo.transform.Translate(movoTranslationOffset);
        movo.transform.Rotate(lastMovoRotation);

        Debug.Log("Movo rotation: " + lastMovoRotation);
    }
}