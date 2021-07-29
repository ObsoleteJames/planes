using Sandbox;
using System;

class VehicleWheel
{
	private readonly PlaneBase parent;

	private float previousLenght;
	private float currentLenght;

	public VehicleWheel( PlaneBase parent )
	{
		this.parent = parent;
		previousLenght = 0;
		currentLenght = 0;
	}

	public bool Raycast( float lenght, bool doPhysics, Vector3 offset, ref float wheel, float dt )
	{
		var pos = parent.Position;
		var rot = parent.Rotation;

		var wheelAttachPos = pos + offset;
		var wheelExtend = wheelAttachPos - rot.Up * (lenght * parent.Scale);

		var tr = Trace.Ray( wheelAttachPos, wheelExtend ).Ignore( parent ).Run();
		wheel = lenght * tr.Fraction;
		var wheelRadius = (14 * parent.Scale);

		if ( PlanesPlayer.debug_plane )
		{
			var wheelPos = tr.Hit ? tr.EndPos : wheelExtend;
			wheelPos += rot.Up * wheelRadius;

			if ( tr.Hit )
			{
				DebugOverlay.Circle( wheelPos, rot * Rotation.FromYaw( 90 ), wheelRadius, Color.Red.WithAlpha( 0.5f ), false );
			}
			else
				DebugOverlay.Circle( wheelPos, rot * Rotation.FromYaw( 90 ), wheelRadius, Color.Green.WithAlpha( 0.5f ), true );

		}

		if ( !tr.Hit && !doPhysics )
			return false;

		var body = parent.PhysicsBody.SelfOrParent;

		previousLenght = currentLenght;
		currentLenght = (lenght * parent.Scale) - tr.Distance;

		var springVelocity = (currentLenght - previousLenght) / dt;
		var springForce = body.Mass * 50.0f * currentLenght;
		var damperForce = body.Mass * (1.5f + (1.0f - tr.Fraction) * 3.0f) * springVelocity;
		var velocity = body.GetVelocityAtPoint( wheelAttachPos );
		var speed = velocity.Length;
		var speedDot = MathF.Abs( speed ) > 0.0f ? MathF.Abs( MathF.Min( Vector3.Dot( velocity, rot.Up.Normal ) / speed, 0.0f ) ) : 0.0f;
		var speedAlongNormal = speedDot * speed;
		var correctionMultiplier = (1.0f - tr.Fraction) * (speedAlongNormal / 1000.0f);
		var correctionForce = correctionMultiplier * 50f * speedAlongNormal / dt;

		body.ApplyImpulseAt( wheelAttachPos, tr.Normal * (springForce + damperForce + correctionForce) * dt );

		return true;
	}
}
