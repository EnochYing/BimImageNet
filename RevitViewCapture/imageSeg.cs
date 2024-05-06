using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.IO;
using System.Drawing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Windows.Forms;


namespace BimImageNet
{
    [Transaction(TransactionMode.Manual)]

    public class imageSeg : IExternalCommand
    {
        string oriEnscapeImageName = "Rendering_Enscape_ori.png";
        //string outputDepthData = "depth.txt";
        string outputObjInstance = "objInstance.json";
        //string outputMaterial = "material.json";
        //string outputObjPCs = "objPCs.txt";
        //string outputSemanticPCs = "semanticPCs.txt";
        //string outputMaterialPCs = "materialPCs.txt";
        //string outputObjPartPCs = "objPartPCs.txt";
        //string output_TM_GCS2PCS = "TM_GCS2PCS.json";

        utils utils = new utils();

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                imageSegmentation form = new imageSegmentation();  //using (imageCapture_form form = new imageCapture_form()): will close the form after implementing all the codes in the block 
                form.ShowDialog();
                if (form.run)
                {
                    //DateTime start_preprocess = DateTime.Now;

                    // Get target physical objects' geometry; should include other enclosure objects like "Roofs" and opening objects like "Windows"
                    List<string> targetObjs = new List<string> {"Walls", "Doors", "Windows", "Floors", "Ceilings", "Roofs"};
                    Document Doc = commandData.Application.ActiveUIDocument.Document;
                    Options geoOption = Doc.Application.Create.NewGeometryOptions();
                    if (geoOption != null)
                    {
                        geoOption.ComputeReferences = true;
                        geoOption.DetailLevel = ViewDetailLevel.Fine;
                    }
                    //List<BIMObject> BimObjs = utils.RetrivePhysicalElements(Doc, geoOption);
                    Dictionary<string, string> openingid_hostclsid = new Dictionary<string, string>();
                    List<BIMObject> BimObjs = utils.RetriveTargetElements(Doc, geoOption, targetObjs, out openingid_hostclsid);

                    //DateTime end_preprocess = DateTime.Now;
                    //double time_preprocess = end_preprocess.Subtract(start_preprocess).TotalSeconds;


                    // label valid images
                    BVH_spiltStrategy strategy = BVH_spiltStrategy.middlepoint;
                    if (!form.useMiddlePoint)
                        strategy = BVH_spiltStrategy.SAH;

                    FilteredElementCollector collector_element = new FilteredElementCollector(Doc);
                    var views = collector_element.OfClass(typeof(View3D)).ToElements();
                    Dictionary<string, int[]> spaceFolder_oToIoPTmT = new Dictionary<string, int[]>();
                    //double time_rayTracing = 0;
                    //double time_writeOutLabels = 0;
                    //double time_postprocess = 0;
                    List<List<string>> objTypes_all = new List<List<string>>();
                    List<List<string>> materialTypes_all = new List<List<string>>();
                    foreach (var view in views)
                    {
                        string[] pieces = view.Name.Split('_');
                        if (pieces.Length == 3) // valid views
                        {
                            //int tri_model;
                            //int tri_scene;
                            //double depth_vf_mm;
                            //double time_rayTracing_single;
                            //double time_writeOutLabels_single;
                            //List<string> objTypes;
                            //int objInstance;
                            //int objPartInstance;
                            //List<string> materialTypes;

                            string imageFolder = @form.output + "\\" + pieces[0] + '_' + pieces[1] + "\\" + pieces[2];
                            segBIMImage(Doc, view, BimObjs, strategy, form.distortion, imageFolder, openingid_hostclsid/*, out tri_model, out tri_scene*//*, out depth_vf_mm,*/
                                /*out time_rayTracing_single, out time_writeOutLabels_single,*/ /*out objTypes, out objInstance, out objPartInstance, out materialTypes*/);

                            //objTypes_all.Add(objTypes);
                            //materialTypes_all.Add(materialTypes);

                            //time_rayTracing += time_rayTracing_single;
                            //time_writeOutLabels += time_writeOutLabels_single;

                            // generate ready-to-use annotations by calling external application along with input
                            // !!!! process entire building images
                            DateTime start_postprocess = DateTime.Now;
                            Process proc = new Process();
                            proc.StartInfo.FileName = @form.postprocessingFunction;
                            string argvs = @form.output;
                            //foreach (var item in form.panopticSeg_semanticList)
                            //{
                            //    argvs += (',' + item);
                            //}

                            proc.StartInfo.Arguments = argvs;  // a single argumnent cannot include space; otherwise will be treated as several argumnents
                            proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden; // normal-show the window; hidden -do not show the window
                            proc.Start();
                            proc.WaitForExit();
                            //DateTime end_postprocess = DateTime.Now;
                            //double time_postprocess_single = end_postprocess.Subtract(start_postprocess).TotalSeconds;
                            //time_postprocess += time_postprocess_single;

                            //string total = (time_preprocess + time_rayTracing_single + time_writeOutLabels_single + time_postprocess_single).ToString();
                            //OutputLog(imageFolder + "\\", time_preprocess.ToString(), time_rayTracing_single.ToString(), time_writeOutLabels_single.ToString(),
                            //    total, tri_model, tri_scene, depth_vf_mm, objTypes.Count(), objInstance, objPartInstance, materialTypes.Count());

                            //string spaceFolder = pieces[0] + '_' + pieces[1];
                            //if (spaceFolder_oToIoPTmT.ContainsKey(spaceFolder))
                            //{
                            //    spaceFolder_oToIoPTmT[spaceFolder][0] += objTypes.Count;
                            //    spaceFolder_oToIoPTmT[spaceFolder][1] += objInstance;
                            //    spaceFolder_oToIoPTmT[spaceFolder][2] += objPartInstance;
                            //    spaceFolder_oToIoPTmT[spaceFolder][3] += materialTypes.Count();
                            //}
                            //else
                            //{
                            //    spaceFolder_oToIoPTmT.Add(spaceFolder, new int[] { objTypes.Count, objInstance, objPartInstance, materialTypes.Count });
                            //}
                        }
                    }

                    //// add new contents to space_level statistics 
                    //foreach (var space in spaceFolder_oToIoPTmT.Keys)
                    //{
                    //    StreamWriter Writer = new StreamWriter(@form.output + "\\" + space + "\\statistics_space.txt", true);
                    //    Writer.WriteLine("Num of object types(duplicates): " + spaceFolder_oToIoPTmT[space][0].ToString());
                    //    Writer.WriteLine("Num of object instances: " + spaceFolder_oToIoPTmT[space][1].ToString());
                    //    Writer.WriteLine("Num of object part instances: " + spaceFolder_oToIoPTmT[space][2].ToString());
                    //    Writer.WriteLine("Num of material types(duplicates): " + spaceFolder_oToIoPTmT[space][03].ToString());
                    //    Writer.Close();
                    //}

                    //int[] sta = getBuildingLevelStatistics(spaceFolder_oToIoPTmT, objTypes_all, materialTypes_all);
                    ////StreamWriter Writer2 = new StreamWriter(form.output + "\\statistics_building.txt", true);
                    ////Writer2.WriteLine("Num of object types: " + sta[0].ToString());
                    ////Writer2.WriteLine("Num of object instances: " + sta[1].ToString());
                    ////Writer2.WriteLine("Num of object part instances: " + sta[2].ToString());
                    ////Writer2.WriteLine("Num of material types: " + sta[3].ToString());
                    ////Writer2.WriteLine("    [time]_preprocessingForImageLabelling: " + time_preprocess.ToString());
                    ////Writer2.WriteLine("    [time]_rayTracing: " + time_rayTracing.ToString());
                    ////Writer2.WriteLine("    [time]_writeOutLabels: " + time_writeOutLabels.ToString());
                    ////Writer2.Close();

                    //if (File.Exists(form.output + "\\statistics_building.json")) // a building processed by batches
                    //{
                    //    JObject o1 = JObject.Parse(File.ReadAllText(form.output + "\\statistics_building.json"));
                    //    if (o1.ContainsKey("Num of object types(duplicates)"))
                    //    {
                    //        o1["Num of object types(duplicates)"] = int.Parse((string)o1["Num of object types(duplicates)"]) + sta[0];
                    //        o1["Num of object instances"] = int.Parse((string)o1["Num of object instances"]) + sta[1];
                    //        o1["Num of object part instances"] = int.Parse((string)o1["Num of object part instances"]) + sta[2];
                    //        o1["Num of material types(duplicates)"] = int.Parse((string)o1["Num of material types(duplicates)"]) + sta[3];
                    //        o1["[Time]_preprocessingForImageLabelling"] = double.Parse((string)o1["[Time]_preprocessingForImageLabelling"]) + time_preprocess;
                    //        o1["[Time]_rayTracing"] = double.Parse((string)o1["[Time]_rayTracing"]) + time_rayTracing;
                    //        o1["[Time]_writeOutLabels"] = double.Parse((string)o1["[Time]_writeOutLabels"]) + time_writeOutLabels;
                    //        o1["[Time]_postprocessingForImageLabelling"] = double.Parse((string)o1["[Time]_postprocessingForImageLabelling"]) + time_postprocess;
                    //    }
                    //    else
                    //    {
                    //        o1.Add("Num of object types(duplicates)", sta[0]);
                    //        o1.Add("Num of object instances", sta[1]);
                    //        o1.Add("Num of object part instances", sta[2]);
                    //        o1.Add("Num of material types(duplicates)", sta[3]);
                    //        o1.Add("[Time]_preprocessingForImageLabelling", time_preprocess);
                    //        o1.Add("[Time]_rayTracing", time_rayTracing);
                    //        o1.Add("[Time]_writeOutLabels", time_writeOutLabels);
                    //        o1.Add("[Time]_postprocessingForImageLabelling", time_postprocess);
                    //    }
                    //    o1["[Time]_total"] = double.Parse((string)o1["[Time]_total"]) + time_preprocess + time_rayTracing + time_writeOutLabels + time_preprocess + time_postprocess;
                    //    File.WriteAllText(form.output + "\\statistics_building.json", o1.ToString());
                    //}
                    TaskDialog.Show("Processing Status", "Image Segmentation Completed!");
                }

