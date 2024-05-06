import os
from utils import *
import numpy as np
import json
import shutil
import cv2
import sys


# !!! we assume each instance's mask boundary is a simple polygon
# !!! the BimImageNet program do generate objInst_maskBoundary with multiple polygons
# !!! COCO data format also support an instance with multiple polygons, but VIA does not
def convertMaskBoundaries(objInst_maskBoundary):
    regions = []

    for key in objInst_maskBoundary.keys():
        region = {}

        poly = objInst_maskBoundary[key][0]
        xs = poly[:, 0].tolist()
        ys = poly[:, 1].tolist()
        region["shape_attributes"] = {"name": "polygon", "all_points_x": xs, "all_points_y": ys}

        if "Wall" in key or "wall" in key:
            cls = "Wall"
        elif "Door" in key or "door" in key:
            cls = "Door"
        elif "Floor" in key or "floor" in key:
            cls = "Floor"
        elif "Ceiling" in key or "ceiling" in key:
            cls = "Ceiling"
        else:  # in case there are some other object types found, the entire program can still run
            cls = "Background"
        region["region_attributes"] = {"ClassName": cls}
        regions.append(region)
    return regions


def prepareAnnotations(sourceFolder):
    objTypes = ['Wall', 'Door', 'Floor', "Ceiling"]

    targetFolder = sourceFolder + "_dataset"
    if not os.path.exists(targetFolder):
        os.makedirs(targetFolder)

    labels = {}
    for root, _, files in os.walk(sourceFolder):
        for file in files:
            if 'objInstance.json' in file:
                label = {}
                label['filename'] = os.path.basename(root) + '.png'
                image_path = os.path.join(root, 'Rendering_Enscape.png')
                (w, h, _) = cv2.imread(image_path).shape
                label['size'] = w * h

                # copy, rename and move render image to target folder
                imageName_new = os.path.basename(root) + '.png'
                shutil.copy(image_path, os.path.join(targetFolder, imageName_new))

                # generate segmentation results
                with open(os.path.join(root, file), 'r', encoding='utf-8') as f:
                    data = json.load(f)
                annotations = np.array(list(data.values())[0]['annotations'])

                # # generate instance segmentation image: using fixed random color to color object instances
                # fileName_instSeg = 'instSeg.png'
                # file_instSeg = os.path.join(root, fileName_instSeg)
                # generateInstSegImage(file_instSeg, annotations)

                # calculate object mask boundaries
                objInst_mask, objInst_maskBoundary = calculateObjMasks_inst(annotations, objTypes, 2)

                # arrange annotation in vig format
                label['regions'] = convertMaskBoundaries(objInst_maskBoundary)
                label['file_attributes'] = {}
                labels[imageName_new] = label
                break

    # write out json file complying with the vig format
    with open(os.path.join(targetFolder, 'via_region_data.json'), 'w') as outfile:
        json.dump(labels, outfile)


if __name__ == "__main__":
    # if len(sys.argv) > 1:
    #     argvs = sys.argv[1].split(",")
    #     sourceFolder = argvs[0]
    #     prepareAnnotations(sourceFolder)
    prepareAnnotations(r"D:\Buildings\007_interior_design\building")
