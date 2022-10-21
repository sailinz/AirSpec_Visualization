using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.Threading;
using System.IO;
using System.Net;
using System.Text;
using System.Net.Sockets;
using System.Linq;

public class VisController : MonoBehaviour
{
    const int PORT_NO = 65467; // needed to change the port number
    const string SERVER_IP = "127.0.0.1";
    static string textToSend;
    static byte[] bytesToSend;
    static byte[] bytesToRead;
    static int bytesRead;
    private TcpClient client; 	
    private Thread clientReceiveThread;
    private bool blinking = true;
    
    // recap on prefab: https
    //www.youtube.com/watch?v=IfcCXVXjLNM

    // // physiological //
    // [Header("3D models")]
    // public GameObject _noseTipTempSensor;
    // public GameObject _noseBrigeTempSensor;
    // public GameObject _templeFrontTempSensor;
    // public GameObject _templeMidTempSensor;
    // public GameObject _templeRearTempSensor;
    // public GameObject _blinkSensor;
    
    // // air quality //
    // public GameObject _co2Sensor;
    // public GameObject _vocSensor;
    // public GameObject _noxSensor; 
    // // public GameObject _pressureSensor:
    // // public GameObject _vscSensor;
    // // public GameObject _hydrogenSensor;

    // // thermal //
    // public GameObject _tempSensor;
    // public GameObject _humiditySensor;

    // // lighting  //
    // public GameObject _spectralSensor; // => need to check how to visualize these as a valid form
    // public GameObject _luxSensor;

    // // noise //
    // // public GameObject _microphone

    [Header("Prefabs")]
    public GameObject _aqPrefab;
    public GameObject _aqFacePrefab;
    private GameObject[] _aqParticles;
    private GameObject[] _aqFaceParticles;

    [Header("Vis dimensions")]
    public GameObject _cogload;
    public GameObject _blink;
    public GameObject _temperature;
    public GameObject _humidity;

    // private GameObject _temperatureColorpallete;

    // air temperature
    public Color color1 = Color.red;
    public Color color2 = Color.blue;
    public float duration = 3.0F;
    public GameObject _cam;
    private Camera cam;

    // dummy values for simulation
    private float _randomNum; //to be replaced to sensor data

    // Normalization

    // data to be updated
    private int aqSensorNo = 3; //
    private int aqFaceSensorNo = 1; // CO2
    private List<int> _aqValues;
    private List<int> _aqFaceValues; 
    private float _earBlink; 

   
   
