using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using ClipperLib_WithoutStatic;

namespace BimImageNet
{
    using clipperPath = List<IntPoint>;
    using clipperPaths = List<List<IntPoint>>;

    [Transaction(TransactionMode.Manual)]

    class utils
    {     
        double offset_objAABB = 0.328;   // (f) 100 mm
        double scale_point_to_IntPoint = 1000000;

        GeometricOperations GO = new GeometricOperations();
        public utils()
        {

        }

        public List<BIMObject> RetriveTargetElements(Document Doc, Options geoOption, List<string> targetObjs, out Dictionary<string, string> openingid_hostclsid)
        {
            List<BIMObject> results = new List<BIMObject>();

            List<Element> elements = new List<Element>();
            FilteredElementCollector collector = new FilteredElementCollector(Doc).WhereElementIsNotElementType();
            foreach (Element e in collector)
            {
                if (e.Category != null && targetObjs.Contains(e.Category.Name))
                    elements.Add(e);
            }

            openingid_hostclsid = new Dictionary<string, string>();
            for (int i = 0; i < elements.Count; i++)
            {
                BIMObject bimObj = new BIMObject();
                bimObj.GUID = elements[i].Id.ToString();
                bimObj.Name = elements[i].Name.Replace(' ', '-');
                bimObj.Name = bimObj.Name.Replace('/', '-');
                bimObj.Category = elements[i].Category.Name.Replace(' ', '-');
                bimObj.Category = bimObj.Category.Replace('/', '-');

                //if (elements[i].LookupParameter("Mark") != null)
                //    bimObj.Mark = elements[i].LookupParameter("Mark").AsString();
                //bimObj.objectType = getObjectType(bimObj.Category, bimObj.Name, bimObj.Mark, objTypes);

                //bimObj.objectType = bimObj.Category + '$' + bimObj.Name;
                foreach (var cls in targetObjs)
                {
                    if (bimObj.Category.Contains(cls))
                    {
                        bimObj.objectType = cls;
                        break;
                    }
                }

                HostObject ele = elements[i] as HostObject;
                if (ele != null)
                {
                    IList<ElementId> inserts = ele.FindInserts(true, true, true, true);
                    if (inserts.Count > 0)
                    {
                        foreach(var opening in inserts)
                        {    
                            if (!openingid_hostclsid.ContainsKey(opening.ToString()))   // !!! NOTE: there may be one opening intersting in multiple host objects
                                openingid_hostclsid.Add(opening.ToString(), bimObj.objectType + '_' + bimObj.GUID);
                        }
                                              
                    }
                }
 
                GeometryElement geoElement = elements[i].get_Geometry(geoOption);
                List<Solid> Solids = new List<Solid>();
                if (geoElement != null)
                {
                    #region extract pure wall geometry without openings
                    //// for a wall with openings, delete all openings before retrieving wall geometry
                    //bool obtainedSolid = false;
                    //HostObject ele = elements[i] as HostObject;
                    //if (ele != null)
                    //{
                    //    IList<ElementId> inserts = ele.FindInserts(true, true, true, true);
                    //    if (inserts.Count > 0)
                    //    {
                    //        using (Transaction t = new Transaction(Doc, "extract original wall geometry"))
                    //        {
                    //            if (t.Start() == TransactionStatus.Started)
                    //            {
                    //                Doc.Delete(inserts);
                    //                Doc.Regenerate();  // update documents based on the changes; unlike t.commit(), which permantly confirms the changes,
                    //                                   // changes made by this function can be cancelled by t.RollBack()
                    //                GeometryElement geoElement1 = elements[i].get_Geometry(geoOption);
                    //                AddSolids(Doc, geoElement1, ref Solids);
                    //                ParseSolid(Doc, Solids, ref bimObj);
                    //                obtainedSolid = true;
                    //                t.RollBack();   // Note teh RollBack() function will affect AddSolids() results;
                    //                                // thus, the ParseSolid function is used to extract all the data first                     
                    //            }
                    //        }
                    //    }
                    //}

                    //if (!obtainedSolid)
                    //{
                    //    AddSolids(Doc, geoElement, ref Solids);
                    //    ParseSolid(Doc, Solids, ref bimObj);
                    //}
                    #endregion

                    AddSolids(Doc, geoElement, ref Solids);
                    ParseSolid(Doc, Solids, ref bimObj);
                    if (bimObj.triBrep.Triangles.Count >= 12)
                        results.Add(bimObj);
                }
            }
            return results;
        }

        public void ParseSolid(Document Doc, List<Solid> Solids, ref BIMObject bimObj)
        {
            int index = 0;
            foreach (Solid so in Solids)
            {
                if (so.Faces.Size != 0)
                {
                    List<Triangle3D> solidMesh = new List<Triangle3D>();
                    index += 1;
                    string part = "p" + index.ToString();
                    foreach (Face fa in so.Faces)
                    {
                        Mesh mesh = fa.Triangulate();
                        List<Triangle3D> face = formatTransf_meshFace(mesh);
                        EnrichPartNO(ref face, part);

                        string malName = null;
                        Material mal = Doc.GetElement(fa.MaterialElementId) as Material;
                        if (mal != null)
                            malName = mal.Name;
                        EnrichSurfaceMaterial(ref face, malName);

                        solidMesh.AddRange(face);

                        bimObj.triBrep.TriangleFaces.Add(face);
                        bimObj.triBrep.Triangles.AddRange(face);
                    }
                    bimObj.AABBS_objParts.Add(GO.BoundingBox3D_Triangles_Create(solidMesh, offset_objAABB));
                }
            }
        }


        public List<BIMObject> RetrivePhysicalElements(Document Doc, Options geoOption)
        {
            List<BIMObject> results = new List<BIMObject>();

            List<Element> elements = new List<Element>();
            FilteredElementCollector collector = new FilteredElementCollector(Doc).WhereElementIsNotElementType();
            foreach (Element e in collector)
            {
                if (e.Category != null &&
                    e.Category.Name != "Rooms" &&
                    e.Category.Name != "Spaces" &&
                    !e.Category.Name.Contains("Analytical") &&
                    !e.Category.Name.Contains("Mass") &&
                    !e.Category.Name.Contains("Detail") &&
                    !e.Category.Name.Contains("Legend") &&
                    !e.Category.Name.Contains("Boundary") &&
                    !e.Category.Name.Contains("Guide") &&
                    !e.Category.Name.Contains(".dwg")/*&& e.Category.HasMaterialQuantities*/) // Note: Some physical objects like MEP objects may not have materials
                    elements.Add(e);
            }

            List<string> categories = new List<string>();
            for (int i = 0; i < elements.Count; i++)
            {
                BIMObject bimObj = new BIMObject();
                bimObj.GUID = elements[i].Id.ToString();
                bimObj.Name = elements[i].Name.Replace(' ', '-');
                bimObj.Name = bimObj.Name.Replace('/', '-');
                bimObj.Category = elements[i].Category.Name.Replace(' ', '-');
                bimObj.Category = bimObj.Category.Replace('/', '-');

                //if (elements[i].LookupParameter("Mark") != null)
                //    bimObj.Mark = elements[i].LookupParameter("Mark").AsString();
                //bimObj.objectType = getObjectType(bimObj.Category, bimObj.Name, bimObj.Mark, objTypes); 

                bimObj.objectType = bimObj.Category + '$' + bimObj.Name;

                GeometryElement geoElement = elements[i].get_Geometry(geoOption);
                List<Solid> Solids = new List<Solid>();
                if (geoElement != null)
                {
                    AddSolids(Doc, geoElement, ref Solids);

                    int index = 0;
                    foreach (Solid so in Solids)
                    {
                        if (so.Faces.Size != 0)
                        {
                            List<Triangle3D> solidMesh = new List<Triangle3D>();
                            index += 1;
                            string part = "p" + index.ToString();
                            foreach (Face fa in so.Faces)
                            {
                                Mesh mesh = fa.Triangulate();
                                List<Triangle3D> face = formatTransf_meshFace(mesh);
                                EnrichPartNO(ref face, part);

                                string malName = null;
                                Material mal = Doc.GetElement(fa.MaterialElementId) as Material;
                                if (mal != null)
                                    malName = mal.Name;
                                EnrichSurfaceMaterial(ref face, malName);

                                solidMesh.AddRange(face);

                                bimObj.triBrep.TriangleFaces.Add(face);
                                bimObj.triBrep.Triangles.AddRange(face);
                            }
                            bimObj.AABBS_objParts.Add(GO.BoundingBox3D_Triangles_Create(solidMesh, offset_objAABB));
                        }
                    }
                    if (bimObj.triBrep.Triangles.Count >= 12)
                    {
                        categories.Add(bimObj.Category);
                        results.Add(bimObj);
                    }
                }
            }
            return results;
        }

        public List<RoomObject> RetriveRooms(Document Doc)
        {
            List<RoomObject> result = new List<RoomObject>();

            FilteredElementCollector collector = new FilteredElementCollector(Doc);
            RoomFilter roomfilter = new RoomFilter();
            FilteredElementCollector rooms = collector.WherePasses(roomfilter);
            SpatialElementGeometryCalculator calculator = new SpatialElementGeometryCalculator(Doc);

            for (int iroom = 0; iroom < rooms.GetElementCount(); iroom++)
            {
                Room r = rooms.ElementAtOrDefault(iroom) as Room;
                if (r != null)
                {
                    RoomObject robj = new RoomObject();
                    robj.GUID = r.Id.ToString();
                    robj.Name = r.Name;
                    SpatialElementGeometryResults rGeometry = calculator.CalculateSpatialElementGeometry(r);
                    Solid rSolid = rGeometry.GetGeometry();

                    if (rSolid.Faces.Size != 0)
                    {
                        foreach (Face fa in rSolid.Faces)
                        {
                            Mesh mesh = fa.Triangulate();
                            List<Triangle3D> face = formatTransf_meshFace(mesh);
                            robj.triBrep.TriangleFaces.Add(face);
                            robj.triBrep.Triangles.AddRange(face);
                            robj.triBrep.AABB = GO.BoundingBox3D_Triangles_Create(robj.triBrep.Triangles, 0);
                        }
                        result.Add(robj);
                    }
                }
            }
            return result;
        }

        public List<RoomObject> RetriveRooms(Document Doc, List<string> spaceIds)
        {
            List<RoomObject> result = new List<RoomObject>();

            SpatialElementGeometryCalculator calculator = new SpatialElementGeometryCalculator(Doc);
            for (int i = 0; i < spaceIds.Count(); i++)
            {
                Room r = Doc.GetElement(ElementId.Parse(spaceIds[i])) as Room;
                if (r != null)
                {
                    RoomObject robj = new RoomObject();
                    robj.GUID = r.Id.ToString();
                    robj.Name = r.Name;
                    SpatialElementGeometryResults rGeometry = calculator.CalculateSpatialElementGeometry(r);
                    Solid rSolid = rGeometry.GetGeometry();

                    if (rSolid.Faces.Size != 0)
                    {
                        foreach (Face fa in rSolid.Faces)
                        {
                            Mesh mesh = fa.Triangulate();
                            List<Triangle3D> face = formatTransf_meshFace(mesh);
                            robj.triBrep.TriangleFaces.Add(face);
                            robj.triBrep.Triangles.AddRange(face);
                            robj.triBrep.AABB = GO.BoundingBox3D_Triangles_Create(robj.triBrep.Triangles, 0);
                        }
                        result.Add(robj);
                    }
                }
            }
            return result;
        }

