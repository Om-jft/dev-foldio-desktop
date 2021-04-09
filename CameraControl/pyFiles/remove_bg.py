#!/usr/bin/env python
# coding: utf-8

import sys
import cv2
import numpy as np
import time
import base64
from cv2.ximgproc import guidedFilter  # pip install opencv-contrib-python



def upload_file(path, modelPath,outputPath, gaussian_kernel=None):
    erode_kernel=1
    gb_fg=3
    if not path:
        return 'No file or path provided'
    try:
        start = time.time()

        # print(path)
        # src = cv2.imdecode(numpy.fromstring(path.read(), numpy.uint8), cv2.IMREAD_UNCHANGED)
        src = cv2.imread(path)
        # height, width, number of channels in image
        original_height = src.shape[0]
        original_width = src.shape[1]
        # print('Original Image Height       : ', original_height)
        # print('Original Image Width        : ', original_width)
        if src.shape[0] and src.shape[1] > 2000:
            src_resize = cv2.resize(src, None, fx=0.2, fy=0.2, interpolation=cv2.INTER_CUBIC)
            print(src_resize.shape)
        else:
            src_resize = src.copy()
        #Gaussian blur is used to reduce noise and detail and soften sphere edges which often contain
        # irregularities due to the rough surface of the marker
        # Blurring function; kernel=5, sigma=auto
        if gaussian_kernel:
           blurred = cv2.GaussianBlur(src_resize, (gaussian_kernel, gaussian_kernel), 0)
        else:
          blurred = cv2.GaussianBlur(src_resize, (5, 5), 0)
        blurred_float = blurred.astype(np.float32) / 255.0
        # pre-trained structured forest ML model
        # model do edge detection without keeping much noise
        edgeDetector = cv2.ximgproc.createStructuredEdgeDetection(modelPath)
        # print("model loaded")
        edges = edgeDetector.detectEdges(blurred_float) * 255.0
        edges_8u = np.asarray(edges, np.uint8)
        filterOutSaltPepperNoise(edges_8u)
        
        contour = findSignificantContour(edges_8u)
        # Draw the contour on the resized  image
        contourImg = np.copy(src_resize)
        cv2.drawContours(contourImg, [contour], 0, (0, 0, 255), 2, cv2.LINE_AA, maxLevel=1)

        mask = np.zeros_like(edges_8u)
        #cv2.fillPoly() function can be used to fill in any shape,here we will fill white colour inside detected edges
        cv2.fillPoly(mask, [contour], 255)
        
        # calculate sure foreground area by dilating the mask
        if erode_kernel:
            mapFg = cv2.erode(mask, np.ones((erode_kernel, erode_kernel), np.uint8), iterations=10)
        else:
            mapFg = cv2.erode(mask, np.ones((5, 5), np.uint8), iterations=10)

        # GC_BGD = 0 an obvious background pixels
        # GC_FGD = 1 an obvious foreground (object) pixel
        # GC_PR_BGD = 2 a possible background pixel
        # GC_PR_FGD = 3 a possible foreground pixel
        trimap = np.copy(mask)
        #mark black  colour in mask as sure backround
        trimap[mask == 0] = cv2.GC_BGD
        # mark white colour in mask as probably background
        trimap[mask == 255] = cv2.GC_PR_BGD
        # mark white colour in mapFg as Probable foreground, because image might have holes or transparency
        # which should not be foreground, if this is not the cases keep it sure foreground cv2.GC_FGD
        if gb_fg:
            trimap[mapFg == 255] = gb_fg
        else:
            trimap[mapFg == 255] = cv2.GC_PR_FGD


        # Run Mask Grabcut Algo.
 
        # bgdModel,fgdModel are required by grabcut algorithm
        bgdModel = np.zeros((1, 65), np.float64)
        fgdModel = np.zeros((1, 65), np.float64)
        # rect=(x,y,width,height)
        rect = (0, 0, mask.shape[0] - 1, mask.shape[1] - 1)
        # grabcut algorithm to get the exact edges
        # grabcut requires a hint on sure foreground, sure background and probable foregorund areas
        trimap, _, _=cv2.grabCut(src_resize, trimap, rect, bgdModel, fgdModel, 5, cv2.GC_INIT_WITH_MASK)
        # create mask again using the mask (trimap) changed from grabcut
        # where pixel value is sure or probable foreground  change it to 255 else 0
        mask2 = np.where(
            (trimap == cv2.GC_FGD) | (trimap == cv2.GC_PR_FGD),
            255,
            0
        ).astype('uint8')
        # print("Grabcut done")
        # Resize mask to original size of image which was before downsampling
        mask2 = cv2.resize(mask2,(original_width,original_height),interpolation=cv2.INTER_AREA)
        # blur the mask to make the zagged edges,lines smooth, you can remove but needed here to make the mask smooth and clean edges
        # mask2 = cv2.medianBlur(mask2,5)
        
        # running contour detection again if found some noise or extra edges etc and filling in the holes.
        #but in some cases pollyfills is not required here as it will fill the holes ,which are required in the image.
        # contour2 = findSignificantContour(mask2)
        # mask3 = np.zeros_like(mask2)
        # cv2.fillPoly(mask3, [contour2], 255)
        
        # If not running counter detection and pollyfill again then use mask2 only
        mask3=mask2.copy()

        # Alpha blending is  used to put rasterized foreground elements over a background
        # blended alpha cut-out
        # refer this for more details on alpha blending https://www.learnopencv.com/alpha-blending-using-opencv-cpp-python/
        # indexing with np.newaxis inserts a new 3rd dimension, which we then repeat the
        # array along, to convert 2 d array to 3 d array
        mask3 = np.repeat(mask3[:, :, np.newaxis], 3, axis=2)
        if gaussian_kernel:
            mask4 = cv2.GaussianBlur(mask3, (gaussian_kernel, gaussian_kernel), 0)
        else:
            mask4 = cv2.GaussianBlur(mask3, (3, 3), 0)

        alpha = mask4.astype(float) * 1.1  # making blend stronger
        alpha[mask3 > 0] = 255
        alpha[alpha > 255] = 255
        alpha = alpha.astype(float)
        
        foreground = np.copy(src).astype(float)
        foreground[mask4 == 0] = 0
        background = np.ones_like(foreground, dtype=float) * 255
        
        # Normalize the alpha mask to keep intensity between 0 and 1
        alpha = alpha / 255.0
        # Multiply the foreground with the alpha matte
        foreground = cv2.multiply(alpha, foreground)
        # Multiply the background with ( 1 - alpha )
        background = cv2.multiply(1.0 - alpha, background)
        # Add the masked foreground and background.
        cutout = cv2.add(foreground, background)
        retval, buffer_img = cv2.imencode('.jpg', cutout)
        final = base64.b64encode(buffer_img)
        # print(outputPath)
        # print(cutout)
        cv2.imwrite(outputPath, cutout)

        end = time.time()
        total_time = "[INFO] applying GrabCut took {:.2f} seconds".format(end - start)
        # print(total_time)
        # print("return")
        return "Success"
    except Exception as e:
        return "Error Occured : " ,e



