using System;
using System.Collections.Concurrent;
using Il2CppInterop.Runtime.Injection;
using LightInDark.Core;
using UnityEngine;
namespace Light.Utilities;
/// <summary>
/// 调度器
/// </summary>
public class Dispatcher : MonoBehaviour
{
    static Dispatcher _instance;
    static readonly object _lock = new object();
    readonly ConcurrentQueue<Action> _executionQueue = new ConcurrentQueue<Action>();
    static Dispatcher()
    {
        try
        {
            ClassInjector.RegisterTypeInIl2Cpp<Dispatcher>();
        }
        catch (Exception ex)
        {
            LightLogger.LogError("[Dispatcher.StaticCtor]", ex);
        }
    }

    public static void Initialize()
    {
        _ = Instance;
    }
    public static Dispatcher Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = FindObjectOfType<Dispatcher>();
                        if (_instance == null)
                        {
                            GameObject go = new GameObject("MainThreadDispatcher");
                            _instance = go.AddComponent<Dispatcher>();
                            DontDestroyOnLoad(go);
                        }
                    }
                }
            }
            return _instance;
        }
    }

    public void Enqueue(Action action)
    {
        if (action == null) return;
        _executionQueue.Enqueue(action);
    }

    void Update()
    {
        while (_executionQueue.TryDequeue(out Action action))
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Dispatcher 执行任务时出现异常: {ex}");
            }
        }
    }
    void OnDestroy()
    {
        while (_executionQueue.TryDequeue(out _)) { }
    }
}