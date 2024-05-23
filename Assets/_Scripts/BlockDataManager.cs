using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class BlockDataManager : MonoBehaviour
{
    public static float textureOffset = 0;
    public static float tileSizeX, tileSizeY;
    public static BlockTypeData[] blockTypeDataDictionary;
    public BlockDataSO textureData;
    public Texture2D atlasTexture;
    public static BlockDataManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Move the resource loading here
            atlasTexture = Resources.Load<Texture2D>("Sprites/sprites");
            if (atlasTexture == null)
            {
                Debug.LogError("Failed to load atlas texture from Resources.");
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        OnValidate();
    }

    public void OnValidate()
    {
        if (textureData == null)
        {
            Debug.LogError("TextureData is not assigned.");
            return;
        }

        blockTypeDataDictionary = new BlockTypeData[textureData.textureDataList.Max(t => (int)t.blockType + 1)];
        foreach (var item in textureData.textureDataList)
        {
            blockTypeDataDictionary[(int)item.blockType] = item;
        }
        tileSizeX = textureData.textureSizeX;
        tileSizeY = textureData.textureSizeY;
    }

    public List<Sprite> GenerateSpritesFromTextureData(TextureData textureData, int gridSize)
    {
        List<Sprite> sprites = new List<Sprite>();
        if (atlasTexture == null)
        {
            Debug.LogError("Atlas Texture is not assigned.");
            return sprites;
        }

        if (textureData == null)
        {
            Debug.LogError("TextureData is null.");
            return sprites;
        }

        float tileWidth = textureData.upExtends.x / gridSize;
        float tileHeight = textureData.upExtends.y / gridSize;

        for (int y = 0; y < gridSize; y++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                Rect rect = new Rect(
                    (textureData.up.x + x * tileWidth) / atlasTexture.width,
                    (textureData.up.y + y * tileHeight) / atlasTexture.height,
                    tileWidth / atlasTexture.width,
                    tileHeight / atlasTexture.height
                );

                Sprite sprite = Sprite.Create(atlasTexture, rect, new Vector2(0.5f, 0.5f), 100.0f);
                sprites.Add(sprite);
            }
        }

        return sprites;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(BlockDataManager))]
public class BlockDataManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (GUILayout.Button("Validate"))
        {
            ((BlockDataManager)target).OnValidate();
        }
    }
}
#endif
