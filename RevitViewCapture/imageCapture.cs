using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Text.RegularExpressions;



namespace BimImageNet
{

    [Transaction(TransactionMode.Manual)]
    public class imageCapture : IExternalCommand
    {
        GeometricOperations GO = new GeometricOperations();
        utils utils = new utils();
        Random gen = new Random();

        string oriRevitImageName = "Rendering_Revit_ori.png";
        string oriEnscapeImageName = "Rendering_Enscape_ori.png";
        //string finalRevitImageName = "Rendering_Revit.png";
        string finalEnscapeImageName = "Rendering_Enscape.png";

        // default revit camera 
        // double HFOV_r_default = 0.8763482;   // 50.211 degree
        // double VFOV_r_default = 0.67586777;  // 38.724 degree
        double distToNearPlane_d = 0.1;        // feet; constant
        double distToFarPlane_d = 56.36078;    // feet; constan
        double offset_room = 0.656;  // (f) 200 mm; f_to_mm = 304.8

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {                
                Document Doc = commandData.Application.ActiveUIDocument.Document;
                imageCapture_form form = new imageCapture_form(Doc);  //using (imageCapture_form form = new imageCapture_form()): will close the form after implementing all the codes in the block 
                form.ShowDialog(); // only close the form, will the remaining code be implemented, otherwise, the program will keep staying here. When click Capture button, the form will close.

                if (form.run_captureView)
                {
                    #region capture model views
                    DateTime start_viewCapturing = DateTime.Now;

                    // 1. Get all physical objects' geometry 
                    Options geoOption = Doc.Application.Create.NewGeometryOptions();
                    if (geoOption != null)
                    {
                        geoOption.ComputeReferences = true;
                        geoOption.DetailLevel = ViewDetailLevel.Fine;
                    }
                    List<BIMObject> BimObjs = utils.RetrivePhysicalElements(Doc, geoOption);

                    // Enrich geometry representation with surface normal
                    // !!! This is not necessary for ray tracing-based approach
                    // utils.EnrichSurfaceNormal(ref BimObjs);
                    BoundingBoxXYZ newCropBox = adjustRevitCameraFOV(form.HFOV_d, form.VFOV_d);  // focal length (distToNearPlane & distToFarPlane) are fixed
                    Dictionary<string, scene_cameraPose> viewID_sceneCameraLabel = new Dictionary<string, scene_cameraPose>();
                    //captureViewsFromSpaces(Doc, BimObjs, form.output, newCropBox, form.gridInterval, form.viewScope, form.angleInterval);
                    int cameraLocations = 0;
                    if (form.useSpaces)
                    {      
                        captureViewsFromSpaces(Doc, form.selectedSpaceIds, BimObjs, form.output, newCropBox,
                            form.xgridInterval, form.ygridInterval, form.zgridInterval, form.yawRotate, form.pitchRotate, form.rollRotate, ref viewID_sceneCameraLabel, out cameraLocations);
                    } else // use location of existing views 
                    {
                        cameraLocations = form.pred_locations.Count();
                        foreach (var existView in form.pred_locations)
                        {
                            FilteredElementCollector collector_element = new FilteredElementCollector(Doc);
                            var viewElements1 = collector_element.OfClass(typeof(View3D)).ToElements();
                            List<Point3D> camLocations = getViewPorts(Doc, viewElements1, existView);
                            using (Transaction trans2 = new Transaction(Doc, "Create3DView"))
                            {
                                if (trans2.Start() == TransactionStatus.Started)
                                {
                                    // set up camera poses and generate BIM views
                                    setUpCameraAndGenerateViews(camLocations, Doc, newCropBox, form.yawRotate, form.rollRotate, form.pitchRotate, "unknwonSpace", "unknownID");
                                }
                                trans2.Commit();
                            }
                        }
                    }
                    
                   
                    DateTime end_viewCapturing = DateTime.Now;
                    double time_viewCapturing = end_viewCapturing.Subtract(start_viewCapturing).TotalSeconds;
                    #endregion

                    #region create space and image folders, and output scene and camera pose labels
                    FilteredElementCollector collector = new FilteredElementCollector(Doc);
                    var viewElements = collector.OfClass(typeof(View3D)).ToElements();
                    foreach (var view in viewElements)
                    {
                        // view.Name = spaceName + "_" + spaceID + "_" + view3D.Id
                        string[] pieces = view.Name.Split('_');
                        if (pieces.Length == 3)
                        {
                            string spaceFolder = @form.output + "\\" + pieces[0] + "_" + pieces[1];
                            if (!Directory.Exists(spaceFolder))
                                Directory.CreateDirectory(spaceFolder);

                            string imageFolder = spaceFolder + "\\" + pieces[2];
                            if (!Directory.Exists(imageFolder))
                                Directory.CreateDirectory(imageFolder);
                            //File.WriteAllText(imageFolder + "\\" + "sceneAndCameraPose.json", JsonConvert.SerializeObject(viewID_sceneCameraLabel[pieces[2]]));
                            // List<int> ignore = outputBIMImage(Doc, view.Id, spaceFolder, form.imagWidth);
                        }
                    }
                    #endregion

                    //// write out building-level statistics
                    //StreamWriter Writer2 = new StreamWriter(form.output + "\\statistics_building.txt", true);
                    //Writer2.WriteLine("Num of camera locations: " + cameraLocations.ToString());
                    //Writer2.WriteLine("Num of all images: " + viewID_sceneCameraLabel.Keys.Count.ToString());
                    //Writer2.WriteLine("Num of space types: " + getSpaceTypes(viewID_sceneCameraLabel.Values.ToList()).ToString());
                    //Writer2.WriteLine("Num of spaces: " + viewID_sceneCameraLabel.Values.Count.ToString());
                    //Writer2.WriteLine("    [Time]_modelViewCapturing: " + time_viewCapturing.ToString());
                    //Writer2.Close();

                    //if (File.Exists(form.output + "\\statistics_building.json")) // a building processed by batches
                    //{
                    //    JObject o1 = JObject.Parse(File.ReadAllText(form.output + "\\statistics_building.json"));

                    //    o1["Num of camera locations"] = int.Parse((string)o1["Num of camera locations"]) + cameraLocations;
                    //    o1["Num of all images"] = int.Parse((string)o1["Num of all images"]) + viewID_sceneCameraLabel.Keys.Count;
                    //    o1["Num of space types(duplicates)"] = int.Parse((string)o1["Num of space types(duplicates)"]) + getSpaceTypes(viewID_sceneCameraLabel.Values.ToList());  //!!!! this can have duplicates
                    //    o1["Num of spaces"] = int.Parse((string)o1["Num of spaces"]) + form.selectedSpaceIds.Count;                 
                    //    o1["[Time]_modelViewCapturing"] = double.Parse((string)o1["[Time]_modelViewCapturing"]) + time_viewCapturing;
                    //    o1["[Time]_total"] = double.Parse((string)o1["[Time]_total"]) + time_viewCapturing;
                    //    File.WriteAllText(form.output + "\\statistics_building.json", o1.ToString());
                    //}
                    //else
                    //{
                    //    JObject statistics_building = new JObject(
                    //    new JProperty("Num of camera locations", cameraLocations),
                    //    new JProperty("Num of all images", viewID_sceneCameraLabel.Keys.Count),
                    //    new JProperty("Num of space types(duplicates)", getSpaceTypes(viewID_sceneCameraLabel.Values.ToList())), //!!!! this can have duplicates
                    //    new JProperty("Num of spaces", form.selectedSpaceIds.Count),
                    //    new JProperty("[Time]_modelViewCapturing", time_viewCapturing),
                    //     new JProperty("[Time]_total", time_viewCapturing));
                    //    File.WriteAllText(form.output + "\\statistics_building.json", statistics_building.ToString());
                    //}

                    TaskDialog.Show("Processing Status", "View Capturing Completed!");
                }     
                
                if (form.run_checkAndDistortImage)
                {
                    #region Move and rename all the Enscape images to relevant image folder
                    string[] enscapeImages_path = Directory.GetFiles(@form.output); // original enscape image names
                    string[] spaceFolders_path = Directory.GetDirectories(@form.output); // space folders
                    foreach (var enscapeImage_path in enscapeImages_path)
                    {
                        string enscapeImage = Path.GetFileName(enscapeImage_path);
                        if (enscapeImage.Contains(".png"))
                        {
                            string[] pieces = enscapeImage.Split('_');
                            string enscapeImage_viewID = pieces.Last(); // viewID+ ".png"
                            string enscapeImage_spaceID = pieces[pieces.Count() - 2];
                            foreach (var spaceFolder_path in spaceFolders_path)
                            {
                                string spaceFolder = Path.GetFileName(spaceFolder_path);
                                if (spaceFolder.Split('_').Last() == enscapeImage_spaceID)
                                {
                                    string[] imageFolders_path = Directory.GetDirectories(spaceFolder_path);
                                    foreach (var imageFolder_path in imageFolders_path)
                                    {
                                        string imageFolder = Path.GetFileName(imageFolder_path);
                                        //if (!imageFolder.Contains("Invalid"))
                                        //{
                                        if ((imageFolder + ".png") == enscapeImage_viewID)
                                        {
                                            string destFile = imageFolder_path + "\\" + this.oriEnscapeImageName;
                                            File.Move(enscapeImage_path, destFile);
                                            break;
                                        }
                                        //}
                                    }
                                    break;
                                }
                            }
                        }
                    }
                    #endregion

                    #region check valid images
                    //DateTime start_validChecking = DateTime.Now;
                    List<string> validViewNames_building = new List<string>();
                    foreach (var spacefolder in spaceFolders_path)
                    {
                        // identify invalid images (similar + plain) and annotate relevant views in revit
                        Process proc = new Process();
                        proc.StartInfo.FileName = @form.invalidCheckingFunction;
                        // A single argumnent cannot include space; otherwise will be treated as several argumnents
                        // we can pass several arguments by using " " to connect, and the called exe can automatically recieved these arguments 
                        // we can also combine several arguments into a single argument with specific symbols, as long as the target exe includes functions to parse it
                        // Note: 1. the first argument is always the path of the exe
                        //       2. the passed arguments are always string type, which may need to be coverted into proper types in the called external exe.
                        proc.StartInfo.Arguments = form.edge_threshold.ToString() + "," + form.similarity_threshold.ToString() + "," + spacefolder;
                        proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden; // normal-show the window; hidden -do not show the window
                        proc.Start();
                        proc.WaitForExit();

                    // get valid view names
                    List<string> validViewNames = new List<string>();
                    using (StreamReader file = new StreamReader(spacefolder + "\\" + "validImageList.txt"))
                    {
                        string line;
                        while ((line = file.ReadLine()) != null)
                        {
                            line = line.Replace("\n", String.Empty);
                            validViewNames.Add(line);
                        }
                    }
                    validViewNames_building.AddRange(validViewNames);

                    //// write out statistics for each space
                    //StreamWriter Writer1 = new StreamWriter(spacefolder + "\\statistics_space.txt", true);
                    //Writer1.WriteLine("Num of valid images: " + validViewNames.Count.ToString());
                    //Writer1.Close();
                }

                // mark invalid views in Revit:
                FilteredElementCollector collector2 = new FilteredElementCollector(Doc);
                var viewElements2 = collector2.OfClass(typeof(View3D)).ToElements();
                foreach (var view in viewElements2)
                {
                    int num_ = view.Name.Split('_').Count();
                    if (num_ == 3)
                    {
                        if (!validViewNames_building.Contains(view.Id.ToString()))
                        {
                            using (Transaction trans = new Transaction(Doc, "Mark invalid images"))
                            {
                                if (trans.Start() == TransactionStatus.Started)
                                    view.Name = "Invalid_" + view.Name;
                                trans.Commit();
                            }
                        }
                    }
                }

                //DateTime end_validChecking = DateTime.Now;
                //double time_validChecking = end_validChecking.Subtract(start_validChecking).TotalSeconds;
                #endregion

                #region  Distort all the valid Enscape imges
                DateTime start_imageDistortion = DateTime.Now;
                    bool distort = true;
                    if (form.distortion[0] == 0 && form.distortion[1] == 0 &&
                        form.distortion[2] == 0 && form.distortion[3] == 0)
                        distort = false;
                    if (distort)
                        distortValidImages(@form.output, form.distortion, Doc);
                    else
                        makeCopiesWithNewName(@form.output, Doc);

                    DateTime end_imageDistortion = DateTime.Now;
                    double time_imageDistortion = end_imageDistortion.Subtract(start_imageDistortion).TotalSeconds;
                    #endregion

                    //// write out building -level statistics
                    //StreamWriter Writer2 = new StreamWriter(form.output + "\\statistics_building.txt", true);
                    ////Writer2.WriteLine("Num of valid images: " + validViewNames_building.Count.ToString());
                    ////Writer2.WriteLine("    [Time]_validChecking: " + time_validChecking.ToString());
                    //Writer2.WriteLine("    [Time]_validImageDistortion: " + time_imageDistortion.ToString());
                    //Writer2.Close();

                    //if (File.Exists(form.output + "\\statistics_building.json")) // a building processed by batches
                    //{
                    //    JObject o1 = JObject.Parse(File.ReadAllText(form.output + "\\statistics_building.json"));
                    //    if (o1.ContainsKey("Num of valid images"))
                    //    {
                    //        o1["Num of valid images"] = int.Parse((string)o1["Num of valid images"]) + validViewNames_building.Count;
                    //        o1["[Time]_validChecking"] = double.Parse((string)o1["[Time]_validChecking"]) + time_validChecking;
                    //        o1["[Time]_validImageDistortion"] = double.Parse((string)o1["[Time]_validImageDistortion"]) + time_imageDistortion;
                    //    }
                    //    else
                    //    {
                    //        o1.Add("Num of valid images", validViewNames_building.Count);
                    //        o1.Add("[Time]_validChecking", time_validChecking);
                    //        o1.Add("[Time]_validImageDistortion", time_imageDistortion);
                    //    }
                    //    o1["[Time]_total"] = double.Parse((string)o1["[Time]_total"]) + time_validChecking + time_imageDistortion;
                    //    File.WriteAllText(form.output + "\\statistics_building.json", o1.ToString());
                    //}

                    TaskDialog.Show("Processing Status", "Valid Image Capturing Completed!");
                }

                return Result.Succeeded;
            }
            catch (Exception e)
            {
                TaskDialog.Show("Revit", e.Message);
                return Result.Failed;
            }
        }

