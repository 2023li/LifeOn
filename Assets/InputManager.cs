using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using Moyo.Unity;
using System.Collections.Generic; // 你的 MonoSingleton

public class InputManager : MonoSingleton<InputManager>
{
    public Camera RealCamera { get; private set; }
    public Vector3 MousePos { get; private set; }

    /// <summary>全局指针位置变更（无状态限制，用于别处需要）</summary>
    public event Action<Vector2> OnMouseMove;

    // —— 建造流程专用事件（与 .inputactions: Building map 对齐）——
    public event Action<Vector2> Building_OnChangeCoordinates;
    public event Action Building_OnConfirmPlacement;
    public event Action Building_OnConfirmConstruction;

    public LOControlsMaps inputActionMap;

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

        inputActionMap.Global.Back.performed += ctx =>
        {
            foreach(IBackHandler h in _backHandlers)
                if (h.TryHandleBack()) return;
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
       

    }

    private bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    // 供外部开关建造 map
    public void EnableBuildingMap() => inputActionMap.Building.Enable();
    public void DisableBuildingMap() => inputActionMap.Building.Disable();

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }


    private readonly List<IBackHandler> _backHandlers = new();
    public void Register(IBackHandler h)
    {
        _backHandlers.Add(h);
        _backHandlers.Sort((a, b) => b.Priority.CompareTo(a.Priority));

    }

}