        public void EnrichSurfaceNormal(ref List<BIMObject> objs)
        {
            GeometricOperations GO = new GeometricOperations();
            foreach (var obj in objs)
            {
                obj.triBrep.Triangles = new List<Triangle3D>();
                foreach (var face in obj.triBrep.TriangleFaces)
                {
                    foreach (var tri in face)
                    {
                        Vector3D n = GO.CalculateSurfaceNormal(GO.formTransf(tri));
                        Point3D centre = new Point3D();
                        centre.x = (tri.Vertex1.x + tri.Vertex2.x + tri.Vertex3.x) / 3.0;
                        centre.y = (tri.Vertex1.y + tri.Vertex2.y + tri.Vertex3.y) / 3.0;
                        centre.z = (tri.Vertex1.z + tri.Vertex2.z + tri.Vertex3.z) / 3.0;

                        centre = GO.OffsetPoint(centre, 20.0 / 304.8, n);
                        Ray3D ray = new Ray3D();
                        ray.StartPoint = centre;
                        ray.Direction = n;
                        if (GO.PointInPolyhedronTest(ray, obj.triBrep.Triangles))
                            n = n.Multiple(n, -1);
                        tri.NormalVector = n;
                    }
                    obj.triBrep.Triangles.AddRange(face);
                }
            }
        }
        string getObjectType(string Category, string Name, string Mark, List<string> objTypes)
        {
            if (Name.Contains("Curtain Wall"))
                return "Curtain-Wall";
            if (Category.Contains("Curtain") && Category.Contains("Mullion"))
                return "Curtain-Wall-Mullion";
            if (Category.Contains("Curtain") && Category.Contains("Panel"))
                return "Curtain-Wall-Panel";
            foreach (var type in objTypes)
            {
                string newType = type.Replace('-', ' ');
                if (Category.Contains(newType) || Name.Contains(newType))
                    return type;

                if (Mark != null)
                    if (Mark.Contains(newType))
                        return type;
            }

            return "Other";
        }


        XYZ computeCentorid(MeshTriangle triangle)
        {
            XYZ vertex1 = triangle.get_Vertex(0);
            XYZ vertex2 = triangle.get_Vertex(1);
            XYZ vertex3 = triangle.get_Vertex(2);

            XYZ centorid = new XYZ((vertex1.X + vertex2.X + vertex3.X) / 3,
                (vertex1.Y + vertex2.Y + vertex3.Y) / 3,
                (vertex1.Z + vertex2.Z + vertex3.Z) / 3);

            return centorid;
        }

        // ???? Assume the planar face does not have a hole
        Polyline3D getPolygon(PlanarFace pf)
        {
            Polyline3D polygon = new Polyline3D();

            EdgeArray loop = pf.EdgeLoops.get_Item(0);
            foreach (Edge edge in loop)
            {
                IList<XYZ> edgePts = edge.Tessellate();
                int n = edgePts.Count();
                for (int i = 0; i < n - 1; ++i)
                {
                    XYZ p = edgePts[i];
                    polygon.Vertices.Add(new Point3D(p.X, p.Y, p.Z));
                }
            }

            return polygon;
        }

        // get a horizontal vector perpendicular to input vector
        XYZ getHorizontalVector(XYZ vector)
        {
            XYZ result = new XYZ();

            if (vector.X == 0 && vector.Y == 0)
                result = new XYZ(1, 0, 0);
            else
            {
                double y = Math.Sqrt(vector.X * vector.X / (vector.X * vector.X + vector.Y * vector.Y));
                double x;
                if (vector.X == 0)
                    x = 1;
                else
                    x = -1 * y * vector.Y / vector.X;
                result = new XYZ(x, y, 0);
            }
            return result;
        }

        Polyline3D CreatePolyline_Round(clipperPath path, double Z)
        {
            Polyline3D polyline = new Polyline3D();

            for (int i = 0; i < path.Count; i++)
            {
                Point3D p = new Point3D();
                p.x = path[i].X / scale_point_to_IntPoint;
                p.y = path[i].Y / scale_point_to_IntPoint;
                p.z = Z;
                polyline.Vertices.Add(p);
            }
            return polyline;
        }

        Polyline2D CreatePolyline_Round(clipperPath path)
        {
            Polyline2D polyline = new Polyline2D();

            for (int i = 0; i < path.Count; i++)
            {
                Point2D p = new Point2D();
                p.x = path[i].X / scale_point_to_IntPoint;
                p.y = path[i].Y / scale_point_to_IntPoint;
                polyline.Vertices.Add(p);
            }
            return polyline;
        }

        // Create path from projected polyline 3D
        clipperPath CreatePath_Round(Polyline3D polyline)
        {
            clipperPath p = new clipperPath();
            for (int i = 0; i < polyline.Vertices.Count; i++)
            {
                double X = polyline.Vertices[i].x * scale_point_to_IntPoint;
                double Y = polyline.Vertices[i].y * scale_point_to_IntPoint;

                p.Add(new IntPoint((long)X, (long)Y));
            }
            return p;
        }

        // Create path from projected polyline 3D
        clipperPaths CreatePath_Round(List<Polyline3D> polylines)
        {
            clipperPaths result = new clipperPaths();

            foreach (var poly in polylines)
                result.Add(CreatePath_Round(poly));

            return result;
        }



        void AddSolids(Document Doc, GeometryElement geoElement, ref List<Solid> solids)
        {
            foreach (GeometryObject go in geoElement)
            {
                Solid solid = go as Solid;
                if (null != solid)
                {
                    GraphicsStyle graphicsStyle = Doc.GetElement(solid.GraphicsStyleId) as GraphicsStyle;
                    if (graphicsStyle != null)
                    {
                        // light sources geometry
                        if (graphicsStyle.GraphicsStyleCategory.Id.IntegerValue == (int)BuiltInCategory.OST_LightingFixtureSource)
                            continue;
                    }
                    solids.Add(solid);
                    continue;
                }

                //If this GeometryObject is Instance, call AddCurvesAndSolids
                GeometryInstance geomInst = go as GeometryInstance;
                if (null != geomInst)
                {
                    GeometryElement transformedGeomElem = geomInst.GetInstanceGeometry();
                    //GeometryElement transformedGeomElem = geomInst.GetInstanceGeometry(geomInst.Transform);  this will transform family instance from WCS to family local CS
                    AddSolids(Doc, transformedGeomElem, ref solids);
                }
            }
        }


        void EnrichPartNO(ref List<Triangle3D> face, string part)
        {
            foreach (var tri in face)
                tri.partNo = part;
        }

        void EnrichSurfaceMaterial(ref List<Triangle3D> face, string malName)
        {
            foreach (var tri in face)
                tri.materialName = malName;
        }

        List<Triangle3D> formatTransf_meshFace(Mesh mesh)
        {
            List<Triangle3D> result = new List<Triangle3D>();
            for (int i = 0; i < mesh.NumTriangles; i++)
            {
                MeshTriangle triangle = mesh.get_Triangle(i);
                result.Add(formatTransf_meshTriangle(triangle));
            }
            return result;
        }

        Triangle3D formatTransf_meshTriangle(MeshTriangle triangle)
        {
            Triangle3D result = new Triangle3D();
            result.Vertex1 = new Point3D(triangle.get_Vertex(0).X, triangle.get_Vertex(0).Y, triangle.get_Vertex(0).Z);
            result.Vertex2 = new Point3D(triangle.get_Vertex(1).X, triangle.get_Vertex(1).Y, triangle.get_Vertex(1).Z);
            result.Vertex3 = new Point3D(triangle.get_Vertex(2).X, triangle.get_Vertex(2).Y, triangle.get_Vertex(2).Z);
            XYZ n = getTriangleNormal(triangle);
            result.NormalVector = new Vector3D(n.X, n.Y, n.Z);
            return result;
        }

        XYZ getTriangleNormal(MeshTriangle tri)
        {
            XYZ pt0 = tri.get_Vertex(0);
            XYZ pt1 = tri.get_Vertex(1);
            XYZ pt2 = tri.get_Vertex(2);
            XYZ vec1 = pt1 - pt0;
            XYZ vec2 = pt2 - pt0;
            XYZ normal = vec1.CrossProduct(vec2);

            return normal;
        }


    }



    class MyExportContext : IExportContext
    {
        public CameraLocalInfo CameraLocalInfo { get; private set; }

        public bool Start()
        {
            return true;
        }

        public void Finish()
        {

        }

        public bool IsCanceled()
        {
            return false;
        }

        public RenderNodeAction OnViewBegin(ViewNode node)
        {
            var cameraInfo = node.GetCameraInfo();
            CameraLocalInfo = new CameraLocalInfo
            {
                LookAtDistance = cameraInfo.TargetDistance,
                DistanceToFarPlane = cameraInfo.FarDistance,
                DistanceToNearPlane = cameraInfo.NearDistance,
                HorizontalExtent = cameraInfo.HorizontalExtent,
                VerticalExtent = cameraInfo.VerticalExtent,
                IsPerspective = cameraInfo.IsPerspective
            };

            return RenderNodeAction.Proceed;
        }

        public void OnViewEnd(ElementId elementId)
        {

        }

        public RenderNodeAction OnElementBegin(ElementId elementId)
        {
            return RenderNodeAction.Skip;
        }

        public void OnElementEnd(ElementId elementId)
        {

        }

        public RenderNodeAction OnInstanceBegin(InstanceNode node)
        {
            return RenderNodeAction.Skip;
        }

        public void OnInstanceEnd(InstanceNode node)
        {

        }

        public RenderNodeAction OnLinkBegin(LinkNode node)
        {
            return RenderNodeAction.Skip;
        }

        public void OnLinkEnd(LinkNode node)
        {

        }

        public RenderNodeAction OnFaceBegin(FaceNode node)
        {
            return RenderNodeAction.Skip;
        }

        public void OnFaceEnd(FaceNode node)
        {

        }

        public void OnRPC(RPCNode node)
        {

        }

        public void OnLight(LightNode node)
        {

        }

        public void OnMaterial(MaterialNode node)
        {

        }

        public void OnPolymesh(PolymeshTopology node)
        {

        }
    }

    class CameraLocalInfo   // view coordinate system
    {
        public double LookAtDistance { get; set; }
        public double DistanceToNearPlane { get; set; }
        public double DistanceToFarPlane { get; set; }
        public double HorizontalExtent { get; set; }
        public double VerticalExtent { get; set; }
        public double HorizontalFov => 2 * Math.Atan2(HorizontalExtent / 2, LookAtDistance);
        public double VerticalFov => 2 * Math.Atan2(VerticalExtent / 2, LookAtDistance);
        public double AspectRatio => HorizontalExtent / VerticalExtent;
        public bool IsPerspective { get; set; }
    }