        // !!! Camera pose (6 DOF):  Euler angles <--> (Roll-x, Pitch-y, Yaw-z)  <--> rotation matrix
        // formular represent how rotation matrix is calculated based Roll, Pitch, Yaw: https://en.wikipedia.org/wiki/Rotation_matrix 
        int setUpCameraAndGenerateViews(List<Point3D> cameraPositions, Document Doc, BoundingBoxXYZ newCropBox,
            double yaw_rotate, double roll_rotate, double pitch_rotate, string spaceName, string spaceID)
        {
            GeometricOperations GO = new GeometricOperations();
            var viewFamilyTypes = from elem in new FilteredElementCollector(Doc).OfClass(typeof(ViewFamilyType))
                                  let type = elem as ViewFamilyType
                                  where type.ViewFamily == ViewFamily.ThreeDimensional
                                  select type;
            ElementId viewFamilyTypes_ID = viewFamilyTypes.FirstOrDefault().Id;
            int num_img = 0;
            //double num_yaw = 360 / yaw_rotate;
            //double num_roll = 360 / roll_rotate;
            double num_pitch = 360 / pitch_rotate;
            //Parallel.For(0, cameraPositions.Count(), icam =>
            //{
            for (int icam = 0; icam < cameraPositions.Count(); icam++)
            {
                XYZ eye = new XYZ(cameraPositions[icam].x, cameraPositions[icam].y, cameraPositions[icam].z);
                //for (int i = 0; i < num_roll; i++)
                //{
                    double roll = (270 /*+ i * roll_rotate*/) * Math.PI / 180;
                    for (int j = 0; j < num_pitch; j++)
                    {
                         double pitch = j * pitch_rotate * Math.PI / 180;
                        //for (int k = 0; k < num_yaw; k++)
                        //{
                            double yaw = 0/*k * yaw_rotate * Math.PI / 180*/;
                            double[,] rt_GLS2LCS = getRotationMatrix(roll, pitch, yaw);
                            double[,] rt_LCS2GLS = GO.IMatrix_Matrix(rt_GLS2LCS);
                            XYZ forward = new XYZ(rt_LCS2GLS[0, 2], rt_LCS2GLS[1, 2], rt_LCS2GLS[2, 2]);
                            XYZ up = new XYZ(rt_LCS2GLS[0, 1], rt_LCS2GLS[1, 1], rt_LCS2GLS[2, 1]);
                            num_img += 1;
                            DateTime randomDateTime = getRandomTime();
                            captureViews(Doc, viewFamilyTypes_ID, eye, forward, up, newCropBox, randomDateTime, spaceName, spaceID);
                        //}
                    }
                //}
            }
                //});
                return num_img;
        }

