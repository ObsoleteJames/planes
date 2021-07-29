using Sandbox;
using System;
using System.Collections.Generic;

[Library]
public class PlaneController : PawnController
{
	public override void FrameSimulate()
	{
		base.FrameSimulate();

		Simulate();
	}

	public override void Simulate()
	{
		var player = Pawn as PlanesPlayer;
		if ( !player.IsValid() )
			return;

		var plane = player.Plane as PlaneBase;
		if ( !plane.IsValid() )
			return;

		plane.Simulate( Client );

		if (player.Plane == null)
		{
			Position = plane.Position + plane.Rotation.Up * (100 * plane.Scale);
			Velocity += plane.Rotation.Right * (200 * plane.Scale);
			return;
		}

		EyeRot = Input.Rotation;
		EyePosLocal = Vector3.Up * (64 - 10) * plane.Scale;
		Velocity = plane.Velocity;

		SetTag( "sitting" );
	}
}
