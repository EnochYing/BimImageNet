import cv2 as cv
import numpy as np
from scipy import spatial
from utils import *
import os
import time


input_path = "C:/Users/enochying/Desktop/segResults/toProcess"
viewlist = "C:/Users/enochying/Desktop/segResults/toProcess/viewlist.txt"
simility_threshold = 0.95


def imageSimilarity(a, b):
    ima = cv.imread(a)
    ima_reshape = cv.resize(ima, (round(ima.shape[0]), round(ima.shape[1])), interpolation=cv.INTER_LINEAR)
    ima_array = np.array(ima_reshape).flatten()/255

    imb = cv.imread(b)
    imb_reshape = cv.resize(imb, (round(imb.shape[0]), round(imb.shape[1])), interpolation=cv.INTER_LINEAR)
    imb_array = np.array(imb_reshape).flatten()/255

    similarity = 1 - spatial.distance.cosine(ima_array, imb_array)
    return similarity

a = r"C:\Users\enochying\Desktop\valid.png"
b = r"C:\Users\enochying\Desktop\invalid.png"

# print(imageSimilarity(a, b))
#
# img = cv.imread(r"C:\Users\enochying\Desktop\segResults\toProcess\unknwonSpace_unknownID_1123791\oriRevitImage.png")
# img_gray = cv.cvtColor(img, cv.COLOR_BGR2GRAY)
# img_blur = cv.GaussianBlur(img_gray, (3, 3), 0)  # Blur the image for better edge detection
# edges = cv.Canny(image=img_blur, threshold1=100, threshold2=200)  # Canny Edge Detection
# contours, hierarchy = cv.findContours(edges, cv.RETR_EXTERNAL,cv.CHAIN_APPROX_NONE)
# print(len(contours))



# start_time = time.time()
# view_image = []
# for root, _, files in os.walk(input_path):
#     for file in files:
#         if ".png" in file:
#             filePath = os.path.join(root, file)
#             viewName = os.path.basename(filePath)
#             view_image.append([viewName, filePath])
#
# similar = []
# for index, e in enumerate(view_image):
#     flatten = [j for sub in similar for j in sub]
#     if e[0] not in flatten:
#         group = [e[0]]
#         flatten.append(e[0])
#         remaining = view_image[index + 1:]
#         for re in remaining:
#             if re[0] not in flatten:
#                 if imageSimilarity(e[1], re[1]) > simility_threshold:
#                     group.append(re[0])
#         similar.append(group)
#
# for list in similar:
#     print(list)
#
# # edge detection
plain = []
similar = []
view_image = ['C:\\Users\\enochying\\Desktop\\test\\unknwonSpace_unknownID\\1120039\\Rendering_Enscape_ori.png']
for e in view_image:
    # if e[0] not in similar:
    img = cv.imread(e)
    img_gray = cv.cvtColor(img, cv.COLOR_BGR2GRAY)
        # Blur the image for better edge detection
    img_blur = cv.GaussianBlur(img_gray, (3, 3), 0)
    edges = cv.Canny(image=img_blur, threshold1=50, threshold2=100)  # Canny Edge Detection
    contours, hierarchy = cv.findContours(edges, cv.RETR_TREE, cv.CHAIN_APPROX_SIMPLE)  # cv.CHAIN_APPROX_SIMPLE
    showContours('contours', [img.shape[0], img.shape[1], 3], contours)


#
#
# # for list in similar:
# #     print(list)
#
# invalid = similar + plain
# with open(viewlist, 'w') as f:
#     for e in view_image:
#         if e[0] not in invalid:
#             f.write(e[0] + '\n')
#         else:
#             new_viewName = "Invalid_" + e[0]
#             new_folderName = os.path.join(os.path.dirname(e[1]), new_viewName)
#             os.rename(e[1], new_folderName)
#
# end_time = time.time()
# print(end_time - start_time)
