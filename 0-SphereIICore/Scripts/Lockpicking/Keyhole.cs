using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

/* 
 * Thank you for using my Lockpicking asset! I hope it works great in your game. You can extend it to suit your game by
 * manipulating some of the code below. For instance, if your player can have a various level of "lockpicking" skill,
 * you may consider multiplying the value of lockGive by their skill, so that a higher skilled player would find it
 * easier to open a lock.
 *
 * Enjoy!
 */

namespace Lockpicking
{
    [RequireComponent(typeof(LockEmissive))]
    public class Keyhole : MonoBehaviour
    {
        // Events
        UnityEvent lockpickBroke = new UnityEvent();
        UnityEvent lockOpen = new UnityEvent();

        [Header("Player Input")]
        public float openPressure = 0f;
        public float lockpickPressure = 0f;


        [Header("Speed Settings")]
        [Tooltip("Speed of the lockpick when input value is full.")]
        [Range(1f, 720f)] public float turnSpeedLockpick = 25f;

        [Tooltip("Speed of the entire keyhole when input value is full.")]
        [Range(1f, 720f)] public float turnSpeedKeyhole = 25f;

        [Tooltip("Speed at which the lock will return to normal when the input value is 0.")]
        [Range(1f, 720f)] public float returnSpeedKeyhole = 150f;

        [Tooltip("Maximum shake distance per shake change.")]
        [SerializeField] private float maxShake = 0.5f;

        [Tooltip("Amount of time between shake changes when shaking.")]
        [SerializeField] private float shakeTime = 0.1f;


        [Header("Pick Settings")]
        [Tooltip("Starting angle of the lock pick.")]
        [SerializeField] private Vector3 _pickAnglesDefault;

        [Tooltip("Minimum angle the lock pick can travel to.")]
        [SerializeField] private float _pickAngleMin;

        [Tooltip("Maximum angle the lock pick can travel to.")]
        [SerializeField] private float _pickAngleMax;


        [Header("Keyhole Settings")]
        [Tooltip("Starting angle of the keyhole.")]
        [SerializeField] private float _keyholeAngleDefault = 0;

        [Tooltip("Maximum angle of the keyhole. At this angle, the lock will open.")]
        [SerializeField] private float _keyholeAngleMax = 0;


        [Header("Lock Settings")]
        [Tooltip("If true, lock details will be randomized on awake")]
        public bool resetOnAwake = true;

        [Tooltip("Minimum angle the lock can be set to.")]
        [Range(0f, 180f)] public float minLockAngle = -90f;

        [Tooltip("Maximum angle the lock can be set to.")]
        [Range(0f, 180f)] public float maxLockAngle = 90f;

        [Tooltip("Minimum distance (plus and minus) from the lock angle that the lock will open.")]
        [Range(1f, 180f)] public float minGiveAmount = 1f;

        [Tooltip("Maximum distance (plus and minus) from the lock angle that the lock will open.")]
        [Range(1f, 180f)] public float maxGiveAmount = 45f;

        [Tooltip("Minimum distance for the pick to be in for the lock will turn partially.")]
        [Range(5f, 180f)] public float minCloseDistance = 5f;

        [Tooltip("Maximum distance for the pick to be in for the lock will turn partially.")]
        [Range(5f, 180f)] public float maxCloseDistance = 20f;

        [Tooltip("Amount of time to ignore player input after a lock pick breaks.")]
        [Range(0f, 5f)] public float breakPause = 2f;


        [Header("Lock Details")]
        [Tooltip("True if the lock is already open (unlocked).")]
        [SerializeField] private bool _lockIsOpen;
        public bool LockIsOpen
        {
            get
            {
                return _lockIsOpen;
            }
            set
            {
                _lockIsOpen = value;
            }
        }
        [Tooltip("The exact angle the lock is set to.")]
        float _lockAngle;
        public float LockAngle
        {
            get { return _lockAngle; }
            set { _lockAngle = Mathf.Clamp(value, minLockAngle, maxLockAngle); }
        }
        [Tooltip("The distance to/from the LockAngle the lock pick needs to be in for the lock to open successfully.")]
        [SerializeField] private float _lockGive;
        public float LockGive
        {
            get { return _lockGive; }
            set
            {
                _lockGive = Mathf.Clamp(value, 1, maxGiveAmount);
            }
        }
        [Tooltip("If the lock pick is within this distance to the angle range which the lock will open, the lock will turn partially when an open attempt is made.")]
        [SerializeField] private float _closeDistance;
        public float CloseDistance
        {
            get { return _closeDistance; }
            set { _closeDistance = Mathf.Clamp(value, 5, maxCloseDistance); }
        }
        [Tooltip("The amount of time before a lock pick breaks when the lock is unable to be opened, but the player is attempting to open it.")]
        [Range(0f, 5f)] public float breakTime = 1f;


