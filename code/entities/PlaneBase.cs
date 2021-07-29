using Sandbox;
using System;
using System.Collections.Generic;

[Library( "ent_plane_base", Title = "Plane", Spawnable = true )]
public partial class PlaneBase : Prop, IUse
{
	private VehicleWheel frontLeft;
	private VehicleWheel frontRight;
	private VehicleWheel back;

	private ModelEntity wheel0;
	private ModelEntity wheel1;
	private ModelEntity wheel2;

	protected string PlaneModel;

	protected float MaxSpeed;
	protected float Acceleration;
	protected float NextUseTime;

	private struct InputState
	{
		public float throttle;
		public float rotate;
		public float roll;
		public float pitch;

		public void Reset()
		{
			throttle = 0;
			rotate = 0;
			roll = 0;
			pitch = 0;
		}
	}

	private InputState currentInput;

	[Net]
	public Player driver { get; private set; }

	public PlaneBase()
	{
		// Placeholder Plane model.
		PlaneModel = "models/tornado.vmdl";
		frontLeft = new VehicleWheel( this );
		frontRight = new VehicleWheel( this );
		back = new VehicleWheel( this );

		MaxSpeed = 860;
		Acceleration = 120;
	}

	public override void Spawn()
	{
		base.Spawn();

		SetModel( PlaneModel );
		SetupPhysicsFromModel( PhysicsMotionType.Dynamic );
		SetInteractsExclude( CollisionLayer.Player );
		EnableSelfCollisions = false;
	}

	public override void ClientSpawn()
	{
		base.ClientSpawn();

		wheel0 = new ModelEntity();
		wheel0.SetModel( "models/citizen_props/wheel02.vmdl" );
		wheel0.SetParent( this, "Wheel_Left", new Transform( Vector3.Forward * 6, Rotation.From( 90, 0, -90 ), 0.75f ) );

		wheel1 = new ModelEntity();
		wheel1.SetModel( "models/citizen_props/wheel02.vmdl" );
		wheel1.SetParent( this, "Wheel_Right", new Transform( -Vector3.Forward * 6, Rotation.From( 90, 180, -90 ), 0.75f ) );

		wheel2 = new ModelEntity();
		wheel2.SetModel( "models/citizen_props/wheel02.vmdl" );
		wheel2.SetParent( this, "Wheel_Back", new Transform( Vector3.Forward * 5, Rotation.From( 90, 0, -90 ), 0.65f ) );
	}

	public void OnDriverKilled( PlanesPlayer player )
	{
		RemoveDriver( player );
	}

	public bool AttachPlayer( PlanesPlayer player )
	{
		if ( player.Plane == null )
		{
			player.Plane = this;
			player.PlaneController = new PlaneController();
			player.Parent = this;
			player.LocalPosition = Vector3.Up * 25;
			player.LocalRotation = Rotation.Identity;
			player.LocalScale = 1;
			player.PhysicsBody.Enabled = false;

			driver = player;
			return true;
		}

		return false;
	}

	public void RemoveDriver( PlanesPlayer player )
	{
		NextUseTime = Time.Now + 0.125f;

		driver = null;
		player.Plane = null;
		player.PlaneController = null;
		player.VehicleCamera = null;
		player.Parent = null;
		player.PhysicsBody.Enabled = true;
		player.PhysicsBody.Position = player.Position;
		
		currentInput.Reset();
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();

		if ( driver is PlanesPlayer player )
			RemoveDriver( player );
	}

	public override void Simulate( Client cl )
	{
		if ( cl == null )
			return;

		if ( !IsServer )
			return;

		using ( Prediction.Off() )
		{
			currentInput.Reset();

			if ( Input.Pressed( InputButton.Use ) && cl.Pawn is PlanesPlayer player )
			{
				RemoveDriver( player );
				return;
			}

			currentInput.throttle = (Input.Down( InputButton.Run ) ? 1 : 0) + (Input.Down( InputButton.Duck ) ? -1 : 0);
			// This needs to be remapped before release.
			currentInput.rotate = (Input.Down( InputButton.Attack1 ) ? 1 : 0) + (Input.Down( InputButton.Attack2 ) ? -1 : 0);
			currentInput.pitch = (Input.Down( InputButton.Forward ) ? 1 : 0) + (Input.Down( InputButton.Back ) ? -1 : 0);
			currentInput.roll = (Input.Down( InputButton.Left ) ? 1 : 0) + (Input.Down( InputButton.Right ) ? -1 : 0);
		}
	}

	[Event.Physics.PreStep]
	public void OnPrePhysicsSetp()
	{
		if ( !IsServer )
			return;

		if ( !PhysicsBody.IsValid() )
			return;

		var body = PhysicsBody.SelfOrParent;
		if ( !body.IsValid() )
			return;

		var dt = Time.Delta;

		body.DragEnabled = true;

		var rot = PhysicsBody.Rotation;
		var center = PhysicsBody.MassCenter;
		var accelerateDirection = currentInput.throttle.Clamp( -1, 1 );

		//UpdateWheels( rot, true, dt );

		var localVelocity = rot.Inverse * body.Velocity;
		var lift = rot.Up * (localVelocity.x.Clamp( -1000, 1000 ) * 0.1f) + rot.Forward * (localVelocity.x * 0.07f);
		var acceleration = (accelerateDirection < 0.0f ? Acceleration * 0.5f : Acceleration) * accelerateDirection;
		var impulse = (rot * new Vector3( acceleration, 0, 0) + lift);
		body.ApplyForce( impulse * 100000f );
		
		body.ApplyForceAt( center + rot.Right * (currentInput.roll * 50f), rot.Up * 100000f * MathF.Abs( currentInput.roll ) );
		body.ApplyForceAt( center - (rot.Forward * 105), rot.Up * currentInput.pitch * 100000f );
		body.ApplyForceAt( center - (rot.Forward * 105), rot.Right * currentInput.rotate * 500000f );

		if ( PlanesPlayer.debug_plane )
		{
			DebugOverlay.ScreenText( impulse.x.ToString() );
			DebugOverlay.Line( center, center + (impulse * 0.5f), 0, false );
			DebugOverlay.Line( center + rot.Right * (currentInput.roll * 50f), center + rot.Right * (currentInput.roll * 50f) + (rot.Up * 15 * MathF.Abs( currentInput.roll )), 0, false );
			DebugOverlay.Line( center - rot.Forward * 105, (center - rot.Forward * 105) + (rot.Up * currentInput.pitch * 10), 0, false );
		}
	}

	protected virtual bool UpdateWheels( Rotation rotation, bool doPhysics, float dt )
	{
		var frontLeftPos = rotation.Forward * 42 + rotation.Right * 32 + rotation.Up * 20;
		var frontRightPos = rotation.Forward * 42 - rotation.Right * 32 + rotation.Up * 20;
		var backPos = -rotation.Forward * 42 + rotation.Right * 32 + rotation.Up * 20;

		float t = 0;

		frontLeft.Raycast( 6f, doPhysics, frontLeftPos, ref t, dt );
		frontRight.Raycast( 6f, doPhysics, frontRightPos, ref t, dt );
		back.Raycast( 6f, doPhysics, backPos, ref t, dt );

		return true;
	}

	public bool IsUsable( Entity user )
	{
		if ( Time.Now < NextUseTime )
			return false;

		return driver == null;
	}

	public bool OnUse( Entity user )
	{
		if ( user is PlanesPlayer player )
		{
			NextUseTime = Time.Now + 0.125f;
			player.VehicleCamera = new PlaneCamera();

			AttachPlayer( player );
		}
		return true;
	}

}
