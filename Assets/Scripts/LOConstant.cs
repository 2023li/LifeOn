using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

public static class LOConstant
{

    public static class SceneName
    {
        public const string Boot = "Boot";
        public const string Start = "Start";
        public const string Game = "Game";
        public const string Transition = "Transition";

    }
    public static class InputPriority
    {

        //
        public const int Priority_BuildingBuilder = 10;
        public const int Priority_暂停面板 = 16;
        public const int Priority_UI控制器 = 15;





        public const int Priority_相机监听鼠标滚轮 = 20;
    }


    

}
