import json
import numpy as np
import open3d
import os
import pandas as pd
import cv2
import random
from utils import *


class visualization:
    objTypes = ['Air-Terminal', "Beam", "Bottle", "Casework", "Cabinet", 'Cable-Tray', 'Cable-Tray-Fitting',
                "Ceiling", "Chair/Drehstuhl", "Cooktop", "Column", "Curtain-Wall-Mullion", "Curtain-Panel",
                "Dishwasher",
                "Door", 'Duct', 'Duct-Fitting', 'Fire-Alarm-Device', "Fire-Place-Hang", "Floor", "Footing", "Fridge",
                'Gutters',
                'Kammer', 'Keyboard', 'Landing', "Microwave", 'Zoll/Monitor', "Oven", 'Pipe', 'Pipe-Accessories',
                'Pipe-Insulation',
                "Plate", "Railing/Rail", 'Ramp', "Rangehood", "Roof", "Run", "Sofa", "Stair", 'Structural-Framing',
                "Table/Desk", "Vase",
                "Wall", "Walvit", "Washing-Machine", "Water-Glass/Cup", "Window", "Background", "Plumbing-Fixture",
                "Furniture",
                "Lighting-Fixture", "Specialty-Equipment", 'Lighting-Device', 'Electrical-Fixture',
                "Mechanical-Equipment", "Others"]

    def visualize_3DOBB_PC(self, folder):
        # obj_color = {}
        # objTypes = ['Air-Terminal', "Beam", "Bottle", "Casework", "Cabinet", 'Cable-Tray', 'Cable-Tray-Fitting',
        #             "Ceiling", "Chair", "Cooktop", "Column", "Curtain-Wall-Mullion", "Curtain-Panel", "Dishwasher",
        #             "Door", 'Duct', 'Duct-Fitting', 'Fire-Alarm-Device', "Fire-Place-Hang", "Floor", "Footing", "Fridge",
        #             'Gutters', 'Kammer', 'Landing', "Microwave", "Oven", 'Pipe', 'Pipe-Accessories', 'Pipe-Insulation',
        #             "Plate", "Railing/Rail", 'Ramp', "Rangehood", "Roof", "Run", "Sofa", "Stair", 'Structural-Framing',
        #             "Table", "Vase", "Wall", "Walvit", "Washing-Machine", "Water-Glass/Cup", "Window", "Background",
        #             "Plumbing-Fixture", "Furniture", "Lighting-Fixture", "Specialty-Equipment", 'Lighting-Device',
        #             'Electrical-Fixture', "Mechanical-Equipment", "Others"]
        # random.seed(1)
        # for obj in objTypes:
        #     r = random.random()
        #     g = random.random()
        #     b = random.random()
        #     obj_color[obj] = [r, g, b]

        geoItems_view = []

        # visualize pcs
        PCs = pd.read_csv(os.path.join(folder, 'objPCs.txt'), sep=' ', header=None, dtype=str).values
        objPCs = [PCs[PCs[:, 0] == k] for k in np.unique(PCs[:, 0])]

        for pc in objPCs:
            objInstance = pc[0, 0]
            pc_np = pc[:, 1:4].astype(float)

            pc_open3d = open3d.geometry.PointCloud()
            pc_open3d.points = open3d.utility.Vector3dVector(pc_np)
            pc_open3d.colors = open3d.utility.Vector3dVector(
                pc[:, 4:7].astype(float) / 255)  # npoints x 3 (RGB)
            geoItems_view.append(pc_open3d)

        # visualize OBBs
        with open(os.path.join(folder, '3DOBB_PC_open3d.json'), 'r') as json_file:
            data = json.load(json_file)
            for key_obj in data:
                center = np.array(data[key_obj]['center'])
                R = np.array(data[key_obj]['R'])
                extent = np.array(data[key_obj]['extent'])

                OBB = open3d.geometry.OrientedBoundingBox(center=center, R=R, extent=extent)
                line_set = open3d.geometry.LineSet.create_from_oriented_bounding_box(OBB)

                # blue lines for plotting bounding box (RGB: 0-1)
                lineColor = np.zeros((12, 3), dtype=float)
                # color = getObjColor(key_obj.split('_')[0], obj_color)
                # lineColor[:, 0] = color[0]
                # lineColor[:, 1] = color[1]
                # lineColor[:, 2] = color[2]

                lineColor[:, 0] = 0
                lineColor[:, 1] = 0
                lineColor[:, 2] = 1

                line_set.colors = open3d.utility.Vector3dVector(lineColor)
                geoItems_view.append(line_set)

        open3d.visualization.draw_geometries(geoItems_view)


    def visualize_3DOBB_part_PC(self, folder):
        # obj_color = {}
        # objTypes = ['Air-Terminal', "Beam", "Bottle", "Casework", "Cabinet", 'Cable-Tray', 'Cable-Tray-Fitting',
        #             "Ceiling", "Chair", "Cooktop", "Column", "Curtain-Wall-Mullion", "Curtain-Panel", "Dishwasher",
        #             "Door", 'Duct', 'Duct-Fitting', 'Fire-Alarm-Device', "Fire-Place-Hang", "Floor", "Footing", "Fridge",
        #             'Gutters', 'Kammer', 'Landing', "Microwave", "Oven", 'Pipe', 'Pipe-Accessories', 'Pipe-Insulation',
        #             "Plate", "Railing/Rail", 'Ramp', "Rangehood", "Roof", "Run", "Sofa", "Stair", 'Structural-Framing',
        #             "Table", "Vase", "Wall", "Walvit", "Washing-Machine", "Water-Glass/Cup", "Window", "Background",
        #             "Plumbing-Fixture", "Furniture", "Lighting-Fixture", "Specialty-Equipment", 'Lighting-Device',
        #             'Electrical-Fixture', "Mechanical-Equipment", "Others"]
        # random.seed(1)
        # for obj in objTypes:
        #     r = random.random()
        #     g = random.random()
        #     b = random.random()
        #     obj_color[obj] = [r, g, b]

        geoItems_view = []

        # visualize pcs
        PCs = pd.read_csv(os.path.join(folder, 'objPartPCs.txt'), sep=' ', header=None, dtype=str).values
        objPCs = [PCs[PCs[:, 0] == k] for k in np.unique(PCs[:, 0])]

        for pc in objPCs:
            objInstance = pc[0, 0]
            pc_np = pc[:, 1:4].astype(float)

            pc_open3d = open3d.geometry.PointCloud()
            pc_open3d.points = open3d.utility.Vector3dVector(pc_np)
            pc_open3d.colors = open3d.utility.Vector3dVector(
                pc[:, 4:7].astype(float) / 255)  # npoints x 3 (RGB)
            geoItems_view.append(pc_open3d)

        # visualize OBBs
        with open(os.path.join(folder, '3DOBB_part_PC_open3d.json'), 'r') as json_file:
            data = json.load(json_file)
            for key_obj in data:
                center = np.array(data[key_obj]['center'])
                R = np.array(data[key_obj]['R'])
                extent = np.array(data[key_obj]['extent'])

                OBB = open3d.geometry.OrientedBoundingBox(center=center, R=R, extent=extent)
                line_set = open3d.geometry.LineSet.create_from_oriented_bounding_box(OBB)

                # blue lines for plotting bounding box (RGB: 0-1)
                lineColor = np.zeros((12, 3), dtype=float)
                # color = getObjColor(key_obj.split('_')[0], obj_color)
                # lineColor[:, 0] = color[0]
                # lineColor[:, 1] = color[1]
                # lineColor[:, 2] = color[2]

                lineColor[:, 0] = 0
                lineColor[:, 1] = 0
                lineColor[:, 2] = 1

                line_set.colors = open3d.utility.Vector3dVector(lineColor)
                geoItems_view.append(line_set)

        open3d.visualization.draw_geometries(geoItems_view)

    def visualize_3DOBB_image(self, folder):
        image = cv2.imread(os.path.join(folder, 'Rendering_Enscape.png'))

        # obj_color = {}
        # objTypes = ['Air-Terminal', "Beam", "Bottle", "Casework", "Cabinet", 'Cable-Tray', 'Cable-Tray-Fitting',
        #             "Ceiling", "Chair", "Cooktop", "Column", "Curtain-Wall-Mullion", "Curtain-Panel", "Dishwasher",
        #             "Door", 'Duct', 'Duct-Fitting', 'Fire-Alarm-Device', "Fire-Place-Hang", "Floor", "Footing",
        #             "Fridge", 'Gutters', 'Kammer', 'Landing', "Microwave", "Oven", 'Pipe', 'Pipe-Accessories', 'Pipe-Insulation',
        #             "Plate", "Railing/Rail", 'Ramp', "Rangehood", "Roof", "Run", "Sofa", "Stair", 'Structural-Framing',
        #             "Table", "Vase", "Wall", "Walvit", "Washing-Machine", "Water-Glass/Cup", "Window", "Background",
        #             "Plumbing-Fixture", "Furniture", "Lighting-Fixture", "Specialty-Equipment", 'Lighting-Device',
        #             'Electrical-Fixture', "Mechanical-Equipment", "Others"]
        # random.seed(2)
        # for obj in objTypes:
        #     r = random.randint(0, 255)
        #     g = random.randint(0, 255)
        #     b = random.randint(0, 255)
        #     obj_color[obj] = [r, g, b]

        # Line Color in BGR Format
        color = (36, 255, 12)

        with open(os.path.join(folder, '3DOBB_image_cv2.json'), 'r') as json_file:
            OBBs = json.load(json_file)

        for key in OBBs:
            corners = OBBs[key]
            lines = [[0, 2], [2, 5], [5, 3], [3, 0],
                     [1, 7], [7, 4], [4, 6], [6, 1],
                     [0, 1], [2, 7], [5, 4], [3, 6]]
            for line in lines:
                start = (corners[0][line[0]], corners[1][line[0]])
                end = (corners[0][line[1]], corners[1][line[1]])
                # image = cv2.line(image, start, end, tuple(getObjColor(key.split('_')[0], obj_color)), thickness=1, lineType=cv2.LINE_AA)
                image = cv2.line(image, start, end, color, thickness=2, lineType=cv2.LINE_AA)

        cv2.imshow("3DOBB_image_cv2.png", image)
        cv2.waitKey(0)
        cv2.destroyAllWindows()

        cv2.imwrite(os.path.join(folder, '3DOBB_image_cv2.png'), image)

    def visualize_3DOBB_part_image(self, folder):
        image = cv2.imread(os.path.join(folder, 'Rendering_Enscape.png'))

        # obj_color = {}
        # objTypes = ['Air-Terminal', "Beam", "Bottle", "Casework", "Cabinet", 'Cable-Tray', 'Cable-Tray-Fitting',
        #             "Ceiling", "Chair", "Cooktop", "Column", "Curtain-Wall-Mullion", "Curtain-Panel", "Dishwasher",
        #             "Door", 'Duct', 'Duct-Fitting', 'Fire-Alarm-Device', "Fire-Place-Hang", "Floor", "Footing",
        #             "Fridge", 'Gutters', 'Kammer', 'Landing', "Microwave", "Oven", 'Pipe', 'Pipe-Accessories', 'Pipe-Insulation',
        #             "Plate", "Railing/Rail", 'Ramp', "Rangehood", "Roof", "Run", "Sofa", "Stair", 'Structural-Framing',
        #             "Table", "Vase", "Wall", "Walvit", "Washing-Machine", "Water-Glass/Cup", "Window", "Background",
        #             "Plumbing-Fixture", "Furniture", "Lighting-Fixture", "Specialty-Equipment", 'Lighting-Device',
        #             'Electrical-Fixture', "Mechanical-Equipment", "Others"]
        # random.seed(2)
        # for obj in objTypes:
        #     r = random.randint(0, 255)
        #     g = random.randint(0, 255)
        #     b = random.randint(0, 255)
        #     obj_color[obj] = [r, g, b]

        # Line Color in BGR Format
        color = (36, 255, 12)

        with open(os.path.join(folder, '3DOBB_part_image_cv2.json'), 'r') as json_file:
            OBBs = json.load(json_file)

        for key in OBBs:
            corners = OBBs[key]
            lines = [[0, 2], [2, 5], [5, 3], [3, 0],
                     [1, 7], [7, 4], [4, 6], [6, 1],
                     [0, 1], [2, 7], [5, 4], [3, 6]]
            for line in lines:
                start = (corners[0][line[0]], corners[1][line[0]])
                end = (corners[0][line[1]], corners[1][line[1]])
                # image = cv2.line(image, start, end, tuple(getObjColor(key.split('_')[0], obj_color)), thickness=1, lineType=cv2.LINE_AA)
                image = cv2.line(image, start, end, color, thickness=2, lineType=cv2.LINE_AA)

        cv2.imshow("3DOBB_part_image_cv2.png", image)
        cv2.waitKey(0)
        cv2.destroyAllWindows()

        cv2.imwrite(os.path.join(folder, '3DOBB_part_image_cv2.png'), image)

    def visualize_2DBB_image(self, folder):
        image = cv2.imread(os.path.join(folder, 'Rendering_Enscape.png'))

        # Line Color in BGR Format
        # obj_color = {}
        # random.seed(2)
        # for obj in objTypes:
        #     r = random.randint(0, 255)
        #     g = random.randint(0, 255)
        #     b = random.randint(0, 255)
        #     obj_color[obj] = [r, g, b]

        with open(os.path.join(folder, '2DBB_image_cv2.json'), 'r') as json_file:
            BBs = json.load(json_file)

        for key in BBs:
            xmaxs = BBs[key]['xmax']
            xmins = BBs[key]['xmin']
            ymaxs = BBs[key]['ymax']
            ymins = BBs[key]['ymin']

            # color = getObjColor(key.split('_')[0], obj_color)
            for i in range(len(xmaxs)):
                image = cv2.rectangle(image, (xmins[i], ymins[i]), (xmaxs[i], ymaxs[i]),
                                      (36, 255, 12), thickness=2, lineType=cv2.LINE_AA)
                obj = getObjType(key, self.objTypes)
                if obj == "Chair/Drehstuhl":
                    obj = "Chair"
                if obj == "Water-Glass/Cup":
                    obj = "Cup"
                # cv2.putText(image, obj, (xmins[i], ymins[i] - 10), cv2.FONT_HERSHEY_SIMPLEX, 1.6, (36, 255, 12), 3)

        cv2.imshow("image", image)
        cv2.waitKey(0)
        cv2.destroyAllWindows()

        cv2.imwrite(os.path.join(folder, '2DBB_image_cv2.png'), image)

    def visualize_2DBB_part_image(self, folder):
        image = cv2.imread(os.path.join(folder, 'Rendering_Enscape.png'))

        # Line Color in BGR Format
        # obj_color = {}
        # random.seed(2)
        # for obj in objTypes:
        #     r = random.randint(0, 255)
        #     g = random.randint(0, 255)
        #     b = random.randint(0, 255)
        #     obj_color[obj] = [r, g, b]

        with open(os.path.join(folder, '2DBB_part_image_cv2.json'), 'r') as json_file:
            BBs = json.load(json_file)

        for key in BBs:
            [xmaxs] = BBs[key]['xmax']
            [xmins] = BBs[key]['xmin']
            [ymaxs] = BBs[key]['ymax']
            [ymins] = BBs[key]['ymin']

            h = ymaxs - ymins
            w = xmaxs - xmins

            # color = getObjColor(key.split('_')[0], obj_color)
            image = cv2.rectangle(image, (xmins, ymins), (xmaxs, ymaxs),
                                  (36, 255, 12), thickness=2, lineType=cv2.LINE_AA)
            if w < 80:
                objPart = "leg"
            elif h > w:
                objPart = "backrest"
            elif ymaxs < 767 and xmaxs < 1800:
                objPart = "backrest"
            else:
                objPart = "seat"

            # cv2.putText(image, objPart, (xmins, ymins - 10), cv2.FONT_HERSHEY_SIMPLEX, 1.6, (36, 255, 12), 3)

        cv2.imshow("image", image)
        cv2.waitKey(0)
        cv2.destroyAllWindows()

        cv2.imwrite(os.path.join(folder, '2DBB_part_image_cv2.png'), image)