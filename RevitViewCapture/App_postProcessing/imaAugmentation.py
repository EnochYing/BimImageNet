import cv2


a= "C:/Users/enochying/Desktop/flip/3D View 21.png"
b= "C:/Users/enochying/Desktop/flip/3D View 29.png"

img = cv2.imread(a)

img_rotate_90_clockwise = cv2.rotate(img, cv2.ROTATE_90_CLOCKWISE)
cv2.imwrite('C:/Users/enochying/Desktop/flip/21_90_clockwise.png', img_rotate_90_clockwise)

img_rotate_90_counterclockwise = cv2.rotate(img, cv2.ROTATE_90_COUNTERCLOCKWISE)
cv2.imwrite('C:/Users/enochying/Desktop/flip/21_90_counterclockwise.png', img_rotate_90_counterclockwise)

img_rotate_180 = cv2.rotate(img, cv2.ROTATE_180)
cv2.imwrite('C:/Users/enochying/Desktop/flip/21_180.png', img_rotate_180)

img_flip_ud = cv2.flip(img, 0)
cv2.imwrite('C:/Users/enochying/Desktop/flip/flip_ud.png', img_flip_ud)

img_flip_lr = cv2.flip(img, 1)
cv2.imwrite('C:/Users/enochying/Desktop/flip/flip_lr.png', img_flip_lr)

img_flip_ud_lr = cv2.flip(img, -1)
cv2.imwrite('C:/Users/enochying/Desktop/flip/flip_ud_lr.png', img_flip_ud_lr)