// Author : torano

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour {
    [SerializeField] private int howManyTsumsGeneratedFirst = 70;
    [SerializeField] private float tsumPosY;
    [SerializeField] private float gameAreaWidth;
    [SerializeField] private LayerMask mask;
    [SerializeField] private float maxDistanceBtwTsums;
    [SerializeField] private GameObject explosion;

    [SerializeField] GameObject diamond;
    [SerializeField] int diamondProbability;
    [SerializeField] GameObject goldCoin;
    [SerializeField] int goldCoinProbability;
    [SerializeField] GameObject silverCoin;
    [SerializeField] int silverCoinProbability;
    [SerializeField] GameObject bomb;
    [SerializeField] int bombProbability;

    [SerializeField] Text scoreText;
    [SerializeField] Text timeText;
    [SerializeField] Text title;
    [SerializeField] Text result;
    [SerializeField] Button startButton;
    [SerializeField] Button retryButton;
    [SerializeField] Button tweetButton;

    [SerializeField] private AudioClip dragBombAudio;
    [SerializeField] private AudioClip dragCoinAudio;
    [SerializeField] private AudioClip dragDiamondAudio;
    [SerializeField] private AudioClip comboAudio;
    [SerializeField] private AudioClip nonComboAudio;
    [SerializeField] private AudioClip explosionAudio;

    public List<GameObject> dragedTsums;
    private List<GameObject> tsumsInsidePolygon;
    private GameObject firstDragedTsum;
    private GameObject lastDragedTsum;
    private int indexOfFinalDragedTsum;
    private bool closedCircle;
    private bool isCollided;
    private bool isPlaying;
    private int cnt = 0;
    private AudioSource audioSource;
    [SerializeField] private float timeCount;


    public static bool brokenTsum;
    public static bool isExploded;
    public static int howManyExploded;
    public static int score = 0;
    
    public void StartGame() {
        isPlaying = true;
        Initialize();

        title.gameObject.SetActive(false);
        startButton.gameObject.SetActive(false);
        retryButton.gameObject.SetActive(true);
    }

    public void RetryGame() {
        brokenTsum = false;
        isExploded = false;
        score = 0;

        SceneManager.LoadScene("GoldTsumTsum");
    }

    public void TweetResult() {
        naichilab.UnityRoomTweet.Tweet("tomiwotsumtsum", score + "円の富を稼ぎました。", "unityroom", "unity1week");
    }

    private void Initialize() {
        StartCoroutine("GenerateTsums", howManyTsumsGeneratedFirst);
        dragedTsums = new List<GameObject>();
        tsumsInsidePolygon = new List<GameObject>();
        audioSource = GetComponent<AudioSource>();
        scoreText.text = score + "円";
        timeText.text = "残り時間:" + (int)timeCount;

        if (diamondProbability + goldCoinProbability + silverCoinProbability + bombProbability != 100) {
            Debug.Log("Make sure that the sum of the probability is 100.");
        }
    }

    public IEnumerator GenerateTsums(int howMany) {
        for (int i = 0; i < howMany; i++) {
            int randomValue = Random.Range(0, 100);
            GameObject obj;

            if (randomValue < diamondProbability) {
                obj = diamond;
            } else if (randomValue < diamondProbability + goldCoinProbability) {
                obj = goldCoin;
            } else if (randomValue < diamondProbability + goldCoinProbability + silverCoinProbability) {
                obj = silverCoin;
            } else {
                obj = bomb;
            }
            GameObject tsum = Instantiate(obj);
            Vector3 pos = tsum.transform.position;
            pos.y = tsumPosY;
            pos.x = Random.Range(gameAreaWidth / 2.0f * (-1.0f), gameAreaWidth / 2.0f);
            tsum.transform.position = pos;
            tsum.GetComponent<Rigidbody2D>().AddForce(Vector2.down * 250.0f);

            yield return new WaitForSeconds(0.1f);
        }
    }

    private void Update() {
        if (isPlaying) {
            if (Input.GetMouseButtonDown(0)) {
                StartDrag();
            } else if (Input.GetMouseButton(0)) {
                OnDragging();
            } else if (Input.GetMouseButtonUp(0)) {
                FinishDrag(indexOfFinalDragedTsum);
            }

            if (isExploded) {
                audioSource.clip = explosionAudio;
                audioSource.Play();
                isExploded = false;
                StartCoroutine("GenerateTsums", howManyExploded);
            }

            timeCount -= Time.deltaTime;

            if (timeCount < 0) {
                isPlaying = false;
                result.text = "あなたは" + score + "円、富を積みました。";
                result.gameObject.SetActive(true);
                tweetButton.gameObject.SetActive(true);
                naichilab.RankingLoader.Instance.SendScoreAndShowRanking(score);
                timeCount = 0;
            }
            timeText.text = "残り時間:" + (int)timeCount;
        }
    }

    private void StartDrag() {
        Collider2D hitCollider = GetHitCollider();
        if (hitCollider != null) {
            PushTsumToList(hitCollider.gameObject);
        }
    }

    private void OnDragging() {
        if (brokenTsum) {
            brokenTsum = false;

            dragedTsums.Clear();
            return;
        }

        Collider2D hitCollider = GetHitCollider();
        if (hitCollider != null && lastDragedTsum != null && hitCollider.gameObject != lastDragedTsum && closedCircle == false) {
            float distance = Vector2.Distance(lastDragedTsum.transform.position, hitCollider.transform.position);

            if (distance <= maxDistanceBtwTsums) {
                indexOfFinalDragedTsum = dragedTsums.IndexOf(hitCollider.gameObject);

                if (indexOfFinalDragedTsum > -1) {
                    if (dragedTsums.Count - indexOfFinalDragedTsum > 2) {
                        closedCircle = true;
                        
                    } else {
                        dragedTsums.Remove(lastDragedTsum.gameObject);
                        ChangeColor(lastDragedTsum, 1.0f);
                        lastDragedTsum = hitCollider.gameObject;
                    }
                } else {
                    PushTsumToList(hitCollider.gameObject);
                }
            }

        }
    }

    private void FinishDrag(int num) {

        if (num > -1 && dragedTsums.Count - num > 2) {
                List<Vector2> surroundingTsumsList = new List<Vector2>();

            for (int i = 0; i < dragedTsums.Count; i++) {
                if (i < num) {
                    ChangeColor(dragedTsums[i], 1.0f);
                } else {
                    if (dragedTsums[i] != null) {
                        surroundingTsumsList.Add(new Vector2(dragedTsums[i].transform.position.x, dragedTsums[i].transform.position.y));
                    }
                }
            }

            GeneratePolygon(surroundingTsumsList);
            surroundingTsumsList.Clear();
        } else {
            if (dragedTsums.Count > 0) {
                for (int i = 0; i < dragedTsums.Count; i++) {
                    if (dragedTsums[i] != null) {
                        ChangeColor(dragedTsums[i], 1.0f);
                    }
                }
            }
        }

        closedCircle = false;
        
        dragedTsums.Clear();
    }

    private void PushTsumToList(GameObject tsum) {
        lastDragedTsum = tsum;
        ChangeColor(tsum, 0.5f);
        dragedTsums.Add(tsum);
        if (tsum.tag == "Bomb") {
            audioSource.clip = dragBombAudio;
        } else if (tsum.tag == "GoldenCoin" || tsum.tag == "SilverCoin") {
            audioSource.clip = dragCoinAudio;
        } else if (tsum.tag == "Diamond") {
            audioSource.clip = dragDiamondAudio;
        }

        audioSource.Play();
    }

    private Collider2D GetHitCollider() {
        RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero, mask);
        return hit.collider;
    }

    private void GeneratePolygon(List<Vector2> positions) {
        Vector2[] surroundingTsumsArray = positions.ToArray();
        gameObject.AddComponent<PolygonCollider2D>();
        gameObject.GetComponent<PolygonCollider2D>().isTrigger = true;
        gameObject.GetComponent<PolygonCollider2D>().pathCount = positions.Count;
        gameObject.GetComponent<PolygonCollider2D>().points = surroundingTsumsArray;
    }

    private void ChangeColor(GameObject obj, float transparency) {
        SpriteRenderer tsumTexture = obj.GetComponent<SpriteRenderer>();
        tsumTexture.color = new Color(tsumTexture.color.r, tsumTexture.color.g, tsumTexture.color.b, transparency);
    }

    private void FixedUpdate() {
        if (isCollided && isPlaying) {
            bool combo = true;
            bool isBomb = false;
            GameObject empty = new GameObject();
            int addedScore = 0;

            foreach (GameObject tsum in tsumsInsidePolygon) {
                tsum.transform.parent = empty.transform;

                if (tsum.tag == "Bomb") {
                    if (addedScore > 0) {
                        addedScore = 0;
                    }

                    addedScore -= 5000;
                    combo = false;
                    isBomb = true;

                    GameObject explosionInstance = Instantiate(explosion);
                    explosionInstance.transform.position = tsum.transform.position;
                    audioSource.clip = explosionAudio;
                    audioSource.Play();
                }

                if (isBomb) {
                    continue;
                } else if (tsum.tag == "GoldenCoin") {
                    addedScore += 1000;
                } else if (tsum.tag == "SilverCoin") {
                    addedScore += 500;
                } else if (tsum.tag == "Diamond") {
                    addedScore += 30000;
                }

                if (tsum.tag != lastDragedTsum.tag) {
                    combo = false;
                }
            }

            if (combo) {
                audioSource.clip = comboAudio;
                audioSource.Play();

                addedScore *= tsumsInsidePolygon.Count;
            } else if (!isBomb) {
                audioSource.clip = nonComboAudio;
                audioSource.Play();
            }

            Destroy(GetComponent<PolygonCollider2D>());
            isCollided = false;
            StartCoroutine("GenerateTsums", cnt);
            cnt = 0;
            score += addedScore;
            scoreText.text = score + "円";
            Destroy(empty);
            

            tsumsInsidePolygon.Clear();
        }
    }

    private void OnTriggerEnter2D(Collider2D collider) {
        if (!isCollided) {
            isCollided = true;
        }

        tsumsInsidePolygon.Add(collider.gameObject);
        cnt++;
    }
}
