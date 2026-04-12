using System.IO;
using KlondikeSolitaire.Core;
using KlondikeSolitaire.Views;
using UnityEditor;
using UnityEngine;

namespace KlondikeSolitaire.Editor
{
    public static class CardAtlasGenerator
    {
        private const string SourceRoot = "Assets/Art/Sprites/cards";
        private const string OutputFolder = "Assets/Art/Sprites/cards/generated";
        private const int CardWidth = 109;
        private const int CardHeight = 164;
        private const int BackStripHeightPercent = 20;
        private const int FaceStripHeightPercent = 26;
        private const int FaceCardPixelsPerUnit = 109;

        private static readonly string[] SuitNames = { "hearts", "diamonds", "clubs", "spades" };

        private static readonly string[] RankFileNames =
        {
            "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K"
        };

        private static readonly string[] RankOutputNames =
        {
            "ace", "2", "3", "4", "5", "6", "7", "8", "9", "10", "jack", "queen", "king"
        };

        private static readonly Color RedSuitColor = new(0.7f, 0.1f, 0.1f, 1f);
        private static readonly Color BlackSuitColor = new(0.1f, 0.1f, 0.1f, 1f);

        [MenuItem("Tools/Klondike/Generate Card Atlas")]
        public static void Generate()
        {
            EditorUtility.DisplayProgressBar("Card Atlas Generator", "Loading source textures...", 0f);

            try
            {
                GenerateInternal();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static void GenerateInternal()
        {
            Texture2D cardFront = LoadTexture($"{SourceRoot}/card_front.png");
            Texture2D cardBack = LoadTexture($"{SourceRoot}/card_back.png");

            if (cardFront == null || cardBack == null)
            {
                Debug.LogError("[CardAtlasGenerator] Missing essential source textures. Aborting.");
                return;
            }

            Texture2D[] rankTextures = LoadRankTextures();
            Texture2D[] suitTextures = LoadSuitTextures();
            Texture2D[] redFigureTextures = LoadFigureTextures("red");
            Texture2D[] blackFigureTextures = LoadFigureTextures("black");

            if (!ValidateTextures(rankTextures, suitTextures, redFigureTextures, blackFigureTextures))
            {
                return;
            }

            EnsureOutputFolder();

            string[] cardSpritePaths = new string[52];
            string[] faceStripPaths = new string[52];
            int totalCards = 52;
            int generatedCount = 0;

            for (int suitIndex = 0; suitIndex < 4; suitIndex++)
            {
                for (int rankIndex = 0; rankIndex < 13; rankIndex++)
                {
                    float progress = (float)(suitIndex * 13 + rankIndex) / totalCards;
                    string progressLabel = $"Generating card_{SuitNames[suitIndex]}_{RankOutputNames[rankIndex]}.png";
                    EditorUtility.DisplayProgressBar("Card Atlas Generator", progressLabel, progress * 0.85f);

                    bool isRed = suitIndex == 0 || suitIndex == 1;
                    Texture2D[] figureTextures = isRed ? redFigureTextures : blackFigureTextures;

                    Texture2D generated = CompositeCard(
                        cardFront,
                        rankTextures[rankIndex],
                        suitTextures[suitIndex],
                        figureTextures,
                        rankIndex,
                        isRed
                    );

                    string outputPath = $"{OutputFolder}/card_{SuitNames[suitIndex]}_{RankOutputNames[rankIndex]}.png";
                    SaveTextureToPng(generated, outputPath);

                    string stripPath = GenerateFaceStrip(generated, suitIndex, rankIndex);
                    faceStripPaths[suitIndex * 13 + rankIndex] = stripPath;

                    Object.DestroyImmediate(generated);

                    cardSpritePaths[suitIndex * 13 + rankIndex] = outputPath;
                    generatedCount++;
                }
            }

            EditorUtility.DisplayProgressBar("Card Atlas Generator", "Generating back strip...", 0.86f);
            string backStripPath = GenerateBackStrip(cardBack);

            EditorUtility.DisplayProgressBar("Card Atlas Generator", "Refreshing AssetDatabase...", 0.90f);
            AssetDatabase.Refresh();

            EditorUtility.DisplayProgressBar("Card Atlas Generator", "Configuring import settings...", 0.93f);
            ConfigureImportSettings(cardSpritePaths, faceStripPaths, backStripPath);

            EditorUtility.DisplayProgressBar("Card Atlas Generator", "Populating CardSpriteMapping...", 0.97f);
            PopulateCardSpriteMapping(cardSpritePaths, faceStripPaths, backStripPath);

            Debug.Log($"[CardAtlasGenerator] Generated {generatedCount} face + {generatedCount} face strips + 1 back strip = {generatedCount * 2 + 1} sprites. Repack Cards.spriteatlas.");
        }

        private static Texture2D CompositeCard(
            Texture2D front,
            Texture2D rankTexture,
            Texture2D suitTexture,
            Texture2D[] figureTextures,
            int rankIndex,
            bool isRed)
        {
            Texture2D result = new Texture2D(CardWidth, CardHeight, TextureFormat.RGBA32, false);

            Color[] frontPixels = ScaleTexture(front, CardWidth, CardHeight);
            result.SetPixels(frontPixels);

            bool isFigure = rankIndex >= 10;
            Color tintColor = isRed ? RedSuitColor : BlackSuitColor;

            if (isFigure && figureTextures[rankIndex - 10] != null)
            {
                BlitCentered(result, figureTextures[rankIndex - 10]);
            }

            if (rankTexture != null)
            {
                BlitCorner(result, rankTexture, false, tintColor);
                BlitCorner(result, rankTexture, true, tintColor);
            }

            if (suitTexture != null)
            {
                BlitSuitCorner(result, suitTexture, false);
                BlitSuitCorner(result, suitTexture, true);
            }

            result.Apply();
            return result;
        }

        private static Color[] ScaleTexture(Texture2D source, int targetWidth, int targetHeight)
        {
            Color[] result = new Color[targetWidth * targetHeight];
            float scaleX = (float)source.width / targetWidth;
            float scaleY = (float)source.height / targetHeight;

            for (int y = 0; y < targetHeight; y++)
            {
                for (int x = 0; x < targetWidth; x++)
                {
                    float sourceX = x * scaleX;
                    float sourceY = y * scaleY;
                    result[y * targetWidth + x] = source.GetPixelBilinear(sourceX / source.width, sourceY / source.height);
                }
            }

            return result;
        }

        private static void BlitCentered(Texture2D target, Texture2D source)
        {
            int targetWidth = target.width;
            int targetHeight = target.height;

            int figureWidth = Mathf.RoundToInt(targetWidth * 0.6f);
            int figureHeight = Mathf.RoundToInt(targetHeight * 0.6f);
            int offsetX = (targetWidth - figureWidth) / 2;
            int offsetY = (targetHeight - figureHeight) / 2;

            float scaleX = (float)source.width / figureWidth;
            float scaleY = (float)source.height / figureHeight;

            for (int y = 0; y < figureHeight; y++)
            {
                for (int x = 0; x < figureWidth; x++)
                {
                    float u = (x * scaleX) / source.width;
                    float v = (y * scaleY) / source.height;
                    Color sourceColor = source.GetPixelBilinear(u, v);

                    if (sourceColor.a > 0.01f)
                    {
                        int targetX = offsetX + x;
                        int targetY = offsetY + y;
                        Color existing = target.GetPixel(targetX, targetY);
                        target.SetPixel(targetX, targetY, AlphaBlend(existing, sourceColor));
                    }
                }
            }
        }

        private static void BlitCorner(Texture2D target, Texture2D source, bool flipped, Color tintColor)
        {
            int targetWidth = target.width;
            int targetHeight = target.height;

            int rankWidth = Mathf.RoundToInt(targetWidth * 0.18f);
            int rankHeight = Mathf.RoundToInt(targetHeight * 0.10f);
            int margin = Mathf.RoundToInt(targetWidth * 0.05f);

            int startX = flipped ? targetWidth - margin - rankWidth : margin;
            int startY = flipped ? margin : targetHeight - margin - rankHeight;

            float scaleX = (float)source.width / rankWidth;
            float scaleY = (float)source.height / rankHeight;

            for (int y = 0; y < rankHeight; y++)
            {
                for (int x = 0; x < rankWidth; x++)
                {
                    int sampleX = flipped ? rankWidth - 1 - x : x;
                    int sampleY = flipped ? rankHeight - 1 - y : y;

                    float u = (sampleX * scaleX) / source.width;
                    float v = (sampleY * scaleY) / source.height;
                    Color sourceColor = source.GetPixelBilinear(u, v);

                    if (sourceColor.a > 0.01f)
                    {
                        Color tinted = new Color(tintColor.r, tintColor.g, tintColor.b, sourceColor.a);
                        Color existing = target.GetPixel(startX + x, startY + y);
                        target.SetPixel(startX + x, startY + y, AlphaBlend(existing, tinted));
                    }
                }
            }
        }

        private static void BlitSuitCorner(Texture2D target, Texture2D source, bool flipped)
        {
            int targetWidth = target.width;
            int targetHeight = target.height;

            int suitWidth = Mathf.RoundToInt(targetWidth * 0.14f);
            int suitHeight = Mathf.RoundToInt(targetHeight * 0.08f);
            int marginX = Mathf.RoundToInt(targetWidth * 0.06f);
            int rankRegionHeight = Mathf.RoundToInt(targetHeight * 0.10f);
            int rankMarginY = Mathf.RoundToInt(targetWidth * 0.05f);

            int startX = flipped ? targetWidth - marginX - suitWidth : marginX;
            int suitOffsetFromEdge = rankMarginY + rankRegionHeight + Mathf.RoundToInt(targetHeight * 0.01f);
            int startY = flipped
                ? suitOffsetFromEdge
                : targetHeight - suitOffsetFromEdge - suitHeight;

            float scaleX = (float)source.width / suitWidth;
            float scaleY = (float)source.height / suitHeight;

            for (int y = 0; y < suitHeight; y++)
            {
                for (int x = 0; x < suitWidth; x++)
                {
                    int sampleX = flipped ? suitWidth - 1 - x : x;
                    int sampleY = flipped ? suitHeight - 1 - y : y;

                    float u = (sampleX * scaleX) / source.width;
                    float v = (sampleY * scaleY) / source.height;
                    Color sourceColor = source.GetPixelBilinear(u, v);

                    if (sourceColor.a > 0.01f)
                    {
                        Color existing = target.GetPixel(startX + x, startY + y);
                        target.SetPixel(startX + x, startY + y, AlphaBlend(existing, sourceColor));
                    }
                }
            }
        }

        private static Color AlphaBlend(Color background, Color foreground)
        {
            float alpha = foreground.a;
            return new Color(
                background.r * (1f - alpha) + foreground.r * alpha,
                background.g * (1f - alpha) + foreground.g * alpha,
                background.b * (1f - alpha) + foreground.b * alpha,
                Mathf.Max(background.a, foreground.a)
            );
        }

        private static string GenerateFaceStrip(Texture2D faceTexture, int suitIndex, int rankIndex)
        {
            int stripHeight = Mathf.RoundToInt(faceTexture.height * FaceStripHeightPercent / 100f);
            int sourceOffsetY = faceTexture.height - stripHeight;

            Texture2D strip = new Texture2D(faceTexture.width, stripHeight, TextureFormat.RGBA32, false);
            Color[] stripPixels = faceTexture.GetPixels(0, sourceOffsetY, faceTexture.width, stripHeight);
            strip.SetPixels(stripPixels);
            strip.Apply();

            string outputPath = $"{OutputFolder}/card_{SuitNames[suitIndex]}_{RankOutputNames[rankIndex]}_strip.png";
            SaveTextureToPng(strip, outputPath);
            Object.DestroyImmediate(strip);
            return outputPath;
        }

        private static string GenerateBackStrip(Texture2D cardBack)
        {
            Texture2D scaled = new Texture2D(CardWidth, CardHeight, TextureFormat.RGBA32, false);
            Color[] scaledPixels = ScaleTexture(cardBack, CardWidth, CardHeight);
            scaled.SetPixels(scaledPixels);
            scaled.Apply();

            int stripHeight = Mathf.RoundToInt(CardHeight * BackStripHeightPercent / 100f);
            int sourceOffsetY = CardHeight - stripHeight;

            Texture2D strip = new Texture2D(CardWidth, stripHeight, TextureFormat.RGBA32, false);
            Color[] stripPixels = scaled.GetPixels(0, sourceOffsetY, CardWidth, stripHeight);
            strip.SetPixels(stripPixels);
            strip.Apply();

            Object.DestroyImmediate(scaled);

            string outputPath = $"{OutputFolder}/card_back_strip.png";
            SaveTextureToPng(strip, outputPath);
            Object.DestroyImmediate(strip);
            return outputPath;
        }

        private static void ConfigureImportSettings(string[] faceCardPaths, string[] faceStripPaths, string backStripPath)
        {
            for (int spriteIndex = 0; spriteIndex < faceCardPaths.Length; spriteIndex++)
            {
                ConfigureSingleSpriteImporter(faceCardPaths[spriteIndex]);
            }

            for (int spriteIndex = 0; spriteIndex < faceStripPaths.Length; spriteIndex++)
            {
                ConfigureSingleSpriteImporter(faceStripPaths[spriteIndex]);
            }

            ConfigureSingleSpriteImporter(backStripPath);
        }

        private static void ConfigureSingleSpriteImporter(string assetPath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            if (importer.textureType == TextureImporterType.Sprite
                && Mathf.RoundToInt(importer.spritePixelsPerUnit) == FaceCardPixelsPerUnit)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = FaceCardPixelsPerUnit;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.Tight;
            settings.spriteExtrude = 1;
            importer.SetTextureSettings(settings);

            importer.SaveAndReimport();
        }

        private static void PopulateCardSpriteMapping(string[] cardSpritePaths, string[] faceStripPaths, string backStripPath)
        {
            string[] guids = AssetDatabase.FindAssets("t:CardSpriteMapping");
            if (guids.Length == 0)
            {
                Debug.LogError("[CardAtlasGenerator] No CardSpriteMapping ScriptableObject found in project. Create one first.");
                return;
            }

            string mappingPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            CardSpriteMapping mapping = AssetDatabase.LoadAssetAtPath<CardSpriteMapping>(mappingPath);

            if (mapping == null)
            {
                Debug.LogError($"[CardAtlasGenerator] Failed to load CardSpriteMapping at {mappingPath}");
                return;
            }

            SerializedObject serializedMapping = new SerializedObject(mapping);

            SerializedProperty faceSpritesProperty = serializedMapping.FindProperty("_faceSprites");
            faceSpritesProperty.arraySize = 52;

            SerializedProperty faceStripSpritesProperty = serializedMapping.FindProperty("_faceStripSprites");
            faceStripSpritesProperty.arraySize = 52;

            for (int spriteIndex = 0; spriteIndex < 52; spriteIndex++)
            {
                Sprite faceSprite = AssetDatabase.LoadAssetAtPath<Sprite>(cardSpritePaths[spriteIndex]);
                if (faceSprite == null)
                {
                    Debug.LogWarning($"[CardAtlasGenerator] Could not load sprite at {cardSpritePaths[spriteIndex]}");
                }
                faceSpritesProperty.GetArrayElementAtIndex(spriteIndex).objectReferenceValue = faceSprite;

                Sprite faceStripSprite = AssetDatabase.LoadAssetAtPath<Sprite>(faceStripPaths[spriteIndex]);
                if (faceStripSprite == null)
                {
                    Debug.LogWarning($"[CardAtlasGenerator] Could not load face strip sprite at {faceStripPaths[spriteIndex]}");
                }
                faceStripSpritesProperty.GetArrayElementAtIndex(spriteIndex).objectReferenceValue = faceStripSprite;
            }

            SerializedProperty backSpriteProperty = serializedMapping.FindProperty("_backSprite");
            backSpriteProperty.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>($"{SourceRoot}/card_back.png");

            SerializedProperty backStripSpriteProperty = serializedMapping.FindProperty("_backStripSprite");
            backStripSpriteProperty.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(backStripPath);

            SerializedProperty baseSpriteProperty = serializedMapping.FindProperty("_baseSprite");
            baseSpriteProperty.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>($"{SourceRoot}/cart_bottom.png");

            serializedMapping.ApplyModifiedProperties();
            EditorUtility.SetDirty(mapping);
            AssetDatabase.SaveAssets();

            Debug.Log($"[CardAtlasGenerator] CardSpriteMapping populated at {mappingPath}");
        }

        private static void SaveTextureToPng(Texture2D texture, string assetPath)
        {
            byte[] pngData = texture.EncodeToPNG();
            string fullPath = Path.Combine(Application.dataPath, "../", assetPath);
            string directory = Path.GetDirectoryName(fullPath);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(fullPath, pngData);
        }

        private static void EnsureOutputFolder()
        {
            string fullPath = Path.Combine(Application.dataPath, "../", OutputFolder);
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }
        }

        private static Texture2D LoadTexture(string assetPath)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (texture == null)
            {
                Debug.LogWarning($"[CardAtlasGenerator] Could not load texture: {assetPath}");
                return null;
            }

            if (!texture.isReadable)
            {
                Debug.LogWarning($"[CardAtlasGenerator] Texture is not readable (Read/Write must be enabled in import settings): {assetPath}");
            }

            return texture;
        }

