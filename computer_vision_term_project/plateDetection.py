import cv2
import pytesseract
import numpy as np

#Path of tesseract ocr exe file
pytesseract.pytesseract.tesseract_cmd = 'C:\\Program Files\\Tesseract-OCR\\tesseract.exe'

#Min area parameter for comparing contour area
MIN_AREA = 100

#Function to calculate area of given contour
def getArea(cnt):
    [x, y, w, h] = cv2.boundingRect(cnt)
    return w*h


for i in range(1,10):

    # Load the image
    image = cv2.imread(f"deneme{i}.jpg")

    # Convert the image to grayscale
    gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)

    # Apply Gaussian blur to the image
    gray = cv2.GaussianBlur(gray, (3,3), 0)

    # Define the sharpening kernel
    kernel = np.array([[-1,-1,-1], [-1,9,-1], [-1,-1,-1]])

    # Apply the kernel to the image using a convolution
    gray = cv2.filter2D(gray, -1, kernel)

    # Apply Canny edge detection to the image
    edges = cv2.Canny(gray, 25, 125)

    # Find contours in the image
    contours, _ = cv2.findContours(edges, cv2.RETR_LIST, cv2.CHAIN_APPROX_SIMPLE)

    plateText = ""
    allowedChars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ"
    bbox_holder = []
    area = 0

    # Iterate through the contours and filter for top 10 contours if there are more than 10
    # Too many contours ma lead to false detections
    if len(contours) > 10:
        contours = sorted(contours, key=getArea, reverse=True)
        contours = contours[:11]

    # Iterate through the contours and filter for rectangular ones
    for cnt in contours:

        #Pass the iteration if contour has smaller area than desired
        if cv2.contourArea(cnt) < MIN_AREA:
            continue
        [x, y, w, h] = cv2.boundingRect(cnt)
        if w/h > 5 or h/w > 5:
            continue
        #cv2.rectangle(image, (x, y), (x + w, y + h), (0, 255, 0), 2)
        roi = gray[y:y + h, x:x + w]

        # Apply Otsu thresholding
        _, roi = cv2.threshold(roi, 0, 255, cv2.THRESH_BINARY + cv2.THRESH_OTSU)

        # Run Tesseract OCR on the region of interest
        text = pytesseract.image_to_string(roi,  
                config='--psm 11')

        #Pass the iteration if there is not detected text in contour
        if text == "":
            continue

        #Remove newline characters if there is any
        if "\n" in text:
            text = text.split("\n")

        #Remove white spaces
        for txt in text:

            txt = txt.replace(" ", "")
            txt = txt.replace( "\t", "")

            for ch in txt:
                if ch not in allowedChars:
                    txt = txt.replace(ch, "")

            if len(txt) > len(plateText):
                plateText = txt
                bbox_holder = [(x, y), (x + w, y + h), (0, 255, 0), 2]
                area =  w * h
            
        print(text)
        print("-----------------------------------------")

    if bbox_holder != []:
        cv2.rectangle(image, bbox_holder[0], bbox_holder[1], bbox_holder[2], bbox_holder[3])
        cv2.putText(image,  plateText, bbox_holder[0], cv2.FONT_HERSHEY_SIMPLEX, 0.9, (36,255,12), 2)

    # Show the original image with rectangles around the license plates
    print("Possible Plate Text: ", plateText)
    cv2.imwrite(f"./results/result{i}.jpg", image)
    #cv2.imshow("License Plate", image)
    #cv2.waitKey(0)