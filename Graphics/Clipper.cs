﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup.Localizer;

namespace _3dGraphics.Graphics
{
    public static class Clipper
    {        
        private enum PlaneId { Near=0, Left=1, Far=2, Right=3, Top=4, Bottom=5 };
        private const int NumPlanes = 6;

        public static List<Triangle> ClipTriangleAndAppendNewVerticesAndTriangles(Triangle triangle, List<Vector4> vertices, List<bool> verticesMask, List<Vector2> texels)
        {
            
            
            List<Triangle> inputList = new List<Triangle>();
            inputList.Add(triangle);

            bool quickInsideCheck = verticesMask[triangle.V1Index] &&
                                    verticesMask[triangle.V2Index] &&
                                    verticesMask[triangle.V3Index];
            
            if (quickInsideCheck)   //if we're totally inside we return the original Triangle which is already in inputList but we need to check the vertices
            {
                /*  the vertices are already checked in the vertex mask
                verticesMask[triangle.V1Index] = true;
                verticesMask[triangle.V2Index] = true;
                verticesMask[triangle.V3Index] = true;
                */
            }                      
            else  //if it's not totally inside we run the halfvolume check loop
            {
                bool done = false;

                for (int i = 0; i < NumPlanes && !done; i++)
                {
                    List<Triangle> resultList = new List<Triangle>();

                    foreach (Triangle t in inputList)
                    {
                        resultList.AddRange(ClipTriangleToPlane(t, (PlaneId)i, vertices, verticesMask, texels));
                    }

                    inputList = resultList;

                    if (resultList.Count == 0)
                    {
                        done = true;
                    }
                }
            }           

            return inputList;
        }

