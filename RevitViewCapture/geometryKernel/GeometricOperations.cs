using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.LinearAlgebra;
using System.IO;


namespace BimImageNet
{
    public class GeometricOperations
    {
        private double coordDiff = 0.00001 /** 0.0032808*/;
        // private int decimalDigit = 10;
        // private double distDiff = 0.00001 * 0.0032808; // mm
        // private double angDiff = 0.000001;  //0.1 du
        public Polyline3D formTransf(Triangle3D tri)
        {
            Polyline3D poly = new Polyline3D();
            poly.Vertices.Add(tri.Vertex1);
            poly.Vertices.Add(tri.Vertex2);
            poly.Vertices.Add(tri.Vertex3);
            poly.SurfaceNormal = tri.NormalVector;

            return poly;
        }

        public List<Polyline3D> formTransf(List<Triangle3D> triangles)
        {
            List<Polyline3D> result = new List<Polyline3D>();
            foreach (var tri in triangles)
            {
                Polyline3D poly = new Polyline3D();
                poly.Vertices.Add(tri.Vertex1);
                poly.Vertices.Add(tri.Vertex2);
                poly.Vertices.Add(tri.Vertex3);
                poly.SurfaceNormal = tri.NormalVector;

                result.Add(poly);
            }
            return result;
        } 

        public Polyline3D ConvertTriangleToPolyline(Triangle3D tri)
        {
            Polyline3D poly = new Polyline3D();
            poly.SurfaceNormal = tri.NormalVector;
            poly.Vertices.Add(tri.Vertex1);
            poly.Vertices.Add(tri.Vertex2);
            poly.Vertices.Add(tri.Vertex3);

            return poly;
        }

        public Point3D polyCentroid(Polyline3D poly)
        {
            Point3D result = new Point3D();
            double x = 0, y = 0, z = 0;
            foreach (var v in poly.Vertices)
            {
                x += v.x;
                y += v.y;
                z += v.z;
            }

            result.x = x / Convert.ToDouble(poly.Vertices.Count);
            result.y = y / Convert.ToDouble(poly.Vertices.Count);
            result.z = z / Convert.ToDouble(poly.Vertices.Count);

            return result;
        }

        public Polyline3D polyTransf (Polyline3D poly, double[,] TM)
        {
            Polyline3D result = new Polyline3D();
            foreach (var vertex in poly.Vertices)
            {
                Point3D p3D = PointTransf3D(vertex, TM);
                result.Vertices.Add(p3D);
            }

            result.SurfaceNormal = SurfaceNormalTransf(poly.SurfaceNormal, TM);

            return result;
        }

        public Triangle3D triTransf(Triangle3D tri, double[,] TM)
        {
            Triangle3D result = new Triangle3D();

            result.Vertex1 = PointTransf3D(tri.Vertex1, TM);
            result.Vertex2 = PointTransf3D(tri.Vertex2, TM);
            result.Vertex3 = PointTransf3D(tri.Vertex3, TM);

            result.NormalVector = SurfaceNormalTransf(tri.NormalVector, TM);

            return result;
        }

        public BoundingBox3D BoxTransf(BoundingBox3D box, double[,] TM)
        {
            BoundingBox3D result = new BoundingBox3D();

            Point3D min = new Point3D(box.xmin, box.ymin, box.zmin);
            Point3D max = new Point3D(box.xmax, box.ymax, box.zmax);

            Point3D _min = PointTransf3D(min, TM);
            result.xmin = _min.x;
            result.ymin = _min.y;
            result.zmin = _min.z;

            Point3D _max = PointTransf3D(max, TM);
            result.xmax = _max.x;
            result.ymax = _max.y;
            result.zmax = _max.z;
            return result;
        }

        public Polyline2D polyTransf2D(Polyline3D poly, double[,] TM)
        {
            Polyline2D result = new Polyline2D();
            foreach (var vertex in poly.Vertices)
            {
                Point3D p3D = PointTransf3D(vertex, TM);
                Point2D p2D = NewPoint(p3D.x, p3D.y);
                result.Vertices.Add(p2D);
            }
            return result;
        }

        public Point2D NewPoint (double x, double y)
        {
            Point2D p = new Point2D();
            p.x = x;
            p.y = y;
            return p;
        }

        public Vector3D SurfaceNormalTransf(Vector3D SurfaceNormal, double[,] TRMatrix)
        {
            Vector3D _Vector3D = new Vector3D();
            Point3D LCSOrigin = new Point3D();
            LCSOrigin.x = 0;
            LCSOrigin.y = 0;
            LCSOrigin.z = 0;

            Point3D _SurfaceNormal = new Point3D();
            _SurfaceNormal.x = SurfaceNormal.x;
            _SurfaceNormal.y = SurfaceNormal.y;
            _SurfaceNormal.z = SurfaceNormal.z;

            Point3D LCSOriginTransfer = new Point3D();
            Point3D _SurfaceNormalTransfer = new Point3D();
            LCSOriginTransfer = PointTransf3D(LCSOrigin, TRMatrix);
            _SurfaceNormalTransfer = PointTransf3D(_SurfaceNormal, TRMatrix);

            _Vector3D.x = _SurfaceNormalTransfer.x - LCSOriginTransfer.x;
            _Vector3D.y = _SurfaceNormalTransfer.y - LCSOriginTransfer.y;
            _Vector3D.z = _SurfaceNormalTransfer.z - LCSOriginTransfer.z;

            _Vector3D = _Vector3D.UnitVector(_Vector3D);
            return _Vector3D;
        }

        // Set LCS for a face
        // Origin: the first vertex of the face
        // X: the first vertex -> the second vertex of the face
        // Z: the surface normal of the face
        public void LCSSetting(Polyline3D face, ref double[,] TM_LCS_GCS, ref double[,] TM_GCS_LCS)
        {
            Vector3D xAxis = new Vector3D();
            Vector3D zAxis = new Vector3D();
            if (VectorLength(face.SurfaceNormal) == 0)
            {
                face.SurfaceNormal = new Vector3D();
                face.SurfaceNormal = ExtractSurfaceNormal(face);
            }

            if (face.SurfaceNormal.z == 1 || face.SurfaceNormal.z == -1) // directly use GCS
            {
                Point3D origin = NewPoint(0, 0, 0);
                xAxis = NewVector(1, 0, 0);
                zAxis = NewVector(0, 0, 1);
                TM_LCS_GCS = Affine_Matrix(origin, xAxis, zAxis);
            }
            else
            {
                xAxis = UnitVector(NewVector(face.Vertices[0], face.Vertices[1]));
                TM_LCS_GCS = Affine_Matrix(face.Vertices[0], xAxis, face.SurfaceNormal);
            }

            TM_GCS_LCS = IMatrix_Matrix(TM_LCS_GCS);
        }

        // calculate the cartesian point coordinates after transformation
        public Point3D PointTransf3D(Point3D _Point3D, double[,] TRMatrix)
        {
            Point3D PointAfterTransf = new Point3D();
            PointAfterTransf.x = TRMatrix[0, 0] * _Point3D.x + TRMatrix[0, 1] * _Point3D.y + TRMatrix[0, 2] * _Point3D.z + TRMatrix[0, 3];
            PointAfterTransf.y = TRMatrix[1, 0] * _Point3D.x + TRMatrix[1, 1] * _Point3D.y + TRMatrix[1, 2] * _Point3D.z + TRMatrix[1, 3];
            PointAfterTransf.z = TRMatrix[2, 0] * _Point3D.x + TRMatrix[2, 1] * _Point3D.y + TRMatrix[2, 2] * _Point3D.z + TRMatrix[2, 3];
            return PointAfterTransf;
        }

        public Point3D PointTransf3D(Point2D _Point2D, double[,] TRMatrix)
        {
            Point3D PointAfterTransf = new Point3D();
            PointAfterTransf.x = TRMatrix[0, 0] * _Point2D.x + TRMatrix[0, 1] * _Point2D.y + TRMatrix[0, 3];
            PointAfterTransf.y = TRMatrix[1, 0] * _Point2D.x + TRMatrix[1, 1] * _Point2D.y + TRMatrix[1, 3];
            PointAfterTransf.z = TRMatrix[2, 0] * _Point2D.x + TRMatrix[2, 1] * _Point2D.y + TRMatrix[2, 3];
            return PointAfterTransf;
        }

        // The starting point != end point
        // More roboust method: Newell's Method for an arbitrary 3D polygon 
        public Vector3D ExtractSurfaceNormal(Polyline3D surface)
        {
            Vector3D vector = new Vector3D();

            for (int i = 0; i < surface.Vertices.Count; i++)
            {
                Point3D CP = surface.Vertices[i];
                Point3D NP = surface.Vertices[(i + 1) % surface.Vertices.Count];

                vector.x = vector.x + (CP.y - NP.y) * (CP.z + NP.z);
                vector.y = vector.y + (CP.z - NP.z) * (CP.x + NP.x);
                vector.z = vector.z + (CP.x - NP.x) * (CP.y + NP.y);
            }

            vector = UnitVector(vector);
            return vector;
        }

        public bool Polyhedron_Polyhedron_Disjoint(List<Triangle3D> A, BoundingBox3D A_aabb, List<Triangle3D> B, BoundingBox3D B_aabb)
        {
            // check whether triangle intersects
            foreach (var triA in A)
            {
                foreach (var triB in B)
                {
                    int intersection = Triangle_Triangle_IntersectOrTouch(ConvertPointToVector(triA.Vertex1), ConvertPointToVector(triA.Vertex2), ConvertPointToVector(triA.Vertex3),
                                        ConvertPointToVector(triB.Vertex1), ConvertPointToVector(triB.Vertex2), ConvertPointToVector(triB.Vertex3));
                    if (intersection !=0)
                        return false;
                }
            }

            // check whether B inside A
            bool testB = PointInAABBTest(B[0].Vertex1, A_aabb);
            if (testB)
            {
                Ray3D ray = new Ray3D();
                ray.StartPoint = B[0].Vertex1;
                ray.Direction = NewVector(0, 1, 0);
                if (PointInPolyhedronTest(ray, A))
                    return false;
            }

            // check whether A inside B
            bool testA = PointInAABBTest(A[0].Vertex1, B_aabb);
            if (testA)
            {
                Ray3D ray = new Ray3D();
                ray.StartPoint = A[0].Vertex1;
                ray.Direction = NewVector(0, 1, 0);
                if (PointInPolyhedronTest(ray, B))
                    return false;
            }

            return true;
        }


