using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Linq;

public class ColoringManager : MonoBehaviour
{
    public static ColoringManager Instance { get; private set; }
    
    [Header("Core References")]
    public SpriteRenderer targetSprite;
    public SaveManager saveManager;
    public string imageId;
    
    [Header("Drawing Settings")]
    public Color currentColor = Color.red;
    public DrawingMode currentMode = DrawingMode.Draw;
    public float drawBrushSize = 2f;
    public float paintBrushSize = 5f;
    public float crayonBrushSize = 8f;
    public float eraserSize = 10f;
    public Color eraserColor = Color.white;
    public float outlineThreshold = 0.2f;
    
    [Header("Cursor Settings")]
    public GameObject cursorPrefab; // Prefab hình tròn cho cursor
    private GameObject cursorInstance;
    
    [Header("Events")]
    public System.Action<string, Texture2D> OnImageSaved;

    // Core texture variables
    private Texture2D coloringTexture;
    private Texture2D crayonTexture;
    private Color[] originalPixels;
    private Color[] currentState;
    private Color[] originalState;
    private Stack<Color[]> undoStack = new Stack<Color[]>();
    private Vector2? lastDrawPosition;
    
    // Optimization variables
    private float textureWidth;
    private float textureHeight;
    private Vector2 spritePivot;
    private Vector2 spriteSize;
    private bool needsApply;
    private List<Vector2Int>[] brushOffsetCache;
    private List<Vector2Int> modifiedPixels = new List<Vector2Int>(1000);
    private bool isModifying = false;
    private int frameCounter = 0;
    private const int APPLY_FREQUENCY = 2;
    private int maxBrushSize = 120;
    
    // State tracking
    private bool isTextureInitialized = false;
    private bool hasChanges = false;
    private bool isUndoing = false;
    private const int MAX_UNDO_STEPS = 10;
    private Color[] originalOutlineState; 

    public enum DrawingMode
    {
        Hand,
        Draw,      // Vẽ
        Crayons,   // Bút màu 
        Brush,     // Cọ vẽ
        Erase      // Tẩy
    }

    #region Initialization

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        if (undoStack != null)
        {
            undoStack.Clear();
        }
        
        // Auto-find SaveManager if not assigned
        if (saveManager == null)
        {
            saveManager = FindObjectOfType<SaveManager>();
            if (saveManager == null)
            {
                Debug.LogWarning("Không tìm thấy SaveManager trong scene! Sẽ lưu ảnh mà không cập nhật tiến độ.");
            }
        }
        
