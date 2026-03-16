using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;
using UnityEngine.UI;
using Agora.Rtc;
using Agora_RTC_Plugin.API_Example;

namespace agora_sample_code
{
    /// <summary>
    ///   Demo for sending individual audio stream into audio source instance.
    ///   This demo does not manage local user's camera for simplicity.
    /// </summary>
    public class UserAudioFrame2SourceSample : MonoBehaviour
    {
        [SerializeField]
        private string APP_ID = "";

        [SerializeField]
        private string TOKEN = "";

        [SerializeField]
        private string CHANNEL_NAME = "YOUR_CHANNEL_NAME";
        public Text logText;
        internal agora_utilities.Logger _logger;
        internal IRtcEngine _rtcEngine = null;

        [SerializeField] Transform rootSpace;
        [SerializeField] GameObject userPrefab;
        Dictionary<uint, GameObject> RemoteUserObject = new Dictionary<uint, GameObject>();
        public readonly int CHANNEL = 1;
        public readonly int PULL_FREQ_PER_SEC = 100;
        public readonly int SAMPLE_RATE = 48000; // this should = CLIP_SAMPLES x PULL_FREQ_PER_SEC
        public readonly int CLIP_SAMPLES = 320;

#if NET_4_6 || NET_STANDARD_2_0
        BlockingCollection<System.Action> blockingCollection;
        HashSet<uint> RemoteUserConfigured = new HashSet<uint>();
        Dictionary<uint, UserAudioFrameHandler> RemoteUserHandlers = new Dictionary<uint, UserAudioFrameHandler>(); 

        public bool MuteAllRemoteAudio= false;
        private void Awake()
        {
            blockingCollection = new BlockingCollection<System.Action>();
            if (userPrefab == null)
            {
                Debug.LogWarning("User prefab wasn't assigned, generating primitive object as prefab.");
                MakePrefab();
            }
        }

        void Start()
        {
            PermissionHelper.RequestMicrophontPermission();
            PermissionHelper.RequestCameraPermission();
            CheckAppId();
            InitEngine();
            JoinChannel();
        }

        void Update()
        {
            System.Action action;
            while (blockingCollection.TryTake(out action)) action();
        }

        void CheckAppId()
        {
            _logger =  new agora_utilities.Logger(logText);
            _logger.DebugAssert(APP_ID.Length > 10, "Please fill in your appId in VideoCanvas!!!!!");
        }

        void InitEngine()
        {
           _rtcEngine = RtcEngine.CreateAgoraRtcEngine();
            UserEventHandler handler = new UserEventHandler(this);
            var context = new RtcEngineContext();
            context.appId = APP_ID;
            context.channelProfile = CHANNEL_PROFILE_TYPE.CHANNEL_PROFILE_LIVE_BROADCASTING;
            context.audioScenario = AUDIO_SCENARIO_TYPE.AUDIO_SCENARIO_GAME_STREAMING;
            context.areaCode = AREA_CODE.AREA_CODE_GLOB;

            var rc = _rtcEngine.Initialize(context);
            Debug.Assert(rc == 0, "rtcEngine init failed");
            rc = _rtcEngine.InitEventHandler(handler);
            Debug.Assert(rc == 0, "rtcEngine init handler failed");

            _rtcEngine.EnableAudio();
            _rtcEngine.EnableLocalAudio(true);

            _rtcEngine.SetClientRole(CLIENT_ROLE_TYPE.CLIENT_ROLE_BROADCASTER);
            _rtcEngine.EnableVideo();

            _rtcEngine.SetLogFile(Application.persistentDataPath + "/log.txt");

            _rtcEngine.SetPlaybackAudioFrameBeforeMixingParameters(SAMPLE_RATE, CHANNEL);
            _rtcEngine.RegisterAudioFrameObserver(new AudioFrameObserver(this),
                 AUDIO_FRAME_POSITION.AUDIO_FRAME_POSITION_BEFORE_MIXING ,
                 OBSERVER_MODE.RAW_DATA);
            // Suppress remote audio from Mixed playback
            _rtcEngine.AdjustPlaybackSignalVolume(0);
        }

        void JoinChannel()
        {
            var option = new ChannelMediaOptions();
            option.autoSubscribeVideo.SetValue(true);
            option.autoSubscribeAudio.SetValue(true);
            option.publishMicrophoneTrack.SetValue(true);
            option.publishCameraTrack.SetValue(false);
            option.clientRoleType.SetValue(CLIENT_ROLE_TYPE.CLIENT_ROLE_BROADCASTER);
            option.channelProfile.SetValue(CHANNEL_PROFILE_TYPE.CHANNEL_PROFILE_LIVE_BROADCASTING);

            //_rtcEngine.SetParameters("che.audio.external_render", true);
            _rtcEngine.JoinChannel(TOKEN, CHANNEL_NAME, "", 0);
        }

        int userCount = 0;
        public Vector3[] SpawnPosition;
        internal void SpawnUser(uint uid)
        {
            GameObject go = Instantiate(userPrefab);
            RemoteUserObject[uid] = go;
            go.transform.SetParent(rootSpace);
            go.transform.localScale = Vector3.one;
            if (userCount < SpawnPosition.Length) {
                go.transform.localPosition = SpawnPosition[userCount];
            }
            else
            {
                go.transform.localPosition = new Vector3(userCount * 2, 0, 0);
            }

            VideoSurface v = go.AddComponent<VideoSurface>();
            v.SetForUser(uid, CHANNEL_NAME, VIDEO_SOURCE_TYPE.VIDEO_SOURCE_REMOTE);
            v.SetEnable(true);
            userCount++;
        }


