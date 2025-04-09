using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Linq;

public class ColoringManager : MonoBehaviour
{
    public SpriteRenderer targetSprite;
    public Color currentColor = Color.red;
    public DrawingMode currentMode = DrawingMode.Draw;

    public float drawBrushSize = 2f;
    public float paintBrushSize = 5f;
    public float crayonBrushSize = 8f; 
    public float outlineThreshold = 0.2f;
    private Texture2D crayonTexture; 

    private Texture2D coloringTexture;
    private Vector2? lastDrawPosition;
    private Color[] originalPixels;
    private bool isTextureInitialized = false;
    private bool hasChanges = false;
    // Cache for common values used in calculations
    private float textureWidth;
    private float textureHeight;
    private Vector2 spritePivot;
    private Vector2 spriteSize;
    private bool needsApply;
    private Color[] currentState;
    private Color[] originalState;
    private Stack<Color[]> undoStack = new Stack<Color[]>();    
    
    // Optimization: Pre-compute and cache pixel offsets for brushes
    private List<Vector2Int>[] brushOffsetCache;
    private int maxBrushSize = 20; // Adjust based on your max expected brush size
    
    // Optimization: Buffer for storing modified pixels
    private List<Vector2Int> modifiedPixels = new List<Vector2Int>(1000);
    private bool isModifying = false;
    
    // Optimization: Frame limiting for texture apply
    private int frameCounter = 0;
    private const int APPLY_FREQUENCY = 2; // Apply texture changes every 2 frames

    private const int MAX_UNDO_STEPS = 10;
    private bool isUndoing = false;

    public enum DrawingMode
    {
        Hand,
        Draw,      // Vẽ
        Crayons,   // Bút màu 
        Brush      // Cọ vẽ
    }
    private void Awake()
{
    // Thiết lập Singleton
    if (Instance == null)
    {
        Instance = this;
    }
    else if (Instance != this)
    {
        Destroy(gameObject);
        return;
    }
    
    // Tự động tìm SaveManager nếu chưa được gán
    if (saveManager == null)
    {
        saveManager = FindObjectOfType<SaveManager>();
        if (saveManager == null)
        {
            Debug.LogWarning("Không tìm thấy SaveManager trong scene! Sẽ lưu ảnh mà không cập nhật tiến độ.");
        }
    }
}

    void Start()
{
    InitializeTexture();
    PrecomputeBrushOffsets();
    InitializeCrayonTexture();
    
    // Get the image ID from PlayerPrefs
    string savedImageId = PlayerPrefs.GetString("SelectedImageId", "");
    Debug.Log($"ColoringManager.Start: ID from PlayerPrefs is: {savedImageId}");
    
    if (!string.IsNullOrEmpty(savedImageId))
    {
        // Use the ID from PlayerPrefs
        imageId = savedImageId;
        Debug.Log($"Using ID from PlayerPrefs: {imageId}");
    }
    else if (string.IsNullOrEmpty(imageId))
    {
        Debug.LogError("No image ID from PlayerPrefs or Inspector!");
        return;
    }
    
    // Find SaveManager if not assigned
    if (saveManager == null)
    {
        saveManager = FindObjectOfType<SaveManager>();
        if (saveManager == null)
        {
            Debug.LogError("SaveManager not found!");
        }
    }
    
    // Load the image using the ID
    LoadSavedImage();
}

