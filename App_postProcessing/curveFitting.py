import numpy as np
import math
import IfcEngine
from circle_fit import taubinSVD
import cv2
import alphashape
import scipy.optimize
from skspatial.objects import Line, Circle
import shapely.geometry as sg


def angles_to_vector(a, b):
    x = math.sin(a) * math.cos(b)
    y = math.sin(a) * math.sin(b)
    z = math.cos(a)

    r = math.sqrt(x*x + y*y + z*z)
    [x, y, z] = [x, y, z] / r

    return [x, y, z]


# threshold unit: mm
def equal(a, b, thresh_mm):
    if math.abs(a[0] - b[0]) > thresh_mm:
        return False
    if math.abs(a[1] - b[1]) > thresh_mm:
        return False
    if math.abs(a[2] - b[2]) > thresh_mm:
        return False
    return True


def norm(a):
    if len(a) == 2:
        return a / math.sqrt(a[0]*a[0] + a[1]*a[1])
    elif len(a) == 3:
        return a / math.sqrt(a[0]*a[0] + a[1]*a[1] + a[2]*a[2])


def angleBetweenVectors(vect1, vect2):
    vect1 = np.array(norm(vect1))
    vect2 = np.array(norm(vect2))
    angle = math.acos(np.dot(vect1, vect2)) * 180 / math.pi
    return angle


def project_point_on_line(point, line_dir, line_point):
    # !!! the change of line_point makes the length non-meaningful for different points
    # if equal(point, line_point, 1):
    #     line_point = line_point + line_dir * 5

    vect = point - line_point
    length = np.dot(np.array(vect), np.array(line_dir))
    return length, line_point + length * line_dir


# PC: (n, 3); direction_angles: (a, b)
def projectPC(PC, direction_vector):
    # project PC on the line that passes through origin to find the footprint plane
    PC = np.array(PC)
    direction_vector = np.array(direction_vector)
    lengths = [np.dot(point, direction_vector) for point in PC]
    index_mini = lengths.index(min(lengths))

    point_footprintPlane = lengths[index_mini] * direction_vector
    normal_footprintPlane = direction_vector

    # project PC on the footprint plane
    points_3D_GCS = []
    for point in PC:
        pp = point - point_footprintPlane
        len = np.dot(pp, normal_footprintPlane)
        point_ = point - len * normal_footprintPlane
        points_3D_GCS.append(point_)

    local_z = np.array(normal_footprintPlane)
    for point in points_3D_GCS:
        if not equal(point, point_footprintPlane, 1):
            local_x = np.array(norm(point - point_footprintPlane))
            break
    local_y = norm(np.cross(local_z, local_x))

    TM_LCS2GCS = np.concatenate((local_x.T, local_y.T), axis=1)
    TM_LCS2GCS = np.concatenate((TM_LCS2GCS, local_z.T), axis=1)
    TM_LCS2GCS = np.concatenate((TM_LCS2GCS, np.array(point_footprintPlane).T), axis=1)
    TM_LCS2GCS = np.concatenate((TM_LCS2GCS, np.array([0,0,0,1])), axis=0)

    TM_GCS2LCS = np.linalg.inv(TM_LCS2GCS)
    points_3D_GCS = np.array(points_3D_GCS)
    points_2D_LCS = np.matmul(np.concatenate((points_3D_GCS, np.ones(points_3D_GCS.shape[0], 1)), axis=1), TM_GCS2LCS)
    points_2D_LCS = points_2D_LCS[:, : -2]

    return points_3D_GCS, points_2D_LCS, TM_LCS2GCS


def func_ls(x, a, b):
    return a*x+b


