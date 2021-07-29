
using Sandbox;
using System;

public partial class PlanesPlayer : Player
{
	[ConVar.Replicated("debug_plane")]
	public static bool debug_plane { get; set; } = false;

	[Net] public PawnController PlaneController { get; set; }
	[Net] public Entity Plane { get; set; }

	[Net] public ICamera VehicleCamera { get; set; }
	[Net] public ICamera MainCamera { get; set; }

	public override void Respawn()
    {
		SetModel("models/citizen/citizen.vmdl");

		Controller = new WalkController();
		Animator = new StandardPlayerAnimator();
		MainCamera = new ThirdPersonCamera();

		EnableAllCollisions = true;
		EnableDrawing = true;
		EnableHideInFirstPerson = true;
		EnableShadowInFirstPerson = false;

		base.Respawn();
    }

	public override PawnController GetActiveController()
	{
		if ( PlaneController != null )
			return PlaneController;

		return base.GetActiveController();
	}

	public override void Simulate( Client cl )
	{
		base.Simulate( cl );

		if (Input.ActiveChild != null)
			ActiveChild = Input.ActiveChild;

		if ( VehicleCamera != null )
			Camera = VehicleCamera;
		else
			Camera = MainCamera;
		
		TickPlayerUse();
		SimulateActiveChild( cl, ActiveChild );
	}

	public override void OnKilled()
	{
		base.OnKilled();

		if ( Plane is PlaneBase plane )
			plane.OnDriverKilled( this );

		PlaneController = null;
		Plane = null;
	}

}
