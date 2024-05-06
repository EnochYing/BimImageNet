using System;
using System.Collections.Generic;
using System.Linq;


namespace BimImageNet

{
    public class Point3D
    {
        public double x;
        public double y;
        public double z;

        public Point3D()
        {

        }
        public Point3D(double X, double Y, double Z)
        {
            this.x = X;
            this.y = Y;
            this.z = Z;
        }

        public bool IsSamePoint(Point3D PointA, Point3D PointB, double tolerance_mm)
        {
            double dDeff_X = Math.Abs(PointB.x - PointA.x);
            double dDeff_Y = Math.Abs(PointB.y - PointA.y);
            double dDeff_Z = Math.Abs(PointB.z - PointA.z);

            if ((dDeff_X < tolerance_mm) && (dDeff_Y < tolerance_mm))
            {
                if (dDeff_Z < tolerance_mm)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
                return false;
        }

        public double Dist(Point3D p1, Point3D p2)
        {
            double result;

            double diffx = (p1.x - p2.x) * (p1.x - p2.x);
            double diffy = (p1.y - p2.y) * (p1.y - p2.y);
            double diffz = (p1.z - p2.z) * (p1.z - p2.z);

            result = Math.Sqrt(diffx + diffy + diffz);

            return result;
        }
    }

    public class Point2D
    {
        public double x;
        public double y;

        public Point2D()
        {

        }
        public Point2D(double x, double y)
        {
            this.x = x;
            this.y = y;
        }


    }

    public class Ray3D
    {
        public Point3D StartPoint = new Point3D();
        public Vector3D Direction = new Vector3D();
    }

    public class Vector3D
    {
        public double x = 0;
        public double y = 0;
        public double z = 0;

        public Vector3D()
        {

        }

        public Vector3D(double a, double b, double c)
        {
            this.x = a;
            this.y = b;
            this.z = c;
        }

        public Vector3D(Point3D a, Point3D b)
        {
            this.x = b.x - a.x;
            this.y = b.y - a.y;
            this.z = b.z - a.z;
        }

        // Dot product
        // When the A is a unit vector and B represents a vertex then Dot Product represents the projection of B on the A axis 
        public double DotProduct(Vector3D A, Vector3D B)
        {
            double DP = 0;
            DP = A.x * B.x + A.y * B.y + A.z * B.z;

            return DP;
        }

        public Vector3D VectorConstructor(Point3D A, Point3D B)
        {
            Vector3D _Vector = new Vector3D();
            _Vector.x = B.x - A.x;
            _Vector.y = B.y - A.y;
            _Vector.z = B.z - A.z;
            return _Vector;
        }

        // create unit vector to represent the axis direction
        public Vector3D UnitVector(Vector3D _Point3D)
        {
            Vector3D _Axis = new Vector3D();
            double k = Math.Sqrt(_Point3D.x * _Point3D.x + _Point3D.y * _Point3D.y + _Point3D.z * _Point3D.z);
            if (k != 0)
            {
                _Axis.x = _Point3D.x / k;
                _Axis.y = _Point3D.y / k;
                _Axis.z = _Point3D.z / k;
                return _Axis;
            }
            else return _Point3D;
        }

        public Vector3D Multiple(Vector3D A, double b)
        {
            Vector3D result = new Vector3D();

            result.x = A.x * b;
            result.y = A.y * b;
            result.z = A.z * b;

            return result;
        }

        public Vector3D Addition (Vector3D A, Vector3D B)
        {
            Vector3D result = new Vector3D();

            result.x = A.x + B.x ;
            result.y = A.y + B.y;
            result.z = A.z + B.z;

            return result;
        }

    }

    public class Triangle3D
    {
        public Point3D Vertex1 = new Point3D();
        public Point3D Vertex2 = new Point3D();
        public Point3D Vertex3 = new Point3D();
        public Vector3D NormalVector = new Vector3D();

        public string GUID;
        public string Name;
        public string Category;
        public string Mark;
        public string objectType;
        
        public string materialName;
        public string partNo;