def curveFitting_ls(points_2D):
    points_2D = np.array(points_2D)
    x = points_2D[:, 0]
    y = points_2D[:, 1]
    popt, pcov = scipy.optimize.curve_fit(func_ls, x, y)

    ang = math.atan(popt[0])
    direct = [math.cos(ang), math.sin(ang)]

    lengths_points = [project_point_on_line(point, direct, [0, popt[1]]) for point in points_2D]
    lengths = lengths_points[:, 0]
    points = lengths_points[:, 1]
    min_index = lengths.index(min(lengths))
    max_index = lengths.index(max(lengths))

    endpoints = [points[min_index], points[max_index]]
    return endpoints


def updateEndPoint_ls(lineSegment, point):
    dist1 = np.linalg.norm(lineSegment[0] - point)
    dist2 = np.linalg.norm(lineSegment[1] - point)

    if dist1 > dist2:
        lineSegment[1] = point
    else:
        lineSegment[0] = point

    return lineSegment


# merge LSs to polygon (!!! first vertex does not repeat as the last one)
def polygon_mergeLSs(lineSegments, ordered=True, continuous=False):
    vertices = []
    if ordered and not continuous:
        for i in range(len(lineSegments)-1):
            line_a = Line(point=lineSegments[i][0], direction=lineSegments[i][1] - lineSegments[i][0])
            line_b = Line(point=lineSegments[i+1][0], direction=lineSegments[i+1][1] - lineSegments[i+1][0])
            intersection = line_a.intersect_line(line_b)
            lineSegments[i] = updateEndPoint_ls(lineSegments[i], intersection)
            lineSegments[i+1] = updateEndPoint_ls(lineSegments[i + 1], intersection)

        line_last = Line(point=lineSegments[-1][0], direction=lineSegments[-1][1] - lineSegments[-1][0])
        line_first = Line(point=lineSegments[0][0], direction=lineSegments[0][1] - lineSegments[0][0])
        intersect = line_last.intersect_line(line_first)
        lineSegments[-1] = updateEndPoint_ls(lineSegments[-1], intersect)
        lineSegments[0] = updateEndPoint_ls(lineSegments[0], intersect)

        vertices = polygon_mergeLSs(lineSegments, True, True)
        return vertices
    elif ordered and continuous:
        for j in range(len(lineSegments)-1):
            if equal(lineSegments[j][1], lineSegments[j+1][0], 0.0001):
                if not equal(lineSegments[j][0], vertices[-1], 0.0001):
                    vertices.append(lineSegments[j][0])
                vertices.append(lineSegments[j][1])
            else:
                if not equal(lineSegments[j][1], vertices[-1], 0.0001):
                    vertices.append(lineSegments[j][1])
                vertices.append(lineSegments[j][0])
        poly = sg.Polygon(vertices)
        if not poly.exterior.is_CCW:
            vertices = np.flipud(vertices)
        return vertices
    else:
        pass


# counter-clockwise arc
def curveFitting_arc(points_2D):
    # circle-fit (optimization method: fit circle to the points)
    xc, yc, r, sigma = taubinSVD(points_2D)

    points = [point - [xc, yc] for point in points_2D]

    num_1quartile = len([p for p in points if p.x > 0 and p.y > 0])
    num_4quartile = len([p for p in points if p.x > 0 and p.y < 0])
    num_3quartile = len([p for p in points if p.x < 0 and p.y < 0])

    span = True if num_1quartile > 0 and num_4quartile > 0 and num_3quartile == 0 else False

    angles = []
    for p in points:
        angle = 0
        if span:
            if abs(p.x) <= 0.01:
                if p.y > 0:
                    angle = math.pi / 2 + 2 * math.pi
                else:
                    angle = math.pi * 1.5
            else:
                angle = math.atan(p.y/p.x)   # -math.pi/2, math.pi/2
                if p.x > 0:   # maximum
                    angle = angle + 2 * math.pi
                if p.x < 0:   # minimum; (!!! if maximum, + 2 math.pi)
                    angle = angle + math.pi
        else:
            if abs(p.x) <= 0.01:
                if p.y > 0:
                    angle = 2 * math.pi
                else:
                    angle = math.pi * 1.5
            else:
                angle = math.atan(p.y/p.x)   # -math.pi/2, math.pi/2
                if p.x > 0 and p.y < 0:
                   angle = angle + 2 * math.pi
                if p.x < 0:
                    angle = angle + math.pi
    angles.append(angle)
    return xc, yc, r, min(angles), max(angles), "CCW"


