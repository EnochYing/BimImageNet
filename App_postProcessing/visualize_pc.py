import numpy as np
import os
import pandas as pd
import random

# random.seed(2)
# PCs = pd.read_csv(os.path.join("./data/018", 'F1-conference4_points.xyz'), sep=' ', header=None, dtype=str).values[:,:5]
# objType_PCs = [PCs[PCs[:, 3] == k] for k in np.unique(PCs[:, 3])]
#
# with open('./data/018/sematicSeg.txt', 'w') as f:
#     for pc in objType_PCs:
#         r = random.randint(0, 255)
#         g = random.randint(0, 255)
#         b = random.randint(0, 255)
#         for row in pc:
#             f.write(str(row[0]) + " " + str(row[1]) + " " + str(row[2]) + " " + str(r) + " " + str(g) + " " + str(b))
#             f.write('\n')


# PCs = pd.read_csv(os.path.join("./data/018", 'F1-conference4_points.xyz'), sep=' ', header=None, dtype=str).values[:,:5]
# PCs[:, 4] = np.array([e.split("-")[0] for e in PCs[:, 4]], dtype=str)
# PCs[:, 3] = PCs[:, 3] + PCs[:, 4]
# objInst_PCs = [PCs[PCs[:, 3] == k] for k in np.unique(PCs[:, 3])]
#
# with open('./data/018/InstanceSeg.txt', 'w') as f:
#     for pc in objInst_PCs:
#         r = random.randint(0, 255)
#         g = random.randint(0, 255)
#         b = random.randint(0, 255)
#         for row in pc:
#             f.write(str(row[0]) + " " + str(row[1]) + " " + str(row[2]) + " " + str(r) + " " + str(g) + " " + str(b))
#             f.write('\n')
#


# object_parts: 303jHYn9P9cvidowMMnmYS-ps....
# PCs = pd.read_csv(os.path.join("./data/018", 'F1-conference4_points.xyz'), sep=' ', header=None, dtype=str).values[:,:5]
# objPCs = []
# for row in PCs:
#     if "303jHYn9P9cvidowMMnmYS" in row[4]:
#         objPCs.append(row)
#
# obj_PCs = np.array(objPCs)
# with open('./data/018/singleObj.txt', 'w') as f:
#     for row in obj_PCs:
#         f.write(str(row[0]) + " " + str(row[1]) + " " + str(row[2]) + " " + str(0) + " " + str(255) + " " + str(0))
#         f.write('\n')
#
# objPart_PCs = [obj_PCs[obj_PCs[:, 4] == k] for k in np.unique(obj_PCs[:, 4])]
# with open('./data/018/singleObj_PartSeg.txt', 'w') as f:
#     for pc in objPart_PCs:
#         r = random.randint(0, 255)
#         g = random.randint(0, 255)
#         b = random.randint(0, 255)
#         for row in pc:
#             f.write(str(row[0]) + " " + str(row[1]) + " " + str(row[2]) + " " + str(r) + " " + str(g) + " " + str(b))
#             f.write('\n')



