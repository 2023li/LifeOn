using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using Moyo.Unity;
using System.Collections.Generic;
using Sirenix.OdinInspector;

public class InputManager : MonoSingleton<InputManager>
{
    public Camera RealCamera { get; private set; }
    public Vector3 MousePos { get; private set; }
    public Vector2 MouseWheelDelta { get; private set; }

    /// <summary>全局指针位置变更（无状态限制，用于别处需要）</summary>
    public event Action<Vector2> OnMouseMove;

    // —— 建造流程专用事件（与 .inputactions: Building map 对齐）——
    public event Action<Vector2> Building_OnChangeCoordinates;
    public event Action Building_OnConfirmPlacement;
    public event Action Building_OnConfirmConstruction;
    

    /// <summary>仅在 GamePlay.MoveCamera 激活时为 true。</summary>
    public event Action<bool> GamePlay_OnMoveCamera;

    public LOControlsMaps inputActionMap;

    public bool IsGamePlayActive => inputActionMap != null && inputActionMap.GamePlay.enabled;

    protected override void Initialize()
    {
        base.Initialize();
        RealCamera = Camera.main;

        inputActionMap = new LOControlsMaps();

        // Global 常开
        inputActionMap.Global.Enable();

        // 指针位置（PassThrough）
        inputActionMap.Global.MousePostionChange.performed += ctx =>
        {
            MousePos = ctx.ReadValue<Vector2>();
            OnMouseMove?.Invoke(MousePos);

            // 仅当 Building map 已启用时，转发为“建造坐标改变”
            if (inputActionMap.Building.enabled)
            {
                Building_OnChangeCoordinates?.Invoke(MousePos);
            }
        };

        inputActionMap.Global.MouseWheelChanges.performed += ctx =>
        {
            Vector2 delta = ctx.ReadValue<Vector2>();
            if (delta.sqrMagnitude <= 0f) return;

            MouseWheelDelta = delta;
            for (int i = 0; i < _mouseWheelHandlers.Count; i++)
            {
                var handler = _mouseWheelHandlers[i];
                if (handler == null)
                {
                    _mouseWheelHandlers.RemoveAt(i);
                    i--;
                    continue;
                }
                if (handler.TryHandleSlide(delta.y)) return;
            }
        };

        inputActionMap.Global.Back.performed += ctx =>
        {
            foreach (IBackHandler h in _backHandlers)
                if (h.TryHandleBack())
                {
                    Debug.Log($"返回被 {h.Priority} 消费");
                    return;
                }
        };


        // Building 默认关闭，进入建造模式时再打开
        inputActionMap.Building.Disable();

        // 确认/取消：只订 performed；确认前做 UI 命中过滤
        inputActionMap.Building.ConfirmPlacement.performed += ctx =>
        {
            if (IsPointerOverUI()) return;
            Building_OnConfirmPlacement?.Invoke();
        };

       

        inputActionMap.Building.ConfirmConstruction.performed += ctx =>
        {
            if (IsPointerOverUI()) return;
            Building_OnConfirmConstruction?.Invoke();
        };

        // GamePlay 默认关闭，仅在游戏主体激活时启用
        inputActionMap.GamePlay.Disable();
        inputActionMap.GamePlay.MoveCamera.started += ctx => GamePlay_OnMoveCamera?.Invoke(true);
        inputActionMap.GamePlay.MoveCamera.performed += ctx => GamePlay_OnMoveCamera?.Invoke(true);
        inputActionMap.GamePlay.MoveCamera.canceled += ctx => GamePlay_OnMoveCamera?.Invoke(false);


    }

    private bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    // 供外部开关建造 map
    public void EnableBuildingMap() => inputActionMap.Building.Enable();
    public void DisableBuildingMap() => inputActionMap.Building.Disable();
    public bool IsBuildingMap() => inputActionMap.Building.enabled;

    [Button]
    public void EnableGamePlayMap() => inputActionMap.GamePlay.Enable();
    public void DisableGamePlayMap()
    {
        inputActionMap.GamePlay.Disable();
        GamePlay_OnMoveCamera?.Invoke(false);
    }
    public bool IsGamePlayMap() => inputActionMap.GamePlay.enabled;

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }


    private readonly List<IBackHandler> _backHandlers = new();
    private readonly List<ISlideHandler> _mouseWheelHandlers = new();

    public void Register(IBackHandler h)
    {
        _backHandlers.Add(h);
        _backHandlers.Sort((a, b) => b.Priority.CompareTo(a.Priority));

    }
    public void UnRegister(IBackHandler handler)
    {
        if (handler == null) return;
        _backHandlers.Remove(handler);
    }
    public void Register(ISlideHandler handler)
    {
        if (handler == null) return;
        if (_mouseWheelHandlers.Contains(handler)) return;

        _mouseWheelHandlers.Add(handler);
        _mouseWheelHandlers.Sort((a, b) => b.Priority.CompareTo(a.Priority));
    }
    public void UnRegister(ISlideHandler handler)
    {
        if (handler == null) return;
        _mouseWheelHandlers.Remove(handler);
    }




    public Vector3 ScreenToWorldPointNoZ()
    {
        var pos = RealCamera.ScreenToWorldPoint(MousePos);
        return new Vector3(pos.x, pos.y, 0);


    }





   



}
