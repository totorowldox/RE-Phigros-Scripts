using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AutoMatchSize : MonoBehaviour
{
    public RectTransform canvas;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Screen.width / Screen.height < 16f / 9f)
            canvas.sizeDelta = new Vector2(canvas.rect.width, 600f / 0.5625f / Screen.width * Screen.height);
        else
            canvas.sizeDelta = new Vector2(canvas.rect.width, 600f);
    }
}