    class imageSegmentation_bruteforce_AABB
    {
        public labelledImage Segmentation(List<BIMObject> BIMObjs, camera ca, BIMImage ima)
        {
            GeometricOperations GO = new GeometricOperations();
            labelledImage image = new labelledImage();
            image.name = ima.name.Substring(0, ima.name.Length - 4);  // remove .png

            // 1. Find BIM objects in view frustum
            double[,] TM_VCS_GCS = GO.Affine_Matrix(ca.origin_GCS, ca.rightDir_x_GCS, ca.upDir_y_GCS, ca.forward_z_GCS);
            double[,] TM_GCS_VCS = GO.IMatrix_Matrix(TM_VCS_GCS);
            image.TM_GCS_to_VCS = TM_GCS_VCS;

            image.TM_3DVCS_to_2DPCS = new double[3, 3];
            double f = ca.vf.nearPlane_VCS.Vertices[0].z;
            image.TM_3DVCS_to_2DPCS[0, 0] = -1 * ima.w_pixel / ca.vf.w_projPlane;
            image.TM_3DVCS_to_2DPCS[0, 1] = 0;
            image.TM_3DVCS_to_2DPCS[0, 2] = ima.w_pixel / 2.0;
            image.TM_3DVCS_to_2DPCS[1, 0] = 0;
            image.TM_3DVCS_to_2DPCS[1, 1] = ima.h_pixel / ca.vf.h_projPlane;
            image.TM_3DVCS_to_2DPCS[1, 2] = ima.h_pixel / 2.0;
            image.TM_3DVCS_to_2DPCS[2, 0] = 0;
            image.TM_3DVCS_to_2DPCS[2, 1] = 0;
            image.TM_3DVCS_to_2DPCS[2, 2] = 1;
            image.focus = f;

            ca.vf.transform2Global(TM_VCS_GCS);

            List<BIMObject> BIMObjs_vf = new List<BIMObject>();
            foreach (var obj in BIMObjs)
            {
                /* NOTE: In revit processing, we had better not to modify BIMObjs 
                  as it seems that these changes will be reserved forever even this function is finished (affect the original BIMObjs)*/
                BIMObject objNew = new BIMObject();
                objNew.GUID = obj.GUID;
                objNew.Name = obj.Name;
                objNew.Category = obj.Category;
                objNew.Mark = obj.Mark;
                objNew.objectType = obj.objectType;
                objNew.triBrep = obj.triBrep;
                //objNew.triBrep = obj.BrepTransformation(TM_GCS_VCS);
                objNew.triBrep.AABB = GO.BoundingBox3D_Triangles_Create(objNew.triBrep.Triangles, 0);
                if (intersectOrcontainBy(objNew.triBrep, ca.vf))
                    BIMObjs_vf.Add(objNew);
            }

            image.triNo_model = getTriNo(BIMObjs);
            image.triNo_image = getTriNo(BIMObjs_vf);

            // 3. ray tracing to annotate image pixels
            // Note: a pixel may correspond to several objects
            // object label = objectType_GUID
            string[,] labels = new string[ima.h_pixel, ima.w_pixel];
            double[,] depths = new double[ima.h_pixel, ima.w_pixel];
            string[,] materials = new string[ima.h_pixel, ima.w_pixel];
            Dictionary<string, List<Point3D>> PCs_objlabel = new Dictionary<string, List<Point3D>>();
            Dictionary<string, List<Point3D>> PCs_semanticlabel = new Dictionary<string, List<Point3D>>();
            Dictionary<string, List<Point3D>> PCs_materiallabel = new Dictionary<string, List<Point3D>>();
            Dictionary<string, List<Point3D>> PCs_objPartlabel = new Dictionary<string, List<Point3D>>();

            for (int i = 0; i < ima.h_pixel; i++)
            {
                for (int j = 0; j < ima.w_pixel; j++)
                {
                    Point3D pij_VCS = new Point3D();
                    pij_VCS.x = (ima.w_pixel / 2 - j) * ca.vf.w_projPlane / Convert.ToDouble(ima.w_pixel);
                    pij_VCS.y = (i - ima.h_pixel / 2) * ca.vf.h_projPlane / Convert.ToDouble(ima.h_pixel);
                    pij_VCS.z = f;
                    Point3D pij_GCS = GO.PointTransf3D(pij_VCS, TM_VCS_GCS);

                    Ray3D ray = new Ray3D();
                    ray.StartPoint = ca.vf.eyePosition_GCS;
                    ray.Direction = GO.UnitVector(new Vector3D(pij_GCS.x - ray.StartPoint.x, pij_GCS.y - ray.StartPoint.y, pij_GCS.z - ray.StartPoint.z));

                    string label = null;
                    double depth = 0;
                    string material = null;
                    Point3D p_vf = new Point3D();

                    // label: objectType_GUID_partNO
                    calculateNeasetIntersection(ray, BIMObjs_vf, out label, out depth, out material, out p_vf);

                    if (p_vf != null)
                    {
                        //Point3D p_GCS = GO.PointTransf3D(p_vf, TM_VCS_GCS);
                        // for the point on the edge of two or more objects, select the first one as the label 
                        if (PCs_objPartlabel.ContainsKey(label.Split('/')[0]))
                            PCs_objPartlabel[label.Split('/')[0]].Add(p_vf);
                        else
                        {
                            List<Point3D> ps = new List<Point3D>();
                            ps.Add(p_vf);
                            PCs_objPartlabel.Add(label.Split('/')[0], ps);
                        }

                        List<string> pieces = label.Split('/')[0].Split('_').ToList().GetRange(0, 2);
                        string objInst = string.Join("_", pieces);
                        if (PCs_objlabel.ContainsKey(objInst))
                            PCs_objlabel[objInst].Add(p_vf);
                        else
                        {
                            List<Point3D> ps = new List<Point3D>();
                            ps.Add(p_vf);
                            PCs_objlabel.Add(objInst, ps);
                        }

                        string objType = pieces[0];
                        if (PCs_semanticlabel.ContainsKey(objType))
                            PCs_semanticlabel[objType].Add(p_vf);
                        else
                        {
                            List<Point3D> ps = new List<Point3D>();
                            ps.Add(p_vf);
                            PCs_semanticlabel.Add(objType, ps);
                        }

                        if (PCs_materiallabel.ContainsKey(material))
                            PCs_materiallabel[material].Add(p_vf);
                        else
                        {
                            List<Point3D> ps = new List<Point3D>();
                            ps.Add(p_vf);
                            PCs_materiallabel.Add(material, ps);
                        }
                    }

                    labels[i, j] = label;
                    depths[i, j] = depth;
                    materials[i, j] = material;
                }
            }
            image.objInstances = new string[ima.h_pixel, ima.w_pixel];
            image.objInstances = labels;

            image.depths = new double[ima.h_pixel, ima.w_pixel];
            image.depths = depths;

            image.materials = new string[ima.h_pixel, ima.w_pixel];
            image.materials = materials;

            image.objPCs = PCs_objlabel;
            image.objPartPCs = PCs_objPartlabel;

            image.semanticPCs = PCs_semanticlabel;
            image.materialPCs = PCs_materiallabel;

            return image;
        }

        int getTriNo(List<BIMObject> BimObjs)
        {
            int result = 0;
            foreach (var obj in BimObjs)
            {
                result += obj.triBrep.Triangles.Count();
            }

            return result;
        }

        void calculateNeasetIntersection(Ray3D ray, List<BIMObject> objects, out string label, out double depth, out string material, out Point3D p_vf)
        {
            GeometricOperations GO = new GeometricOperations();
            label = string.Empty;
            material = string.Empty;

            List<intersection> intersections = new List<intersection>();
            foreach (var obj in objects)
            {
                if (GO.Ray_AABB_do_intersection(ray, obj.triBrep.AABB))
                {
                    IntersectFace intface = GO.Ray_Brep_do_intersection(ray, obj.triBrep.Triangles, true);
                    if (intface.intersection)
                    {
                        intersection inter = new intersection();
                        inter.objectLabel = obj.objectType + "_" + obj.GUID + "_" + intface.face[0].partNo;
                        inter.dist = intface.dist * 304.8;  // convert to mm
                        inter.materialName = intface.face[0].materialName;
                        inter.intPoint = new Point3D(intface.IP.x, intface.IP.y, intface.IP.z);
                        intersections.Add(inter);
                    }
                }
            }

            // Find nearest intersections 
            // sort by dist in ascending order
            if (intersections.Count() == 0)  // no intersect physical objects; intersect air 
            {
                depth = 100000000; // 2^16
                label = "Background";
                material = "Background";
                p_vf = null;
            }
            else
            {
                intersections.Sort((x, y) => x.dist.CompareTo(y.dist));
                depth = intersections[0].dist;
                p_vf = intersections[0].intPoint;
                List<string> labels = new List<string>();
                List<string> materials = new List<string>();
                foreach (var inter in intersections)
                {
                    if (Math.Abs(inter.dist - depth) < 1)  // 1 mm
                    {
                        labels.Add(inter.objectLabel);
                        materials.Add(inter.materialName);
                    }
                }
                label = string.Join("/", labels.ToArray());
                material = string.Join("/", materials.ToArray());
            }
        }

        bool intersectOrcontainBy(TriangulatedBrep obj, viewFrustum vf)
        {
            GeometricOperations GO = new GeometricOperations();
            if (!GO.AABB_AABB_IntersectionTest(obj.AABB, vf.AABB_GCS))
                return false;

            if (GO.Polyhedron_Polyhedron_Disjoint(obj.Triangles, obj.AABB, vf.triFaces_GCS, vf.AABB_GCS))
                return false;

            return true;
        }
    }

    class imageSegmentation_2TierBVHTree
    {
        int MaxDepth_object = 30;
        int MaxDepth_model = 20;

