using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Realtime;
using Photon.Pun;

public class Arrow : MonoBehaviourPun
{
    int damage;
    int launchDirection;
    public int speed;
    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Enemy"))
        {
            Enemy enemy = other.gameObject.GetComponent<Enemy>();
            enemy.photonView.RPC("TakeDamage", RpcTarget.MasterClient, damage);
            photonView.RPC("DestroyArrow", RpcTarget.All, 0f);
        }
    }

    void OnCollisionEnter()
    {

    }
    public void Fire(int damage, Vector3 dir)
    {
        Rigidbody2D rig = this.GetComponent<Rigidbody2D>();
        this.damage = damage;
        //transform.forward = dir;
        float rotZ = Mathf.Atan2(dir.y, dir.x) * 180/Mathf.PI + 90;
        Debug.Log(rotZ);
        transform.rotation = Quaternion.Euler(new Vector3(0,0,rotZ));
        //transform.rotation = new Quaternion(0,0,rotZ,1);
        rig.velocity = speed * dir;
        photonView.RPC("DestroyArrow", RpcTarget.All, 1f);
    }

    // Update is called once per frame
    [PunRPC]
    public void DestroyArrow(float timeToDestroy)
    {
        Destroy(this.gameObject, timeToDestroy);
    }
}
