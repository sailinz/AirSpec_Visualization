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
using System.Diagnostics;
using UnityEngine.UI;
using TMPro;

public class VisController : MonoBehaviour
{
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
    [Header("UI elements")]
    public TMPro.TextMeshProUGUI _cogText; 
    public TMPro.TextMeshProUGUI _templeTempText;
    public TMPro.TextMeshProUGUI _VOCText; 
    public TMPro.TextMeshProUGUI _eVOCText; 
    public Image _eVOCcursor; 
    public TMPro.TextMeshProUGUI _NOxText; 
    public Image _NOxcursor; 
    public TMPro.TextMeshProUGUI _CO2Text; 
    public TMPro.TextMeshProUGUI _ambientTempText;
    public TMPro.TextMeshProUGUI _ambienHumText;
    private List<TMPro.TextMeshProUGUI> _aqText;
    private List<TMPro.TextMeshProUGUI> _aqFaceText;
    public TMPro.TextMeshProUGUI _iaqText;
    public Image _IAQcursor; 
    private List<string> _aqTextNames;
    private List<string> _aqFaceTextNames;
    private List<string> _aqTextUnits;
    private List<string> _aqFaceTextUnits;
    public TMPro.TextMeshProUGUI _luxText;
    public Image _spectralImg;

    private float _eVOCcursorOriginalX;
    private float _NOxcursorOriginalX;
    private float _IAQcursorOriginalX;


    [Header("Prefabs")]
    public GameObject _aqPrefab;
    public GameObject _aqFacePrefab;
    private GameObject[] _aqParticles;
    private GameObject[] _aqFaceParticles;

    [Header("Vis dimensions 3D models")]
    public GameObject _cogload;
    public GameObject _blink;
    public GameObject _temperature;
    public GameObject _humidity;

    // private GameObject _temperatureColorpallete;
    // 
    [Header("Air quality smoke colors (Env)")]
    public List<Color> aqColors = new List<Color>(new Color[aqSensorNo]);
    
    [Header("Air quality smoke colors (human)")]
    public List<Color> aqFaceColors = new List<Color>(new Color[aqFaceSensorNo]);

    [Header("Skin (temple) temperature")]
    public Color skinTempColor;

    [Header("Background (ambient temperature)")]
    public Color color1 = Color.red;
    public Color color2 = Color.blue;
    // public float duration = 3.0F;
    public GameObject _cam;
    private Camera cam;

    // dummy values for simulation
    private float _randomNum; //to be replaced to sensor data

    // Normalization - real-time
    const float THERMOPILE_COG_MAX = 5f; // to be verified 5;
    const float THERMOPILE_COG_MIN = 0; 
    const float THERMOPILE_TEMPLE_MAX = 308;
    const float THERMOPILE_TEMPLE_MIN = 294;
    const float SGP_GAS_VOC_MAX = 200; // env (average 100) 500
    const float SGP_GAS_VOC_MIN = 50; // 1
    const float SGP_GAS_NOx_MAX = 100; // env (theoretically the range is 1-500, average 1)
    const float SGP_GAS_NOx_MIN = 1; 
    const float BME_CO2_EQ_MAX = 1700; // bioeffluence
    const float BME_CO2_EQ_MIN = 600;
    const float BME_VOC_EQ_MAX = 2; // env 2000
    const float BME_VOC_EQ_MIN = 0; //0
    const float SHT45_TEMP_MAX = 35;
    const float SHT45_TEMP_MIN = 24; // 20
    const float SHT45_HUM_MAX = 60; // relative humidity, influenced by person breathing; shall we put it on face?
    const float SHT45_HUM_MIN = 30; //20 
    
    const int aqSensorNo = 3; // VOC, NOx, eVOC
    const int aqFaceSensorNo = 1; // CO2

    // data to be updated
    private float _cogValue; 
    private float _templeTempValue;
    private List<float> _aqValues; //x3
    private List<float> _aqFaceValues; //x1
    private float _ambientTemp;
    private float _ambienHum;
    private float _iaqValue;
    private float _luxValue;
    private Color _spectralValue;

    private float _cogValuePrev = -1; 
    private float _templeTempValuePrev = -1;
    private List<float> _aqValuesPrev; //x3
    private List<float> _aqFaceValuesPrev; //x1
    private float _ambientTempPrev = -1;
    private float _ambienHumPrev = -1;
    private float _iaqValuePrev = -1;
    private float _luxValuePrev = -1; 
    private Color _spectralValuePrev = new Color(0,0,0);

