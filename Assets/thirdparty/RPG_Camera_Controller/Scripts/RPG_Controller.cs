using UnityEngine;
using KBEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class RPG_Controller : MonoBehaviour {

    public static RPG_Controller instance = null;
	
    public CharacterController characterController;
    public float walkSpeed = 10f;
    public float turnSpeed = 2.5f;
    public float jumpHeight = 10f;
    public float gravity = 20f;
    public float fallingThreshold = -6f; // -6f gets the character beeing almost always grounded
	public static bool enabled = false;
	
    private Vector3 playerDir;
    private Vector3 playerDirWorld;
    public static Vector3 rotation = new Vector3(0f, 0f, 0f);
	public static Vector3 initPos = new Vector3(0f, -99999999f, 0f);
	public static Vector3 initRot = new Vector3(0f, 0f, 0f);
	
	bool isShowAlert = false;
	
	int canControl = 0;
	
    void Awake() {
        instance = this;
        characterController = GetComponent("CharacterController") as CharacterController;
		
        if(initPos.y == -99999999f)
        {
			initPos.x = transform.position.x;
			initPos.y = transform.position.y;
			initPos.z = transform.position.z;
		}
    }

    void Start() {
		transform.position = initPos;
		transform.Rotate(initRot);
		this.transform.gameObject.layer = LayerMask.NameToLayer("kbentity");
		Common.DEBUG_MSG("RPG_Controller::Start: initPos=" + transform.position + "layer=" + LayerMask.LayerToName(transform.gameObject.layer));
		
		if(RPG_Animation.instance != null)
			Common.DEBUG_MSG("RPG_Controller::Start: rotation=" + RPG_Animation.instance.transform.rotation + ", rotation=" + initRot);
 
		Physics.IgnoreLayerCollision(LayerMask.NameToLayer("kbentity"), LayerMask.NameToLayer("kbentity"), true);
        RPG_Camera.CameraSetup();
    }
    
    void FixedUpdate () {
    	KBEngine.Entity player = KBEngineApp.app.player();

    	if(player != null)
    	{
	    	player.position.x = transform.position.x;
	    	player.position.y = transform.position.y;
	    	player.position.z = transform.position.z;
			
			if(RPG_Animation.instance != null)
	    		player.direction.z = RPG_Animation.instance.transform.rotation.eulerAngles.y;
	    	
	    	if(characterController != null)
	    	{
	    		player.isOnGround = characterController.isGrounded;
	    	}
    	}
    	
		if(WorldManager.currinst != null)
			WorldManager.currinst.Update();
    }
    
	void Update () {
        if (Camera.main == null)
		{
			Common.DEBUG_MSG("RPG_Controller::Update: not ready!");
            return;
		}
		
        if (characterController == null) {
            Common.DEBUG_MSG("Error: No Character Controller component found! Please add one to the GameObject which has this script attached.");
            return;
        }
		
		if(WorldManager.currinst != null)
		{
			WorldManager.ChunkPos currChunk = WorldManager.currinst.atChunk();
			if(currChunk.x >= 0 && currChunk.y >= 0)
			{
				if(WorldManager.currinst.loadedChunk(currChunk) == false)
				{
					canControl = 0;
					showAlert(true);
					Common.DEBUG_MSG("RPG_Controller::Update: wait for load(" + (currChunk.x + 1) + "," + (currChunk.y + 1) + "), currpos=" + transform.position);
					return;
				}
				
				if(WorldManager.currinst.loadedWorldObjsCamera() == false)
				{
					canControl = 0;
					showAlert(true);
					Common.DEBUG_MSG("RPG_Controller::Update: wait for load(worldObject)!");
					return;
				}
				
				if(canControl < 5)
				{
					canControl += 1;
					return;
				}
				
				if(enabled == false)
				{
					// Common.DEBUG_MSG("RPG_Controller::Update: enabled = false!");
			        if(isShowAlert == true)
			        {
			        	showAlert(false);
			        }
					return;
				}
				
				KBEngine.Entity player = KBEngineApp.app.player();
				if(player == null)
					return;

				walkSpeed = ((float)(Byte)player.getDefinedPropterty("moveSpeed")) / 10f; // 每秒速度
			}
		}
	
        GetInput();
        StartMotor();
        
        if(isShowAlert == true)
        {
        	showAlert(false);
        }
	}

	void showAlert(bool v)
	{
        if(isShowAlert == v)
        	return;

		isShowAlert = v;
		UnityEngine.GameObject mv_limit_log = UnityEngine.GameObject.Find("mv_limit_log");
		if(mv_limit_log != null)
			NGUITools.SetActive(mv_limit_log, v);
	}
	
    void GetInput() {
        
        //MovementKeys():
        
        float horizontalStrafe = 0f;
        float vertical = 0f;

        if (Input.GetButton("Horizontal Strafe"))
            horizontalStrafe = Input.GetAxis("Horizontal Strafe") < 0 ? -1f : Input.GetAxis("Horizontal Strafe") > 0 ? 1f : 0f;

        if (Input.GetButton("Vertical"))
            vertical = Input.GetAxis("Vertical") < 0 ? -1f : Input.GetAxis("Vertical") > 0 ? 1f : 0f;

        if (Input.GetMouseButton(0) && Input.GetMouseButton(1))
            vertical = 1f;
            
        playerDir = horizontalStrafe * Vector3.right + vertical * Vector3.forward;
        if (RPG_Animation.instance != null)
            RPG_Animation.instance.SetCurrentMoveDir(playerDir);

        if (characterController.isGrounded) {    
            playerDirWorld = transform.TransformDirection(playerDir);
            
            if (Mathf.Abs(playerDir.x) + Mathf.Abs(playerDir.z) > 1)
                playerDirWorld.Normalize();
            
            playerDirWorld *= walkSpeed;
            playerDirWorld.y = fallingThreshold;
            
            if (Input.GetButtonDown("Jump")) {
                playerDirWorld.y = jumpHeight;
                if (RPG_Animation.instance != null)
                    RPG_Animation.instance.Jump(); // the pattern for calling animations is always the same: just add some lines under line 77 and write an if statement which
            }                                      // checks for an arbitrary key if it is pressed and, if true, calls "RPG_Animation.instance.YourAnimation()". After that you add
        }                                          // this method to the other animation clip methods in "RPG_Animation" (do not forget to make it public) 

        rotation.y = Input.GetAxis("Horizontal") * turnSpeed;
    }


    void StartMotor() {
        playerDirWorld.y -= gravity * Time.deltaTime;
        characterController.Move(playerDirWorld * Time.deltaTime);
        transform.Rotate(rotation);
        if (!Input.GetMouseButton(0))
            RPG_Camera.instance.RotateWithCharacter();
        
        KBEngineApp.app.updatePlayerToServer();
    }
}
