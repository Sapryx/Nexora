using System.Numerics;
using Avalonia;

namespace Nexora.Render;

public class CardTransform
{
    public bool IsBackFacing => Matrix4x4.CreateFromQuaternion(rotation).M33 < 0;
    public bool IsSpinning => velocity != Vector2.Zero;
    public bool IsAnimating =>
        IsSpinning || rotation != target || scale != targetScale || (!isDragging && !IsSettled);
    private bool IsSettled => GetAngle(target, GetSettledRotation()) < MinSettleAngle;
    private const float RadiansPerPixel = 0.01f;
    private const double VelocityWindow = 0.1;
    private const double MinVelocitySpan = 0.01;
    private const float MaxSpinSpeed = 2500;
    private const float Friction = 4;
    private const float MinSpinSpeed = 10;
    private const double MaxReleaseDelay = 0.08;
    private const double FollowTime = 0.04;
    private const double SettleDelay = 1;
    private const float SettleRate = 6;
    private const float MinSettleAngle = 0.001f;
    private const float SnapAngle = 0.005f;
    private const float QuarterTurn = MathF.PI / 2;
    private const double PerspectiveDepthFactor = 2;
    private const double ZoomStep = 1.2;
    private const double MinZoom = 0.5;
    private const double MaxZoom = 4;
    private const double MinScaleDifference = 0.001;
    private readonly List<DragSample> samples = [];
    private Quaternion target = Quaternion.Identity;
    private Quaternion rotation = Quaternion.Identity;
    private Vector2 velocity;
    private double targetScale = 1;
    private double scale = 1;
    private double idleSeconds;
    private double lastDragTime;
    private bool isDragging;

    public void Rotate(double deltaX, double deltaY)
    {
        var axis = new Vector3((float)-deltaY, (float)deltaX, 0);
        float length = axis.Length();

        if(length == 0)
        {
            return;
        }

        var delta = Quaternion.CreateFromAxisAngle(axis / length, length * RadiansPerPixel);
        target = Quaternion.Normalize(Quaternion.Concatenate(target, delta));
    }

    public void Grab(double time)
    {
        Stop();
        samples.Clear();
        isDragging = true;
        lastDragTime = time;
    }

    public void Drag(double deltaX, double deltaY, double time)
    {
        Rotate(deltaX, deltaY);
        samples.Add(new DragSample()
        {
            StartTime = lastDragTime,
            Time = time,
            Delta = new Vector2((float)deltaX, (float)deltaY)
        });
        samples.RemoveAll(it => it.Time < time - VelocityWindow);
        lastDragTime = time;
        idleSeconds = 0;
    }

    public void Release(double time)
    {
        isDragging = false;
        samples.RemoveAll(it => it.Time < time - VelocityWindow);
        velocity = time - lastDragTime > MaxReleaseDelay ? Vector2.Zero : GetWindowVelocity();
        samples.Clear();

        if(velocity.Length() < MinSpinSpeed)
        {
            Stop();
        }
    }

    public void Zoom(double wheelDelta)
    {
        targetScale = Math.Clamp(targetScale * Math.Pow(ZoomStep, wheelDelta), MinZoom, MaxZoom);
    }

    public bool Advance(double seconds)
    {
        if(IsSpinning)
        {
            Spin(seconds);
            idleSeconds = 0;
        }
        else if(!isDragging)
        {
            idleSeconds += seconds;

            if(idleSeconds >= SettleDelay && !IsSettled)
            {
                Settle(seconds);
            }
        }

        bool isRotated = Follow(seconds);
        bool isScaled = FollowScale(seconds);

        return isRotated || isScaled;
    }

    public Matrix ToMatrix(double cardSize)
    {
        var scaling = Matrix4x4.CreateScale(IsBackFacing ? -(float)scale : (float)scale, (float)scale, (float)scale);
        var perspective = Matrix4x4.Identity;
        perspective.M34 = -1 / (float)(cardSize * scale * PerspectiveDepthFactor);
        var matrix = scaling * Matrix4x4.CreateFromQuaternion(rotation) * perspective;

        return new Matrix(
            matrix.M11, matrix.M12, matrix.M14,
            matrix.M21, matrix.M22, matrix.M24,
            matrix.M41, matrix.M42, matrix.M44
        );
    }

    private Vector2 GetWindowVelocity()
    {
        if(samples.Count == 0)
        {
            return Vector2.Zero;
        }

        double span = samples[samples.Count - 1].Time - samples[0].StartTime;

        if(span < MinVelocitySpan)
        {
            return Vector2.Zero;
        }

        var windowVelocity = samples.Aggregate(Vector2.Zero, (sum, it) => sum + it.Delta) / (float)span;
        float speed = windowVelocity.Length();

        return speed > MaxSpinSpeed ? windowVelocity * (MaxSpinSpeed / speed) : windowVelocity;
    }

    private void Stop()
    {
        velocity = Vector2.Zero;
        idleSeconds = 0;
    }

    private void Spin(double seconds)
    {
        Rotate(velocity.X * seconds, velocity.Y * seconds);
        velocity *= MathF.Exp(-Friction * (float)seconds);

        if(velocity.Length() < MinSpinSpeed)
        {
            Stop();
        }
    }

    private void Settle(double seconds)
    {
        var settled = GetSettledRotation();
        float progress = 1 - MathF.Exp(-SettleRate * (float)seconds);

        target = GetAngle(target, settled) * (1 - progress) < SnapAngle
            ? settled
            : Quaternion.Normalize(Quaternion.Slerp(target, settled, progress));
    }

    private bool Follow(double seconds)
    {
        if(rotation == target)
        {
            return false;
        }

        float progress = 1 - (float)Math.Exp(-seconds / FollowTime);

        rotation = GetAngle(rotation, target) * (1 - progress) < SnapAngle
            ? target
            : Quaternion.Normalize(Quaternion.Slerp(rotation, target, progress));

        return true;
    }

    private bool FollowScale(double seconds)
    {
        if(scale == targetScale)
        {
            return false;
        }

        double progress = 1 - Math.Exp(-seconds / FollowTime);
        scale += (targetScale - scale) * progress;

        if(Math.Abs(targetScale - scale) < MinScaleDifference)
        {
            scale = targetScale;
        }

        return true;
    }

    private Quaternion GetSettledRotation()
    {
        var normal = Vector3.Transform(Vector3.UnitZ, target);
        var facing = normal.Z < 0 ? -Vector3.UnitZ : Vector3.UnitZ;
        var aligned = Quaternion.Concatenate(target, GetShortestRotation(normal, facing));
        var axisX = Vector3.Transform(Vector3.UnitX, aligned);
        float angle = MathF.Atan2(axisX.Y, axisX.X);
        float snappedAngle = MathF.Round(angle / QuarterTurn) * QuarterTurn;
        var snap = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, snappedAngle - angle);

        return Quaternion.Normalize(Quaternion.Concatenate(aligned, snap));
    }

    private static Quaternion GetShortestRotation(Vector3 from, Vector3 to)
    {
        var axis = Vector3.Cross(from, to);
        float length = axis.Length();

        if(length == 0)
        {
            return Quaternion.Identity;
        }

        float angle = MathF.Acos(Math.Clamp(Vector3.Dot(from, to), -1, 1));
        return Quaternion.CreateFromAxisAngle(axis / length, angle);
    }

    private static float GetAngle(Quaternion from, Quaternion to)
    {
        return 2 * MathF.Acos(MathF.Min(MathF.Abs(Quaternion.Dot(from, to)), 1));
    }
}