def dist_point_line_2D(point, line):
    point = np.array(point)
    line = np.array(line)
    dist = np.abs(np.linalg.norm(np.cross(line[1]-line[0], line[0]-point)))/np.linalg.norm(line[1]-line[0])

    return dist


# detect corner points
# return the index of corner points orderly
def detectCorners(polygon, angle_thresh):
    num_vertes = len(polygon)
    if num_vertes == 3:
        return [0, 1, 2]

    corners_index = []
    for i in range(num_vertes - 1):
        if i != corners_index[-1]:
            corners_index.append(i)
        vect1 = polygon[i+1] - polygon[i]
        if (i+1) == num_vertes - 1:
            nn = polygon[0]
        else:
            nn = polygon[i+2]
        vect2 = nn - polygon[i+1]
        if angleBetweenVectors(vect1, vect2) < angle_thresh:
            next = findNextCorner(polygon, i, i+2)
        else:
            next = i+1
        corners_index.append(next)
        i = next

    vect1_ = polygon[corners_index[1]] - polygon[corners_index[0]]
    vect2_ = polygon[corners_index[0]] - polygon[corners_index[-1]]
    if angleBetweenVectors(vect1_, vect2_) < angle_thresh:
        corners_index = corners_index[1:]

    return corners_index


def findNextCorner(polygon, last, start, angle_thresh):
    if (start+1) <= len(polygon) -1:
        vect1 = polygon[start] - polygon[last]
        vect2 = polygon[start+1] - polygon[start]
        if angleBetweenVectors(vect1, vect2) < angle_thresh:
            findNextCorner(polygon, last, start+1, angle_thresh)
        else:
            return start


# group points according to the edges of the polygon in order
def groupPoints(points, polygon):
    edges = []
    for i in range(len(polygon) - 1):
        edges.append([polygon[i], polygon[i + 1]])
    edges.append([polygon[-1], polygon[0]])
    pointGroups = {}
    for point in points:
        dists = [dist_point_line_2D(point, ls) for ls in edges]
        min_value = min(dists)
        indexes = [index for index, value in enumerate(dists) if value == min_value]
        for index in indexes:
            if index in pointGroups:
                pointGroups[index].append(point)
            else:
                pointGroups[index] = [point]
    # get ordered groups consistent with edges
    pointGroups = dict(sorted(pointGroups.items()))

    return pointGroups


# convert shapely POLYGON format into numpy array
def POLYGON2NumPy(Polygon):
    xx, yy = Polygon.exterior.coords.xy
    xx = np.array(xx.tolist())
    yy = np.array(yy.tolist())

    zz = np.vstack((xx, yy))
    return zz.T


def groupPointsByCorners(points_2D, alpha, angle_thresh):
    # find concave hull: counter-wise in POLYGON type; repeat first vertex as the las one
    polygon = alphashape.alphashape(points_2D, alpha)
    polygon = POLYGON2NumPy(polygon)     # convert into numpy array
    polygon = polygon[:-1, :]            # remove repeat vertex
    polygon = np.flipud(polygon)         # reverse the polygon direction

    # group points to edges of concave hull
    pointGroups = groupPoints(points_2D, polygon)

    # get the index of corners of polygon
    corners_index = detectCorners(polygon, angle_thresh)

    # further merge pointGroups according to detected corners
    pointGroups_corner = []
    num_corner = len(corners_index)
    for i in range(num_corner-1):
        points = []
        for j in range(corners_index[i], corners_index[i+1]):
            points.extend(pointGroups[j])
        pointGroups_corner.append(points)

    if corners_index[-1] == len(polygon) - 1:
        pointGroups_corner.append(pointGroups[-1])
    else:
        points = []
        for k in range(corners_index[-1], len(polygon)):
            points.extend(pointGroups[k])
        points.extend(pointGroups[len(polygon)-1])
        pointGroups_corner.append(points)

    return pointGroups_corner