        // Initialize cursor
        InitializeCursor();
    }

    void Start()
    {
        InitializeTexture();
        PrecomputeBrushOffsets();
        InitializeCrayonTexture();
        
        // Get the image ID from PlayerPrefs
        string savedImageId = PlayerPrefs.GetString("SelectedImageId", "");
        if (eraserSizePanel != null)
        {
            eraserSizePanel.SetActive(false);
        }
        
        if (!string.IsNullOrEmpty(savedImageId))
        {
            imageId = savedImageId;
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
        ClearUndoStack();
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
        
        // Lấy pixels từ sprite gốc
        var pixels = sprite.texture.GetPixels(
            (int)sprite.textureRect.x,
            (int)sprite.textureRect.y,
            (int)sprite.textureRect.width,
            (int)sprite.textureRect.height
        );
        
        // Áp dụng vào texture tô màu
        coloringTexture.SetPixels(pixels);
        coloringTexture.Apply();
        
        // Tạo sprite mới từ texture
        targetSprite.sprite = Sprite.Create(
            coloringTexture,
            new Rect(0, 0, width, height),
            new Vector2(0.5f, 0.5f)
        );
        
        // Cache common values
        textureWidth = coloringTexture.width;
        textureHeight = coloringTexture.height;
        spriteSize = targetSprite.sprite.bounds.size;
        spritePivot = targetSprite.sprite.pivot / new Vector2(width, height);
        
        // Trích xuất đường viền - hãy gọi sau khi đã khởi tạo texture
        ExtractOutlineOnly();
        
        isTextureInitialized = true;
    }
    catch (System.Exception e)
    {
        Debug.LogError($"Error creating coloring texture: {e.Message}");
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
    
    private void InitializeCursor()
    {
        // Tạo cursor hình tròn nếu chưa có
        if (cursorPrefab == null)
        {
            // Tạo cursor động nếu không có prefab
            cursorInstance = new GameObject("CursorCircle");
            SpriteRenderer renderer = cursorInstance.AddComponent<SpriteRenderer>();
            
            // Tạo texture hình tròn
            int size = 128;
            Texture2D circleTexture = new Texture2D(size, size);
            
            // Vẽ hình tròn với đường viền
            int radius = size / 2 - 2;
            int thickness = 2;
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(size/2, size/2));
                    
                    // Nếu nằm trong đường viền
                    if (distance > radius - thickness && distance < radius)
                    {
                        circleTexture.SetPixel(x, y, Color.white);
                    }
                    else
                    {
                        circleTexture.SetPixel(x, y, new Color(0, 0, 0, 0));
                    }
                }
            }
            
            circleTexture.Apply();
            
            // Tạo sprite từ texture
            Sprite circleSprite = Sprite.Create(
                circleTexture, 
                new Rect(0, 0, size, size), 
                new Vector2(0.5f, 0.5f)
            );
            
            renderer.sprite = circleSprite;
            renderer.sortingOrder = 100; // Đảm bảo nổi trên các sprite khác
            
            // Vô hiệu hóa ban đầu
            cursorInstance.SetActive(false);
        }
        else
        {
            // Instantiate từ prefab
            cursorInstance = Instantiate(cursorPrefab);
            cursorInstance.SetActive(false);
        }
    }

    public void InitializeWithSprite(Sprite sprite)
    {
        if (sprite == null)
        {
            Debug.LogError("Sprite is null");
            return;
        }
        
        // Get texture from sprite
        Texture2D tex = sprite.texture;
        
        // Initialize originalPixels
        originalPixels = tex.GetPixels();
        
        // Initialize coloringTexture
        coloringTexture = new Texture2D(tex.width, tex.height);
        coloringTexture.SetPixels(originalPixels);
        coloringTexture.Apply();
        
        // Initialize currentState
        currentState = new Color[originalPixels.Length];
        Array.Copy(originalPixels, currentState, originalPixels.Length);
        
        // Initialize originalState
        originalState = new Color[originalPixels.Length];
        Array.Copy(originalPixels, originalState, originalPixels.Length);
        
        // Clear stack and add initial state
        undoStack.Clear();
        Color[] initialState = new Color[originalPixels.Length];
        Array.Copy(originalPixels, initialState, originalPixels.Length);
        undoStack.Push(initialState);
        
        isTextureInitialized = true;
        hasChanges = false;
    }

    private void InitializeOriginalState()
    {
        if (coloringTexture != null && isTextureInitialized)
        {
            Color[] pixels = coloringTexture.GetPixels();
            
            // Initialize originalState
            if (originalState == null || originalState.Length != pixels.Length)
                originalState = new Color[pixels.Length];
            Array.Copy(pixels, originalState, pixels.Length);
            
            // Initialize currentState and push to stack
            if (currentState == null || currentState.Length != pixels.Length)
                currentState = new Color[pixels.Length];
            Array.Copy(pixels, currentState, pixels.Length);
            
            // Ensure stack is empty and contains initial state
            undoStack.Clear();
            undoStack.Push(pixels);
        }
    }
    
    #endregion

    #region Loading and Saving

 private void LoadSavedImage()
{
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
    
    // Load saved image if exists
    Texture2D savedTexture = saveManager.LoadSavedImage(imageId);
    
    if (savedTexture != null)
    {
        // Found saved image, use it
        coloringTexture.SetPixels(savedTexture.GetPixels());
        coloringTexture.Apply();
    }
    
    // Always extract outline after loading
    ExtractOutlineOnly();
    
    // Set up undo stack with current state
    ClearUndoStack();
}

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
    
    #endregion

    #region Drawing Logic

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
        
        // Update cursor position and visibility
        UpdateCursor();
    }
    
   private void UpdateCursor()
{
    // Only show cursor in erase mode
    if (currentMode == DrawingMode.Erase && cursorInstance != null)
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        cursorInstance.transform.position = new Vector3(mousePos.x, mousePos.y, -1);
        
        // Calculate world size for cursor
        float worldSpaceSize = CalculateCursorWorldSize(eraserSize);
        
        // Update cursor scale
        cursorInstance.transform.localScale = new Vector3(worldSpaceSize, worldSpaceSize, 1);
        
        // Show cursor if not already active
        if (!cursorInstance.activeSelf)
        {
            cursorInstance.SetActive(true);
        }
    }
    else if (cursorInstance != null && cursorInstance.activeSelf)
    {
        // Hide cursor when not in erase mode
        cursorInstance.SetActive(false);
    }
}