        [Header("Animation Trigger Strings")]
        public string openTrigger = "OpenPadlock";
        public string closeTrigger = "ClosePadlock";
        public string lockPickBreakTrigger = "BreakLockpick1";
        public string lockPickInsertTrigger = "InsertLockpick";

        // Private animation hashes
        private int _openTrigger;
        private int _closeTrigger;
        private int _lockpickBreakTrigger;
        private int _lockpickInsertTrigger;

        [Header("Plumbing")]
        public GameObject keyhole; // The keyhole with lockpick A that turns the entire keyhole object to open it
        public GameObject lockpickObject; // The lockpick that turns to match the secret lockAngle
        public Animator lockpickAnimator; // Animator on the lockpick in the lockpickObject
        private Animator _padlockAnimator;
        public GameObject padlock1; // Link to the padlock 1 game object
        public GameObject button;
        private LockEmissive _lockEmissive; // Link to the lockEmissive script on this object
        public LocksetAudio audioTurnClick;
        public LocksetAudio audioSqueek;
        public LocksetAudio audioOpen;
        public LocksetAudio audioJiggle;
        public LocksetAudio audioJiggle2;
        public LocksetAudio audioJiggle3;
        public LocksetAudio audioPadlockOpen;
        public LocksetAudio audioPadlockJiggle;
        public LocksetAudio audioLockpickBreak;
        public LocksetAudio audioLockpickEnter;
        public LocksetAudio audioLockpickClick;

        // Audio Settings
        [Range(0f, 1f)] public float clickVolumeMin = 0.1f;
        [Range(0f, 1f)] public float clickVolumeMax = 0.4f;
        [Range(0, 100)] public int clickChance = 100;
        [Range(0f, 15f)] public float clickRate = 10f;
        [Range(0f, 1f)] public float squeekVolumeMin = 0.1f;
        [Range(0f, 1f)] public float squeekVolumeMax = 0.4f;
        [Range(0, 100)] public int squeekChance = 50;
        [Range(0f, 360f)] public float squeekRate = 20f;

        // Private variables
        private float breakCounter; // Counter for taking a break after a broken lockpick
        private float breakTimeCounter;
        private float shakeTimer; // Counter for the shake Time
        private bool isShaking; // Whether we are currently shaking or not
        private Vector3 preshakeLockpick; // Saves the pre-shake angles
        private Vector3 preshakeKeyhole; // Saves the pre-shake angles
        private float _lockpickAnglePrev;
        private float squeekTimer = 0f;
        public bool buttonDown;

        public float BreakTimeCounter()
        {
            return breakTimeCounter;
        }
        public float LockPickAngle()
        {
            return GetAngle(LockpickAngles().z);
            }
        public float KeyholeAngle() { return GetAngle(KeyholeAngles().z); }

        
        void OnValidate()
        {
            LockAngle = LockAngle;
            LockGive = LockGive;
            CloseDistance = CloseDistance;

            _pickAngleMin = Mathf.Clamp(_pickAngleMin, minLockAngle, maxLockAngle);
            _pickAngleMax = Mathf.Clamp(_pickAngleMax, minLockAngle, maxLockAngle);

            minGiveAmount = Mathf.Clamp(minGiveAmount, 1f, 360f);
            maxGiveAmount = Mathf.Clamp(maxGiveAmount, 1f, 360f);
            minCloseDistance = Mathf.Clamp(minCloseDistance, 5f, 360f);
            maxCloseDistance = Mathf.Clamp(maxCloseDistance, 5f, 360f);
        }
        
        public void EditorOnValidate()
        {
            #if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            #endif
            OnValidate();
        }
        
        void Awake()
        {
            squeekTimer = squeekRate;
            
            _pickAnglesDefault = LockpickAngles();
            _lockEmissive = gameObject.AddComponent<LockEmissive>();

            _padlockAnimator = padlock1.gameObject.AddComponent<Animator>();
            _closeTrigger = Animator.StringToHash(closeTrigger);
            _openTrigger = Animator.StringToHash(openTrigger);
            _lockpickBreakTrigger = Animator.StringToHash(lockPickBreakTrigger);
            _lockpickInsertTrigger = Animator.StringToHash(lockPickInsertTrigger);

            if (resetOnAwake)
                ResetLock();
        }
        
        void Update()
        {
            PassValuesToEmissiveScript();
            
            if (BreakingForAnimation())
                return;

            HandlePlayerInput();
        }

