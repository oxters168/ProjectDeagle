using UnityEngine;
using System.Collections.Generic;

public class AtlasMapper {

    public Texture2D atlas;
    public Rect[] mappedUVs { get { return coordinates != null ? FinalUVSet() : null; } private set { } }
    private List<Rect> coordinates;
    public int cushion = 8;
    public int maxSize = 2048;
    public float ratio { get; private set; }
    private static Color emptySpaceColor = Color.green;
    public bool addTexturesSeperately { get; private set; }
    private bool addedTextures;

    public AtlasMapper(bool addTexturesSeperately = false)
    {
        this.addTexturesSeperately = addTexturesSeperately;
        atlas = new Texture2D(0, 0);
        coordinates = new List<Rect>();
        ratio = 1;
        //uvPoints = new List<Rect>();
    }
		
	public void AddTextures(params Texture2D[] textures)
	{
        if (addTexturesSeperately) SequentialAdd(textures);
        else if (!addedTextures) AllAtOnce(textures);
        addedTextures = true;

        atlas.Apply();
	}
    private void SequentialAdd(Texture2D[] textures)
    {
        foreach (Texture2D texture in textures)
        {
            if (texture != null)
            {
                //Debug.Log("Width: " + atlas.width + " Height: " + atlas.height);
                Rect imageBoundingBox = new Rect();

                Texture2D scaledTexture = new Texture2D(texture.width, texture.height);
                scaledTexture.SetPixels(texture.GetPixels());
                if (ratio != 1) TextureScale.Point(scaledTexture, ((int)(scaledTexture.width / ratio)), ((int)(scaledTexture.height / ratio)));

                int imageX = 0, imageY = 0;
                int ratioedCushion = ((int)(cushion / ratio));

                if (coordinates != null && coordinates.Count > 0)
                {
                    imageX = Mathf.RoundToInt((coordinates[coordinates.Count - 1].x + coordinates[coordinates.Count - 1].width)) + ratioedCushion;
                    imageY = Mathf.RoundToInt(coordinates[coordinates.Count - 1].y);
                    //Debug.Log("X From Coordinates: " + (coordinates[coordinates.Count - 1].x + coordinates[coordinates.Count - 1].width) + " Y From Coordinates: " + coordinates[coordinates.Count - 1].y + " Image X: " + imageX + " Image Y: " + imageY);
                }

                Color[] oldAtlas = null;
                if (atlas.width > 0 && atlas.height > 0) oldAtlas = atlas.GetPixels();
                int oldWidth = atlas.width, oldHeight = atlas.height;

                if (imageX + scaledTexture.width > maxSize)
                {
                    //Resize atlas to fit image and change image coordinates to top left corner
                    atlas.Resize(oldWidth, oldHeight + scaledTexture.height + ratioedCushion);
                    imageX = 0;
                    imageY = oldHeight;
                    Debug.Log("Going Up! Resized Atlas: " + atlas.height + " Old Atlas Height: " + oldHeight + " Next Image Y: " + imageY + " Next Image Height: " + scaledTexture.height);
                }
                else if (imageX + scaledTexture.width > oldWidth || imageY + scaledTexture.height > oldHeight)
                {
                    //Resize atlas to fit image on the right side
                    //atlas.Resize((imageX + scaledTexture.width > atlas.width) ? (atlas.width + (scaledTexture.width - ((atlas.width - 1) - imageX))) : atlas.width, (imageY + scaledTexture.height > atlas.height) ? (atlas.height + (scaledTexture.height - ((atlas.height - 1) - imageY))) : atlas.height);
                    atlas.Resize((imageX + scaledTexture.width + ratioedCushion > oldWidth) ? (oldWidth + ((scaledTexture.width + ratioedCushion) - (oldWidth - imageX))) : oldWidth, (imageY + scaledTexture.height + ratioedCushion > oldHeight) ? (oldHeight + ((scaledTexture.height + ratioedCushion) - (oldHeight - imageY))) : oldHeight);
                }

                //Add previous images back in
                if (oldAtlas != null) atlas.SetPixels(0, 0, oldWidth, oldHeight, oldAtlas);

                //Debug.Log("Image X: " + imageX + " Image Y: " + imageY + " Image Width: " + scaledTexture.width + " Image Height: " + scaledTexture.height);
                //Add image to atlas at image coordinates
                atlas.SetPixels(imageX, imageY, scaledTexture.width, scaledTexture.height, scaledTexture.GetPixels());
                #region Green Border
                /*for(int col = 0; col < scaledTexture.width; col++)
                {
                    atlas.SetPixel(imageX + col, imageY, Color.green); //Bottom Border
                }
                for (int col = 0; col < scaledTexture.width; col++)
                {
                    atlas.SetPixel(imageX + col, imageY + scaledTexture.height - 1, Color.green); //Top Border
                }
                for (int row = 0; row < scaledTexture.height; row++)
                {
                    atlas.SetPixel(imageX, imageY + row, Color.green); //Left Border
                }
                for (int row = 0; row <= scaledTexture.height; row++)
                {
                    atlas.SetPixel(imageX + scaledTexture.width - 1, imageY + row, Color.green); //Right Border
                }*/
                #endregion

                //Set coordinates
                imageBoundingBox.x = imageX;
                imageBoundingBox.y = imageY;
                imageBoundingBox.width = scaledTexture.width;
                imageBoundingBox.height = scaledTexture.height;
                coordinates.Add(imageBoundingBox);
                //uv.x = (uv.x - (0 / ratio)) / atlas.width; //38
                //uv.y = (uv.y + (0 / ratio)) / atlas.height; //6
                //uv.width /= atlas.width;
                //uv.height /= atlas.height;
                //uvPoints.Add(uv);

                if (atlas.width > maxSize || atlas.height > maxSize)
                {
                    //Debug.Log("Reduced");
                    //Scale down atlas
                    //float changeInRatio = ratio;
                    float decreasedSizeRatio = ((float)Mathf.Max(atlas.width, atlas.height)) / maxSize;
                    int decreasedWidth = (int)(atlas.width / decreasedSizeRatio), decreasedHeight = (int)(atlas.height / decreasedSizeRatio);
                    //decreasedSizeRatio = ((atlas.width / decreasedWidth) + (atlas.height / decreasedHeight)) / 2;
                    //decreasedWidth = (int)(atlas.width / decreasedSizeRatio);
                    //decreasedHeight = (int)(atlas.height / decreasedSizeRatio);
                    ratio += decreasedSizeRatio - 1;
                    //changeInRatio = sizedDownBy - changeInRatio;
                    TextureScale.Point(atlas, decreasedWidth, decreasedHeight);
                    ApplyRatio(decreasedSizeRatio);
                }

                Object.DestroyImmediate(scaledTexture);
                scaledTexture = null;
            }

            System.GC.Collect();
        }
    }
    private void AllAtOnce(Texture2D[] textures)
    {
        int maxStackedWidth = 0, maxStackedHeight = 0, largestWidth = 0, largestHeight = 0;
        int colCount = Mathf.CeilToInt(Mathf.Sqrt(textures.Length));
        for (int i = 0; i < textures.Length; i++)
        {
            if (textures[i] != null)
            {
                maxStackedWidth += textures[i].width;
                if (textures[i].width > largestWidth) largestWidth = textures[i].width;
                maxStackedHeight += textures[i].height;
                if (textures[i].height > largestHeight) largestHeight = textures[i].height;
            }
        }

        //int stackedWidthWithCushion = (maxStackedWidth + (cushion * (textures.Length - 0))) / 2, stackedHeightWithCushion = (maxStackedHeight + (cushion * (textures.Length - 0))) / 2;
        int stackedWidthWithCushion = ((largestWidth * colCount) + (cushion * (textures.Length - 0))) / 2, stackedHeightWithCushion = ((largestHeight * colCount) + (cushion * (textures.Length - 0))) / 2;
        if (stackedWidthWithCushion > maxSize || stackedHeightWithCushion > maxSize)
        {
            ratio = Mathf.Max(stackedWidthWithCushion / ((float)maxSize), stackedHeightWithCushion / ((float)maxSize));
        }

        int ratioedCushion = ((int)(cushion / ratio));
        int largestHeightInRow = 0, currentX = 0, currentY = 0;
        for(int i = 0; i < textures.Length; i++)
        {
            if(textures[i] != null)
            {
                Texture2D scaledTexture = new Texture2D(textures[i].width, textures[i].height);
                scaledTexture.SetPixels(textures[i].GetPixels());
                if (ratio != 1) TextureScale.Point(scaledTexture, ((int)(scaledTexture.width / ratio)), ((int)(scaledTexture.height / ratio)));

                int col = (i + 0) % colCount, row = (i + 0) / colCount;
                if(col == 0) { currentX = 0; currentY += largestHeightInRow + ratioedCushion; largestHeightInRow = 0; }
                if (scaledTexture.height > largestHeightInRow) largestHeightInRow = scaledTexture.height;

                Color[] oldAtlas = null;
                if (atlas.width > 0 && atlas.height > 0) oldAtlas = atlas.GetPixels();
                int oldWidth = atlas.width, oldHeight = atlas.height;
                if (atlas.width < currentX + scaledTexture.width + ratioedCushion || atlas.height < currentY + scaledTexture.height + ratioedCushion)
                {
                    int resizedWidth = Mathf.Max(atlas.width, currentX + scaledTexture.width + ratioedCushion), resizedHeight = Mathf.Max(atlas.height, currentY + scaledTexture.height + ratioedCushion);
                    atlas.Resize(resizedWidth, resizedHeight);

                    Color[] backDrop = new Color[resizedWidth * resizedHeight];
                    for(int j = 0; j < backDrop.Length; j++)
                    {
                        backDrop[j] = emptySpaceColor;
                    }
                    atlas.SetPixels(backDrop);
                }
                if (oldAtlas != null) atlas.SetPixels(0, 0, oldWidth, oldHeight, oldAtlas);

                atlas.SetPixels(currentX, currentY, scaledTexture.width, scaledTexture.height, scaledTexture.GetPixels());
                coordinates.Add(new Rect(currentX, currentY, scaledTexture.width, scaledTexture.height));

                currentX += scaledTexture.width + ratioedCushion;

                Object.DestroyImmediate(scaledTexture);
                scaledTexture = null;
            }

            System.GC.Collect();
        }
    }

