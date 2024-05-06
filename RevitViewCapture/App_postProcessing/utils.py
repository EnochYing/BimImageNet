import numpy as np
import png
import os
import cv2 as cv
import random
import matplotlib.pyplot as plt
import colorsys
import open3d
import json
import pandas as pd
import alphashape


# # Get indexes of pixels inside the polygon and set them to 1
# rr, cc = skimage.draw.polygon(p['all_points_y'], p['all_points_x'])
# mask[rr, cc, i] = 1

# Pull masks of instances belonging to the same class.
# m = mask[:, :, np.where(class_ids == class_id)[0]]

def random_colors(N, bright=True):
    """
    Generate random colors.
    To get visually distinct colors, generate them in HSV space then
    convert to RGB.
    """
    brightness = 1.0 if bright else 0.7
    hsv = [(i / N, 1, brightness) for i in range(N)]
    colors = list(map(lambda c: colorsys.hsv_to_rgb(*c), hsv))
    random.shuffle(colors)
    return colors


def getObjectTypes(annotations, objectTypes):
    objects = []

    for row in annotations:
        for ele in row:
            pieces = ele.split('/')
            obj = [pi.split('_')[0] for pi in pieces if pi.split('_')[0] not in objects
                   and not containObjType(pi.split('_')[0], objectTypes)]
            objects.extend(obj)

    return objects


def containObjType(obj, objTypes):
    for objType in objTypes:
        pieces = objType.split('/')

        for pie in pieces:
            lots = pie.split('-')
            exist = True
            for lot in lots:
                if lot not in obj:
                    exist = False
                    break
            if exist:
                return True
    return False


def getObjType(obj, objTypes):
    for objType in objTypes:
        pieces = objType.split('/')

        for pie in pieces:
            lots = pie.split('-')
            exist = True
            for lot in lots:
                if lot not in obj:
                    exist = False
                    break
            if exist:
                return objType
    return "Others"


def getObjectInstances(annotations):
    objects = []

    for row in annotations:
        for ele in row:
            pieces = ele.split('/')
            obj = [pi for pi in pieces if pi not in objects]
            objects.extend(obj)

    return objects


#  represent depths using 16-bit unsigned integers: 0-65535
def generateDepthImage(depthdata):
    # depths = np.loadtxt(depthdata, dtype=np.float64)  ---> not stable
    depths = pd.read_csv(depthdata, sep=' ', header=None, dtype=np.float64).values

    # (32 m - 64000)
    # to make image visual
    scale_mm = 3  # 1.5
    depths = (depths * scale_mm).astype(np.uint16)  # 2^16: 65536
    folderName = os.path.dirname(depthdata)
    fileName = os.path.splitext(os.path.basename(depthdata))[0]
    fileName_new = fileName + '.png'

    with open(os.path.join(folderName, fileName_new), 'wb') as f:
        writer = png.Writer(width=depths.shape[1], height=depths.shape[0], bitdepth=16, greyscale=True)
        depths2list = depths.tolist()
        writer.write(f, depths2list)


def generateSemSegImage(targetFilePath, annotations, obj_color):
    # create empty white RGB images
    image = 255 * np.ones(shape=[annotations.shape[0], annotations.shape[1], 3], dtype=np.uint8)

    for i, row in enumerate(image):
        for j, col in enumerate(row):
            pieces = annotations[i, j].split('/')
            objs = [pi.split('_')[0] for pi in pieces]

            colors = []
            for obj in objs:
                color = getObjColor(obj, obj_color)
                colors.append(color)

            colors = np.ceil(np.mean(np.array(colors), axis=0))

            # Note: cv2 use BGR color model
            col[0] = colors[2]
            col[1] = colors[1]
            col[2] = colors[0]

    cv.imwrite(targetFilePath, image)


def generatePanoSegImage(file_panoSeg, annotations, objInst_mask, obj_color, interestObjInst_color):
    # create empty white RGB images
    image = 255 * np.ones(shape=[annotations.shape[0], annotations.shape[1], 3], dtype=np.uint8)
    for inst in objInst_mask.keys():
        if inst in interestObjInst_color.keys():
            color = interestObjInst_color[inst]
        else:
            color = getObjColor(inst, obj_color)
            color = [color[2], color[1], color[0]]
        for pixel in objInst_mask[inst]:
            if pixel[0] < image.shape[0] and pixel[1] < image.shape[1]:
                image[pixel[0], pixel[1]] = color
    cv.imwrite(file_panoSeg, image)


