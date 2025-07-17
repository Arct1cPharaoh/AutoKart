// using UnityEngine;
// using System.Collections.Generic;
//
// public class ConeFilter
// {
//     private readonly int minPoints;
//     private readonly int minSize;
//     private readonly int mergePadding;
//
//     public ConeFilter(int minPoints = 10, int minSize = 5, int mergePadding = 20)
//     {
//         this.minPoints = minPoints;
//         this.minSize = minSize;
//         this.mergePadding = mergePadding;
//     }
//
//     // Not used
//     private List<Contour> RemoveNestedContours(List<Contour> contours)
//     {
//         int count = contours.Count;
//         bool[] keep = new bool[count];
//         System.Array.Fill(keep, true);
//
//         RectInt[] boxes = new RectInt[count];
//         for (int i = 0; i < count; i++)
//             boxes[i] = contours[i].GetBoundingBox();
//
//         for (int i = 0; i < count; i++)
//         {
//             if (!keep[i]) continue;
//
//             for (int j = 0; j < count; j++)
//             {
//                 if (i == j || !keep[j]) continue;
//                 if (ContourDetector.IsInside(boxes[i], boxes[j]))
//                 {
//                     keep[i] = false;
//                     break;
//                 }
//             }
//         }
//
//         List<Contour> filtered = new List<Contour>();
//         for (int i = 0; i < count; i++)
//             if (keep[i])
//                 filtered.Add(contours[i]);
//
//         return filtered;
//     }
//
//     private List<Contour> MergeContours(List<Contour> contours, int pad)
//     {
//         List<Contour> mergedContours = new List<Contour>();
//         bool[] used = new bool[contours.Count];
//
//         for (int i = 0; i < contours.Count; i++)
//         {
//             if (used[i]) continue;
//
//             Contour baseContour = contours[i];
//             List<Vector2Int> groupPoints = new List<Vector2Int>(baseContour.points);
//             RectInt groupBox = baseContour.GetBoundingBox();
//
//             used[i] = true;
//
//             for (int j = i + 1; j < contours.Count; j++)
//             {
//                 if (used[j]) continue;
//
//                 RectInt otherBox = contours[j].GetBoundingBox();
//                 if (!ContourDetector.Overlaps(groupBox, otherBox, pad)) continue;
//
//                 Vector2 centerA = groupBox.center;
//                 Vector2 centerB = otherBox.center;
//                 float dx = Mathf.Abs(centerA.x - centerB.x);
//                 float avgWidth = (groupBox.width + otherBox.width) * 0.5f;
//
//                 if (dx <= avgWidth * 0.25f)
//                 {
//                     groupPoints.AddRange(contours[j].points);
//                     groupBox = ContourDetector.Expand(groupBox, otherBox);
//                     used[j] = true;
//                 }
//             }
//
//             mergedContours.Add(new Contour { points = groupPoints });
//         }
//
//         return mergedContours;
//     }
//
//     private bool IsConeAspectRatio(RectInt box, float expectedAspect,
//             float tolerance)
//     {
//         if (box.height == 0) return false; // Prevent divide by zero
//         float aspect = (float)box.width / box.height;
//         return Mathf.Abs(aspect - expectedAspect) <= tolerance;
//     }
//
//     public List<DetectedCone> FilterContours(List<Contour> rawContours,
//         Texture2D img, Vector2 cropOffset)
//     {
//         List<Contour> merged = MergeContours(rawContours, mergePadding);
//         // List<Contour> filtered = RemoveNestedContours(merged);
//
//         List<DetectedCone> cones = new List<DetectedCone>();
//
//         foreach (Contour contour in merged)
//         {
//             // if (contour.points.Count < minPoints)
//             //     continue;
//             //
//             RectInt box = contour.GetBoundingBox();
//             // if (box.width < minSize || box.height < minSize)
//             //     continue;
//             //
//             // float aspect = 0.9f;
//             // float tolerance = 0.3f;
//             // if (!IsConeAspectRatio(box, aspect, tolerance))
//             //     continue;
//
//             Color? detectedColor = ConeColorMask.SampleConeColor(img, box, cropOffset);
//             if (detectedColor == null)
//                 continue;
//
//             DetectedCone cone = new DetectedCone
//             {
//                 boundingBox = new Rect(box.x, box.y, box.width, box.height),
//                 color = detectedColor.Value
//             };
//
//             cones.Add(cone);
//         }
//
//         return cones;
//     }
// }
