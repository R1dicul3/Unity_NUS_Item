using System;
using UnityEngine;

namespace SaveSystem
{
    /// <summary>
    /// 全局游戏计时器。累计游玩时间，不受 Time.timeScale 影响。
    /// 单例 + DontDestroyOnLoad，自动创建。
    /// </summary>
    public class GameTimer : MonoBehaviour
    {
        public static GameTimer Instance { get; private set; }

        private float elapsedTime = 0f;
        private bool isRunning = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            if (Instance == null)
            {
                GameObject go = new GameObject("GameTimer");
                go.AddComponent<GameTimer>();
            }
        }

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (isRunning)
            {
                elapsedTime += Time.unscaledDeltaTime;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>开始计时。</summary>
        public void StartTimer()
        {
            isRunning = true;
        }

        /// <summary>暂停计时。</summary>
        public void StopTimer()
        {
            isRunning = false;
        }

        /// <summary>重置计时器为 0。</summary>
        public void ResetTimer()
        {
            elapsedTime = 0f;
        }

        /// <summary>设置已累积的时间（用于加载存档时恢复）。</summary>
        public void SetElapsedTime(float seconds)
        {
            elapsedTime = Mathf.Max(0f, seconds);
        }

        /// <summary>获取当前累计时间（秒）。</summary>
        public float GetElapsedTime()
        {
            return elapsedTime;
        }

        /// <summary>获取格式化后的时间字符串 "HH:MM:SS"。</summary>
        public string GetFormattedTime()
        {
            TimeSpan ts = TimeSpan.FromSeconds(elapsedTime);
            return $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        }
    }
}