def getObjColor(obj, obj_color):
    for objType in obj_color.keys():
        pieces = objType.split('/')
        for piece in pieces:
            lots = piece.split('-')
            tar = True
            for lot in lots:
                if lot not in obj:
                    tar = False
                    break
            if tar:
                return obj_color[objType]
    return obj_color['Others']


def generateMaterialSegImage(targetFilePath, annotations, mal_color):
    # create empty white RGB images
    image = 255 * np.ones(shape=[annotations.shape[0], annotations.shape[1], 3], dtype=np.uint8)

    for i, row in enumerate(image):
        for j, col in enumerate(row):
            pieces = annotations[i, j].split('/')

            colors = []
            for piece in pieces:
                colors.append(getMalColor(piece, mal_color))
            colors = np.ceil(np.mean(np.array(colors), axis=0))

            # Note: cv2 use BGR color model
            col[0] = colors[2]
            col[1] = colors[1]
            col[2] = colors[0]
    cv.imwrite(targetFilePath, image)


def generateMaterialSegImage(targetFilePath, annotations, material_color):
    # create empty white RGB images
    image = 255 * np.ones(shape=[annotations.shape[0], annotations.shape[1], 3], dtype=np.uint8)

    random.seed(10)
    mal_color = {}
    for row in annotations:
        for item in row:
            if item not in mal_color.keys():
                if item == "Background":
                    mal_color[item] = [255, 255, 255]
                else:
                    r = random.randint(0, 255)
                    g = random.randint(0, 255)
                    b = random.randint(0, 255)
                    mal_color[item] = [r, g, b]

    for i, row in enumerate(image):
        for j, col in enumerate(row):
            colors = mal_color[annotations[i, j]]

            # Note: cv2 use BGR color model
            col[0] = colors[2]
            col[1] = colors[1]
            col[2] = colors[0]
    cv.imwrite(targetFilePath, image)


def getMalColor(mal, mal_color):
    mal = mal.upper()
    for key in mal_color:
        pieces = key.split('/')
        for pie in pieces:
            if pie.upper() in mal:
                return mal_color[key]
    return mal_color['Others']


def generateInstSegImage(targetFilePath, annotations_part):
    random.seed(10)

    annotations_obj = np.copy(annotations_part)
    for i, row in enumerate(annotations_obj):
        for j, col in enumerate(row):
            annotations_obj[i, j] = '_'.join(col.split('_')[0:2])

    objInstances = getObjectInstances(annotations_obj)
    objInstance_color = {}
    for inst in objInstances:
        r = random.randint(0, 255)
        g = random.randint(0, 255)
        b = random.randint(0, 255)
        objInstance_color[inst] = [r, g, b]

    # create empty white RGB images
    image = 255 * np.ones(shape=[annotations_obj.shape[0], annotations_obj.shape[1], 3], dtype=np.uint8)
    for i, row in enumerate(image):
        for j, col in enumerate(row):
            # if 'Columns' in annotations_obj[i, j]:
            pieces = annotations_obj[i, j].split('/')
            objInsts = [pi for pi in pieces]
            colors = []
            for obj in objInsts:
                colors.append(objInstance_color[obj])
            colors = np.ceil(np.mean(np.array(colors), axis=0))

            # Note: cv2 use BGR color model
            col[0] = colors[2]
            col[1] = colors[1]
            col[2] = colors[0]

    cv.imwrite(targetFilePath, image)


def generatePartInstSegImage(targetFilePath, annotations_part):
    random.seed(10)
    objPartInstances = getObjectInstances(annotations_part)
    objPartInstance_color = {}
    for inst in objPartInstances:
        r = random.randint(0, 255)
        g = random.randint(0, 255)
        b = random.randint(0, 255)
        objPartInstance_color[inst] = [r, g, b]

    # create empty white RGB images
    image = 255 * np.ones(shape=[annotations_part.shape[0], annotations_part.shape[1], 3], dtype=np.uint8)
    for i, row in enumerate(image):
        for j, col in enumerate(row):
            pieces = annotations_part[i, j].split('/')
            partInsts = [pi for pi in pieces]
            colors = []
            for part in partInsts:
                colors.append(objPartInstance_color[part])
            colors = np.ceil(np.mean(np.array(colors), axis=0))

            # Note: cv2 use BGR color model
            col[0] = colors[2]
            col[1] = colors[1]
            col[2] = colors[0]

    cv.imwrite(targetFilePath, image)