        // ???? May have robustness issue if the ray's endpoint is on the contacting face
        // ????? Only one ray is used; may have robustenss issues
        // No intersection/containment: touch result can be a point, a line segment, or a polygon
        // Daum, S. and Borrmann, A., 2014. Processing of topological BIM queries using boundary representation based methods. Advanced Engineering Informatics, 28(4), pp.272-286.       
        public bool Polyhedron_Polyhedron_Touch(BoundingBox3D A_aabb, List<List<Triangle3D>> A, BoundingBox3D B_aabb, List<List<Triangle3D>> B, bool checkAContainB, bool checkBContainA)
        {
            bool haveTouch = false;
            List<Triangle3D> ASet = new List<Triangle3D>();
            List<Triangle3D> BSet = new List<Triangle3D>();
            foreach (var item in A)
                ASet.AddRange(item);
            foreach (var item in B)
                BSet.AddRange(item);
            foreach (var triA in ASet)
            {
                foreach (var triB in BSet)
                {
                    int intersection = Triangle_Triangle_IntersectOrTouch(ConvertPointToVector(triA.Vertex1), ConvertPointToVector(triA.Vertex2), ConvertPointToVector(triA.Vertex3),
                                        ConvertPointToVector(triB.Vertex1), ConvertPointToVector(triB.Vertex2), ConvertPointToVector(triB.Vertex3));
                    if (intersection == 1)
                        return false;
                    else if (intersection == 2)
                        haveTouch = true;
                }
            }

            if (!haveTouch)
                return false;

            if (checkAContainB)
            {
                Ray3D ray = new Ray3D();
                ray.StartPoint.x = (B_aabb.xmax + B_aabb.xmin) / 2;
                ray.StartPoint.y = (B_aabb.ymax + B_aabb.ymin) / 2;
                ray.StartPoint.z = (B_aabb.zmax + B_aabb.zmin) / 2;
                bool test = PointInAABBTest(ray.StartPoint, A_aabb);
                if (test)
                {
                    ray.Direction = NewVector(0, 1, 0);
                    if (PointInPolyhedronTest(ray, A))
                        return false;
                }
            }

            if (checkBContainA)
            {
                Ray3D ray = new Ray3D();
                ray.StartPoint.x = (A_aabb.xmax + A_aabb.xmin) / 2;
                ray.StartPoint.y = (A_aabb.ymax + A_aabb.ymin) / 2;
                ray.StartPoint.z = (A_aabb.zmax + A_aabb.zmin) / 2;
                bool test = PointInAABBTest(ray.StartPoint, B_aabb);
                if (test)
                {
                    ray.Direction = NewVector(0, 1, 0);
                    if (PointInPolyhedronTest(ray, B))
                        return false;
                }
            }

            return true;
        }

        public bool Polyhedron_Polyhedron_Overlap(List<List<Triangle3D>> A, List<List<Triangle3D>> B)
        {
            // 1. check whether there any triangles in A and B have an intersection
            List<Triangle3D> ASet = new List<Triangle3D>();
            List<Triangle3D> BSet = new List<Triangle3D>();
            foreach (var item in A)
                ASet.AddRange(item);
            foreach (var item in B)
                BSet.AddRange(item);
            foreach (var triA in ASet)
            {
                foreach (var triB in BSet)
                {
                    int intersection = Triangle_Triangle_IntersectOrTouch(ConvertPointToVector(triA.Vertex1), ConvertPointToVector(triA.Vertex2), ConvertPointToVector(triA.Vertex3),
                                        ConvertPointToVector(triB.Vertex1), ConvertPointToVector(triB.Vertex2), ConvertPointToVector(triB.Vertex3));
                    if (intersection == 1)
                        return true;
                }
            }

            return false;
        }


        // !!!!!! Extend the original implemnetaion of Möller(1997) by distinguishing intersect and touch 
        // Intersect: Vertices of one triangle distribute in the two half spaces formed by the plane where another triangle lies on;
        // Touch: all other contacting situations 
        // 0: No intersection; 1: Intersect; 2: Touch
        // Möller, T., 1997. A fast triangle-triangle intersection test.Journal of graphics tools, 2(2), pp.25-30.
        // C# code in https://answers.unity.com/questions/861719/a-fast-triangle-triangle-intersection-algorithm-fo.html
        // original C code in http://fileadmin.cs.lth.se/cs/Personal/Tomas_Akenine-Moller/code/opttritri.txt
        public int Triangle_Triangle_IntersectOrTouch(Vector3D v0, Vector3D v1, Vector3D v2, Vector3D u0, Vector3D u1, Vector3D u2)
        {
            double Epsilon = 0.000001;
            Vector3D e1, e2;
            Vector3D n1, n2;
            Vector3D dd;
            double[] isect1 = new double[2];
            double[] isect2 = new double[2];

            double du0, du1, du2, dv0, dv1, dv2, d1, d2;
            double du0du1, du0du2, dv0dv1, dv0dv2;
            double vp0, vp1, vp2;
            double up0, up1, up2;
            double bb, cc, max;

            int index;

            // compute plane equation of triangle(v0,v1,v2) 
            // plane equation 1: N1.X+d1=0
            e1 = Subtraction(v0, v1);
            e2 = Subtraction(v0, v2);
            n1 = CrossProduct(e1, e2);
            d1 = DotProduct(n1, v0) * (-1);

            // put u0,u1,u2 into plane equation 1 to compute signed distances to the plane
            du0 = DotProduct(n1, u0) + d1;
            du1 = DotProduct(n1, u1) + d1;
            du2 = DotProduct(n1, u2) + d1;

            // coplanarity robustness check 
            if (Math.Abs(du0) < Epsilon) { du0 = 0.0; }
            if (Math.Abs(du1) < Epsilon) { du1 = 0.0; }
            if (Math.Abs(du2) < Epsilon) { du2 = 0.0; }

            du0du1 = du0 * du1;
            du0du2 = du0 * du2;

            // same sign on all of them + not equal 0 ? 
            if (du0du1 > 0.0 && du0du2 > 0.0)
            {
                // no intersection occurs
                //return false;
                return 0;  // override
            }

            // compute plane of triangle (u0,u1,u2)
            e1 = Subtraction(u0, u1);
            e2 = Subtraction(u0, u2);
            n2 = CrossProduct(e1, e2);
            d2 = -1 * DotProduct(n2, u0);

            // plane equation 2: N2.X+d2=0 
            // put v0,v1,v2 into plane equation 2
            dv0 = DotProduct(n2, v0) + d2;
            dv1 = DotProduct(n2, v1) + d2;
            dv2 = DotProduct(n2, v2) + d2;

            if (Math.Abs(dv0) < Epsilon) { dv0 = 0.0; }  // = 0 means the vertex on the plane of another triangle
            if (Math.Abs(dv1) < Epsilon) { dv1 = 0.0; }
            if (Math.Abs(dv2) < Epsilon) { dv2 = 0.0; }

            dv0dv1 = dv0 * dv1;
            dv0dv2 = dv0 * dv2;

            // same sign on all of them + not equal 0 ? 
            if (dv0dv1 > 0.0 && dv0dv2 > 0.0)
            {
                // no intersection occurs
                //return false;
                return 0;  // override
            }

            // compute direction of intersection line 
            dd = CrossProduct(n1, n2);

            // compute and index to the largest component of D 
            max = Math.Abs(dd.x);
            index = 0;  // 0:x; 1:y; 2:z
            bb = Math.Abs(dd.y);
            cc = Math.Abs(dd.z);
            if (bb > max) { max = bb; index = 1; }
            if (cc > max) { max = cc; index = 2; }

            // this is the simplified projection onto L
            if (index == 0)
            {
                vp0 = v0.x; vp1 = v1.x; vp2 = v2.x;
                up0 = u0.x; up1 = u1.x; up2 = u2.x;
            }
            else if (index == 1)
            {
                vp0 = v0.y; vp1 = v1.y; vp2 = v2.y;
                up0 = u0.y; up1 = u1.y; up2 = u2.y;
            }
            else
            {
                vp0 = v0.z; vp1 = v1.z; vp2 = v2.z;
                up0 = u0.z; up1 = u1.z; up2 = u2.z;
            }

            // compute interval for triangle 1 
            double a = 0, b = 0, c = 0, x0 = 0, x1 = 0;
            if (ComputeIntervals(vp0, vp1, vp2, dv0, dv1, dv2, dv0dv1, dv0dv2, ref a, ref b, ref c, ref x0, ref x1))
            {
                double[] n1_ = ConvertVectortoMatrix(n1);
                double[] v0_ = ConvertVectortoMatrix(v0);
                double[] v1_ = ConvertVectortoMatrix(v1);
                double[] v2_ = ConvertVectortoMatrix(v2);
                double[] u0_ = ConvertVectortoMatrix(u0);
                double[] u1_ = ConvertVectortoMatrix(u1);
                double[] u2_ = ConvertVectortoMatrix(u2);

                //return TriTriCoplanar(n1_, v0_, v1_, v2_, u0_, u1_, u2_);
                bool coplanrTest = TriTriCoplanar(n1_, v0_, v1_, v2_, u0_, u1_, u2_);  // override
                if (coplanrTest) // override
                    return 2; // override
                else
                    return 0;
            }

            // compute interval for triangle 2 
            double d = 0, e = 0, f = 0, y0 = 0, y1 = 0;
            if (ComputeIntervals(up0, up1, up2, du0, du1, du2, du0du1, du0du2, ref d, ref e, ref f, ref y0, ref y1))
            {
                double[] n1_ = ConvertVectortoMatrix(n1);
                double[] v0_ = ConvertVectortoMatrix(v0);
                double[] v1_ = ConvertVectortoMatrix(v1);
                double[] v2_ = ConvertVectortoMatrix(v2);
                double[] u0_ = ConvertVectortoMatrix(u0);
                double[] u1_ = ConvertVectortoMatrix(u1);
                double[] u2_ = ConvertVectortoMatrix(u2);

                //return TriTriCoplanar(n1_, v0_, v1_, v2_, u0_, u1_, u2_);
                bool coplanrTest = TriTriCoplanar(n1_, v0_, v1_, v2_, u0_, u1_, u2_);  // override
                if (coplanrTest) // override
                    return 2; // override
                else
                    return 0;
            }

            double xx, yy, xxyy, tmp;
            xx = x0 * x1;
            yy = y0 * y1;
            xxyy = xx * yy;

            tmp = a * xxyy;
            isect1[0] = tmp + b * x1 * yy;
            isect1[1] = tmp + c * x0 * yy;

            tmp = d * xxyy;
            isect2[0] = tmp + e * xx * y1;
            isect2[1] = tmp + f * xx * y0;

            isect1 = Sort_d2(isect1);
            isect2 = Sort_d2(isect2);

            //return !(isect1[1] < isect2[0] || isect2[1] < isect1[0]);
            if (isect1[1] < isect2[0] || isect2[1] < isect1[0])   // override
                return 0;    // override
            else if (isect1[1] == isect2[0] || isect2[1] == isect1[0])   // override
            {
                return 2;
            }
            else
            {
                if (existContrarySign(du0, du1, du2) && existContrarySign(dv0, dv1, dv2))
                    return 1;
                else
                    return 2;
            }
        }

        private bool existContrarySign(double a, double b, double c)
        {
            if (a < 0)
            {
                if (b > 0 || c > 0)
                    return true;
                else
                    return false;
            }
            else if (a > 0)
            {
                if (b < 0 || c < 0)
                    return true;
                else
                    return false;
            }
            else
            {
                if (b * c < 0)
                    return true;
                else
                    return false;
            }
        }

        private double[] Sort_d2(double[] a)
        {
            double[] result = new double[2];

            if (a[0] > a[1])
            {
                result[0] = a[1];
                result[1] = a[0];
            }
            else
            {
                result[0] = a[0];
                result[1] = a[1];
            }

            return result;
        }

