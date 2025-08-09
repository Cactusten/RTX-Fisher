using MelonLoader;
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;

[assembly: MelonInfo(typeof(SSRTMod.TimeWeatherMod), "SSRTMod", "0.1", "You")]
[assembly: MelonGame(null, null)]

namespace SSRTMod
{
    public class TimeWeatherMod : MelonMod
    {
        private GameObject managerObj;
        private RTInjector injector;

        public override void OnApplicationStart()
        {
            MelonLogger.Msg("SSRTMod loaded");
            managerObj = new GameObject("SSRT_Manager");
            Object.DontDestroyOnLoad(managerObj);
            injector = managerObj.AddComponent<RTInjector>();
        }
    }

    public class RTInjector : MonoBehaviour
    {
        public ComputeShader ssrtCompute;
        public Shader blitShader;

        private Camera mainCamera;
        private RenderTexture accumRT;
        private int kernelHandle;
        private int frameIndex = 0;
        private bool initialized = false;

        // settings (tuneable)
        public int maxSteps = 32;
        public float maxDistance = 30.0f;
        public float thickness = 0.05f;
        public int traceResolutionDivisor = 1; // 1 = full res, 2 = half res

        void Start()
        {
            mainCamera = Camera.main;
            if (mainCamera != null)
                mainCamera.depthTextureMode |= DepthTextureMode.DepthNormals;

            if (ssrtCompute != null)
            {
                kernelHandle = ssrtCompute.FindKernel("CSMain");
                initialized = true;
            }
            else
            {
                MelonLogger.Warning("SSRTMod: ssrtCompute not assigned in inspector.");
            }
        }

        void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        }

        void OnBeginCameraRendering(ScriptableRenderContext ctx, Camera cam)
        {
            if (!initialized)
            {
                if (ssrtCompute != null)
                {
                    kernelHandle = ssrtCompute.FindKernel("CSMain");
                    initialized = true;
                }
                else return;
            }

            if (cam != Camera.main) return;

            int w = cam.pixelWidth / Mathf.Max(1, traceResolutionDivisor);
            int h = cam.pixelHeight / Mathf.Max(1, traceResolutionDivisor);

            EnsureRenderTexture(w, h);

            // set compute params
            ssrtCompute.SetFloat("_MaxDistance", maxDistance);
            ssrtCompute.SetFloat("_Thickness", thickness);
            ssrtCompute.SetInt("_MaxSteps", maxSteps);
            ssrtCompute.SetInt("_FrameIndex", frameIndex++);

            // camera matrices
            Matrix4x4 proj = cam.projectionMatrix;
            Matrix4x4 invProj = proj.inverse;
            ssrtCompute.SetMatrix("_ProjectionMatrix", proj);
            ssrtCompute.SetMatrix("_InvProjectionMatrix", invProj);
            ssrtCompute.SetMatrix("_CameraToWorld", cam.cameraToWorldMatrix);

            // bind global textures (Unity global names)
            var depth = Shader.GetGlobalTexture("_CameraDepthTexture");
            var normals = Shader.GetGlobalTexture("_CameraNormalsTexture");
            if (depth == null || normals == null)
            {
                // fallback: try _CameraDepthTexture and _CameraNormalsTexture may not be ready - skip
                // alternatively, could copy from camera's target if available
            }

            ssrtCompute.SetTexture(kernelHandle, "_DepthTex", depth as Texture);
            ssrtCompute.SetTexture(kernelHandle, "_NormalTex", normals as Texture);
            ssrtCompute.SetTexture(kernelHandle, "Result", accumRT);

            int threadX = Mathf.CeilToInt(w / 8.0f);
            int threadY = Mathf.CeilToInt(h / 8.0f);
            ssrtCompute.Dispatch(kernelHandle, threadX, threadY, 1);

            CommandBuffer cmd = CommandBufferPool.Get("SSRT Blit");
            cmd.Blit(accumRT, BuiltinRenderTextureType.CameraTarget);
            ctx.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        void EnsureRenderTexture(int w, int h)
        {
            if (accumRT == null || accumRT.width != w || accumRT.height != h)
            {
                if (accumRT != null) accumRT.Release();
                accumRT = new RenderTexture(w, h, 0, RenderTextureFormat.ARGBFloat);
                accumRT.enableRandomWrite = true;
                accumRT.Create();
            }
        }

        void OnDestroy()
        {
            if (accumRT != null) accumRT.Release();
        }
    }
}