def showContours(imaName, imaShape, boundaries):
    image = np.zeros(shape=imaShape, dtype=np.uint8)
    cv.drawContours(image, boundaries, -1, (255, 255, 255), 4)
    cv.imshow(imaName, image)
    cv.waitKey(0)


def showPolylines(imaName, imaShape, boundaries):
    image = np.zeros(shape=imaShape, dtype=np.uint8)
    cv.polylines(image, boundaries, True, (0, 0, 255), 2)

    for poly in boundaries:
        poly = poly.reshape((-1, 2))
        for v in poly:
            cv.circle(image, tuple(v), radius=4, color=(0, 255, 0), thickness=-1)

    cv.imshow(imaName, image)
    cv.waitKey(0)


# convert shapely POLYGON format into numpy array
def POLYGON2NumPy(Polygon):
    xx, yy = Polygon.exterior.coords.xy
    xx = np.array(xx.tolist())
    yy = np.array(yy.tolist())

    zz = np.vstack((xx, yy))
    return zz.T


def calculateObjMasks_inst(annotations, targetObjs, threshold_area):
    objInst_mask = {}

    for i, row in enumerate(annotations):
        for j, col in enumerate(row):
            if col:
                pieces = col.split('/')
                # piece = pieces[0]
                # if len(pieces)>1:
                #     if random.random()>0.8:
                #         piece = pieces[1]
                #
                # objInst = '_'.join(piece.split('_')[0:2])
                # if objInst in objInst_mask.keys():
                #     objInst_mask[objInst].append([i, j])
                # else:
                #     objInst_mask[objInst] = [[i, j]]

                for piece in pieces:
                    objInst = '_'.join(piece.split('_')[0:2])
                    objcls = piece.split('_')[0][:-1]
                    if objcls in targetObjs:
                        if objInst in objInst_mask.keys():
                            objInst_mask[objInst].append([i, j])
                        else:
                            objInst_mask[objInst] = [[i, j]]

    objInst_maskBoundary = {}
    for key in objInst_mask.keys():
        # create binary image
        image = np.zeros(shape=[annotations.shape[0], annotations.shape[1]], dtype=np.uint8)
        for pixel in objInst_mask[key]:
            image[pixel[0], pixel[1]] = 255
        _, binary = cv.threshold(image, 110, 255, cv.THRESH_BINARY)
        # cv.imshow(key, binary)
        # cv.waitKey(0)

        # contours refer to a list of independent simple boundaries
        contours, hierarchy = cv.findContours(binary, cv.RETR_TREE, cv.CHAIN_APPROX_SIMPLE)  # cv.CHAIN_APPROX_SIMPLE
        # showContours('contours', [annotations.shape[0], annotations.shape[1], 3], contours)
        # showPolylines('contours', [annotations.shape[0], annotations.shape[1], 3], contours)
        polys = []
        approxes = []  # for visualization
        for cnt in contours:
            area = cv.contourArea(cnt)
            if area > threshold_area:
                approx = cv.approxPolyDP(cnt, 0.001 * cv.arcLength(cnt, True), True)
                approxes.append(approx)
                poly = approx.reshape((-1, 2))
                polys.append(poly)

        # showPolylines('poly', [annotations.shape[0], annotations.shape[1], 3], approxes)
        if polys:
            objInst_maskBoundary[key] = polys
        # elif len(polys) > 1:
        #     points = []
        #     for poly in polys:
        #         for row in poly:
        #             tu = (row[0], row[1])
        #             points.append(tu)
        #     polygon = alphashape.alphashape(points, 2)
        #     polygon = POLYGON2NumPy(polygon)  # convert into numpy array
        #     polygon = np.flipud(polygon[:-1, :])  # remove repeat vertex and reverse the polygon direction
        #     objInst_maskBoundary[key] = [polygon]
        #     print(polys)
        #     print(polygon)

    return objInst_mask, objInst_maskBoundary


