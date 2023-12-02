using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SuikaItem : MonoBehaviour
{
    public SuikaGameManager GameManager;
    public Guid Id = Guid.NewGuid();
    public int Type;
    
    private Rigidbody2D rigidbody2D;
    private float timeSinceActive;
    
    // Start is called before the first frame update
    void Start()
    {
        GameManager = Camera.main!.GetComponent<SuikaGameManager>();
    }

    private void Update()
    {
        if (rigidbody2D == null)
        {
            return;
        }
        
        if(rigidbody2D.velocity.magnitude < 0.0001f && transform.position.y > 1.7f && Time.time - timeSinceActive > 1f)
        {
            GameManager.TriggerGameOver(this);
        }
    }
    
    public void AttachRigidbody()
    {
        rigidbody2D = gameObject.AddComponent<Rigidbody2D>();
        timeSinceActive = Time.time;
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        var otherSuikaItem = other.gameObject.GetComponent<SuikaItem>();
        if (otherSuikaItem == null)
        {
            return;
        }
        
        if (otherSuikaItem.Type == Type)
        {
            GameManager.RequestMerge(this, other.gameObject.GetComponent<SuikaItem>());
        }
    }
}