    private Vector2 FindEmptySpaceFor(int requiredWidth, int requiredHeight)
    {
        Color[] flattenedAtlas = atlas.GetPixels();

        for (int row = 0; row < atlas.height; row += requiredHeight / 2)
        {
            for (int col = 0; col < atlas.width; col += requiredWidth / 2)
            {
                int flatSearchIndex = (row * atlas.width) + col;
                
                if (FivePointCheck(flattenedAtlas, atlas.width, requiredWidth, requiredHeight, flatSearchIndex))
                {
                    #region Check Entire Area Empty
                    bool doesntFit = false;

                    for (int verifyRow = 0; verifyRow < requiredHeight; verifyRow++)
                    {
                        for (int verifyCol = 0; verifyCol < requiredWidth; verifyCol++)
                        {
                            int flatVerifyIndex = flatSearchIndex + (verifyRow * (atlas.width - 0)) + verifyCol;
                            if (flatVerifyIndex >= flattenedAtlas.Length || ColorDiff(flattenedAtlas[flatVerifyIndex], emptySpaceColor) >= 0.1f)
                            {
                                doesntFit = true;
                                break;
                            }
                        }

                        if (doesntFit)
                            break;
                    }

                    if (!doesntFit)
                        return new Vector2(flatSearchIndex % atlas.width, flatSearchIndex / atlas.height);
                    #endregion
                }
            }
            
            //col = 0;
        }
        return new Vector2(-1, -1);
    }