        public labelledImage Segmentation(List<BIMObject> BIMObjs, camera ca, BIMImage ima, BVH_spiltStrategy strategy)
        {
            GeometricOperations GO = new GeometricOperations();
            labelledImage image = new labelledImage();
            image.name = ima.name.Substring(0, ima.name.Length - 4);  // remove .png

            // 1. Find BIM objects in view frustum
            double[,] TM_VCS_GCS = GO.Affine_Matrix(ca.origin_GCS, ca.rightDir_x_GCS, ca.upDir_y_GCS, ca.forward_z_GCS);
            double[,] TM_GCS_VCS = GO.IMatrix_Matrix(TM_VCS_GCS);
            image.TM_GCS_to_VCS = TM_GCS_VCS;

            image.TM_3DVCS_to_2DPCS = new double[3, 3];
            double f = ca.vf.nearPlane_VCS.Vertices[0].z;
            image.TM_3DVCS_to_2DPCS[0, 0] = -1 * ima.w_pixel / ca.vf.w_projPlane;
            image.TM_3DVCS_to_2DPCS[0, 1] = 0;
            image.TM_3DVCS_to_2DPCS[0, 2] = ima.w_pixel / 2.0;
            image.TM_3DVCS_to_2DPCS[1, 0] = 0;
            image.TM_3DVCS_to_2DPCS[1, 1] = ima.h_pixel / ca.vf.h_projPlane;
            image.TM_3DVCS_to_2DPCS[1, 2] = ima.h_pixel / 2.0;
            image.TM_3DVCS_to_2DPCS[2, 0] = 0;
            image.TM_3DVCS_to_2DPCS[2, 1] = 0;
            image.TM_3DVCS_to_2DPCS[2, 2] = 1;
            image.focus = f;

            ca.vf.transform2Global(TM_VCS_GCS);

            List<BIMObject> BIMObjs_vf = new List<BIMObject>();
            Parallel.ForEach(BIMObjs, obj => {
                /* NOTE: In revit processing, we had better not to modify BIMObjs 
                  as it seems that these changes will be reserved forever even this function is finished (affect the original BIMObjs)*/
                BIMObject objNew = new BIMObject();
                objNew.GUID = obj.GUID;
                objNew.Name = obj.Name;
                objNew.Category = obj.Category;
                objNew.Mark = obj.Mark;
                objNew.objectType = obj.objectType;
                objNew.triBrep = obj.triBrep;
                //objNew.triBrep = obj.BrepTransformation(TM_GCS_VCS);
                objNew.triBrep.AABB = GO.BoundingBox3D_Triangles_Create(objNew.triBrep.Triangles, 0);
                if (intersectOrcontainBy(objNew.triBrep, ca.vf))
                    BIMObjs_vf.Add(objNew);
            });

            image.triNo_model = getTriNo(BIMObjs);
            image.triNo_image = getTriNo(BIMObjs_vf);

            // 2. Two-tier BVH indexing of detected BIM objects 
            // BVH tree for object triangles
            Parallel.ForEach(BIMObjs_vf, obj =>
            {
                obj.BVH = new BVH_triangle(obj.triBrep.Triangles, MaxDepth_object, strategy);
            });

            // AABB tree for all BIMObjects
            List<BoundingBox3D> AABBs = new List<BoundingBox3D>();
            foreach (var obj in BIMObjs_vf)
                AABBs.Add(obj.triBrep.AABB);
            BoundingBox3D BuildingAABB = GO.BoundingBox3D_AABBs_Create(AABBs);
            BVH ModelTree = new BVH(BIMObjs_vf, BuildingAABB, MaxDepth_model);

            // 3. ray tracing to annotate image pixels
            // Note: a pixel may correspond to several objects
            // object label = objectType_GUID
            string[,] labels = new string[ima.h_pixel, ima.w_pixel];
            double[,] depths = new double[ima.h_pixel, ima.w_pixel];
            string[,] materials = new string[ima.h_pixel, ima.w_pixel];
            Dictionary<string, List<Point3D>> PCs_objlabel = new Dictionary<string, List<Point3D>>();
            Dictionary<string, List<Point3D>> PCs_semanticlabel = new Dictionary<string, List<Point3D>>();
            Dictionary<string, List<Point3D>> PCs_materiallabel = new Dictionary<string, List<Point3D>>();
            Dictionary<string, List<Point3D>> PCs_objPartlabel = new Dictionary<string, List<Point3D>>();

            Ray3D[,] rays = new Ray3D[ima.h_pixel, ima.w_pixel];
            for (int i = 0; i < ima.h_pixel; i++)
            {
                for (int j = 0; j < ima.w_pixel; j++)
                {
                    Point3D pij_VCS = new Point3D();
                    pij_VCS.x = (ima.w_pixel / 2 - j) * ca.vf.w_projPlane / Convert.ToDouble(ima.w_pixel);
                    pij_VCS.y = (i - ima.h_pixel / 2) * ca.vf.h_projPlane / Convert.ToDouble(ima.h_pixel);
                    pij_VCS.z = f;
                    Point3D pij_GCS = GO.PointTransf3D(pij_VCS, TM_VCS_GCS);

                    Ray3D ray = new Ray3D();
                    ray.StartPoint = ca.vf.eyePosition_GCS;
                    ray.Direction = GO.UnitVector(new Vector3D(pij_GCS.x - ray.StartPoint.x, pij_GCS.y - ray.StartPoint.y, pij_GCS.z - ray.StartPoint.z));
                    rays[i, j] = ray;
                }
            }

            Point3D[,] p_vfs = new Point3D[ima.h_pixel, ima.w_pixel];
            Parallel.For(0, ima.h_pixel, i =>
            {
                Parallel.For(0, ima.w_pixel, j =>
                {
                    string label = null;
                    double depth = 0;
                    string material = null;
                    Point3D p_vf = new Point3D();

                    // AABB tree based
                    List<BIMObject> Candidates = new List<BIMObject>();
                    List<BVHNode> LeafNodes = ModelTree.RootNode.BVHTraversing(rays[i, j]);
                    foreach (var node in LeafNodes)
                        Candidates.AddRange(node.Objects);
                    // label: objectType_GUID_partNO
                    calculateNeasetIntersection_tree(rays[i, j], Candidates, out label, out depth, out material, out p_vf);
                    labels[i, j] = label;
                    depths[i, j] = depth;
                    materials[i, j] = material;
                });
            });

            for (int i = 0; i < ima.h_pixel; i++)
            {
                for (int j = 0; j < ima.w_pixel; j++)
                {
                    if (p_vfs[i, j] != null)
                    {
                        //Point3D p_GCS = GO.PointTransf3D(p_vf, TM_VCS_GCS);
                        // for the point on the edge of two or more objects, select the first one as the label 
                        if (PCs_objPartlabel.ContainsKey(labels[i, j].Split('/')[0]))
                            PCs_objPartlabel[labels[i, j].Split('/')[0]].Add(p_vfs[i, j]);
                        else
                        {
                            List<Point3D> ps = new List<Point3D>();
                            ps.Add(p_vfs[i, j]);
                            PCs_objPartlabel.Add(labels[i, j].Split('/')[0], ps);
                        }

                        List<string> pieces = labels[i, j].Split('/')[0].Split('_').ToList().GetRange(0, 2);
                        string objInst = string.Join("_", pieces);
                        if (PCs_objlabel.ContainsKey(objInst))
                            PCs_objlabel[objInst].Add(p_vfs[i, j]);
                        else
                        {
                            List<Point3D> ps = new List<Point3D>();
                            ps.Add(p_vfs[i, j]);
                            PCs_objlabel.Add(objInst, ps);
                        }

                        string objType = pieces[0];
                        if (PCs_semanticlabel.ContainsKey(objType))
                            PCs_semanticlabel[objType].Add(p_vfs[i, j]);
                        else
                        {
                            List<Point3D> ps = new List<Point3D>();
                            ps.Add(p_vfs[i, j]);
                            PCs_semanticlabel.Add(objType, ps);
                        }

                        if (PCs_materiallabel.ContainsKey(materials[i, j]))
                            PCs_materiallabel[materials[i, j]].Add(p_vfs[i, j]);
                        else
                        {
                            List<Point3D> ps = new List<Point3D>();
                            ps.Add(p_vfs[i, j]);
                            PCs_materiallabel.Add(materials[i, j], ps);
                        }
                    }
                }
            }

            #region nonparallel version
            //for (int i = 0; i < ima.h_pixel; i++) 
            //{
            //    for(int j = 0; j < ima.w_pixel; j++)
            //    {
            //         Point3D pij_VCS = new Point3D();
            //         pij_VCS.x = (ima.w_pixel / 2 - j) * ca.vf.w_projPlane / Convert.ToDouble(ima.w_pixel);
            //         pij_VCS.y = (i - ima.h_pixel / 2) * ca.vf.h_projPlane / Convert.ToDouble(ima.h_pixel);
            //         pij_VCS.z = f;
            //         Point3D pij_GCS = GO.PointTransf3D(pij_VCS, TM_VCS_GCS);

            //         Ray3D ray = new Ray3D();
            //         ray.StartPoint = ca.vf.eyePosition_GCS;
            //         ray.Direction = GO.UnitVector(new Vector3D(pij_GCS.x - ray.StartPoint.x, pij_GCS.y - ray.StartPoint.y, pij_GCS.z - ray.StartPoint.z));

            //         string label = null;
            //         double depth = 0;
            //         string material = null;
            //         Point3D p_vf = new Point3D();

            //         // AABB tree based
            //         List<BIMObject> Candidates = new List<BIMObject>();
            //         List<BVHNode> LeafNodes = ModelTree.RootNode.BVHTraversing(ray);
            //         foreach (var node in LeafNodes)
            //             Candidates.AddRange(node.Objects);
            //         // label: objectType_GUID_partNO
            //         calculateNeasetIntersection_tree(ray, Candidates, out label, out depth, out material, out p_vf);

            //         if (p_vf != null)
            //         {
            //             //Point3D p_GCS = GO.PointTransf3D(p_vf, TM_VCS_GCS);
            //             // for the point on the edge of two or more objects, select the first one as the label 
            //             if (PCs_objPartlabel.ContainsKey(label.Split('/')[0]))
            //                 PCs_objPartlabel[label.Split('/')[0]].Add(p_vf);
            //             else
            //             {
            //                 List<Point3D> ps = new List<Point3D>();
            //                 ps.Add(p_vf);
            //                 PCs_objPartlabel.Add(label.Split('/')[0], ps);
            //             }

            //             List<string> pieces = label.Split('/')[0].Split('_').ToList().GetRange(0, 2);
            //             string objInst = string.Join("_", pieces);
            //             if (PCs_objlabel.ContainsKey(objInst))
            //                 PCs_objlabel[objInst].Add(p_vf);
            //             else
            //             {
            //                 List<Point3D> ps = new List<Point3D>();
            //                 ps.Add(p_vf);
            //                 PCs_objlabel.Add(objInst, ps);
            //             }

            //             string objType = pieces[0];
            //             if (PCs_semanticlabel.ContainsKey(objType))
            //                 PCs_semanticlabel[objType].Add(p_vf);
            //             else
            //             {
            //                 List<Point3D> ps = new List<Point3D>();
            //                 ps.Add(p_vf);
            //                 PCs_semanticlabel.Add(objType, ps);
            //             }

            //             if (PCs_materiallabel.ContainsKey(material))
            //                 PCs_materiallabel[material].Add(p_vf);
            //             else
            //             {
            //                 List<Point3D> ps = new List<Point3D>();
            //                 ps.Add(p_vf);
            //                 PCs_materiallabel.Add(material, ps);
            //             }
            //         }

            //         labels[i, j] = label;
            //         depths[i, j] = depth;
            //         materials[i, j] = material;
            //    }         
            //}
            #endregion

            image.objInstances = new string[ima.h_pixel, ima.w_pixel];
            image.objInstances = labels;

            image.depths = new double[ima.h_pixel, ima.w_pixel];
            image.depths = depths;

            image.materials = new string[ima.h_pixel, ima.w_pixel];
            image.materials = materials;

            image.objPCs = PCs_objlabel;
            image.objPartPCs = PCs_objPartlabel;

            image.semanticPCs = PCs_semanticlabel;
            image.materialPCs = PCs_materiallabel;

            return image;
        }