        public List<double> getCentroid()
        {
            List<double> result = new List<double>();
            result.Add((Vertex1.x + Vertex2.x + Vertex3.x) / 3);
            result.Add((Vertex1.y + Vertex2.y + Vertex3.y) / 3);
            result.Add((Vertex1.z + Vertex2.z + Vertex3.z) / 3);
            return result;
        }
    } 

    public class BoundingBox3D
    {
        public double xmin;
        public double ymin;
        public double zmin;
        public double xmax;
        public double ymax;
        public double zmax;

        public BoundingBox3D()
        {

        }

        public double getSurfaceArea()
        {
            double x = xmax - xmin;
            double y = ymax - ymin;
            double z = zmax - zmin;

            return 2 * (x * y + x * z + y * z);
        }
    }

    public class BoundingBox2D
    {
        public double xmin;
        public double ymin;
        public double xmax;
        public double ymax;

        public BoundingBox2D()
        {

        }
    }

    public class Polyline3D
    {
        public List<Point3D> Vertices = new List<Point3D>();
        public Vector3D SurfaceNormal = new Vector3D(); // should be unit vector        
    }

    public class Polyline2D
    {
        public List<Point2D> Vertices = new List<Point2D>();    
    }

    public class BVH_triangle
    {
        public BVHNode_triangle RootNode = new BVHNode_triangle();

        public BVH_triangle()
        {

        }

        public BVH_triangle(List<Triangle3D> Objects, int MaxDepth, BVH_spiltStrategy strategy)
        {
            GeometricOperations GO = new GeometricOperations();

            // 1. create root node
            BoundingBox3D RootBox = GO.BoundingBox3D_Triangles_Create(Objects, 0);

            List<BVHNode_triangle> TreeNodes = new List<BVHNode_triangle>();
            RootNode.AABB = RootBox;
            RootNode.Depth = 1;
            RootNode.Objects = Objects; // This will be reset to null when it is spilt 
            TreeNodes.Add(RootNode);

            // 2. add the children node          
            bool DoSpilt = true;
            while (DoSpilt)
            {
                DoSpilt = false;
                for (int i = 0; i < TreeNodes.Count; i++) // Cannot use foreach as TreeNodes is dynamic
                {
                    if (TreeNodes[i].LeftChildren == null && TreeNodes[i].RightChildren == null) // MUS be kept
                    {
                        if (TreeNodes[i].Depth < MaxDepth)  // Terminal condition 1
                        {
                            if (TreeNodes[i].Objects.Count >= 2) // Terminal condition 2 (after spilt, the TriangulatedSBs is set to null) (when NUM == 2 this node is not necessary to be spilt as they probaly come from a common SB which means they actually will be not spilt in spilt.())
                            {
                                double parentCost = 0;
                                if (strategy == BVH_spiltStrategy.SAH)
                                    parentCost = TreeNodes[i].AABB.getSurfaceArea() * TreeNodes[i].Objects.Count();
                                if (TreeNodes[i].Split(strategy, parentCost))
                                {
                                    if (TreeNodes[i].LeftChildren != null)
                                        TreeNodes.Add(TreeNodes[i].LeftChildren);
                                    if (TreeNodes[i].RightChildren != null)
                                        TreeNodes.Add(TreeNodes[i].RightChildren);
                                    DoSpilt = true;
                                }
                            }
                        }
                    }
                }
            }
        }

