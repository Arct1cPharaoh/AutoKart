// using UnityEngine;
// using System.Collections.Generic;
// using System.Linq;
//
// public struct DetectedCone
// {
//     public Rect boundingBox;
//     public Color color;
// }
//
// public class ConeDetector : MonoBehaviour
// {
//     [Header("Capture Settings")]
//     [SerializeField] private CameraSensor cameraSensor;
//     [SerializeField] private float frameRate = 30f;
//     [SerializeField] private string imageSaveFolder = "CapturedFrames";
//     [SerializeField] private bool captureOnStart = false;
//     [SerializeField] private bool saveFrames = false;
//
//     [Header("Filter Settings")]
//     [SerializeField] private int contourMergePad = 20;
//     [SerializeField] private int minPoints = 10;
//     [SerializeField] private int minSize = 5;
//
//     private float frameTimer = 0f;
//     private int frameCounter = 0;
//
//     private ContourDetector contourDetector;
//     private ConeFilter coneFilter;
//     private Vector2 cropOffset;
//
//     void Awake()
//     {
//         contourDetector = new ContourDetector();
//         coneFilter = new ConeFilter(minPoints, minSize, contourMergePad);
//     }
//
//     void Start()
//     {
//         if (captureOnStart)
//         {
//             Texture2D frame = cameraSensor.CaptureFrame();
//             DetectCones(frame);
//         }
//     }
//
//     List<DetectedCone> DetectCones(Texture2D img)
//     {
//         // code...
//         return detected;
//     }
//
//     public List<DetectedCone> TryDetectFrame(float deltaTime)
//     {
//         frameTimer += deltaTime;
//
//         if (captureOnStart || frameTimer < 1f / frameRate)
//             return null;
//
//         frameTimer = 0f;
//
//         Texture2D frame = cameraSensor.CaptureFrame();
//         List<DetectedCone> cones = DetectCones(frame);
//
//         if (saveFrames)
//             AnnotateImage(frame, cones);
//
//         return cones;
//     }
// }