        public labelledImage Segmentation_distort(List<BIMObject> BIMObjs, camera ca, BIMImage ima, BVH_spiltStrategy strategy, /*string filePath,*/ 
            double[] distortion, Dictionary<string, string> openingid_hostclsid)
        {
            GeometricOperations GO = new GeometricOperations();
            labelledImage image = new labelledImage();
            image.name = ima.name.Substring(0, ima.name.Length - 4);  // remove .png
            bool distort = true;
            if (distortion[0] == 0 && distortion[1] == 0 &&
                distortion[2] == 0 && distortion[3] == 0)
                distort = false;

            // 1. Find BIM objects in view frustum
            double[,] TM_VCS_GCS = GO.Affine_Matrix(ca.origin_GCS, ca.rightDir_x_GCS, ca.upDir_y_GCS, ca.forward_z_GCS);
            double[,] TM_GCS_VCS = GO.IMatrix_Matrix(TM_VCS_GCS);
            image.TM_GCS_to_VCS = TM_GCS_VCS;

            image.TM_3DVCS_to_2DPCS = new double[3, 3];
            double f = ca.vf.nearPlane_VCS.Vertices[0].z;
            image.TM_3DVCS_to_2DPCS[0, 0] = -1 * ima.w_pixel / ca.vf.w_projPlane;
            image.TM_3DVCS_to_2DPCS[0, 1] = 0;
            image.TM_3DVCS_to_2DPCS[0, 2] = ima.w_pixel / 2.0;
            image.TM_3DVCS_to_2DPCS[1, 0] = 0;
            image.TM_3DVCS_to_2DPCS[1, 1] = ima.h_pixel / ca.vf.h_projPlane;
            image.TM_3DVCS_to_2DPCS[1, 2] = ima.h_pixel / 2.0;
            image.TM_3DVCS_to_2DPCS[2, 0] = 0;
            image.TM_3DVCS_to_2DPCS[2, 1] = 0;
            image.TM_3DVCS_to_2DPCS[2, 2] = 1;
            image.focus = f;

            ca.vf.transform2Global(TM_VCS_GCS);

            List<BIMObject> BIMObjs_vf = new List<BIMObject>();
            Parallel.ForEach(BIMObjs, obj => {
                /* NOTE: In revit processing, we had better not to modify BIMObjs 
                  as it seems that these changes will be reserved forever even this function is finished (affect the original BIMObjs)*/
                BIMObject objNew = new BIMObject();
                objNew.GUID = obj.GUID;
                objNew.Name = obj.Name;
                objNew.Category = obj.Category;
                objNew.Mark = obj.Mark;
                objNew.objectType = obj.objectType;
                objNew.triBrep = obj.triBrep;
                //objNew.triBrep = obj.BrepTransformation(TM_GCS_VCS);
                objNew.triBrep.AABB = GO.BoundingBox3D_Triangles_Create(objNew.triBrep.Triangles, 0);
                if (intersectOrcontainBy(objNew.triBrep, ca.vf))
                    BIMObjs_vf.Add(objNew);
            });

            image.triNo_model = getTriNo(BIMObjs);
            image.triNo_image = getTriNo(BIMObjs_vf);

            // 2. Two-tier BVH indexing of detected BIM objects 
            // BVH tree for object triangles
            Parallel.ForEach(BIMObjs_vf, obj =>
            {
                obj.BVH = new BVH_triangle(obj.triBrep.Triangles, MaxDepth_object, strategy);
            });

            // AABB tree for all BIMObjects
            List<BoundingBox3D> AABBs = new List<BoundingBox3D>();
            foreach (var obj in BIMObjs_vf)
                AABBs.Add(obj.triBrep.AABB);
            BoundingBox3D BuildingAABB = GO.BoundingBox3D_AABBs_Create(AABBs);
            BVH ModelTree = new BVH(BIMObjs_vf, BuildingAABB, MaxDepth_model);

            // 3. ray tracing to annotate image pixels
            // Note: a pixel may correspond to several objects
            // object label = objectType_GUID
            string[,] labels = new string[ima.h_pixel, ima.w_pixel];
            //double[,] depths = new double[ima.h_pixel, ima.w_pixel];
            //string[,] materials = new string[ima.h_pixel, ima.w_pixel];
            //Dictionary<string, List<Point3D>> PCs_objlabel = new Dictionary<string, List<Point3D>>();
            //Dictionary<string, List<Point3D>> PCs_semanticlabel = new Dictionary<string, List<Point3D>>();
            //Dictionary<string, List<Point3D>> PCs_materiallabel = new Dictionary<string, List<Point3D>>();
            //Dictionary<string, List<Point3D>> PCs_objPartlabel = new Dictionary<string, List<Point3D>>();

            Ray3D[,] rays = new Ray3D[ima.h_pixel, ima.w_pixel];
            Parallel.For(0, ima.h_pixel, i =>
            {
                Parallel.For(0, ima.w_pixel, j =>
                {
                    Point3D pij_VCS = new Point3D();
                    if (distort)
                    {
                        double[] newpxiel = getDistortPixel(j, i, ca.interiorParameters, distortion);
                        pij_VCS.x = (ima.w_pixel / 2 - newpxiel[0]) * ca.vf.w_projPlane / Convert.ToDouble(ima.w_pixel);
                        pij_VCS.y = (newpxiel[1] - ima.h_pixel / 2) * ca.vf.h_projPlane / Convert.ToDouble(ima.h_pixel);
                    } else
                    {
                        pij_VCS.x = (ima.w_pixel / 2 - j) * ca.vf.w_projPlane / Convert.ToDouble(ima.w_pixel);
                        pij_VCS.y = (i - ima.h_pixel / 2) * ca.vf.h_projPlane / Convert.ToDouble(ima.h_pixel);
                    }
                    
                    pij_VCS.z = f;
                    Point3D pij_GCS = GO.PointTransf3D(pij_VCS, TM_VCS_GCS);

                    Ray3D ray = new Ray3D();
                    ray.StartPoint = ca.vf.eyePosition_GCS;
                    ray.Direction = GO.UnitVector(new Vector3D(pij_GCS.x - ray.StartPoint.x, pij_GCS.y - ray.StartPoint.y, pij_GCS.z - ray.StartPoint.z));
                    rays[i, j] = ray;
                });
            });

            Point3D[,] p_vfs = new Point3D[ima.h_pixel, ima.w_pixel];
            Parallel.For(0, ima.h_pixel, i =>
            {
                Parallel.For(0, ima.w_pixel, j =>
                {
                    string label = null;
                    double depth = 0;
                    string material = null;
                    Point3D p_vf = new Point3D();

                    // AABB tree based
                    List<BIMObject> Candidates = new List<BIMObject>();
                    List<BVHNode> LeafNodes = ModelTree.RootNode.BVHTraversing(rays[i, j]);
                    foreach (var node in LeafNodes)
                        Candidates.AddRange(node.Objects);
                    // label: objectType_GUID_partNO
                    calculateNeasetIntersection_tree(rays[i, j], Candidates, out label, out depth, out material, out p_vf);
                    labels[i, j] = label;
                    //depths[i, j] = depth;
                    //materials[i, j] = material;
                    p_vfs[i, j] = p_vf;
                });
            });

            //for (int i = 0; i < ima.h_pixel; i++)
            //{
            //    for (int j = 0; j < ima.w_pixel; j++)
            //    {
            //        if (p_vfs[i, j] != null)
            //        {
            //            //Point3D p_GCS = GO.PointTransf3D(p_vf, TM_VCS_GCS);
            //            // for the point on the edge of two or more objects, select the first one as the label 
            //            //if (PCs_objPartlabel.ContainsKey(labels[i, j].Split('/')[0]))
            //            //    PCs_objPartlabel[labels[i, j].Split('/')[0]].Add(p_vfs[i, j]);
            //            //else
            //            //{
            //            //    List<Point3D> ps = new List<Point3D>();
            //            //    ps.Add(p_vfs[i, j]);
            //            //    PCs_objPartlabel.Add(labels[i, j].Split('/')[0], ps);
            //            //}

            //            //List<string> pieces = labels[i, j].Split('/')[0].Split('_').ToList().GetRange(0, 2);
            //            //string objInst = string.Join("_", pieces);
            //            //if (PCs_objlabel.ContainsKey(objInst))
            //            //    PCs_objlabel[objInst].Add(p_vfs[i, j]);
            //            //else
            //            //{
            //            //    List<Point3D> ps = new List<Point3D>();
            //            //    ps.Add(p_vfs[i, j]);
            //            //    PCs_objlabel.Add(objInst, ps);
            //            //}

            //            //string objType = pieces[0];
            //            //if (PCs_semanticlabel.ContainsKey(objType))
            //            //    PCs_semanticlabel[objType].Add(p_vfs[i, j]);
            //            //else
            //            //{
            //            //    List<Point3D> ps = new List<Point3D>();
            //            //    ps.Add(p_vfs[i, j]);
            //            //    PCs_semanticlabel.Add(objType, ps);
            //            //}

            //            //if (PCs_materiallabel.ContainsKey(materials[i, j]))
            //            //    PCs_materiallabel[materials[i, j]].Add(p_vfs[i, j]);
            //            //else
            //            //{
            //            //    List<Point3D> ps = new List<Point3D>();
            //            //    ps.Add(p_vfs[i, j]);
            //            //    PCs_materiallabel.Add(materials[i, j], ps);
            //            //}
            //        }
            //    }
            //}

            #region nonparallel version
            //for (int i = 0; i < ima.h_pixel; i++) 
            //{
            //    for(int j = 0; j < ima.w_pixel; j++)
            //    {
            //         Point3D pij_VCS = new Point3D();
            //         pij_VCS.x = (ima.w_pixel / 2 - j) * ca.vf.w_projPlane / Convert.ToDouble(ima.w_pixel);
            //         pij_VCS.y = (i - ima.h_pixel / 2) * ca.vf.h_projPlane / Convert.ToDouble(ima.h_pixel);
            //         pij_VCS.z = f;
            //         Point3D pij_GCS = GO.PointTransf3D(pij_VCS, TM_VCS_GCS);

            //         Ray3D ray = new Ray3D();
            //         ray.StartPoint = ca.vf.eyePosition_GCS;
            //         ray.Direction = GO.UnitVector(new Vector3D(pij_GCS.x - ray.StartPoint.x, pij_GCS.y - ray.StartPoint.y, pij_GCS.z - ray.StartPoint.z));

            //         string label = null;
            //         double depth = 0;
            //         string material = null;
            //         Point3D p_vf = new Point3D();

            //         // AABB tree based
            //         List<BIMObject> Candidates = new List<BIMObject>();
            //         List<BVHNode> LeafNodes = ModelTree.RootNode.BVHTraversing(ray);
            //         foreach (var node in LeafNodes)
            //             Candidates.AddRange(node.Objects);
            //         // label: objectType_GUID_partNO
            //         calculateNeasetIntersection_tree(ray, Candidates, out label, out depth, out material, out p_vf);

            //         if (p_vf != null)
            //         {
            //             //Point3D p_GCS = GO.PointTransf3D(p_vf, TM_VCS_GCS);
            //             // for the point on the edge of two or more objects, select the first one as the label 
            //             if (PCs_objPartlabel.ContainsKey(label.Split('/')[0]))
            //                 PCs_objPartlabel[label.Split('/')[0]].Add(p_vf);
            //             else
            //             {
            //                 List<Point3D> ps = new List<Point3D>();
            //                 ps.Add(p_vf);
            //                 PCs_objPartlabel.Add(label.Split('/')[0], ps);
            //             }

            //             List<string> pieces = label.Split('/')[0].Split('_').ToList().GetRange(0, 2);
            //             string objInst = string.Join("_", pieces);
            //             if (PCs_objlabel.ContainsKey(objInst))
            //                 PCs_objlabel[objInst].Add(p_vf);
            //             else
            //             {
            //                 List<Point3D> ps = new List<Point3D>();
            //                 ps.Add(p_vf);
            //                 PCs_objlabel.Add(objInst, ps);
            //             }

            //             string objType = pieces[0];
            //             if (PCs_semanticlabel.ContainsKey(objType))
            //                 PCs_semanticlabel[objType].Add(p_vf);
            //             else
            //             {
            //                 List<Point3D> ps = new List<Point3D>();
            //                 ps.Add(p_vf);
            //                 PCs_semanticlabel.Add(objType, ps);
            //             }

            //             if (PCs_materiallabel.ContainsKey(material))
            //                 PCs_materiallabel[material].Add(p_vf);
            //             else
            //             {
            //                 List<Point3D> ps = new List<Point3D>();
            //                 ps.Add(p_vf);
            //                 PCs_materiallabel.Add(material, ps);
            //             }
            //         }

            //         labels[i, j] = label;
            //         depths[i, j] = depth;
            //         materials[i, j] = material;
            //    }         
            //}
            #endregion

            image.objInstances = new string[ima.h_pixel, ima.w_pixel];
            image.objInstances = ignoreHolesByOpenings(labels, openingid_hostclsid);
            //image.objInstances = labels;


            //image.depths = new double[ima.h_pixel, ima.w_pixel];
            //image.depths = depths;

            //image.materials = new string[ima.h_pixel, ima.w_pixel];
            //image.materials = materials;

            //image.objPCs = PCs_objlabel;
            //image.objPartPCs = PCs_objPartlabel;

            //image.semanticPCs = PCs_semanticlabel;
            //image.materialPCs = PCs_materiallabel;

            return image;
        }

        // !!!! assume all host objects are single objects
        string[,] ignoreHolesByOpenings(string[,] labels, Dictionary<string, string> openingid_hostclsid)
        {
            string[,] results = new string[labels.GetLength(0), labels.GetLength(1)];

            for(int i = 0; i < labels.GetLength(0); i++)
            {
                for (int j = 0; j < labels.GetLength(1); j++)
                {
                    if (labels[i, j].Contains("door") || labels[i, j].Contains("Door")
                        || labels[i, j].Contains("window") || labels[i, j].Contains("Window"))
                    {
                        List<string> seqs = labels[i, j].Split('/').ToList();
                        foreach (var seg in seqs)
                        {
                            string id = seg.Split('_')[1];
                            if (openingid_hostclsid.Keys.Contains(id))
                            {
                                string newLabel = labels[i, j] + '/' + openingid_hostclsid[id] + "_p1";
                                results[i, j] = newLabel;
                                break;
                            }
                        }
                    }
                    else
                        results[i, j] = labels[i, j];
                }
            }
            return results;
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

        int getTriNo(List<BIMObject> BimObjs)
        {
            int result = 0;
            foreach (var obj in BimObjs)
            {
                if (obj != null && obj.triBrep != null)
                    result += obj.triBrep.Triangles.Count();
            }

            return result;
        }

        void calculateNeasetIntersection_tree(Ray3D ray, List<BIMObject> objects, out string label, out double depth, out string material, out Point3D p_vf)
        {
            GeometricOperations GO = new GeometricOperations();
            label = string.Empty;
            material = string.Empty;
            depth = 0;
            p_vf = new Point3D();

            List<intersection> intersections = new List<intersection>();
            foreach (var obj in objects)
            {
                if (GO.Ray_AABB_do_intersection(ray, obj.triBrep.AABB))
                {
                    List<Triangle3D> CandidateTriangles = new List<Triangle3D>();
                    List<BVHNode_triangle> leafNodes = obj.BVH.RootNode.BVHTraversing(ray);
                    foreach (var item in leafNodes)
                        CandidateTriangles.AddRange(item.Objects);

                    IntersectFace intface = GO.Ray_Brep_do_intersection(ray, CandidateTriangles, true);
                    if (intface.intersection)
                    {
                        intersection inter = new intersection();
                        inter.objectLabel = obj.objectType + "_" + obj.GUID + "_" + intface.face[0].partNo;
                        inter.dist = intface.dist * 304.8;  // convert to mm
                        inter.materialName = intface.face[0].materialName;
                        inter.intPoint = new Point3D(intface.IP.x, intface.IP.y, intface.IP.z);
                        intersections.Add(inter);
                    }
                }
            }

            // Find nearest intersections 
            // sort by dist in ascending order
            if (intersections.Count() == 0)  // no intersect physical objects; intersect air 
            {
                depth = 100000000; // 2^16
                label = "Background";
                material = "Background";
                p_vf = null;
            }
            else
            {
                intersections.Sort((x, y) => x.dist.CompareTo(y.dist));
                depth = intersections[0].dist;
                p_vf = intersections[0].intPoint;
                List<string> labels = new List<string>();
                List<string> materials = new List<string>();
                foreach (var inter in intersections)
                {
                    if (Math.Abs(inter.dist - depth) < 0.1)  // 1 mm
                    {
                        labels.Add(inter.objectLabel);
                        materials.Add(inter.materialName);
                    }
                }
                label = string.Join("/", labels.ToArray());
                material = string.Join("/", materials.ToArray());
            }
        }

        bool intersectOrcontainBy(TriangulatedBrep obj, viewFrustum vf)
        {
            GeometricOperations GO = new GeometricOperations();
            if (!GO.AABB_AABB_IntersectionTest(obj.AABB, vf.AABB_GCS))
                return false;

            if (GO.Polyhedron_Polyhedron_Disjoint(obj.Triangles, obj.AABB, vf.triFaces_GCS, vf.AABB_GCS))
                return false;

            return true;
        }
    }