# fit point sets with a rectangle
def curveFitting_rect(points_2D):
    # find minimal area rect first
    rect = cv2.minAreaRect(points_2D)
    corners = cv2.boxpoints(rect)  # clockwise
    corners = np.flipud(corners)  # counter-clockwise

    # subdivide point set into four groups corresponding minAreaRect's 4 edges
    pointGroups = groupPoints(points_2D, corners)

    # fit a line segment to each group
    lineSegments = [curveFitting_ls(group) for group in pointGroups.values()]

    # merge ordered, uncontinuous line sgements to create a polygon
    corners_fit = polygon_mergeLSs(lineSegments, True, False)

    # reconstruct mini rect from the fitted polygon
    rect = cv2.minAreaRect(corners_fit)
    corners_fit = cv2.boxpoints(rect)  # clockwise
    corners_fit = np.flipud(corners_fit)  # counter-clockwise

    return corners_fit


def curveFitting_polygon(points_2D, alpha, angle_thresh):
    # group points by detected corners
    pointGroups = groupPointsByCorners(points_2D, alpha, angle_thresh)

    # fit each group of points with line segment
    lineSegments = [curveFitting_ls(group) for group in pointGroups]

    # merge line segments to create polygon
    vertices = polygon_mergeLSs(lineSegments)

    return vertices


def findCorrectIntersection_ls(intersect_a, intersect_b, refPoint):
    dist_a = np.linalg.norm(intersect_a-refPoint)
    dist_b = np.linalg.norm(intersect_b-refPoint)
    if dist_a < dist_b:
        return intersect_a
    else:
        return intersect_b


def findCorrectIntersection_arc(intersect_a, intersect_b, refPoint1, refPoint2):
    dist_a1 = np.linalg.norm(intersect_a-refPoint1)
    dist_a2 = np.linalg.norm(intersect_a-refPoint2)
    dist_b1 = np.linalg.norm(intersect_b-refPoint1)
    dist_b2 = np.linalg.norm(intersect_b-refPoint2)

    if min([dist_a1, dist_a2]) < min([dist_b1, dist_b2]):
        return intersect_a
    else:
        return intersect_b


def updateEndPoint_arc(arc, point):
    endpoint_min = [arc[0] + math.cos(arc[3]) * arc[2], arc[1] + math.sin(arc[3]) * arc[2]]
    endpoint_max = [arc[0] + math.cos(arc[4]) * arc[2], arc[1] + math.sin(arc[4]) * arc[2]]
    dist_min = np.linalg.norm(endpoint_min - point)
    dist_max = np.linalg.norm(endpoint_max - point)

    if abs(point.x - arc[0]) < 0.0001:
        angle = math.pi / 2 if point.y > 0 else 1.5 * math.pi
    else:
        angle = math.atan((point.y-arc[1])/(point.x - arc[0]))

    if dist_min < dist_max:
        if point.x - arc[0] < 0:
            angle = angle + math.pi
        if point.x - arc[0] > 0 and point.y - arc[1] < 0:
            angle = angle + math.pi * 2
        arc[3] = angle
    else:
        if arc[4] > math.pi * 2:  # span
            if point.x - arc[0] > 0 and point.y - arc[1] > 0:
                angle = angle + math.pi * 2
            if point.x - arc[0] < 0:
                angle = angle + math.pi * 3
            if point.x - arc[0] > 0 and point.y - arc[1] < 0:
                angle = angle + math.pi * 4
        else:
            if point.x - arc[0] < 0:
                angle = angle + math.pi
            if point.x - arc[0] > 0 and point.y - arc[1] < 0:
                angle = angle + math.pi * 2
        arc[4] = angle
    return arc


