using System.Numerics;

namespace BvhViewer.Bvh;

public enum ChannelType
{
    Xposition, Yposition, Zposition, Xrotation, Yrotation, Zrotation
}

public sealed class BvhJoint
{
    public required string Name { get; init; }
    public BvhJoint? Parent { get; init; }
    public List<BvhJoint> Children { get; } = [];
    public Vector3 Offset { get; set; }
    public List<ChannelType> Channels { get; } = [];
    public Vector3 EndSiteOffset { get; set; }
    public bool HasEndSite { get; set; }
    public int ChannelStart { get; set; }
}

public sealed class BvhAnimation
{
    public required BvhJoint Root { get; init; }
    public required float[] MotionValues { get; init; }
    public required int FrameCount { get; init; }
    public required double FrameTime { get; init; }
    public int ChannelCount => Root.Enumerate().Sum(j => j.Channels.Count);
    public double Duration => FrameCount * FrameTime;

    public List<(Vector3 From, Vector3 To)> GetBones(int frame)
    {
        var bones = new List<(Vector3 From, Vector3 To)>();
        var values = MotionValues.AsSpan(frame * ChannelCount, ChannelCount);
        Evaluate(Root, Vector3.Zero, Quaternion.Identity, values, bones);
        return bones;
    }

    private static void Evaluate(BvhJoint joint, Vector3 parentPosition, Quaternion parentRotation,
        ReadOnlySpan<float> values, List<(Vector3 From, Vector3 To)> bones)
    {
        var localPosition = joint.Offset;
        var localRotation = Quaternion.Identity;
        var cursor = joint.ChannelStart;
        foreach (var channel in joint.Channels)
        {
            var value = values[cursor++];
            switch (channel)
            {
                case ChannelType.Xposition: localPosition.X += value; break;
                case ChannelType.Yposition: localPosition.Y += value; break;
                case ChannelType.Zposition: localPosition.Z += value; break;
                case ChannelType.Xrotation: localRotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitX, Degrees(value)); break;
                case ChannelType.Yrotation: localRotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitY, Degrees(value)); break;
                case ChannelType.Zrotation: localRotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitZ, Degrees(value)); break;
            }
        }

        var worldRotation = Quaternion.Normalize(parentRotation * localRotation);
        var worldPosition = parentPosition + Vector3.Transform(localPosition, parentRotation);

        if (joint.Parent != null)
        {
            bones.Add((parentPosition, worldPosition));
        }

        foreach (var child in joint.Children)
            Evaluate(child, worldPosition, worldRotation, values, bones);

        if (joint.HasEndSite)
        {
            var endPosition = worldPosition + Vector3.Transform(joint.EndSiteOffset, worldRotation);
            bones.Add((worldPosition, endPosition));
        }
    }

    private static float Degrees(float value) => MathF.PI * value / 180f;
}

public static class BvhJointExtensions
{
    public static IEnumerable<BvhJoint> Enumerate(this BvhJoint joint)
    {
        yield return joint;
        foreach (var child in joint.Children)
            foreach (var descendant in child.Enumerate())
                yield return descendant;
    }
}
