using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectSplash : MonoBehaviour
{
    private float spd = 0;
    private float a;
    private float b;
    private float rad;
    public float t = 0;
    private Animation animation1;

    void Awake()
    {
        animation1 = GetComponent<Animation>();
    }

    // Start is called before the first frame update
    void Start()
    {
        //animation1.Play("splashGradient");
        t = 0;
        spd = Random.Range(0f, 1f) * 80f + 185f;
        rad = Random.Range(0f, 360f) * Mathf.Deg2Rad;
    }

    // Update is called once per frame
    void Update()
    {
        t += Time.deltaTime * 2f;
        a = (6.234f * Mathf.Pow(t, 3) - 49.572f * t * t + 49.197f * t + 14.964f);
        b = ((spd) * 9 * t / (8 * t + 1)) * 0.011f;
        transform.localScale = new Vector3(a, a, 1f);
        transform.localPosition = new Vector3(b * Mathf.Cos(rad), b * Mathf.Sin(rad));
        //transform.localPosition += dir.normalized * spd * Time.fixedDeltaTime;
        //spd -= Time.fixedDeltaTime * 10f;
            gameObject.GetComponent<SpriteRenderer>().color = 
                new Color(transform.parent.gameObject.GetComponent<SpriteRenderer>().color.r,
                transform.parent.gameObject.GetComponent<SpriteRenderer>().color.g,
                transform.parent.gameObject.GetComponent<SpriteRenderer>().color.b,
                gameObject.GetComponent<SpriteRenderer>().color.a);
    }

    private void OnEnable()
    {
        animation1.Play("splashGradient");
        t = 0;
        spd = Random.Range(0f, 1f) * 80f + 185f;
        rad = Random.Range(0f, 360f) * Mathf.Deg2Rad;
    }

    private void OnDisable()
    {
        t = 0f;
        transform.localPosition = new Vector3(0, 0, 0);
        transform.localScale = new Vector3(0, 0, 0);
    }

    public void DestroyThis()
    {
        //Destroy(gameObject);
    }
}