# !!! there may be a bug if the arc's direction (CCW) is not consistent with footprint's CCW
def merge_ls_arc(footprint, ordered=True, continuous=False):
    if ordered and not continuous:
        num_curve = len(footprint)
        for i in range(num_curve):
            if footprint[i][0] == "ls":
                la = Line(point=footprint[i][1][0], direction=footprint[i][1][1] - footprint[i][1][0])
                if footprint[i+1][0] == "ls":
                    lb = Line(point=footprint[i+1][1][0], direction=footprint[i+1][1][0] - footprint[i+1][1][0])
                    intersection = la.intersect_line(lb)
                    footprint[i][1] = updateEndPoint_ls(footprint[i][1], intersection)
                    footprint[i+1][1] = updateEndPoint_ls(footprint[i + 1][1], intersection)
                elif footprint[i+1][0] == "arc":
                    arcb = Circle([footprint[i+1][1][0], footprint[i+1][1][1]], footprint[i+1][1][2])
                    pa, pb = arcb.intersect_line(la)
                    point = findCorrectIntersection_ls(pa, pb, footprint[i][1][0])
                    footprint[i][1] = updateEndPoint_ls(footprint[i][1], point)
                    footprint[i+1][1] = updateEndPoint_arc(footprint[i+1][1], point)
            elif footprint[i][0] == "arc":
                arca = Circle([footprint[i][1][0], footprint[i][1][1]], footprint[i][1][2])
                if footprint[i+1][0] == "ls":
                    lb = Line(point=footprint[i+1][1][0], direction=footprint[i+1][1][0] - footprint[i+1][1][0])
                    pa, pb = arca.intersect_line(lb)
                    point = findCorrectIntersection_ls(pa, pb, footprint[i + 1][1][0])
                    footprint[i][1] = updateEndPoint_arc(footprint[i][1], point)
                    footprint[i+1][1] = updateEndPoint_ls(footprint[i+1][1], point)
                elif footprint[i+1][0] == "arc":
                    arcb = Circle([footprint[i+1][1][0], footprint[i+1][1][1]], footprint[i+1][1][2])
                    pa, pb = arca.intersect_circle(arcb)
                    ra = footprint[i][1][2]
                    ang1a = footprint[i][1][3]
                    ang2a = footprint[i][1][4]
                    ref1 = [footprint[i][1][0] + math.cos(ang1a) * ra, footprint[i][1][1] + math.sin(ang1a) * ra]
                    ref2 = [footprint[i][1][0] + math.cos(ang2a) * ra, footprint[i][1][1] + math.sin(ang2a) * ra]
                    point = findCorrectIntersection_arc(pa, pb, ref1, ref2)
                    footprint[i][1] = updateEndPoint_arc(footprint[i][1], point)
                    footprint[i+1][1] = updateEndPoint_arc(footprint[i+1][1], point)
            if i == num_curve-2:
                footprint.append(footprint[0])
        footprint[0] = footprint[-1]
        footprint = footprint[:-1]
        footprint = merge_ls_arc(footprint, True, True)
        return footprint
    elif ordered and continuous:
        for j in range(len(footprint)):
            if j < len(footprint) - 1:
                if footprint[j][0] == "ls":
                    if footprint[j+1][0] == "ls":
                        if not equal(footprint[j][1][1], footprint[j+1][1][0], 0.0001):
                            footprint[j+1][1] = [footprint[j+1][1][1], footprint[j+1][1][0]]
                    elif footprint[j+1][0] == "arc":
                        arc = footprint[j+1][1]
                        endpoint_min = [arc[0] + math.cos(arc[3]) * arc[2], arc[1] + math.sin(arc[3]) * arc[2]]
                        if not equal(footprint[j][1][1], endpoint_min, 0.0001):
                            # !!! exchange angle_min, angle_max and direction
                            # angles does not affect endpoint's coordinates
                            # in IFC, arc representation use coordinates
                            footprint[j+1][1][3:] = [footprint[j+1][1][4], footprint[j+1][1][3], "CW"]
                elif footprint[j][0] == "arc":
                    if footprint[j+1][0] == "ls":
                        arc = footprint[j][1]
                        endpoint_max = [arc[0] + math.cos(arc[4]) * arc[2], arc[1] + math.sin(arc[4]) * arc[2]]
                        if not equal(endpoint_max, footprint[j+1][1][0], 0.0001):
                            footprint[j+1][1] = [footprint[j+1][1][1], footprint[j+1][1][0]]
                    elif footprint[j+1][0] == "arc":
                        arc_a = footprint[j][1]
                        endpoint_max = [arc_a[0] + math.cos(arc_a[4]) * arc_a[2], arc_a[1] + math.sin(arc_a[4]) * arc_a[2]]
                        arc_b = footprint[j+1][1]
                        endpoint_min = [arc_b[0] + math.cos(arc_b[3]) * arc_b[2], arc_b[1] + math.sin(arc_b[3]) * arc_b[2]]
                        if not equal(endpoint_max,  endpoint_min , 0.0001):
                            footprint[j+1][1][3:] = [footprint[j+1][1][4], footprint[j+1][1][3], "CW"]
        # todo: confirm whether footprint is CCW
        return footprint
    else:
        pass


