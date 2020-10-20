using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
public enum WeaponType
{
    melee,
    bow
}
public class PlayerController : MonoBehaviourPun
{
    [HideInInspector]
    public int id;

    [Header("Info")]
    public float moveSpeed;
    public int gold;
    public int curHP;
    public int maxHP;
    public bool dead;

    [Header("Attack")]
    public GameObject[] weapons;
    public WeaponType curWeaponType;
    public int meleeDamage;
    public int arrowDamage;
    public float meleeAttackRange;
    public float attackRate;
    private float lastAttackTime;

    [Header("Components")]
    public Rigidbody2D rig;
    public Player photonPlayer;
    public SpriteRenderer sr;
    public string arrowPrefab;
    public Animator meleeWeaponAnim;
    public HeaderInfo headerInfo;

    //Local player
    public static PlayerController thisPlayer;
    // Start is called before the first frame update

    [PunRPC]
    public void Initialize(Player player)
    {
        id = player.ActorNumber;
        photonPlayer = player;
        headerInfo.Initialize(player.NickName, maxHP);
        if (player.IsLocal)
            thisPlayer = this;
        else
            rig.isKinematic = true;

        GameManager.instance.players[id - 1] = this; 
    }

    // Update is called once per frame
    void Update()
    {
        if (!photonView.IsMine)
            return;
        Move();
        if (Input.GetMouseButtonDown(0) && Time.time - lastAttackTime > attackRate)
            Attack();
        if (Input.GetKeyDown(KeyCode.E))
        {
            photonView.RPC("SwapWeapons", RpcTarget.All);
        }

        // flip the player sprite if the mouse crosses half way point on screen;
        float mouseX = (Screen.width / 2 - Input.mousePosition.x);
        if (mouseX > 0)
            meleeWeaponAnim.transform.parent.localScale = new Vector3(-1, 1, 1);
        else
            meleeWeaponAnim.transform.parent.localScale = new Vector3(1, 1, 1);
    }

    void Move()
    {
        float x = Input.GetAxis("Horizontal");
        float y = Input.GetAxis("Vertical");
        rig.velocity = new Vector2(x, y) * moveSpeed;
    } 
    void Attack()
    {
        lastAttackTime = Time.time;
        // Get a direction vector based on mouse position
        Vector3 dir = (Input.mousePosition - Camera.main.WorldToScreenPoint(transform.position)).normalized;
        if (curWeaponType == WeaponType.melee)
            meleeAttack(dir);
        else
            photonView.RPC("BowAttack", RpcTarget.All, arrowDamage, dir);
        
    }

    void meleeAttack(Vector3 dir)
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position + dir, dir, meleeAttackRange);
        if (hit.collider != null && hit.collider.gameObject.CompareTag("Enemy"))
        {
            //TO DO: Reference enemy and damage them
            Enemy enemy = hit.collider.GetComponent<Enemy>();
            enemy.photonView.RPC("TakeDamage", RpcTarget.MasterClient, meleeDamage);
        }

        //play animaton
        meleeWeaponAnim.SetTrigger("Attack");
    }

    [PunRPC]
    public void BowAttack(int arrowDamage, Vector3 arrowDir)
    {
        GameObject shotArrowGO = PhotonNetwork.Instantiate(arrowPrefab, transform.position + arrowDir, Quaternion.identity);
        Arrow shotArrow = shotArrowGO.GetComponent<Arrow>();
        shotArrow.Fire(arrowDamage, arrowDir);
    }

    [PunRPC]
    public void SwapWeapons()
    {
        if (curWeaponType == WeaponType.bow)
        {
            weapons[1].SetActive(false);
            weapons[0].SetActive(true);
            curWeaponType = WeaponType.melee;
        }
        else
        {
            weapons[0].SetActive(false);
            weapons[1].SetActive(true);
            curWeaponType = WeaponType.bow;
        }
    }
    [PunRPC]
    public void TakeDamage(int damage)
    {
        curHP -= damage;
        //Debug.Log(curHP);
        headerInfo.photonView.RPC("UpdateHealthBar", RpcTarget.All, curHP);
        if (curHP <= 0)
        {
            
            Die();
        }
        else
        {
            // Make the player flash red when taking damage;
            StartCoroutine(DamageFlash());
            IEnumerator DamageFlash()
            {
                sr.color = Color.red;
                yield return new WaitForSeconds(0.05f);
                sr.color = Color.white;
            }
        }
    }
    void Die()
    {
        dead = true;
        rig.isKinematic = true;
        transform.position = new Vector3(0, 99, 0);
        Vector3 spawnPos = GameManager.instance.spawnPoints[Random.Range(0, GameManager.instance.spawnPoints.Length)].position;
        StartCoroutine(Spawn(spawnPos, GameManager.instance.respawnTime));
    }

    IEnumerator Spawn(Vector3 spawnPos, float timeToSpawn)
    {
        yield return new WaitForSeconds(timeToSpawn);

        dead = false;
        transform.position = spawnPos;
        curHP = maxHP;
        rig.isKinematic = false;

        headerInfo.photonView.RPC("UpdateHealthBar", RpcTarget.All, curHP);
    }

    [PunRPC]
    void Heal (int amountToHeal)
    {
        curHP = Mathf.Clamp(curHP + amountToHeal, 0, maxHP);
        //TO DO: Update health bar
        headerInfo.photonView.RPC("UpdateHealthBar", RpcTarget.All, curHP);
    }

    [PunRPC]
    void GiveGold (int goldToGive)
    {
        gold += goldToGive;
        // TO DO: update UI
        GameUI.instance.UpdateGoldText(gold);
    }
}
