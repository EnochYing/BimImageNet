from utils import *
import random
import pandas as pd
import sys
import numpy as np
import time


def prepareObjectColors(objTypes):
    obj_color = {}
    random.seed(6)
    for obj in objTypes:
        if obj == "Background":
            obj_color[obj] = [255, 255, 255]
        else:
            r = random.randint(0, 255)
            g = random.randint(0, 255)
            b = random.randint(0, 255)
            obj_color[obj] = [r, g, b]
    return obj_color


def prepareObjectAndMaterialColors(objTypes, materialTypes):
    obj_color = {}
    random.seed(6)
    for obj in objTypes:
        if obj == "Background":
            obj_color[obj] = [255, 255, 255]
        else:
            r = random.randint(0, 255)
            g = random.randint(0, 255)
            b = random.randint(0, 255)
            obj_color[obj] = [r, g, b]

    material_color = {}
    random.seed(20)
    for mal in materialTypes:
        if mal == "Background":
            material_color[mal] = [255, 255, 255]
        else:
            r = random.randint(0, 255)
            g = random.randint(0, 255)
            b = random.randint(0, 255)
            material_color[mal] = [r, g, b]

    return obj_color, material_color


def prepareAnnotations(sourceFolder, panopticSeg_semanticList):
    # a-b means a and b must be included
    # a/b means either a or b is included
    objTypes = ['Air-Terminal', "Beam", "Bottle", "Casework", "Cabinet", 'Cable-Tray', 'Cable-Tray-Fitting', "Ceiling",
                "Chair/Drehstuhl", "Cooktop", "Column", "Curtain-Wall-Mullion", "Curtain-Panel", "Dishwasher", "Door",
                'Duct',
                'Duct-Fitting', 'Fire-Alarm-Device', "Fire-Place-Hang", "Floor", "Footing", "Fridge", 'Gutters',
                'Kammer', 'Keyboard',
                'Landing', "Microwave", 'Zoll/Monitor', "Oven", 'Pipe', "Pipe-Fitting", 'Pipe-Accessories',
                'Pipe-Insulation', "Plate", "Railing/Rail",
                'Ramp', "Rangehood", "Roof", "Run", "Sofa", "Stair", 'Structural-Framing', "Table/Desk", "Vase", "Wall",
                "Walvit",
                "Washing-Machine", "Water-Glass/Cup", "Window", "Background", "Plumbing-Fixture", "Furniture",
                "Lighting-Fixture",
                "Specialty-Equipment", 'Lighting-Device', 'Electrical-Fixture', "Mechanical-Equipment", "Others"]
    materialTypes = ['Aluminum', 'Background', 'Cement', 'Ceramic', 'Chrome', 'Concrete', 'Fabric/Carpet', 'Glass',
                     'Gypsum/Plasterboard', 'Iron', 'Leather', 'Maple', 'Matte', 'Metal', 'Plastic', 'Rubber', 'Steel',
                     'Terrazzo', 'Wood/Timber', 'Others']

    obj_color, material_color = prepareObjectAndMaterialColors(objTypes, materialTypes)

    newobjTypes = []
    newMaterialTypes = []

    time_pixelwiseAnnotation = 0
    time_BB = 0
    for root, _, files in os.walk(sourceFolder):
        for file in files:
            if 'depth.txt' in file:
                # 1. generate depth image
                # depth limited to 21m
                # output depth image named depth.png in the same folder
                time_depth = time.time()
                generateDepthImage(os.path.join(root, file))
                time_pixelwiseAnnotation += (time.time() - time_depth)
            elif 'objInstance.json' in file:
                time_instance = time.time()
                # 2. generate segmentation results
                with open(os.path.join(root, file), 'r', encoding='utf-8') as f:
                    data = json.load(f)
                annotations = np.array(list(data.values())[0]['annotations'])
                # newobjTypes.extend(getObjectTypes(annotations, objTypes))

                # 2.1 generate semantic segmentation image
                # using obj_color to color object types
                fileName_segSeg = 'semSeg.png'
                file_segSeg = os.path.join(root, fileName_segSeg)
                generateSemSegImage(file_segSeg, annotations, obj_color)

                # 2.2 generate instance segmentation image
                # using fixed random color to color object instances
                fileName_instSeg = 'instSeg.png'
                file_instSeg = os.path.join(root, fileName_instSeg)
                generateInstSegImage(file_instSeg, annotations)
                #
                # 2.3 calculate object mask boundaries
                # objInst_mask, objInst_maskBoundary = calculateObjMasks(annotations, threshold_area=0)
                objInst_mask = calculateObjMasks(annotations, threshold_area=0)
                #
                # 2.4 generate object part instance segmentation image
                fileName_partInstSeg = 'partInstSeg.png'
                file_partInstSeg = os.path.join(root, fileName_partInstSeg)
                generatePartInstSegImage(file_partInstSeg, annotations)

                time_pixelwiseAnnotation += (time.time() - time_instance)

                # 2.5 calculate object part mask boundaries
                # objPartInst_mask, objPartInst_maskBoundary = calculateObjPartMasks(annotations)
                time_2DBB = time.time()
                objPartInst_mask = calculateObjPartMasks(annotations)

                # # 2.6 generate 2D object BB
                object_2DBB = os.path.join(root, '2DBB_image_cv2.json')
                # objTypes_demo = ["Bottle", "Plate", "Chair/Drehstuhl", "Water-Glass/Cup"]
                # objTypes_demo = ["Keyboard", 'Zoll/Monitor']
                # objTypes_demo = ['Pipe-Accessories', "Pipe-Fitting"]
                #objTypes_demo = objTypes
                # generate2DBB_objMB(objInst_maskBoundary, object_2DBB, objTypes_demo)
                #generate2DBB_objM(objInst_mask, object_2DBB, objTypes_demo)
                generate2DBB_objM(objInst_mask, object_2DBB)

                objectPart_2DBB = os.path.join(root, '2DBB_part_image_cv2.json')
                # objPartTypes_demo = ["Chair/Drehstuhl"]
                # objPartTypes_demo = ["Keyboard", 'Zoll/Monitor']
                # objPartTypes_demo = ['Pipe-Accessories', "Pipe-Fitting"]
                # generate2DBB_objMB(objPartInst_maskBoundary, objectPart_2DBB, objPartTypes_demo)
                # generate2DBB_objM(objPartInst_mask, objectPart_2DBB, objTypes_demo)
                generate2DBB_objM(objPartInst_mask, objectPart_2DBB)

                time_BB += (time.time() - time_2DBB)

                # # 2.7 enhance rendering image with object masks
                # # Method 1 (opencv): using rendering image as the background and add masks with transparancy by cv.addWeighted()
                # # Method 2 (matplotlib.pyplot): flexibly add mask labels and adjust figure size
                # renderImage = os.path.join(root, 'Rendering_Enscape.png')
                # output = os.path.join(root, 'maskedRendering_Enscape.png')
                # objTypes_demo = ["Bottle", "Plate", "Chair/Drehstuhl", "Water-Glass/Cup"]
                # # objTypes_demo = ["Keyboard", 'Zoll/Monitor']
                # # objTypes_demo = ['Pipe-Accessories', "Pipe-Fitting"]
                # interestObjInst_color = generateMaskedRenderingImage(renderImage, objInst_mask, output, objTypes_demo)
                #
                # # 2.8 enhance rendering image with object part masks
                # renderImage = os.path.join(root, 'Rendering_Enscape.png')
                # output = os.path.join(root, 'maskedRendering_objPart_Enscape.png')
                # objTypes_demo = ["Chair/Drehstuhl"]
                # # objTypes_demo = ["Keyboard", 'Zoll/Monitor']
                # # objTypes_demo = ['Pipe-Accessories', "Pipe-Fitting"]
                # generateMaskedRenderingImage(renderImage, objPartInst_mask, output, objTypes_demo)

                # 2.9 generate panoptic annotaions
                time_panoptic = time.time()
                fileName_panoSeg = 'panoSeg.png'
                file_panoSeg = os.path.join(root, fileName_panoSeg)
                interestObjInst_color = prepareObjectColors(panopticSeg_semanticList)
                generatePanoSegImage(file_panoSeg, annotations, objInst_mask, obj_color, interestObjInst_color)

                time_pixelwiseAnnotation += (time.time() - time_panoptic)

            elif 'material.json' in file:
                # 3. generate segmentation results
                time_material = time.time()
                with open(os.path.join(root, file), 'r',
                          encoding='utf-8') as f:  # encoding = ''utf-8' enables to parse chinese characters
                    data = json.load(f)
                # print(data)
                annotations = np.array(list(data.values())[0]['annotations'])
                # newMaterialTypes.extend(getObjectTypes(annotations, materialTypes))

                # 3.1 generate semantic segmentation image
                # using material_color to color material types
                file_material = os.path.join(root, 'material.png')
                ## generateMaterialSegImage(file_material, annotations, material_color)
                generateMaterialSegImage(file_material, annotations, material_color)

                time_pixelwiseAnnotation += (time.time() - time_material)

            elif 'objPCs.txt' in file:
                time_3DObjBB = time.time()
                PCs = pd.read_csv(os.path.join(root, file), sep=' ', header=None, dtype=str).values

                # # 4.1 generate colored raw point clouds
                # renderImage = os.path.join(root, 'Rendering_Enscape.png')
                # rgb = cv.imread(renderImage)
                # rgb = rgb.reshape((-1, 3)).astype(str)
                # rgb[:, [0, 2]] = rgb[:, [2, 0]]
                # purePCs = PCs[:, 0:4]
                #
                # # remove 'background' pixels (without corresponding points in PC)
                # with open(os.path.join(root, 'objInstance.json'), 'r', encoding='utf-8') as f:
                #     data = json.load(f)
                # annotations = np.array(list(data.values())[0]['annotations'])
                # background_pixels = []
                # for i, row in enumerate(annotations):
                #     for j, ele in enumerate(row):
                #         if ele == 'Background':
                #             background_pixels.append(i * annotations.shape[1] + j)
                # rgb = np.delete(rgb, background_pixels, axis=0)
                #
                # coloredPCs = np.column_stack((purePCs, rgb))
                # with open(os.path.join(root, "coloredObjPCs.txt"), "w", encoding="utf-8") as wri:
                #     for row in coloredPCs:
                #         line = " ".join(row) + '\n'
                #         wri.write(line)

                # 4.2 generate 3D OBB for objects
                objPCs = [PCs[PCs[:, 0] == k] for k in np.unique(PCs[:, 0])]
                # objTypes_demo = ["Bottle", "Plate", "Chair/Drehstuhl", "Water-Glass/Cup"]
                # objTypes_demo = ["Keyboard", 'Zoll/Monitor']
                # objTypes_demo = ['Pipe-Accessories', "Pipe-Fitting"]

                output_3DOBB_PC = os.path.join(root, '3DOBB_PC_open3d.json')
                #OBB_corners = get3DOBB_PCs_store(objPCs, objTypes_demo, output_3DOBB_PC)
                OBB_corners = get3DOBB_PCs_store(objPCs, output_3DOBB_PC)

                output_3DOBB_image = os.path.join(root, '3DOBB_image_cv2.json')
                get3DOBB_image_store(OBB_corners, os.path.join(root, 'TM_GCS2PCS.json'), output_3DOBB_image)

                time_BB += (time.time() - time_3DObjBB)

            elif 'objPartPCs.txt' in file:
                time_3DObjPartBB = time.time()
                PCs = pd.read_csv(os.path.join(root, file), sep=' ', header=None, dtype=str).values

                # 5.1 generate 3D OBB for object parts
                objPCs = [PCs[PCs[:, 0] == k] for k in np.unique(PCs[:, 0])]
                # objTypes_demo = ["Chair/Drehstuhl"]
                # objTypes_demo = ["Keyboard", 'Zoll/Monitor']
                # objTypes_demo = ['Pipe-Accessories', "Pipe-Fitting"]

                output_3DOBB_part_PC = os.path.join(root, '3DOBB_part_PC_open3d.json')
                # OBB_corners = get3DOBB_PCs_store(objPCs, objTypes_demo, output_3DOBB_part_PC)
                OBB_corners = get3DOBB_PCs_store(objPCs, output_3DOBB_part_PC)

                output_3DOBB_part_image = os.path.join(root, '3DOBB_part_image_cv2.json')
                get3DOBB_image_store(OBB_corners, os.path.join(root, 'TM_GCS2PCS.json'), output_3DOBB_part_image)

                time_BB += (time.time() - time_3DObjPartBB)

    with open(os.path.join(sourceFolder, "anotationTime.txt"), "w") as file:
        file.write("Pixelwise annotation time: " + str(time_pixelwiseAnnotation) + '\n')
        file.write("Other annotation time: " + str(time_BB) + '\n')



    # # detect new object types
    # newobjTypes = list(set(newobjTypes))
    # newobjTypes = [obj for obj in newobjTypes if obj not in objTypes]
    # print(newobjTypes)
    #
    # # detect new material types
    # newMaterialTypes = list(set(newMaterialTypes))
    # newMaterialTypes = [mal for mal in newMaterialTypes if mal not in materialTypes]
    # print(newMaterialTypes)


if __name__ == "__main__":
   if len(sys.argv) > 1:
       argvs = sys.argv[1].split(",")
       sourceFolder = argvs[0]
       panopticSeg_semanticList = argvs[1:]
       prepareAnnotations(sourceFolder, panopticSeg_semanticList)
   # prepareAnnotations(r"C:\Users\enochying\Desktop\Case1_rac_basic_sample_project_v2\Dining_3DView 4\1536", ["Wall", "Roof"])