    private static bool FivePointCheck(Color[] flattenedImage, int imageWidth, int requiredWidth, int requiredHeight, int bottomLeftFlatIndex)
    {
    		bool fivePointCheck = true;
    		
	    int bottomLeftCorner = bottomLeftFlatIndex;
	    int bottomRightCornerFlatIndex = bottomLeftFlatIndex + requiredWidth - 1;
	    int topLeftCornerFlatIndex = bottomLeftFlatIndex + ((imageWidth - 0) * (requiredHeight - 1));
	    int topRightCornerFlatIndex = bottomLeftFlatIndex + ((imageWidth - 0) * (requiredHeight - 1)) + (requiredWidth - 1);
	    int centerFlatIndex = bottomLeftFlatIndex + ((imageWidth - 0) * ((requiredHeight - 1) / 2)) + ((requiredWidth - 1) / 2);
	    if (ColorDiff(flattenedImage[bottomLeftCorner], emptySpaceColor) >= 0.1f) fivePointCheck = false;
	    if (bottomRightCornerFlatIndex >= flattenedImage.Length || ColorDiff(flattenedImage[bottomRightCornerFlatIndex], emptySpaceColor) >= 0.1f) fivePointCheck = false;
	    if (topLeftCornerFlatIndex >= flattenedImage.Length || ColorDiff(flattenedImage[topLeftCornerFlatIndex], emptySpaceColor) >= 0.1f) fivePointCheck = false;
	    if (topRightCornerFlatIndex >= flattenedImage.Length || ColorDiff(flattenedImage[topRightCornerFlatIndex], emptySpaceColor) >= 0.1f) fivePointCheck = false;
	    if (centerFlatIndex >= flattenedImage.Length || ColorDiff(flattenedImage[centerFlatIndex], emptySpaceColor) >= 0.1f) fivePointCheck = false;
	    
	    return fivePointCheck;
	}