# fit point sets with line segments and/or arcs
# return an ordered closed area profile (counter-clockwise)
def curveFitting_ls_arc(points_2D):

    """
    :param points_2D:
    :return: footprint: [["ls", [point]], ["arc", [xc, yc, r, min_ang, max_ang, dir]], ...]
    """

    # group points by detected corners
    pointGroups = groupPointsByCorners(points_2D, 2.0, 15)

    # for each group of points, fit either a line segment or an acr
    footprint = []
    for group in pointGroups:
        if len(group) == 2:  # line segment
            footprint.append(["ls", group])
        elif len(group) > 2:
            xc, yc, r, min_ang, max_ang = curveFitting_arc(group)
            if r > 100000:
                ls = curveFitting_ls(group)  # [point]
                footprint.append(["ls", ls])
            else:
                footprint.append(["arc", [xc, yc, r, min_ang, max_ang, "CCW"]])

    # merge line segments and arcs to create footprint
    footprint = merge_ls_arc(footprint, True, False)
    return footprint


# todo: consider curved sections
def curveFitting_IShape(points_2D):
    footprint = {}

    # fit point set with polygon (CCW)
    vertices = curveFitting_polygon(points_2D, 2, 60)

    # find the longest edge
    len_edges = {}
    vertices.append(vertices[0])

    for i in range(len(vertices)-1):
        len_edges[str(i)] = np.linalg.norm(vertices[i+1] - vertices[i])
    len_edges = dict(sorted(len_edges.items(), key=lambda item: item[1]))

    longest_1st, _ = len_edges.items()[-1]
    center1 = (vertices[longest_1st] + vertices[longest_1st+1]) / 2

    # get orientated BB of points
    # rect: (center, width, height, angle): angle refers to the rotation angle of the rect WRT x-axis
    rect = cv2.minAreaRect(points_2D)
    center = rect[0]
    footprint["origin_LCS"] = center

    corners = cv2.boxpoints(rect)  # clockwise
    centroid1a = (corners[0] + corners[1]) / 2
    centroid1b = (corners[2] + corners[3]) / 2
    centroid2a = (corners[1] + corners[2]) / 2
    centroid2b = (corners[3] + corners[0]) / 2

    dist1_1a = np.linalg.norm(centroid1a-center1)
    dist1_1b = np.linalg.norm(centroid1b-center1)
    dist1_2a = np.linalg.norm(centroid2a-center1)
    dist1_2b = np.linalg.norm(centroid2b-center1)

    # use fitted polygon to derive parameters
    if dist1_1a < 5 or dist1_1b < 5 or dist1_2a < 5 or dist1_2b < 5:  # longest_1st -> horizontal
        footprint["Yaxis_LCS"] = norm(center1 - center)
        y3D = np.array(footprint["Yaxis_LCS"].append(0))
        z3D = np.array([0, 0, 1])
        x3D = np.cross(y3D, z3D)
        footprint["Xaxis_LCS"] = x3D[:2].tolist()

        footprint["OverallWidth"] = np.linalg.norm(vertices[longest_1st+1] - vertices[longest_1st])
        if longest_1st+2 > len(vertices)-1:   # last vertex repeated with first one
            footprint["FlangeThickness"] = np.linalg.norm(vertices[1] - vertices[longest_1st+1])
            footprint["WebThickness"] = footprint["OverallWidth"] - 2 * np.linalg.norm(vertices[2] - vertices[1])
            footprint["OverallDepth"] = footprint["FlangeThickness"]*2 + np.linalg.norm(vertices[3] - vertices[2])
        else:
            footprint["FlangeThickness"] = np.linalg.norm(vertices[longest_1st+2] - vertices[longest_1st + 1])
            if longest_1st + 3 > len(vertices) - 1:
                footprint["WebThickness"] = footprint["OverallWidth"] - 2 * np.linalg.norm(
                    vertices[1] - vertices[longest_1st + 2])
                footprint["OverallDepth"] = footprint["FlangeThickness"] * 2 + np.linalg.norm(
                    vertices[2] - vertices[1])
            else:
                footprint["WebThickness"] = footprint["OverallWidth"] - 2 * np.linalg.norm(
                    vertices[longest_1st + 3] - vertices[longest_1st + 2])
                if longest_1st + 4 > len(vertices) - 1:
                    footprint["OverallDepth"] = footprint["FlangeThickness"] * 2 + np.linalg.norm(
                    vertices[1] - vertices[longest_1st + 3])
                else:
                    footprint["OverallDepth"] = footprint["FlangeThickness"] * 2 + np.linalg.norm(
                        vertices[longest_1st + 4] - vertices[longest_1st + 3])
    else:   # longest_1st -> vertical
        footprint["Xaxis_LCS"] = norm(center1 - center)
        x3D = np.array(footprint["Xaxis_LCS"].append(0))
        z3D = np.array([0, 0, 1])
        y3D = np.cross(z3D, x3D)
        footprint["Yaxis_LCS"] = y3D[:2].tolist()

        h1 = np.linalg.norm(vertices[longest_1st + 1] - vertices[longest_1st])
        if longest_1st+2 > len(vertices)-1:   # last vertex repeated with first one
            v1 = np.linalg.norm(vertices[1] - vertices[longest_1st+1])
            footprint["FlangeThickness"] = np.linalg.norm(vertices[2] - vertices[1])
            footprint["OverallDepth"] = h1 + 2 * footprint["FlangeThickness"]
            footprint["OverallWidth"] = np.linalg.norm(vertices[3] - vertices[2])
            footprint["WebThickness"] = footprint["OverallWidth"] - 2 * v1
        else:
            v1 = np.linalg.norm(vertices[longest_1st + 2] - vertices[longest_1st + 1])
            if longest_1st + 3 > len(vertices) - 1:
                footprint["FlangeThickness"] = np.linalg.norm(vertices[1] - vertices[longest_1st + 2])
                footprint["OverallDepth"] = h1 + 2 * footprint["FlangeThickness"]
                footprint["OverallWidth"] = np.linalg.norm(vertices[2] - vertices[1])
                footprint["WebThickness"] = footprint["OverallWidth"] - 2 * v1
            else:
                footprint["FlangeThickness"] = np.linalg.norm(vertices[longest_1st + 3] - vertices[longest_1st + 2])
                footprint["OverallDepth"] = h1 + 2 * footprint["FlangeThickness"]
                if longest_1st + 4 > len(vertices) - 1:
                    footprint["OverallWidth"] = np.linalg.norm(vertices[1] - vertices[longest_1st + 3])
                    footprint["WebThickness"] = footprint["OverallWidth"] - 2 * v1
                else:
                    footprint["OverallDepth"] = np.linalg.norm(vertices[longest_1st + 4] - vertices[longest_1st + 3])
                    footprint["WebThickness"] = footprint["OverallWidth"] - 2 * v1
    return footprint


