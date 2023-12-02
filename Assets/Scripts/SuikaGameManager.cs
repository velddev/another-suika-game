using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

public record MergeRequest 
{
    public Guid Item1 { get; set; }
    public Guid Item2 { get; set; }
}

public class SuikaGameManager : MonoBehaviour
{
    public GameObject[] AllObjects;
    public Camera MainCamera;

    public Queue<int> Queue = new();
    public List<SuikaItem> Items = new();
    
    private GameObject hoveredObject;
    private Queue<MergeRequest> mergeRequests = new();

    private float timeNow = 0;
    private float timeSinceLastMerge = 0;
    private int mergeCombo = 0;

    private int totalMerges = 0;
    private int totalScore = 0;
    
    private bool gameOver = false;
    private float timeGameOver = 0;

    [SerializeField]
    public Text ScoreLabel;
    [SerializeField]
    public Text MergeLabel;
    [SerializeField]
    public Text ComboLabel;

    public AudioSource AudioSource;
    public AudioSource ComboAudioSource;
    
    public AudioClip MergeSound;
    public AudioClip GameOverSound;
    public AudioClip GameOverSound2;
    public AudioClip ComboSound;
    
    public GameObject BottomBar;
    
    void Start()
    {
        MainCamera = GameObject.Find("Main Camera").GetComponent<Camera>();
        AudioSource = GetComponent<AudioSource>();
        PrepareQueue();
        SelectNextObject();
    }
    
    void Update()
    {
        UpdateHoverObject();
        ProcessMergeRequest();
        
        timeNow += Time.deltaTime;

        if (mergeCombo > 0)
        {
            if (timeNow - timeSinceLastMerge > 1)
            {
                mergeCombo = 0;
                ComboLabel.text = $"x{mergeCombo}";
            }
            
            var timeLeft = 1 - (timeNow - timeSinceLastMerge);
            var scale = Mathf.Lerp(1, 1.5f, timeLeft);
            ComboLabel.transform.localScale = new Vector3(scale, scale, scale);
            
            var color = Color.Lerp(Color.white, Color.red, timeLeft);
            ComboLabel.color = color;
        }

        if (gameOver)
        {
            var timeSinceGameOver = timeNow - timeGameOver;

            if (timeSinceGameOver > 1f && BottomBar.activeSelf)
            {
                AudioSource.PlayOneShot(GameOverSound2);
                BottomBar.SetActive(false);
            }
            
            if (timeSinceGameOver > 5f)
            {
                SceneManager.LoadScene("SampleScene");
            }
        }
    }

    void UpdateHoverObject()
    {
        if (gameOver)
        {
            return;
        }
        
        var worldPos = MainCamera.ScreenToWorldPoint(Input.mousePosition);
        worldPos.z = 0;
        worldPos.y = 2.5f;
        worldPos.x = Mathf.Clamp(worldPos.x, -2.1f, 2.1f);
        
        hoveredObject.transform.position = worldPos;

        if (Input.GetMouseButtonDown(0))
        {
            hoveredObject.GetComponent<SuikaItem>().AttachRigidbody();
            hoveredObject.GetComponent<CircleCollider2D>().isTrigger = false;
            hoveredObject = null;
         
            AudioSource.PlayOneShot(MergeSound);
            
            if (Queue.Count > 0)
            {
                SelectNextObject();
            }
        }
    }

    private void ProcessMergeRequest()
    {
        if (!mergeRequests.Any())
        {
            return;
        }
        
        var request = mergeRequests.Dequeue();
        var item1 = Items.FirstOrDefault(x => x.Id == request.Item1);
        var item2 = Items.FirstOrDefault(x => x.Id == request.Item2);
        if (item1 == null || item2 == null)
        {
            return;
        }

        if (item1.Type != item2.Type)
        {
            return;
        }
        
        var newItem = Instantiate(AllObjects[item1.Type + 1]);
        newItem.AddComponent<Rigidbody2D>();
        newItem.GetComponent<CircleCollider2D>().isTrigger = false;
        
        Destroy(item1.gameObject);
        Destroy(item2.gameObject);
        Items.Remove(item1);
        Items.Remove(item2);
        
        var newPos = (item1.transform.position + item2.transform.position) / 2;
        newPos.z = 0;
        
        newItem.transform.position = newPos;
        Items.Add(newItem.GetComponent<SuikaItem>());
        AddMerge(item1.Type);
        AudioSource.PlayOneShot(MergeSound, 0.5f);
    }
    
    public void RequestMerge(SuikaItem item1, SuikaItem item2)
    {
        mergeRequests.Enqueue(
            new MergeRequest
            {
                Item1 = item1.Id, 
                Item2 = item2.Id
            });
    }

    public void AddMerge(int score)
    {
        totalScore += score + (score * mergeCombo);
        totalMerges++;
        
        if(timeSinceLastMerge > timeNow - 1)
        {
            mergeCombo++;
            ComboAudioSource.pitch = 1 + (mergeCombo / 50f);
            ComboAudioSource.PlayOneShot(ComboSound, 0.5f);
        }
        
        timeSinceLastMerge = timeNow;
        
        ScoreLabel.text = $"Score: {totalScore}";
        MergeLabel.text = $"Merges: {totalMerges}";
        ComboLabel.text = $"x{mergeCombo}";
    }

    void SelectNextObject()
    {
        var nextObject = Queue.Dequeue();
        var newObject = Instantiate(AllObjects[nextObject]);
        
        var suikaItem = newObject.GetComponent<SuikaItem>();
        suikaItem.GameManager = this;
        Items.Add(suikaItem);

        hoveredObject = newObject;
        PushRandomToQueue();
    }
    void PushRandomToQueue()
    {
        Queue.Enqueue(Random.Range(0, 4));
    }
    void PrepareQueue()
    {
        for (var i = 0; i < 10; i++)
        {
            PushRandomToQueue();
        }
    }

    public void TriggerGameOver(SuikaItem suikaItem)
    {
        if (gameOver)
        {
            return;
        }
        
        AudioSource.PlayOneShot(GameOverSound);
        
        var allItems = Items.Where(x => x.Id != suikaItem.Id);
        foreach (var item in allItems)
        {
            item.GetComponent<SpriteRenderer>().color = Color.grey;
        }

        gameOver = true;
        timeGameOver = timeNow;
    }
}