        private static List<Triangle> ClipTriangleToPlane(Triangle triangle, PlaneId planeId, List<Vector4> vertices, List<bool> verticesMask, List<Vector2> texels)
        {
            List<Triangle> toReturn = new List<Triangle>();

            Vector4 P1 = vertices[triangle.V1Index];
            Vector4 P2 = vertices[triangle.V2Index];
            Vector4 P3 = vertices[triangle.V3Index];

            Vector2 T1 = texels[triangle.T1Index];
            Vector2 T2 = texels[triangle.T2Index];
            Vector2 T3 = texels[triangle.T3Index];

            //check whick point is inside
            bool P1Inside = IsInsidePlane(P1, planeId);
            bool P2Inside = IsInsidePlane(P2, planeId);
            bool P3Inside = IsInsidePlane(P3, planeId);

            //count how many points are inside
            int numPointsInside = 0;
            if (P1Inside)
                numPointsInside++;
            if (P2Inside)
                numPointsInside++;
            if (P3Inside)
                numPointsInside++;

            if (numPointsInside == 3)
            {
                //triangle already inside, we add it to the list and mark the vertices
                toReturn.Add(triangle);

                /*  the vertices are already checked in the vertex mask
                verticesMask[triangle.V1Index] = true;
                verticesMask[triangle.V2Index] = true;
                verticesMask[triangle.V3Index] = true;
                */
            }
            else if(numPointsInside == 2)
            {
                if (!P1Inside)  
                {
                    //we calculate the 2 new points
                    Vector4 newP = ClipLineToPlane(P1, P2, planeId);
                    Vector4 newQ = ClipLineToPlane(P1, P3, planeId);
                    //we calculate the new texels
                    float pRatio = GetRatio(P2, P1, newP);
                    float qRatio = GetRatio(P3, P1, newQ);
                    Vector2 pTexel = new Vector2(T2.X + pRatio * (T1.X - T2.X), T2.Y + pRatio * (T1.Y - T2.Y));
                    Vector2 qTexel = new Vector2(T3.X + qRatio * (T1.X - T3.X), T3.Y + qRatio * (T1.Y - T3.Y));

                    //we add a new Triangle with P substituting P1
                    toReturn.Add(new Triangle(vertices.Count, triangle.V2Index, triangle.V3Index, texels.Count, triangle.T2Index, triangle.T3Index, triangle.LightIntensity));
                    //we add another new Triangle with Q substituting P1 and P substituting P2 keeping the clockwise order
                    toReturn.Add(new Triangle(vertices.Count+1, vertices.Count, triangle.V3Index, texels.Count+1, texels.Count, triangle.T3Index, triangle.LightIntensity));
                    
                    vertices.Add(newP);
                    vertices.Add(newQ);
                    verticesMask.Add(true);
                    verticesMask.Add(true);
                    texels.Add(pTexel);
                    texels.Add(qTexel);
                }
                else if (!P2Inside)
                {                                     
                    Vector4 newP = ClipLineToPlane(P2, P3, planeId);
                    Vector4 newQ = ClipLineToPlane(P1, P2, planeId);
                    //we calculate the new texels
                    float pRatio = GetRatio(P2, P3, newP);
                    float qRatio = GetRatio(P2, P1, newQ);
                    Vector2 pTexel = new Vector2(T3.X + pRatio * (T2.X - T3.X), T3.Y + pRatio * (T2.Y - T3.Y));
                    Vector2 qTexel = new Vector2(T1.X + qRatio * (T2.X - T1.X), T1.Y + qRatio * (T2.Y - T1.Y));


                    toReturn.Add(new Triangle(vertices.Count, triangle.V3Index, triangle.V1Index, texels.Count, triangle.T3Index, triangle.T1Index, triangle.LightIntensity));                    
                    toReturn.Add(new Triangle(vertices.Count + 1, vertices.Count, triangle.V1Index, texels.Count + 1, texels.Count, triangle.T1Index, triangle.LightIntensity));

                    vertices.Add(newP);
                    vertices.Add(newQ);
                    verticesMask.Add(true);
                    verticesMask.Add(true);
                    texels.Add(pTexel);
                    texels.Add(qTexel);
                }
                else  //P1 AND P2 inside, P3 outside
                {
                    Vector4 newP = ClipLineToPlane(P1, P3, planeId);
                    Vector4 newQ = ClipLineToPlane(P2, P3, planeId);
                    //we calculate the new texels
                    float pRatio = GetRatio(P3, P1, newP);
                    float qRatio = GetRatio(P3, P2, newQ);
                    Vector2 pTexel = new Vector2(T1.X + pRatio * (T3.X - T1.X), T1.Y + pRatio * (T3.Y - T1.Y));
                    Vector2 qTexel = new Vector2(T2.X + qRatio * (T3.X - T2.X), T2.Y + qRatio * (T3.Y - T2.Y));

                    toReturn.Add(new Triangle(vertices.Count, triangle.V1Index, triangle.V2Index, texels.Count, triangle.T1Index, triangle.T2Index, triangle.LightIntensity));
                    toReturn.Add(new Triangle(vertices.Count + 1, vertices.Count, triangle.V2Index, texels.Count + 1, texels.Count, triangle.T2Index, triangle.LightIntensity));

                    vertices.Add(newP);
                    vertices.Add(newQ);
                    verticesMask.Add(true);
                    verticesMask.Add(true);
                    texels.Add(pTexel);
                    texels.Add(qTexel);
                }
            }
            else if(numPointsInside == 1)
            {
                if (P1Inside)
                {
                    //calculate the new points keeping the clockwise ordering
                    Vector4 newP2 = ClipLineToPlane(P1, P2, planeId);
                    Vector4 newP3 = ClipLineToPlane(P1, P3, planeId);
                    //we calculate the new texel
                    float pRatio = GetRatio(P1, P2, newP2);
                    float qRatio = GetRatio(P1, P3, newP3);
                    Vector2 pTexel = new Vector2(T1.X + pRatio * (T2.X - T1.X), T1.Y + pRatio * (T2.Y - T1.Y));
                    Vector2 qTexel = new Vector2(T1.X + qRatio * (T3.X - T1.X), T1.Y + qRatio * (T3.Y - T1.Y));

                    //add the "new" triangle to the ones to return and process against the other planes but NOT to the output triangle list
                    //the V2 and V3 indexes are for the vertices we've created and going to add. This vertices will be valid even if we split the triangles even further
                    toReturn.Add(new Triangle(triangle.V1Index, vertices.Count, vertices.Count + 1, triangle.T1Index, texels.Count, texels.Count + 1, triangle.LightIntensity));
                    //we add the new vertices
                    vertices.Add(newP2);
                    vertices.Add(newP3);
                    //and we mark them as valid to process further
                    verticesMask.Add(true);
                    verticesMask.Add(true);
                    //and we add the texels
                    texels.Add(pTexel);
                    texels.Add(qTexel);
                }
                else if(P2Inside)
                {
                    Vector4 newP1 = ClipLineToPlane(P1, P2, planeId);
                    Vector4 newP3 = ClipLineToPlane(P2, P3, planeId);
                    //
                    float pRatio = GetRatio(P2, P1, newP1);
                    float qRatio = GetRatio(P2, P3, newP3);
                    Vector2 pTexel = new Vector2(T2.X + pRatio * (T1.X - T2.X), T2.Y + pRatio * (T1.Y - T2.Y));
                    Vector2 qTexel = new Vector2(T2.X + qRatio * (T3.X - T2.X), T2.Y + qRatio * (T3.Y - T2.Y));

                    toReturn.Add(new Triangle(vertices.Count, triangle.V2Index, vertices.Count + 1, texels.Count, triangle.T2Index, texels.Count + 1, triangle.LightIntensity));                  
                    
                    vertices.Add(newP1);
                    vertices.Add(newP3);                    
                    verticesMask.Add(true);
                    verticesMask.Add(true);
                    texels.Add(pTexel);
                    texels.Add(qTexel);
                }
                else    //P3 inside
                {
                    Vector4 newP1 = ClipLineToPlane(P1, P3, planeId);
                    Vector4 newP2 = ClipLineToPlane(P2, P3, planeId);
                    //
                    float pRatio = GetRatio(P3, P1, newP1);
                    float qRatio = GetRatio(P3, P2, newP2);
                    Vector2 pTexel = new Vector2(T3.X + pRatio * (T1.X - T3.X), T3.Y + pRatio * (T1.Y - T3.Y));
                    Vector2 qTexel = new Vector2(T3.X + qRatio * (T2.X - T3.X), T3.Y + qRatio * (T2.Y - T3.Y));

                    toReturn.Add(new Triangle(vertices.Count, vertices.Count + 1, triangle.V3Index, texels.Count, texels.Count + 1, triangle.T3Index, triangle.LightIntensity));
                    vertices.Add(newP1);
                    vertices.Add(newP2);
                    verticesMask.Add(true);
                    verticesMask.Add(true);
                    texels.Add(pTexel);
                    texels.Add(qTexel);
                }
            }
            else    //no points inside
            {
                //we keep the return list empty
            }


            return toReturn;
        }

