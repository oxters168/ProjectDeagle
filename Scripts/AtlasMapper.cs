using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AtlasMapper {

    public Texture2D atlas;
    public Rect[] mappedUVs { get { return coordinates.ToArray(); } private set { } }
    private List<Rect> coordinates;
    public int maxSize = 2048;
    public float ratio = 1;
    private static Color emptySpaceColor = Color.green;

    public AtlasMapper()
    {
        atlas = new Texture2D(16, 16);
        Color[] greenScreen = new Color[16 * 16];
        for (int i = 0; i < greenScreen.Length; i++)
        {
            greenScreen[i] = Color.green;
        }
        atlas.SetPixels(greenScreen);
        coordinates = new List<Rect>();
    }
		
		public void AddTextures(params Texture2D[] textures)
		{
			foreach(Texture2D texture in textures)
			{
				if(texture != null)
				{
					Rect uv = new Rect();
					
					Texture2D scaledTexture = new Texture2D(texture.width, texture.height);
					scaledTexture.SetPixels(texture.GetPixels());
					TextureScale.Point(scaledTexture, ((int)(scaledTexture.width / ratio)), ((int)(scaledTexture.height / ratio)));
					
					int imageX = 0, imageY = 0;
					if(coordinates != null && coordinates.Count > 0)
					{
						imageX = coordinates[coordinates.Count - 1].x + coordinates[coordinates.Count - 1].width;
						imageY = coordinates[coordinates.Count - 1].y;
					}
					
					Color[] oldAtlas = atlas.GetPixels();
					int oldWidth = atlas.width, oldHeight = atlas.Height;
					
					if(imageX + scaledTexture.width > maxSize && imageY + scaledTexture.height > maxSize)
					{
						//Resize atlas to fit image and change image coordinates to top left corner
						atlas.Resize(scaledTexture.width, atlas.height + scaledTexture.height);
						imageX = 0;
						imageY = oldHeight;
					}
					else if(imageX + scaledTexture.width > maxSize)
					{
						//Resize atlas to fit image and change image coordinates to top left corner
						atlas.Resize(atlas.width, atlas.height + scaledTexture.height);
						imageX = 0;
						imageY = oldHeight;
					}
					else
					{
						atlas.Resize(atlas.width + scaledTexture.width, atlas.height);
					}
					
					//Add previous images back in
					atlas.SetPixels(0, 0, oldWidth, oldHeight, oldAtlas);
					
					//Add image to atlas at image coordinates
					atlas.SetPixels(imageX, imageY, scaledTexture.width, scaledTexture.Height, scaledTexture.GetPixels());
					
					//Set UV coordinates
					uv.x = imageX;
					uv.y = imageY;
					uv.width = scaledTexture.width;
					uv.height = scaledTexture.height;
					
					if (atlas.width > maxSize || atlas.height > maxSize)
					{
					    //Scale down atlas
					    float changeInRatio = ratio;
					    ratio = ((float) Mathf.Max(atlas.width, atlas.height)) / maxSize;
					    changeInRatio = ratio - changeInRatio;
					    TextureScale.Point(atlas, (int)(atlas.width / ratio), (int)(atlas.height / ratio));
					    ApplyRatio(changeInRatio);
					}
					
					coordinates.Add(uv);
					Texture2D.DestroyImmediate(scaledTexture);
					scaledTexture = null;
				}
				
				System.GC.collect();
			}
			
			atlas.Apply();
		}
		
    /*public void AddTextures(params Texture2D[] textures)
    {
        int textureCount = 0;
        foreach (Texture2D texture in textures)
        {
            if (texture != null)
            {
                textureCount++;

                Rect uv = new Rect();
                //coordinates.Add(uv);

                Texture2D scaledTexture = new Texture2D(texture.width, texture.height);
                scaledTexture.SetPixels(texture.GetPixels());
                TextureScale.Point(scaledTexture, ((int)(scaledTexture.width / ratio)), ((int)(scaledTexture.height / ratio)));

                Vector2 emptySpacePosition = FindEmptySpaceFor(scaledTexture.width, scaledTexture.height);

                if (emptySpacePosition.x >= 0 && emptySpacePosition.y >= 0)
                {
                    atlas.SetPixels((int) emptySpacePosition.x, (int) emptySpacePosition.y, scaledTexture.width, scaledTexture.height, scaledTexture.GetPixels());
                    uv.x = emptySpacePosition.x * ratio;
                    uv.y = emptySpacePosition.y * ratio;
                    uv.width = scaledTexture.width * ratio;
                    uv.height = scaledTexture.height * ratio;
                    //atlas.Apply();
                }
                else
                {
                    Color[] oldAtlas = atlas.GetPixels();
                    int oldWidth = atlas.width, oldHeight = atlas.height;
                    if (atlas.width <= atlas.height) //Make Width Bigger
                    {
                        if (scaledTexture.height <= atlas.height) //If the height of the new texture already fits in current atlas, only resize width
                        {
                            atlas.Resize(atlas.width + scaledTexture.width, atlas.height);
                        }
                        else //Else resize both width and height
                        {
                            atlas.Resize(atlas.width + scaledTexture.width, atlas.height + scaledTexture.height);
                        }

                        atlas.SetPixels(0, 0, oldWidth, oldHeight, oldAtlas);
                        atlas.SetPixels(atlas.width - scaledTexture.width, 0, scaledTexture.width, scaledTexture.height, scaledTexture.GetPixels()); //Add new texture on the right side of the atlas
                        uv.x = (atlas.width - scaledTexture.width) * ratio;
                        uv.y = 0 * ratio;
                        uv.width = scaledTexture.width * ratio;
                        uv.height = scaledTexture.height * ratio;
                        //atlas.Apply();

                        Color[] emptySpace = new Color[(atlas.height - scaledTexture.height) * scaledTexture.width];
                        //Add empty space to extra areas created above new texture
                        for (int row = 0; row < atlas.height - scaledTexture.height; row++)
                        {
                            for (int col = 0; col < scaledTexture.width; col++)
                            {
                                //atlas.SetPixel(col, row, emptySpaceColor);
                                emptySpace[row * (scaledTexture.width) + col] = emptySpaceColor;
                            }
                        }
                        atlas.SetPixels(atlas.width - scaledTexture.width, scaledTexture.height, scaledTexture.width, atlas.height - scaledTexture.height, emptySpace);
                        //atlas.Apply();
                    }
                    else //Make Height Bigger
                    {
                        if (scaledTexture.width <= atlas.width) //If the width of the new texture already fits in current atlas, only resize height
                        {
                            atlas.Resize(atlas.width, atlas.height + scaledTexture.height);
                        }
                        else //Else resize both width and height
                        {
                            atlas.Resize(atlas.width + scaledTexture.width, atlas.height + scaledTexture.height);
                        }

                        atlas.SetPixels(0, 0, oldWidth, oldHeight, oldAtlas);
                        atlas.SetPixels(0, atlas.height - scaledTexture.height, scaledTexture.width, scaledTexture.height, scaledTexture.GetPixels()); //Add new texture on the top side of the atlas
                        uv.x = 0 * ratio;
                        uv.y = (atlas.height - scaledTexture.height) * ratio;
                        uv.width = scaledTexture.width * ratio;
                        uv.height = scaledTexture.height * ratio;
                        //atlas.Apply();

                        Color[] emptySpace = new Color[scaledTexture.height * (atlas.width - scaledTexture.width)];
                        //Add empty space to extra areas created to the right of new texture
                        for (int row = 0; row < scaledTexture.height; row++)
                        {
                            for (int col = 0; col < atlas.width - scaledTexture.width; col++)
                            {
                                //atlas.SetPixel(col, row, emptySpaceColor);
                                emptySpace[(row * (atlas.width - scaledTexture.width)) + col] = emptySpaceColor;
                            }
                        }
                        atlas.SetPixels(scaledTexture.width, atlas.height - scaledTexture.height, atlas.width - scaledTexture.width, scaledTexture.height, emptySpace);
                        //atlas.Apply();
                    }
                }

                if (atlas.width > maxSize || atlas.height > maxSize)
                {
                    //Scale down atlas
                    ratio = ((float) Mathf.Max(atlas.width, atlas.height)) / maxSize;
                    TextureScale.Point(atlas, (int)(atlas.width / ratio), (int)(atlas.height / ratio));
                }

                //Resources.UnloadAsset(scaledTexture);
                coordinates.Add(uv);
                Texture2D.DestroyImmediate(scaledTexture);
                scaledTexture = null;
            }

            //Resources.UnloadUnusedAssets();
            System.GC.Collect();

            //if (textureCount >= 10) break;
        }
        //Color[] colorBlock = new Color[16];
        //for (int i = 0; i < colorBlock.Length; i++) colorBlock[i] = Color.blue;
        //atlas.SetPixels(0, 0, 4, 4, colorBlock);

        ApplyRatio();
        atlas.Apply();
    }*/

    /*public Vector2 FindEmptySpaceFor(int requiredWidth, int requiredHeight)
    {
        int searchPosX = 0, searchPosY = 0;
        bool possibleLocation = false, foundSpace = false;
        for (int row = 0; row < atlas.height; row++)
        {
            for (int col = 0; col < atlas.width; col++)
            {
                if (ColorDiff(atlas.GetPixel(col, row), emptySpaceColor) <= 0.1f)
                    possibleLocation = true;
                else if(possibleLocation)
                {
                    possibleLocation = false;
                    searchPosX = col;
                    searchPosY = row;
                }

                if (col - searchPosX + 1 >= requiredWidth && row - searchPosY + 1 >= requiredHeight)
                {
                    foundSpace = true;
                    break;
                }
                if(!possibleLocation) searchPosX++;
            }

            if (foundSpace) break;
            if (!possibleLocation)
            {
                searchPosX = 0;
                searchPosY++;
            }
        }

        if (foundSpace) return new Vector2(searchPosX, searchPosY);
        else return new Vector2(-1, -1);
    }*/
    /*public Vector2 FindEmptySpaceFor(int requiredWidth, int requiredHeight)
    {
        if (atlas.width >= requiredWidth && atlas.height >= requiredHeight)
        {
            int flattenedSpaceNeeded = requiredWidth * requiredHeight;
            Color[] flattenedAtlas = atlas.GetPixels();

            int nextGreenIndex = -1;
            for (int i = 0; i < flattenedAtlas.Length; i++)
            {
                if (ColorDiff(flattenedAtlas[i], emptySpaceColor) < 0.1f)
                {
                    if (nextGreenIndex < 0) nextGreenIndex = i;
                    int searchY = (i - nextGreenIndex) / requiredWidth, searchX = ((i - nextGreenIndex) % requiredWidth) - (searchY * requiredWidth);
                    if (searchY >= requiredHeight && searchX >= requiredWidth)
                    {
                        return new Vector2(nextGreenIndex % atlas.width, nextGreenIndex / atlas.width);
                    }
                    else if (searchX >= requiredWidth)
                    {
                        i += atlas.width - requiredWidth;
                    }
                }
                else if (nextGreenIndex > -1)
                {
                    i = nextGreenIndex + requiredWidth;
                    nextGreenIndex = -1;
                }
            }
        }

        return new Vector2(-1, -1);
    }*/
    /*public Vector2 FindEmptySpaceFor(int requiredWidth, int requiredHeight)
    {
        int currentPosition = 0, flattenedSpaceNeeded = requiredWidth * requiredHeight;
        List<Color> flattenedAtlas = new List<Color>(atlas.GetPixels());

        int nextGreenIndex = -1;
        do
        {
            nextGreenIndex = flattenedAtlas.IndexOf(emptySpaceColor);
            if (nextGreenIndex > -1)
            {
                flattenedAtlas.RemoveRange(0, nextGreenIndex);
                currentPosition += nextGreenIndex;

                bool itFits = true;
                for (int i = 0; i < flattenedSpaceNeeded; i++)
                {
                    if (i < flattenedAtlas.Count)
                    {
                        if (ColorDiff(flattenedAtlas[i], emptySpaceColor) >= 0.1f)
                        {
                            itFits = false;
                            break;
                        }
                    }
                    else
                    {
                        itFits = false;
                        break;
                    }
                }

                if (itFits) return new Vector2(currentPosition % atlas.width, currentPosition / atlas.width);
            }
        } while (nextGreenIndex > -1);

        return new Vector2(-1, -1);
    }*/
    /*public Vector2 FindEmptySpaceFor(int requiredWidth, int requiredHeight)
    {
        Color[] flattenedAtlas = atlas.GetPixels();

        for (int row = 0; row < atlas.height; row++)
        {
            for (int col = 0; col < atlas.width; col++)
            {
                int flatSearchIndex = (row * atlas.width) + col;

                //if (ColorDiff(flattenedAtlas[flatSearchIndex], emptySpaceColor) < 0.1f)
                //{
                bool fivePointCheck = true;
                #region Check Corners and Center
                int bottomLeftCorner = flatSearchIndex;
                int bottomRightCornerFlatIndex = flatSearchIndex + requiredWidth - 0;
                int topLeftCornerFlatIndex = flatSearchIndex + ((atlas.width - 0) * (requiredHeight - 0));
                int topRightCornerFlatIndex = flatSearchIndex + ((atlas.width - 0) * (requiredHeight - 0)) + (requiredWidth - 0);
                int centerFlatIndex = flatSearchIndex + ((atlas.width - 0) * ((requiredHeight - 0) / 2)) + ((requiredWidth - 0) / 2);

                if (ColorDiff(flattenedAtlas[bottomLeftCorner], emptySpaceColor) >= 0.1f) fivePointCheck = false;
                if (bottomRightCornerFlatIndex >= flattenedAtlas.Length || ColorDiff(flattenedAtlas[bottomRightCornerFlatIndex], emptySpaceColor) >= 0.1f) fivePointCheck = false;
                if (topLeftCornerFlatIndex >= flattenedAtlas.Length || ColorDiff(flattenedAtlas[topLeftCornerFlatIndex], emptySpaceColor) >= 0.1f) fivePointCheck = false;
                if (topRightCornerFlatIndex >= flattenedAtlas.Length || ColorDiff(flattenedAtlas[topRightCornerFlatIndex], emptySpaceColor) >= 0.1f) fivePointCheck = false;
                if (centerFlatIndex >= flattenedAtlas.Length || ColorDiff(flattenedAtlas[centerFlatIndex], emptySpaceColor) >= 0.1f) fivePointCheck = false;
                #endregion

                if (fivePointCheck)
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
                //}
            }
        }
        return new Vector2(-1, -1);
    }*/
    public Vector2 FindEmptySpaceFor(int requiredWidth, int requiredHeight)
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
            
            col = 0;
        }
        return new Vector2(-1, -1);
    }
    
    public static bool FivePointCheck(Color[] flattenedImage, int imageWidth, int requiredWidth, int requiredHeight, int bottomLeftFlatIndex)
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

    public void ApplyRatio(float r)
    {
        for (int i = 0; i < coordinates.Count; i++)
        {
            Rect uv = new Rect();
            uv.x = coordinates[i].x / r;
            uv.y = coordinates[i].y / r;
            uv.width = coordinates[i].width / r;
            uv.height = coordinates[i].height / r;
            coordinates[i] = uv;
        }
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