    // Start is called before the first frame update
    void Start()
    {
        
        
        // air quality visualization
        // environment
        var aq_colors = new List<Color>()
                    {new Color(102/255f, 204/255f, 255f/255f), new Color(102f/255f, 153f/255f, 255f/255f), new Color(0f/255f, 0f/255f, 153f/255f)}; // add more if there's more than 3 parameters
        _aqParticles = new GameObject[aqSensorNo];
        for (int i=0; i<aqSensorNo; i++){
            
            // initiate air quality particle systems
            _aqParticles[i] = Instantiate(_aqPrefab, new Vector3(0.0f, -1.08f, -0.66f), Quaternion.Euler(90f, 0f, 0f)) as GameObject; 
            // assign color
            var col = _aqParticles[i].GetComponent<ParticleSystem>().colorOverLifetime;
            col.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys( new GradientColorKey[] { new GradientColorKey(aq_colors[i], 0.0f), new GradientColorKey(aq_colors[i], 1.0f) }, new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) } );
            col.color = grad;
            
        }

        UnityEngine.Debug.Log("particle created");

        

        // face
        // color palette: https://www.researchgate.net/publication/236595099_Perceptual_difference_in_L_a_b_color_space_as_the_base_for_object_colour_identfication/figures?lo=1
        var aqFace_colors = new List<Color>()
                    {new Color(204f/255f, 0f/255f, 153f/255f), new Color(153f/255f, 51f/255f, 102f/255f), new Color(153f/255f, 0f/255f, 102f/255f)};
        _aqFaceParticles = new GameObject[aqFaceSensorNo];
        for (int i=0; i<aqFaceSensorNo; i++){
            
            // initiate air quality particle systems
            _aqFaceParticles[i] = Instantiate(_aqFacePrefab, new Vector3(0.0f, 0.56f, -0.92f), Quaternion.Euler(156f, 0f, 0f)) as GameObject;  // add more if there's more than 3 parameters
            // assign color
            var col = _aqFaceParticles[i].GetComponent<ParticleSystem>().colorOverLifetime;
            col.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys( new GradientColorKey[] { new GradientColorKey(aqFace_colors[i], 0.0f), new GradientColorKey(aqFace_colors[i], 1.0f) }, new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) } );
            col.color = grad;
            
        }

        // blink 
        StartCoroutine(AnimateBlink());
        StartCoroutine(NumberGen());

        cam = _cam.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;

        // temperature color pallete
        // _temperatureColorpallete = new MultiColorPalette
        
        // receive data
        ConnectToTcpServer();    
    } 

    // Update is called once per frame
    void Update()
    {

        // float modifier = 5f;
        // float nextTime = Time.time + modifier;
        // //Check if current system time is greater than nextTime
        // if(Time.time > nextTime)
        // {
        //     _randomNum = UnityEngine.Random.Range(5, 20);
        // }

        // Cognitive load //
        // _cogload.GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", new Color(1.0f,1.0f,0.8f) * Random.Range(0.1f, 0.8f));

        // Blink //
        // Eye Aspect Ratio (EAR)
        // __blink.GetComponent<SkinnedMeshRenderer>().SetBlendShapeWeight (1, _earBlink)
        

        // Air quality //
        // example random values for air quality
        _aqValues = new List<int>()
                    {UnityEngine.Random.Range(1, 20), UnityEngine.Random.Range(1, 20), UnityEngine.Random.Range(1, 20)};

        _aqFaceValues = new List<int>()
                    {UnityEngine.Random.Range(1, 20), UnityEngine.Random.Range(1, 20), UnityEngine.Random.Range(1, 20)};


        // Air quality: change emission rate according to sensor data
        // environment
        for (int i=0; i<aqSensorNo; i++){
            var emission = _aqParticles[i].GetComponent<ParticleSystem>().emission;
            // emission.rateOverTime = _aqValues[i]; // to be updated to sensor readings
            emission.rateOverTime = _randomNum; // to be updated to sensor readings
        }
        //face (bioeffluence)
        for (int i=0; i<aqFaceSensorNo; i++){
            var emissionFace = _aqFaceParticles[i].GetComponent<ParticleSystem>().emission;
            // emissionFace.rateOverTime = _aqFaceValues[i];    // to be updated to sensor readings
            emissionFace.rateOverTime = _randomNum;    // to be updated to sensor readings
        }

        // Humidity //
        var emissionHum = _humidity.GetComponent<ParticleSystem>().emission;
        // emissionHum.rateOverTime = UnityEngine.Random.Range(1, 50); // to be updated to sensor readings
        emissionHum.rateOverTime = _randomNum; // to be updated to sensor readings


        // Temperature //
        // _temperature.GetComponent<SkinnedMeshRenderer>().material.SetColor("_EmissionColor", Desaturate(Color.red, Random.Range(0.0f, 1.0f)));
        _temperature.GetComponent<SkinnedMeshRenderer>().material.SetColor("_EmissionColor", Desaturate(Color.blue, UnityEngine.Random.Range(0.4f, 0.5f)));

        float t = Mathf.PingPong(Time.time, duration) / duration;
        cam.backgroundColor = Color.Lerp(color1, color2, t);
    }


    public Color Desaturate(Color rgbColor, float saturation)
    {
        float myH, myS, myV;
        ColorConvert.RGBToHSV(rgbColor, out myH, out myS, out myV);

        Color returnColor = ColorConvert.HSVToRGB(myH, myS * saturation, myV);
        return returnColor;
    }

    public IEnumerator NumberGen(){
        while(true){
            _randomNum = UnityEngine.Random.Range(1,20);
            yield return new WaitForSeconds(6);
        }
    }
 

    private IEnumerator LerpShape(float startValue, float endValue, float duration) {
        float elapsed = 0;
        while (elapsed  < duration) {
            elapsed += Time.deltaTime;
            float value = Mathf.Lerp(startValue, endValue, elapsed / duration);
            _blink.GetComponent<SkinnedMeshRenderer>().SetBlendShapeWeight (0, value);
            yield return null;
        }
    }

    // dummy values
    private IEnumerator AnimateBlink() {
        while (blinking) {
            //yield return StartCoroutine waits for that coroutine to finish before continuing
            yield return StartCoroutine(LerpShape(0, 100, 1.0f));
            yield return StartCoroutine(LerpShape(100, 0, 1.0f));
        }
    }  

    //https://gist.github.com/danielbierwirth/0636650b005834204cb19ef5ae6ccedb
    private void ConnectToTcpServer () { 	
        try {  			
            clientReceiveThread = new Thread (new ThreadStart(ListenForData)); 			
            clientReceiveThread.IsBackground = true; 			
            clientReceiveThread.Start();  		
        } 		
        catch (Exception e) { 			
            Debug.Log("On client connect exception " + e); 		
        }
		
	}  	

    private void ListenForData() { 	

        while(true){	
            try { 			
                client = new TcpClient(SERVER_IP, PORT_NO);  
                UnityEngine.Debug.Log("Connected to Python script!");			          
            }         
            catch (SocketException socketException) {             
                Debug.Log("Socket exception: " + socketException);         
            }  
            
            break; 
        }

        NetworkStream nwStream = client.GetStream();

        while (true)
        {
            
            //---read back the text---
            // using (NetworkStream nwStream = client.GetStream()) {
            try
            {  
                bytesToRead = new byte[client.ReceiveBufferSize];
                bytesRead = nwStream.Read(bytesToRead, 0, client.ReceiveBufferSize);
                UnityEngine.Debug.Log("get data");
            }catch (System.IO.IOException e)
            {
                UnityEngine.Debug.Log("ERROR: can't read from socket");
                // Console.WriteLine("ERROR: can't read from socket");
                break;
            }
            string receivedString = Encoding.ASCII.GetString(bytesToRead, 0, bytesRead);
            UnityEngine.Debug.Log(receivedString);
            List<string> result = receivedString.Remove(receivedString.Length - 1, 1).Remove(0, 1).Split(',').ToList();
            if (result[0] == "THERMOPILE_COG")
            {
                float value = (float)Convert.ToDouble(result[1]); // difference of two temperature values

                // this value is the difference between a temple measurement and the tip of the nose
                //   (i.e., TEMPLE_TEMP - NOSE_TEMP = THERMOPILE_COG)
                UnityEngine.Debug.Log(result[0]);
            }
            else if (result[0] == "THERMOPILE_TEMPLE")
            {
                // this value is temperature, in Kelvin, of the middle thermopile of the temple thermopile array of 3
                float value = (float)Convert.ToDouble(result[1]); // kelvin

                UnityEngine.Debug.Log(result[0]);
            }
            else if (result[0] == "SGP_GAS")
            {
                // reference datasheet for SGP41 on how the indices are defined
                float voc_index = (float)Convert.ToDouble(result[1]); // range: 1-500, ambient average: 100
                float nox_index = (float)Convert.ToDouble(result[2]); // range: 1-500, ambient average: 1

                UnityEngine.Debug.Log(result[0]);
            }
            else if (result[0] == "BME_IAQ")
            {
                /**
                    * @brief Indoor-air-quality estimate [0-500]
                    * 
                    * Indoor-air-quality (IAQ) gives an indication of the relative change in ambient TVOCs detected by BME680. 
                    * 
                    * @note The IAQ scale ranges from 0 (clean air) to 500 (heavily polluted air). During operation, algorithms 
                    * automatically calibrate and adapt themselves to the typical environments where the sensor is operated 
                    * (e.g., home, workplace, inside a car, etc.).This automatic background calibration ensures that users experience 
                    * consistent IAQ performance. The calibration process considers the recent measurement history (typ. up to four 
                    * days) to ensure that IAQ=25 corresponds to typical good air and IAQ=250 indicates typical polluted air.
                    */
                float IAQ = (float)Convert.ToDouble(result[1]);

                UnityEngine.Debug.Log(result[0]);
            }
            else if (result[0] == "BME_CO2_EQ")
            {
                float co2_eq = (float)Convert.ToDouble(result[1]); // co2 equivalent estimate [ppm]

                UnityEngine.Debug.Log(result[0]);
            }
            else if (result[0] == "BME_VOC_EQ")
            {
                float voc_index = (float)Convert.ToDouble(result[1]); // breath VOC concentration estimate [ppm]
                UnityEngine.Debug.Log(result[0]);
            }
            else if (result[0] == "SHT45")
            {
                float ambient_temperature = (float)Convert.ToDouble(result[1]); // Celsius
                float ambient_humidity = (float)Convert.ToDouble(result[2]); // relative humidity, influenced by person breathing

                UnityEngine.Debug.Log(result[0]);
            }
            else if (result[0] == "Lux")
            {
                float lux = (float)Convert.ToDouble(result[1]); // light intensity [lux]

                UnityEngine.Debug.Log(result[0]);
            }
            else if (result[0] == "Blink")
            {
                // !!!!! NOT IMPLEMENTED !!!!! 
                float blink = (float)Convert.ToDouble(result[1]); // blink: 1, no_blink: 0

                UnityEngine.Debug.Log(result[0]);
            }
            // } 
            
        }
          



	}  	




}