                #region Get yaw, pitch, and roll of an existing 3D view
                //if (form.run)
                //{
                //    // get orientation of existing view
                //    Document Doc = commandData.Application.ActiveUIDocument.Document;
                //    FilteredElementCollector collector_element = new FilteredElementCollector(Doc);
                //    var views = collector_element.OfClass(typeof(View3D)).ToElements();
                //    foreach (var view in views)
                //    {
                //        if (view.Name == "Equipment_Enscape_1024")
                //        {
                //            View3D view_ = view as View3D;
                //            var viewOrientation3D = view_.GetOrientation();
                //            Vector3D forward_z_GCS = XYZToVector(viewOrientation3D.ForwardDirection);
                //            forward_z_GCS = new Vector3D().Multiple(forward_z_GCS, -1);
                //            Vector3D upDir_y_GCS = XYZToVector(viewOrientation3D.UpDirection);
                //            Vector3D rightDir_x_GCS = XYZToVector(viewOrientation3D.UpDirection.CrossProduct(viewOrientation3D.ForwardDirection));
                //            Point3D origin_GCS = XYZToPoint(viewOrientation3D.EyePosition);

                //            // "Dining_3DView4_1024"
                //            // forward_z_GCS = {(-0.964846854, -0.262812762, 0.000000000)} 
                //            // upDir_y_GCS = {(0.000000000, 0.000000000, 1.000000000)}
                //            // rightDir_x_GCS = 0.26281276249762792, -0.96484685410088034, 0;
                //            // origin_GCS = {(20.079158899, 6.799038453, 5.741469816)}
                //            // yaw = 285.2370271879662, pitch = 0, roll = 90

