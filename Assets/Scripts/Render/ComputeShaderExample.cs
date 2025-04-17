using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class ComputeShaderExample : MonoBehaviour
{
    public ComputeShader computeShader;
    public RenderTexture rtCanvas, rtLayerBG, rtLayerPen;
    public Renderer _renderer;
    public Color colorValueBG = new Color(0.5f, 0.5f, 0.5f, 1.0f);
    public Color colorValuePen = new Color(1.0f, 0.0f, 0.0f, 1.0f);
    public float brushSize = 10.0f;
    public float fadeOutAlpha = 0.1f; // Alpha value for fading out
    private string colorValueBGProperty = "colorValueBG";
    private string colorValuePenProperty = "colorValuePen";
    private string canvasProperty = "canvas";
    private string layerBGProperty = "layerBG";
    private string layerPenProperty = "layerPen";
    private string resolutionProperty = "resolution";
    private string positionXProperty = "positionX";
    private string positionYProperty = "positionY";
    private string brushSizeProperty = "brushSize";
    private string fadeOutAlphaProperty = "fadeOutAlpha";

    private int resolution = 512;

    int kernelCSMain;
    int kernelDrawPen;
    int kernelFadeOut;


    public Vector2Int position;

    void Start()
    {
        _renderer = GetComponent<Renderer>();
        kernelCSMain = computeShader.FindKernel("CSMain");
        kernelDrawPen = computeShader.FindKernel("DrawPen");
        kernelFadeOut = computeShader.FindKernel("FadeOut");
        rtCanvas = new RenderTexture(resolution, resolution, 0);
        rtCanvas.enableRandomWrite = true;
        rtCanvas.Create();

        rtLayerBG = new RenderTexture(resolution, resolution, 0);
        rtLayerBG.enableRandomWrite = true;
        rtLayerBG.Create();

        rtLayerPen = new RenderTexture(resolution, resolution, 0);
        rtLayerPen.enableRandomWrite = true;
        rtLayerPen.Create();

        _renderer.material.SetTexture("_BaseMap", rtCanvas);

        position = new Vector2Int(0, 0);

        computeShader.SetTexture(kernelCSMain, canvasProperty, rtCanvas);
        computeShader.SetTexture(kernelDrawPen, canvasProperty, rtCanvas);
        computeShader.SetTexture(kernelFadeOut, canvasProperty, rtCanvas);

        computeShader.SetTexture(kernelCSMain, layerBGProperty, rtLayerBG);
        computeShader.SetTexture(kernelDrawPen, layerBGProperty, rtLayerBG);
        computeShader.SetTexture(kernelFadeOut, layerBGProperty, rtLayerBG);

        computeShader.SetTexture(kernelCSMain, layerPenProperty, rtLayerPen);
        computeShader.SetTexture(kernelDrawPen, layerPenProperty, rtLayerPen);
        computeShader.SetTexture(kernelFadeOut, layerPenProperty, rtLayerPen);

        computeShader.SetVector(colorValueBGProperty, colorValueBG);
        computeShader.SetVector(colorValuePenProperty, colorValuePen);
        computeShader.SetInt(resolutionProperty, resolution);
        computeShader.SetInt(positionXProperty, position.x);
        computeShader.SetInt(positionYProperty, position.y);
        computeShader.Dispatch(kernelCSMain, resolution / 8, resolution / 8, 1);
    }

    private void Update()
    {
        computeShader.SetFloat(brushSizeProperty, brushSize);
        computeShader.SetFloat(fadeOutAlphaProperty, fadeOutAlpha);
        if (Input.GetMouseButton(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (!hit.collider.gameObject == gameObject)
                    return;
                Vector2 uv = hit.textureCoord;
                position.x = (int)(uv.x * resolution);
                position.y = (int)(uv.y * resolution);
            }

            DrawPen();
        }
        computeShader.Dispatch(kernelFadeOut, resolution / 8, resolution / 8, 1);
    }

    void DrawPen()
    {
        if (!computeShader)
            return;

        computeShader.SetFloat(positionXProperty, position.x);
        computeShader.SetFloat(positionYProperty, position.y);
        computeShader.Dispatch(kernelDrawPen, resolution / 8, resolution / 8, 1);
    }
}