        List<Point3D> getViewPorts(Document doc, IList<Element> views, string viewList)
        {
            List<Point3D> results = new List<Point3D>();
            foreach (var view in views)
            {
                if (viewList == view.Name)
                {
                    View3D view3D = view as View3D;
                    var context = new MyExportContext();
                    CustomExporter exporter = new CustomExporter(doc, context)
                    {
                        IncludeGeometricObjects = false,
                        ShouldStopOnError = true
                    };
                    exporter.Export(view3D);

                    var viewOrientation3D = view3D.GetOrientation();
                    results.Add(new Point3D(viewOrientation3D.EyePosition.X, viewOrientation3D.EyePosition.Y, viewOrientation3D.EyePosition.Z));
                }
            }
            return results;
        }

        int getSpaceTypes(List<scene_cameraPose> input)
        {
            List<string> spaceTypes = new List<string>();
            foreach (var scene in input)
            {
                string pureName = Regex.Replace(scene.scene, @"[\d-]", string.Empty);
                if (!spaceTypes.Contains(pureName))
                    spaceTypes.Add(pureName);
            }
            return spaceTypes.Count();
        }

        void distortValidImages(string output, double[] distortion, Document Doc)
        {
            FilteredElementCollector collector_element = new FilteredElementCollector(Doc);
            var views = collector_element.OfClass(typeof(View3D)).ToElements();
            foreach (var view in views)
            {
                string[] pieces = view.Name.Split('_');
                if (pieces.Length == 3) // valid views
                {
                    // distort the enscape image
                    string targetFile = output + "\\" + pieces[0] + '_' + pieces[1] + "\\" + pieces[2] + "\\" + this.oriEnscapeImageName;
                    Image image = Image.FromFile(targetFile);
                    double[,] interiorParameters = getCameraInteriorParameters(Doc, view, image.Width, image.Width);  // get interior prameters
                    distortImage(targetFile, interiorParameters, distortion, this.finalEnscapeImageName);

                    //// distort the Revit image
                    //string targetFile2 = output + "\\" + pieces[0] + '_' + pieces[1] + "\\" + pieces[2] + "\\" + this.oriRevitImageName;
                    //distortImage(targetFile2, interiorParameters, distortion, this.finalRevitImageName);
                }
            } 
        }