        private static Texture2D[] LoadRankTextures()
        {
            Texture2D[] textures = new Texture2D[13];
            for (int rankIndex = 0; rankIndex < 13; rankIndex++)
            {
                textures[rankIndex] = LoadTexture($"{SourceRoot}/card numbers/new/{RankFileNames[rankIndex]}.png");
            }
            return textures;
        }

        private static Texture2D[] LoadSuitTextures()
        {
            Texture2D[] textures = new Texture2D[4];
            textures[0] = LoadTexture($"{SourceRoot}/semi/new/hearts.png");
            textures[1] = LoadTexture($"{SourceRoot}/semi/new/diamonds.png");
            textures[2] = LoadTexture($"{SourceRoot}/semi/new/flowers.png");
            textures[3] = LoadTexture($"{SourceRoot}/semi/new/spades.png");
            return textures;
        }

        private static Texture2D[] LoadFigureTextures(string color)
        {
            Texture2D[] textures = new Texture2D[3];
            textures[0] = LoadTexture($"{SourceRoot}/figures/{color}/jack.png");
            textures[1] = LoadTexture($"{SourceRoot}/figures/{color}/queen.png");
            textures[2] = LoadTexture($"{SourceRoot}/figures/{color}/re.png");
            return textures;
        }

        private static bool ValidateTextures(
            Texture2D[] ranks,
            Texture2D[] suits,
            Texture2D[] redFigures,
            Texture2D[] blackFigures)
        {
            bool valid = true;

            for (int rankIndex = 0; rankIndex < ranks.Length; rankIndex++)
            {
                if (ranks[rankIndex] == null)
                {
                    Debug.LogError($"[CardAtlasGenerator] Missing rank texture for: {RankFileNames[rankIndex]}");
                    valid = false;
                }
            }

            string[] suitFileNames = { "hearts", "diamonds", "flowers", "spades" };
            for (int suitIndex = 0; suitIndex < suits.Length; suitIndex++)
            {
                if (suits[suitIndex] == null)
                {
                    Debug.LogError($"[CardAtlasGenerator] Missing suit texture for: {suitFileNames[suitIndex]}");
                    valid = false;
                }
            }

            string[] figureFileNames = { "jack", "queen", "re" };
            for (int figureIndex = 0; figureIndex < redFigures.Length; figureIndex++)
            {
                if (redFigures[figureIndex] == null)
                {
                    Debug.LogError($"[CardAtlasGenerator] Missing red figure texture for: {figureFileNames[figureIndex]}");
                    valid = false;
                }
                if (blackFigures[figureIndex] == null)
                {
                    Debug.LogError($"[CardAtlasGenerator] Missing black figure texture for: {figureFileNames[figureIndex]}");
                    valid = false;
                }
            }

            return valid;
        }
    }
}
