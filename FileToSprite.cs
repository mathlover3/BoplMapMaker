using UnityEngine;
using System.Collections;
using System.IO;
// https://forum.unity.com/threads/generating-sprites-dynamically-from-png-or-jpeg-files-in-c.343735/
public class IMG2Sprite : MonoBehaviour
{
    //https://forum.unity.com/threads/generating-sprites-dynamically-from-png-or-jpeg-files-in-c.343735/
    // This script loads a PNG or JPEG image from disk and returns it as a Sprite
    // Drop it on any GameObject/Camera in your scene (singleton implementation)
    //
    // Usage from any other script:
    // MySprite = IMG2Sprite.instance.LoadNewSprite(FilePath, [PixelsPerUnit (optional)])

    private static IMG2Sprite _instance;

    public static IMG2Sprite instance
    {
        get
        {
            //If _instance hasn't been set yet, we grab it from the scene!
            //This will only happen the first time this reference is used.

            if (_instance == null)
                _instance = GameObject.FindObjectOfType<IMG2Sprite>();
            return _instance;
        }
    }

    public static Sprite LoadNewSprite(byte[] FileData, float PixelsPerUnit = 100.0f)
    {

        // Load a PNG or JPG image from disk to a Texture2D, assign this texture to a new sprite and return its reference

        Sprite NewSprite = new Sprite();
        Texture2D SpriteTexture = LoadTexture(FileData);
        var rect = new Rect(0, 0, SpriteTexture.width, SpriteTexture.height);
        // Assuming your sprite size is 100x100 pixels
        float pivotX = 480f / (float)SpriteTexture.width; // Convert pixel coordinate to Unity units
        float pivotY = 360f / (float)SpriteTexture.height; // Convert pixel coordinate to Unity units

        // Create the sprite with the correct pivot point
        Vector2 vector = new Vector2(pivotX, pivotY);

        Debug.Log($"vector is {vector}");
        NewSprite = Sprite.Create(SpriteTexture, rect, vector, PixelsPerUnit);

        return NewSprite;
    }

    public static Texture2D LoadTexture(byte[] FileData)
    {

        // Load a PNG or JPG file from disk to a Texture2D
        // Returns null if load fails

        Texture2D Tex2D;
        Tex2D = new Texture2D(2, 2);           // Create new "empty" texture
        if (Tex2D.LoadImage(FileData))           // Load the imagedata into the texture (size is set automatically)
            return Tex2D;                 // If data = readable -> return texture
        return null;                     // Return null if load failed
    }
}
