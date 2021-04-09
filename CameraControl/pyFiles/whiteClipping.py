import sys
import cv2
import concurrent.futures

def white_clipping(path, outputPath, value):
    lower_value = value
    upper_value = 255
    lower_color_bounds = (lower_value, lower_value, lower_value)
    upper_color_bounds = (upper_value, upper_value, upper_value)
    
    image = cv2.imread(path)
    #cv2.imshow('frame',image)
    gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)
    #cv2.imshow('gray',gray)
    mask = cv2.inRange(image,lower_color_bounds,upper_color_bounds )
    #cv2.imshow('mask',mask)
    mask_rgb = cv2.cvtColor(mask, cv2.COLOR_GRAY2BGR)
    #cv2.imshow('mask_rgb',mask_rgb)
    image = image | mask_rgb
    #cv2.imshow('res',image)
    cv2.imwrite(outputPath, image)
    
    
    return True


def multiProcessing(input_path_array, output_path_array, value):

    value_array = [value] * len(input_path_array)
    with concurrent.futures.ProcessPoolExecutor() as executor:
        results = executor.map(white_clipping, input_path_array, output_path_array, value_array)
    return "Success"

def ConvertStringToStringArray(string): 
    li = list(string.split(",")) 
    return li 


if __name__ == "__main__":
    #path = ['E:\\4.Jpg', 'E:\\5.jpg']
    #outputpath = ['E:\\New Folder\\4.Jpg','E:\\New Folder\\5.Jpg']
    #value = 112
    #result = white_clipping(path, outputPath, value)

    path = sys.argv[1]
    outputpath = sys.argv[2]
    value= int(sys.argv[3])
    SorM = int(sys.argv[4])

    if SorM == 1:
        input_path_array = ConvertStringToStringArray(path)
        output_path_array = ConvertStringToStringArray(outputpath)

        result = multiProcessing(input_path_array, output_path_array, value)  
    else:
        result = white_clipping(path,outputpath,value)
    print(result)

