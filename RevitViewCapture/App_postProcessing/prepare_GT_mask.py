import numpy as np
import os
import cv2 as cv
import random
import json
import matplotlib.path as mplPath


targetIamge = "IMG_1608.JPG"
annotation_path = "./data/image_annotation/test_30_in.json"

image_path = os.path.join("./data/image_annotation", targetIamge)
output_path = os.path.join("./data/image_annotation", "label_" + targetIamge)

def generateMaskedRenderingImage(renderImage, objInst_mask, outputPath):
    background = cv.imread(renderImage)  # !!! imread() cannot accept path with chinese characters
    random.seed(0)
    for inst in objInst_mask.keys():
        mask = np.zeros([background.shape[0], background.shape[1], 4])
        r = random.randint(0, 255)
        g = random.randint(0, 255)
        b = random.randint(0, 255)
        for pixel in objInst_mask[inst]:
            mask[pixel[1], pixel[0]] = [r, g, b, 0.6] # alpha vaule (0,1)

        alpha_channel = mask[:, :, 3]
        mask_color = mask[:, :, 0:3]
        alpha_mask = np.dstack((alpha_channel, alpha_channel, alpha_channel))

        # combine the background with the overlay image weighted by alpha
        composite = background * (1 - alpha_mask) + mask_color * alpha_mask

        # overwrite the section of the background image that has been updated
        background = composite
    cv.imwrite(outputPath, background)


def get_pixels_in_polygon(polygon, image_shape):

    # Create a mesh grid of pixel coordinates
    y, x = np.meshgrid(np.arange(image_shape[0]), np.arange(image_shape[1]), indexing='ij')
    x, y = x.flatten(), y.flatten()
    points = np.vstack((x,y)).T

    # Create a path from the polygon vertices
    poly_path = mplPath.Path(polygon)

    # Use the path to check if each point is inside the polygon
    grid = poly_path.contains_points(points)
    inside_points = points[grid]

    # Return the points inside the polygon
    return inside_points


# get objInst_mask: {objInst:[[i, j]]}
image = cv.imread(image_path)
objInst_lable = {}
with open(annotation_path, "r") as f:
    label = json.load(f)

for imageName in label:
    if targetIamge == label[imageName]["filename"]:
        regions = label[imageName]["regions"]
        for i in range(len(regions)):
            objInst = regions[i]["region_attributes"]["ClassName"] + "_" + str(i)
            x = regions[i]["shape_attributes"]["all_points_x"]
            y = regions[i]["shape_attributes"]["all_points_y"]

            polygon = []
            for j in range(len(x)):
                polygon.append((x[j], y[j]))

            pixels_in_polygon = get_pixels_in_polygon(polygon, image.shape)
            objInst_lable[objInst] = pixels_in_polygon
        break

generateMaskedRenderingImage(image_path, objInst_lable, output_path)