        void makeCopiesWithNewName(string output, Document Doc)
        {
            FilteredElementCollector collector_element = new FilteredElementCollector(Doc);
            var views = collector_element.OfClass(typeof(View3D)).ToElements();
            foreach (var view in views)
            {
                string[] pieces = view.Name.Split('_');
                if (pieces.Length == 3) // valid views
                { 
                    string file1 = output + "\\" + pieces[0] + '_' + pieces[1] + "\\" + pieces[2] + "\\" + this.oriEnscapeImageName;
                    string target1 = output + "\\" + pieces[0] + '_' + pieces[1] + "\\" + pieces[2] + "\\" + this.finalEnscapeImageName;
                    File.Copy(file1, target1);

                    //string file2 = output + "\\" + pieces[0] + '_' + pieces[1] + "\\" + pieces[2] + "\\" + this.oriRevitImageName;
                    //string target2 = output + "\\" + pieces[0] + '_' + pieces[1] + "\\" + pieces[2] + "\\"  + this.finalRevitImageName;
                    //File.Copy(file2, target2);
                }
            }
        }

        void distortImage(string ima, double[,] interiorParameters, double[] distortion, string targetFileName)
        {
            Bitmap img = new Bitmap(ima);
            Bitmap distIma = new Bitmap(img.Width, img.Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);

            for (int i = 0; i < img.Width; i++)
            {
                for (int j = 0; j < img.Height; j++)
                {
                    double[] newPixel = getDistortPixel(i, j, interiorParameters, distortion);

                    // bilinear interplation                 
                    int u0 = Convert.ToInt32(Math.Floor(newPixel[0]));
                    int v0 = Convert.ToInt32(Math.Floor(newPixel[1]));
                    int u1 = u0 + 1;
                    int v1 = v0 + 1;

                    double dx = newPixel[0] - u0;
                    double dy = newPixel[1] - v0;

                    if (u0 >= 0 && u1 < img.Width && v0 >= 0 && v1 < img.Height)
                    {
                        double w1 = (1 - dx) * (1 - dy);
                        double w2 = (1 - dx) * dy;
                        double w3 = dx * (1 - dy);
                        double w4 = dx * dy;

                        double apha1 = w1 * img.GetPixel(u0, v0).A;
                        double apha2 = w2 * img.GetPixel(u0, v1).A;
                        double apha3 = w3 * img.GetPixel(u1, v0).A;
                        double apha4 = w4 * img.GetPixel(u1, v1).A;
                        int apha = Convert.ToInt32(apha1 + apha2 + apha3 + apha4);

                        double r1 = w1 * img.GetPixel(u0, v0).R;
                        double r2 = w2 * img.GetPixel(u0, v1).R;
                        double r3 = w3 * img.GetPixel(u1, v0).R;
                        double r4 = w4 * img.GetPixel(u1, v1).R;
                        int r = Convert.ToInt32(r1 + r2 + r3 + r4);

                        double g1 = w1 * img.GetPixel(u0, v0).G;
                        double g2 = w2 * img.GetPixel(u0, v1).G;
                        double g3 = w3 * img.GetPixel(u1, v0).G;
                        double g4 = w4 * img.GetPixel(u1, v1).G;
                        int g = Convert.ToInt32(g1 + g2 + g3 + g4);

                        double b1 = w1 * img.GetPixel(u0, v0).B;
                        double b2 = w2 * img.GetPixel(u0, v1).B;
                        double b3 = w3 * img.GetPixel(u1, v0).B;
                        double b4 = w4 * img.GetPixel(u1, v1).B;
                        int b = Convert.ToInt32(b1 + b2 + b3 + b4);

                        System.Drawing.Color color = System.Drawing.Color.FromArgb(apha, r, g, b);
                        distIma.SetPixel(i, j, color);
                    }
                }
            }
            string folder = Path.GetDirectoryName(ima);
            distIma.Save(folder + "\\" + targetFileName);
        }

