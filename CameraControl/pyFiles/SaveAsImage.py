import cv2
#import os
import sys

inputPath = sys.argv[1]
outputpath = sys.argv[2]

img = cv2.imread(inputPath, 1)
#cv2.imwrite(os.path.join(path , '4.jpg'),img)
cv2.imwrite(outputpath,img)
cv2.waitKey(0)