        private void HandlePlayerInput()
        {
            if (openPressure > 0)
            {
                TryToTurnKeyhole();
            }
            else
            {
                StopShaking();
                ReturnKeyholeToDefaultPosition();
                TurnLockpick(turnSpeedLockpick * lockpickPressure);
            }
        }
        
        private bool BreakingForAnimation()
        {
            if (breakCounter > 0f)
            {
                breakCounter -= Time.deltaTime;
                ReturnKeyholeToDefaultPosition();
                return true;
            }

            return false;
        }
        
        private void ReturnKeyholeToDefaultPosition()
        {
            TurnKeyhole(-returnSpeedKeyhole);
        }

        private void TryToTurnKeyhole()
        {
            if (LockCanTurn())
            {
                TurnKeyhole(turnSpeedKeyhole * openPressure);
                    
                if (KeyholeTurnValue() <= 0 && LockpickIsInPosition())
                    OpenLock();
            }
            else
            {
                Shake();
            }
        }

        private void PassValuesToEmissiveScript()
        {
            _lockEmissive.breakpointValue = Mathf.Clamp(breakTimeCounter / breakTime, 0, 1);
            _lockEmissive.successValue = Mathf.Clamp(KeyholeTurnValue(), 0, 1);
        }

        private bool LockpickIsInPosition()
        {
          return  LockPickAngle() < _lockAngle + _lockGive && LockPickAngle() > _lockAngle - _lockGive;
        }

        private void Shake()
        {
            // If we are not already shaking, save the original rotations.
            if (!isShaking)
            {
                if (audioPadlockJiggle && padlock1.activeSelf)
                {
                    audioPadlockJiggle.PlayLoop();
                }
                else if (audioJiggle && !padlock1.activeSelf)
                {
                    audioJiggle.PlayLoop();
                    if ( audioJiggle2 != null)
                        audioJiggle2.PlayLoop();
                    if ( audioJiggle3 != null)
                        audioJiggle3.PlayLoop();
                }
                preshakeKeyhole = KeyholeAngles();
                preshakeLockpick = LockpickAngles();
                isShaking = true;
            }
            
            // Check breakTimeCounter to stop shaking at the right time
            breakTimeCounter += Time.deltaTime;
            if (breakTimeCounter > breakTime)
            {
                StopShaking();
                BreakLockpick();
                return;
            }

            shakeTimer -= Time.deltaTime;
            if (shakeTimer <= 0)
            {
                // Start with the current values
                Vector3 newShakeKeyhole = preshakeKeyhole;
                Vector3 newShakeLockpick = preshakeLockpick;

                // Add some modification
                newShakeKeyhole.z += Random.Range(-maxShake, maxShake);
                newShakeLockpick.z += Random.Range(-maxShake, maxShake);
                
                // Set the value + modification
                SetKeyholeAngles(newShakeKeyhole);
                SetLockpickAngles(newShakeLockpick);

                // Reset the timer
                shakeTimer = shakeTime;
            }
        }

        private void StopShaking()
        {
            if (isShaking)
            {
                if (audioPadlockJiggle && padlock1.activeSelf)
                {
                    audioPadlockJiggle.StopLoop();
                }
                else if (audioJiggle && !padlock1.activeSelf)
                {
                    audioJiggle.StopLoop();
                    if (audioJiggle2 != null)
                        audioJiggle2.StopLoop();
                    if (audioJiggle3 != null)
                        audioJiggle3.StopLoop();

                }
                SetKeyholeAngles(preshakeKeyhole);
                SetLockpickAngles(preshakeLockpick);
                isShaking = false;
            }
        }

        private void SetKeyholeAngles(Vector3 value)
        {
            keyhole.transform.localEulerAngles = value;
        }
        
        private void SetLockpickAngles(Vector3 value)
        {
            lockpickObject.transform.localEulerAngles = value;
        }

        private void BreakLockpick()
        {
            if (audioLockpickBreak)
            {
                audioLockpickBreak.PlayOnce();
            }
            breakCounter = breakPause; // Set so we can't do any actions for a short time
            breakTimeCounter = 0f; // Reset the breakCounter
            ResetLockpickPosition(); // Reset the lockpick position
            lockpickAnimator.SetTrigger(_lockpickBreakTrigger); // Play the break animation
            lockpickBroke.Invoke(); // Invoke this event in case other scripts are listening
            
            if (audioLockpickEnter)
            {
                audioLockpickEnter.DelayPlay(1f);
            }
        }