        internal void HandleOnUserOffline(uint uid)
        {
            dispatch(() => { _logger.UpdateLog("Dispatched log + OFFLINE uid = " + uid); });

            lock (RemoteUserConfigured)
            {
                if (RemoteUserObject.ContainsKey(uid))
                {
                    Destroy(RemoteUserObject[uid]);
                    RemoteUserObject.Remove(uid);
                }
                if (RemoteUserHandlers.ContainsKey(uid))
                {
                    RemoteUserHandlers.Remove(uid);
                }

                if (RemoteUserConfigured.Contains(uid))
                {
                    RemoteUserConfigured.Remove(uid);
                }
            }
        }

        public void dispatch(System.Action action)
        {
            blockingCollection.Add(action);
        }

        int count = 0;
        const int MAXAUC = 10;
        internal void HandlePlaybackAudioFrameBeforeMixing(uint uid, AudioFrame audioFrame)
        {
            UserAudioFrameHandler userAudio = null;
            // The audio stream info contains in this audioframe, we will use this construct the AudioClip
            lock (RemoteUserHandlers)
            {
                // if (count < MAXAUC)
                if (!RemoteUserHandlers.ContainsKey(uid) && RemoteUserObject.ContainsKey(uid))
                {
                   // Debug.LogWarning($"RemoteUserHandlers.ContainsKey({uid}):" + RemoteUserHandlers.ContainsKey(uid) + $" RemoteUserObject.ContainsKey({uid}):" + RemoteUserObject.ContainsKey(uid));
                    GameObject go = RemoteUserObject[uid];
                    if (go != null)
                    {
                        dispatch(() =>
                        {
                            UserAudioFrameHandler userAudio = go.GetComponent<UserAudioFrameHandler>();
                            if (userAudio == null)
                            {
                                userAudio = go.AddComponent<UserAudioFrameHandler>();
                            }
                            userAudio.Init(uid, audioFrame);
                            RemoteUserHandlers[uid] = userAudio;
                            go.SetActive(true);
                        });
                    }
                    else
                    {
                        dispatch(() =>
                        {
                            _logger.UpdateLog("Uid: " + uid + " <> no go");
                        });
                    }
                }

                if (RemoteUserHandlers.ContainsKey(uid))
                {
                    userAudio = RemoteUserHandlers[uid];
                }
            }

            if (userAudio != null)
            {
                // delegate the audio frame to the user audio frame handler, which contains the AudioSource component.
                userAudio.HandleAudioFrame(uid, audioFrame);
            }
        }

        private void OnDestroy()
        {
            Debug.Log("OnDestroy: Agora Clean up");
            if (_rtcEngine != null)
            {
                _rtcEngine.LeaveChannel();

                // Important: clean up the engine as the last step
                _rtcEngine.Dispose();
                _rtcEngine = null;
            }
        }
        protected virtual void MakePrefab()
        {
            Debug.Log("Generating cube as prefab.");
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            userPrefab = go;
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0, 45f, 45f));
            MeshRenderer mesh = GetComponent<MeshRenderer>();
            if (mesh != null)
            {
                mesh.material = new Material(Shader.Find("Unlit/Texture"));
            }
            go.SetActive(false);
        }
#else
    public string USE_NET46 = "PLEASE USE .NET 4.6 or Standard 2.0";
    void Start()
    {
        Debug.LogError("PLease use .Net 4.6 or standard 2.0 to run this demo!!");
    }
#endif

    }

    internal class UserEventHandler : IRtcEngineEventHandler
    {
        private readonly UserAudioFrame2SourceSample _app;

        internal UserEventHandler(UserAudioFrame2SourceSample agoraController)
        {
            _app = agoraController;
        }

        #region -- Agora Event Callbacks --
        public override void OnError(int err, string msg)
        {
            _app._logger.UpdateLog(string.Format("OnError err: {0}, msg: {1}", err, _app._rtcEngine.GetErrorDescription(err)));
        }
        public override void OnJoinChannelSuccess(RtcConnection connection, int elapsed)
        {
            _app._logger.UpdateLog(string.Format("OnJoinChannelSuccess channelName: {0}, uid: {1}, elapsed: {2}",
                    connection.channelId, connection.localUid, elapsed));
        }
        public override void OnUserJoined(RtcConnection connection, uint uid, int elapsed)
        {
            _app._logger.UpdateLog(string.Format("OnUserJoined uid: ${0} elapsed: ${1}", uid, elapsed));
            _app.SpawnUser(uid);
        }
        public override void OnUserOffline(RtcConnection connection, uint uid, USER_OFFLINE_REASON_TYPE reason)
        {
            _app._logger.UpdateLog(string.Format("OnUserOffLine uid: {0}, reason: {1}", uid, (int)reason));
            _app.HandleOnUserOffline(uid);
        }
        #endregion
    }

    internal class AudioFrameObserver : IAudioFrameObserver
    {
        internal UserAudioFrame2SourceSample _app;
        internal AudioFrameObserver(UserAudioFrame2SourceSample app)
        {
            _app = app;
        }

        public override bool OnPlaybackAudioFrameBeforeMixing(string channel_id,
                                                        uint uid,
                                                        AudioFrame audioFrame)
        {
            _app.HandlePlaybackAudioFrameBeforeMixing(uid, audioFrame);
            return false;
        }
    }   
}