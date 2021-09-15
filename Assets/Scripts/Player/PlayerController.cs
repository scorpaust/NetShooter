using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PlayerController : MonoBehaviourPunCallbacks
{
    public Transform viewPoint;

    public float mouseSensitivity = 1f;

    public float moveSpeed = 5f, runSpeed = 8f;

    public float jumpForce = 5f;

    public float gravityMode = 2.5f;

    public bool invertLook;

    public Transform groundCheckPoint;

    public LayerMask groundLayer;

    public CharacterController charControl;

    public Gun[] allGuns;

    public GameObject bulletImpact;

    public GameObject playerHitImpact;

    // public float timeBetweenShots = 0.1f;

    public float maxHeat = 10f, /* heatPerShot = 1f,*/ coolRate = 4f, overheatCoolRate = 5f;

    public float muzzleDisplayTime;

    public int maxHealth = 100;

    public Animator anim;

    public GameObject playerModel;

    public Transform modelGunPoint;

    public Transform gunHolder;

    private int currentHealth;

    private float verticalRotStore;

    private Vector2 mouseInput;

    private Vector3 moveDir, movement;

    private float activeMoveSpeed;

    private Camera cam;

    private bool isGrounded;

    private float shotCounter;

    private float heatCounter;

    private float muzzleCounter;

    private bool overheated;

    private int selectedGun = 0;

	// Start is called before the first frame update
	void Start()
    {
        Cursor.lockState = CursorLockMode.Confined;

        cam = Camera.main;        

        Transform newTransf = SpawnManager.instance.GetSpawnPoint();

        transform.position = newTransf.position;

        transform.rotation = newTransf.rotation;

        currentHealth = maxHealth;

        photonView.RPC("SetGun", RpcTarget.All, selectedGun);

        if (photonView.IsMine)
		{
            playerModel.SetActive(false);

            UIController.instance.healthSlider.maxValue = maxHealth;

            UIController.instance.healthSlider.value = currentHealth;

            UIController.instance.weaponTempSlider.maxValue = maxHeat;
        } else
		{
            gunHolder.parent = modelGunPoint;

            gunHolder.localPosition = Vector3.zero;

            gunHolder.localRotation = Quaternion.identity;
		}

    }

    // Update is called once per frame
    void Update()
    {
        if (photonView.IsMine)
		{
            mouseInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * mouseSensitivity;

            transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y + mouseInput.x, transform.rotation.eulerAngles.z);

            verticalRotStore += mouseInput.y;

            verticalRotStore = Mathf.Clamp(verticalRotStore, -60f, 60f);

            if (invertLook)
            {
                viewPoint.rotation = Quaternion.Euler(verticalRotStore, viewPoint.rotation.eulerAngles.y, viewPoint.rotation.eulerAngles.z);
            }
            else
            {
                viewPoint.rotation = Quaternion.Euler(-verticalRotStore, viewPoint.rotation.eulerAngles.y, viewPoint.rotation.eulerAngles.z);
            }

            moveDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));

            if (Input.GetKey(KeyCode.LeftShift))
            {
                activeMoveSpeed = runSpeed;
            }
            else
            {
                activeMoveSpeed = moveSpeed;
            }

            float yVel = movement.y;

            movement = ((transform.forward * moveDir.z) + (transform.right * moveDir.x)).normalized * activeMoveSpeed;

            if (!charControl.isGrounded)
                movement.y = yVel;
            else
                movement.y = 0f;


            isGrounded = Physics.Raycast(groundCheckPoint.position, Vector3.down, 0.25f, groundLayer);

            if (Input.GetButtonDown("Jump") && isGrounded)
            {
                movement.y = jumpForce;
            }

            movement.y += Physics.gravity.y * Time.deltaTime * gravityMode;

            charControl.Move(movement * Time.deltaTime);

            if (allGuns[selectedGun].muzzleFlash.activeInHierarchy)
            {
                muzzleCounter -= Time.deltaTime;

                if (muzzleCounter <= 0)
                    allGuns[selectedGun].muzzleFlash.SetActive(false);
            }

            if (!overheated)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    Shoot();
                }

                if (Input.GetMouseButton(0) && allGuns[selectedGun].isAutomatic)
                {
                    shotCounter -= Time.deltaTime;

                    if (shotCounter <= 0f)
                    {
                        Shoot();
                    }
                }

                heatCounter -= coolRate * Time.deltaTime;

            }
            else
            {
                heatCounter -= overheatCoolRate * Time.deltaTime;

                if (heatCounter <= 0f)
                {
                    overheated = false;

                    UIController.instance.overheatedMessage.gameObject.SetActive(false);
                }
            }

            if (heatCounter < 0f)
                heatCounter = 0f;

            UIController.instance.weaponTempSlider.value = heatCounter;


            if (Input.GetAxisRaw("Mouse ScrollWheel") > 0f)
            {
                selectedGun++;

                if (selectedGun >= allGuns.Length)
                    selectedGun = 0;

                // SwitchGun();
                photonView.RPC("SetGun", RpcTarget.All, selectedGun);
            }
            else if (Input.GetAxisRaw("Mouse ScrollWheel") < 0f)
            {
                selectedGun--;

                if (selectedGun < 0)
                    selectedGun = allGuns.Length - 1;

                // SwitchGun();
                photonView.RPC("SetGun", RpcTarget.All, selectedGun);
            }

            for (int i = 0; i < allGuns.Length; i++)
            {
                if (Input.GetKeyDown((i + 1).ToString()))
                {

                    selectedGun = i;

                    // SwitchGun();
                    photonView.RPC("SetGun", RpcTarget.All, selectedGun);
                }
            }

            anim.SetBool("grounded", isGrounded);

            anim.SetFloat("speed", moveDir.magnitude);

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
            }
            else if (Cursor.lockState == CursorLockMode.None)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    Cursor.lockState = CursorLockMode.Locked;
                }
            }
        }
        
    }

    private void Shoot()
	{
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        ray.origin = cam.transform.position;

        if (Physics.Raycast(ray, out RaycastHit hit)) 
        {
            // Debug.Log("We hit " + hit.collider.gameObject.name);

            if (hit.collider.gameObject.CompareTag("Player"))
			{
                PhotonNetwork.Instantiate(playerHitImpact.name, hit.point, Quaternion.identity);

                hit.collider.gameObject.GetPhotonView().RPC("DealDamage", RpcTarget.All, photonView.Owner.NickName, allGuns[selectedGun].shotDamage);
			} 
            else
			{
                GameObject bulletImpactObject = Instantiate(bulletImpact, hit.point + (hit.normal * 0.002f), Quaternion.LookRotation(hit.normal, Vector3.up));

                Destroy(bulletImpactObject, 10f);
            }

        }

        shotCounter = allGuns[selectedGun].timeBetweenShots;

        heatCounter += allGuns[selectedGun].heatPerShot;

        if (heatCounter >= maxHeat)
		{
            heatCounter = maxHeat;

            overheated = true;

            UIController.instance.overheatedMessage.gameObject.SetActive(true);
		}

        allGuns[selectedGun].muzzleFlash.SetActive(true);

        muzzleCounter = muzzleDisplayTime;
	}

	private void LateUpdate()
	{
        if (photonView.IsMine)
		{
            cam.transform.position = viewPoint.position;

            cam.transform.rotation = viewPoint.rotation;
        }
	}

    private void SwitchGun()
	{
        foreach (Gun gun in allGuns)
            gun.gameObject.SetActive(false);

        allGuns[selectedGun].gameObject.SetActive(true);

        allGuns[selectedGun].muzzleFlash.SetActive(false);
	}

    [PunRPC]
    public void DealDamage(string damager, int damageAmount)
	{
        TakeDamage(damager, damageAmount);
	}

    [PunRPC]
    public void TakeDamage(string damager, int damageAmount)
	{
        if (photonView.IsMine)
		{

            currentHealth -= damageAmount;

            if (currentHealth <= 0)
			{
                currentHealth = 0;

                PlayerSpawner.instance.Die(damager);
            }

            UIController.instance.healthSlider.value = currentHealth;

            // Debug.Log(photonView.Owner.NickName + " has been hit by " + damager);            
        }
        
    }

    [PunRPC]
    public void SetGun(int GunToSwitchTo)
	{
        if (GunToSwitchTo < allGuns.Length)
		{
            selectedGun = GunToSwitchTo;

            SwitchGun();
		}
	}
}