def calculateObjMasks(annotations, threshold_area=0):
    objInst_mask = {}

    for i, row in enumerate(annotations):
        for j, col in enumerate(row):
            pieces = col.split('/')
            # piece = pieces[0]
            # if len(pieces)>1:
            #     if random.random()>0.8:
            #         piece = pieces[1]
            #
            # objInst = '_'.join(piece.split('_')[0:2])
            # if objInst in objInst_mask.keys():
            #     objInst_mask[objInst].append([i, j])
            # else:
            #     objInst_mask[objInst] = [[i, j]]

            for piece in pieces:
                objInst = '_'.join(piece.split('_')[0:2])
                if objInst in objInst_mask.keys():
                    objInst_mask[objInst].append([i, j])
                else:
                    objInst_mask[objInst] = [[i, j]]

    return objInst_mask


def calculateObjPartMasks(annotations):
    objPartInst_mask = {}
    for i, row in enumerate(annotations):
        for j, col in enumerate(row):
            pieces = col.split('/')
            for piece in pieces:
                if piece in objPartInst_mask.keys():
                    objPartInst_mask[piece].append([i, j])
                else:
                    objPartInst_mask[piece] = [[i, j]]

    objPartInst_maskBoundary = {}
    for key in objPartInst_mask.keys():
        # creat binary image
        image = np.zeros(shape=[annotations.shape[0], annotations.shape[1]], dtype=np.uint8)
        for pixel in objPartInst_mask[key]:
            image[pixel[0], pixel[1]] = 255
        _, binary = cv.threshold(image, 110, 255, cv.THRESH_BINARY)
        # cv.imshow(key, binary)
        # cv.waitKey(0)

        # contours refer to a list of independent simple boundaries
        contours, hierarchy = cv.findContours(binary, cv.RETR_TREE, cv.CHAIN_APPROX_SIMPLE)  # cv.CHAIN_APPROX_SIMPLE
        # showContours('contours', [annotations.shape[0], annotations.shape[1], 3], contours)
        polys = []
        for cnt in contours:
            # area = cv.contourArea(cnt)
            approx = cv.approxPolyDP(cnt, 0.001 * cv.arcLength(cnt, True), True)
            # showContours('poly', [annotations.shape[0], annotations.shape[1], 3], [approx])
            poly = approx.reshape((-1, 2))
            polys.append(poly)

        objPartInst_maskBoundary[key] = polys

    return objPartInst_mask, objPartInst_maskBoundary


def calculateObjPartMasks(annotations):
    objPartInst_mask = {}
    for i, row in enumerate(annotations):
        for j, col in enumerate(row):
            pieces = col.split('/')
            for piece in pieces:
                if piece in objPartInst_mask.keys():
                    objPartInst_mask[piece].append([i, j])
                else:
                    objPartInst_mask[piece] = [[i, j]]

    return objPartInst_mask


def generate2DBB_objPart(objInst_maskBoundary, outputPath, objTypes):
    BBs = {}
    for key in objInst_maskBoundary:
        allowed = False
        for obj in objTypes:
            if obj in key:
                allowed = True
                break
        if allowed:
            BB = {}
            xmaxs = []
            xmins = []
            ymaxs = []
            ymins = []
            for poly in objInst_maskBoundary[key]:  # poly: npoints x 2
                poly = np.asarray(poly)
                xmaxs.append(max(poly[:, 0].tolist()))
                xmins.append(min(poly[:, 0].tolist()))
                ymaxs.append(max(poly[:, 1].tolist()))
                ymins.append(min(poly[:, 1].tolist()))
            BB['xmax'] = xmaxs
            BB['xmin'] = xmins
            BB['ymax'] = ymaxs
            BB['ymin'] = ymins

            BBs[key] = BB

    with open(outputPath, 'w') as json_file:
        json.dump(BBs, json_file)


def generate2DBB_objMB(objInst_maskBoundary, outputPath, objTypes):
    BBs = {}
    for key in objInst_maskBoundary:
        if containObjType(key, objTypes):
            BB = {}
            xmaxs = []
            xmins = []
            ymaxs = []
            ymins = []
            for poly in objInst_maskBoundary[key]:  # poly: npoints x 2
                poly = np.asarray(poly)
                xmaxs.append(max(poly[:, 0].tolist()))
                xmins.append(min(poly[:, 0].tolist()))
                ymaxs.append(max(poly[:, 1].tolist()))
                ymins.append(min(poly[:, 1].tolist()))

            BB['xmax'] = [max(xmaxs)]
            BB['xmin'] = [min(xmins)]
            BB['ymax'] = [max(ymaxs)]
            BB['ymin'] = [min(ymins)]

            BBs[key] = BB

    with open(outputPath, 'w') as json_file:
        json.dump(BBs, json_file)


