// Author : torano

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombController : MonoBehaviour {
    [SerializeField] private GameObject centerObj;
    [SerializeField] private float radius;
    [SerializeField] private LayerMask mask;

    private AnimatorStateInfo animeInfo;
    private GameController gameController;
    private AudioSource audioSource;

    private void Start() {
        gameController = GameObject.Find("GameController").GetComponent<GameController>();
    }

    private void Update() {
        animeInfo = GetComponent<Animator>().GetCurrentAnimatorStateInfo(0);

        if (animeInfo.normalizedTime > 0.99f) {
            Explode();
        }
    }

    private void Explode() {

        Vector2 point = new Vector2(centerObj.transform.position.x, centerObj.transform.position.y);
        Collider2D[] colliders = Physics2D.OverlapCircleAll(point, radius, mask);

        if (colliders.Length > 2) {
            foreach (Collider2D collider in colliders) {
                if (collider.gameObject != this.gameObject) {
                    if (gameController.dragedTsums.IndexOf(collider.gameObject) > -1) {
                        GameController.brokenTsum = true;
                    }

                    Destroy(collider.gameObject);
                }
            }

            GameController.isExploded = true;
            GameController.howManyExploded = colliders.Length - 2;
        }

        Destroy(gameObject);
    }
}