        public double[] ConvertVectortoMatrix(Vector3D v)
        {
            double[] result = new double[3];
            result[0] = v.x;
            result[1] = v.y;
            result[2] = v.z;

            return result;
        }

        private bool ComputeIntervals(double VV0, double VV1, double VV2,
                                      double D0, double D1, double D2, double D0D1, double D0D2,
                                      ref double A, ref double B, ref double C, ref double X0, ref double X1)
        {
            if (D0D1 > 0.0)
            {
                // here we know that D0D2<=0.0 
                // that is D0, D1 are on the same side, D2 on the other or on the plane 
                A = VV2; B = (VV0 - VV2) * D2; C = (VV1 - VV2) * D2; X0 = D2 - D0; X1 = D2 - D1;
            }
            else if (D0D2 > 0.0)
            {
                // here we know that d0d1<=0.0 
                A = VV1; B = (VV0 - VV1) * D1; C = (VV2 - VV1) * D1; X0 = D1 - D0; X1 = D1 - D2;
            }
            else if (D1 * D2 > 0.0 || D0 != 0.0)
            {
                // here we know that d0d1<=0.0 or that D0!=0.0 
                A = VV0; B = (VV1 - VV0) * D0; C = (VV2 - VV0) * D0; X0 = D0 - D1; X1 = D0 - D2;
            }
            else if (D1 != 0.0)
            {
                A = VV1; B = (VV0 - VV1) * D1; C = (VV2 - VV1) * D1; X0 = D1 - D0; X1 = D1 - D2;
            }
            else if (D2 != 0.0)
            {
                A = VV2; B = (VV0 - VV2) * D2; C = (VV1 - VV2) * D2; X0 = D2 - D0; X1 = D2 - D1;
            }
            else
            {
                return true;
            }

            return false;
        }

        private bool TriTriCoplanar(double[] N, double[] v0, double[] v1, double[] v2, double[] u0, double[] u1, double[] u2)
        {
            double[] A = new double[3];
            int i0, i1;

            // first project onto an axis-aligned plane, that maximizes the area
            // of the triangles, compute indices: i0,i1. 
            A[0] = Math.Abs(N[0]);
            A[1] = Math.Abs(N[1]);
            A[2] = Math.Abs(N[2]);
            if (A[0] > A[1])
            {
                if (A[0] > A[2])
                {
                    // A[0] is greatest
                    i0 = 1;
                    i1 = 2;
                }
                else
                {
                    // A[2] is greatest
                    i0 = 0;
                    i1 = 1;
                }
            }
            else
            {
                if (A[2] > A[1])
                {
                    // A[2] is greatest 
                    i0 = 0;
                    i1 = 1;
                }
                else
                {
                    // A[1] is greatest 
                    i0 = 0;
                    i1 = 2;
                }
            }

            // test all edges of triangle 1 against the edges of triangle 2 
            if (EdgeAgainstTriEdges(v0, v1, u0, u1, u2, i0, i1)) { return true; }
            if (EdgeAgainstTriEdges(v1, v2, u0, u1, u2, i0, i1)) { return true; }
            if (EdgeAgainstTriEdges(v2, v0, u0, u1, u2, i0, i1)) { return true; }

            // finally, test if tri1 is totally contained in tri2 or vice versa 
            if (PointInTri(v0, u0, u1, u2, i0, i1)) { return true; }
            if (PointInTri(u0, v0, v1, v2, i0, i1)) { return true; }

            return false;
        }

        private bool EdgeAgainstTriEdges(double[] v0, double[] v1, double[] u0, double[] u1, double[] u2, int i0, int i1)
        {
            // test edge u0,u1 against v0,v1
            if (EdgeEdgeTest(v0, v1, u0, u1, i0, i1)) { return true; }

            // test edge u1,u2 against v0,v1 
            if (EdgeEdgeTest(v0, v1, u1, u2, i0, i1)) { return true; }

            // test edge u2,u1 against v0,v1 
            if (EdgeEdgeTest(v0, v1, u2, u0, i0, i1)) { return true; }

            return false;
        }

        // This edge to edge test is based on Franlin Antonio's gem: "Faster Line Segment Intersection", in Graphics Gems III, pp. 199-202 
        private bool EdgeEdgeTest(double[] v0, double[] v1, double[] u0, double[] u1, int i0, int i1)
        {
            double Ax, Ay, Bx, By, Cx, Cy, e, d, f;
            Ax = v1[i0] - v0[i0];
            Ay = v1[i1] - v0[i1];

            Bx = u0[i0] - u1[i0];
            By = u0[i1] - u1[i1];
            Cx = v0[i0] - u0[i0];
            Cy = v0[i1] - u0[i1];
            f = Ay * Bx - Ax * By;
            d = By * Cx - Bx * Cy;
            if ((f > 0 && d >= 0 && d <= f) || (f < 0 && d <= 0 && d >= f))
            {
                e = Ax * Cy - Ay * Cx;
                if (f > 0)
                {
                    if (e >= 0 && e <= f) { return true; }
                }
                else
                {
                    if (e <= 0 && e >= f) { return true; }
                }
            }

            return false;
        }

        private bool PointInTri(double[] v0, double[] u0, double[] u1, double[] u2, int i0, int i1)
        {
            double a, b, c, d0, d1, d2;

            // is T1 completly inside T2?
            // check if v0 is inside tri(u0,u1,u2)
            a = u1[i1] - u0[i1];
            b = -(u1[i0] - u0[i0]);
            c = -a * u0[i0] - b * u0[i1];
            d0 = a * v0[i0] + b * v0[i1] + c;

            a = u2[i1] - u1[i1];
            b = -(u2[i0] - u1[i0]);
            c = -a * u1[i0] - b * u1[i1];
            d1 = a * v0[i0] + b * v0[i1] + c;

            a = u0[i1] - u2[i1];
            b = -(u0[i0] - u2[i0]);
            c = -a * u2[i0] - b * u2[i1];
            d2 = a * v0[i0] + b * v0[i1] + c;

            if (d0 * d1 > 0.0f)
            {
                if (d0 * d2 > 0.0f) { return true; }
            }

            return false;
        }

        private Vector3D ConvertPointToVector(Point3D p)
        {
            Vector3D v = new Vector3D();
            v.x = p.x;
            v.y = p.y;
            v.z = p.z;
            return v;
        }


        // The last one refer to the bottom face
        // Normal refer to an outward normal
        public List<Polyline3D> ExtractFacesWithNormal(BoundingBox3D AABB)
        {
            List<Polyline3D> result = new List<Polyline3D>();
            Polyline3D face1 = new Polyline3D();
            face1.Vertices.Add(NewPoint(AABB.xmin, AABB.ymin, AABB.zmin));
            face1.Vertices.Add(NewPoint(AABB.xmax, AABB.ymin, AABB.zmin));
            face1.Vertices.Add(NewPoint(AABB.xmax, AABB.ymin, AABB.zmax));
            face1.Vertices.Add(NewPoint(AABB.xmin, AABB.ymin, AABB.zmax));
            face1.SurfaceNormal = NewVector(0, -1, 0);
            result.Add(face1);

            Polyline3D face2 = new Polyline3D();
            face2.Vertices.Add(NewPoint(AABB.xmax, AABB.ymin, AABB.zmin));
            face2.Vertices.Add(NewPoint(AABB.xmax, AABB.ymax, AABB.zmin));
            face2.Vertices.Add(NewPoint(AABB.xmax, AABB.ymax, AABB.zmax));
            face2.Vertices.Add(NewPoint(AABB.xmax, AABB.ymin, AABB.zmax));
            face2.SurfaceNormal = NewVector(1, 0, 0);
            result.Add(face2);

            Polyline3D face3 = new Polyline3D();
            face3.Vertices.Add(NewPoint(AABB.xmax, AABB.ymax, AABB.zmin));
            face3.Vertices.Add(NewPoint(AABB.xmin, AABB.ymax, AABB.zmin));
            face3.Vertices.Add(NewPoint(AABB.xmin, AABB.ymax, AABB.zmax));
            face3.Vertices.Add(NewPoint(AABB.xmax, AABB.ymax, AABB.zmax));
            face3.SurfaceNormal = NewVector(0, 1, 0);
            result.Add(face3);

            Polyline3D face4 = new Polyline3D();
            face4.Vertices.Add(NewPoint(AABB.xmin, AABB.ymax, AABB.zmin));
            face4.Vertices.Add(NewPoint(AABB.xmin, AABB.ymin, AABB.zmin));
            face4.Vertices.Add(NewPoint(AABB.xmin, AABB.ymin, AABB.zmax));
            face4.Vertices.Add(NewPoint(AABB.xmin, AABB.ymax, AABB.zmax));
            face4.SurfaceNormal = NewVector(-1, 0, 0);
            result.Add(face4);
            
            Polyline3D face5 = new Polyline3D();
            face5.Vertices.Add(NewPoint(AABB.xmin, AABB.ymin, AABB.zmax));
            face5.Vertices.Add(NewPoint(AABB.xmax, AABB.ymin, AABB.zmax));
            face5.Vertices.Add(NewPoint(AABB.xmax, AABB.ymax, AABB.zmax));
            face5.Vertices.Add(NewPoint(AABB.xmin, AABB.ymax, AABB.zmax));
            face5.SurfaceNormal = NewVector(0, 0, 1);
            result.Add(face5);

            Polyline3D face6 = new Polyline3D();
            face6.Vertices.Add(NewPoint(AABB.xmin, AABB.ymin, AABB.zmin));
            face6.Vertices.Add(NewPoint(AABB.xmin, AABB.ymax, AABB.zmin));
            face6.Vertices.Add(NewPoint(AABB.xmax, AABB.ymax, AABB.zmin));
            face6.Vertices.Add(NewPoint(AABB.xmax, AABB.ymin, AABB.zmin));
            face6.SurfaceNormal = NewVector(0, 0, -1);
            result.Add(face6);

            return result;
        }

        public Point3D OffsetPoint(Point3D p, double offset, Vector3D direction)
        {
            Point3D result = new Point3D();
            result.x = p.x + offset * direction.x;
            result.y = p.y + offset * direction.y;
            result.z = p.z + offset * direction.z;

            return result;
        }

        public Point3D Subtraction(Point3D A, Point3D B)
        {
            Point3D _Point3D = new Point3D();
            _Point3D.x = B.x - A.x;
            _Point3D.y = B.y - A.y;
            _Point3D.z = B.z - A.z;

            return _Point3D;
        }

        // Ray-casting point-in-polyhedron test algorithm: Odd-even rule: Based on Jordan curve theorem     
        public bool PointInPolyhedronTest(Ray3D ray, List<Triangle3D> Polyhedron)
        {
            bool Result = false;

            // Intersection points are located on the edges shared by two triangle surfaces: Remove same intersection points
            List<Point3D> points = new List<Point3D>();
            foreach (var triangle in Polyhedron)
            {
                RayTri_InterPoint test = Ray_Triangle_do_Intersection(ray, triangle);
                if (test.intersection)
                {
                    if (points.Count == 0)
                        points.Add(test.IP);
                    else
                    {
                        bool repeat = false;
                        for (int j = 0; j < points.Count; j++)
                        {
                            Point3D p = new Point3D();
                            if (p.IsSamePoint(points[j], test.IP, this.coordDiff))
                            {
                                repeat = true;
                                break;
                            }
                        }
                        if (!repeat)
                            points.Add(test.IP);
                    }
                }
            }

            if (points.Count % 2 == 1)
                Result = true;

            return Result;
        }

