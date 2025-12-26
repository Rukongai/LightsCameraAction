using CameraLoader.Utils;
using System.Numerics;

namespace CameraLoader.Game;

public unsafe class CameraPreset : PresetBase
{
    public float Distance { get; set; }
    public float HRotation { get; set; }
    public float VRotation { get; set; }
    public float ZoomFoV { get; set; }  // Applies when zooming in very closely
    public float GposeFoV { get; set; } // Can be adjusted in the GPose settings menu
    public float Pan { get; set; }
    public float Tilt { get; set; }
    public float Roll { get; set; }
    public Vector3 RelativePosition { get; set; }

    public CameraPreset() { }
    public CameraPreset(string name, int mode = 0)
    {
        float cameraRot = _camera->HRotation;
        float relativeRot = cameraRot;

        Vector3 cameraPos = _camera->Position;
        Vector3 relativePos = Vector3.Zero;

        if (mode == (int)PresetMode.CharacterOrientation)
        {
            var player = Service.ObjectTable.LocalPlayer;
            var playerRot = player?.Rotation ?? 0f;
            var playerPos = player?.Position ?? Vector3.Zero;

            relativeRot = MathUtils.ConvertToRelative(cameraRot, playerRot);
            relativePos = MathUtils.ConvertPositionToRelative(cameraPos, playerPos, playerRot);

            var worldOffset = new Vector3(cameraPos.X - playerPos.X, cameraPos.Y - playerPos.Y, cameraPos.Z - playerPos.Z);
            Service.PluginLog.Info($"[CameraPreset] Save - PlayerRot: {playerRot:F3} rad ({MathUtils.RadToDeg(playerRot):F1}°)");
            Service.PluginLog.Info($"[CameraPreset] Save - WorldOffset: {worldOffset}, LocalOffset (saved): {relativePos}");
        }

        // First Person Mode
        if (_camera->Mode == 0) { relativeRot = MathUtils.SubPiRad(relativeRot); }

        this.Name = name;
        this.PositionMode = mode;
        this.Distance = (_camera->Mode == 0) ? 0f : _camera->Distance;
        this.HRotation = relativeRot;
        this.VRotation = _camera->VRotation;
        this.ZoomFoV = _camera->FoV;
        this.GposeFoV = _camera->AddedFoV;
        this.Pan = _camera->Pan;
        this.Tilt = _camera->Tilt;
        this.Roll = _camera->Roll;
        this.RelativePosition = relativePos;
    }

    public bool IsValid()
    {
        // Doesn't go above Max, but Max can be externally modified
        if (this.Distance > 20f)
            return false;

        // Zoom FoV carries outside of gpose! Negative values flip the screen, High positive values are effectively a zoom hack
        // Gpose FoV resets when exiting gpose, but we don't want people suddenly entering gpose during a fight.
        if (this.ZoomFoV < 0.69f || this.ZoomFoV > 0.78f || this.GposeFoV < -0.5f || this.GposeFoV > 0.5f)
            return false;

        // Both reset when exiting gpose, but can still be modified beyond the limits the game sets
        if (this.Pan < -0.873f || this.Pan > 0.873f || this.Tilt < -0.647f || this.Tilt > 0.342f)
            return false;

        return true;
    }

    public override bool Load()
    {
        if (!this.IsValid()) { return false; }

        float hRotation = this.HRotation;

        // Set up position offset if in CharacterOrientation mode
        if (this.PositionMode == (int)PresetMode.CharacterOrientation)
        {
            var player = Service.ObjectTable.LocalPlayer;
            var playerRot = player?.Rotation ?? 0f;

            hRotation = MathUtils.ConvertFromRelative(this.HRotation, playerRot);

            // Set up the camera service to apply position offset
            Service.CameraService.ActiveRelativeOffset = this.RelativePosition;
            Service.CameraService.ApplyPositionOffset = true;

            var playerPos = player?.Position ?? Vector3.Zero;
            var worldOffset = MathUtils.RotateVectorHorizontal(this.RelativePosition, playerRot);
            var expectedCameraPos = playerPos + worldOffset;
            Service.PluginLog.Info($"[CameraPreset] Load - PlayerRot: {playerRot:F3} rad ({MathUtils.RadToDeg(playerRot):F1}°)");
            Service.PluginLog.Info($"[CameraPreset] Load - LocalOffset (loaded): {this.RelativePosition}, WorldOffset: {worldOffset}");
            Service.PluginLog.Info($"[CameraPreset] Load - PlayerPos: {playerPos}, Expected CamPos: {expectedCameraPos}");
        }
        else
        {
            // Disable position offset for non-character-relative presets
            Service.CameraService.ApplyPositionOffset = false;
            Service.PluginLog.Info($"[CameraPreset] Load - ApplyOffset: false (not CharacterOrientation mode)");
        }

        // First Person Mode
        if (_camera->Mode == 0) { hRotation = MathUtils.AddPiRad(hRotation); }

        _camera->Mode = (this.Distance == 0) ? 0 : 1;
        _camera->Distance = (this.Distance == 0) ? 1.5f : this.Distance;
        _camera->HRotation = hRotation;
        _camera->VRotation = this.VRotation;
        _camera->FoV = this.ZoomFoV;
        _camera->AddedFoV = this.GposeFoV;
        _camera->Pan = this.Pan;
        _camera->Tilt = this.Tilt;
        _camera->Roll = this.Roll;

        return true;
    }
}
