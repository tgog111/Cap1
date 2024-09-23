using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LSL;
using LSL4Unity.Utils;

public class SphereMovement : MonoBehaviour
{
    public float speed = 0.1f;
    public float jumpPower=5;
    private Rigidbody rigid;
    private bool isJump = false;

    #region LSL4Unity_inlet
    public string StreamName; // must be same with the OpenViBE streamname
    ContinuousResolver resolver;
    double max_chunk_duration = 0.5; // epoch interval 2.0sec
    private StreamInlet inlet;

    private float[,] data_buffer;
    private double[] timestamp_buffer;
    float EEGpow;
    bool isSatisfied = false;
    #endregion

    void Start()
    {
        speed = 0.1f;
        rigid = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        rigid.AddForce(new Vector3(h,0,v)*speed, ForceMode.Impulse);

        #region LSL_inlet_update
        if(inlet!=null){
            int samples_returned = inlet.pull_chunk(data_buffer, timestamp_buffer);
            if(samples_returned > 0){
                float x = data_buffer[samples_returned - 1, 0];

                Debug.Log(x);
                EEGpow = x;

                if(EEGpow < 500){
                    isSatisfied = true;
                    speed = 0.0f;
                }
                else{
                    isSatisfied = false;
                    speed = 0.5f;
                }
            }
        }
        #endregion
    }

    void Awake(){
        if(!StreamName.Equals(""))
            resolver = new ContinuousResolver("name", StreamName);
        else{
            Debug.LogError("Object must specify a name for resolver to lookup a stream.");
            this.enabled = false;
            return;
        }
        StartCoroutine(ResolveExpectedStream());
    }

    IEnumerator ResolveExpectedStream(){
        var results = resolver.results();
        while(results.Length == 0){
            yield return new WaitForSeconds(.1f);
            results = resolver.results();
        }
        inlet = new StreamInlet(results[0]);
        int buf_Samples = (int)Mathf.Ceil((float)(inlet.info().nominal_srate() * max_chunk_duration));
        int n_channels = inlet.info().channel_count();
        data_buffer = new float[buf_Samples, n_channels];
        timestamp_buffer = new double[buf_Samples];
    }

    private void OnCollisionEnter(Collision other) {
        if(other.gameObject.tag == "Floor"){
            isJump = false;
        }
    }
}