        double[] getDistortPixel(int col, int row, double[,] ip, double[] dist)
        {
            double[] result = new double[2];

            double x_ = (col - ip[0, 2]) / ip[0, 0];
            double y_ = (row - ip[1, 2]) / ip[1, 1];
            double r2 = x_ * x_ + y_ * y_;

            double x__ = x_ * (1 + dist[0] * r2 + dist[1] * r2 * r2) + 2 * dist[2] * x_ * y_ + dist[3] * (r2 * r2 + 2 * x_ * x_);
            double y__ = y_ * (1 + dist[0] * r2 + dist[1] * r2 * r2) + dist[2] * (r2 + 2 * y_ * y_) + 2 * dist[3] * x_ * y_;

            result[0] = ip[0, 0] * x__ + ip[0, 2];
            result[1] = ip[1, 1] * y__ + ip[1, 2];
            return result;
        }

        double[,] getCameraInteriorParameters(Document Doc, Element view, int Width, int Height)
        {
            double[,] para = new double[3, 3];

            View3D view_ = view as View3D;
            var context = new MyExportContext();
            CustomExporter exporter = new CustomExporter(Doc, context)
            {
                IncludeGeometricObjects = false,
                ShouldStopOnError = true
            };
            exporter.Export(view_);
            var cameraInfo = context.CameraLocalInfo;

            double w = view_.Outline.Max.U - view_.Outline.Min.U;
            double h = view_.Outline.Max.V - view_.Outline.Min.V;
            double f = w / (2 * Math.Tan(cameraInfo.HorizontalFov / 2.0));
            para[0, 0] = -1 * Width * f / w;
            para[0, 1] = 0;
            para[0, 2] = Width / 2;
            para[1, 0] = 0;
            para[1, 1] = Height * f / h;
            para[1, 2] = Height / 2;
            para[2, 0] = 0;
            para[2, 1] = 0;
            para[2, 2] = 1;
            return para;
        }

