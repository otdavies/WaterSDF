using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class JumpFillSDF : MonoBehaviour
{
    public ComputeShader jumpfill;

    private RenderTexture _capture;
    private RenderTexture _lookup1;
    private RenderTexture _lookup2;
    private Camera _camera;

    private void Start()
    {
        _camera = GetComponent<Camera>();
        _capture = CopyRenderTexture(_camera.targetTexture);
        _lookup1 = new RenderTexture(_capture.width, _capture.height, 0, GraphicsFormat.R32G32B32A32_SInt);
        _lookup1.enableRandomWrite = true;
        _lookup2 = new RenderTexture(_capture.width, _capture.height, 0, GraphicsFormat.R32G32B32A32_SInt);
        _lookup2.enableRandomWrite = true;
    }

    private void Update()
    {
        CaptureWorld();
        ExecuteJF();
        WriteResult();
    }

    private RenderTexture CopyRenderTexture(RenderTexture toCopy)
    {
        RenderTexture t = new RenderTexture(toCopy);
        t.enableRandomWrite = true;
        t.Create();
        return t;
    }

    private void CaptureWorld()
    {
        // Collect data from scene
        RenderTexture temp = _camera.targetTexture;
        _camera.targetTexture = _capture;
        _camera.Render();
        _camera.targetTexture = temp;
    }

    private void WriteResult()
    {
        Graphics.Blit(_capture, _camera.targetTexture);
    }

    private RenderTexture ExecuteJF()
    {
        // Initialize the lookup table
        jumpfill.SetInt("resolution", 1 << 9);
        jumpfill.SetTexture(0, "example", _capture);
        jumpfill.SetTexture(0, "current", _lookup1);
        jumpfill.SetTexture(0, "next", _lookup2);
        jumpfill.Dispatch(0, _capture.width/8, _capture.height/8, 1);

        jumpfill.SetTexture(1, "current", _lookup1);
        jumpfill.SetTexture(1, "next", _lookup2);
        jumpfill.SetTexture(3, "current", _lookup1);
        jumpfill.SetTexture(3, "next", _lookup2);

        // Calculate distances
        for (int i = 8; i >= 0; i--)
        {
            jumpfill.SetInt("stepLength", 1 << i);
            jumpfill.Dispatch(1, _capture.width / 8, _capture.height / 8, 1); // jumpfill
            jumpfill.Dispatch(3, _capture.width / 16, _capture.height / 16, 1); // copy
        }

        // Write back to usable range
        jumpfill.SetTexture(2, "current", _lookup1);
        jumpfill.SetTexture(2, "result", _capture);
        jumpfill.Dispatch(2, _capture.width/8, _capture.height/8, 1);

        return _capture;
    }
}