    class imageSegmentation_1TierBVHTree_obj
    {
        int MaxDepth = 30;

        public labelledImage Segmentation(List<BIMObject> BIMObjs, camera ca, BIMImage ima)
        {
            GeometricOperations GO = new GeometricOperations();
            labelledImage image = new labelledImage();
            image.name = ima.name.Substring(0, ima.name.Length - 4);  // remove .png

            // 1. Find BIM objects in view frustum
            double[,] TM_VCS_GCS = GO.Affine_Matrix(ca.origin_GCS, ca.rightDir_x_GCS, ca.upDir_y_GCS, ca.forward_z_GCS);
            double[,] TM_GCS_VCS = GO.IMatrix_Matrix(TM_VCS_GCS);
            image.TM_GCS_to_VCS = TM_GCS_VCS;

            image.TM_3DVCS_to_2DPCS = new double[3, 3];
            double f = ca.vf.nearPlane_VCS.Vertices[0].z;
            image.TM_3DVCS_to_2DPCS[0, 0] = -1 * ima.w_pixel / ca.vf.w_projPlane;
            image.TM_3DVCS_to_2DPCS[0, 1] = 0;
            image.TM_3DVCS_to_2DPCS[0, 2] = ima.w_pixel / 2.0;
            image.TM_3DVCS_to_2DPCS[1, 0] = 0;
            image.TM_3DVCS_to_2DPCS[1, 1] = ima.h_pixel / ca.vf.h_projPlane;
            image.TM_3DVCS_to_2DPCS[1, 2] = ima.h_pixel / 2.0;
            image.TM_3DVCS_to_2DPCS[2, 0] = 0;
            image.TM_3DVCS_to_2DPCS[2, 1] = 0;
            image.TM_3DVCS_to_2DPCS[2, 2] = 1;
            image.focus = f;

            ca.vf.transform2Global(TM_VCS_GCS);

            List<BIMObject> BIMObjs_vf = new List<BIMObject>();
            foreach (var obj in BIMObjs)
            {
                /* NOTE: In revit processing, we had better not to modify BIMObjs 
                  as it seems that these changes will be reserved forever even this function is finished (affect the original BIMObjs)*/
                BIMObject objNew = new BIMObject();
                objNew.GUID = obj.GUID;
                objNew.Name = obj.Name;
                objNew.Category = obj.Category;
                objNew.Mark = obj.Mark;
                objNew.objectType = obj.objectType;
                objNew.triBrep = obj.triBrep;
                //objNew.triBrep = obj.BrepTransformation(TM_GCS_VCS);
                objNew.triBrep.AABB = GO.BoundingBox3D_Triangles_Create(objNew.triBrep.Triangles, 0);
                if (intersectOrcontainBy(objNew.triBrep, ca.vf))
                    BIMObjs_vf.Add(objNew);
            }

            image.triNo_model = getTriNo(BIMObjs);
            image.triNo_image = getTriNo(BIMObjs_vf);

            // 2. 1-tier AABB indexing of detected BIM objects 
            List<BoundingBox3D> AABBs = new List<BoundingBox3D>();
            foreach (var obj in BIMObjs_vf)
                AABBs.Add(obj.triBrep.AABB);
            BoundingBox3D BuildingAABB = GO.BoundingBox3D_AABBs_Create(AABBs);
            BVH ModelTree = new BVH(BIMObjs_vf, BuildingAABB, MaxDepth);

            // 3. ray tracing to annotate image pixels
            // Note: a pixel may correspond to several objects
            // object label = objectType_GUID
            string[,] labels = new string[ima.h_pixel, ima.w_pixel];
            double[,] depths = new double[ima.h_pixel, ima.w_pixel];
            string[,] materials = new string[ima.h_pixel, ima.w_pixel];
            Dictionary<string, List<Point3D>> PCs_objlabel = new Dictionary<string, List<Point3D>>();
            Dictionary<string, List<Point3D>> PCs_semanticlabel = new Dictionary<string, List<Point3D>>();
            Dictionary<string, List<Point3D>> PCs_materiallabel = new Dictionary<string, List<Point3D>>();
            Dictionary<string, List<Point3D>> PCs_objPartlabel = new Dictionary<string, List<Point3D>>();

            for (int i = 0; i < ima.h_pixel; i++)
            {
                for (int j = 0; j < ima.w_pixel; j++)
                {
                    Point3D pij_VCS = new Point3D();
                    pij_VCS.x = (ima.w_pixel / 2 - j) * ca.vf.w_projPlane / Convert.ToDouble(ima.w_pixel);
                    pij_VCS.y = (i - ima.h_pixel / 2) * ca.vf.h_projPlane / Convert.ToDouble(ima.h_pixel);
                    pij_VCS.z = f;
                    Point3D pij_GCS = GO.PointTransf3D(pij_VCS, TM_VCS_GCS);

                    Ray3D ray = new Ray3D();
                    ray.StartPoint = ca.vf.eyePosition_GCS;
                    ray.Direction = GO.UnitVector(new Vector3D(pij_GCS.x - ray.StartPoint.x, pij_GCS.y - ray.StartPoint.y, pij_GCS.z - ray.StartPoint.z));

                    string label = null;
                    double depth = 0;
                    string material = null;
                    Point3D p_vf = new Point3D();

                    // label: objectType_GUID_partNO
                    List<BIMObject> Candidates = new List<BIMObject>();
                    List<BVHNode> LeafNodes = ModelTree.RootNode.BVHTraversing(ray);
                    foreach (var node in LeafNodes)
                        Candidates.AddRange(node.Objects);

                    calculateNeasetIntersection(ray, Candidates, out label, out depth, out material, out p_vf);

                    if (p_vf != null)
                    {
                        //Point3D p_GCS = GO.PointTransf3D(p_vf, TM_VCS_GCS);
                        // for the point on the edge of two or more objects, select the first one as the label 
                        if (PCs_objPartlabel.ContainsKey(label.Split('/')[0]))
                            PCs_objPartlabel[label.Split('/')[0]].Add(p_vf);
                        else
                        {
                            List<Point3D> ps = new List<Point3D>();
                            ps.Add(p_vf);
                            PCs_objPartlabel.Add(label.Split('/')[0], ps);
                        }

                        List<string> pieces = label.Split('/')[0].Split('_').ToList().GetRange(0, 2);
                        string objInst = string.Join("_", pieces);
                        if (PCs_objlabel.ContainsKey(objInst))
                            PCs_objlabel[objInst].Add(p_vf);
                        else
                        {
                            List<Point3D> ps = new List<Point3D>();
                            ps.Add(p_vf);
                            PCs_objlabel.Add(objInst, ps);
                        }

                        string objType = pieces[0];
                        if (PCs_semanticlabel.ContainsKey(objType))
                            PCs_semanticlabel[objType].Add(p_vf);
                        else
                        {
                            List<Point3D> ps = new List<Point3D>();
                            ps.Add(p_vf);
                            PCs_semanticlabel.Add(objType, ps);
                        }

                        if (PCs_materiallabel.ContainsKey(material))
                            PCs_materiallabel[material].Add(p_vf);
                        else
                        {
                            List<Point3D> ps = new List<Point3D>();
                            ps.Add(p_vf);
                            PCs_materiallabel.Add(material, ps);
                        }
                    }

                    labels[i, j] = label;
                    depths[i, j] = depth;
                    materials[i, j] = material;
                }
            }
            image.objInstances = new string[ima.h_pixel, ima.w_pixel];
            image.objInstances = labels;

            image.depths = new double[ima.h_pixel, ima.w_pixel];
            image.depths = depths;

            image.materials = new string[ima.h_pixel, ima.w_pixel];
            image.materials = materials;

            image.objPCs = PCs_objlabel;
            image.objPartPCs = PCs_objPartlabel;

            image.semanticPCs = PCs_semanticlabel;
            image.materialPCs = PCs_materiallabel;

            return image;
        }

        int getTriNo(List<BIMObject> BimObjs)
        {
            int result = 0;
            foreach (var obj in BimObjs)
            {
                result += obj.triBrep.Triangles.Count();
            }

            return result;
        }

        void calculateNeasetIntersection(Ray3D ray, List<BIMObject> objects, out string label, out double depth, out string material, out Point3D p_vf)
        {
            GeometricOperations GO = new GeometricOperations();
            label = string.Empty;
            material = string.Empty;

            List<intersection> intersections = new List<intersection>();
            foreach (var obj in objects)
            {
                if (GO.Ray_AABB_do_intersection(ray, obj.triBrep.AABB))
                {
                    IntersectFace intface = GO.Ray_Brep_do_intersection(ray, obj.triBrep.Triangles, true);
                    if (intface.intersection)
                    {
                        intersection inter = new intersection();
                        inter.objectLabel = obj.objectType + "_" + obj.GUID + "_" + intface.face[0].partNo;
                        inter.dist = intface.dist * 304.8;  // convert to mm
                        inter.materialName = intface.face[0].materialName;
                        inter.intPoint = new Point3D(intface.IP.x, intface.IP.y, intface.IP.z);
                        intersections.Add(inter);
                    }
                }
            }

            // Find nearest intersections 
            // sort by dist in ascending order
            if (intersections.Count() == 0)  // no intersect physical objects; intersect air 
            {
                depth = 100000000; // 2^16
                label = "Background";
                material = "Background";
                p_vf = null;
            }
            else
            {
                intersections.Sort((x, y) => x.dist.CompareTo(y.dist));
                depth = intersections[0].dist;
                p_vf = intersections[0].intPoint;
                List<string> labels = new List<string>();
                List<string> materials = new List<string>();
                foreach (var inter in intersections)
                {
                    if (Math.Abs(inter.dist - depth) < 1)  // 1 mm
                    {
                        labels.Add(inter.objectLabel);
                        materials.Add(inter.materialName);
                    }
                }
                label = string.Join("/", labels.ToArray());
                material = string.Join("/", materials.ToArray());
            }
        }


        bool intersectOrcontainBy(TriangulatedBrep obj, viewFrustum vf)
        {
            GeometricOperations GO = new GeometricOperations();
            if (!GO.AABB_AABB_IntersectionTest(obj.AABB, vf.AABB_GCS))
                return false;

            if (GO.Polyhedron_Polyhedron_Disjoint(obj.Triangles, obj.AABB, vf.triFaces_GCS, vf.AABB_GCS))
                return false;

            return true;
        }
    }

    class imageSegmentation_1TierBVHTree_tri
    {
        int MaxDepth = 30;