        // Ray-casting point-in-polyhedron test algorithm: Odd-even rule: Based on Jordan curve theorem     
        public bool PointInPolyhedronTest(Ray3D ray, List<List<Triangle3D>> Polyhedron)
        {
            bool Result = false;

            // Intersection points are located on the edges shared by two triangle surfaces: Remove same intersection points
            List<Point3D> points = new List<Point3D>();
            foreach (var plane in Polyhedron)
            {
                foreach (var triangle in plane)
                {
                    RayTri_InterPoint test = Ray_Triangle_do_Intersection(ray, triangle);
                    if (test.intersection)
                    {
                        if (points.Count == 0)
                            points.Add(test.IP);
                        else
                        {
                            bool repeat = false;
                            for (int j = 0; j < points.Count; j++)
                            {
                                Point3D p = new Point3D();
                                if (p.IsSamePoint(points[j], test.IP, this.coordDiff))
                                {
                                    repeat = true;
                                    break;
                                }
                            }
                            if (!repeat)
                                points.Add(test.IP);
                        }
                        break;
                    }
                }
            }

            if (points.Count % 2 == 1)
                Result = true;

            return Result;
        }

        public bool PointInFacetedBrep(Point3D p, Vector3D direction, TriangulatedBrep Brep)
        {
            bool result = false;

            if (PointInAABBTest(p, Brep.AABB))
            {
                if (PointInPolyhedronTest(p, direction, Brep.TriangleFaces))
                    result = true;
            }

            return result;
        }

        public bool PointInPolyhedronTest(Point3D point, Vector3D direction, List<List<Triangle3D>> Polyhedron)
        {
            bool Result = false;

            // Intersection points are located on the edges shared by two triangle surfaces: Remove same intersection points
            List<Point3D> points = new List<Point3D>();
            foreach (var face in Polyhedron)
            {
                foreach (var triangle in face)
                {
                    Ray3D ray = new Ray3D();
                    ray.StartPoint = point;
                    ray.Direction = direction;
                    RayTri_InterPoint test = Ray_Triangle_do_Intersection(ray, triangle);
                    if (test.intersection)
                    {
                        if (points.Count == 0)
                            points.Add(test.IP);
                        else
                        {
                            bool repeat = false;
                            for (int j = 0; j < points.Count; j++)
                            {
                                Point3D p = new Point3D();
                                if (p.IsSamePoint(points[j], test.IP, 0.00001))
                                {
                                    repeat = true;
                                    break;
                                }
                            }
                            if (!repeat)
                                points.Add(test.IP);
                        }
                        break;
                    }
                }
            }

            if (points.Count % 2 == 1)
                Result = true;

            return Result;
        }

        public bool PointInAABBTest(Point3D p, BoundingBox3D AABB)
        {
            bool result = false;

            if (p.x <= AABB.xmax && p.x >= AABB.xmin)
            {
                if (p.y <= AABB.ymax && p.y >= AABB.ymin)
                {
                    if (p.z <= AABB.zmax && p.z >= AABB.zmin)
                    {
                        result = true;
                    }
                }
            }

            return result;
        }

        // number * Vector3D
        public Point3D Multiply_double_Point3D(double A, Vector3D B)
        {
            Point3D _Point3D = new Point3D();
            _Point3D.x = B.x * A;
            _Point3D.y = B.y * A;
            _Point3D.z = B.z * A;

            return _Point3D;
        }

        // number * Point3D
        public Point3D Multiply_double_Point3D(double A, Point3D B)
        {
            Point3D _Point3D = new Point3D();
            _Point3D.x = B.x * A;
            _Point3D.y = B.y * A;
            _Point3D.z = B.z * A;

            return _Point3D;
        }

        public Point3D NewPoint(double x, double y, double z)
        {
            Point3D p = new Point3D();
            p.x = x;
            p.y = y;
            p.z = z;

            return p;
        }

        public Point3D NewPoint(Vector3D vector)
        {
            Point3D p = new Point3D();
            p.x = vector.x;
            p.y = vector.y;
            p.z = vector.z;

            return p;
        }

        public Vector3D NewVector(double x, double y, double z)
        {
            Vector3D Vector = new Vector3D();
            Vector.x = x;
            Vector.y = y;
            Vector.z = z;

            return Vector;
        }

        public Vector3D NewVector(Point3D A, Point3D B)
        {
            Vector3D Vector = new Vector3D();
            Vector.x = B.x - A.x;
            Vector.y = B.y - A.y;
            Vector.z = B.z - A.z;

            return Vector;
        }

        public Vector3D NewVector(Point3D A)
        {
            Vector3D Vector = new Vector3D();
            Vector.x = A.x;
            Vector.y = A.y;
            Vector.z = A.z;

            return Vector;
        }

