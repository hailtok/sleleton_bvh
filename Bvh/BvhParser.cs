using System.Globalization;
using System.Numerics;

namespace BvhViewer.Bvh;

public static class BvhParser
{
    public static BvhAnimation Parse(string text)
    {
        var tokens = Tokenize(text).ToArray();
        var index = 0;
        Expect(tokens, ref index, "HIERARCHY");
        Expect(tokens, ref index, "ROOT");
        var channelCursor = 0;
        var root = ParseJoint(tokens, ref index, null, ref channelCursor);
        Expect(tokens, ref index, "MOTION");
        Expect(tokens, ref index, "Frames:");
        var frameCount = ParseInt(tokens, ref index);
        Expect(tokens, ref index, "Frame");
        Expect(tokens, ref index, "Time:");
        var frameTime = ParseDouble(tokens, ref index);
        var values = new float[checked(frameCount * channelCursor)];
        for (var i = 0; i < values.Length; i++)
            values[i] = ParseFloat(tokens, ref index);
        return new BvhAnimation { Root = root, FrameCount = frameCount, FrameTime = frameTime, MotionValues = values };
    }

    private static BvhJoint ParseJoint(string[] tokens, ref int index, BvhJoint? parent, ref int channelCursor)
    {
        var joint = new BvhJoint { Name = tokens[index++], Parent = parent };
        Expect(tokens, ref index, "{");
        while (tokens[index] != "}")
        {
            switch (tokens[index++])
            {
                case "OFFSET":
                    joint.Offset = new Vector3(ParseFloat(tokens, ref index), ParseFloat(tokens, ref index), ParseFloat(tokens, ref index));
                    break;
                case "CHANNELS":
                    var count = ParseInt(tokens, ref index);
                    joint.ChannelStart = channelCursor;
                    for (var i = 0; i < count; i++)
                        joint.Channels.Add(ParseChannel(tokens[index++]));
                    channelCursor += count;
                    break;
                case "JOINT":
                    joint.Children.Add(ParseJoint(tokens, ref index, joint, ref channelCursor));
                    break;
                case "End":
                    Expect(tokens, ref index, "Site");
                    Expect(tokens, ref index, "{");
                    Expect(tokens, ref index, "OFFSET");
                    joint.EndSiteOffset = new Vector3(ParseFloat(tokens, ref index), ParseFloat(tokens, ref index), ParseFloat(tokens, ref index));
                    joint.HasEndSite = true;
                    Expect(tokens, ref index, "}");
                    break;
                default:
                    throw new FormatException($"Unexpected hierarchy token '{tokens[index - 1]}'.");
            }
        }
        Expect(tokens, ref index, "}");
        return joint;
    }

    private static ChannelType ParseChannel(string token) => token switch
    {
        "Xposition" => ChannelType.Xposition, "Yposition" => ChannelType.Yposition, "Zposition" => ChannelType.Zposition,
        "Xrotation" => ChannelType.Xrotation, "Yrotation" => ChannelType.Yrotation, "Zrotation" => ChannelType.Zrotation,
        _ => throw new FormatException($"Unknown channel '{token}'.")
    };

    private static IEnumerable<string> Tokenize(string text) =>
        text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

    private static void Expect(string[] tokens, ref int index, string expected)
    {
        if (index >= tokens.Length || tokens[index++] != expected)
            throw new FormatException($"Expected '{expected}' in BVH file.");
    }

    private static int ParseInt(string[] tokens, ref int index) =>
        int.Parse(tokens[index++], CultureInfo.InvariantCulture);

    private static float ParseFloat(string[] tokens, ref int index) =>
        float.Parse(tokens[index++], CultureInfo.InvariantCulture);

    private static double ParseDouble(string[] tokens, ref int index) =>
        double.Parse(tokens[index++], CultureInfo.InvariantCulture);
}
