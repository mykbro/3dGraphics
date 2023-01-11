﻿using _3dGraphics.Graphics;
using _3dGraphics.Tests;
using _3dGraphics.Windows;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Threading.Tasks;
using System.Threading;
using System.Text;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Reflection;
using System.IO;


namespace _3dGraphics
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private World _world;
        private MainWindow _mainWindow;
        private ConsoleWindow _console;
        private Vector3 _cameraForwardMovement;     // 0 or 1
        private Vector3 _cameraBackwardMovement;    // 0 or 1
        private Vector3 _cameraPositiveRotation;
        private Vector3 _cameraNegativeRotation;
        private float _fovIncrease; //0 or 1
        private float _fovDecrease; //0 or 1

        private void Application_Startup(object sender, StartupEventArgs e)
        {

            _mainWindow = new MainWindow(StartMovingCameraLeft, StopMovingCameraLeft, StartMovingCameraRight, StopMovingCameraRight,
                                        StartMovingCameraForward, StopMovingCameraForward, StartMovingCameraBackward, StopMovingCameraBackward,
                                        StartMovingCameraUp, StopMovingCameraUp, StartMovingCameraDown, StopMovingCameraDown,
                                        StartPitchingCameraUp, StopPitchingCameraUp, StartPitchingCameraDown, StopPitchingCameraDown,
                                        StartYawingCameraRight, StopYawingCameraRight, StartYawingCameraLeft, StopYawingCameraLeft,
                                        StartRollingCameraLeft, StopRollingCameraLeft, StartRollingCameraRight, StopRollingCameraRight,
                                        StartIncreasingFov, StopIncreasingFov, StartDecreasingFov, StopDecreasingFov);
            _console = new ConsoleWindow();
            
            CreateWorld(_mainWindow.ScreenWidth, _mainWindow.ScreenWidth);

            _mainWindow.Show();
            _console.Show();
            
            StartEngineLoopAsync();
        }

        private void CreateWorld(int screenWidth, int screenHeight)
        {
            Vector3[] positions = {
                new Vector3(0f, 0f, 0f),
                new Vector3(1f, 0f, 0f),
                new Vector3(0f, 1f, 0f),
                new Vector3(1f, 1f, 0f),
                new Vector3(0f, 0f, 1f),
                new Vector3(1f, 0f, 1f),
                new Vector3(0f, 1f, 1f),
                new Vector3(1f, 1f, 1f),
            };

            Triangle[] triangles = {
                new Triangle(0, 2 ,3),
                new Triangle(3, 1, 0),
                new Triangle(2, 6, 7),
                new Triangle(7, 3, 2),
                new Triangle(1, 3, 7),
                new Triangle(7, 5, 1),
                new Triangle(4, 0, 1),
                new Triangle(1, 5, 4),
                new Triangle(5, 7, 6),
                new Triangle(6, 4, 5),
                new Triangle(4, 6, 2),
                new Triangle(2, 0, 4),
            };
            
            //we create Vertex[] from Vector3[]
            var vertices = positions.Select<Vector3, Vertex>((pos) => new Vertex(pos));
            
            Mesh cube = new Mesh(vertices, triangles);
            
            float FOV = 90f;
            float zNear = 0.1f;
            float zFar = 100f;
            float speedKmh = 6f;
            float rotSpeedDegSec = 60f;
            float fovIncSpeedDegSec = 10f;

            //we create the world and populate it with objects
            _world = new World(screenWidth, screenHeight, FOV, zNear, zFar, speedKmh, rotSpeedDegSec, fovIncSpeedDegSec);

            /*
            for(int i=0; i<8; i++)
            {
                _world.Objects.Add(new WorldObject(cube, Vector3.Zero, 1f));
            }
               
            //we move the objects  
            _world.Objects[1].Position = new Vector3(0f, 0f, 5f);
            _world.Objects[2].Position = new Vector3(5f, 0f, 5f);
            _world.Objects[3].Position = new Vector3(5f, 0f, 0f);
            _world.Objects[4].Position = new Vector3(0f, 5f, 0f);
            _world.Objects[5].Position = new Vector3(0f, 5f, 5f);
            _world.Objects[6].Position = new Vector3(5f, 5f, 5f);
            _world.Objects[7].Position = new Vector3(5f, 5f, 0f);
            */

            /*
            //big distances test
            
            float d = 10e6f;
            Vector3 movement = Vector3.One * d;
            _world.Camera.MoveBy(movement);
            for (int i = 0; i < 8; i++)
            {
                _world.Objects[i].MoveBy(movement);
            }
            */



            //TEAPOT

            //Mesh teapot = LoadMeshFromObjFile(@"D:\teapot.txt");
            //Mesh suzanne = LoadMeshFromObjFile(@"D:\suzanne.txt");
            Mesh bunny = LoadMeshFromObjFile(@"D:\bunny.txt");
            _world.Objects.Add(new WorldObject(bunny, Vector3.Zero, 50f));
            
        }

        private void Render()
        {           

            Matrix4x4 worldToCamera = _world.Camera.WorldToCameraMatrix;
            Matrix4x4 projMatrix = _world.Camera.ProjectionMatrix;
            Matrix4x4 viewportMatrix = _world.Camera.ViewPortTransformMatrix;            

            Matrix4x4 worldToProj = worldToCamera * projMatrix;
            List<Fragment> fragments = new List<Fragment>();

            int debugNumTrianglesFromObjects = 0;
            int debugNumTrianglesSentToClip = 0;         
            int debugNumTrianglesSentToRender = 0;           
            

            /*
            //prepare the list of ALL vertices
            List<Vector4> vertices4D = new List<Vector4>(_world.TotalVertexCount);
            for (int i = 0; i < _world.Meshes.Count; i++)
            {
                Mesh m = _world.Meshes[i];
                for(int j=0; j < m.VertexCount; j++)
                {
                    vertices4D.Add(m.GetVertex(j).Position4D);
                }
            }
            */

            //for each WorldObject
            for (int i = 0; i < _world.Objects.Count; i++)
            {
                WorldObject wObject = _world.Objects[i];        //store to avoid repetitive calls
                Mesh mesh= wObject.Mesh;                        //store to avoid repetitive calls
                int numVertices = mesh.VertexCount;             //store to avoid repetitive calls
                int numTriangles = mesh.TriangleCount;          //store to avoid repetitive calls

                //we create an empty List of Vector4 sized to the total nr of Vertices...
                //we'll fill it all but we'll avoid relocations and we're going to create new Vertices during the Clipping stage so an Array would not work
                //we'll also create a companion list of bools that will keep track of the Vertices we need to transform proceeding in the pipeline (initialized to false)
                List<Vector4> vertices4D = new List<Vector4>(numVertices);
                List<bool> verticesMask = new List<bool>(numVertices);
                //we also create a list for the Triangles that we want to clip sizing it to TriangleCount... we'll probably fill half of it
                List<Triangle> trianglesToClip = new List<Triangle>(numTriangles);
               

                //we populate the lists
                for (int vIndex = 0; vIndex < numVertices; vIndex++)
                {
                    vertices4D.Add(mesh.GetVertex(vIndex).Position4D);
                    verticesMask.Add(false);
                }

                //we transform the camera from World to Object space for backface culling using normals
                Matrix4x4 worldToLocalMatrix = wObject.WorldToLocalMatrix;
                Vector3 cameraPosInObjSpace = Vector3.Transform(_world.Camera.Position, worldToLocalMatrix);

                //...and we check each mesh's triangle asserting the vertices' flags and adding the triangle to the processTriangles list
                for (int tIndex = 0; tIndex < numTriangles; tIndex++)
                {
                    Triangle tempTriangle = mesh.GetTriangle(tIndex);
                    Vector3 pointToCameraVec = cameraPosInObjSpace - mesh.GetVertex(tempTriangle.V1Index).Position3D;
                    float scalarProd = Vector3.Dot(pointToCameraVec, mesh.GetNormal(tIndex));

                    if(scalarProd > 0)
                    {
                        trianglesToClip.Add(tempTriangle);                        
                        verticesMask[tempTriangle.V1Index] = true;
                        verticesMask[tempTriangle.V2Index] = true;
                        verticesMask[tempTriangle.V3Index] = true;
                    }                    
                }              

                //calculate global Matrix
                Matrix4x4 localToWorld = wObject.LocalToWorldMatrix;
                Matrix4x4 globalMatrix = localToWorld * worldToProj;

                //projection
                for (int vIndex = 0; vIndex < vertices4D.Count; vIndex++)
                {
                    if (verticesMask[vIndex])
                    {
                        vertices4D[vIndex] = Vector4.Transform(vertices4D[vIndex], globalMatrix);
                        verticesMask[vIndex] = false;  //we reset the status for the clipping stage
                    }
                                          
                }
                
                //we create a new List<Triangle> for the triangles that passes the clip stage initializing it to trianglesToClip.Count
                List<Triangle> trianglesToRender = new List<Triangle>(trianglesToClip.Count);                

                //triangle clipping (not fully implemented)
                for (int tIndex = 0; tIndex < trianglesToClip.Count; tIndex++)
                {
                    Triangle tempTri = trianglesToClip[tIndex];

                    Vector4 p1 = vertices4D[tempTri.V1Index]; 
                    Vector4 p2 = vertices4D[tempTri.V2Index];
                    Vector4 p3 = vertices4D[tempTri.V3Index];                    

                    
                    //for now we reject only triangles completely outside
                    if (IsVertexInsideNDCSpace(p1) || IsVertexInsideNDCSpace(p2) || IsVertexInsideNDCSpace(p3))
                    {
                        trianglesToRender.Add(tempTri);
                        verticesMask[tempTri.V1Index] = true;
                        verticesMask[tempTri.V2Index] = true;
                        verticesMask[tempTri.V3Index] = true;
                    }
                    
                }
                //here we can free the trianglesToClip list
                //trianglesToClip = null;

                //division and transformation to viewport
                for (int vIndex = 0; vIndex < vertices4D.Count; vIndex++)
                {
                    if (verticesMask[vIndex])
                    {
                        vertices4D[vIndex] = vertices4D[vIndex] / vertices4D[vIndex].W;
                        vertices4D[vIndex] = Vector4.Transform(vertices4D[vIndex], viewportMatrix);
                    }                        
                }

                //creating the fragments to display
                for (int tIndex = 0; tIndex < trianglesToRender.Count; tIndex++)
                {
                    Triangle tempTri = trianglesToRender[tIndex];
                    Point p1 = new Point(vertices4D[tempTri.V1Index].X, vertices4D[tempTri.V1Index].Y);
                    Point p2 = new Point(vertices4D[tempTri.V2Index].X, vertices4D[tempTri.V2Index].Y);
                    Point p3 = new Point(vertices4D[tempTri.V3Index].X, vertices4D[tempTri.V3Index].Y);

                    fragments.Add(new Fragment(p1, p2, p3));
                }

                debugNumTrianglesFromObjects += numTriangles;
                debugNumTrianglesSentToClip += trianglesToClip.Count;
                debugNumTrianglesSentToRender += trianglesToRender.Count;
            }           

            //preparing text to display in the console
            StringBuilder consoleSB = new StringBuilder();
            consoleSB.AppendLine(String.Format("X: {0:F3}", _world.Camera.Position.X));
            consoleSB.AppendLine(String.Format("Y: {0:F3}", _world.Camera.Position.Y));
            consoleSB.AppendLine(String.Format("Z: {0:F3}", _world.Camera.Position.Z));
            consoleSB.AppendLine();
            consoleSB.AppendLine(String.Format("thetaX: {0:F3}", _world.Camera.Orientation.X));
            consoleSB.AppendLine(String.Format("thetaY: {0:F3}", _world.Camera.Orientation.Y));
            consoleSB.AppendLine(String.Format("thetaZ: {0:F3}", _world.Camera.Orientation.Z));
            consoleSB.AppendLine();
            consoleSB.AppendLine(String.Format("FOV: {0:F3}", _world.Camera.FOV));
            consoleSB.AppendLine();
            consoleSB.AppendLine(String.Format("Triangles (meshes): {0}", debugNumTrianglesFromObjects));
            consoleSB.AppendLine(String.Format("Triangles (facing): {0}", debugNumTrianglesSentToClip));
            consoleSB.AppendLine(String.Format("Triangles (render): {0}", debugNumTrianglesSentToRender));
                       
            //draw
            Dispatcher.Invoke(() =>
            {
                //_mainWindow.ClearCanvas();
                _mainWindow.DrawFragments(fragments);

                _console.Clear();
                _console.WriteLine(consoleSB.ToString());
            }, DispatcherPriority.Background);
        }

        public Task RenderAsync()
        {
            return Task.Run(() => Render());
        }

        public async void StartEngineLoopAsync()
        {
            Stopwatch globalWatch = Stopwatch.StartNew();
            double lastCycleTimeInSecs = 0.0;
            long numFrames = 0;
            

            while (true)
            {
                double timeInSecs = (globalWatch.ElapsedTicks * 1.0) / Stopwatch.Frequency;
                float deltaTimeInSecs = (float) (timeInSecs - lastCycleTimeInSecs);

                Vector3 cameraMovement = _cameraForwardMovement - _cameraBackwardMovement; 
                if (cameraMovement != Vector3.Zero)
                    cameraMovement = Vector3.Normalize(cameraMovement);

                Vector3 cameraRotation = _cameraPositiveRotation - _cameraNegativeRotation;
                float fovChange = _fovIncrease - _fovDecrease;

                _world.Update(deltaTimeInSecs, cameraMovement, cameraRotation, fovChange);
                await RenderAsync();
                //await Task.Delay(1);        //to keep the FPS at bay

                int fps = (int)(1.0f / deltaTimeInSecs);
                int avgFps = (int)(numFrames / timeInSecs);
                _mainWindow.Title = String.Format("FPS: {0} | AVG: {1}", fps, avgFps);

                numFrames++;

                lastCycleTimeInSecs = timeInSecs;
            }
        }

        private static bool IsVertexInsideNDCSpace(Vector4 v)
        {
            return -v.W <= v.X && v.X <= v.W &&
                   -v.W <= v.Y && v.Y <= v.W &&
                    0 <= v.Z && v.Z<= v.W;
        }

        private static Mesh LoadMeshFromObjFile(string filename)
        {
            List<Vertex> vertices = new List<Vertex>();
            List<Triangle> triangles = new List<Triangle>();

            using (FileStream fs = File.OpenRead(filename))
            {
                using (StreamReader objReader = new StreamReader(fs))
                {
                    while (!objReader.EndOfStream)
                    {
                        string text = objReader.ReadLine();
                        string[] parts = text.Split();
                        switch (parts[0])
                        {
                            case "v":
                                float x = float.Parse(parts[1]);
                                float y = float.Parse(parts[2]);
                                float z = float.Parse(parts[3]);
                                vertices.Add(new Vertex(new Vector3(x, y, z)));
                                break;
                            case "f":
                                int v1 = Int32.Parse(parts[1]);
                                int v2 = Int32.Parse(parts[2]);
                                int v3 = Int32.Parse(parts[3]);
                                triangles.Add(new Triangle(v1 - 1, v2 - 1, v3 - 1));
                                break;
                            default:
                                break;
                        }

                    }
                }

                return new Mesh(vertices, triangles);
            }


        }

        #region MOVEMENT
        /// START MOVING
        private void StartMovingCameraRight() 
        {
            _cameraForwardMovement.X = 1f;
        }

        private void StartMovingCameraLeft()
        {
            _cameraBackwardMovement.X = 1f;
        }

        private void StartMovingCameraUp()
        {
            _cameraForwardMovement.Y = 1f;
        }

        private void StartMovingCameraDown()
        {
            _cameraBackwardMovement.Y = 1f;
        }

        private void StartMovingCameraForward()
        {
            _cameraForwardMovement.Z = 1f;
        }

        private void StartMovingCameraBackward()
        {
            _cameraBackwardMovement.Z = 1f;
        }

        /// STOP MOVING
        
        private void StopMovingCameraRight()
        {
            _cameraForwardMovement.X = 0f;
        }

        private void StopMovingCameraLeft()
        {
            _cameraBackwardMovement.X = 0f;
        }

        private void StopMovingCameraUp()
        {
            _cameraForwardMovement.Y = 0f;
        }

        private void StopMovingCameraDown()
        {
            _cameraBackwardMovement.Y = 0f;
        }

        private void StopMovingCameraForward()
        {
            _cameraForwardMovement.Z = 0f;
        }
                
        private void StopMovingCameraBackward()
        {
            _cameraBackwardMovement.Z = 0f;
        }
        #endregion

        #region ROTATION
        private void StartPitchingCameraDown()  //positive rotation is clockwise around the X axis
        {
            _cameraPositiveRotation.X = 1f;
        }

        private void StartPitchingCameraUp()
        {
            _cameraNegativeRotation.X = 1f;
        }

        private void StartYawingCameraRight()    //positive rotation is clockwise around the Y axis
        {
            _cameraPositiveRotation.Y = 1f;
        }

        private void StartYawingCameraLeft()
        {
            _cameraNegativeRotation.Y = 1f;
        }

        private void StartRollingCameraLeft()  //positive rotation is clockwise around the Z axis but Z axis is reversed
        {
            _cameraPositiveRotation.Z = 1f;
        }

        private void StartRollingCameraRight()
        {
            _cameraNegativeRotation.Z = 1f;
        }

        private void StopPitchingCameraDown()
        {
            _cameraPositiveRotation.X = 0f;
        }

        private void StopPitchingCameraUp()
        {
            _cameraNegativeRotation.X = 0f;
        }

        private void StopYawingCameraRight()
        {
            _cameraPositiveRotation.Y = 0f;
        }

        private void StopYawingCameraLeft()
        {
            _cameraNegativeRotation.Y = 0f;
        }   

        private void StopRollingCameraLeft()
        {
            _cameraPositiveRotation.Z = 0f;
        }

        private void StopRollingCameraRight()
        {
            _cameraNegativeRotation.Z = 0f;
        }

        #endregion

        #region FOV
        public void StartIncreasingFov()
        {
            _fovIncrease = 1;
        }

        public void StartDecreasingFov()
        {
            _fovDecrease = 1;
        }

        public void StopIncreasingFov()
        {
            _fovIncrease = 0;
        }

        public void StopDecreasingFov()
        {
            _fovDecrease = 0;
        }
        #endregion

    }
}