        void captureViewsFromSpaces(Document Doc, List<string> spaceIds, List<BIMObject> BimObjs, string outputPath, BoundingBoxXYZ newCropBox, 
            double xgridInterval, double ygridInterval, double zgridInterval, double yaw_rotate, double pitch_rotate, double roll_rotate,
            ref Dictionary<string, scene_cameraPose> viewID_sceneCameraLabel, out int cameraLocations)
        {
            cameraLocations = 0;

            List<RoomObject> rooms = utils.RetriveRooms(Doc, spaceIds);
            using (Transaction trans = new Transaction(Doc, "Create3DView"))
            {
                if (trans.Start() == TransactionStatus.Started)
                {
                    foreach (var r in rooms)
                    {
                        // calclulate fesibile camera positions (grid-based)
                        List<Point3D> camera_positions = generateGrid(r.triBrep, xgridInterval, ygridInterval, zgridInterval);
                        camera_positions = removeInvalid(camera_positions, BimObjs);
                        cameraLocations += camera_positions.Count;

                        // set up camera poses and generate BIM views
                        int images = setUpCameraAndGenerateViews(camera_positions, Doc, newCropBox, yaw_rotate, roll_rotate, pitch_rotate, r.Name, r.GUID,
                            ref viewID_sceneCameraLabel);

                        // write out statistics for each space
                        string spacefolder = outputPath + "\\" + r.Name.Replace(" ", string.Empty) + "_" + r.GUID;
                        if (!Directory.Exists(spacefolder))
                            Directory.CreateDirectory(spacefolder);
 
                        // true means enable to append new contents to an existing file; if not exist, create a new one
                        StreamWriter Writer = new StreamWriter(spacefolder + "\\statistics_space.txt", true);
                        Writer.WriteLine("Num of camera locations: " + camera_positions.Count.ToString());
                        Writer.WriteLine("Num of all images: " + images.ToString());
                        Writer.Close();
                    }
                }
                trans.Commit();
            }
        }

        BoundingBoxXYZ adjustRevitCameraFOV(double HFOV_degree, double VFOV_degree)
        {
            BoundingBoxXYZ result = new BoundingBoxXYZ();

            double zmax = -1 * distToNearPlane_d;
            double xmax = distToNearPlane_d * Math.Tan(Math.PI * HFOV_degree / 360);
            double ymax = distToNearPlane_d * Math.Tan(Math.PI * VFOV_degree / 360);
            result.Max = new XYZ(xmax, ymax, zmax);

            double xmin = -1 * xmax;
            double ymin = -1 * ymax;
            double zmin = -1 * distToFarPlane_d;
            result.Min = new XYZ(xmin, ymin, zmin);

            return result;
        }

        // !!! Camera pose (6 DOF):  Euler angles <--> (Roll-x, Pitch-y, Yaw-z)  <--> rotation matrix
        // formular represent how rotation matrix is calculated based Roll, Pitch, Yaw: https://en.wikipedia.org/wiki/Rotation_matrix 
        int setUpCameraAndGenerateViews(List<Point3D> cameraPositions, Document Doc, BoundingBoxXYZ newCropBox, 
            double yaw_rotate, double roll_rotate, double pitch_rotate, string spaceName, string spaceID, ref Dictionary<string, scene_cameraPose> viewID_sceneCameraLabel)
        {
            GeometricOperations GO = new GeometricOperations();
            var viewFamilyTypes = from elem in new FilteredElementCollector(Doc).OfClass(typeof(ViewFamilyType))
                                  let type = elem as ViewFamilyType
                                  where type.ViewFamily == ViewFamily.ThreeDimensional
                                  select type;
            ElementId viewFamilyTypes_ID = viewFamilyTypes.FirstOrDefault().Id;
            int num_img = 0;
            double num_yaw = 90 / yaw_rotate;
            double num_roll = 360 / roll_rotate;
            double num_pitch = 180 / pitch_rotate;  // either pitch or roll should be (0, 180); the other one is (0,360)
            //Parallel.For(0, cameraPositions.Count(), icam =>
            //{
            for (int icam = 0; icam < cameraPositions.Count(); icam++)
            {
                XYZ eye = new XYZ(cameraPositions[icam].x, cameraPositions[icam].y, cameraPositions[icam].z);
                for (int i = 0; i < num_roll; i++)
                {
                    double roll_d = 270 + i * roll_rotate;  // set starting point as 270 to make sure z-axis is rotated to be horizontal
                    double roll = roll_d * Math.PI / 180;
                    for (int j = 0; j < num_pitch; j++)
                    {
                        double pitch_d = j * pitch_rotate;
                        double pitch = pitch_d * Math.PI / 180;
                        for (int k = 0; k < num_yaw; k++)
                        {
                            double yaw_d = k * yaw_rotate;
                            double yaw = yaw_d * Math.PI / 180;
                            double[,] rt_GLS2LCS = getRotationMatrix(roll, pitch, yaw);
                            double[,] rt_LCS2GLS = GO.IMatrix_Matrix(rt_GLS2LCS);
                            XYZ forward = new XYZ(rt_LCS2GLS[0, 2], rt_LCS2GLS[1, 2], rt_LCS2GLS[2, 2]); // !!!! forward is reverse to the z of CCS
                            XYZ up = new XYZ(rt_LCS2GLS[0, 1], rt_LCS2GLS[1, 1], rt_LCS2GLS[2, 1]);
                            num_img += 1;
                            DateTime randomDateTime = getRandomTime();
                            string viewid = captureViews(Doc, viewFamilyTypes_ID, eye, forward, up, newCropBox, randomDateTime, spaceName, spaceID);

                            scene_cameraPose sceneCamera = new scene_cameraPose();
                            sceneCamera.scene = spaceName;
                            sceneCamera.cameraPose.X = cameraPositions[icam].x * 304.8;
                            sceneCamera.cameraPose.Y = cameraPositions[icam].y * 304.8;
                            sceneCamera.cameraPose.Z = cameraPositions[icam].z * 304.8;
                            sceneCamera.cameraPose.Yaw = yaw_d;
                            sceneCamera.cameraPose.Pitch = pitch_d;
                            sceneCamera.cameraPose.Roll = roll_d;
                            viewID_sceneCameraLabel.Add(viewid, sceneCamera);
                        }
                    }
                }
            }
            //});
            return num_img;
        }