        // vector length
        public double VectorLength(Vector3D A)
        {
            double VL = 0;
            VL = Math.Sqrt(A.x * A.x + A.y * A.y + A.z * A.z);

            return VL;
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

        // Vector - Vector
        public Vector3D Subtraction(Vector3D A, Vector3D B)
        {
            Vector3D _Vector3D = new Vector3D();
            _Vector3D.x = B.x - A.x;
            _Vector3D.y = B.y - A.y;
            _Vector3D.z = B.z - A.z;

            return _Vector3D;
        }

        // Vector + Vector
        public Vector3D Addition(Vector3D A, Vector3D B)
        {
            Vector3D _Vector3D = new Vector3D();
            _Vector3D.x = B.x + A.x;
            _Vector3D.y = B.y + A.y;
            _Vector3D.z = B.z + A.z;

            return _Vector3D;
        }

        //  + 
        public Point3D Addition(Point3D A, Point3D B)
        {
            Point3D _Point3D = new Point3D();
            _Point3D.x = B.x + A.x;
            _Point3D.y = B.y + A.y;
            _Point3D.z = B.z + A.z;

            return _Point3D;
        }

        // number * Vector
        public Vector3D Multiply_double_Vector3D(double A, Vector3D B)
        {
            Vector3D _Vector3D = new Vector3D();
            _Vector3D.x = B.x * A;
            _Vector3D.y = B.y * A;
            _Vector3D.z = B.z * A;

            return _Vector3D;
        }

        // Dot product
        // When the A is a unit vector and B represents a vertex then Dot Product represents the projection of B on the A axis 
        public double DotProduct(Vector3D A, Vector3D B)
        {
            double DP = 0;
            DP = A.x * B.x + A.y * B.y + A.z * B.z;

            return DP;
        }

        // Cross product: a vector that is vectial to the A and B according to the right hand rule from A to B
        // When A and B are unit vector and their angle is 90 degree, the cross product is unit vector
        public Vector3D CrossProduct(Vector3D A, Vector3D B)
        {
            Vector3D CP = new Vector3D();
            CP.x = A.y * B.z - B.y * A.z;
            CP.y = (-1) * (A.x * B.z - B.x * A.z);
            CP.z = A.x * B.y - B.x * A.y;

            return CP;
        }

        // create a root bounding box that contains a sequence of 3D triangles
        // create a root bounding box that contains a sequence of 3D triangles
        public BoundingBox3D BoundingBox3D_Triangles_Create(List<Triangle3D> Triangles, double tolerance_mm)
        {
            BoundingBox3D _BoundingBox = new BoundingBox3D();
            List<double> _ListXmax = new List<double>();
            List<double> _ListXmin = new List<double>();
            List<double> _ListYmax = new List<double>();
            List<double> _ListYmin = new List<double>();
            List<double> _ListZmax = new List<double>();
            List<double> _ListZmin = new List<double>();

            for (int i = 0; i < Triangles.Count; i++)
            {
                double _xmin = 0;
                double _ymin = 0;
                double _zmin = 0;
                double _xmax = 0;
                double _ymax = 0;
                double _zmax = 0;

                double[] listx = { Triangles[i].Vertex1.x, Triangles[i].Vertex2.x, Triangles[i].Vertex3.x };
                _xmin = listx.Min();
                _xmax = listx.Max();
                double[] listy = { Triangles[i].Vertex1.y, Triangles[i].Vertex2.y, Triangles[i].Vertex3.y };
                _ymin = listy.Min();
                _ymax = listy.Max();
                double[] listz = { Triangles[i].Vertex1.z, Triangles[i].Vertex2.z, Triangles[i].Vertex3.z };
                _zmin = listz.Min();
                _zmax = listz.Max();

                _ListXmax.Add(_xmax);
                _ListYmax.Add(_ymax);
                _ListZmax.Add(_zmax);
                _ListXmin.Add(_xmin);
                _ListYmin.Add(_ymin);
                _ListZmin.Add(_zmin);
            }
            _BoundingBox.xmax = _ListXmax.Max() + tolerance_mm;
            _BoundingBox.ymax = _ListYmax.Max() + tolerance_mm;
            _BoundingBox.zmax = _ListZmax.Max() + tolerance_mm;
            _BoundingBox.xmin = _ListXmin.Min() - tolerance_mm;
            _BoundingBox.ymin = _ListYmin.Min() - tolerance_mm;
            _BoundingBox.zmin = _ListZmin.Min() - tolerance_mm;

            return _BoundingBox;
        }

        public BoundingBox3D BoundingBox3D_Polygons_Create(List<Polyline3D> Polygons)
        {
            BoundingBox3D _BoundingBox = new BoundingBox3D();
            List<double> _ListXmax = new List<double>();
            List<double> _ListXmin = new List<double>();
            List<double> _ListYmax = new List<double>();
            List<double> _ListYmin = new List<double>();
            List<double> _ListZmax = new List<double>();
            List<double> _ListZmin = new List<double>();

            for (int i = 0; i < Polygons.Count; i++)
            {
                double _xmin = 0;
                double _ymin = 0;
                double _zmin = 0;
                double _xmax = 0;
                double _ymax = 0;
                double _zmax = 0;

                List<double> listx = new List<double>();
                List<double> listy = new List<double>();
                List<double> listz = new List<double>();
                foreach (var vertex in Polygons[i].Vertices)
                {
                    listx.Add(vertex.x);
                    listy.Add(vertex.y);
                    listz.Add(vertex.z);
                }
                _xmin = listx.Min();
                _xmax = listx.Max();

                _ymin = listy.Min();
                _ymax = listy.Max();

                _zmin = listz.Min();
                _zmax = listz.Max();

                _ListXmax.Add(_xmax);
                _ListYmax.Add(_ymax);
                _ListZmax.Add(_zmax);
                _ListXmin.Add(_xmin);
                _ListYmin.Add(_ymin);
                _ListZmin.Add(_zmin);
            }
            _BoundingBox.xmax = _ListXmax.Max();
            _BoundingBox.ymax = _ListYmax.Max();
            _BoundingBox.zmax = _ListZmax.Max();
            _BoundingBox.xmin = _ListXmin.Min();
            _BoundingBox.ymin = _ListYmin.Min();
            _BoundingBox.zmin = _ListZmin.Min();

            return _BoundingBox;
        }

        public BoundingBox2D BoundingBox2D_Polygons_Create(List<Polyline2D> Polygons)
        {
            BoundingBox2D _BoundingBox = new BoundingBox2D();
            List<double> _ListXmax = new List<double>();
            List<double> _ListXmin = new List<double>();
            List<double> _ListYmax = new List<double>();
            List<double> _ListYmin = new List<double>();

            for (int i = 0; i < Polygons.Count; i++)
            {
                double _xmin = 0;
                double _ymin = 0;
                double _xmax = 0;
                double _ymax = 0;

                List<double> listx = new List<double>();
                List<double> listy = new List<double>();
                foreach (var vertex in Polygons[i].Vertices)
                {
                    listx.Add(vertex.x);
                    listy.Add(vertex.y);
                }
                _xmin = listx.Min();
                _xmax = listx.Max();

                _ymin = listy.Min();
                _ymax = listy.Max();

                _ListXmax.Add(_xmax);
                _ListYmax.Add(_ymax);
                _ListXmin.Add(_xmin);
                _ListYmin.Add(_ymin);
            }
            _BoundingBox.xmax = _ListXmax.Max();
            _BoundingBox.ymax = _ListYmax.Max();
            _BoundingBox.xmin = _ListXmin.Min();
            _BoundingBox.ymin = _ListYmin.Min();

            return _BoundingBox;
        }

        public BoundingBox3D BoundingBox3D_Polygons_Create(List<Polyline3D> Polygons, double tolerance_mm)
        {
            BoundingBox3D _BoundingBox = new BoundingBox3D();
            List<double> _ListXmax = new List<double>();
            List<double> _ListXmin = new List<double>();
            List<double> _ListYmax = new List<double>();
            List<double> _ListYmin = new List<double>();
            List<double> _ListZmax = new List<double>();
            List<double> _ListZmin = new List<double>();

            for (int i = 0; i < Polygons.Count; i++)
            {
                double _xmin = 0;
                double _ymin = 0;
                double _zmin = 0;
                double _xmax = 0;
                double _ymax = 0;
                double _zmax = 0;

                List<double> listx = new List<double>();
                List<double> listy = new List<double>();
                List<double> listz = new List<double>();
                foreach (var vertex in Polygons[i].Vertices)
                {
                    listx.Add(vertex.x);
                    listy.Add(vertex.y);
                    listz.Add(vertex.z);
                }
                _xmin = listx.Min();
                _xmax = listx.Max();

                _ymin = listy.Min();
                _ymax = listy.Max();

                _zmin = listz.Min();
                _zmax = listz.Max();

                _ListXmax.Add(_xmax);
                _ListYmax.Add(_ymax);
                _ListZmax.Add(_zmax);
                _ListXmin.Add(_xmin);
                _ListYmin.Add(_ymin);
                _ListZmin.Add(_zmin);
            }
            _BoundingBox.xmax = _ListXmax.Max() + tolerance_mm;
            _BoundingBox.ymax = _ListYmax.Max() + tolerance_mm;
            _BoundingBox.zmax = _ListZmax.Max() + tolerance_mm;
            _BoundingBox.xmin = _ListXmin.Min() - tolerance_mm;
            _BoundingBox.ymin = _ListYmin.Min() - tolerance_mm;
            _BoundingBox.zmin = _ListZmin.Min() - tolerance_mm;

            return _BoundingBox;
        }

        public BoundingBox3D BoundingBox3D_Polygons_Create(List<List<Polyline3D>> Polygons)
        {
            BoundingBox3D _BoundingBox = new BoundingBox3D();
            List<double> _ListXmax = new List<double>();
            List<double> _ListXmin = new List<double>();
            List<double> _ListYmax = new List<double>();
            List<double> _ListYmin = new List<double>();
            List<double> _ListZmax = new List<double>();
            List<double> _ListZmin = new List<double>();

            List<Polyline3D> polygons = new List<Polyline3D>();
            foreach (var ps in Polygons)
            {
                polygons.AddRange(ps);
            }

            for (int i = 0; i < polygons.Count; i++)
            {
                double _xmin = 0;
                double _ymin = 0;
                double _zmin = 0;
                double _xmax = 0;
                double _ymax = 0;
                double _zmax = 0;

                List<double> listx = new List<double>();
                List<double> listy = new List<double>();
                List<double> listz = new List<double>();
                foreach (var vertex in polygons[i].Vertices)
                {
                    listx.Add(vertex.x);
                    listy.Add(vertex.y);
                    listz.Add(vertex.z);
                }
                _xmin = listx.Min();
                _xmax = listx.Max();

                _ymin = listy.Min();
                _ymax = listy.Max();

                _zmin = listz.Min();
                _zmax = listz.Max();

                _ListXmax.Add(_xmax);
                _ListYmax.Add(_ymax);
                _ListZmax.Add(_zmax);
                _ListXmin.Add(_xmin);
                _ListYmin.Add(_ymin);
                _ListZmin.Add(_zmin);
            }
            _BoundingBox.xmax = _ListXmax.Max();
            _BoundingBox.ymax = _ListYmax.Max();
            _BoundingBox.zmax = _ListZmax.Max();
            _BoundingBox.xmin = _ListXmin.Min();
            _BoundingBox.ymin = _ListYmin.Min();
            _BoundingBox.zmin = _ListZmin.Min();

            return _BoundingBox;
        }

        // Extend the query rectangle by toleance in all three dimensions
        public BoundingBox3D AABBUpdate(BoundingBox3D AABB, double tolerance)
        {
            BoundingBox3D rect = new BoundingBox3D();
            rect.xmin = AABB.xmin - tolerance;
            rect.ymin = AABB.ymin - tolerance;
            rect.zmin = AABB.zmin - tolerance;

            rect.xmax = AABB.xmax + tolerance;
            rect.ymax = AABB.ymax + tolerance;
            rect.zmax = AABB.zmax + tolerance;

            return rect;
        }

        public BoundingBox3D BoundingBox3D_AABBs_Create(List<BoundingBox3D> AABBs)
        {
            BoundingBox3D result = new BoundingBox3D();

            double xmin = AABBs[0].xmin;
            double ymin = AABBs[0].ymin;
            double zmin = AABBs[0].zmin;
            double xmax = AABBs[0].xmax;
            double ymax = AABBs[0].ymax;
            double zmax = AABBs[0].zmax;

            for (int i = 1; i < AABBs.Count; i++)
            {
                if (xmin > AABBs[i].xmin)
                    xmin = AABBs[i].xmin;
                if (ymin > AABBs[i].ymin)
                    ymin = AABBs[i].ymin;
                if (zmin > AABBs[i].zmin)
                    zmin = AABBs[i].zmin;

                if (xmax < AABBs[i].xmax)
                    xmax = AABBs[i].xmax;
                if (ymax < AABBs[i].ymax)
                    ymax = AABBs[i].ymax;
                if (zmax < AABBs[i].zmax)
                    zmax = AABBs[i].zmax;
            }

            result.xmax = xmax;
            result.xmin = xmin;
            result.ymax = ymax;
            result.ymin = ymin;
            result.zmax = zmax;
            result.zmin = zmin;

            return result;
        }

        // The starting point != end point
        public Vector3D CalculateSurfaceNormal(Polyline3D Polygon3D)
        {             
            Vector3D vector = new Vector3D();
            for (int i = 0; i < Polygon3D.Vertices.Count; i++)
            {
                Point3D CP = Polygon3D.Vertices[i];
                Point3D NP = Polygon3D.Vertices[(i + 1) % Polygon3D.Vertices.Count];

                vector.x = vector.x + (CP.y - NP.y) * (CP.z + NP.z);
                vector.y = vector.y + (CP.z - NP.z) * (CP.x + NP.x);
                vector.z = vector.z + (CP.x - NP.x) * (CP.y + NP.y);
            }

            vector = UnitVector(vector);
            return vector;
        }

        // Möller, Tomas; Trumbore, Ben(1997). "Fast, Minimum Storage Ray-Triangle Intersection". 
        // Journal of Graphics Tools. 2: 21–28. doi:10.1080/10867651.1997.10487468
        public RayTri_InterPoint Ray_Triangle_do_Intersection(Ray3D ray, Triangle3D triangle)
        {
            RayTri_InterPoint result = new RayTri_InterPoint();

            const double EPSILON = 0.000000001;

            Vector3D vertex0 = NewVector(triangle.Vertex1);
            Vector3D vertex1 = NewVector(triangle.Vertex2);
            Vector3D vertex2 = NewVector(triangle.Vertex3);

            Vector3D edge1, edge2, h, s, q = new Vector3D();
            double a, f, u, v = 0;

            edge1 = Subtraction(vertex0, vertex1);
            edge2 = Subtraction(vertex0, vertex2);
            h = CrossProduct(ray.Direction, edge2);
            a = DotProduct(edge1, h);

            if (Math.Abs(a) < EPSILON)
                result.intersection = false;
            else
            {
                f = 1 / a;
                s = Subtraction(vertex0, NewVector(ray.StartPoint));
                u = f * DotProduct(s, h);
                if (u < 0.0 || u > 1.0)
                    result.intersection = false;
                else
                {
                    q = CrossProduct(s, edge1);
                    v = f * DotProduct(ray.Direction, q);
                    if (v < 0.0 || (u + v) > 1.0)
                        result.intersection = false;
                    else
                    {
                        double t = f * DotProduct(edge2, q);
                        if (t > EPSILON)
                        {
                            result.intersection = true;
                            Vector3D IP = Addition
                                (NewVector(ray.StartPoint), Multiply_double_Vector3D(t, ray.Direction));
                            result.IP.x = IP.x;
                            result.IP.y = IP.y;
                            result.IP.z = IP.z;
                            result.dist = t;
                        }
                        else
                            result.intersection = false;
                    }
                }
            }
            return result;
        }

        public RayTri_InterPoint Ray_Triangle_do_Intersection(Ray3D ray, Triangle3D triangle, double range)
        {
            RayTri_InterPoint result = new RayTri_InterPoint();

            const double EPSILON = 0.000000001;

            Vector3D vertex0 = NewVector(triangle.Vertex1);
            Vector3D vertex1 = NewVector(triangle.Vertex2);
            Vector3D vertex2 = NewVector(triangle.Vertex3);

            Vector3D edge1, edge2, h, s, q = new Vector3D();
            double a, f, u, v = 0;

            edge1 = Subtraction(vertex0, vertex1);
            edge2 = Subtraction(vertex0, vertex2);
            h = CrossProduct(ray.Direction, edge2);
            a = DotProduct(edge1, h);

            if (Math.Abs(a) < EPSILON)
                result.intersection = false;
            else
            {
                f = 1 / a;
                s = Subtraction(vertex0, NewVector(ray.StartPoint));
                u = f * DotProduct(s, h);
                if (u < 0.0 || u > 1.0)
                    result.intersection = false;
                else
                {
                    q = CrossProduct(s, edge1);
                    v = f * DotProduct(ray.Direction, q);
                    if (v < 0.0 || (u + v) > 1.0)
                        result.intersection = false;
                    else
                    {
                        double t = f * DotProduct(edge2, q);
                        if (t > EPSILON && t <= range)  // add range here
                        {
                            result.intersection = true;
                            Vector3D IP = Addition
                                (NewVector(ray.StartPoint), Multiply_double_Vector3D(t, ray.Direction));
                            result.IP.x = IP.x;
                            result.IP.y = IP.y;
                            result.IP.z = IP.z;
                            result.dist = t;
                        }
                        else
                            result.intersection = false;
                    }
                }
            }
            return result;
        }

        public List<Ray3D> Rays_Rect_MonteCarlo(Polyline3D rect, bool isBackSide, Int64 NumRay)
        {
            List<Ray3D> result = new List<Ray3D>();
            Vector3D axis_a = UnitVector(NewVector(rect.Vertices[0], rect.Vertices[1]));
            Vector3D axis_b = UnitVector(NewVector(rect.Vertices[0], rect.Vertices[3]));
            double fa = 0;
            double fb = 0;
            double tolerancePolarAngle = 89.9 / 90.0;

            Random random = new Random();
            for(int i = 0; i < NumRay; i++)
            {
                Ray3D ray = new Ray3D();

                fa = random.NextDouble();
                Vector3D pa = Multiply_double_Vector3D(fa, axis_a);
                fb = random.NextDouble();
                Vector3D pb = Multiply_double_Vector3D(fb, axis_b);
                ray.StartPoint = NewPoint(Addition(NewVector(rect.Vertices[0]), Addition(pa, pb)));

                int flag = 1;
                if (!isBackSide)
                    flag = -1;
                Vector3D Z = Multiply_double_Vector3D(flag, rect.SurfaceNormal);
                Vector3D X = UnitVector(VectorConstructor(ray.StartPoint, rect.Vertices[0]));
                Vector3D Y = yAxis(X, Z);
                double a = Math.PI * tolerancePolarAngle * random.NextDouble() / 2; // Random1 != 1 -- Random.nextdouble() : Returns a random floating-point number that is greater than or equal to 0.0, and less than 1.0.
                double b = 2 * Math.PI * random.NextDouble();
                double parameterX = Math.Sin(a) * Math.Cos(b);
                double parameterY = Math.Sin(a) * Math.Sin(b);
                double parameterZ = Math.Cos(a);
                ray.Direction.x = parameterX * X.x + parameterY * Y.x + parameterZ * Z.x;
                ray.Direction.y = parameterX * X.y + parameterY * Y.y + parameterZ * Z.y;
                ray.Direction.z = parameterX * X.z + parameterY * Y.z + parameterZ * Z.z;
                ray.Direction = UnitVector(ray.Direction);

                result.Add(ray);
            }
            return result;
        }

        // Each point have three rays
        public List<Ray3D> Rays_Rect_Determine(Polyline3D rect, bool isBackSide, double origin_interval, double direction_interval)
        {
            List<Ray3D> RaySet = new List<Ray3D>();
            Vector3D v = new Vector3D();
            Point3D p = new Point3D();

            List<Point3D> Origins = new List<Point3D>();
            Vector3D v1v2 = UnitVector(NewVector(rect.Vertices[0], rect.Vertices[1]));
            Vector3D v1v3 = UnitVector(NewVector(rect.Vertices[0], rect.Vertices[3]));                  
            double H = p.Dist(rect.Vertices[0], rect.Vertices[1]);
            double L = p.Dist(rect.Vertices[0], rect.Vertices[3]);
            double num_H = H / origin_interval;
            double num_L = L / origin_interval;

            for(int i = 0; i < num_H; i++)
            {
                for (int j = 0; j < num_L; j++)
                {
                    Point3D p_ = Addition(NewPoint(Multiply_double_Vector3D(i * origin_interval, v1v2)), NewPoint(Multiply_double_Vector3D(j * origin_interval, v1v3)));
                    p_ = Addition(p_, rect.Vertices[0]);
                    Origins.Add(p_);
                }

                Point3D p__ = Addition(NewPoint(Multiply_double_Vector3D(i * origin_interval, v1v2)), NewPoint(NewVector(rect.Vertices[0], rect.Vertices[3])));
                p__= Addition(p__, rect.Vertices[0]);
                Origins.Add(p__);
            }

            for (int j = 0; j < num_L; j++)
            {
                Point3D p_ = Addition(NewPoint(NewVector(rect.Vertices[0], rect.Vertices[1])), NewPoint(Multiply_double_Vector3D(j * origin_interval, v1v3)));
                p_ = Addition(p_, rect.Vertices[0]);
                Origins.Add(p_);
            }

            Point3D __p = Addition(NewPoint(NewVector(rect.Vertices[0], rect.Vertices[1])), NewPoint(NewVector(rect.Vertices[0], rect.Vertices[3])));
            __p = Addition(__p, rect.Vertices[0]);
            Origins.Add(__p);

            int flag = 1;
            if (!isBackSide)
                flag = -1;
            Vector3D ZAxis = Multiply_double_Vector3D(flag, rect.SurfaceNormal);
            Vector3D XAxis = UnitVector(NewVector(rect.Vertices[0], rect.Vertices[1]));
            Vector3D YAxis = yAxis(XAxis, ZAxis);

            foreach (var item in Origins)
            {
                Ray3D ray1 = new Ray3D();
                ray1.StartPoint = item;
                ray1.Direction = ZAxis;
                RaySet.Add(ray1);

                Ray3D ray2 = new Ray3D();
                ray2.StartPoint = item;
                ray2.Direction = v.Addition(v.Multiple(XAxis, Math.Cos((90-4* direction_interval) * Math.PI / 180)), v.Multiple(ZAxis, Math.Sin((90- 4 * direction_interval) * Math.PI / 180)));
                RaySet.Add(ray2);

                Ray3D ray3 = new Ray3D();
                ray3.StartPoint = item;
                ray3.Direction = v.Addition(v.Multiple(XAxis, Math.Cos((90+ 4 * direction_interval) * Math.PI / 180)), v.Multiple(ZAxis, Math.Sin((90+ 4 * direction_interval) * Math.PI / 180))); ;
                RaySet.Add(ray3);

                // Not necessary and too time-consuming
                //RaySet.AddRange(ComputeDetermineRaysForAHalfPlane(item, XAxis, ZAxis, direction_interval));
                //Vector3D X1 = v.Addition(v.Multiple(XAxis, 0), v.Multiple(YAxis, 1));
                //RaySet.AddRange(ComputeDetermineRaysForAHalfPlane(item, X1, ZAxis, direction_interval));
            }

            return RaySet;
        }

        // Each point have three rays 
        // The Plane refers to the plane in the GCS not the built LCS
        public KeyValuePair<Plane,List<Ray3D>> Plane_Rays_Rect_Determine(Polyline3D rect, bool isBackSide, double origin_interval, double direction_interval)
        {
            Vector3D v = new Vector3D();
            Point3D p = new Point3D();

            List<Point3D> Origins = new List<Point3D>();
            Vector3D v1v2 = UnitVector(NewVector(rect.Vertices[0], rect.Vertices[1]));
            Vector3D v1v3 = UnitVector(NewVector(rect.Vertices[0], rect.Vertices[3]));
            double H = p.Dist(rect.Vertices[0], rect.Vertices[1]);
            double L = p.Dist(rect.Vertices[0], rect.Vertices[3]);
            double num_H = H / origin_interval;
            double num_L = L / origin_interval;

            for (int i = 0; i < num_H; i++)
            {
                for (int j = 0; j < num_L; j++)
                {
                    Point3D p_ = Addition(NewPoint(Multiply_double_Vector3D(i * origin_interval, v1v2)), NewPoint(Multiply_double_Vector3D(j * origin_interval, v1v3)));
                    p_ = Addition(p_, rect.Vertices[0]);
                    Origins.Add(p_);
                }

                Point3D p__ = Addition(NewPoint(Multiply_double_Vector3D(i * origin_interval, v1v2)), NewPoint(NewVector(rect.Vertices[0], rect.Vertices[3])));
                p__ = Addition(p__, rect.Vertices[0]);
                Origins.Add(p__);
            }

            for (int j = 0; j < num_L; j++)
            {
                Point3D p_ = Addition(NewPoint(NewVector(rect.Vertices[0], rect.Vertices[1])), NewPoint(Multiply_double_Vector3D(j * origin_interval, v1v3)));
                p_ = Addition(p_, rect.Vertices[0]);
                Origins.Add(p_);
            }

            Point3D __p = Addition(NewPoint(NewVector(rect.Vertices[0], rect.Vertices[1])), NewPoint(NewVector(rect.Vertices[0], rect.Vertices[3])));
            __p = Addition(__p, rect.Vertices[0]);
            Origins.Add(__p);

            int flag = 1;
            if (!isBackSide)
                flag = -1;
            Vector3D ZAxis = Multiply_double_Vector3D(flag, rect.SurfaceNormal);
            Vector3D XAxis = UnitVector(NewVector(rect.Vertices[0], rect.Vertices[1]));
            Vector3D YAxis = yAxis(XAxis, ZAxis);
            Plane pla = DetectPlane(XAxis, ZAxis);

            List<Ray3D> RaySet = new List<Ray3D>();
            foreach (var item in Origins)
            {
                Ray3D ray1 = new Ray3D();
                ray1.StartPoint = item;
                ray1.Direction = ZAxis;
                RaySet.Add(ray1);

                Ray3D ray2 = new Ray3D();
                ray2.StartPoint = item;
                ray2.Direction = v.Addition(v.Multiple(XAxis, Math.Cos((90 - 4 * direction_interval) * Math.PI / 180)), v.Multiple(ZAxis, Math.Sin((90 - 4 * direction_interval) * Math.PI / 180)));
                RaySet.Add(ray2);

                Ray3D ray3 = new Ray3D();
                ray3.StartPoint = item;
                ray3.Direction = v.Addition(v.Multiple(XAxis, Math.Cos((90 + 4 * direction_interval) * Math.PI / 180)), v.Multiple(ZAxis, Math.Sin((90 + 4 * direction_interval) * Math.PI / 180))); ;
                RaySet.Add(ray3);

                // Not necessary and too time-consuming
                //RaySet.AddRange(ComputeDetermineRaysForAHalfPlane(item, XAxis, ZAxis, direction_interval));
                //Vector3D X1 = v.Addition(v.Multiple(XAxis, 0), v.Multiple(YAxis, 1));
                //RaySet.AddRange(ComputeDetermineRaysForAHalfPlane(item, X1, ZAxis, direction_interval));
            }

            KeyValuePair<Plane, List<Ray3D>> result = new KeyValuePair<Plane, List<Ray3D>>(pla,RaySet);
            return result;
        }

        public Plane DetectPlane(Vector3D aAxis, Vector3D bAxis)
        {
            Plane result = new Plane();

            Vector3D zGCS = new Vector3D(0, 0, 1);
            Vector3D yGCS = new Vector3D(0, 1, 0);
            Vector3D xGCS = new Vector3D(1, 0, 0);

            if (Math.Abs(zGCS.DotProduct(zGCS, aAxis)) < 0.0001 && Math.Abs(zGCS.DotProduct(zGCS, bAxis)) < 0.0001)
                result = Plane.xyplane;
            else if (Math.Abs(yGCS.DotProduct(yGCS, aAxis)) < 0.0001 && Math.Abs(yGCS.DotProduct(yGCS, bAxis)) < 0.0001)
                result = Plane.xzplane;
            else if (Math.Abs(xGCS.DotProduct(xGCS, aAxis)) < 0.0001 && Math.Abs(xGCS.DotProduct(xGCS, bAxis)) < 0.0001)
                result = Plane.yzplane;

            return result;
        }

        public List<Ray3D> Rays_Tri_MonteCarlo(Triangle3D tri, Int64 NumRay)
        {
            List<Ray3D> result = new List<Ray3D>();
            Vector3D axis_a = UnitVector(NewVector(tri.Vertex1, tri.Vertex2));
            Vector3D axis_b = UnitVector(NewVector(tri.Vertex1, tri.Vertex3));
            double fa = 0;
            double fb = 0;
            double tolerancePolarAngle = 89.9 / 90.0;
            double tolerancePosition_d = 0.00001;
            double tolerancePosition_u = 0.99999;
            
            Random random = new Random();
            for (int i = 0; i < NumRay; i++)
            {
                Ray3D ray = new Ray3D();

                fa = random.NextDouble();
                if (fa < tolerancePosition_d)
                    fa += tolerancePosition_d;
                else if (fa > tolerancePosition_u)
                    fa -= 2* tolerancePosition_d;
                Vector3D pa = Multiply_double_Vector3D(fa, axis_a);

                fb = random.NextDouble();
                if (fb < tolerancePosition_d)
                    fb += tolerancePosition_d;
                if (fb + fa > tolerancePosition_u)
                    fb = tolerancePosition_u - fa;
                Vector3D pb = Multiply_double_Vector3D(fb, axis_b);
                ray.StartPoint = NewPoint(Addition(NewVector(tri.Vertex1), Addition(pa, pb)));

                Vector3D Z = tri.NormalVector;
                Vector3D X = UnitVector(VectorConstructor(ray.StartPoint, tri.Vertex1));
                Vector3D Y = yAxis(X, Z);
                double a = Math.PI * tolerancePolarAngle * random.NextDouble() / 2; // Random1 != 1 -- Random.nextdouble() : Returns a random floating-point number that is greater than or equal to 0.0, and less than 1.0.
                double b = 2 * Math.PI * random.NextDouble();
                double parameterX = Math.Sin(a) * Math.Cos(b);
                double parameterY = Math.Sin(a) * Math.Sin(b);
                double parameterZ = Math.Cos(a);
                ray.Direction.x = parameterX * X.x + parameterY * Y.x + parameterZ * Z.x;
                ray.Direction.y = parameterX * X.y + parameterY * Y.y + parameterZ * Z.y;
                ray.Direction.z = parameterX * X.z + parameterY * Y.z + parameterZ * Z.z;
                ray.Direction = UnitVector(ray.Direction);

                result.Add(ray);
            }
            return result;
        }

        public List<Ray3D> Rays_Origin_MonteCarlo(Point3D Origin, Vector3D LocalZ, Triangle3D xyplane, Int64 NumRay, double scale)
        {
            List<Ray3D> RaySet = new List<Ray3D>();
         
            Random random = new Random();
            Point3D p = new Point3D();
            Vector3D X = new Vector3D();
            if (!p.IsSamePoint(Origin, xyplane.Vertex1, 0.00001/scale))
                X = UnitVector(VectorConstructor(Origin, xyplane.Vertex1));
            else
                X = UnitVector(VectorConstructor(Origin, xyplane.Vertex2));
            Vector3D Y = yAxis(X, LocalZ);

            for (Int64 i = 0; i < NumRay; i++)
            {
                Ray3D ray = new Ray3D();

                ray.StartPoint.x = Origin.x;
                ray.StartPoint.y = Origin.y;
                ray.StartPoint.z = Origin.z;

                double a = Math.PI * 89.9/90.0 * random.NextDouble() / 2; // Random1 != 1 -- Random.nextdouble() : Returns a random floating-point number that is greater than or equal to 0.0, and less than 1.0.
                double b = 2 * Math.PI * random.NextDouble();
                double parameterX = Math.Sin(a) * Math.Cos(b);
                double parameterY = Math.Sin(a) * Math.Sin(b);
                double parameterZ = Math.Cos(a);
                ray.Direction.x = parameterX * X.x + parameterY * Y.x + parameterZ * LocalZ.x;
                ray.Direction.y = parameterX * X.y + parameterY * Y.y + parameterZ * LocalZ.y;
                ray.Direction.z = parameterX * X.z + parameterY * Y.z + parameterZ * LocalZ.z;
                ray.Direction = UnitVector(ray.Direction);

                RaySet.Add(ray);
            }
            return RaySet;
        }

        public List<Ray3D> Rays_Origin_Determine(Point3D Origin, Vector3D LocalZ, Triangle3D xyplane, double angle_interval, double scale)
        {
            List<Ray3D> RaySet = new List<Ray3D>();

            Random random = new Random();
            Point3D p = new Point3D();
            Vector3D X = new Vector3D();
            if (!p.IsSamePoint(Origin, xyplane.Vertex1, 0.00001 / scale))
                X = UnitVector(VectorConstructor(Origin, xyplane.Vertex1));
            else
                X = UnitVector(VectorConstructor(Origin, xyplane.Vertex2));
            Vector3D Y = yAxis(X, LocalZ);

            Vector3D v = new Vector3D();
            double num_xyplane = 360.0 / angle_interval;        

            for (int i = 0; i < num_xyplane; i++)
            {
                Vector3D _X = v.Addition(v.Multiple(X, Math.Cos(i * angle_interval * Math.PI / 180)), v.Multiple(Y, Math.Sin(i * angle_interval * Math.PI / 180)));
                //Vector3D _Y = v.Addition(v.Multiple(X, Math.Sin(i * angle_interval * Math.PI / 180)*(-1)), v.Multiple(Y, Math.Cos(i * angle_interval * Math.PI / 180)));
                RaySet.AddRange(ComputeDetermineRaysForAHalfPlane(Origin, _X, LocalZ, angle_interval));
            }
            return RaySet;
        }

        private List<Ray3D> ComputeDetermineRaysForAHalfPlane(Point3D Origin, Vector3D XAxis, Vector3D ZAxis, double angle_interval)
        {
            List<Ray3D> RaySet = new List<Ray3D>();

            Vector3D v = new Vector3D();
            double num_zplnae = 180.0 / angle_interval;

            if (angle_interval > 1)
            {
                Ray3D ray_0 = new Ray3D();
                ray_0.StartPoint.x = Origin.x;
                ray_0.StartPoint.y = Origin.y;
                ray_0.StartPoint.z = Origin.z;
                ray_0.Direction = v.Addition(v.Multiple(XAxis, Math.Cos(1 * Math.PI / 180)), v.Multiple(ZAxis, Math.Sin(1 * Math.PI / 180)));
                RaySet.Add(ray_0);

                Ray3D ray_l = new Ray3D();
                ray_l.StartPoint.x = Origin.x;
                ray_l.StartPoint.y = Origin.y;
                ray_l.StartPoint.z = Origin.z;
                ray_l.Direction = v.Addition(v.Multiple(XAxis, Math.Cos(179 * Math.PI / 180)), v.Multiple(ZAxis, Math.Sin(179 * Math.PI / 180)));
                RaySet.Add(ray_l);
            }
 
            for (int j = 1; j < num_zplnae; j++)
            {
                Ray3D ray_m = new Ray3D();
                ray_m.StartPoint.x = Origin.x;
                ray_m.StartPoint.y = Origin.y;
                ray_m.StartPoint.z = Origin.z;
                ray_m.Direction = v.Addition(v.Multiple(XAxis, Math.Cos(j * angle_interval * Math.PI / 180)), v.Multiple(ZAxis, Math.Sin(j * angle_interval * Math.PI / 180)));
                RaySet.Add(ray_m);
            }

            return RaySet;
        }


        // Amy Williams, Steve Barrus, R. Keith Morley, and Peter Shirley: "An
        // Efficient and Robust Ray-Box Intersection Algorithm" Journal of graphics tools, 10(1):49-54, 2005
        // This described algorithm using IEEE numerical properties to ensure the intersection test is both robust and efficient
        public bool Ray_AABB_do_intersection (Ray3D Ray, BoundingBox3D AABB)
        {
            double txmin;
            double txmax;
            double tymin;
            double tymax;
            double tzmin;
            double tzmax;

            Vector3D InRayDir = new Vector3D();
            InRayDir.x = 1 / Ray.Direction.x;   // Can store in the Ray structure instead of calculting in each time of ray - box inersection test 
            InRayDir.y = 1 / Ray.Direction.y;   // Can store in the Ray structure instead of calculting in each time of ray - box inersection test 
            InRayDir.z = 1 / Ray.Direction.z;   // Can store in the Ray structure instead of calculting in each time of ray - box inersection test 

            if (InRayDir.x >= 0)
            {
                txmin = (AABB.xmin - Ray.StartPoint.x) * InRayDir.x;
                txmax = (AABB.xmax - Ray.StartPoint.x) * InRayDir.x;
            }
            else
            {
                txmin = (AABB.xmax - Ray.StartPoint.x) * InRayDir.x;
                txmax = (AABB.xmin - Ray.StartPoint.x) * InRayDir.x;
            }

            if (InRayDir.y >= 0)
            {
                tymin = (AABB.ymin - Ray.StartPoint.y) * InRayDir.y;
                tymax = (AABB.ymax - Ray.StartPoint.y) * InRayDir.y;
            }
            else
            {
                tymin = (AABB.ymax - Ray.StartPoint.y) * InRayDir.y;
                tymax = (AABB.ymin - Ray.StartPoint.y) * InRayDir.y;
            }

            if ((txmin > tymax) || (tymin > txmax))
            {
                return false;
            }

            if (tymin > txmin)
            {
                txmin = tymin;
            }
            if (tymax < txmax)
            {
                txmax = tymax;
            }

            if (InRayDir.z >= 0)
            {
                tzmin = (AABB.zmin - Ray.StartPoint.z) * InRayDir.z;
                tzmax = (AABB.zmax - Ray.StartPoint.z) * InRayDir.z;
            }
            else
            {
                tzmin = (AABB.zmax - Ray.StartPoint.z) * InRayDir.z;
                tzmax = (AABB.zmin - Ray.StartPoint.z) * InRayDir.z;
            }

            if ((txmin > tzmax) || (tzmin > txmax))
            {
                return false;
            }

            if (tzmin > txmin)
            {
                txmin = tzmin;
            }

            if (tzmax < txmax)
            {
                txmax = tzmax;
            }

            if (txmin < 0)
            {
                if (txmax < 0)
                // txmax = (ymax - r.y) / rd.y > 0 means the intersection point is on the "ray" instead of on the "line"  -- as long as the txmax >0, it will add the geometric meaning of "Ray"
                // txmax >= txmin --- always correct
                // txmax = 0 means that if a ray emitted from the surface of the box and point out of the box, the start point will be is recongnised as an intersection (obviously this is not necessary)!!!
                // txmax = tmin means that the instersection point is on the edges of the box (this is required)
                // For ray, the direction vector should be careful to distingusih from a line; as the slope of a ray is equal to the relevant line
                // therefore, the direction should be represented 
                {
                    return false;
                }
            }
            return true;
        }

        // For a triangulated Brep, once an intersection triangle is detected, other triangles on the same plane can be ignored 
        // For some specific shapes consisting of a very large amount of triangles, considering spatial indexing?
        public RayTri_InterPoint Ray_Brep_do_intersection (Ray3D ray, List<List<Triangle3D>> CoplanarTris, double range)
        {
            RayTri_InterPoint baseIP = new RayTri_InterPoint();
            baseIP.dist = 10000000000000;
            foreach (var face in CoplanarTris)
            {
                foreach (var tri in face)
                {
                    RayTri_InterPoint IP = Ray_Triangle_do_Intersection(ray, tri, range);
                    if (IP.intersection)
                    {
                        if (IP.dist < baseIP.dist)
                        {
                            baseIP.dist = IP.dist;
                            baseIP.intersection = true;
                            baseIP.IP = new Point3D();
                            baseIP.IP = IP.IP;
                        }
                        break;
                    }
                }
            }
            return baseIP;
        }

        // For a triangulated Brep, once an intersection triangle is detected, other triangles on the same plane can be ignored 
        // For some specific shapes consisting of a very large amount of triangles, considering spatial indexing?
        public IntersectFace Ray_Brep_do_intersection(Ray3D ray, List<List<Triangle3D>> CoplanarTris, bool usingNormal)
        {
            GeometricOperations GO = new GeometricOperations();
            IntersectFace baseface = new IntersectFace();
            Vector3D v = new Vector3D();

            baseface.dist = 10000000000000;
            for (int i = 0; i < CoplanarTris.Count; i++)
            {
                double dotproduct = -1;
                if (usingNormal)
                    dotproduct = v.DotProduct(ray.Direction, CoplanarTris[i][0].NormalVector);
                if (dotproduct < 0)
                {
                    foreach (var tri in CoplanarTris[i])
                    {
                        RayTri_InterPoint IP = Ray_Triangle_do_Intersection(ray, tri);
                        if (IP.intersection)
                        {
                            if (IP.dist < baseface.dist)
                            {
                                baseface.intersection = true;
                                baseface.dist = IP.dist;
                                // Only for LCS setting up; Actually, storing one triangle is enough
                                // baseface.face_index = i;
                                baseface.face = new List<Triangle3D>();
                                baseface.face.AddRange(CoplanarTris[i]);
                                // Only for LCS setting up
                                baseface.IP = IP.IP;
                                if (GO.DotProduct(ray.Direction, tri.NormalVector) > 0)
                                    baseface.Normal = GO.Multiply_double_Vector3D(-1, tri.NormalVector);
                                else
                                    baseface.Normal = tri.NormalVector;
                            }
                            break;
                        }
                    }
                }                
            }
            return baseface;
        }

        // For a triangulated Brep, once an intersection triangle is detected, other triangles on the same plane can be ignored 
        // For some specific shapes consisting of a very large amount of triangles, considering spatial indexing?
        public IntersectFace Ray_Brep_do_intersection(Ray3D ray, List<Triangle3D> Tris, bool usingNormal)
        {
            GeometricOperations GO = new GeometricOperations();
            IntersectFace baseface = new IntersectFace();
            Vector3D v = new Vector3D();

            baseface.dist = 10000000000000;
            for (int i = 0; i < Tris.Count; i++)
            {
                double dotproduct = -1;
                if (usingNormal)
                    dotproduct = v.DotProduct(ray.Direction, Tris[i].NormalVector);
                if (dotproduct < 0)
                {
                    RayTri_InterPoint IP = Ray_Triangle_do_Intersection(ray, Tris[i]);
                    if (IP.intersection)
                    {
                        if (IP.dist < baseface.dist)
                        {
                            baseface.intersection = true;
                            baseface.dist = IP.dist;
                            // Only for LCS setting up; Actually, storing one triangle is enough
                            baseface.face = new List<Triangle3D>();
                            baseface.face.Add(Tris[i]);
                            // Only for LCS setting up
                            baseface.IP = IP.IP;
                        }
                    }
                }
            }
            return baseface;
        }

        public bool AABB_AABB_IntersectionTest(BoundingBox3D A, BoundingBox3D B)
        {
            if (A.xmax <= B.xmin || A.xmin >= B.xmax)
                return false;
            if (A.ymax <= B.ymin || A.ymin >= B.ymax)
                return false;
            if (A.zmax <= B.zmin || A.zmin >= B.zmax)
                return false;
            return true;
        }

        // !!!: Tolerance setting: 0.00001/2
        // !!!: The correctness of the normal direction does not affect the result
       public bool isCoplanar(Polyline3D surfaceA, Polyline3D surfaceB, double ang, double dist_mm)
        {
            bool test = false;

            Vector3D vector = new Vector3D();
            Vector3D SA = surfaceA.SurfaceNormal;
            Vector3D SB = surfaceB.SurfaceNormal;

            double d = vector.DotProduct(SA, SB);
            double d1 = Math.Abs(Math.Abs(d) - 1);
            if (d1 < ang) // Coplanar or parallal         
            {
                Point3D p = new Point3D();
                Vector3D AB = new Vector3D();

                if (!p.IsSamePoint(surfaceA.Vertices[0], surfaceB.Vertices[0], 0.00001))
                    AB = vector.VectorConstructor(surfaceA.Vertices[0], surfaceB.Vertices[0]);
                else
                    AB = vector.VectorConstructor(surfaceA.Vertices[0], surfaceB.Vertices[1]);

                double d2 = vector.DotProduct(AB, SA);
                if (Math.Abs(d2) < dist_mm)  // coplanar: 2mm (for two prallal surfaces only)
                    test = true;
            }
            return test;
        }

        public double[,] Affine_Matrix(Point3D Origin, Vector3D _xAxis, Vector3D _zAxis)
        {
            double[,] Rotat_Matrix = Rot_Matrix(_xAxis, _zAxis); // 4* 4
            Rotat_Matrix[0, 3] = Origin.x;
            Rotat_Matrix[1, 3] = Origin.y;
            Rotat_Matrix[2, 3] = Origin.z;

            return Rotat_Matrix;
        }

        public double[,] Affine_Matrix(Point3D Origin, Vector3D _xAxis, Vector3D _yAxis, Vector3D _zAxis)
        {
            double[,] matrix = new double[4,4];

            matrix[0, 0] = _xAxis.x;
            matrix[1, 0] = _xAxis.y;
            matrix[2, 0] = _xAxis.z;
            matrix[3, 0] = 0;

            matrix[0, 1] = yAxis(_xAxis, _zAxis).x;
            matrix[1, 1] = yAxis(_xAxis, _zAxis).y;
            matrix[2, 1] = yAxis(_xAxis, _zAxis).z;
            matrix[3, 1] = 0;

            matrix[0, 2] = _zAxis.x;
            matrix[1, 2] = _zAxis.y;
            matrix[2, 2] = _zAxis.z;
            matrix[3, 2] = 0;

            matrix[0, 3] = Origin.x;
            matrix[1, 3] = Origin.y;
            matrix[2, 3] = Origin.z;
            matrix[3, 3] = 1;

            return matrix;
        }

        // Construct a 4X4 matrix --- for rotation
        private double[,] Rot_Matrix(Vector3D _xAxis, Vector3D _zAxis)
        {
            double[,] Matrix2 = new double[4, 4];
            Matrix2[0, 0] = _xAxis.x;
            Matrix2[1, 0] = _xAxis.y;
            Matrix2[2, 0] = _xAxis.z;
            Matrix2[3, 0] = 0;
            Matrix2[0, 1] = yAxis(_xAxis, _zAxis).x;
            Matrix2[1, 1] = yAxis(_xAxis, _zAxis).y;
            Matrix2[2, 1] = yAxis(_xAxis, _zAxis).z;
            Matrix2[3, 1] = 0;
            Matrix2[0, 2] = _zAxis.x;
            Matrix2[1, 2] = _zAxis.y;
            Matrix2[2, 2] = _zAxis.z;
            Matrix2[3, 2] = 0;
            Matrix2[0, 3] = 0;
            Matrix2[1, 3] = 0;
            Matrix2[2, 3] = 0;
            Matrix2[3, 3] = 1;
            return Matrix2;
        }

        private Vector3D yAxis(Vector3D _xAxis, Vector3D _zAxis)
        {
            Vector3D _yAxis = new Vector3D();

            _yAxis.x = _zAxis.y * _xAxis.z - _xAxis.y * _zAxis.z;
            _yAxis.y = (-1) * (_zAxis.x * _xAxis.z - _xAxis.x * _zAxis.z);
            _yAxis.z = _zAxis.x * _xAxis.y - _xAxis.x * _zAxis.y;

            return _yAxis.UnitVector(_yAxis);
        }

        public double[,] IMatrix_Matrix(double[,] input)
        {
            Matrix<double> A = DenseMatrix.OfArray(input);
            Matrix<double> Inverse_A = A.Inverse();

            double[,] output = Inverse_A.ToArray();

            return output;
        }
    }