        public BVH_triangle(List<Triangle3D> Objects, BoundingBox3D RootBox, int MaxDepth, BVH_spiltStrategy strategy)
        {
            GeometricOperations GO = new GeometricOperations();

            // 1. create root node
            List<BVHNode_triangle> TreeNodes = new List<BVHNode_triangle>();
            RootNode.AABB = RootBox;
            RootNode.Depth = 1;
            RootNode.Objects = Objects; // This will be reset to null when it is spilt 
            TreeNodes.Add(RootNode);

            // 2. add the children node          
            bool DoSpilt = true;
            while (DoSpilt)
            {
                DoSpilt = false;
                for (int i = 0; i < TreeNodes.Count; i++) // Cannot use foreach as TreeNodes is dynamic
                {
                    if (TreeNodes[i].LeftChildren == null && TreeNodes[i].RightChildren == null) // MUS be kept
                    {
                        if (TreeNodes[i].Depth < MaxDepth)  // Terminal condition 1
                        {
                            if (TreeNodes[i].Objects.Count >= 2) // Terminal condition 2 (after spilt, the TriangulatedSBs is set to null) (when NUM == 2 this node is not necessary to be spilt as they probaly come from a common SB which means they actually will be not spilt in spilt.())
                            {
                                double parentCost = 0;
                                if (strategy == BVH_spiltStrategy.SAH)
                                    parentCost = TreeNodes[i].AABB.getSurfaceArea() * TreeNodes[i].Objects.Count();
                                if (TreeNodes[i].Split(strategy, parentCost))
                                {
                                    if (TreeNodes[i].LeftChildren != null)
                                        TreeNodes.Add(TreeNodes[i].LeftChildren);
                                    if (TreeNodes[i].RightChildren != null)
                                        TreeNodes.Add(TreeNodes[i].RightChildren);
                                    DoSpilt = true;
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    public class BVHNode_triangle
    {
        public BoundingBox3D AABB = null;
        public List<Triangle3D> Objects = new List<Triangle3D>(); // for leaf nodes only
        public int Depth = 0;
        public BVHNode_triangle LeftChildren = null;
        public BVHNode_triangle RightChildren = null;

        public bool Split(BVH_spiltStrategy strategy, double parentCost)
        {
            if (this.LeftChildren != null || this.RightChildren != null)
                return false;
            spiltPlane sp = new spiltPlane();
            double location = 0;
            if (strategy == BVH_spiltStrategy.middlepoint)
                splitStrategy_midpoint(out sp, out location);
            else if (strategy == BVH_spiltStrategy.SAH)
            {
                double bestCost = splitStrategy_SAH(out sp, out location);
                if (bestCost >= parentCost)
                    return false;
            }      

            // create the left and right children: left--> smaller; right --> bigger
            List<Triangle3D> Objects_Left = new List<Triangle3D>();
            List<Triangle3D> Objects_Right = new List<Triangle3D>();
            ClassifyObjects(this.Objects, sp, location, ref Objects_Left, ref Objects_Right);

            if (Objects_Left.Count == 0 || Objects_Right.Count == 0) // !!! if any children node is empty, this mode MUST not be spilt as the child is same to the parent. 
                return false;
            else
            {
                GeometricOperations GO = new GeometricOperations();

                this.LeftChildren = new BVHNode_triangle(); // Null-->Initilaization 
                this.LeftChildren.AABB = GO.BoundingBox3D_Triangles_Create(Objects_Left, 0);
                this.LeftChildren.Depth = this.Depth + 1;
                this.LeftChildren.Objects = Objects_Left;

                this.RightChildren = new BVHNode_triangle(); // Null-->Initilaization 
                this.RightChildren.AABB = GO.BoundingBox3D_Triangles_Create(Objects_Right, 0);
                this.RightChildren.Depth = this.Depth + 1;
                this.RightChildren.Objects = Objects_Right;

                this.Objects = null; // only leaf nodes reserve the TSBs (once spilt, set empty)
                return true;  // spilt successfully 
            }
        }

        private double splitStrategy_SAH(out spiltPlane sp, out double spiltlocation)
        {
            int bestAxis = -1;
            double bestPos = 0;
            double bestCost = 1e30f;

            for (int axis = 0; axis < 3; axis++)
            {
                for (int i = 0; i < Objects.Count(); i++)
                {
                    double candidatePos = Objects[i].getCentroid()[axis];
                    double cost = EvaluateSAH(axis, candidatePos);
                    if (cost < bestCost)
                    {
                        bestPos = candidatePos;
                        bestAxis = axis;
                        bestCost = cost;
                    }
                }
            }
            if (bestAxis == 0)
                sp = spiltPlane.xAxis;
            else if (bestAxis == 1)
                sp = spiltPlane.yAxis;
            else
                sp = spiltPlane.zAxis;
            spiltlocation = bestPos;

            return bestCost;
        }

        private double EvaluateSAH(int axis, double pos)
        {
            GeometricOperations GO = new GeometricOperations();
            List<Triangle3D> left = new List<Triangle3D>();
            List<Triangle3D> right = new List<Triangle3D>();

            foreach (var tri in Objects)
            {
                if (tri.getCentroid()[axis] < pos)
                    left.Add(tri);
                else
                    right.Add(tri);
            }

            BoundingBox3D leftbox = GO.BoundingBox3D_Triangles_Create(left, 0);
            BoundingBox3D rightbox = GO.BoundingBox3D_Triangles_Create(right, 0);

            double cost = left.Count() * leftbox.getSurfaceArea() + right.Count() * rightbox.getSurfaceArea();
            return cost > 0 ? cost : 1e30f;
        }

        private void splitStrategy_midpoint(out spiltPlane sp, out double spiltlocation)
        {
            sp = new spiltPlane();
            double Axisx = AABB.xmax - AABB.xmin;
            double Axisy = AABB.ymax - AABB.ymin;
            double Axisz = AABB.zmax - AABB.zmin;

            if (Axisx > Axisy)
            {
                if (Axisx > Axisz)
                    sp = spiltPlane.xAxis;
                else
                    sp = spiltPlane.zAxis;
            }
            else
            {
                if (Axisz > Axisy)
                    sp = spiltPlane.zAxis;
                else
                    sp = spiltPlane.yAxis;
            }

            spiltlocation = 0;
            if (sp == spiltPlane.xAxis)
                spiltlocation = Axisx / 2.0 + AABB.xmin;
            if (sp == spiltPlane.yAxis)
                spiltlocation = Axisy / 2.0 + AABB.ymin;
            if (sp == spiltPlane.zAxis)
                spiltlocation = Axisz / 2.0 + AABB.zmin;
        }

        public List<BVHNode_triangle> BVHTraversing(Ray3D ray)
        {
            List<BVHNode_triangle> LeafNodes = new List<BVHNode_triangle>();

            GeometricOperations GO = new GeometricOperations();
            bool Intersection = GO.Ray_AABB_do_intersection(ray, this.AABB);

            if (Intersection)
            {
                if ((this.LeftChildren == null) && (this.RightChildren == null))
                    LeafNodes.Add(this);
                else
                {
                    if (this.LeftChildren != null)
                    {
                        List<BVHNode_triangle> LeftNodes = this.LeftChildren.BVHTraversing(ray);
                        for (int i = 0; i < LeftNodes.Count; i++)
                            LeafNodes.Add(LeftNodes[i]);
                    }
                    if (this.RightChildren != null)
                    {
                        List<BVHNode_triangle> RightNodes = this.RightChildren.BVHTraversing(ray);
                        for (int j = 0; j < RightNodes.Count; j++)
                            LeafNodes.Add(RightNodes[j]);
                    }
                }
            }
            return LeafNodes;
        }

        public List<BVHNode_triangle> BVHTraversing(Plane pla, Ray3D ray)
        {
            List<BVHNode_triangle> LeafNodes = new List<BVHNode_triangle>();

            bool keep = true;
            if (pla == Plane.xyplane)
            {
                if (ray.StartPoint.z > this.AABB.zmax || ray.StartPoint.z < this.AABB.zmin)
                    keep = false;
            }
            else if (pla == Plane.xzplane)
            {
                if (ray.StartPoint.y > this.AABB.ymax || ray.StartPoint.y < this.AABB.ymin)
                    keep = false;
            }
            else if (pla == Plane.yzplane)
            {
                if (ray.StartPoint.x > this.AABB.xmax || ray.StartPoint.x < this.AABB.xmin)
                    keep = false;
            }

            if (keep)
            {
                GeometricOperations GO = new GeometricOperations();
                bool Intersection = GO.Ray_AABB_do_intersection(ray, this.AABB);

                if (Intersection)
                {
                    if ((this.LeftChildren == null) && (this.RightChildren == null))
                        LeafNodes.Add(this);
                    else
                    {
                        if (this.LeftChildren != null)
                        {
                            List<BVHNode_triangle> LeftNodes = this.LeftChildren.BVHTraversing(pla, ray);
                            for (int i = 0; i < LeftNodes.Count; i++)
                                LeafNodes.Add(LeftNodes[i]);
                        }
                        if (this.RightChildren != null)
                        {
                            List<BVHNode_triangle> RightNodes = this.RightChildren.BVHTraversing(pla, ray);
                            for (int j = 0; j < RightNodes.Count; j++)
                                LeafNodes.Add(RightNodes[j]);
                        }
                    }
                }
            }
            return LeafNodes;
        }

        public List<BVHNode_triangle> BVHTraversing(BoundingBox3D aabb)
        {
            List<BVHNode_triangle> LeafNodes = new List<BVHNode_triangle>();

            bool Intersection = AABB_AABB_IntersectionTest(aabb, this.AABB);

            if (Intersection)
            {
                if ((this.LeftChildren == null) && (this.RightChildren == null))
                    LeafNodes.Add(this);
                else
                {
                    if (this.LeftChildren != null)
                    {
                        List<BVHNode_triangle> LeftNodes = this.LeftChildren.BVHTraversing(aabb);
                        for (int i = 0; i < LeftNodes.Count; i++)
                            LeafNodes.Add(LeftNodes[i]);
                    }
                    if (this.RightChildren != null)
                    {
                        List<BVHNode_triangle> RightNodes = this.RightChildren.BVHTraversing(aabb);
                        for (int j = 0; j < RightNodes.Count; j++)
                            LeafNodes.Add(RightNodes[j]);
                    }
                }
            }
            return LeafNodes;
        }

        // left--> smaller; right --> bigger
        private void ClassifyObjects(List<Triangle3D> Objects, spiltPlane spilt, double spiltlocation, ref List<Triangle3D> Objects_Left, ref List<Triangle3D> Objects_right)
        {
            if (Objects.Count != 0)
            {
                foreach (var element in Objects)
                {
                    double mid = 0;
                    switch (spilt)
                    {
                        case spiltPlane.xAxis:
                            List<double> list = new List<double>();
                            list.Add(element.Vertex1.x);
                            list.Add(element.Vertex2.x);
                            list.Add(element.Vertex3.x);
                            mid = (list.Max() + list.Min()) / 2.0;
                            if (mid < spiltlocation)
                                Objects_Left.Add(element);
                            else
                                Objects_right.Add(element);
                            break;
                        case spiltPlane.yAxis:
                            List<double> list1 = new List<double>();
                            list1.Add(element.Vertex1.y);
                            list1.Add(element.Vertex2.y);
                            list1.Add(element.Vertex3.y);
                            mid = (list1.Max() + list1.Min()) / 2.0;
                            if (mid < spiltlocation)
                                Objects_Left.Add(element);
                            else
                                Objects_right.Add(element);
                            break;
                        case spiltPlane.zAxis:
                            List<double> list2 = new List<double>();
                            list2.Add(element.Vertex1.z);
                            list2.Add(element.Vertex2.z);
                            list2.Add(element.Vertex3.z);
                            mid = (list2.Max() + list2.Min()) / 2.0;
                            if (mid < spiltlocation)
                                Objects_Left.Add(element);
                            else
                                Objects_right.Add(element);
                            break;
                    }
                }
            }
        }

        private bool AABB_AABB_IntersectionTest(BoundingBox3D A, BoundingBox3D B)
        {
            if (A.xmax <= B.xmin || A.xmin >= B.xmax)
                return false;
            if (A.ymax <= B.ymin || A.ymin >= B.ymax)
                return false;
            if (A.zmax <= B.zmin || A.zmin >= B.zmax)
                return false;
            return true;
        }
    }

    public enum BVH_spiltStrategy
    {
        middlepoint,
        SAH
    }





    class BVH
    {
        public BVHNode RootNode = new BVHNode();

        public BVH()
        {

        }

        public BVH(List<BIMObject> Objects, int MaxDepth)
        {
            GeometricOperations GO = new GeometricOperations();

            // 1. create root node
            List<BoundingBox3D> AABBs = new List<BoundingBox3D>();
            foreach (var item in Objects)
                AABBs.Add(item.triBrep.AABB);
            BoundingBox3D RootBox = GO.BoundingBox3D_AABBs_Create(AABBs);

            List<BVHNode> TreeNodes = new List<BVHNode>();
            RootNode.AABB = RootBox;
            RootNode.Depth = 1;
            RootNode.Objects = Objects; // This will be reset to null when it is spilt 
            TreeNodes.Add(RootNode);

            // 2. add the children node          
            bool DoSpilt = true;
            while (DoSpilt)
            {
                DoSpilt = false;
                for (int i = 0; i < TreeNodes.Count; i++) // Cannot use foreach as TreeNodes is dynamic
                {
                    if (TreeNodes[i].LeftChildren == null && TreeNodes[i].RightChildren == null) // MUS be kept
                    {
                        if (TreeNodes[i].Depth < MaxDepth)  // Terminal condition 1
                        {
                            if (TreeNodes[i].Objects.Count >= 2) // Terminal condition 2 (after spilt, the TriangulatedSBs is set to null) (when NUM == 2 this node is not necessary to be spilt as they probaly come from a common SB which means they actually will be not spilt in spilt.())
                            {
                                if (TreeNodes[i].Split_Elements_median())
                                {
                                    if (TreeNodes[i].LeftChildren != null)
                                        TreeNodes.Add(TreeNodes[i].LeftChildren);
                                    if (TreeNodes[i].RightChildren != null)
                                        TreeNodes.Add(TreeNodes[i].RightChildren);
                                    DoSpilt = true;
                                }
                            }
                        }
                    }
                }
            }
        }

        public BVH(List<BIMObject> Objects, BoundingBox3D RootBox, int MaxDepth)
        {
            GeometricOperations GO = new GeometricOperations();

            // 1. create root node
            List<BVHNode> TreeNodes = new List<BVHNode>();
            RootNode.AABB = RootBox;
            RootNode.Depth = 1;
            RootNode.Objects = Objects; // This will be reset to null when it is spilt 
            TreeNodes.Add(RootNode);

            // 2. add the children node          
            bool DoSpilt = true;
            while (DoSpilt)
            {
                DoSpilt = false;
                for (int i = 0; i < TreeNodes.Count; i++) // Cannot use foreach as TreeNodes is dynamic
                {
                    if (TreeNodes[i].LeftChildren == null && TreeNodes[i].RightChildren == null) // MUS be kept
                    {
                        if (TreeNodes[i].Depth < MaxDepth)  // Terminal condition 1
                        {
                            if (TreeNodes[i].Objects.Count >= 2) // Terminal condition 2 (after spilt, the TriangulatedSBs is set to null) (when NUM == 2 this node is not necessary to be spilt as they probaly come from a common SB which means they actually will be not spilt in spilt.())
                            {
                                if (TreeNodes[i].Split_Elements_median())
                                {
                                    if (TreeNodes[i].LeftChildren != null)
                                        TreeNodes.Add(TreeNodes[i].LeftChildren);
                                    if (TreeNodes[i].RightChildren != null)
                                        TreeNodes.Add(TreeNodes[i].RightChildren);
                                    DoSpilt = true;
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    class BVHNode
    {
        public BoundingBox3D AABB = null;
        public List<BIMObject> Objects = new List<BIMObject>(); // for leaf nodes only
        public int Depth = 0;
        public BVHNode LeftChildren = null;
        public BVHNode RightChildren = null;


        public bool Split_Elements_median()
        {
            if (this.LeftChildren != null || this.RightChildren != null)
                return false;

            // select the spilt axis
            spiltPlane spilt = new spiltPlane();
            double Axisx = AABB.xmax - AABB.xmin;
            double Axisy = AABB.ymax - AABB.ymin;
            double Axisz = AABB.zmax - AABB.zmin;

            if (Axisx > Axisy)
            {
                if (Axisx > Axisz)
                    spilt = spiltPlane.xAxis;
                else
                    spilt = spiltPlane.zAxis;
            }
            else
            {
                if (Axisz > Axisy)
                    spilt = spiltPlane.zAxis;
                else
                    spilt = spiltPlane.yAxis;
            }

            // create the left and right children: left--> smaller; right --> bigger
            List<BIMObject> Objects_Left = new List<BIMObject>();
            List<BIMObject> Objects_Right = new List<BIMObject>();
            double spiltlocation = 0;

            if (spilt == spiltPlane.xAxis)
                spiltlocation = Axisx / 2.0 + AABB.xmin;
            if (spilt == spiltPlane.yAxis)
                spiltlocation = Axisy / 2.0 + AABB.ymin;
            if (spilt == spiltPlane.zAxis)
                spiltlocation = Axisz / 2.0 + AABB.zmin;
            ClassifyObjects(this.Objects, spilt, spiltlocation, ref Objects_Left, ref Objects_Right);


            if (Objects_Left.Count == 0 || Objects_Right.Count == 0) // !!! if any children node is empty, this mode MUST not be spilt as the child is same to the parent. 
                return false;
            else
            {
                this.LeftChildren = new BVHNode(); // Null-->Initilaization 
                this.LeftChildren.AABB = CreateAABB_Objects(Objects_Left);
                this.LeftChildren.Depth = this.Depth + 1;
                this.LeftChildren.Objects = Objects_Left;

                this.RightChildren = new BVHNode(); // Null-->Initilaization 
                this.RightChildren.AABB = CreateAABB_Objects(Objects_Right);
                this.RightChildren.Depth = this.Depth + 1;
                this.RightChildren.Objects = Objects_Right;

                this.Objects = null; // only leaf nodes reserve the TSBs (once spilt, set empty)
                return true;  // spilt successfully 
            }
        }

        public List<BVHNode> BVHTraversing(Ray3D ray)
        {
            List<BVHNode> LeafNodes = new List<BVHNode>();

            GeometricOperations GO = new GeometricOperations();
            bool Intersection = GO.Ray_AABB_do_intersection(ray, this.AABB);

            if (Intersection)
            {
                if ((this.LeftChildren == null) && (this.RightChildren == null))
                    LeafNodes.Add(this);
                else
                {
                    if (this.LeftChildren != null)
                    {
                        List<BVHNode> LeftNodes = this.LeftChildren.BVHTraversing(ray);
                        for (int i = 0; i < LeftNodes.Count; i++)
                            LeafNodes.Add(LeftNodes[i]);
                    }
                    if (this.RightChildren != null)
                    {
                        List<BVHNode> RightNodes = this.RightChildren.BVHTraversing(ray);
                        for (int j = 0; j < RightNodes.Count; j++)
                            LeafNodes.Add(RightNodes[j]);
                    }
                }
            }
            return LeafNodes;
        }

        public List<BVHNode> BVHTraversing(Plane pla, Ray3D ray)
        {
            List<BVHNode> LeafNodes = new List<BVHNode>();

            bool keep = true;
            if (pla == Plane.xyplane)
            {
                if (ray.StartPoint.z > this.AABB.zmax || ray.StartPoint.z < this.AABB.zmin)
                    keep = false;
            }
            else if (pla == Plane.xzplane)
            {
                if (ray.StartPoint.y > this.AABB.ymax || ray.StartPoint.y < this.AABB.ymin)
                    keep = false;
            }
            else if (pla == Plane.yzplane)
            {
                if (ray.StartPoint.x > this.AABB.xmax || ray.StartPoint.x < this.AABB.xmin)
                    keep = false;
            }

            if (keep)
            {
                GeometricOperations GO = new GeometricOperations();
                bool Intersection = GO.Ray_AABB_do_intersection(ray, this.AABB);

                if (Intersection)
                {
                    if ((this.LeftChildren == null) && (this.RightChildren == null))
                        LeafNodes.Add(this);
                    else
                    {
                        if (this.LeftChildren != null)
                        {
                            List<BVHNode> LeftNodes = this.LeftChildren.BVHTraversing(pla, ray);
                            for (int i = 0; i < LeftNodes.Count; i++)
                                LeafNodes.Add(LeftNodes[i]);
                        }
                        if (this.RightChildren != null)
                        {
                            List<BVHNode> RightNodes = this.RightChildren.BVHTraversing(pla, ray);
                            for (int j = 0; j < RightNodes.Count; j++)
                                LeafNodes.Add(RightNodes[j]);
                        }
                    }
                }
            }
            return LeafNodes;
        }

        public List<BVHNode> BVHTraversing(BoundingBox3D aabb)
        {
            List<BVHNode> LeafNodes = new List<BVHNode>();

            bool Intersection = AABB_AABB_IntersectionTest(aabb, this.AABB);

            if (Intersection)
            {
                if ((this.LeftChildren == null) && (this.RightChildren == null))
                    LeafNodes.Add(this);
                else
                {
                    if (this.LeftChildren != null)
                    {
                        List<BVHNode> LeftNodes = this.LeftChildren.BVHTraversing(aabb);
                        for (int i = 0; i < LeftNodes.Count; i++)
                            LeafNodes.Add(LeftNodes[i]);
                    }
                    if (this.RightChildren != null)
                    {
                        List<BVHNode> RightNodes = this.RightChildren.BVHTraversing(aabb);
                        for (int j = 0; j < RightNodes.Count; j++)
                            LeafNodes.Add(RightNodes[j]);
                    }
                }
            }
            return LeafNodes;
        }

        // left--> smaller; right --> bigger
        private void ClassifyObjects(List<BIMObject> Objects, spiltPlane spilt, double spiltlocation, ref List<BIMObject> Objects_Left, ref List<BIMObject> Objects_right)
        {
            if (Objects.Count != 0)
            {
                foreach (var element in Objects)
                {
                    double mid = 0;
                    switch (spilt)
                    {
                        case spiltPlane.xAxis:
                            mid = (element.triBrep.AABB.xmin + element.triBrep.AABB.xmax) / 2.0;
                            if (mid < spiltlocation)
                                Objects_Left.Add(element);
                            else
                                Objects_right.Add(element);
                            break;
                        case spiltPlane.yAxis:
                            mid = (element.triBrep.AABB.ymin + element.triBrep.AABB.ymax) / 2.0;
                            if (mid < spiltlocation)
                                Objects_Left.Add(element);
                            else
                                Objects_right.Add(element);
                            break;
                        case spiltPlane.zAxis:
                            mid = (element.triBrep.AABB.zmin + element.triBrep.AABB.zmax) / 2.0;
                            if (mid < spiltlocation)
                                Objects_Left.Add(element);
                            else
                                Objects_right.Add(element);
                            break;
                    }
                }
            }
        }

        private BoundingBox3D CreateAABB_Objects(List<BIMObject> Objects)
        {
            GeometricOperations GO = new GeometricOperations();

            List<BoundingBox3D> AABBs = new List<BoundingBox3D>();
            foreach (var e in Objects)
                AABBs.Add(e.triBrep.AABB);

            BoundingBox3D AABB = GO.BoundingBox3D_AABBs_Create(AABBs);
            return AABB;
        }

        private bool AABB_AABB_IntersectionTest(BoundingBox3D A, BoundingBox3D B)
        {
            if (A.xmax <= B.xmin || A.xmin >= B.xmax)
                return false;
            if (A.ymax <= B.ymin || A.ymin >= B.ymax)
                return false;
            if (A.zmax <= B.zmin || A.zmin >= B.zmax)
                return false;
            return true;
        }
    }

    public enum spiltPlane
    {
        xAxis,
        yAxis,
        zAxis
    }

    public class TriangulatedBrep
    {
        public List<List<Triangle3D>> TriangleFaces = new List<List<Triangle3D>>();  // normal have been checked and can be used 
        public BoundingBox3D AABB = new BoundingBox3D();
        public List<Triangle3D> Triangles = new List<Triangle3D>();  // normal have not been checked        

    }
}