                //            // "Equipment_Enscape_1024"
                //            // forward_z_GCS = {(-0.974603948, 0.176571342, -0.137730554)}
                //            // upDir_y_GCS = {(-0.135524325, 0.024553268, 0.990469734)}
                //            // rightDir_x_GCS = 0.17827030547287237,0.9839815537837121, 0
                //            // origin_GCS = {(45.281337738, 29.323972702, -53.530841827)}
                //            // yaw, pitch, roll = -100.26902616448201, 3.989433348053318e-10, 97.91654440786506

                //            int k = 1;
                //        }
                //    }

                //    // get yaw, pitch, and roll based on python function
                //}
                #endregion
                return Result.Succeeded;
            }
            catch (Exception e)
            {
                TaskDialog.Show("Revit", e.Message);
                return Result.Failed;
            }
        }


        Vector3D XYZToVector(XYZ xyz)
        {
            return new Vector3D(xyz.X, xyz.Y, xyz.Z);
        }
        Point3D XYZToPoint(XYZ xyz)
        {
            return new Point3D(xyz.X, xyz.Y, xyz.Z);
        }

        int[] getBuildingLevelStatistics(Dictionary<string, int[]> spaceFolder_oToIoPTmT, List<List<string>> objTypes, List<List<string>> materialTypes)
        {
            int[] result = new int[4];

            List<string> objTypes_set = new List<string>();
            List<string> materialTypes_set = new List<string>();
            foreach (var set in objTypes)
            {
                foreach (var e in set)
                {
                    if (!objTypes_set.Contains(e))
                        objTypes_set.Add(e);
                }
            }
            foreach (var set in materialTypes)
            {
                foreach (var e in set)
                {
                    if (!materialTypes_set.Contains(e))
                        materialTypes_set.Add(e);
                }
            }

            int objInstances = 0;
            int objPartInstances = 0;
            foreach (var set in spaceFolder_oToIoPTmT.Values)
            {
                objInstances += set[1];
                objPartInstances += set[2];
            }

            result[0] = objTypes_set.Count;
            result[1] = objInstances;
            result[2] = objPartInstances;
            result[3] = materialTypes_set.Count;
            return result;
        }


        // NOTE: already output images will be skipped 
        void segBIMImage(Document Doc, Element view, List<BIMObject> BimObjs, BVH_spiltStrategy strategy, double[] distortion, 
            string imageFolder, Dictionary<string, string> openingid_hostclsid
            /*, out int tri_model, out int tri_scene*/ /*, out double depth_vf_mm,*//* out double time_rayTracing, out double time_outputResult, */
            /*out List<string> objTypes, out int objInstanace, out int objPartInstance, out List<string> materialTypes*/)
        {
            //string[] files = Directory.GetFiles(imageFolder);
            //string extension = Path.GetExtension(files[i]).ToUpperInvariant();  // .PNG
            // if (Path.GetFileName(files[i]) == this.oriEnscapeImageName)

            //string extension = Path.GetExtension(files[i]).ToUpperInvariant();  // .PNG  
            //DateTime start_process = DateTime.Now;

            string imagefile = imageFolder + "\\" + this.oriEnscapeImageName;
            BIMImage bimImage = new BIMImage();
            Image img = Image.FromFile(imagefile);
            bimImage.name = Path.GetFileName(imagefile);
            bimImage.w_pixel = img.Width;
            bimImage.h_pixel = img.Height;
            bimImage.hori_resolution = img.HorizontalResolution;
            bimImage.vert_resolution = img.VerticalResolution;
            bimImage.pixelSize = Image.GetPixelFormatSize(img.PixelFormat);
            bimImage.size = (int)new FileInfo(imagefile).Length;

            camera ca = new camera(Doc, view, img.Width);
            //depth_vf_mm = ca.vf.depth_mm;

            //imageSegmentation_bruteforce_AABB imaSeg = new imageSegmentation_bruteforce_AABB();
            imageSegmentation_2TierBVHTree imaSeg = new imageSegmentation_2TierBVHTree();  // fastest
            //imageSegmentation_1TierBVHTree_obj imaSeg = new imageSegmentation_1TierBVHTree_obj();
            // imageSegmentation_1TierBVHTree_tri imaSeg = new imageSegmentation_1TierBVHTree_tri(); // slowest
            //labelledImage seg = imaSeg.Segmentation(BimObjs, ca, bimImage, strategy);

            labelledImage seg = imaSeg.Segmentation_distort(BimObjs, ca, bimImage, strategy, /*imagefile,*/ distortion, openingid_hostclsid);

            //DateTime end_process = DateTime.Now;
            //time_rayTracing = end_process.Subtract(start_process).TotalSeconds;

            //tri_model = seg.triNo_model;
            //tri_scene = seg.triNo_image;

            //objTypes = seg.semanticPCs.Keys.ToList();
            //objPartInstance = seg.objPartPCs.Keys.Count;
            //objInstanace = seg.objPCs.Keys.Count;
            //materialTypes = seg.materialPCs.Keys.ToList(); 

            //start_process = DateTime.Now;
            OutputSegmetations(seg, imageFolder + "\\");  // annotations + depths
            //end_process = DateTime.Now;
            //time_outputResult = end_process.Subtract(start_process).TotalSeconds;
        }

        void OutputLog(string path, string time_preprocessing, string time_rayTracing, string time_resultOutput, string total, int tri_model, int tri_scene, 
            double depth_vf_mm, int objType, int objInstanace, int objPartInstance, int meterialType)
        {
            string fullName = path + "statistics_image.txt";
            StreamWriter Writer = new StreamWriter(fullName, true);
            Writer.WriteLine("[time]_Total: " + total);
            Writer.WriteLine("   [time]_preprocessingForLabeling: " + time_preprocessing);
            Writer.WriteLine("   [time]_rayTracing: " + time_rayTracing);
            Writer.WriteLine("   [time]_writeOutLabels: " + time_resultOutput);       
            Writer.WriteLine("Num of triangles in the model: " + tri_model.ToString());
            Writer.WriteLine("Num of triangles in the VF: " + tri_scene.ToString());
            Writer.WriteLine("depth of VF: " + depth_vf_mm.ToString());
            Writer.WriteLine("Num of object types: " + objType.ToString());
            Writer.WriteLine("Num of object instances: " + objInstanace.ToString());
            Writer.WriteLine("Num of object part instances: " + objPartInstance.ToString());
            Writer.WriteLine("Num of material types: " + meterialType.ToString());
            Writer.Close();
        }

        void OutputSegmetations(labelledImage seg, string folderPath)
        {
            //string fullName_depth = folderPath + outputDepthData;
            //StreamWriter depthWriter = new StreamWriter(fullName_depth, true);

            int rows = seg.objInstances.GetLength(0);
            int columns = seg.objInstances.GetLength(1);
            //for (int i = 0; i < rows; i++)
            //{
            //    string s1 = null;
            //    for (int j = 0; j < columns; j++)
            //    {
            //        // 1000 m; in case ray does not detect the intersection
            //        if (seg.depths[i, j] > 1000000)
            //            seg.depths[i, j] = 1000000;
            //        s1 += seg.depths[i, j].ToString() + ' ';
            //    }
            //    depthWriter.WriteLine(s1);
            //}
            //depthWriter.Close();

            //string fullName_PC = folderPath + outputObjPCs;
            //StreamWriter PCWriter = new StreamWriter(fullName_PC, true);
            //Random ran = new Random(0);

            //foreach (var item in seg.objPCs)
            //{
            //    int R = ran.Next(0, 255);
            //    int G = ran.Next(0, 255);
            //    int B = ran.Next(0, 255);

            //    foreach (var p in item.Value)
            //    {
            //        string line = item.Key + ' ' + p.x.ToString() + ' ' + p.y.ToString() + ' ' + p.z.ToString() + ' '
            //            + R.ToString() + ' ' + G.ToString() + ' ' + B.ToString() + ' ';
            //        PCWriter.WriteLine(line);
            //    }
            //}
            //PCWriter.Close();

            //string fullName_semPC = folderPath + outputSemanticPCs;
            //StreamWriter semPCWriter = new StreamWriter(fullName_semPC, true);
            //Random ran1 = new Random(0);

            //foreach (var item in seg.semanticPCs)
            //{
            //    int R = ran1.Next(0, 255);
            //    int G = ran1.Next(0, 255);
            //    int B = ran1.Next(0, 255);

            //    foreach (var p in item.Value)
            //    {
            //        string line = item.Key + ' ' + p.x.ToString() + ' ' + p.y.ToString() + ' ' + p.z.ToString() + ' '
            //            + R.ToString() + ' ' + G.ToString() + ' ' + B.ToString() + ' ';
            //        semPCWriter.WriteLine(line);
            //    }
            //}
            //semPCWriter.Close();

            //string fullName_malPC = folderPath + outputMaterialPCs;
            //StreamWriter malPCWriter = new StreamWriter(fullName_malPC, true);
            //Random ran3 = new Random(0);

            //foreach (var item in seg.materialPCs)
            //{
            //    int R = ran3.Next(0, 255);
            //    int G = ran3.Next(0, 255);
            //    int B = ran3.Next(0, 255);

            //    foreach (var p in item.Value)
            //    {
            //        string line = item.Key + ' ' + p.x.ToString() + ' ' + p.y.ToString() + ' ' + p.z.ToString() + ' '
            //            + R.ToString() + ' ' + G.ToString() + ' ' + B.ToString() + ' ';
            //        malPCWriter.WriteLine(line);
            //    }
            //}
            //malPCWriter.Close();

            //string fullName_objPartPC = folderPath + outputObjPartPCs;
            //StreamWriter PCWriter2 = new StreamWriter(fullName_objPartPC, true);
            //Random ran2 = new Random(0);

            //foreach (var item in seg.objPartPCs)
            //{
            //    int R = ran2.Next(0, 255);
            //    int G = ran2.Next(0, 255);
            //    int B = ran2.Next(0, 255);

            //    foreach (var p in item.Value)
            //    {
            //        string line = item.Key + ' ' + p.x.ToString() + ' ' + p.y.ToString() + ' ' + p.z.ToString() + ' '
            //            + R.ToString() + ' ' + G.ToString() + ' ' + B.ToString() + ' ';
            //        PCWriter2.WriteLine(line);
            //    }
            //}
            //PCWriter2.Close();

            //TM_GCS2PCS tm = new TM_GCS2PCS();
            //tm.TM_GCS2VCS = seg.TM_GCS_to_VCS;
            //tm.TM_VCS2PCS = seg.TM_3DVCS_to_2DPCS;
            //tm.focus = seg.focus;
            //File.WriteAllText(folderPath + output_TM_GCS2PCS, JsonConvert.SerializeObject(tm));

            imageAnnotate ia = new imageAnnotate();
            ia.shape = new int[2];
            ia.shape[0] = rows;
            ia.shape[1] = columns;
            ia.annotations = new string[rows, columns];
            ia.annotations = seg.objInstances;          
            Dictionary<string, imageAnnotate> annotate = new Dictionary<string, imageAnnotate>();
            annotate.Add(seg.name, ia);
            File.WriteAllText(folderPath + outputObjInstance, JsonConvert.SerializeObject(annotate));

            //imageAnnotate ia_mal = new imageAnnotate();
            //ia_mal.shape = new int[2];
            //ia_mal.shape[0] = rows;
            //ia_mal.shape[1] = columns;
            //ia_mal.annotations = new string[rows, columns];
            //ia_mal.annotations = seg.materials;
            //Dictionary<string, imageAnnotate> annotate_mal = new Dictionary<string, imageAnnotate>();
            //annotate_mal.Add(seg.name, ia_mal);
            //File.WriteAllText(folderPath + outputMaterial, JsonConvert.SerializeObject(annotate_mal));
        }
    }
}