# points: unordered point set
def generateFootprint(points2D_SPCS, TM_SPCS2OCS, footprintType):

    """

    :param points2D_SPCS:
    :param TM_LCS2GCS:
    :param footprintType:
    :return: footprint: {'type': , 'geometry_SPCS': , "TM_SPCS2OCS":}

    -> IfcRectangleProfileDef: 'geometry_SPCS': [point2D] (no duplicates)
    -> IfcCircleProfileDef: geometry_SPCS': [xc,yc, r]
    -> IfcIShapeProfileDef: geometry_SPCS': {"origin_LCS":, "Yaxis_LCS": , "Xaxis_LCS": , "OverallWidth": ,
                                             "FlangeThickness":, "WebThickness": , "OverallDepth": }
    - createIfcArbitraryClosedProfileDef: geometry_SPCS':
         [point2D] (no duplicates) for "polygon"
         [["ls", [point]], ["arc", [xc, yc, r, min_ang, max_ang, dir]], ...] for "ls_arc"

    """

    footprint = {}
    footprint['type'] = footprintType
    footprint["TM_SPCS2OCS"] = TM_SPCS2OCS

    if footprintType == "IfcRectangleProfileDef":
        # # Method 1: opencv
        # # rect: (center, width, height, angle): angle refers to the rotation angle of the rect WRT x-axis
        # # !!! may have big error due to noises and outliers
        # rect = cv2.minAreaRect(points2D_SPCS)
        # corners = cv2.boxpoints(rect)  # clockwise
        # corners = np.flipud(corners)   # counter-clockwise

        # Method 2: real fitting based on optimization
        corners = curveFitting_rect(points2D_SPCS)
        footprint['geometry_SPCS'] = corners

    elif footprintType == "IfcCircleProfileDef":
        # # Method 1: opencv- mini circle contains all the points
        # # may have big error due to noises and outliers
        # (xc, yc), r = cv2.minEnclosingCircle(points2D_SPCS)

        # Method 2: real-fitting: circle-fit (optimization method: fit circle to the points)
        xc, yc, r, sigma = taubinSVD(points2D_SPCS)
        # plot_data_circle(points_2D_LCS, xc, yc, r)

        footprint['geometry_SPCS'] = [xc, yc, r]

    elif footprintType == "IfcIShapeProfileDef":
        footprint['geometry_SPCS'] = curveFitting_IShape(points2D_SPCS)

    elif footprintType == "IfcArbitraryClosedProfileDef_polygon":
        # # Method 1: alphashape - creat minimal polygon bounding points (use original points as the vertices)
        # # Alpha shapes are often used to generalize bounding polygons (Concave/Convex Hull) containing sets of points.
        # # may have big error due to noises and outliers
        # # As alpha value increase, the bounding shape fit the sample data with more tightly
        # polygon = alphashape.alphashape(points2D_SPCS, 2.0)

        # Method 2: real fitting based on optimization
        footprint['geometry_SPCS'] = curveFitting_polygon(points2D_SPCS, 2, 15)

    elif footprintType == "IfcArbitraryClosedProfileDef_ls_arc":
        footprint['geometry_SPCS'] = curveFitting_ls_arc(points2D_SPCS)
    else:
        print("Cannot handle this footprint type now!")

    return footprint