        DateTime getRandomTime_inst()
        {
            //List<int[]> list_time = new List<int[]>();
            //int[] time1 = { 9, 0, 0 };
            //int[] time2 = { 15, 0, 0 };
            //int[] time3 = { 21, 0, 0 };
            //list_time.Add(time1);
            //list_time.Add(time2);
            //list_time.Add(time3);

            DateTime start = new DateTime(2021, 1, 1);
            DateTime end = new DateTime(2021, 12, 31);
            int range = (end - start).Days;
            DateTime newStart = start.AddDays(gen.Next(range));

            //int[] time = list_time[gen.Next(3)];
            DateTime result = new DateTime(newStart.Year, newStart.Month, newStart.Day, 21, 0, 0 /*time[0], time[1], time[2]*/);
            return result;
        }


        DateTime getRandomTime()
        {
            List<int[]> list_time = new List<int[]>();
            int[] time1 = { 9, 0, 0 };
            int[] time2 = { 15, 0, 0 };
            int[] time3 = { 21, 0, 0 };
            list_time.Add(time1);
            list_time.Add(time2);
            list_time.Add(time3);

            DateTime start = new DateTime(2021, 1, 1);
            DateTime end = new DateTime(2021, 12, 31);
            int range = (end - start).Days;
            DateTime newStart = start.AddDays(gen.Next(range));

            int[] time = list_time[gen.Next(3)];
            DateTime result = new DateTime(newStart.Year, newStart.Month, newStart.Day, time[0], time[1], time[2]);
            return result;
        }

        double[,] getRotationMatrix(double roll, double pitch, double yaw)
        {
            double[,] result = new double[3, 3];

            double cosr = Math.Cos(roll);
            double sinr = Math.Sin(roll);

            double cosb = Math.Cos(pitch);
            double sinb = Math.Sin(pitch);

            double cosa = Math.Cos(yaw);
            double sina = Math.Sin(yaw);

            result[0, 0] = cosa * cosb;
            result[1, 0] = sina * cosb;
            result[2, 0] = -1 * sinb;
            result[0, 1] = cosa * sinb * sinr - sina * cosr;
            result[1, 1] = sina * sinb * sinr + cosa * cosr;
            result[2, 1] = cosb * sinr;
            result[0, 2] = cosr * sinb * cosa + sinr * sina;
            result[1, 2] = cosr * sinb * sina - sinr * cosa;
            result[2, 2] = cosr * cosb;

            return result;
        }

        string captureViews(Document Doc, ElementId viewFamilyTypes_ID, XYZ eye, XYZ forward, XYZ up, BoundingBoxXYZ newCropBox, DateTime dt, string spaceName, string spaceID)
        {
            View3D view3D = View3D.CreatePerspective(Doc, viewFamilyTypes_ID);
            if (view3D != null)
            {
                // !!! forward is opposite to CCS z-axis direction
                view3D.SetOrientation(new ViewOrientation3D(eye, up, forward));

                // turn on the far clip plane with standard parameter API
                Parameter farClip = view3D.LookupParameter("Far Clip Active");
                farClip.Set(1);

                Parameter cropRegionVisible = view3D.LookupParameter("Crop Region Visible");
                cropRegionVisible.Set(1);

                Parameter cropView = view3D.LookupParameter("Crop View");
                cropView.Set(1);

                view3D.DetailLevel = ViewDetailLevel.Fine;

                view3D.SunAndShadowSettings.SunAndShadowType = SunAndShadowType.StillImage;
                view3D.SunAndShadowSettings.StartDateAndTime = DateTime.SpecifyKind(dt, DateTimeKind.Local);
                
                // cannot assign project location from here
                // view3D.SunAndShadowSettings.ProjectLocationName

                // RevitAPI does not expose other items in GUI of Graphifc Display Option, e.g., Lighting, Realistic, Background 
                view3D.DisplayStyle = DisplayStyle.Realistic;  // not stable -- usually produce gray image              

                if (view3D.CropBoxActive == false)
                    view3D.CropBoxActive = true;
                view3D.CropBox = newCropBox;

                spaceName = spaceName.Replace(" ", string.Empty);
                view3D.Name = spaceName + "_" + spaceID + "_" + view3D.Id;
                //view3D.get_Parameter(BuiltInParameter.MODEL_GRAPHICS_STYLE).Set(3);
                // 1: wireframe; 2: hidenline; 3: shade without edge (gray); 4: shade with edge (gray); 5=2; 6=3
            }
            return view3D.Id.ToString();
        }