        public labelledImage Segmentation(List<BIMObject> BIMObjs, camera ca, BIMImage ima, BVH_spiltStrategy strategy)
        {
            GeometricOperations GO = new GeometricOperations();
            labelledImage image = new labelledImage();
            image.name = ima.name.Substring(0, ima.name.Length - 4);  // remove .png

            // 1. Find BIM objects in view frustum
            double[,] TM_VCS_GCS = GO.Affine_Matrix(ca.origin_GCS, ca.rightDir_x_GCS, ca.upDir_y_GCS, ca.forward_z_GCS);
            double[,] TM_GCS_VCS = GO.IMatrix_Matrix(TM_VCS_GCS);
            image.TM_GCS_to_VCS = TM_GCS_VCS;

            image.TM_3DVCS_to_2DPCS = new double[3, 3];
            double f = ca.vf.nearPlane_VCS.Vertices[0].z;
            image.TM_3DVCS_to_2DPCS[0, 0] = -1 * ima.w_pixel / ca.vf.w_projPlane;
            image.TM_3DVCS_to_2DPCS[0, 1] = 0;
            image.TM_3DVCS_to_2DPCS[0, 2] = ima.w_pixel / 2.0;
            image.TM_3DVCS_to_2DPCS[1, 0] = 0;
            image.TM_3DVCS_to_2DPCS[1, 1] = ima.h_pixel / ca.vf.h_projPlane;
            image.TM_3DVCS_to_2DPCS[1, 2] = ima.h_pixel / 2.0;
            image.TM_3DVCS_to_2DPCS[2, 0] = 0;
            image.TM_3DVCS_to_2DPCS[2, 1] = 0;
            image.TM_3DVCS_to_2DPCS[2, 2] = 1;
            image.focus = f;

            ca.vf.transform2Global(TM_VCS_GCS);

            List<BIMObject> BIMObjs_vf = new List<BIMObject>();
            foreach (var obj in BIMObjs)
            {
                /* NOTE: In revit processing, we had better not to modify BIMObjs 
                  as it seems that these changes will be reserved forever even this function is finished (affect the original BIMObjs)*/
                BIMObject objNew = new BIMObject();
                objNew.GUID = obj.GUID;
                objNew.Name = obj.Name;
                objNew.Category = obj.Category;
                objNew.Mark = obj.Mark;
                objNew.objectType = obj.objectType;
                objNew.triBrep = obj.triBrep;
                //objNew.triBrep = obj.BrepTransformation(TM_GCS_VCS);
                objNew.triBrep.AABB = GO.BoundingBox3D_Triangles_Create(objNew.triBrep.Triangles, 0);
                if (intersectOrcontainBy(objNew.triBrep, ca.vf))
                    BIMObjs_vf.Add(objNew);
            }

            image.triNo_model = getTriNo(BIMObjs);
            image.triNo_image = getTriNo(BIMObjs_vf);

            // 2. 1-tier AABB indexing of detected BIM objects's triangles 
            // stack all triangles
            List<Triangle3D> objTriangles = new List<Triangle3D>();
            foreach (var obj in BIMObjs_vf)
            {
                foreach (var tri in obj.triBrep.Triangles)
                {
                    tri.Category = obj.Category;
                    tri.Name = obj.Name;
                    tri.Mark = obj.Mark;
                    tri.GUID = obj.GUID;
                    tri.objectType = obj.objectType;
                    objTriangles.Add(tri);
                }
            }

            // AABB tree
            BVH_triangle bvh = new BVH_triangle(objTriangles, MaxDepth, strategy);

            // 3. ray tracing to annotate image pixels
            // Note: a pixel may correspond to several objects
            // object label = objectType_GUID
            string[,] labels = new string[ima.h_pixel, ima.w_pixel];
            double[,] depths = new double[ima.h_pixel, ima.w_pixel];
            string[,] materials = new string[ima.h_pixel, ima.w_pixel];
            Dictionary<string, List<Point3D>> PCs_objlabel = new Dictionary<string, List<Point3D>>();
            Dictionary<string, List<Point3D>> PCs_semanticlabel = new Dictionary<string, List<Point3D>>();
            Dictionary<string, List<Point3D>> PCs_materiallabel = new Dictionary<string, List<Point3D>>();
            Dictionary<string, List<Point3D>> PCs_objPartlabel = new Dictionary<string, List<Point3D>>();

            for (int i = 0; i < ima.h_pixel; i++)
            {
                for (int j = 0; j < ima.w_pixel; j++)
                {
                    Point3D pij_VCS = new Point3D();
                    pij_VCS.x = (ima.w_pixel / 2 - j) * ca.vf.w_projPlane / Convert.ToDouble(ima.w_pixel);
                    pij_VCS.y = (i - ima.h_pixel / 2) * ca.vf.h_projPlane / Convert.ToDouble(ima.h_pixel);
                    pij_VCS.z = f;
                    Point3D pij_GCS = GO.PointTransf3D(pij_VCS, TM_VCS_GCS);

                    Ray3D ray = new Ray3D();
                    ray.StartPoint = ca.vf.eyePosition_GCS;
                    ray.Direction = GO.UnitVector(new Vector3D(pij_GCS.x - ray.StartPoint.x, pij_GCS.y - ray.StartPoint.y, pij_GCS.z - ray.StartPoint.z));

                    string label = null;
                    double depth = 0;
                    string material = null;
                    Point3D p_vf = new Point3D();

                    // label: objectType_GUID_partNO
                    calculateNeasetIntersection_tree(ray, bvh, out label, out depth, out material, out p_vf);

                    if (p_vf != null)
                    {
                        //Point3D p_GCS = GO.PointTransf3D(p_vf, TM_VCS_GCS);
                        // for the point on the edge of two or more objects, select the first one as the label 
                        if (PCs_objPartlabel.ContainsKey(label.Split('/')[0]))
                            PCs_objPartlabel[label.Split('/')[0]].Add(p_vf);
                        else
                        {
                            List<Point3D> ps = new List<Point3D>();
                            ps.Add(p_vf);
                            PCs_objPartlabel.Add(label.Split('/')[0], ps);
                        }

                        List<string> pieces = label.Split('/')[0].Split('_').ToList().GetRange(0, 2);
                        string objInst = string.Join("_", pieces);
                        if (PCs_objlabel.ContainsKey(objInst))
                            PCs_objlabel[objInst].Add(p_vf);
                        else
                        {
                            List<Point3D> ps = new List<Point3D>();
                            ps.Add(p_vf);
                            PCs_objlabel.Add(objInst, ps);
                        }

                        string objType = pieces[0];
                        if (PCs_semanticlabel.ContainsKey(objType))
                            PCs_semanticlabel[objType].Add(p_vf);
                        else
                        {
                            List<Point3D> ps = new List<Point3D>();
                            ps.Add(p_vf);
                            PCs_semanticlabel.Add(objType, ps);
                        }

                        if (PCs_materiallabel.ContainsKey(material))
                            PCs_materiallabel[material].Add(p_vf);
                        else
                        {
                            List<Point3D> ps = new List<Point3D>();
                            ps.Add(p_vf);
                            PCs_materiallabel.Add(material, ps);
                        }
                    }

                    labels[i, j] = label;
                    depths[i, j] = depth;
                    materials[i, j] = material;
                }
            }
            image.objInstances = new string[ima.h_pixel, ima.w_pixel];
            image.objInstances = labels;

            image.depths = new double[ima.h_pixel, ima.w_pixel];
            image.depths = depths;

            image.materials = new string[ima.h_pixel, ima.w_pixel];
            image.materials = materials;

            image.objPCs = PCs_objlabel;
            image.objPartPCs = PCs_objPartlabel;

            image.semanticPCs = PCs_semanticlabel;
            image.materialPCs = PCs_materiallabel;

            return image;
        }

        int getTriNo(List<BIMObject> BimObjs)
        {
            int result = 0;
            foreach (var obj in BimObjs)
            {
                result += obj.triBrep.Triangles.Count();
            }

            return result;
        }

        void calculateNeasetIntersection_tree(Ray3D ray, BVH_triangle bvh, out string label, out double depth, out string material, out Point3D p_vf)
        {
            GeometricOperations GO = new GeometricOperations();
            label = string.Empty;
            material = string.Empty;
            depth = 0;
            p_vf = new Point3D();

            List<intersection> intersections = new List<intersection>();
            List<Triangle3D> CandidateTriangles = new List<Triangle3D>();
            List<BVHNode_triangle> leafNodes = bvh.RootNode.BVHTraversing(ray);
            foreach (var item in leafNodes)
                CandidateTriangles.AddRange(item.Objects);

            IntersectFace intface = GO.Ray_Brep_do_intersection(ray, CandidateTriangles, true);
            if (intface.intersection)
            {
                intersection inter = new intersection();
                inter.objectLabel = intface.face[0].objectType + "_" + intface.face[0].GUID + "_" + intface.face[0].partNo;
                inter.dist = intface.dist * 304.8;  // convert to mm
                inter.materialName = intface.face[0].materialName;
                inter.intPoint = new Point3D(intface.IP.x, intface.IP.y, intface.IP.z);
                intersections.Add(inter);
            }

            // Find nearest intersections 
            // sort by dist in ascending order
            if (intersections.Count() == 0)  // no intersect physical objects; intersect air 
            {
                depth = 100000000; // 2^16
                label = "Background";
                material = "Background";
                p_vf = null;
            }
            else
            {
                intersections.Sort((x, y) => x.dist.CompareTo(y.dist));
                depth = intersections[0].dist;
                p_vf = intersections[0].intPoint;
                List<string> labels = new List<string>();
                List<string> materials = new List<string>();
                foreach (var inter in intersections)
                {
                    if (Math.Abs(inter.dist - depth) < 0.1)  // 1 mm
                    {
                        labels.Add(inter.objectLabel);
                        materials.Add(inter.materialName);
                    }
                }
                label = string.Join("/", labels.ToArray());
                material = string.Join("/", materials.ToArray());
            }
        }


        bool intersectOrcontainBy(TriangulatedBrep obj, viewFrustum vf)
        {
            GeometricOperations GO = new GeometricOperations();
            if (!GO.AABB_AABB_IntersectionTest(obj.AABB, vf.AABB_GCS))
                return false;

            if (GO.Polyhedron_Polyhedron_Disjoint(obj.Triangles, obj.AABB, vf.triFaces_GCS, vf.AABB_GCS))
                return false;

            return true;
        }
    }

    class TM_GCS2PCS
    {
        public double[,] TM_GCS2VCS = new double[4, 4];
        public double[,] TM_VCS2PCS = new double[3, 3];
        public double focus;
    }

    class imageAnnotate
    {
        public int[] shape;
        public string[,] annotations;
    }

    class labelledImage
    {
        public string name;

        public int triNo_model;
        public int triNo_image;

        public string[,] objInstances;
        public double[,] depths;
        public string[,] materials;

        public Dictionary<string, List<Point3D>> objPCs = new Dictionary<string, List<Point3D>>();
        public Dictionary<string, List<Point3D>> semanticPCs = new Dictionary<string, List<Point3D>>();
        public Dictionary<string, List<Point3D>> materialPCs = new Dictionary<string, List<Point3D>>();
        public Dictionary<string, List<Point3D>> objPartPCs = new Dictionary<string, List<Point3D>>();

        public double[,] TM_GCS_to_VCS = new double[4, 4];
        public double[,] TM_3DVCS_to_2DPCS = new double[3, 3];
        public double focus;
    }

    class intersection
    {
        public string objectLabel;
        public double dist;
        public string materialName;
        public Point3D intPoint = new Point3D();
    }

    class BIMObject
    {
        public string GUID;
        public string Name;
        public string Category;
        public string Mark;
        public string objectType;
        public BVH_triangle BVH = new BVH_triangle();
        public TriangulatedBrep triBrep = new TriangulatedBrep();
        public List<BoundingBox3D> AABBS_objParts = new List<BoundingBox3D>();
        //public List<BIMObject> Components = new List<BIMObject>();   // for aggragate object