# filter out further noise using median filters
# after applying this step the most main edges remain.
# This step is important as countour detection is sensitive to noise
#  and will detect noise too as countour
def filterOutSaltPepperNoise(edgeImg):
    # Get rid of salt & pepper noise.
    count = 0
    lastMedian = edgeImg
    # median blur is used it works by zeroing out any intensities
    # below the mean of all intensities throughout the image.
    median = cv2.medianBlur(edgeImg, 3)
    # where median and edgeImg are single color channel image vectors
    while not np.array_equal(lastMedian, median):
        # get those pixels that gets zeroed out / black
        zeroed = np.invert(np.logical_and(median, edgeImg))
        edgeImg[zeroed] = 0
 
        count = count + 1
        if count > 70:
            break
        lastMedian = median
        median = cv2.medianBlur(edgeImg, 3)



# The contours are a useful tool for shape analysis and object detection and recognition.
#  In OpenCV, finding contours is like finding white object from black background
 
def findSignificantContour(edgeImg):
    # This gives lot of contours
    contours, hierarchy = cv2.findContours(
        edgeImg,
        cv2.RETR_TREE,
        cv2.CHAIN_APPROX_SIMPLE
    )
    # pick the largest “first level” contour
    # Contour detection is hierarchical.
    # So you have contours inside contours inside contours inside.
    # We are only concerned with the “outer-most” contours.
    level1Meta = []
    for contourIndex, tupl in enumerate(hierarchy[0]):
        # Each array is in format (Next, Prev, First child, Parent)
        # Filter the ones without parent, because parent countour will have lots of child countour
        # parentId = -1
        if tupl[3] == -1:
            tupl = np.insert(tupl.copy(), 0, [contourIndex])
            level1Meta.append(tupl)
    # From among them, find the contours with large surface area.
    contoursWithArea = []
    for tupl in level1Meta:
        contourIndex = tupl[0]
        contour = contours[contourIndex]
        area = cv2.contourArea(contour)
        contoursWithArea.append([contour, area, contourIndex])
        
    contoursWithArea.sort(key=lambda meta: meta[1], reverse=True)
    largestContour = contoursWithArea[0][0]
    return largestContour


if __name__ == "__main__":

    modelPath = sys.argv[1]
    path = sys.argv[2]
    outputPath = sys.argv[3]
    gaussian_kernel= int(sys.argv[4])
    result = upload_file(path, modelPath,outputPath, gaussian_kernel)
    

    # kernels will be odd numbers always
    #erode_kernel=5
    #gaussian_kernel=5
    # gb_fg can have only 2 values (1 0r 3)
    # GC_PR_FGD = 3 a possible foreground pixel
    # GC_FGD = 1 sure foreground pixel
    #gb_fg = 3    
    #result = upload_file(path, erode_kernel=erode_kernel,gaussian_kernel=gaussian_kernel,gb_fg=gb_fg)
    print(result)
