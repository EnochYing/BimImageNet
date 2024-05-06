import cv2 as cv
import numpy as np
from scipy import spatial
import os
import sys


def imageSimilarity(a, b):
    ima = cv.imread(a)
    scale = 1
    if ima.shape[0] >= 500:
        scale = 0.5
    resize_w = round(ima.shape[0] * scale)
    resize_h = round(ima.shape[1] * scale)
    ima_reshape = cv.resize(ima, (resize_w, resize_h), interpolation=cv.INTER_LINEAR)
    ima_array = np.array(ima_reshape).flatten() / 255

    imb = cv.imread(b)
    imb_reshape = cv.resize(imb, (resize_w, resize_h), interpolation=cv.INTER_LINEAR)
    imb_array = np.array(imb_reshape).flatten() / 255

    similarity = 1 - spatial.distance.cosine(ima_array, imb_array)
    return similarity


def invalidImageLabeling(edge_threshold, similarity_threshold, input_path):
    # read full set of images
    view_image = []
    for root, _, files in os.walk(input_path):
        for file in files:
            if "Rendering_Enscape_ori.png" in file:
                filePath = os.path.join(root, file)
                path = os.path.dirname(filePath)
                viewName = os.path.basename(path)
                view_image.append([viewName, filePath])

    # check plain images without edges
    plain = []
    for e in view_image:
        img = cv.imread(e[1])
        img_gray = cv.cvtColor(img, cv.COLOR_BGR2GRAY)
        img_blur = cv.GaussianBlur(img_gray, (3, 3), 0)  # Blur the image for better edge detection
        edges = cv.Canny(image=img_blur, threshold1=50, threshold2=100)  # Canny Edge Detection
        contours, _ = cv.findContours(edges, cv.RETR_EXTERNAL, cv.CHAIN_APPROX_NONE)
        if len(contours) < edge_threshold:
            plain.append(e[0])

    # check similar images
    similar = []
    for index, e in enumerate(view_image):
        if e[0] not in plain:
            remaining = view_image[index + 1:]
            for re in remaining:
                if re[0] not in plain:
                    if imageSimilarity(e[1], re[1]) > similarity_threshold:
                        similar.append(e[0])
                        break

    # create finalized image list for further processing
    invalid = similar + plain
    viewlist = os.path.join(input_path, "validImageList.txt")
    with open(viewlist, 'w') as f:
        for e in view_image:
            if e[0] not in invalid:
                f.write(e[0] + '\n')
            else:
                newviewName = "Invalid_" + e[0]
                parentFolder = os.path.abspath(os.path.join(os.path.dirname(e[1]), os.pardir))
                newfolderName = os.path.join(parentFolder, newviewName)
                os.rename(os.path.dirname(e[1]), newfolderName)


if __name__ == "__main__":
    # !!!! edges = cv.Canny(image=img_blur, threshold1=50, threshold2=100): threshold values matter a lot
    # if len(sys.argv) > 1:
    #     inputs = sys.argv[1].split(",")
    #     invalidImageLabeling(float(inputs[0]), float(inputs[1]), inputs[2])
    invalidImageLabeling(2, 0.99, r'C:\Users\enochying\Desktop\005_Revit_office')