        public TriangulatedBrep BrepTransformation(double[,] TM)
        {
            TriangulatedBrep newBrep = new TriangulatedBrep();

            GeometricOperations Go = new GeometricOperations();
            foreach (var tri in triBrep.Triangles)
            {
                Triangle3D tri_new = Go.triTransf(tri, TM);
                tri_new.partNo = tri.partNo;
                tri_new.materialName = tri.materialName;
                newBrep.Triangles.Add(tri_new);
            }


            foreach (var face in triBrep.TriangleFaces)
            {
                List<Triangle3D> newFace = new List<Triangle3D>();
                foreach (var tri in face)
                {
                    Triangle3D tri_new = Go.triTransf(tri, TM);
                    tri_new.partNo = tri.partNo;
                    tri_new.materialName = tri.materialName;
                    newFace.Add(tri_new);
                }
                newBrep.TriangleFaces.Add(newFace);
            }

            return newBrep;
        }
    }


    class RoomObject
    {
        public string GUID;
        public string Name;
        public TriangulatedBrep triBrep = new TriangulatedBrep();
    }

    class BIMImage
    {
        public string name;
        public int w_pixel;
        public int h_pixel;
        public float hori_resolution;
        public float vert_resolution;
        public int pixelSize;
        public int size;  // byte
    }

    class camera
    {
        //public string spaceName;
        public Point3D origin_GCS = new Point3D(); // in GCS
        public Vector3D upDir_y_GCS = new Vector3D(); // in GCS 
        public Vector3D rightDir_x_GCS = new Vector3D();  // in GCS 
        public Vector3D forward_z_GCS = new Vector3D();  // in GCS 

        public viewFrustum vf = null;  // in VCS
        public double[,] interiorParameters = new double[3, 3];

        public camera(Document Doc, Element view, int W)
        {
            View3D view3D = view as View3D;
            var context = new MyExportContext();
            CustomExporter exporter = new CustomExporter(Doc, context)
            {
                IncludeGeometricObjects = false,
                ShouldStopOnError = true
            };
            exporter.Export(view3D);

            var cameraInfo = context.CameraLocalInfo;

            interiorParameters = getCameraInteriorParameters(view3D, cameraInfo, W);

            var viewOrientation3D = view3D.GetOrientation();
            forward_z_GCS = XYZToVector(viewOrientation3D.ForwardDirection);
            forward_z_GCS = new Vector3D().Multiple(forward_z_GCS, -1);
            upDir_y_GCS = XYZToVector(viewOrientation3D.UpDirection);
            rightDir_x_GCS = XYZToVector(viewOrientation3D.UpDirection.CrossProduct(viewOrientation3D.ForwardDirection));
            origin_GCS = XYZToPoint(viewOrientation3D.EyePosition);

            vf = new viewFrustum(view3D, cameraInfo);
        }

        double[,] getCameraInteriorParameters(View3D view, CameraLocalInfo cameraInfo, int W)
        {
            double[,] para = new double[3, 3];

            double w = view.Outline.Max.U - view.Outline.Min.U;
            double h = view.Outline.Max.V - view.Outline.Min.V;
            double f = w / (2 * Math.Tan(cameraInfo.HorizontalFov / 2.0));
            int H = Convert.ToInt32(h * W / w);
            para[0, 0] = -1 * W * f / w;
            para[0, 1] = 0;
            para[0, 2] = W / 2;
            para[1, 0] = 0;
            para[1, 1] = H * f / h;
            para[1, 2] = H / 2;
            para[2, 0] = 0;
            para[2, 1] = 0;
            para[2, 2] = 1;
            return para;
        }

        public Vector3D XYZToVector(XYZ xyz)
        {
            return new Vector3D(xyz.X, xyz.Y, xyz.Z);
        }

        public Point3D XYZToPoint(XYZ xyz)
        {
            return new Point3D(xyz.X, xyz.Y, xyz.Z);
        }
    }

    class viewFrustum
    {
        public Polyline3D nearPlane_VCS = new Polyline3D();  // outward outnormal
        public Polyline3D farPlane_VCS = new Polyline3D();  // outward outnormal
        public Polyline3D leftPlane_VCS = new Polyline3D();   // outward outnormal
        public Polyline3D rightPlane_VCS = new Polyline3D();  // outward outnormal
        public Polyline3D bottomPlane_VCS = new Polyline3D(); // outward outnormal
        public Polyline3D topPlane_VCS = new Polyline3D();  // outward outnormal
        public Point3D eyePosition_VCS = new Point3D();
        public Polyline3D projectionPlane_VCS = new Polyline3D();
        public List<Triangle3D> triFaces_VCS = new List<Triangle3D>();
        public BoundingBox3D AABB_VCS = new BoundingBox3D();

        public Point3D eyePosition_GCS = new Point3D();
        public List<Triangle3D> triFaces_GCS = new List<Triangle3D>();
        public BoundingBox3D AABB_GCS = new BoundingBox3D();

        public double w_projPlane;
        public double h_projPlane;
        public double depth_mm;

        // !!! WE REPLACE real nearPlane with projection Plane to construct real VF: near plane->projection plane->far plane
        public viewFrustum(View3D view, CameraLocalInfo cli)
        {
            GeometricOperations GO = new GeometricOperations();
            eyePosition_VCS = new Point3D(0.0, 0.0, 0.0);

            w_projPlane = view.Outline.Max.U - view.Outline.Min.U;
            h_projPlane = view.Outline.Max.V - view.Outline.Min.V;
            double z = -1 * w_projPlane / (2 * Math.Tan(cli.HorizontalFov / 2.0));

            // vertex order consistent with nearPlane_VCS = createPlaneBoundary(), which affect the determination of other planes
            projectionPlane_VCS.Vertices.Add(new Point3D(view.Outline.Max.U, view.Outline.Min.V, z));
            projectionPlane_VCS.Vertices.Add(new Point3D(view.Outline.Max.U, view.Outline.Max.V, z));
            projectionPlane_VCS.Vertices.Add(new Point3D(view.Outline.Min.U, view.Outline.Max.V, z));
            projectionPlane_VCS.Vertices.Add(new Point3D(view.Outline.Min.U, view.Outline.Min.V, z));
            projectionPlane_VCS.SurfaceNormal = new Vector3D(0, 0, 1);

            //this.nearPlane_VCS = createPlaneBoundary(cli.HorizontalFov, cli.VerticalFov, cli.DistanceToNearPlane, true);
            nearPlane_VCS.Vertices.AddRange(projectionPlane_VCS.Vertices);
            nearPlane_VCS.SurfaceNormal = new Vector3D(0, 0, 1);

            farPlane_VCS = createPlaneBoundary(cli.HorizontalFov, cli.VerticalFov, cli.DistanceToFarPlane, false);

            depth_mm = Math.Abs(farPlane_VCS.Vertices[0].z - nearPlane_VCS.Vertices[0].z) * 304.8;

            leftPlane_VCS.Vertices.Add(nearPlane_VCS.Vertices[3]);
            leftPlane_VCS.Vertices.Add(nearPlane_VCS.Vertices[2]);
            leftPlane_VCS.Vertices.Add(farPlane_VCS.Vertices[2]);
            leftPlane_VCS.Vertices.Add(farPlane_VCS.Vertices[1]);
            leftPlane_VCS.SurfaceNormal = GO.CalculateSurfaceNormal(leftPlane_VCS);

            rightPlane_VCS.Vertices.Add(nearPlane_VCS.Vertices[0]);
            rightPlane_VCS.Vertices.Add(farPlane_VCS.Vertices[0]);
            rightPlane_VCS.Vertices.Add(farPlane_VCS.Vertices[3]);
            rightPlane_VCS.Vertices.Add(nearPlane_VCS.Vertices[1]);
            rightPlane_VCS.SurfaceNormal = GO.CalculateSurfaceNormal(rightPlane_VCS);

            topPlane_VCS.Vertices.Add(nearPlane_VCS.Vertices[1]);
            topPlane_VCS.Vertices.Add(farPlane_VCS.Vertices[3]);
            topPlane_VCS.Vertices.Add(farPlane_VCS.Vertices[2]);
            topPlane_VCS.Vertices.Add(nearPlane_VCS.Vertices[2]);
            topPlane_VCS.SurfaceNormal = GO.CalculateSurfaceNormal(topPlane_VCS);

            bottomPlane_VCS.Vertices.Add(nearPlane_VCS.Vertices[0]);
            bottomPlane_VCS.Vertices.Add(nearPlane_VCS.Vertices[3]);
            bottomPlane_VCS.Vertices.Add(farPlane_VCS.Vertices[1]);
            bottomPlane_VCS.Vertices.Add(farPlane_VCS.Vertices[0]);
            bottomPlane_VCS.SurfaceNormal = GO.CalculateSurfaceNormal(bottomPlane_VCS);

            triFaces_VCS.AddRange(rectTriangulation(nearPlane_VCS));
            triFaces_VCS.AddRange(rectTriangulation(farPlane_VCS));
            triFaces_VCS.AddRange(rectTriangulation(leftPlane_VCS));
            triFaces_VCS.AddRange(rectTriangulation(rightPlane_VCS));
            triFaces_VCS.AddRange(rectTriangulation(topPlane_VCS));
            triFaces_VCS.AddRange(rectTriangulation(bottomPlane_VCS));

            List<Polyline3D> polys = new List<Polyline3D>();
            polys.Add(nearPlane_VCS);
            polys.Add(farPlane_VCS);
            AABB_VCS = GO.BoundingBox3D_Polygons_Create(polys);
        }

        public void transform2Global(double[,] TM_VCS_GCS)
        {
            GeometricOperations GO = new GeometricOperations();
            eyePosition_GCS = GO.PointTransf3D(eyePosition_VCS, TM_VCS_GCS);
            foreach (var tri in triFaces_VCS)
            {
                Triangle3D newTri = new Triangle3D();
                newTri = GO.triTransf(tri, TM_VCS_GCS);
                triFaces_GCS.Add(newTri);
            }
            AABB_GCS = GO.BoundingBox3D_Triangles_Create(triFaces_GCS, 0);

        }

        private List<Triangle3D> rectTriangulation(Polyline3D rect)
        {
            List<Triangle3D> result = new List<Triangle3D>();

            Triangle3D t1 = new Triangle3D();
            t1.Vertex1 = new Point3D(rect.Vertices[0].x, rect.Vertices[0].y, rect.Vertices[0].z);
            t1.Vertex2 = new Point3D(rect.Vertices[1].x, rect.Vertices[1].y, rect.Vertices[1].z);
            t1.Vertex3 = new Point3D(rect.Vertices[2].x, rect.Vertices[2].y, rect.Vertices[2].z);
            t1.NormalVector = rect.SurfaceNormal;
            result.Add(t1);

            Triangle3D t2 = new Triangle3D();
            t2.Vertex1 = new Point3D(rect.Vertices[2].x, rect.Vertices[2].y, rect.Vertices[2].z);
            t2.Vertex2 = new Point3D(rect.Vertices[3].x, rect.Vertices[3].y, rect.Vertices[3].z);
            t2.Vertex3 = new Point3D(rect.Vertices[0].x, rect.Vertices[0].y, rect.Vertices[0].z);
            t2.NormalVector = rect.SurfaceNormal;
            result.Add(t2);

            return result;
        }

        private Polyline3D createPlaneBoundary(double hFOV, double vFOV, double dist, bool nearPlane)
        {
            Polyline3D result = new Polyline3D();

            double z = -1 * dist;
            double xmax = dist * Math.Tan(hFOV / 2.0);
            double ymax = dist * Math.Tan(vFOV / 2.0);

            if (nearPlane)
            {
                result.SurfaceNormal = new Vector3D(0, 0, 1);
                result.Vertices.Add(new Point3D(xmax, -1 * ymax, z));
                result.Vertices.Add(new Point3D(xmax, ymax, z));
                result.Vertices.Add(new Point3D(-1 * xmax, ymax, z));
                result.Vertices.Add(new Point3D(-1 * xmax, -1 * ymax, z));
            }
            else
            {
                result.SurfaceNormal = new Vector3D(0, 0, -1);
                result.Vertices.Add(new Point3D(xmax, -1 * ymax, z));
                result.Vertices.Add(new Point3D(-1 * xmax, -1 * ymax, z));
                result.Vertices.Add(new Point3D(-1 * xmax, ymax, z));
                result.Vertices.Add(new Point3D(xmax, ymax, z));
            }

            return result;
        }
    }
}