    private void LoadSavedImage()
{
    Debug.Log($"LoadSavedImage: Đang tìm hình ảnh với ID: {imageId}");
    
    if (string.IsNullOrEmpty(imageId))
    {
        Debug.LogError("ImageId trống, không thể tải hình ảnh!");
        return;
    }
    
    if (saveManager == null)
    {
        saveManager = FindObjectOfType<SaveManager>();
        if (saveManager == null)
        {
            Debug.LogError("Không tìm thấy SaveManager!");
            return;
        }
    }
    
    // Tải hình ảnh đã lưu nếu có
    Texture2D savedTexture = saveManager.LoadSavedImage(imageId);
    
    if (savedTexture != null)
    {
        // Đã tìm thấy hình ảnh đã lưu, sử dụng nó
        Debug.Log($"Đã tìm thấy và tải hình ảnh đã lưu cho ID: {imageId}");
        coloringTexture.SetPixels(savedTexture.GetPixels());
        coloringTexture.Apply();
        
        // Lưu bản gốc để reset
        if (originalPixels == null)
        {
            originalPixels = savedTexture.GetPixels();
            originalState = originalPixels;
        }
        
        hasChanges = true;
    }
    else
    {
        Debug.Log($"Không tìm thấy texture đã lưu cho id {imageId}, sử dụng texture mặc định");
    }
    if (undoStack.Count == 0)
    {
        Color[] pixels = coloringTexture.GetPixels();
        Color[] pixelsCopy = new Color[pixels.Length];
        System.Array.Copy(pixels, pixelsCopy, pixels.Length);
        undoStack.Push(pixelsCopy);
    }
}
    private void PrecomputeBrushOffsets()
    {
        // Pre-compute pixel offsets for circular brushes of various sizes
        brushOffsetCache = new List<Vector2Int>[maxBrushSize + 1];
        
        for (int radius = 1; radius <= maxBrushSize; radius++)
        {
            brushOffsetCache[radius] = new List<Vector2Int>();
            int radiusSquared = radius * radius;
            
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    int distSquared = x * x + y * y;
                    if (distSquared <= radiusSquared)
                    {
                        brushOffsetCache[radius].Add(new Vector2Int(x, y));
                    }
                }
            }
        }
    }

    private void InitializeTexture()
    {
        if (targetSprite == null)
        {
            Debug.LogError("Target Sprite is not assigned!");
            return;
        }

        Sprite sprite = targetSprite.sprite;
        if (sprite == null || sprite.texture == null)
        {
            Debug.LogError("Invalid sprite or texture!");
            return;
        }

        try
        {
            if (!sprite.texture.isReadable)
            {
                Debug.LogError("Source texture is not readable! Please enable Read/Write in texture import settings.");
                return;
            }

            // Create and initialize texture
            int width = (int)sprite.rect.width;
            int height = (int)sprite.rect.height;
            coloringTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            coloringTexture.filterMode = FilterMode.Point;
            
            var pixels = sprite.texture.GetPixels(
                (int)sprite.textureRect.x,
                (int)sprite.textureRect.y,
                (int)sprite.textureRect.width,
                (int)sprite.textureRect.height
            );
            
            coloringTexture.SetPixels(pixels);
            coloringTexture.Apply();
            
            targetSprite.sprite = Sprite.Create(
                coloringTexture,
                new Rect(0, 0, width, height),
                new Vector2(0.5f, 0.5f)
            );
            
            // Cache original pixels for reset functionality
            originalPixels = pixels;
            
            // Cache common values
            textureWidth = coloringTexture.width;
            textureHeight = coloringTexture.height;
            spriteSize = targetSprite.sprite.bounds.size;
            spritePivot = targetSprite.sprite.pivot / new Vector2(width, height);
            
            isTextureInitialized = true;
        }
        
        catch (System.Exception e)
        {
            Debug.LogError($"Error creating coloring texture: {e.Message}");
        }
        originalState = coloringTexture.GetPixels();
        originalPixels = originalState;
    }
    
    void Update()
    {
        if (!isTextureInitialized)
            return;
            
        // Handle input and drawing
        HandleDrawInput();
        
        // Apply texture changes periodically instead of every frame
        frameCounter++;
        if (needsApply && (frameCounter % APPLY_FREQUENCY == 0 || !isModifying))
        {
            coloringTexture.Apply();
            needsApply = false;
            frameCounter = 0;
        }
    }
    
    void HandleDrawInput()
{
    if (Input.GetMouseButtonDown(0))
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if (Input.GetMouseButtonDown(0))
        {
            SaveCurrentState();
        }
        isModifying = true;
        lastDrawPosition = null;
        modifiedPixels.Clear();
    }
    else if (Input.GetMouseButtonUp(0))
    {
        isModifying = false;
        lastDrawPosition = null;
        isUndoing = false;
        
        if (needsApply)
        {
            coloringTexture.Apply();
            needsApply = false;
        }
        return;
    }
    
    if (!isModifying || !Input.GetMouseButton(0))
        return;
        
    Vector2 currentMousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    if (!targetSprite.bounds.Contains(currentMousePos)) 
    {
        return;
    }
    
    float currentBrushSize = GetCurrentBrushSize();
    switch (currentMode)
    {
        case DrawingMode.Draw:
            DrawAtPosition(currentMousePos, currentColor, currentBrushSize);
            break;
        case DrawingMode.Crayons:
            float variableBrushSize = crayonBrushSize * (0.9f + 0.2f * Mathf.PerlinNoise(Time.time * 0.5f, 0));
            DrawCrayonAtPosition(currentMousePos, currentColor, variableBrushSize);
            break;
        case DrawingMode.Brush:
            OptimizedBrushAtPosition(currentMousePos, currentColor, currentBrushSize);
            break;
    }
    hasChanges = true;
}
    private void DrawCrayonAtPosition(Vector2 pos, Color color, float brushSize)
{
    Vector2 pixelPos = WorldToTextureCoordinates(pos);
    int x = (int)pixelPos.x;
    int y = (int)pixelPos.y;

    int radius = Mathf.RoundToInt(brushSize);
    
    DrawCrayonSpot(x, y, color, radius);

    if (lastDrawPosition.HasValue)
    {
        Vector2 lastPixelPos = WorldToTextureCoordinates(lastDrawPosition.Value);
        int lastX = (int)lastPixelPos.x;
        int lastY = (int)lastPixelPos.y;
        
        DrawCrayonLineSegment(lastX, lastY, x, y, color, radius);
    }

    lastDrawPosition = pos;
    
    needsApply = true;
}

    private void DrawCrayonLineSegment(int x0, int y0, int x1, int y1, Color color, int radius)
{
    int dx = Mathf.Abs(x1 - x0);
    int dy = Mathf.Abs(y1 - y0);
    float stepCount = Mathf.Max(dx, dy);
    
    if (stepCount <= 0) return;
    
    int steps = Mathf.Min(Mathf.RoundToInt(stepCount * 1.2f), 20);
    float stepSize = 1f / steps;
    
    System.Random rand = new System.Random();
    
    for (float t = 0; t <= 1; t += stepSize)
    {
        float jitterX = (float)(rand.NextDouble() * 0.6 - 0.3);
        float jitterY = (float)(rand.NextDouble() * 0.6 - 0.3);
        
        int x = Mathf.RoundToInt(Mathf.Lerp(x0, x1, t) + jitterX);
        int y = Mathf.RoundToInt(Mathf.Lerp(y0, y1, t) + jitterY);
        
        float radiusVariation = 1.0f + (float)(rand.NextDouble() * 0.3 - 0.15);
        int variableRadius = Mathf.RoundToInt(radius * radiusVariation);
        variableRadius = Mathf.Clamp(variableRadius, 1, maxBrushSize);
        
        DrawCrayonSpot(x, y, color, variableRadius);
    }
}

    private void DrawCrayonSpot(int x, int y, Color color, int radius)
{
    float baseNoise = Mathf.PerlinNoise(x * 0.08f, y * 0.08f);
    
    foreach (var offset in brushOffsetCache[radius])
    {
        int currentX = x + offset.x;
        int currentY = y + offset.y;
        
        if (IsInTextureBounds(currentX, currentY))
        {
            Color existingColor = coloringTexture.GetPixel(currentX, currentY);
            if (existingColor.a > 0.1f && !IsOutlinePixel(existingColor))
            {
                float distanceFromCenter = Mathf.Sqrt(offset.x * offset.x + offset.y * offset.y) / radius;
                
                float pressure = Mathf.Pow(1 - distanceFromCenter, 1.5f);
                
                float noise1 = Mathf.PerlinNoise(currentX * 0.3f, currentY * 0.3f);
                float noise2 = Mathf.PerlinNoise(currentX * 0.5f + baseNoise, currentY * 0.5f + baseNoise);
                float noise3 = Mathf.PerlinNoise(currentX * 0.7f + 100, currentY * 0.7f + 100);
                
                float combinedNoise = noise1 * 0.5f + noise2 * 0.3f + noise3 * 0.2f;
                
                float edgeEffect = Mathf.Lerp(0.7f, 1.0f, Mathf.Pow(combinedNoise, 0.5f));
                pressure *= distanceFromCenter > 0.7f ? edgeEffect * 0.8f : edgeEffect;
                
                float alpha = pressure * (0.7f + combinedNoise * 0.3f);
                
                Color adjustedColor = new Color(
                    color.r + (combinedNoise * 0.15f - 0.075f),
                    color.g + (combinedNoise * 0.15f - 0.075f),
                    color.b + (combinedNoise * 0.15f - 0.075f),
                    color.a
                );
                
                Color newColor = Color.Lerp(existingColor, adjustedColor, alpha);
                
                coloringTexture.SetPixel(currentX, currentY, newColor);
                modifiedPixels.Add(new Vector2Int(currentX, currentY));
            }
        }
    }
}

    private float GetCurrentBrushSize()
    {
        return currentMode switch
        {
        DrawingMode.Draw => drawBrushSize,
        DrawingMode.Crayons => crayonBrushSize,
        DrawingMode.Brush => paintBrushSize,
        _ => drawBrushSize
        };
    }

    Vector2 WorldToTextureCoordinates(Vector2 worldPos)
    {
        Vector2 local = targetSprite.transform.InverseTransformPoint(worldPos);
        
        // Convert to normalized coordinates (0-1)
        float normalizedX = (local.x / spriteSize.x) + 0.5f;
        float normalizedY = (local.y / spriteSize.y) + 0.5f;
        
        // Convert to texture pixel coordinates
        int x = Mathf.RoundToInt(normalizedX * textureWidth);
        int y = Mathf.RoundToInt(normalizedY * textureHeight);
        
        return new Vector2(x, y);
    }

    void DrawAtPosition(Vector2 pos, Color color, float brushSize)
    {
        Vector2 pixelPos = WorldToTextureCoordinates(pos);
        int x = (int)pixelPos.x;
        int y = (int)pixelPos.y;

        if (lastDrawPosition.HasValue)
        {
            Vector2 lastLocal = lastDrawPosition.Value;
            int lastX = Mathf.RoundToInt(lastLocal.x * textureWidth);
            int lastY = Mathf.RoundToInt(lastLocal.y * textureHeight);
            
            OptimizedDrawLine(lastX, lastY, x, y, color, Mathf.RoundToInt(brushSize));
        }
        else
        {
            OptimizedDrawCircle(x, y, Mathf.RoundToInt(brushSize), color);
        }
    
        Vector2 normalized = new Vector2(
            pixelPos.x / textureWidth,
            pixelPos.y / textureHeight
        );
        
        lastDrawPosition = normalized;
        needsApply = true;
        hasChanges = true;
    }

    void OptimizedBrushAtPosition(Vector2 pos, Color color, float brushSize)
{
    Vector2 pixelPos = WorldToTextureCoordinates(pos);
    int x = (int)pixelPos.x;
    int y = (int)pixelPos.y;

    int radius = Mathf.Min(Mathf.RoundToInt(brushSize), maxBrushSize);
    
    foreach (var offset in brushOffsetCache[radius])
    {
        int currentX = x + offset.x;
        int currentY = y + offset.y;
        
        if (IsInTextureBounds(currentX, currentY))
        {
            Color existingColor = coloringTexture.GetPixel(currentX, currentY);
            if (existingColor.a > 0.1f && !IsOutlinePixel(existingColor))
            {
                float distSquared = offset.x * offset.x + offset.y * offset.y;
                float distance = Mathf.Sqrt(distSquared);
                float alpha = Mathf.Pow(1 - (distance / radius), 2) * 0.2f; 
                
                Color blendedColor = Color.Lerp(existingColor, color, alpha);
                coloringTexture.SetPixel(currentX, currentY, blendedColor);
                modifiedPixels.Add(new Vector2Int(currentX, currentY));
            }
        }
    }

    if (lastDrawPosition.HasValue)
    {
        Vector2 lastLocal = lastDrawPosition.Value;
        int lastX = Mathf.RoundToInt(lastLocal.x * textureWidth);
        int lastY = Mathf.RoundToInt(lastLocal.y * textureHeight);
        
        // Draw smooth line between points
        OptimizedDrawSmoothLine(lastX, lastY, x, y, color, radius);
    }
    
    Vector2 normalized = new Vector2(
        pixelPos.x / textureWidth,
        pixelPos.y / textureHeight
    );
    
    lastDrawPosition = normalized;
    needsApply = true;
}

    void OptimizedDrawSmoothLine(int x0, int y0, int x1, int y1, Color color, int radius)
{
    int dx = Mathf.Abs(x1 - x0);
    int dy = Mathf.Abs(y1 - y0);
    float stepCount = Mathf.Max(dx, dy);
    
    if (stepCount <= 0) return;
    
    int actualSteps = Mathf.Min(Mathf.RoundToInt(stepCount), 10);
    float stepSize = 1f / actualSteps;
    
    for (float t = 0; t <= 1; t += stepSize)
    {
        int x = Mathf.RoundToInt(Mathf.Lerp(x0, x1, t));
        int y = Mathf.RoundToInt(Mathf.Lerp(y0, y1, t));
        
        foreach (var offset in brushOffsetCache[radius])
        {
            int currentX = x + offset.x;
            int currentY = y + offset.y;
            
            if (IsInTextureBounds(currentX, currentY))
            {
                Color existingColor = coloringTexture.GetPixel(currentX, currentY);
                if (existingColor.a > 0.1f && !IsOutlinePixel(existingColor))
                {
                    float distSquared = offset.x * offset.x + offset.y * offset.y;
                    float distance = Mathf.Sqrt(distSquared);
                    float alpha = Mathf.Pow(1 - (distance / radius), 2) * 0.2f;
                    
                    Color blendedColor = Color.Lerp(existingColor, color, alpha);
                    coloringTexture.SetPixel(currentX, currentY, blendedColor);
                    modifiedPixels.Add(new Vector2Int(currentX, currentY));
                }
            }
        }
    }
    hasChanges = true;
}
    void OptimizedDrawLine(int x0, int y0, int x1, int y1, Color color, int radius)
    {
        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        
        if (dx <= 1 && dy <= 1)
        {
            OptimizedDrawCircle(x1, y1, radius, color);
            return;
        }
        
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;
        
        int lineLength = Mathf.Max(dx, dy);
        int skipFactor = Mathf.Max(1, lineLength / 10); 
        int counter = 0;

        while (true)
        {
            if (counter % skipFactor == 0)
            {
                OptimizedDrawCircle(x0, y0, radius, color);
            }
            
            counter++;

            if (x0 == x1 && y0 == y1) break;
            
            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }

    void OptimizedDrawCircle(int centerX, int centerY, int radius, Color color)
{
    radius = Mathf.Min(radius, maxBrushSize);
    
    foreach (var offset in brushOffsetCache[radius])
    {
        int currentX = centerX + offset.x;
        int currentY = centerY + offset.y;
        
        if (IsInTextureBounds(currentX, currentY))
        {
            Color existingColor = coloringTexture.GetPixel(currentX, currentY);
            if (existingColor.a > 0.1f && !IsOutlinePixel(existingColor))
            {
                coloringTexture.SetPixel(currentX, currentY, color);
                modifiedPixels.Add(new Vector2Int(currentX, currentY));
            }
        }
    }
}

    bool IsInTextureBounds(int x, int y)
    {
        return x >= 0 && x < textureWidth && y >= 0 && y < textureHeight;
    }

    public void SetDrawingMode(DrawingMode mode)
    {
        currentMode = mode;
        Debug.Log($"Mode changed to: {mode}");
    }

    public void SetBrushSize(float size)
    {
        switch (currentMode)
        {
            case DrawingMode.Draw:
                drawBrushSize = size;
                break;
            case DrawingMode.Brush:
                paintBrushSize = size;
                break;
            default:
                break;
        }
        Debug.Log($"Changed {currentMode} brush size to: {size}");
    }
      
    public void SetColor(Color newColor)
    {
        currentColor = newColor;
        Debug.Log($"Selected color: {newColor}");
    }

    private bool ColorEquals(Color a, Color b)
    {
        const float tolerance = 0.01f;
        return Mathf.Abs(a.r - b.r) < tolerance &&
               Mathf.Abs(a.g - b.g) < tolerance &&
               Mathf.Abs(a.b - b.b) < tolerance &&
               Mathf.Abs(a.a - b.a) < tolerance;
    }

    public void ClearCanvas()
    {
        if (!hasChanges)
        {
            Debug.Log("Không có gì để xóa.");
            ConfirmPopup.Instance.popup.SetActive(false);
            return; 
        }

        SaveCurrentState();

        Color[] originalPixels = GetOriginalPixels();
        coloringTexture.SetPixels(originalPixels);
        coloringTexture.Apply();

        hasChanges = false;
    }

    private Color[] GetOriginalPixels()
    {
        return originalPixels;
    }

    private void SaveCurrentState()
{
    if (!isTextureInitialized || coloringTexture == null)
    {Debug.Log("Texture not initialized or null");return;}
        
    Color[] currentPixels = coloringTexture.GetPixels();
	
    if (currentState != null)
    {
        bool pixelsMatch = ComparePixels(currentPixels, currentState);
        Debug.Log($"Current state exists. Pixels match: {pixelsMatch}");
    }
    else
    {
        Debug.Log("Current state is null");
    }
	
    if (ComparePixels(currentPixels, currentState))
        return;
		
    Color[] newState = new Color[currentPixels.Length];
    Array.Copy(currentPixels, newState, currentPixels.Length);
	
    // Giới hạn kích thước stack
    if (undoStack.Count >= MAX_UNDO_STEPS)
        {
            undoStack = new Stack<Color[]>(undoStack.Take(MAX_UNDO_STEPS - 1));
        }
        undoStack.Push(currentPixels);
	
    Debug.Log($"Stack size after save: {undoStack.Count}");
}

    private bool ComparePixels(Color[] array1, Color[] array2)
{
    Debug.Log("=== Comparing Pixels ===");
    if (array1 == null || array2 == null)
    {
        Debug.Log($"Array1 null: {array1 == null}, Array2 null: {array2 == null}");
        return false;
    }

    if (array1.Length != array2.Length)
    {
        Debug.Log($"Array lengths don't match. A1: {array1.Length}, A2: {array2.Length}");
        return false;
    }
    
    const float tolerance = 0.001f;
    
    for (int i = 0; i < array1.Length; i++)
    {
        Color c1 = array1[i];
        Color c2 = array2[i];
        
        if (Mathf.Abs(c1.r - c2.r) > tolerance ||
            Mathf.Abs(c1.g - c2.g) > tolerance ||
            Mathf.Abs(c1.b - c2.b) > tolerance ||
            Mathf.Abs(c1.a - c2.a) > tolerance)
        {
            return false; 
        }
    }
    
    return true; 
}

public void Undo() 
{
    if (undoStack.Count <= 1)
    {
        coloringTexture.SetPixels(originalPixels);
        coloringTexture.Apply();
        
        undoStack.Clear();
        undoStack.Push(originalPixels);
        
        currentState = new Color[originalPixels.Length];
        Array.Copy(originalPixels, currentState, originalPixels.Length);
        
        hasChanges = false;
        return;
    }

    isUndoing = true;
    undoStack.Pop();
    Color[] previousState = undoStack.Peek();
    coloringTexture.SetPixels(previousState);
    coloringTexture.Apply();
    currentState = new Color[previousState.Length];
    Array.Copy(previousState, currentState, previousState.Length);
    hasChanges = !ComparePixels(currentState, originalPixels);
    isUndoing = false;
}

    public void DebugUndoStack()
    {
        Debug.Log($"Kích thước stack hiện tại: {undoStack.Count}");
        Debug.Log($"isUndoing: {isUndoing}");
        Debug.Log($"hasChanges: {hasChanges}");
        Debug.Log($"needsApply: {needsApply}");
        
        if (coloringTexture != null)
        {
            Debug.Log($"Texture size: {coloringTexture.width}x{coloringTexture.height}");
        }
        
        if (currentState != null)
        {
            Debug.Log($"Current state size: {currentState.Length}");
        }
    }

    //Save
    public string imageId; 
    public System.Action<string, Texture2D> OnImageSaved;
     public SaveManager saveManager;

    public static ColoringManager Instance { get; private set; }

   public bool SaveCurrentImage()
    {
        if (!isTextureInitialized || coloringTexture == null || string.IsNullOrEmpty(imageId))
        {
            Debug.LogError("Cannot save: Invalid state or missing imageId");
            return false;
        }
        
        try
        {
            // Create texture copy for saving
            Texture2D savedTexture = new Texture2D(coloringTexture.width, coloringTexture.height, TextureFormat.RGBA32, false);
            savedTexture.SetPixels(coloringTexture.GetPixels());
            savedTexture.Apply();
            
            // Find SaveManager if not already assigned
            if (saveManager == null)
            {
                saveManager = SaveManager.Instance;
                if (saveManager == null)
                {
                    Debug.LogError("SaveManager not found!");
                    return false;
                }
            }
            
            // Save the image with current ID
            saveManager.SaveImage(imageId, savedTexture);
            
            // Mark as completed if needed
            // saveManager.MarkAsCompleted(imageId);
            
            // Invoke event if needed
            OnImageSaved?.Invoke(imageId, savedTexture);
            
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Save Image Error: {e.Message}");
            return false;
        }
    }

    private void InitializeCrayonTexture()
{
    crayonTexture = new Texture2D(64, 64);
    for (int y = 0; y < crayonTexture.height; y++)
    {
        for (int x = 0; x < crayonTexture.width; x++)
        {
            float noise1 = Mathf.PerlinNoise(x * 0.2f, y * 0.2f);
            float noise2 = Mathf.PerlinNoise(x * 0.4f + 100, y * 0.4f + 100);
            float finalNoise = noise1 * 0.6f + noise2 * 0.4f;
            crayonTexture.SetPixel(x, y, new Color(1, 1, 1, finalNoise));
        }
    }
    crayonTexture.Apply();
}
private bool IsOutlinePixel(Color color)
{
    float brightness = color.r * 0.299f + color.g * 0.587f + color.b * 0.114f;
    return brightness <= outlineThreshold && color.a > 0.5f;
}
        
}