def generate2DBB_objM(objInst_mask, outputPath, objTypes):
    BBs = {}
    for key in objInst_mask:
        if containObjType(key, objTypes):
            BB = {}

            mask = np.asarray(objInst_mask[key])
            ymax, xmax = np.max(mask, axis=0)
            ymin, xmin = np.min(mask, axis=0)

            BB['xmax'] = [int(xmax)]
            BB['xmin'] = [int(xmin)]
            BB['ymax'] = [int(ymax)]
            BB['ymin'] = [int(ymin)]

            BBs[key] = BB

    with open(outputPath, 'w') as json_file:
        json.dump(BBs, json_file)


def generate2DBB_objM(objInst_mask, outputPath):
    BBs = {}
    for key in objInst_mask:
        BB = {}

        mask = np.asarray(objInst_mask[key])
        ymax, xmax = np.max(mask, axis=0)
        ymin, xmin = np.min(mask, axis=0)

        BB['xmax'] = [int(xmax)]
        BB['xmin'] = [int(xmin)]
        BB['ymax'] = [int(ymax)]
        BB['ymin'] = [int(ymin)]

        BBs[key] = BB
    with open(outputPath, 'w') as json_file:
        json.dump(BBs, json_file)


# images[0] refer to the rendering image
def generateMaskedRenderingImage(renderImage, objInst_mask, outputPath, objTypes):
    background = cv.imread(renderImage)  # !!! imread() cannot accept path with chinese characters
    random.seed(3)
    interestObjInst_color = {}
    for inst in objInst_mask.keys():
        mask = np.zeros([background.shape[0], background.shape[1], 4])
        r = random.randint(0, 255)
        g = random.randint(0, 255)
        b = random.randint(0, 255)
        interest = containObjType(inst, objTypes)
        if interest:
            interestObjInst_color[inst] = [r, g, b]
        for pixel in objInst_mask[inst]:
            if pixel[0] < background.shape[0] and pixel[1] < background.shape[1]:
                if interest:
                    mask[pixel[0], pixel[1]] = [r, g, b, 1]
                else:
                    mask[pixel[0], pixel[1]] = [r, g, b, 0]
        alpha_channel = mask[:, :, 3]  # alpha vaule (0,1)
        mask_color = mask[:, :, 0:3]

        alpha_mask = np.dstack((alpha_channel, alpha_channel, alpha_channel))

        # combine the background with the overlay image weighted by alpha
        composite = background * (1 - alpha_mask) + mask_color * alpha_mask

        # overwrite the section of the background image that has been updated
        background = composite
    cv.imwrite(outputPath, background)
    return interestObjInst_color


# get 3D oriented Bounding boxes of object point cloud clusters
def get3DOBB_PCs_view(objPCs):
    geoItems_view = []

    # blue lines for plotting bounding box (RGB: 0-1)
    lineColor = np.zeros((12, 3), dtype=np.float)
    lineColor[:, 2] = 1

    for pc in objPCs:
        objInstance = pc[0, 0]

        pc_np = pc[:, 1:4].astype(np.float)

        pc_open3d = open3d.geometry.PointCloud()
        pc_open3d.points = open3d.utility.Vector3dVector(pc_np)
        pc_open3d.colors = open3d.utility.Vector3dVector(pc[:, 4:7].astype(np.float) / 255)  # npoints x 3 (RGB)
        geoItems_view.append(pc_open3d)

        try:
            # OBB.get_box_points()
            OBB = open3d.geometry.OrientedBoundingBox.create_from_points(pc_open3d.points)
            line_set = open3d.geometry.LineSet.create_from_oriented_bounding_box(OBB)
            line_set.colors = open3d.utility.Vector3dVector(lineColor)
            geoItems_view.append(line_set)
        except:
            continue

    open3d.visualization.draw_geometries(geoItems_view)