private float CalculateCursorWorldSize(float brushSizeInPixels)
{
    if (targetSprite == null || coloringTexture == null) 
        return brushSizeInPixels * 0.01f; // Fallback value
    
    // Calculate pixel to world unit ratio
    float pixelToWorldRatio = spriteSize.x / coloringTexture.width;
    
    // Không nhân với 2 nữa
    float worldSize = brushSizeInPixels * pixelToWorldRatio;
    
    return worldSize;
}

    void HandleDrawInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
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
            case DrawingMode.Erase:
                DrawAtPosition(currentMousePos, eraserColor, currentBrushSize, true);
                break;
        }
        hasChanges = true;
    }

Vector2 WorldToTextureCoordinates(Vector2 worldPos)
{
    Vector2 local = targetSprite.transform.InverseTransformPoint(worldPos);
    
    // Convert to normalized coordinates (0-1)
    float normalizedX = (local.x / spriteSize.x) + 0.5f;
    float normalizedY = (local.y / spriteSize.y) + 0.5f;
    
    // Convert to texture pixel coordinates
    int x = Mathf.Clamp(Mathf.RoundToInt(normalizedX * textureWidth), 0, coloringTexture.width - 1);
    int y = Mathf.Clamp(Mathf.RoundToInt(normalizedY * textureHeight), 0, coloringTexture.height - 1);
    
    return new Vector2(x, y);
}

    private float GetCurrentBrushSize()
    {
        return currentMode switch
        {
            DrawingMode.Draw => drawBrushSize,
            DrawingMode.Crayons => crayonBrushSize,
            DrawingMode.Brush => paintBrushSize,
            DrawingMode.Erase => eraserSize,
            _ => drawBrushSize
        };
    }

    #endregion

    #region Drawing Methods
    
  void DrawAtPosition(Vector2 pos, Color color, float brushSize, bool isEraser = false)
{
    float actualBrushSize = isEraser ? eraserSize : brushSize;
    
    Vector2 pixelPos = WorldToTextureCoordinates(pos);
    int x = (int)pixelPos.x;
    int y = (int)pixelPos.y;

    if (lastDrawPosition.HasValue)
    {
        Vector2 lastLocal = lastDrawPosition.Value;
        int lastX = Mathf.RoundToInt(lastLocal.x * textureWidth);
        int lastY = Mathf.RoundToInt(lastLocal.y * textureHeight);
        OptimizedDrawLine(lastX, lastY, x, y, color, Mathf.RoundToInt(actualBrushSize), isEraser);
    }
    else
    {
        if (isEraser)
        {
            RestoreOriginalPixels(x, y, Mathf.RoundToInt(actualBrushSize));
        }
        else
        {
            OptimizedDrawCircle(x, y, Mathf.RoundToInt(actualBrushSize), color);
        }
    }
    
    lastDrawPosition = new Vector2(pixelPos.x / textureWidth, pixelPos.y / textureHeight);
    needsApply = true;
}
void RestoreOriginalPixels(int centerX, int centerY, int radius)
{
    radius = Mathf.Min(radius, maxBrushSize);
    
    foreach (var offset in brushOffsetCache[radius])
    {
        int currentX = centerX + offset.x;
        int currentY = centerY + offset.y;
        
        if (IsInTextureBounds(currentX, currentY))
        {
            int pixelIndex = currentY * (int)textureWidth + currentX;
            
            if (pixelIndex >= 0 && pixelIndex < originalOutlineState.Length)
            {
                // Always use originalOutlineState to restore the pixel
                Color outlinePixel = originalOutlineState[pixelIndex];
                coloringTexture.SetPixel(currentX, currentY, outlinePixel);
                modifiedPixels.Add(new Vector2Int(currentX, currentY));
            }
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

    void OptimizedDrawLine(int x0, int y0, int x1, int y1, Color color, int radius, bool isEraser = false)
    {
        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        
        if (dx <= 1 && dy <= 1)
        {
            if (isEraser)
            {
                RestoreOriginalPixels(x1, y1, radius);
            }
            else
            {
                OptimizedDrawCircle(x1, y1, radius, color);
            }
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
                if (isEraser)
                {
                    RestoreOriginalPixels(x0, y0, radius);
                }
                else
                {
                    OptimizedDrawCircle(x0, y0, radius, color);
                }
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
    
    #endregion

    #region Utility Methods

    private bool IsOutlinePixel(Color color)
    {
        float brightness = color.r * 0.299f + color.g * 0.587f + color.b * 0.114f;
        return brightness <= outlineThreshold && color.a > 0.5f;
    }

    bool IsInTextureBounds(int x, int y)
    {
        return x >= 0 && x < textureWidth && y >= 0 && y < textureHeight;
    }

    private bool ColorEquals(Color a, Color b)
    {
        const float tolerance = 0.01f;
        return Mathf.Abs(a.r - b.r) < tolerance &&
               Mathf.Abs(a.g - b.g) < tolerance &&
               Mathf.Abs(a.b - b.b) < tolerance &&
               Mathf.Abs(a.a - b.a) < tolerance;
    }

    private bool ComparePixels(Color[] array1, Color[] array2)
    {
        if (array1 == null || array2 == null)
            return false;

        if (array1.Length != array2.Length)
            return false;
        
        int checkInterval = Mathf.Max(1, array1.Length / Math.Min(10, array2.Length / 1000));
        const float tolerance = 0.001f;
        
        for (int i = 0; i < array1.Length; i += checkInterval)
        {
            if (array1[i] != array2[i])
                return false;
        }
        
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
    
    #endregion

    #region State Management

    public void ClearUndoStack()
    {
        undoStack.Clear();
        if (coloringTexture != null && isTextureInitialized)
        {
            // Save the current state as the only state in the stack
            Color[] pixels = coloringTexture.GetPixels();
            undoStack.Push(pixels);
            
            // Update current state reference
            if (currentState == null || currentState.Length != pixels.Length)
                currentState = new Color[pixels.Length];
            Array.Copy(pixels, currentState, pixels.Length);
        }
    }

    private void SaveCurrentState()
    {
        if (!isTextureInitialized || coloringTexture == null)
            return;
        
        // Get current pixels
        Color[] currentPixels = coloringTexture.GetPixels();
        
        // Don't save if no changes from current state
        if (currentState != null && ComparePixels(currentPixels, currentState))
            return;
        
        // Create a copy of the current pixels
        Color[] newState = new Color[currentPixels.Length];
        Array.Copy(currentPixels, newState, currentPixels.Length);
        
        // Limit stack size
        if (undoStack.Count >= MAX_UNDO_STEPS)
        {
            // Create a new stack with only the most recent states
            Stack<Color[]> tempStack = new Stack<Color[]>();
            Color[] oldest = null;
            
            // Remove oldest state (keep MAX_UNDO_STEPS - 1 states)
            for (int i = 0; i < MAX_UNDO_STEPS - 1; i++)
            {
                if (undoStack.Count > 0)
                {
                    oldest = undoStack.Pop();
                    tempStack.Push(oldest);
                }
            }
            
            // Clear the original stack
            undoStack.Clear();
            
            // Restore the states in the correct order
            while (tempStack.Count > 0)
            {
                undoStack.Push(tempStack.Pop());
            }
        }
        
        // Add the new state
        undoStack.Push(newState);
        
        // Update current state reference
        if (currentState == null || currentState.Length != newState.Length)
            currentState = new Color[newState.Length];
        Array.Copy(newState, currentState, newState.Length);
    }

    public void Undo() 
    {
        if (undoStack.Count <= 1)
        {
            return;
        }

        isUndoing = true;
        undoStack.Pop();
        Color[] previousState = undoStack.Peek();
        coloringTexture.SetPixels(previousState);
        coloringTexture.Apply();
        
        // Update current state
        if (currentState == null || currentState.Length != previousState.Length)
            currentState = new Color[previousState.Length];
        Array.Copy(previousState, currentState, previousState.Length);
        
        // Check if we're back to the original state
        hasChanges = !ComparePixels(currentState, originalPixels);
        
        isUndoing = false;
    }

    private void ResetToOriginal()
    {
        if (originalPixels == null)
            return;
        
        // Apply original pixels
        coloringTexture.SetPixels(originalPixels);
        coloringTexture.Apply();
        
        // Clear undo stack and add the original state
        undoStack.Clear();
        undoStack.Push(originalPixels);
        
        // Update current state reference
        if (currentState == null || currentState.Length != originalPixels.Length)
            currentState = new Color[originalPixels.Length];
        Array.Copy(originalPixels, currentState, originalPixels.Length);
        
        hasChanges = false;
    }

  public void ClearCanvas()
{
    if (!hasChanges)
    {
        if (ConfirmPopup.Instance != null)
            ConfirmPopup.Instance.popup.SetActive(false);
        return; 
    }
    
    // Always use originalOutlineState as the "clear" state
    if (originalOutlineState != null && originalOutlineState.Length > 0)
    {
        // Create a copy to avoid reference issues
        Color[] clearState = new Color[originalOutlineState.Length];
        Array.Copy(originalOutlineState, clearState, originalOutlineState.Length);
        
        // Apply to texture
        coloringTexture.SetPixels(clearState);
        coloringTexture.Apply();
        
        // Update current state
        if (currentState == null || currentState.Length != clearState.Length)
            currentState = new Color[clearState.Length];
        Array.Copy(clearState, currentState, clearState.Length);
        
        // Reset undo stack with this clean state
        undoStack.Clear();
        undoStack.Push(clearState);
        
        hasChanges = false;
    }
    else
    {
        Debug.LogError("originalOutlineState is null or empty. Cannot clear canvas.");
    }
}
    
    #endregion

    #region Public Interface Methods
    public GameObject eraserSizePanel; 

    public void SetDrawingMode(DrawingMode mode)
    {
        currentMode = mode;
        if (eraserSizePanel != null)
    {
        eraserSizePanel.SetActive(mode == DrawingMode.Erase);
    }
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
            case DrawingMode.Erase:
                eraserSize = size;
                break;
            default:
                break;
        }
    }
      
    public void SetColor(Color newColor)
    {
        currentColor = newColor;
    }
    private void DebugImageStates(int x, int y)
{
    // Choose a specific pixel to examine (e.g., near where you're erasing)
    int pixelIndex = y * (int)textureWidth + x;
    
    if (pixelIndex >= 0 && pixelIndex < coloringTexture.GetPixels().Length)
    {
        Color currentPixel = coloringTexture.GetPixel(x, y);
        Color originalPixel = originalPixels[pixelIndex];
        Color originalStatePixel = originalState[pixelIndex];
        
        Debug.Log($"Pixel at ({x},{y}):");
        Debug.Log($"  Current texture: R:{currentPixel.r:F2} G:{currentPixel.g:F2} B:{currentPixel.b:F2} A:{currentPixel.a:F2}");
        Debug.Log($"  originalPixels: R:{originalPixel.r:F2} G:{originalPixel.g:F2} B:{originalPixel.b:F2} A:{originalPixel.a:F2}");
        Debug.Log($"  originalState: R:{originalStatePixel.r:F2} G:{originalStatePixel.g:F2} B:{originalStatePixel.b:F2} A:{originalStatePixel.a:F2}");
        
        // Check if we're dealing with an outline pixel
        bool isOutline = IsOutlinePixel(originalPixel);
        Debug.Log($"  Is outline pixel? {isOutline}");
    }
}
private void ExtractOutlineOnly()
{
    if (targetSprite == null || targetSprite.sprite == null)
    {
        Debug.LogError("Cannot extract outline from null sprite");
        return;
    }

    int width = coloringTexture.width;
    int height = coloringTexture.height;
    
    if (originalOutlineState == null || originalOutlineState.Length != width * height)
        originalOutlineState = new Color[width * height];
    
    // Get pixels from the original sprite
    Color[] pixels = targetSprite.sprite.texture.GetPixels(
        (int)targetSprite.sprite.textureRect.x,
        (int)targetSprite.sprite.textureRect.y,
        width,
        height
    );
    
    // Create a copy for the outline state (outline only, white interior)
    for (int i = 0; i < pixels.Length; i++)
    {
        Color pixel = pixels[i];
        // If it's an outline pixel (dark), keep it, otherwise make it white/transparent
        if (IsOutlinePixel(pixel))
        {
            originalOutlineState[i] = pixel; // Keep the outline pixel
        }
        else
        {
            originalOutlineState[i] = eraserColor; // Use eraser color (usually white)
        }
    }
}
    #endregion
}