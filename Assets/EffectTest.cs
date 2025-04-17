using UnityEngine;

public class EffectTest : MonoBehaviour
{
    [SerializeField]
    Renderer _renderer;
    private MaterialPropertyBlock block;
    [SerializeField]
    [Range(0, 1)]
    float _strength = 1f;

    [ColorUsage(true, true)]
    public Color _color;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        block = new MaterialPropertyBlock();
        _renderer.GetPropertyBlock(block);
    }

    // Update is called once per frame
    void Update()
    {
        block.SetFloat("_Strength", _strength);
        block.SetColor("_GlowColor", _color);
        _renderer.SetPropertyBlock(block);
    }
}
