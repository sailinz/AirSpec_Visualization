using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlowMat : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(glowcycle());
    }

    // Update is called once per frame
    void Update()
    {
        
    }



    IEnumerator glowcycle()
    {
        while(true){
            yield return new WaitForSeconds(.05f);
            for (float glowintense = 0.1f; glowintense < 0.8f; glowintense += .05f)
            {
                GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", new Color(1.0f,1.0f,0.8f) * glowintense);
                yield return new WaitForSeconds(.05f);
            }

            for (float glowintense = 0.8f; glowintense > 0.1f; glowintense -= .05f)
            {
                GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", new Color(1.0f,1.0f,0.8f) * glowintense);
                yield return new WaitForSeconds(.05f);
            }
        }
        
    }
}