# get 3D oriented Bounding boxes in point clouds
def get3DOBB_PCs_store(objPCs, objTypes, outputPath):
    OBB_PC_json = {}
    OBB_corners = {}
    for pc in objPCs:
        objInstance = pc[0, 0]
        if containObjType(objInstance.split('_')[0], objTypes):
            OBB_PC = {}
            pc_np = pc[:, 1:4].astype(np.float)
            points_open3d = open3d.utility.Vector3dVector(pc_np)
            try:
                OBB = open3d.geometry.OrientedBoundingBox.create_from_points(points_open3d)
                OBB_PC['center'] = OBB.center.tolist()
                OBB_PC['R'] = OBB.R.tolist()
                OBB_PC['extent'] = OBB.extent.tolist()
                OBB_PC_json[objInstance] = OBB_PC
                # !!! npoints x 3 --? transpose for numpy
                # !!! order: top face (0, 2, 5, 3) -> buttom face (1, 7, 4, 6)
                OBB_corners[objInstance] = np.asarray(OBB.get_box_points())
            except:
                continue
    with open(outputPath, "w") as write_file:
            json.dump(OBB_PC_json, write_file)

    return OBB_corners

def get3DOBB_PCs_store(objPCs, outputPath):
    OBB_PC_json = {}
    OBB_corners = {}
    for pc in objPCs:
        objInstance = pc[0, 0]
        OBB_PC = {}
        pc_np = pc[:, 1:4].astype(np.float)
        points_open3d = open3d.utility.Vector3dVector(pc_np)
        try:
            OBB = open3d.geometry.OrientedBoundingBox.create_from_points(points_open3d)
            OBB_PC['center'] = OBB.center.tolist()
            OBB_PC['R'] = OBB.R.tolist()
            OBB_PC['extent'] = OBB.extent.tolist()
            OBB_PC_json[objInstance] = OBB_PC
            # !!! npoints x 3 --? transpose for numpy
            # !!! order: top face (0, 2, 5, 3) -> buttom face (1, 7, 4, 6)
            OBB_corners[objInstance] = np.asarray(OBB.get_box_points())
        except:
            continue
    with open(outputPath, "w") as write_file:
            json.dump(OBB_PC_json, write_file)

    return OBB_corners

def affineTransformation_vector3D(vector, TM):
    # TM: 4x4
    origin = np.zeros((3, 1), dtype=np.float64)
    origin_transf = affineTransformation_point3D(origin, TM)

    vector_transf = affineTransformation_point3D(vector.reshape((3, 1)), TM)
    vector_transf = np.subtract(vector_transf, origin_transf)
    vector_transf = vector_transf / np.linalg.norm(vector_transf)

    return vector_transf


def affineTransformation_points3D(points, TM):
    # TM: 4x4
    col = points.shape[1]
    row = np.ones((1, col), dtype=np.float64)
    points = np.vstack((points, row))
    points = np.dot(TM, points)
    points = points[:3, :]

    return points


def affineTransformation_point3D(point, TM):
    # TM: 4x4
    point = np.vstack((point, [1]))
    point = np.dot(TM, point)
    point3d = point[:3]

    return point3d


def affineTransformation_point2D(point, TM):
    # TM: 3x3
    point = np.vstack((point, [1]))
    point = np.dot(TM, point)

    point2d = point[:2]
    return point2d


def affineTransformation_points2D(points, TM):
    # TM: 3x3
    col = points.shape[1]
    row = np.ones((1, col), dtype=np.float64)
    points = np.vstack((points, row))
    points = np.dot(TM, points)
    points = points[:2, :]

    return points


# get 3D oriented Bounding boxes in image
def get3DOBB_image_store(OBB_PC_corners, TM_json, outputPath):
    with open(TM_json, 'r') as f:
        data = json.load(f)

    TM_GCS2VCS = np.asarray(data['TM_GCS2VCS'])
    TM_VCS2PCS = np.asarray(data['TM_VCS2PCS'])
    f = float(data['focus'])

    OBB_image_corners = {}
    for key in OBB_PC_corners:
        corners = affineTransformation_points3D(np.transpose(np.asarray(OBB_PC_corners[key])), TM_GCS2VCS)
        lastRow = corners[-1, :]
        corners2d = corners / lastRow
        corners2d = corners2d * f

        corners_pixel = affineTransformation_points2D(corners2d[0:2, :], TM_VCS2PCS).astype(int)
        OBB_image_corners[key] = corners_pixel.tolist()

    with open(outputPath, "w") as write_file:
        json.dump(OBB_image_corners, write_file)