        private static bool IsInsidePlane(Vector4 point, PlaneId planeId)
        {
            switch (planeId)
            {
                case PlaneId.Near:
                    return point.Z >= 0;
                    break;
                case PlaneId.Far:
                    return point.Z <= point.W;
                    break;
                case PlaneId.Left:
                    return point.X >= -point.W;
                    break;
                case PlaneId.Right:
                    return point.X <= point.W;
                    break;
                case PlaneId.Top:
                    return point.Y <= point.W;
                    break;
                case PlaneId.Bottom:
                    return point.Y >= -point.W;
                    break;
                default:
                    return false;
            }
        }

        private static Vector4 ClipLineToPlane(Vector4 p1, Vector4 p2, PlaneId planeId)
        {
            //based on the following equation: p = alpha*[p1.X, p1.Y, p1.Z, p1.W] + (1-alpha)*[p2.X, p2.Y, p2.Z, p2.W]
            float alpha;

            switch (planeId)
            {
                case PlaneId.Near:
                    alpha = -p2.Z / (p1.Z - p2.Z);
                    break;
                case PlaneId.Far:
                    alpha = (-p2.Z + p2.W) / (p1.Z - p2.Z - p1.W + p2.W);
                    break;
                case PlaneId.Left:
                    alpha = (-p2.X - p2.W) / (p1.X - p2.X + p1.W - p2.W);
                    break;
                case PlaneId.Right:
                    alpha = (-p2.X + p2.W) / (p1.X - p2.X - p1.W + p2.W);
                    break;
                case PlaneId.Top:
                    alpha = (-p2.Y + p2.W) / (p1.Y - p2.Y - p1.W + p2.W);
                    break;
                case PlaneId.Bottom:
                    alpha = (-p2.Y - p2.W) / (p1.Y - p2.Y + p1.W - p2.W);
                    break;
                default:
                    return Vector4.Zero;
            }

            float oneMinusAlpha = 1 - alpha;

            return alpha * p1 + oneMinusAlpha * p2; 
        }
        
        private static float GetRatio(Vector4 pBase, Vector4 pOld, Vector4 pNew)
        {
            Vector4 newV = pNew - pBase;
            Vector4 oldV = pOld - pBase;

            float newNormSquared = Vector4.Dot(newV, newV);
            float oldNormSquared = Vector4.Dot(oldV, oldV);
            float toReturn = MathF.Sqrt(newNormSquared / oldNormSquared);

            return toReturn;
        }

        public static bool IsPointInsideViewVolume(Vector4 p)
        {
            return -p.W <= p.X && p.X <= p.W &&
                    -p.W <= p.Y && p.Y <= p.W &&
                    0f <= p.Z && p.Z <= p.W;
        }

        








    }
}
