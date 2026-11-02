import math

hierarchy = """HIERARCHY
ROOT Hips
{
  OFFSET 0.0 0.0 0.0
  CHANNELS 6 Xposition Yposition Zposition Zrotation Xrotation Yrotation
  JOINT Spine
  {
    OFFSET 0.0 20.0 0.0
    CHANNELS 3 Zrotation Xrotation Yrotation
    JOINT Neck
    {
      OFFSET 0.0 15.0 0.0
      CHANNELS 3 Zrotation Xrotation Yrotation
      End Site
      {
        OFFSET 0.0 10.0 0.0
      }
    }
    JOINT LeftArm
    {
      OFFSET -10.0 10.0 0.0
      CHANNELS 3 Zrotation Xrotation Yrotation
      End Site
      {
        OFFSET -20.0 0.0 0.0
      }
    }
    JOINT RightArm
    {
      OFFSET 10.0 10.0 0.0
      CHANNELS 3 Zrotation Xrotation Yrotation
      End Site
      {
        OFFSET 20.0 0.0 0.0
      }
    }
  }
  JOINT LeftLeg
  {
    OFFSET -8.0 -5.0 0.0
    CHANNELS 3 Zrotation Xrotation Yrotation
    End Site
    {
      OFFSET 0.0 -40.0 0.0
    }
  }
  JOINT RightLeg
  {
    OFFSET 8.0 -5.0 0.0
    CHANNELS 3 Zrotation Xrotation Yrotation
    End Site
    {
      OFFSET 0.0 -40.0 0.0
    }
  }
}
MOTION
Frames: 60
Frame Time: 0.033333"""

lines = [hierarchy]

for i in range(60):
    t = i / 60.0
    wave = math.sin(t * math.pi * 4) * 45  # Wave left arm
    wave2 = math.cos(t * math.pi * 4) * 45 # Wave right arm
    spin = math.sin(t * math.pi * 2) * 20  # Spine sway
    
    frame_data = f"0.0 0.0 0.0 0.0 0.0 0.0 0.0 0.0 {spin:.2f} 0.0 0.0 0.0 {wave:.2f} 0.0 0.0 {wave2:.2f} 0.0 0.0 0.0 0.0 0.0 0.0 0.0 0.0"
    lines.append(frame_data)

with open('sample.bvh', 'w') as f:
    f.write('\n'.join(lines))