    private void ApplyRatio(float r)
    {
        for (int i = 0; i < coordinates.Count; i++)
        {
            Rect imageBoundingBox = new Rect();
            imageBoundingBox.x = coordinates[i].x / r;
            imageBoundingBox.y = coordinates[i].y / r;
            imageBoundingBox.width = coordinates[i].width / r;
            imageBoundingBox.height = coordinates[i].height / r;
            //imageBoundingBox.x = Mathf.Floor(coordinates[i].x / r);
            //imageBoundingBox.y = Mathf.Ceil(coordinates[i].y / r);
            //imageBoundingBox.width = Mathf.Floor(coordinates[i].width / r);
            //imageBoundingBox.height = Mathf.Floor(coordinates[i].height / r);
            coordinates[i] = imageBoundingBox;
            //uv.x = (uv.x - (8 / ratio)) / atlas.width; //16
            //uv.y = (uv.y + (0 / ratio)) / atlas.height; //8
            //uv.width = (uv.width - (0 / ratio)) / atlas.width; //8
            //uv.height = (uv.height - (0 / ratio)) / atlas.height; //4
            //uvPoints[i] = uv;
        }
    }
    private Rect[] FinalUVSet()
    {
        Rect[] uvPoints = new Rect[coordinates.Count];

        for (int i = 0; i < uvPoints.Length; i++)
        {
            Rect uv = new Rect();
            uv = coordinates[i];
            uv.x = (Mathf.Floor(uv.x) + (cushion / ratio)) / atlas.width;
            uv.y = (Mathf.Floor(uv.y) + (cushion / ratio)) / atlas.height;
            uv.width = (Mathf.Floor(uv.width) - (cushion / ratio) - 1) / atlas.width;
            uv.height = (Mathf.Floor(uv.height) - (cushion / ratio) - 1) / atlas.height;
            uvPoints[i] = uv;
            //uvPoints.Add(uv);
        }

        return uvPoints;
    }

    public static float ColorDiff(Color firstC, Color secondC)
    {
        int rDiff = (int)((Mathf.Max(firstC.r, secondC.r) - Mathf.Min(firstC.r, secondC.r)) * 1000f);
        int gDiff = (int)((Mathf.Max(firstC.g, secondC.g) - Mathf.Min(firstC.g, secondC.g)) * 1000f);
        int bDiff = (int)((Mathf.Max(firstC.b, secondC.b) - Mathf.Min(firstC.b, secondC.b)) * 1000f);
        int aDiff = (int)((Mathf.Max(firstC.a, secondC.a) - Mathf.Min(firstC.a, secondC.a)) * 1000f);

        float totalDiff = (((float) (rDiff + gDiff + bDiff + aDiff)) / 4000f);
        return totalDiff;
    }
}