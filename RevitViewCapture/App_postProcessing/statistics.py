import os
import json
import numpy as np

input = r"D:\Huaquan\BimImageNet\segResults\dataset\rac_basic_sample_project"
# input = r"D:\Huaquan\BimImageNet\segResults\dataset\Equipment_room"

num_camLocation = 0        # No. of camera locations
num_images = 0             # No. of total images
num_validImages = 0        # No. of valid images
num_invalidImages = 0      # No. of invalid images
num_objInstances = 0       # No. of object instances
num_objPartInstances = 0   # No. of object part instances

num_objTypes = 0           # No. of object types
num_objMaterialTypes = 0        # No. of object material

objTypes =[]
materialTypes = []

for root, _, files in os.walk(input):
    for file in files:
        if file == "statistics_space.txt":
            with open(os.path.join(root, file)) as f:
                contents = f.readlines()
                num_camLocation += int(contents[0].split(":")[-1])
            directory_contents = os.listdir(root)
            for item in directory_contents:
                if os.path.isdir(os.path.join(root, item)):
                    if "Invalid" in item:
                        num_invalidImages += 1
                    else:
                        num_validImages += 1
                    num_images += 1
                    for root2, _, files2 in os.walk(os.path.join(root, item)):
                        for file2 in files2:
                            if file2 == "statistics_image.txt":
                                with open(os.path.join(root2, file2)) as f2:
                                    contents2 = f2.readlines()
                                    num_objInstances += int(contents2[8].split(":")[-1])
                                    num_objPartInstances += int(contents2[9].split(":")[-1])
                            if file2 == "material.json":
                                with open(os.path.join(root2, file2), encoding="utf8") as json1:
                                    data = json.load(json1)["Rendering_Enscape_ori"]["annotations"]
                                    data = np.unique(np.array(data).flatten()).tolist()
                                    materialTypes.extend(data)
                            if file2 == "objInstance.json":
                                with open(os.path.join(root2, file2), encoding="utf8") as json1:
                                    data = json.load(json1)["Rendering_Enscape_ori"]["annotations"]
                                    data = np.array(data).flatten().tolist()
                                    data = [x.split('$')[0] for x in data]
                                    data = [*set(data)]
                                    objTypes.extend(data)
objTypes = [*set(objTypes)]
materialTypes = [*set(materialTypes)]

print(objTypes)
print(materialTypes)
num_objTypes = len(objTypes)
num_objMaterialTypes = len(materialTypes)

with open(os.path.join(input, 'readme.txt'), 'w') as f:
    f.write("num_camLocation: " + str(num_camLocation) + '\n')
    f.write("num_images: " + str(num_images) + '\n')
    f.write("num_validImages: " + str(num_validImages) + '\n')
    f.write("num_invalidImages: " + str(num_invalidImages) + '\n')
    f.write("num_objInstances: " + str(num_objInstances) + '\n')
    f.write("num_objPartInstances: " + str(num_objPartInstances) + '\n')
    f.write("num_objTypes: " + str(num_objTypes) + '\n')
    f.write("num_objMaterialTypes: " + str(num_objMaterialTypes) + '\n')
