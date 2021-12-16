using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NaiveSDF: MonoBehaviour
{
    public ComputeShader compute;
    public RenderTexture resultTarget;

    private RenderTexture _target;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // Collect data from scene
            Camera c = GetComponent<Camera>();
            _target = CopyRenderTexture(c.targetTexture);
            c.targetTexture = _target;
            c.Render();

            // Invert
            compute.SetTexture(1, "res", _target);
            compute.Dispatch(1, _target.width / 8, _target.height / 8, 1);

            // Calculate SDF
            compute.SetTexture(0, "res", _target);
            Execute(0, 1);
            Execute(1, 0);

            // Convert back into a managable space
            compute.SetTexture(2, "res", _target);
            compute.Dispatch(2, _target.width / 8, _target.height / 8, 1);


            // Copy into result
            resultTarget.Release();
            resultTarget.Create();
            Graphics.Blit(_target, resultTarget);
        }
    }

    private RenderTexture CopyRenderTexture(RenderTexture toCopy)
    {
        // Get a render target for Ray Tracing
        RenderTexture t = new RenderTexture(toCopy);
        t.enableRandomWrite = true;
        t.Create();
        return t;
    }

    private void Execute(int x, int y)
    {
        compute.SetInts("passOffset", x, y);
        for (int i = 0; i < 1024; i++) {
            compute.SetInt("iterationCount", i);
            compute.Dispatch(0, _target.width/8, _target.height/8, 1);
        }
    }
}