    private List<float> _aqMaxValues; 
    private List<float> _aqMinValues; 
    private List<float> _aqFaceMinValues;
    private List<float> _aqFaceMaxValues; 
    // + overall IAQ

    // private float _earBlink; 

    // Server info
    const int PORT_NO = 65467; // needed to change the port number
    const string SERVER_IP = "127.0.0.1";
    private TcpClient client; 	
    private Thread clientReceiveThread;
    private bool blinking = true;
    NetworkStream nwStream;
    private bool stopThread = false;
    Process processConnServer;
    Process processUnityParser;
   
    // Start is called before the first frame update
    void Start()
    {
        _eVOCcursorOriginalX = _eVOCcursor.GetComponent<RectTransform>().anchoredPosition.x;
        _NOxcursorOriginalX = _NOxcursor.GetComponent<RectTransform>().anchoredPosition.x;
        _IAQcursorOriginalX = _IAQcursor.GetComponent<RectTransform>().anchoredPosition.x;

        _aqValues = new List<float>(new float[aqSensorNo]);
        _aqText = new List<TMPro.TextMeshProUGUI>(new TMPro.TextMeshProUGUI[aqSensorNo]);
        _aqFaceValues = new List<float>(new float[aqFaceSensorNo]);
        _aqFaceText = new List<TMPro.TextMeshProUGUI>(new TMPro.TextMeshProUGUI[aqFaceSensorNo]);
        _aqValuesPrev = new List<float>(){-1f, -1f, -1f};
        _aqFaceValuesPrev = new List<float>(){-1f, -1f, -1f};
        _aqTextNames = new List<string>(){"VOC: ", "NOx index: ", "VOC index: "};
        _aqFaceTextNames = new List<string>(){"eCO2: "};
        _aqTextUnits = new List<string>(){" ppm", "", ""}; //eVOC and NOx are index: no units; " ppm", " (avg: 1)", " (avg: 100)"
        _aqFaceTextUnits = new List<string>(){" ppm"};


        _aqMaxValues = new List<float>(new float[aqSensorNo]);
        _aqMinValues = new List<float>(new float[aqSensorNo]);
        _aqFaceMinValues = new List<float>(new float[aqFaceSensorNo]);  
        _aqFaceMaxValues = new List<float>(new float[aqFaceSensorNo]);  
        _aqMaxValues[0] = SGP_GAS_VOC_MAX;
        _aqMaxValues[1] = SGP_GAS_NOx_MAX;
        _aqMaxValues[2] = BME_VOC_EQ_MAX;
        _aqMinValues[0] = SGP_GAS_VOC_MIN;
        _aqMinValues[1] = SGP_GAS_NOx_MIN;
        _aqMinValues[2] = BME_VOC_EQ_MIN;
        _aqFaceMaxValues[0] = BME_CO2_EQ_MAX;
        _aqFaceMinValues[0] =BME_CO2_EQ_MIN ;
        _aqText[0] = _VOCText;
        _aqText[1] = _NOxText;
        _aqText[2] = _eVOCText;
        _aqFaceText[0] = _CO2Text;

        

        // air quality (environment)
        // var aq_colors = new List<Color>()
        //             {new Color(102/255f, 204/255f, 255f/255f), new Color(102f/255f, 153f/255f, 255f/255f), new Color(0f/255f, 0f/255f, 153f/255f)}; // add more if there's more than 3 parameters
        _aqParticles = new GameObject[aqSensorNo];
        for (int i=0; i<aqSensorNo; i++){
            // initiate air quality particle systems
            _aqParticles[i] = Instantiate(_aqPrefab, new Vector3(0.0f, -0.65f, -0.20f), Quaternion.Euler(90f, 0f, 0f)) as GameObject; 
            // assign color
            var col = _aqParticles[i].GetComponent<ParticleSystem>().colorOverLifetime;
            col.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys( new GradientColorKey[] { new GradientColorKey(aqColors[i], 0.0f), new GradientColorKey(aqColors[i], 1.0f) }, new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) } );
            col.color = grad;
            
        }

        var psmain1 = _aqParticles[0].GetComponent<ParticleSystem>().main;
        psmain1.startSize = 20f;
        var psmain2 = _aqParticles[1].GetComponent<ParticleSystem>().main;
        psmain2.startSize = 12f;
        var psmain3 = _aqParticles[2].GetComponent<ParticleSystem>().main;
        psmain3.startSize = 5f;

        // air quality (face)
        // color palette: https://www.researchgate.net/publication/236595099_Perceptual_difference_in_L_a_b_color_space_as_the_base_for_object_colour_identfication/figures?lo=1
        // var aqFace_colors = new List<Color>()
        //             {new Color(204f/255f, 0f/255f, 153f/255f), new Color(153f/255f, 51f/255f, 102f/255f), new Color(153f/255f, 0f/255f, 102f/255f)};
        _aqFaceParticles = new GameObject[aqFaceSensorNo];
        for (int i=0; i<aqFaceSensorNo; i++){
            
            // initiate air quality particle systems
            _aqFaceParticles[i] = Instantiate(_aqFacePrefab, new Vector3(0.0f, 0.41f, -0.7f), Quaternion.Euler(156f, 0f, 0f)) as GameObject;  // add more if there's more than 3 parameters
            // assign color
            var col = _aqFaceParticles[i].GetComponent<ParticleSystem>().colorOverLifetime;
            col.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys( new GradientColorKey[] { new GradientColorKey(aqFaceColors[i], 0.0f), new GradientColorKey(aqFaceColors[i], 1.0f) }, new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) } );
            col.color = grad;
            
        }

        var psfacemain1 = _aqFaceParticles[0].GetComponent<ParticleSystem>().main;
        psfacemain1.startSize = 30f;

        // blink 
        StartCoroutine(AnimateBlink());
        // StartCoroutine(NumberGen());

        // air temperature
        cam = _cam.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        
        // receive data (to find python process pgrep -f airSpecSimulator.py)
        // processConnServer = ExecuteCommand("/Users/zhongs/Desktop/medialab/AirSpec/env_glasses/python/airSpecSimulator.py");
        // Thread.Sleep(1000);
        // processUnityParser = ExecuteCommand("/Users/zhongs/Desktop/medialab/AirSpec/env_glasses/python/unityParser.py");
        // Thread.Sleep(1000);    
        
        ConnectToTcpServer();    
    } 


    // Update is called once per frame
    void Update()
    {
        // How to quit the game: press "Q" (https://gamedevbeginner.com/how-to-quit-the-game-in-unity/)
        if (Input.GetKeyDown(KeyCode.Q))
        {
            // StartCoroutine(WaitAndDoSomething());
            stopThread=true;
            stopClient();
            UnityEngine.Debug.Log("try to stop TCP client");

            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }

        // Cognitive load //
        if(_cogValue != _cogValuePrev){
            float normalized_cog = (_cogValue - THERMOPILE_COG_MIN)/(THERMOPILE_COG_MAX - THERMOPILE_COG_MIN) * 0.7f;
            if(normalized_cog > 0){
                _cogload.GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", new Color(1.0f,1.0f,0.8f) * (Math.Abs(normalized_cog) + 0.1f));
                _cogValuePrev = _cogValue;
                
                // UnityEngine.Debug.Log("cog: " + _cogValue);
            }else{
                _cogload.GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", new Color(1.0f,1.0f,0.8f) * 0.1f);
                // _cogText.text = "Cognitive load: 0"; //+ " °C";
            }
            _cogText.text = "Cognitive load: " + Math.Round(_cogValue, 2); //+ " °C";
            
        }
            

        // Blink //
        // Eye Aspect Ratio (EAR) - not working yet
        // __blink.GetComponent<SkinnedMeshRenderer>().SetBlendShapeWeight (1, _earBlink)
        

        // Air quality //
        // environment
        for (int i=0; i<aqSensorNo; i++){
            if(_aqValues[i] != _aqValuesPrev[i]){
                var emission = _aqParticles[i].GetComponent<ParticleSystem>().emission;
                float normalized_aqValue = (_aqValues[i] - _aqMinValues[i])/(_aqMaxValues[i] - _aqMinValues[i]); 
                
                if (normalized_aqValue > 0){
                    emission.rateOverTime = normalized_aqValue * 20 + 5; // updated to sensor readings
                }else{
                    emission.rateOverTime = 5; // be updated to sensor readings
                }
                _aqValuesPrev[i] = _aqValues[i];
                _aqText[i].text = _aqTextNames[i] + Math.Round(_aqValues[i], 2) + _aqTextUnits[i];
                // UnityEngine.Debug.Log("AQ" + i + " : " + _aqValues[i]);

                if(i == 1) // VOC index
                {
                    _eVOCcursor.GetComponent<RectTransform>().anchoredPosition = new Vector3 (_eVOCcursorOriginalX + _aqValues[i]/500f*800f, _eVOCcursor.GetComponent<RectTransform>().anchoredPosition.y, 0);
                }
                if(i == 2) // NOx index
                {
                    _NOxcursor.GetComponent<RectTransform>().anchoredPosition = new Vector3 (_NOxcursorOriginalX + _aqValues[i]/500f*800f, _NOxcursor.GetComponent<RectTransform>().anchoredPosition.y, 0);
                }
                
            }
        }

        if(_iaqValue != _iaqValuePrev)
        {

            _iaqText.text = "IAQ index: " + Math.Round(_iaqValue, 2);
            _iaqValuePrev = _iaqValue;
            if(_iaqValue < 500)
            {
                _IAQcursor.GetComponent<RectTransform>().anchoredPosition = new Vector3 (_IAQcursorOriginalX + _iaqValue/400f*800f, _IAQcursor.GetComponent<RectTransform>().anchoredPosition.y, 0);
            }else
            {
                _IAQcursor.GetComponent<RectTransform>().anchoredPosition = new Vector3 (_IAQcursorOriginalX + 800f, _IAQcursor.GetComponent<RectTransform>().anchoredPosition.y, 0);
            }
            
        }
        
        //now hard-coded...
 
        //face (bioeffluence)
        for (int i=0; i<aqFaceSensorNo; i++){
            if(_aqFaceValues[i] != _aqFaceValuesPrev[i]){
                var emissionFace = _aqFaceParticles[i].GetComponent<ParticleSystem>().emission;
                float normalized_aqFaceValue = (_aqFaceValues[i] - _aqFaceMinValues[i])/(_aqFaceMaxValues[i] - _aqFaceMinValues[i]);
                
                if (normalized_aqFaceValue > 0){
                    emissionFace.rateOverTime = normalized_aqFaceValue * 20 + 2;    // updated to sensor readings
                }else{
                    emissionFace.rateOverTime = 2;
                }
                _aqFaceValuesPrev[i] = _aqFaceValues[i];
                _aqFaceText[i].text = _aqFaceTextNames[i] + Math.Round(_aqFaceValues[i], 2) + _aqFaceTextUnits[i];
                // UnityEngine.Debug.Log("Face AQ" + i + " : " + _aqFaceValues[i]);
            } 
        }

        // Humidity //
        if(_ambienHum != _ambienHumPrev){
            var emissionHum = _humidity.GetComponent<ParticleSystem>().emission;
            float normalized_hum = (_ambienHum - SHT45_HUM_MIN)/(SHT45_HUM_MAX - SHT45_HUM_MIN);
            if(normalized_hum > 0){
                emissionHum.rateOverTime =  normalized_hum * 40 + 3;
            }else{
                emissionHum.rateOverTime =  3;
            }
            _ambienHumPrev = _ambienHum;
            _ambienHumText.text = "Humidity: " + Math.Round(_ambienHum, 2) + " %";
            // UnityEngine.Debug.Log("humidity: " +_ambienHum);
        }

        // Face temperature //
        if(_templeTempValue != _templeTempValuePrev){
            float normalized_templeTemp = (_templeTempValue - THERMOPILE_TEMPLE_MIN)/(THERMOPILE_TEMPLE_MAX - THERMOPILE_TEMPLE_MIN); 
            if(normalized_templeTemp > 0){
                _temperature.GetComponent<SkinnedMeshRenderer>().material.SetColor("_EmissionColor", Desaturate(skinTempColor, normalized_templeTemp));  
            }else{
                _temperature.GetComponent<SkinnedMeshRenderer>().material.SetColor("_EmissionColor", Desaturate(skinTempColor, 0));
            }
            _templeTempValuePrev = _templeTempValue;
            _templeTempText.text = "Skin temperature: " + Math.Round(_templeTempValue - 273.15, 2) + " °C";
            // UnityEngine.Debug.Log("temple temp: " + _templeTempValue);
        }
        
        // Ambient Temperature //
        //// cam.backgroundColor = Color.Lerp(color1, color2, t);
        if(_ambientTemp != _ambientTempPrev){
            float normalized_temp = (_ambientTemp - SHT45_TEMP_MIN)/(SHT45_TEMP_MAX - SHT45_TEMP_MIN);
            if(normalized_temp > 0){
                // cam.backgroundColor = Desaturate(color2, normalized_temp);
                cam.backgroundColor = new Color(color2.r, color2.g, color2.b, 255 - 255 * normalized_temp);
            }
            _ambientTempPrev = _ambientTemp; 
            _ambientTempText.text = "Ambient temperature: " + Math.Round(_ambientTemp, 2) + " °C";
            // UnityEngine.Debug.Log("Ambient temp: " + _ambientTemp);
        }  

        // Lighting //
        if(_luxValue != _luxValuePrev)
        {
            _luxText.text = "Light intensity: " + Math.Round(_luxValue, 2)  + " Lux";
            _luxValuePrev = _luxValue;
                        
        }

        if(_spectralValuePrev != _spectralValue)
        {
            _spectralImg.GetComponent<Image>().color = _spectralValue; 
            UnityEngine.Debug.Log("Light color: " + _spectralValue);
        }
    }

    // execute other scripts (e.g., python) via terminal -- a workaround for the server-client communication issue [NOW RESOLVED -> we can ignore this]
    public static Process ExecuteCommand(string command)
    {
        // Process proc = new System.Diagnostics.Process ();
        // proc.StartInfo.FileName = @"/System/Applications/Utilities/Terminal.app/Contents/MacOS/Terminal";
        // proc.StartInfo.Arguments = "-c \" " + command + " \"";
        // // proc.StartInfo.Arguments = "-c \" " + command + " \"";
        // proc.StartInfo.UseShellExecute = false; 
        // proc.StartInfo.RedirectStandardOutput = false;
        // proc.Start();

        var processStartInfo = new ProcessStartInfo();
        processStartInfo.FileName = "/usr/local/bin/python3.9";
        var script = command;

        processStartInfo.Arguments = $"\"{script}\"";
        processStartInfo.UseShellExecute = false;
        processStartInfo.CreateNoWindow = false;
        processStartInfo.RedirectStandardOutput = true;
        processStartInfo.RedirectStandardError = true;

        var process = Process.Start(processStartInfo);
        return process;
    }

    // close the TCP socket! Otherwise it will become a zumbie keep occupying the port
    private void stopClient() { 

        Thread.Sleep(3000); //need to give some buffer for the background thread (for data receiving) to close! 
        //stop thread
        if (clientReceiveThread != null)
        {
            try
            {
                nwStream.Close();
                client.Close();
                clientReceiveThread.Abort();
                // processUnityParser.Kill();
                // processConnServer.Kill();

                UnityEngine.Debug.Log("TCP client stopped: thread - " + clientReceiveThread.ThreadState + "; client connected? - " + client.Connected); 
            }catch
            {
                clientReceiveThread.Abort();
                UnityEngine.Debug.Log("No previous connected TCP client was found. Just close the thread.");
            }        
        }   
    }

    // private void OnApplicationQuit()
    // {
    //     client.Close();
    // }


    public Color Desaturate(Color rgbColor, float saturation)
    {
        float myH, myS, myV;
        ColorConvert.RGBToHSV(rgbColor, out myH, out myS, out myV);

        Color returnColor = ColorConvert.HSVToRGB(myH, myS * saturation, myV);
        return returnColor;
    }

    // public IEnumerator NumberGen(){
    //     while(true){
    //         _randomNum = UnityEngine.Random.Range(1,20);
    //         yield return new WaitForSeconds(6);
    //     }
    // }

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
            clientReceiveThread.IsBackground = false; 		 	// originally true
            clientReceiveThread.Start();  		
        } 		
        catch (Exception e) { 			
            UnityEngine.Debug.Log("On client connect exception " + e); 		
        }
		
	}  	

    private void ListenForData() { 	

        while(true){	
            try { 			
                client = new TcpClient(SERVER_IP, PORT_NO);  
                UnityEngine.Debug.Log("Connected to Python script!");			          
            }         
            catch (SocketException socketException) {             
                UnityEngine.Debug.Log("Socket exception: " + socketException);         
            }  
            
            break; 
        }

        // nwStream = client.GetStream();


        using(nwStream = client.GetStream()){
            byte[] bytesToRead;
            int bytesRead;
            while (!stopThread)
            {
                //---read back the text---
                try
                {  
                    bytesToRead = new byte[client.ReceiveBufferSize];
                    bytesRead = nwStream.Read(bytesToRead, 0, client.ReceiveBufferSize);
                    // UnityEngine.Debug.Log("get data");
                }catch (System.IO.IOException e)
                {
                    UnityEngine.Debug.Log("ERROR: can't read from socket" + e);
                    // Console.WriteLine("ERROR: can't read from socket");
                    break;
                }
                string receivedString = Encoding.ASCII.GetString(bytesToRead, 0, bytesRead);
                // UnityEngine.Debug.Log(receivedString);
                List<string> result = receivedString.Remove(receivedString.Length - 1, 1).Remove(0, 1).Split(',').ToList();
                if (result[0] == "THERMOPILE_COG")
                {
                    try{
                        _cogValue = (float)Convert.ToDouble(result[1]); // difference of two temperature values
                        // this value is the difference between a temple measurement and the tip of the nose
                        //   (i.e., TEMPLE_TEMP - NOSE_TEMP = THERMOPILE_COG)
                        // UnityEngine.Debug.Log(result[1]);
                    }catch (Exception e)
                    {
                        Console.WriteLine("{0} Exception caught.", e);
                    }
                
                }
                else if (result[0] == "THERMOPILE_TEMPLE")
                {
                    try{
                        // this value is temperature, in Kelvin, of the middle thermopile of the temple thermopile array of 3
                        _templeTempValue = (float)Convert.ToDouble(result[1]); // kelvin
                        // UnityEngine.Debug.Log(result[0]);
                    }catch (Exception e)
                    {
                        Console.WriteLine("{0} Exception caught.", e);
                    }

                }
                else if (result[0] == "SGP_GAS")
                {
                    try{
                        // reference datasheet for SGP41 on how the indices are defined
                        float voc_index = (float)Convert.ToDouble(result[1]); // range: 1-500, ambient average: 100
                        float nox_index = (float)Convert.ToDouble(result[2]); // range: 1-500, ambient average: 1
                        
                        _aqValues[0] = voc_index;
                        _aqValues[1] = nox_index;
                        UnityEngine.Debug.Log(result[2]);

                    }catch (Exception e)
                    {
                        Console.WriteLine("{0} Exception caught.", e);
                    }
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
                    try{
                        float IAQ = (float)Convert.ToDouble(result[1]);
                        _iaqValue = IAQ;
                        // UnityEngine.Debug.Log(result[0]);
                    }catch (Exception e)
                    {
                        Console.WriteLine("{0} Exception caught.", e);
                    }

                }
                else if (result[0] == "BME_CO2_EQ")
                {
                    try{
                        float co2_eq = (float)Convert.ToDouble(result[1]); // co2 equivalent estimate [ppm]
                        _aqFaceValues[0] = co2_eq;
                        // UnityEngine.Debug.Log(result[0]);
                    }catch (Exception e)
                    {
                        Console.WriteLine("{0} Exception caught.", e);
                    }
                }
                else if (result[0] == "BME_VOC_EQ")
                {
                    try{
                        float voc_index = (float)Convert.ToDouble(result[1]); // breath VOC concentration estimate [ppm]
                        _aqValues[2] = voc_index;
                        // UnityEngine.Debug.Log(result[0]);
                    }catch (Exception e)
                    {
                        Console.WriteLine("{0} Exception caught.", e);
                    }

                }
                else if (result[0] == "SHT45")
                {
                    try{
                    _ambientTemp = (float)Convert.ToDouble(result[1]); // Celsius
                    _ambienHum = (float)Convert.ToDouble(result[2]); // relative humidity, influenced by person breathing
                    // UnityEngine.Debug.Log(result[2]);
                    }catch (Exception e)
                    {
                        Console.WriteLine("{0} Exception caught.", e);
                    }

                }
                else if (result[0] == "Lux")
                {
                    try{
                        float lux = (float)Convert.ToDouble(result[1]); // light intensity [lux]
                        _luxValue = lux;
                        // UnityEngine.Debug.Log(result[0]);
                    }catch (Exception e)
                    {
                        Console.WriteLine("{0} Exception caught.", e);
                    }
                    
                }
                else if (result[0] == "Blink")
                {
                    try{
                        // !!!!! NOT IMPLEMENTED !!!!! 
                        float blink = (float)Convert.ToDouble(result[1]); // blink: 1, no_blink: 0
                        // UnityEngine.Debug.Log(result[0]);

                    }catch (Exception e)
                    {
                        Console.WriteLine("{0} Exception caught.", e);
                    }
                }
                else if (result[0] == "Spec")
                {
                    float red = (float)Convert.ToDouble(result[1]); // 0->1.0
	                float green = (float)Convert.ToDouble(result[2]); // 0->1.0
		            float blue = (float)Convert.ToDouble(result[3]); // 0->1.0
                    _spectralValue = new Color(red, green, blue);
                    // Console.WriteLine(result[0]);
                }
            }  
        }
          



	}  	




}
