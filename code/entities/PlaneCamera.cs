using Sandbox;
using System;

class PlaneCamera : Camera
{
	protected float OrbitHeight = 35f;
	protected float OrbitDistance = 250f;

	private Angles orbitAngles;
	private Rotation orbitYawRot;
	private Rotation orbitPitchRot;
	private float planePitch;

	public override void Activated()
	{
		base.Activated();
		orbitAngles = Angles.Zero;
		orbitYawRot = Rotation.Identity;
		orbitPitchRot = Rotation.Identity;
		planePitch = 0;
	}

	public override void Update()
	{
		var pawn = Local.Pawn;
		if ( pawn == null )
			return;

		var plane = (pawn as PlanesPlayer)?.Plane as PlaneBase;
		if ( !plane.IsValid() )
			return;

		var planeRot = plane.Rotation;
		planePitch = planePitch.LerpTo( planeRot.Pitch().Clamp( -80, 80 ), Time.Delta * 1.5f );

		orbitYawRot = Rotation.Slerp( orbitYawRot, Rotation.From( 0f, orbitAngles.yaw, 0f ), Time.Delta * 25f );
		orbitPitchRot = Rotation.Slerp( orbitPitchRot, Rotation.From( orbitAngles.pitch, 0f, 0f ), Time.Delta * 25f );

		Rot = orbitYawRot * orbitPitchRot;

		var planePos = plane.Position + Vector3.Up * OrbitHeight;
		var targetPos = planePos + Rot.Backward * (OrbitDistance) + (Vector3.Up * (OrbitHeight));
		var tr = Trace.Ray( planePos, targetPos ).Ignore( plane ).Radius( Math.Clamp( 8 * plane.Scale, 2f, 10f ) ).WorldOnly().Run();

		Pos = tr.EndPos;
		Viewer = null;

		FieldOfView = 90;
	}

	public override void BuildInput( InputBuilder input )
	{
		base.BuildInput( input );

		orbitAngles.yaw += input.AnalogLook.yaw;
		orbitAngles.pitch += input.AnalogLook.pitch;
	}
}