        // remove all the points inside the bounding box of non structural element physical objects
        List<Point3D> removeInvalid(List<Point3D> gridCorners, List<BIMObject> BimObjects)
        {
            List<Point3D> result = new List<Point3D>();

            List<BIMObject> nonstructuralElements = new List<BIMObject>();
            foreach (var obj in BimObjects)
            {
                bool skip = false;
                if (obj.objectType.Contains("Wall") || obj.objectType.Contains("Floor") || obj.objectType.Contains("Curtain")
                    || obj.objectType.Contains("Window") || obj.objectType.Contains("Door") || obj.objectType.Contains("Roof"))
                    skip = true;
                if (!skip)
                    nonstructuralElements.Add(obj);
            }

            foreach (var p in gridCorners)
            {
                bool inside = false;
                foreach (var obj in nonstructuralElements)
                {
                    foreach (var bb in obj.AABBS_objParts)
                    {
                        if (GO.PointInAABBTest(p, bb))
                        {
                            inside = true;
                            break;
                        }
                    }
                    if (inside)
                        break;
                }
                if (!inside)
                    result.Add(p);
            }
            return result;
        }

        List<Point3D> generateGrid(TriangulatedBrep triBrep, double xgridInterval, double ygridInterval, double zgridInterval)
        {
            List<Point3D> result = new List<Point3D>();

            double xnum = (triBrep.AABB.xmax - triBrep.AABB.xmin - 2 * offset_room) / xgridInterval;
            double ynum = (triBrep.AABB.ymax - triBrep.AABB.ymin - 2 * offset_room) / ygridInterval;
            double znum = (triBrep.AABB.zmax - triBrep.AABB.zmin - 2 * offset_room) / zgridInterval;

            for (int i = 0; i <= xnum; i++)
            {
                double x = triBrep.AABB.xmin + offset_room + i * xgridInterval;
                for (int j = 0; j <= ynum; j++)
                {
                    double y = triBrep.AABB.ymin + offset_room + j * ygridInterval;
                    for (int k = 0; k <= znum; k++)
                    {
                        double z = triBrep.AABB.zmin + offset_room + k * zgridInterval;
                        Ray3D ray = new Ray3D();
                        ray.StartPoint = new Point3D(x, y, z);
                        ray.Direction = new Vector3D(0, 0, 1);

                        if (GO.PointInPolyhedronTest(ray, triBrep.Triangles))
                            result.Add(ray.StartPoint);
                    }
                }
            }
            return result;
        }

        List<int> outputBIMImage(Document Doc, ElementId viewId, string output, int Width)
        {
            // output BIM image
            List<ElementId> viewIDs = new List<ElementId>();
            viewIDs.Add(viewId);
            List<string> viewNames = new List<string>();
            viewNames.Add(viewId.ToString());
            List<int> ignore = outputViewImages(Doc, viewIDs, viewNames, output, Width);
            return ignore;
        }

        List<int> outputViewImages(Document doc, IList<ElementId> views, List<string> viewNames, string output, int Width)
        {
            var tempFileName = Path.ChangeExtension(Path.GetRandomFileName(), "png");
            string tempImageFile = Path.Combine(Path.GetTempPath(), tempFileName);
            var ieo = new ImageExportOptions
            {
                FilePath = tempImageFile,
                HLRandWFViewsFileType = ImageFileType.PNG,

                FitDirection = FitDirectionType.Horizontal,

                ImageResolution = ImageResolution.DPI_72,
                ShouldCreateWebSite = false
            };

            if (views.Count > 0)
            {
                ieo.SetViewsAndSheets(views);
                ieo.ExportRange = ExportRange.SetOfViews;
            }
            else
            {
                ieo.ExportRange = ExportRange.VisibleRegionOfCurrentView;
            }

            ieo.ZoomType = ZoomFitType.FitToPage;
            // default 512 * 383; NOTE: W (*8) and H (*8.018) are not exactly enlarged with the same scalar
            // So we only specify W, and get real H from exported BIM image
            ieo.PixelSize = Width;

            //ieo.ZoomType = ZoomFitType.Zoom;
            //ieo.Zoom = 100;

            // ieo.ViewName = "tmp";

            if (ImageExportOptions.IsValidFileName(tempImageFile))
                doc.ExportImage(ieo);

            var files = Directory.GetFiles(Path.GetTempPath(), string.Format("{0}*.*", Path.GetFileNameWithoutExtension(tempFileName)));
            string fileNew = string.Empty;

            List<int> ignore = new List<int>();
            for (int i = 0; i < files.Length; i++)
            {
                string folder = output + "\\" + viewNames[i];
                Directory.CreateDirectory(folder);
                fileNew = folder + "\\" + this.oriRevitImageName;
                if (File.Exists(fileNew))
                {
                    ignore.Add(i);
                    continue;
                }
                File.Move(files[i], fileNew);
            }
            return ignore;
        }

    }

    class scene_cameraPose
    {
        public string scene;
        public CameraPose cameraPose = new CameraPose();
    }

    class CameraPose
    {
        public double X;
        public double Y;
        public double Z;
        public double Yaw;
        public double Pitch;
        public double Roll;
    }
}