    public class RayTri_InterPoint
    {
        public bool intersection = false;
        public Point3D IP = new Point3D();
        public double dist = 0;
    }

    public class IntersectFace
    {
        public bool intersection = false;
        public double dist = 0;
        //public int face_index = -1;
        public List<Triangle3D> face = new List<Triangle3D>();
        public Point3D IP = new Point3D();
        public Vector3D Normal = new Vector3D(); // outward normal
    }

    public enum Plane
    {
        error,
        xyplane,
        xzplane,
        yzplane
    }

    public class debug
    {
        public void outputFace(Polyline3D poly, string name)
        {
            StreamWriter txtWriter = new StreamWriter("C:\\Users\\Huanquan Ying\\Desktop\\ViewImages\\Polyline" + name, true);
            string sb = null;
            foreach (var vertex in poly.Vertices)
                sb += vertex.x.ToString() + ' ' + vertex.y.ToString() + ' ' + vertex.z.ToString() + ' ';
            txtWriter.WriteLine(sb);
            txtWriter.Close();
        }

        public void outputFaces(List<Polyline3D> polys, string name)
        {
            StreamWriter txtWriter = new StreamWriter("C:\\Users\\Huanquan Ying\\Desktop\\" + name, true);
            foreach (var s in polys)
            {
                string sb = null;
                foreach (var vertex in s.Vertices)
                    sb += vertex.x.ToString() + ' ' + vertex.y.ToString() + ' ' + vertex.z.ToString() + ' ';

                txtWriter.WriteLine(sb);
            }
            txtWriter.Close();
        }
    }
}
