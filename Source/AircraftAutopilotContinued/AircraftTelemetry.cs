// GPL-3.0-only. VesselState subset adapted from MechJeb 2.15.4.0.
using System;
using UnityEngine;
namespace AircraftAutopilotContinued
{
    public class AircraftTelemetry
    {
        public readonly MovingAverage speedSurface = new MovingAverage(), speedVertical = new MovingAverage(),
            vesselPitch = new MovingAverage(), altitudeASL = new MovingAverage();
        public double currentPitch, currentRoll, currentHeading;
        public Vector3d surfaceVelocity, forward, north, east;
        public double HeadingFromDirection(Vector3d dir) => Maths.ClampDegrees360(Math.Atan2(Vector3d.Dot(dir, east), Vector3d.Dot(dir, north)) * 180 / Math.PI);
        public void Update(Vessel vessel)
        {
            Vector3d com = vessel.CoMD;
            Vector3d up = (com - vessel.mainBody.position).normalized;
            north = vessel.north; east = vessel.east; forward = vessel.GetTransform().up;
            Quaternion rotationSurface = Quaternion.LookRotation(north, up);
            Quaternion rotationVesselSurface = Quaternion.Inverse(Quaternion.Euler(90, 0, 0) *
                Quaternion.Inverse(vessel.GetTransform().rotation) * rotationSurface);
            surfaceVelocity = vessel.obt_velocity - vessel.mainBody.getRFrmVel(com);
            Vector3 angles = rotationVesselSurface.eulerAngles;
            currentPitch = Maths.Pitch(angles.x); currentRoll = Maths.Roll(angles.z); currentHeading = angles.y;
            Sample(surfaceVelocity.magnitude, Vector3d.Dot(surfaceVelocity, up), currentPitch, vessel.mainBody.GetAltitude(com));
        }
        public void Sample(double speed, double vertical, double pitch, double altitude)
        {
            if (!Maths.Finite(speed) || !Maths.Finite(vertical) || !Maths.Finite(pitch) || !Maths.Finite(altitude))
                throw new InvalidOperationException("Invalid aircraft telemetry");
            speedSurface.Value = speed; speedVertical.Value = vertical;
            vesselPitch.Value = pitch; altitudeASL.Value = altitude;
        }
    }
}
