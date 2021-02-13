using ECM.Components;
using ECM.Helpers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Adam Draheim
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PhysicsBody2D : MonoBehaviour
{
    #region EDITOR EXPOSED FIELDS

    public enum FORCE_MODE
    {
        acceleration,
        force,
        impulse,
        velocity
    }

    //Sound 
    private Rigidbody slimeRigidbody;
    private CapsuleCollider playerCollider;

    //IEnumerator Start()
    //{
    //    while (true)
    //    {
    //        float speed = slimeRigidbody.velocity.magnitude;
    //        RaycastHit hit = new RaycastHit();
    //        string floortag;
    //        bool hitBool = Physics.Raycast(transform.position, Vector3.down, out hit);
    //        Debug.Log("Hit: " + hitBool);
    //        if (hitBool)
    //        {
    //            Debug.Log("floor dist: " + (transform.position.y - hit.point.y));
    //            Debug.Log("tag: " + hit.collider.gameObject.tag);
    //        }
    //        if (hitBool && transform.position.y - hit.point.y <= playerCollider.height / 2f + 0.3f && speed > 0.3f)
    //        {
    //            floortag = hit.collider.gameObject.tag;
    //            switch (floortag)
    //            {
    //                case "Wall":
    //                    AkSoundEngine.SetSwitch("Material", "Wall", gameObject);
    //                    AkSoundEngine.PostEvent("Play_Walking", gameObject);
    //                    break;
    //                case "Water":
    //                    AkSoundEngine.SetSwitch("Material", "Water", gameObject);
    //                    AkSoundEngine.PostEvent("Play_Walking", gameObject);
    //                    break;
    //            }
    //        }
    //        else
    //        {
    //           yield return null;
    //        }
    //    }
   // }

    [Header("Object physics stats")]
    [SerializeField]
    [Tooltip("Default volume of physics object")]
    protected float baseVolume = 1;
    [SerializeField]
    [Tooltip("Default mass of the physics object")]
    protected float baseMass = 1;
    [SerializeField]
    [Tooltip("Gravity applied to physics object")]
    protected float appliedGravity = 10;
    
    [SerializeField]
    [Tooltip("Maximum vertical speed the object can travel")]
    protected float terminalVelocity = 20;

    [Header("Resistence forces")]
    [Tooltip(
        "Opposing force against any force applied while player is on groun")]
    [SerializeField]
    protected float _groundFriction = 8f;

    [SerializeField]
    [Tooltip("Default Rate at which a an object in motion slows down when no force is acted on it")]
    [Range(0.0f, 1.0f)]
    protected float staticBrakingRate = 1;


    [SerializeField]
    [Tooltip("Default Rate at which a an object in motion in air slows down when no force is acted on it")]
    [Range(0.0f, 1.0f)]
    protected float airBrakingRate = 0;

    [Tooltip("Friction coefficient applied when braking (when there is no input acceleration).\n" +
             "Only used if useBrakingFriction is true, otherwise groundFriction is used.")]
    protected float _brakingFriction = 8f;

    [Tooltip("Friction coefficient applied when 'not grounded' and force acting on it.")]
    [SerializeField]
    protected float _airFriction = 0f;

    [Header("Rotation Values")]
    [SerializeField]
    [Tooltip("How much the normal angle in degrees can differ from gravity for the player to hold on; is the angle too steep?")]
    protected float maxAngleSteepness;

    #endregion

    #region PROPERTIES
    private Rigidbody rb;
    private Physics2DDetections detections;

    //Direction gravity is applied
    protected Vector2 gravity_orientation;

    protected Vector2 DEFAULT_ORIENTATION = new Vector2(0, -1);

    //Resistance to movement in a liquid
    protected float viscosity;

    //Whether the physics body has direct movement from some source, like AI or player
    protected bool HasInput;


    //Adjusted for when the player is on a surface with non-normal friction like ice, for ground use
    protected float BrakingRateScale;

    //Handles whether the object is in liquid or not
    protected bool inLiquid;

    //Handles whether the object was previously in liquid
    protected bool wasInLiquid;

    //How much to modify gravity by, defaults to 1
    protected float gravityScale;

    protected Vector3 current_rotation;

    /// <summary>
    /// The default velocity the player moves at
    /// </summary>
    protected Vector3 base_velocity;

    private float frictionlessTime;

    /// <summary>
    /// Setting that affects movement control. Higher values allow faster changes in direction.
    /// If useBrakingFriction is false, this also affects the ability to stop more quickly when braking.
    /// </summary>

    public float groundFriction
    {
        get { return _groundFriction; }
        set { _groundFriction = Mathf.Max(0.0f, value); }
    }

    private float airFriction;

    /// <summary>
    /// True if character is falling, false if not.
    /// </summary>

    public bool isFalling
    {
        get { return !isGrounded; }
    }

    /// <summary>
    /// Is the character standing on 'ground'?
    /// </summary>

    public bool isGrounded
    {
        get { return detections.isGrounded; }
    }

    /// <summary>
    /// Toggle pause / resume.
    /// </summary>

    public bool pause { get; set; }

    /// <summary>
    /// Is the character paused?
    /// </summary>

    public bool isPaused { get; set; }

    #endregion

    #region METHODS

    /// <summary>
    /// Applies a force in the set 2D direction
    /// </summary>
    /// <param name="dir"></param>
    /// <param name="appliedValue"></param>
    /// <param name="force_mode">Acceleration does not consider mass while force does consider mass</param>
    public void ApplyForce(Vector2 dir, float appliedValue, FORCE_MODE force_mode, bool considerFriction=false, float frictionlessTime = 0.0f)
    {
        dir = dir.normalized;
        //Consider frictional forces if they are applied, max it to an opposing value of applied force
        Vector2 appliedFriction = new Vector2(0, 0);

        this.frictionlessTime = frictionlessTime;

        if (considerFriction)
        {
            //Check whether to use ground or air friction
            if (isFalling && !InsideLiquid())
            {
                appliedFriction = dir * airFriction;
            }else if (isGrounded && !InsideLiquid())
            {
                //appliedFriction = dir * groundFriction;
            }else if (InsideLiquid())
            {
                appliedFriction = dir.normalized * viscosity;
            }

            if (appliedFriction.magnitude > appliedValue)
            {
                //appliedFriction = appliedFriction.normalized * appliedValue;
            }
        }

        //Use the corresponding force mode
        switch (force_mode)
        {
            case FORCE_MODE.acceleration:
                rb.AddForce((dir * appliedValue - appliedFriction));
                break;
            case FORCE_MODE.force:
                rb.AddForce((dir * appliedValue * this.baseMass - appliedFriction));
                break;
            case FORCE_MODE.impulse:
                rb.velocity += (Vector3)(dir * appliedValue * baseMass - appliedFriction);
                break;
            case FORCE_MODE.velocity:
                rb.velocity += (Vector3)(dir * appliedValue - appliedFriction);
                break;
        }

    }


    /// <summary>
    /// Rotates toward gravity if airborne and rotates toward ground normal otherwise
    /// </summary>
    private void RotateTowardPosition()
    {
        if (isGrounded)
        {

            Vector3 g_norm = detections.groundNormal;

            float steepness = Vector2.SignedAngle(-g_norm, gravity_orientation);

            if (Mathf.Abs(steepness) <= maxAngleSteepness)
            {
                float angleDiff = -Vector2.SignedAngle(current_rotation, -g_norm);

                this.transform.Rotate(Vector3.forward, -angleDiff);
                current_rotation = -g_norm;
            }
        }
        else
        {
            Vector3 g_norm = this.gravity_orientation;

            float angleDiff = Vector2.SignedAngle(current_rotation, g_norm);

            this.transform.Rotate(Vector3.forward, angleDiff);
            current_rotation = g_norm;
        }

    }

    /// <summary>
    /// Applies gravity to the object, assuming the velocity has not already reached terminal. 
    /// </summary>
    private void ApplyGravity()
    {

        if (isFalling && !inLiquid)
        {

            rb.velocity += (Vector3) (appliedGravity * gravityScale * Time.deltaTime * gravity_orientation);

            if (GetVelocityInDirection(gravity_orientation) > terminalVelocity)
            {
                Vector2 ortho = new Vector2(gravity_orientation.y, -gravity_orientation.x);
                float dot = Vector2.Dot(ortho, rb.velocity);
                Vector2 proj = (dot / Mathf.Pow(ortho.magnitude, 2)) * ortho;

                rb.velocity = proj + (gravity_orientation.normalized * terminalVelocity);
            }
        }
    }

    /// <summary>
    /// Checks whether the player was in liquid or not before and if it was not, decrease y velocity
    /// </summary>
    private void CheckLiquid()
    {
        if (!wasInLiquid)
        {
            //Cut velocity in half when hitting a liquid
            rb.velocity /= 4;
            wasInLiquid = true;
        }
    }


    /// <summary>
    /// Checks if the player has changed its grounded state, and if now grounded where was not before then remove vertical component
    /// </summary>
    private void CheckGrounded()
    {

        if (isGrounded) {
            if (GetVelocityInDirection(current_rotation) > 0)
            {
                Vector2 ortho = new Vector2(current_rotation.y, -current_rotation.x);
                float dot = Vector2.Dot(ortho, rb.velocity);
                Vector2 proj = (dot / Mathf.Pow(ortho.magnitude, 2)) * ortho;

                rb.velocity = proj + (this.gravity_orientation * GetVelocityInDirection(this.gravity_orientation, base_velocity));

            }
        }
        
    }
    //Slows the object in opposite direction of horizontal component
    private void ApplyHorizBraking()
    {
        if (isGrounded)
        {


            //Get current velocity in direction
            Vector2 currHoriz = this.GetVelocityInDirection(GetOrthogonal(this.GetRotation())) 
                * GetOrthogonal(this.GetRotation());

            //Get base velocity in direction
            Vector2 getBaseHoriz = this.GetVelocityInDirection(GetOrthogonal(this.GetRotation()), this.base_velocity)
                * GetOrthogonal(this.GetRotation());

            Vector2 diff = currHoriz - getBaseHoriz;

            this.rb.velocity -= (Vector3)diff * staticBrakingRate;


            //Vector2 currDiscrepancy = this.rb.velocity - this.base_velocity;
            //rb.velocity -= (Vector3) (currDiscrepancy * staticBrakingRate);


        }
        else if (isFalling)
        {

            Vector2 ortho = GetOrthogonal(gravity_orientation);
            float speed = GetVelocityInDirection(ortho);

            speed = Mathf.Lerp(speed, 0, 1 - (airBrakingRate * BrakingRateScale));
            ortho *= speed;
            rb.velocity -= (Vector3)ortho;
        }
    }

    //Slows the object in opposite direction of horizontal component
    private void ApplyVertBraking()
    {
        if (isGrounded)
        {

            //Get current velocity in direction
            Vector2 currVert = this.GetVelocityInDirection(this.GetRotation()) * GetRotation();

            //Get base velocity in direction
            Vector2 getBaseVert = this.GetVelocityInDirection(this.GetRotation(), this.base_velocity) * GetRotation();

            Vector2 diff = currVert - getBaseVert;

            this.rb.velocity -= (Vector3)diff * staticBrakingRate;

        }
    }

    /// <summary>
    /// Sets the player horizontal velocity if grounded to the incline direction so that it continues smoothly
    /// </summary>
    private void SetHorizVelocityToIncline()
    {
        if (isGrounded)
        {
            Vector3 g_norm = detections.groundNormal;

            float steepness = Vector2.SignedAngle(-g_norm, gravity_orientation);

            if (Mathf.Abs(steepness) <= maxAngleSteepness)
            {
                //in case of jumping, do not rotate vertical speed
                float vertSpd = GetVelocityInDirection(current_rotation);
                Vector2 vertVel = rb.velocity - (Vector3)(vertSpd * current_rotation);

                float sign = GetVelocityInDirection(GetOrthogonal(current_rotation));

                float angleDiff = Vector2.SignedAngle(sign * GetOrthogonal(current_rotation), vertVel);

                Vector2 rotated_vel = RotateVector(vertVel, angleDiff * Mathf.Deg2Rad);

                //Readd vertical speed to the rotated velocity
                this.rb.velocity = ((Vector3)rotated_vel); //+ (vertSpd * current_rotation));

                this.rb.velocity += (vertSpd * current_rotation);
                

            }
        }
    }

    /// <summary>
    /// Perform character animation.
    /// </summary>

    protected virtual void Animate() { }

    /// <summary>
    /// Handles input.
    /// </summary>

    #endregion

    #region MONOBEHAVIOUR

    /// <summary>
    /// Validate editor exposed fields.
    /// 
    /// NOTE: If you override this, it is important to call the parent class' version of method
    /// eg: base.OnValidate, in the derived class method implementation, in order to fully validate the class.  
    /// </summary>

    public virtual void OnValidate()
    {

        groundFriction = _groundFriction;
        airFriction = _airFriction;

    }

    /// <summary>
    /// Initialize this component.
    /// 
    /// NOTE: If you override this, it is important to call the parent class' version of method,
    /// (eg: base.Awake) in the derived class method implementation, in order to fully initialize the class. 
    /// </summary>

    public virtual void Awake()
    {
        // Cache components

        rb = this.GetComponent<Rigidbody>();
        detections = this.GetComponentInChildren<Physics2DDetections>();

        BrakingRateScale = 0.1f;
        gravityScale = 1.0f;

        DEFAULT_ORIENTATION = new Vector2(0, -1);
        this.gravity_orientation = DEFAULT_ORIENTATION;

        current_rotation = this.gravity_orientation;

        //Sound
        slimeRigidbody = GetComponent<Rigidbody>();
        playerCollider = GetComponent<CapsuleCollider>();

    }

    public void FixedUpdate()
    {

    }

    public virtual void Update()
    {
        // If paused, return

        if (isPaused)
            return;
        //Handle rotations
        RotateTowardPosition();

        this.base_velocity = detections.baseVelocity;


        inLiquid = detections.inLiquid;
        //Update gravitational force
        if (!inLiquid)
        {
            ApplyGravity();

            //Update checking if ground
            CheckGrounded();
            SetHorizVelocityToIncline();
            if(frictionlessTime <= 0)
                ApplyHorizBraking();
            ApplyVertBraking();
            
            wasInLiquid = false;
           
        }
        else
        {
            CheckLiquid();
        }

        // Perform character animation (if not paused)

        Animate();

        //Set inliquid to false so that other liquid need not know internal logic
        inLiquid = false;

        detections.UpdateCollision();

        frictionlessTime -= (frictionlessTime > 0 ? Time.deltaTime : 0);

    }


    #endregion

    #region Getters and Setters

    public float GetMass()
    {
        return baseMass;
    }

    public void SetMass(float mass)
    {
        this.baseMass = mass;
    }

    public float GetVolume()
    {
        return baseVolume;
    }

    public void SetVolume(float vol)
    {
        this.baseVolume = vol;
    }
    public void SetGravityScale(float gravityScale)
    {
        this.gravityScale = gravityScale;
        
    }

    public void SetBrakingScale(float scale)
    {
        this.BrakingRateScale = scale;
    }

    public float GetDensity()
    {
        if(baseVolume == 0)
        {
            return 1;
        }

        return GetMass() / GetVolume();
       
    }

    public bool InsideLiquid()
    {
        return detections.inLiquid;
    }

    public float GetWeight()
    {
        return this.appliedGravity * this.gravityScale * GetMass();
    }
     
    public void SetViscosity(float visc)
    {
        this.viscosity = visc;
    }

    /// <summary>
    /// Inverts the gravity orientation so it is facing the opposite direction
    /// </summary>
    public void FlipGravity()
    {
        this.SetGravityOrientation(-gravity_orientation);
    }

    public void SetDefaultGravity(float value)
    {
        this.appliedGravity = value;
    }

    public Vector2 GetGravityOrientation()
    {
        return this.gravity_orientation;
    }

    public void SetGravityOrientation(Vector2 orientation)
    {
        this.gravity_orientation = orientation.normalized;
        
    }

    public void SetGravityToDefault()
    {
        this.SetGravityOrientation(DEFAULT_ORIENTATION);
    }

    public Vector2 GetGravity()
    {
        return this.gravity_orientation * this.gravityScale * this.appliedGravity;
    }

    /// <summary>
    /// Set to true IFF the object has other movement logic acting, like player input or AI
    /// </summary>
    /// <param name="hasInput"></param>
    public void SetHasInput(bool hasInput)
    {
        this.HasInput = hasInput;
    }

    public float GetVelocityInDirection(Vector2 direction)
    {
        float velocity = (rb.velocity.x * direction.x) + (rb.velocity.y * direction.y);
        return velocity;
    }

    public float GetVelocityInDirection(Vector2 direction, Vector2 vel)
    {
        float velocity = (vel.x * direction.x) + (vel.y * direction.y);
        return velocity;
    }

    public Vector2 GetRotation()
    {
        return this.current_rotation;
    }

    private Vector2 GetOrthogonal(Vector2 pos)
    {
        return new Vector2(pos.y, -pos.x);
    }

    private Vector2 RotateVector(Vector2 vec, float rotation)
    {
        float x = (Mathf.Cos(rotation) * vec.x) - (Mathf.Sin(rotation) * vec.y);
        float y = (Mathf.Sin(rotation) * vec.x) + (Mathf.Cos(rotation) * vec.y);

        return new Vector2(x, y);
    }

    public Vector2 GetBaseVelocity()
    {
        return this.base_velocity;
    }

    #endregion
}
