using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.GraphicsBuffer;

public class HedgehogController : GameEntity
{
    public GameManager gameManager;
    public float frameMultiplier;
    public float scriptLock;
    public int rings;
    public bool localPlayer;
    public Vector3 LastUp;
    public enum Badge
    {
        //base
        Unequip, 
        //normal
        BounceBracelet, 
        LightSpeed, //charge a spindash and release to zip toward closeby rings.
        BlueShoes, //gives Tails a flight cancel and Sonic a Momentum Flip
        LightweightShoes, //disables the homing attack but gives you a better airdash.
        //handicap
        OneHitHero, //cannot keep rings but collecting them does give you a small speed boost. 
    }
    public Badge badge1;
    public Badge badge2;
    public Badge badge3;
    public enum CharacterState
    {
        Walk,
        Turn,
        Spin,
        Spindash,
        Die,
        Stun,
        Sprung
    }
    public CharacterState currentstate;
    public CharacterInputManager input;
    public float turnSpeed;
    public float delta;
    public bool uncapped;
    public Vector3 CamOffset;
    public float SlopeForce;
    public Animator anim;
    public AnimationCurve runSpeedCurve;
    public GameObject rollModel;
    public GameObject normModel;
    public GameObject ballModel;
    public Transform ShadowCaster;
    public Transform reticle;
    public Transform head;
    // Start is called before the first frame update
    void Start()
    {
        if (localPlayer)
        {
            Application.targetFrameRate = 30 * Mathf.RoundToInt(frameMultiplier);
            delta *= 2;
            delta /= frameMultiplier;
        }
        input = GetComponent<CharacterInputManager>();
        if (cameraTransform)
        {
            if (mouseLock)
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
            }
        }
        foreach (MeshRenderer render in GetComponentsInChildren<MeshRenderer>()) //change the shoe color to green if we have some lightweight shoes
        { 
            foreach(Material mat in render.materials)
            {
                if (HasBadge(Badge.LightweightShoes))
                {
                    if (HasBadge(Badge.BlueShoes))
                    {
                        mat.SetColor("_WithColor", new Color(0, 0.75f, 1, 1));
                    }
                    else
                    {
                        mat.SetColor("_WithColor", new Color(0, 0.75f, 0, 1));
                    }
                }
                else
                {
                    if (HasBadge(Badge.BlueShoes))
                    {
                        mat.SetColor("_WithColor", new Color(0, 0f, 1, 1));
                    }
                    else
                    {
                        mat.SetColor("_WithColor", mat.GetColor("_ReplaceColor"));
                    }
                }
            }
        }
    }
    public float charge;
    public float chargeAngle;
    // Update is called once per frame
    void Update()
    {
        if (spd < 2 && incline > 45)
        {
            transform.rotation = Quaternion.identity;
        }
        anim.SetFloat("Turn", 0.5f);
        if (uncapped && localPlayer)
        {
            delta = Time.deltaTime;
            frameMultiplier = 1 / Time.deltaTime / 30;
            Application.targetFrameRate = -1;
        }
        if (rings < 0) //prevent ring debt
        {
            rings = 0;
        }
        if (transform.position.y < gameManager.killPlane)
        {
            transform.position = Vector3.zero;
            airspd = Vector3.zero;
            spd = 0;
        }
        if(scriptLock > 0)
        {
            scriptLock -= delta;
            if(scriptLock < 0)
            {
                scriptLock = 0;
                model.transform.localRotation = Quaternion.Euler(0, movementAngle, 0);
                airspd = airspd/4;
            }
        }
        reticle.parent = null;
        reticle.gameObject.SetActive(false);
        if (localPlayer)
        {
            cameraTransform.transform.rotation = Quaternion.Euler(0, yRotation - roff, 0);
            input.CheckInputs();
        }
        int steps = 16;
        if(input.Z.wasReleasedThisTick)
        {
            if(currentstate == CharacterState.Spin)
            {
                currentstate = CharacterState.Walk;
            }
        }
        if (grounded)
        {
            roff = 0;
            if (currentstate == CharacterState.Spin && spd <= 0 || currentstate == CharacterState.Turn && spd <= 0)
            {
                currentstate= CharacterState.Walk;
            }
            if (currentstate == CharacterState.Spindash)
            {
                charge = Mathf.Lerp(charge, 100, delta * 2);
                if(charge > 100)
                {
                    charge = 100;
                }
            }
            if (input.B.wasPressedThisTick)
            {
                if (currentstate == CharacterState.Spin)
                {
                    currentstate = CharacterState.Walk;
                }
                else if(currentstate == CharacterState.Walk)
                {
                    currentstate = CharacterState.Spindash;
                    charge = 0;
                    chargeAngle = movementAngle;
                }
            }
            if (input.B.wasReleasedThisTick)
            {
                if(currentstate == CharacterState.Spindash)
                {
                    spd += charge;
                    charge = 0;
                    currentstate = CharacterState.Spin;
                    movementAngle = chargeAngle;
                    cameraImpact = 0.05f;
                }
            }

            if (input.Z.wasPressedThisTick && spd > 0.1f)
            {
                currentstate = CharacterState.Spin;
            }
            float scaledTurnSpeed = turnSpeed / Mathf.Max(1f, spd);
            scaledTurnSpeed = Mathf.Max(3, scaledTurnSpeed);
            if(currentstate == CharacterState.Spin)
            {
                scaledTurnSpeed *= 2;
            }
            float joystickAngle = Vector3.SignedAngle(Vector3.forward, input.joystick, Vector3.up);
            float angle = Mathf.DeltaAngle(joystickAngle, movementAngle);
            angle = Mathf.Abs(angle);
            if (input.joystickRaw.magnitude > .9f)
            {
                if(currentstate != CharacterState.Spin && currentstate != CharacterState.Spindash)
                {
                    Debug.Log(angle);
                    if (angle > 45)
                    {
                        if (angle > 90)
                        {
                            currentstate = CharacterState.Turn; 
                            if (spd > 0)
                            {
                                spd -= delta * 50;
                            }
                            else
                            {
                                spd = 0;
                            }
                            if (spd <= 0)
                            {
                                movementAngle = Quaternion.LookRotation(input.joystick).eulerAngles.y;
                                currentstate = CharacterState.Walk;
                            }
                        }
                        else
                        {
                            if (currentstate == CharacterState.Turn)
                                currentstate = CharacterState.Walk;
                            if (spd > 0)
                            {
                                spd -= delta * 20;
                            }
                            else
                            {
                                spd = 0;
                            }
                        }
                    }
                    else
                    {
                        if (currentstate == CharacterState.Turn)
                            currentstate = CharacterState.Walk;
                        if (spd < 20)
                        {
                            spd += delta * 10;
                            if (spd > 20)
                            {
                                spd = 20;
                            }
                        }
                    }
                }
                else
                {
                    if(currentstate == CharacterState.Turn)
                        currentstate = CharacterState.Walk;
                    if (spd > 0)
                    {
                        spd -= delta * 5;
                    }
                    else
                    {
                        spd = 0;
                    }
                }
                if (currentstate != CharacterState.Spindash)
                {
                    if (spd == 0)
                    {
                        movementAngle = Quaternion.LookRotation(input.joystick).y;
                        spd = 0.25f;
                    }
                    else
                    {
                        anim.SetFloat("Turn", -(movementAngle - Quaternion.Slerp(Quaternion.Euler(0, movementAngle, 0), Quaternion.LookRotation(input.joystick), scaledTurnSpeed * delta).eulerAngles.y) / 30 + 0.5f);
                        if (currentstate != CharacterState.Turn)
                            movementAngle = Quaternion.Slerp(Quaternion.Euler(0, movementAngle, 0), Quaternion.LookRotation(input.joystick), scaledTurnSpeed * delta).eulerAngles.y;
                    }
                }
                else
                {
                    if(input.joystick.magnitude > 0)
                    {
                        chargeAngle = Quaternion.LookRotation(input.joystick).eulerAngles.y;
                    }
                }
            }
            else
            {
                if(currentstate == CharacterState.Turn)
                {
                    currentstate = CharacterState.Walk;
                }
                if(spd != 0)
                {
                    if (spd > 0)
                    {
                        spd -= delta * 15;
                        if (spd < 0)
                        {
                            spd = 0;
                        }
                    }
                    else
                    {
                        spd += delta * 15;
                        if (spd > 0)
                        {
                            spd = 0;
                        }
                    }
                }
            }
            anim.SetFloat("ASpd", runSpeedCurve.Evaluate(Mathf.Abs(spd) / 20) + (Mathf.Abs(spd) / 20));
            if (currentstate == CharacterState.Spin)
            {
                incline = incline * 2f;
            }
            spd += incline / 1.75f / 90 * SlopeForce * (delta / 0.01666667f);
            bool grnded = false;
            bool wasgrounded = grounded;
            for (int i = 0; i < steps; i++)
            {
                CheckGrounded(); //applies the airspd if nesesary and resolves an unresolved collisions
                if (grounded)
                {
                    grnded = true;
                }
                transform.position += airspd * delta / steps;
                CheckGrounded(); //resolves any collisions resulting from our movement
                if (grounded)
                {
                    grnded = true;
                }
                CheckWalls();
            }
            grounded = grnded;
            if (input.A.wasPressedThisTick && scriptLock == 0)
            {
                airspd += transform.up * 12.5f; transform.position += airspd * delta;
                transform.position += airspd/100;
                grounded = false;
                currentstate = CharacterState.Spin;
            }
        }
        else
        {
            roff = 0;
            LastUp = Vector3.up;
            if(currentstate == CharacterState.Spin)
            {
                if (input.B.wasPressedThisTick)
                {
                    currentstate = CharacterState.Walk;
                    airspd = Vector3.zero;
                }
            }
            if (HasBadge(Badge.LightweightShoes) && airspd.y < -40)
            {
                airspd.y = -40;
            }
            if (currentstate == CharacterState.Spindash)
            {
                currentstate = CharacterState.Walk;
            }
            if((currentstate == CharacterState.Spin || currentstate == CharacterState.Sprung) && !grounded)
            {
                HomingTarget tar = FindHomingTarget(transform.position + (model.forward*3.5f) + (transform.forward));
                if(currentstate == CharacterState.Sprung && scriptLock > 0)
                {
                    tar = FindHomingTarget(transform.position + (model.up * 5f));
                }
                if (HasBadge(Badge.LightweightShoes))
                {
                    reticle.gameObject.SetActive(false);
                }
                if (input.A.wasPressedThisTick && !(scriptLock > 0 && currentstate == CharacterState.Spin))
                {
                    if (HasBadge(Badge.BlueShoes))
                    {
                        if (HasBadge(Badge.LightweightShoes) && currentstate != CharacterState.Sprung)
                        {
                            currentstate = CharacterState.Sprung;
                            airspd.y = 15;
                            if (input.joystick.magnitude > 0)
                                airspd = input.joystick * new Vector3(airspd.x, 0, airspd.z).magnitude + (Vector3.up * airspd.y);
                        }
                        else
                        {
                            if (tar != null && reticle.gameObject.activeSelf)
                            {
                                airspd = (tar.transform.position + tar.offset - transform.position).normalized * 40;
                                currentstate = CharacterState.Spin;
                                model.localRotation = Quaternion.Euler(0, movementAngle + roff, 0);
                                scriptLock = 1;
                                tar.disableTimer = 1;
                            }
                            else if (currentstate != CharacterState.Sprung)
                            {
                                currentstate = CharacterState.Walk;
                                airspd.y = 15;
                                if (input.joystick.magnitude > 0)
                                    airspd = input.joystick * new Vector3(airspd.x, 0, airspd.z).magnitude * 1.2f + (Vector3.up * airspd.y);
                            }
                        }
                    }
                    else
                    {
                        if (HasBadge(Badge.LightweightShoes) && currentstate != CharacterState.Sprung)
                        {
                            currentstate = CharacterState.Walk;
                            if (input.joystick.magnitude > 0)
                                model.rotation = Quaternion.LookRotation(input.joystick);
                            airspd = model.forward * 25;
                            airspd.y = 15;
                        }
                        else
                        {
                            if (tar != null && reticle.gameObject.activeSelf)
                            {
                                airspd = (tar.transform.position + tar.offset - transform.position).normalized * 40;
                                currentstate = CharacterState.Spin;
                                model.localRotation = Quaternion.Euler(0, movementAngle + roff, 0);
                                scriptLock = 1;
                                tar.disableTimer = 1;
                            }
                            else if (currentstate != CharacterState.Sprung)
                            {
                                currentstate = CharacterState.Walk;
                                if (input.joystick.magnitude > 0)
                                    model.rotation = Quaternion.LookRotation(input.joystick);
                                airspd = model.forward * 20 + (Vector3.up * airspd.y);
                                if (airspd.y < 0)
                                {
                                    airspd.y = 0;
                                }
                            }
                        }
                    }
                }
            }
            incline = 0;
            transform.rotation = Quaternion.identity;
            Vector3 grav = Physics.gravity;
            if (((currentstate == CharacterState.Spin) || currentstate == CharacterState.Sprung)  && airspd.y > 0)
            {
                if(input.A.pressed || currentstate == CharacterState.Sprung)
                    grav = grav / 2;
                if(scriptLock == 0 && !HasBadge(Badge.BlueShoes))
                {
                    Vector3 decel = new Vector3(airspd.x, 0, airspd.z);
                    decel /= ((1.015f - 1.0f) / frameMultiplier) + 1.0f;
                    airspd = new Vector3(decel.x, airspd.y, decel.z);
                }
            }
            airspd += grav * delta;
            if(scriptLock == 0 && currentstate != CharacterState.Stun)
            {
                airspd += input.joystick * delta * 20;
                if (new Vector2(airspd.x + input.joystick.x, airspd.z + input.joystick.z).magnitude < new Vector2(airspd.x, airspd.z).magnitude)
                {
                    airspd += input.joystick * delta * 80;
                }
            }
            if (new Vector2(airspd.x, airspd.z).magnitude > 20)
            {
                if (scriptLock == 0 && currentstate != CharacterState.Stun)
                {
                    Vector3 decel = new Vector3(airspd.x, 0, airspd.z);
                    decel /= ((1.01f - 1.0f) / frameMultiplier) + 1.0f;
                    airspd = new Vector3(decel.x, airspd.y, decel.z);
                }
            }
            bool grnded = false;
            float incln = 0;
            for (int i = 0; i < steps; i++)
            {
                CheckCeil();
                CheckGrounded(); //applies the airspd if nesesary and resolves an unresolved collisions
                if (grounded)
                {
                    grnded = true;
                    incln = incline;
                }
                transform.position += airspd * delta / steps;
                CheckGrounded(); //resolves any collisions resulting from our movement
                if (grounded)
                {
                    grnded = true;
                    incln = incline;
                }
                CheckWalls();
            }
            grounded = grnded;
            incline = incln;
            spd = new Vector2(airspd.x, airspd.z).magnitude;
            if (spd > 0)
            {
                movementAngle = Quaternion.LookRotation(new Vector3(airspd.x, 0, airspd.z)).eulerAngles.y;
            }
            if (grounded) //landed
            {
                LastUp = transform.up;
                scriptLock = 0;
                currentstate = CharacterState.Walk;
                if (incline > -121 && incline <= -45)
                {
                    spd = -airspd.y;
                }
                if(incline < -45 && incline < 0)
                {
                    spd = airspd.y * 0.5f * -Mathf.Sign(Mathf.Sin(incln * Mathf.Deg2Rad));
                }
                if(incline > -45 && incline < 45)
                {
                    spd = new Vector2(airspd.x, airspd.z).magnitude;
                }
                if(incline > 45)
                {
                    spd = airspd.y * 0.5f * Mathf.Sign(Mathf.Sin(incln * Mathf.Deg2Rad));
                }
                if(incline > 90)
                {
                    spd = airspd.y;
                }
            }
            if(input.joystickRaw.magnitude > 0 && currentstate != CharacterState.Stun)
            {
                model.rotation = Quaternion.Lerp(model.rotation, Quaternion.LookRotation(input.joystick), delta*15);
            }
            if(currentstate == CharacterState.Stun)
            {
                model.rotation = Quaternion.LookRotation(new Vector3(-airspd.x, 0, -airspd.z));
            }
        }
        
        anim.SetFloat("Spd", Mathf.Abs(spd));
        anim.SetBool("Grounded", grounded);
        anim.SetInteger("State", (int)currentstate);
        anim.SetFloat("Ysp", airspd.y);
        normModel.SetActive(currentstate != CharacterState.Spin && currentstate != CharacterState.Spindash);
        rollModel.SetActive(currentstate == CharacterState.Spin || currentstate == CharacterState.Spindash);
        rollModel.transform.localRotation = Quaternion.Euler(0, -90, -Time.time * 360 * 5);
        ballModel.transform.localRotation = Quaternion.Euler(0, -90, -Time.time * 360 * 5);
        ballModel.SetActive((currentstate == CharacterState.Spin || currentstate == CharacterState.Spindash) && Mathf.Sin(Time.time * 360 * 20 * Mathf.Deg2Rad) > 0);
        if (cameraTransform != null && localPlayer)
        {
            moveCamera();
        }
        if(currentstate == CharacterState.Spindash)
        {
            model.localScale = new Vector3(0.8f, 1.2f, 0.8f);
            model.localRotation = Quaternion.Euler(35, chargeAngle + roff, 0);
        }
        else
        {
            model.localScale = Vector3.one;
        }
        Vector3 castRot = -transform.up;
        if (currentstate == CharacterState.Sprung && scriptLock > 0)
        {
            castRot = Vector3.down;
            transform.rotation = Quaternion.LookRotation(airspd, Vector3.up);
            model.localRotation = Quaternion.Euler(90, 0, 0);
            anim.SetFloat("Ysp", 1);
        }
        RaycastHit hit;
        Vector3 raycastOrigin = transform.position + (transform.up * height);
        if (Physics.Raycast(raycastOrigin, castRot, out hit, 256, GroundMask))
        {
            ShadowCaster.parent.position = hit.point;
            ShadowCaster.parent.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            ShadowCaster.localRotation = Quaternion.Euler(0, model.localRotation.eulerAngles.y, 0);
        }
        else
        {
            ShadowCaster.parent.position = Vector3.up * -10000;
            ShadowCaster.parent.rotation = Quaternion.identity;
            ShadowCaster.localRotation = Quaternion.Euler(0, 0, 180);
        }
    }
    public float distance = 5.0f;
    public float mouseSensitivity = 100.0f;
    public float controllerSensitivity = 2.0f; // Adjust this value for controller sensitivity
    public float maxYAngle = 80.0f;
    public float zoomSpeed = 2.0f;
    public float minDistance = 1.0f;
    public float maxDistance = 10.0f;

    private float xRotation = 0.0f;
    private float yRotation = 0.0f;
    public bool canzoom;
    public Transform cameraTransform;

    public bool mouseLock = true;
    public float speedDist;
    public float zoomDist;
    public float cameraImpact;
    private void moveCamera()
    {
        cameraImpact += delta / 2;
        speedDist = Mathf.Lerp(speedDist, ((airspd.magnitude + (charge/2)) / 100 + 1), delta * 5);
        zoomDist = Mathf.Lerp(zoomDist, distance, delta * 5);
        if(zoomDist < 1)
        {
            speedDist = 0;
            normModel.SetActive(false);
            rollModel.SetActive(false);
            ballModel.SetActive(false);
        }
        cameraTransform.parent.position = Vector3.Lerp(cameraTransform.parent.position, transform.position, cameraImpact);
        cameraTransform.parent.rotation = Quaternion.Lerp(cameraTransform.parent.rotation, transform.rotation, delta * 10);
        if (Mouse.current.middleButton.wasPressedThisFrame) //soinc utopia style mouse locking
        {
            mouseLock = !mouseLock; //toggle mouselock
            if (mouseLock)
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
            }
        }
        if (mouseLock)
        {
            float controllerX;
            float controllerY;
            if (input.input.devices[0].name == "Keyboard")
            {
                controllerX = input.look.x * mouseSensitivity;
                controllerY = input.look.y * mouseSensitivity;
            }
            else
            {
                controllerX = input.look.x * controllerSensitivity;
                controllerY = input.look.y * controllerSensitivity;
            }

            // Update the Y rotation based on the input
            yRotation += controllerX;
            // Calculate the X rotation based on the input and clamp it within the specified range
            xRotation -= controllerY;


            xRotation = Mathf.Clamp(xRotation, -maxYAngle, maxYAngle);
        }
        // Create a Quaternion based on the X and Y rotations
        Quaternion rotation = Quaternion.Euler(xRotation, yRotation, 0f);

        // Zoom in/out with the scroll wheel
        if (canzoom)
        {
            float scrollWheel = Mouse.current.scroll.ReadValue().y;
            distance -= scrollWheel * zoomSpeed;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);// Calculate the desired camera position
        }

        Vector3 desiredCameraPosition = ((Vector3.up * CamOffset.y)) - rotation * Vector3.forward * speedDist * zoomDist;
        cameraTransform.transform.localRotation = rotation;

        RaycastHit hit;
        Vector3 playerEyePosition = transform.position + (transform.up * CamOffset.y);
        if (Physics.Raycast(playerEyePosition, -cameraTransform.forward, out hit, distance, GroundMask))
        {
            // If the ray hits an obstacle, set the camera position to the hit point
            desiredCameraPosition = ((Vector3.up * CamOffset.y)) - rotation * Vector3.forward * hit.distance;
        }
        cameraTransform.localPosition = desiredCameraPosition;
    }
    public void OnEditor()
    {
        if(transform.position.y < -100)
        {
            transform.position = Vector3.zero;
            airspd = Vector3.zero;
            spd = 0;
        }
    }
    public HomingTarget FindHomingTarget(Vector3 position)
    {
        Transform closest = null;
        HomingTarget tar = null;
        Vector3 off = Vector3.zero;
        float closestDistance = Mathf.Infinity;

        foreach (HomingTarget target in FindObjectsOfType<HomingTarget>())
        {
            if (target.disableTimer <= 0)
            {
                if (target.gameObject != gameObject)
                {
                    float distance = Vector3.Distance(position, target.transform.position + target.offset);

                    if (distance < closestDistance)
                    {
                        tar = target;
                        off = target.offset;
                        closestDistance = distance;
                        closest = target.transform;
                    }
                }
            }
        }
        if (closestDistance < 8)
        {
            reticle.gameObject.SetActive(true);
            reticle.position = closest.position + off;
        }
        else
        {
            reticle.gameObject.SetActive(false);
        }
        return tar;
    }
    public bool HasBadge(Badge badge)
    {
        return badge1 == badge || badge2 == badge || badge3 == badge;
    }
    private void OnTriggerEnter(Collider other)
    {
        HedgehogController obj = other.GetComponent<HedgehogController>();
        if (obj != null)
        {
            if(currentstate == CharacterState.Spin || currentstate == CharacterState.Spindash)
            {
                if (obj.currentstate == CharacterState.Spin || obj.currentstate == CharacterState.Spindash)
                { //both spinning
                    obj.airspd = -(transform.position - obj.transform.position).normalized * 20;
                    airspd = -(obj.transform.position - transform.position).normalized * 20;
                }
                else
                { //you're spinning but he's not
                    currentstate = CharacterState.Spin;
                    if (!grounded)
                    {
                        airspd = Vector3.up * 15;
                        scriptLock = 0f;
                    }
                    obj.airspd = -(transform.position - obj.transform.position).normalized * 20;
                    obj.airspd.y =  10;
                    obj.currentstate = CharacterState.Stun;
                    obj.grounded = false;
                    obj.transform.position += Vector3.up * 0.1f;
                }
            }
            else
            {
                if (obj.currentstate == CharacterState.Spin || obj.currentstate == CharacterState.Spindash)
                { //he's spinning but you're not
                    obj.currentstate = CharacterState.Spin;
                    if (!grounded)
                    {
                        airspd = Vector3.up * 15;
                        scriptLock = 0f;
                    }
                    airspd = (transform.position - obj.transform.position).normalized * 20;
                    airspd.y = 10;
                    currentstate = CharacterState.Stun;
                    grounded = false;
                    transform.position += Vector3.up * 0.1f;
                }
                //both aren't spinning
            }
        }
    }
}