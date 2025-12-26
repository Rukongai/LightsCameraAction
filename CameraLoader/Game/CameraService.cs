using System;
using System.Numerics;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using CameraLoader.Game.Structs;
using CameraLoader.Utils;

namespace CameraLoader.Game;

public unsafe class CameraService : IDisposable
{
    // Active position offset to apply (in character's local space - relative to character's forward direction)
    public Vector3 ActiveRelativeOffset { get; set; } = Vector3.Zero;

    // Whether position offset should be applied
    public bool ApplyPositionOffset { get; set; } = false;

    private delegate nint CameraUpdateDelegate(GameCamera* camera);

    [Signature("40 55 53 57 48 8D 6C 24 A0 48 81 EC ?? ?? ?? ?? 48 8B 1D", DetourName = nameof(CameraUpdateDetour))]
    private Hook<CameraUpdateDelegate>? _cameraUpdateHook = null!;

    public CameraService()
    {
        Service.GameInteropProvider.InitializeFromAttributes(this);

        if (_cameraUpdateHook != null)
        {
            _cameraUpdateHook.Enable();
            Service.PluginLog.Info($"Camera update hook initialized and enabled at {_cameraUpdateHook.Address:X}");
        }
        else
        {
            Service.PluginLog.Error("Camera update hook is NULL after initialization!");
        }
    }

    private unsafe nint CameraUpdateDetour(GameCamera* camera)
    {
        // Call original camera update first
        var result = _cameraUpdateHook!.Original(camera);

        // Only apply offset in GPose and when enabled
        if (ApplyPositionOffset && Service.GPoseHooking.IsGPosing)
        {
            var player = Service.ObjectTable.LocalPlayer;
            if (player != null)
            {
                // Get current character rotation and position
                float currentCharacterRotation = player.Rotation;
                Vector3 playerPos = player.Position;

                // Rotate the local-space offset by current character rotation to get world-space offset
                Vector3 worldOffset = MathUtils.RotateVectorHorizontal(ActiveRelativeOffset, currentCharacterRotation);

                // Set camera position = player position + world offset
                Vector3 newCameraPos = playerPos + worldOffset;
                Vector3 currentCameraPos = camera->Camera.CameraBase.SceneCamera.Object.Position;

                camera->Camera.CameraBase.SceneCamera.Object.Position = newCameraPos;

                // Also update the LookAtVector to maintain camera direction
                Vector3 currentLookAt = camera->Camera.CameraBase.SceneCamera.LookAtVector;
                camera->Camera.CameraBase.SceneCamera.LookAtVector = currentLookAt + (newCameraPos - currentCameraPos);
            }
        }

        return result;
    }

    public void Dispose()
    {
        _cameraUpdateHook?.Disable();
        _cameraUpdateHook?.Dispose();
    }
}
