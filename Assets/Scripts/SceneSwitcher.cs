using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AutoSceneSwitcher : MonoBehaviour
{
    [Header("触发设置")]
    [Tooltip("需要碰撞的物体标签")]
    public string playerTag = "Player";
    
    [Tooltip("等待时间，建议测试时先改成 3 秒")]
    public float waitTime = 30f;
    
    [Tooltip("要跳转的场景名称（如果不填，默认跳到下一个场景）")]
    public string sceneToLoad;

    [Header("UI 设置")]
    [Tooltip("黑色的 UI Image")]
    public Image blackScreen;
    public float fadeTime = 2f;

    private bool isTriggered = false; // 防止重复触发

    void Start()
    {
        // 确保一开始黑屏是透明的，且不挡鼠标
        if (blackScreen != null)
        {
            blackScreen.color = new Color(0, 0, 0, 0);
            blackScreen.raycastTarget = false;
        }
    }

    // ==========================================
    // 万能检测区：无论你是3D/2D、Trigger还是Collision，全部拦截！
    // ==========================================
    void OnTriggerEnter(Collider other) { CheckHit(other.gameObject); }
    void OnCollisionEnter(Collision col) { CheckHit(col.gameObject); }
    void OnTriggerEnter2D(Collider2D other) { CheckHit(other.gameObject); }
    void OnCollisionEnter2D(Collision2D col) { CheckHit(col.gameObject); }

    // 核心判断逻辑
    void CheckHit(GameObject hitObject)
    {
        if (!isTriggered && hitObject.CompareTag(playerTag))
        {
            isTriggered = true;
            Debug.Log($"[SceneSwitcher] 当前物体:{name} 开始切换到:{sceneToLoad}");
            StartCoroutine(SwitchRoutine());
        }
    }

    // 计时、黑屏与切换逻辑
    IEnumerator SwitchRoutine()
    {
        // 1. 倒计时部分（会在控制台打印，证明代码没死机）
        float timeLeft = waitTime;
        while (timeLeft > 0)
        {
            // 打印剩余时间（只在整秒时打印）
            if (Mathf.Floor(timeLeft) == timeLeft) 
            {
                Debug.Log("距离切换场景还剩: " + timeLeft + " 秒");
            }
            yield return new WaitForSeconds(1f);
            timeLeft -= 1f;
        }

        Debug.Log("时间到！开始渐黑...");

        // 2. 屏幕渐黑部分
        if (blackScreen != null)
        {
            float timer = 0;
            while (timer < fadeTime)
            {
                timer += Time.deltaTime;
                float alpha = timer / fadeTime;
                blackScreen.color = new Color(0, 0, 0, alpha);
                yield return null;
            }
        }

        // 3. 场景切换部分
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }
    }
}