using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectSplash : MonoBehaviour
{
    public float spd = 0;
    public float a;
    public float b;
    public float rad;
    public const float factor = 1f;
    public float t = 0;

    // Start is called before the first frame update
    void Start()
    {
        spd = Random.Range(0f, 1f) * 80f + 185f;
        rad = Random.Range(0f, 360f) * Mathf.Deg2Rad;

    }

    // Update is called once per frame
    void FixedUpdate()
    {
        t += Time.fixedDeltaTime * 2f;
        a = (6.234f * Mathf.Pow(t, 3) - 49.572f * t * t + 49.197f * t + 14.964f) * factor;
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

    public void DestroyThis()
    {
        Destroy(gameObject);
    }
}