        /// <summary>
        /// Call this when the lock is open successfully.
        /// </summary>
        public void OpenLock()
        {
            if (!_lockIsOpen)
            {
                if (audioOpen && !padlock1.activeSelf)
                {
                    audioOpen.PlayOnce();
                }
                else if (audioPadlockOpen && padlock1.activeSelf)
                {
                    audioPadlockOpen.PlayOnce();
                }
                
                // Only run this on the padlock 1
                if (padlock1.activeSelf)
                    _padlockAnimator.SetTrigger(_openTrigger);

                // Invoke the event for any other scripts that are listening
                lockOpen.Invoke();
                
                _lockIsOpen = true;
            }
        }

        private void DoSqueekAudio(float speed)
        {
            if (audioSqueek)
            {
                if (squeekRate > 0)
                {
                    squeekTimer -= Mathf.Abs(speed) * Time.deltaTime;
                    if (squeekTimer <= 0)
                    {
                        if (Random.Range(0, 100) < squeekChance)
                        {
                            audioSqueek.PlayAudioClip(Random.Range(squeekVolumeMin, squeekVolumeMax));
                        }
                        squeekTimer = squeekRate;
                    }
                }
            }
        }

        private float GetAngle(float eulerAngle)
        {
            float angle = eulerAngle;
            angle %= 360;
            if (angle > 180)
                angle -= 360;
            return angle;
        }

        private void TurnLockpick(float speed)
        {
            // If we are at or outside of our max range, return
            if (LockPickAngle() >= _pickAngleMax && speed > 0 || LockPickAngle() <= _pickAngleMin && speed < 0)
                return;

            // Set the new angle
            Vector3 newAngle = new Vector3(LockpickAngles().x, LockpickAngles().y,
                LockpickAngles().z + speed * Time.deltaTime);
            SetLockpickAngles(newAngle);

            DoClickAudio(speed, newAngle);
        }

        private void DoClickAudio(float speed, Vector3 newAngle)
        {
            float angleMod = newAngle.z % clickRate;
            float prevMod = _lockpickAnglePrev % clickRate;

            if ((speed > 0 && angleMod < prevMod) || (speed < 0 && angleMod > prevMod))
            {
                audioTurnClick.PlayAudioClip(Random.Range(clickVolumeMin, clickVolumeMax));
            }
            
            _lockpickAnglePrev = newAngle.z;
        }

        private Vector3 LockpickAngles()
        {
            if (lockpickObject == null )
            {
                return Vector3.zero;
            }
            return lockpickObject.transform.localEulerAngles;
        }
        
        private void TurnKeyhole(float speed)
        {
            // If we are at or outside of our max range, return
            if (KeyholeAngle() >= _keyholeAngleMax && speed > 0 || KeyholeAngle() <= _keyholeAngleDefault && speed < 0)
                return;

            // Set the new angle
            SetKeyholeAngles(new Vector3(KeyholeAngles().x, KeyholeAngles().y, KeyholeAngles().z + speed * Time.deltaTime));

            DoSqueekAudio(speed);
        }

        private Vector3 KeyholeAngles()
        {
            return keyhole.transform.localEulerAngles;
        }

        public float KeyholeTurnValue()
        {
            return (_keyholeAngleMax - KeyholeAngle()) / (_keyholeAngleMax - _keyholeAngleDefault);
        }

        public void SetLock(float newLockAngle, float newLockGive, float newCloseDistance)
        {
            _lockAngle = newLockAngle;
            _lockGive = newLockGive;
            _closeDistance = newCloseDistance;

            if (audioLockpickEnter)
            {
                audioLockpickEnter.DelayPlay(0.7f);
            }

            ResetLockpickPosition();
            lockpickAnimator.SetTrigger(_lockpickInsertTrigger); // Play the  animation
        }

        public void SetLock(float lockAngleMin, float lockAngleMax, float lockGiveMin,
            float lockGiveMax, float closeDistanceMin, float closeDistanceMax)
        {
            SetLock(Random.Range(lockAngleMin, lockAngleMax), 
                Random.Range(lockGiveMin, lockGiveMax), 
                Random.Range(closeDistanceMin, closeDistanceMax));
        }

        public void ResetLock()
        {
            LockIsOpen = false;
            SetLock(minLockAngle, maxLockAngle, 
                minGiveAmount, maxGiveAmount, 
                minCloseDistance, maxCloseDistance);
        }
        
        public void ResetLockpickPosition()
        {
            SetLockpickAngles(_pickAnglesDefault);
            if (padlock1.activeSelf)
                _padlockAnimator.SetTrigger(_closeTrigger);
        }

        public bool LockCanTurn()
        {
           return !(LockPickAngle() < GetAngle(_lockAngle) - _lockGive - (_closeDistance * KeyholeTurnValue())) &&
            !(LockPickAngle() > GetAngle(_lockAngle) + _lockGive + (_closeDistance * KeyholeTurnValue()));
        }
    }
}