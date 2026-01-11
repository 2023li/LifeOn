using Moyo.Unity;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using System;

public enum AppLanguage
{
    简体中文,
    English,

}

public enum TableName
{
    BaseString
}

public enum TableKey
{
    Base_是,
    Base_否,
    Base_确认,
    Base_取消,

}

public class LocalStr:Singleton<LocalStr>
{
    protected LocalStr() { }

    public string Base_是;
    public string Base_否;
    public string Base_确认;
    public string Base_取消;
}


public class LocalizationManager : MonoSingleton<LocalizationManager>
{
    public event Action OnAppLanguageChange;

    public const string LocalTale_Base = "BaseString";

    


    [SerializeField] private LocalizationTable baseTable;

    private void  Start()
    {
       _ = AppManager.Instance.BGRunTask(InitLocakStr, () =>
       {
           Debug.Log(LocalStr.Instance.Base_是);
           Debug.Log("字符串加载完成");
       });
    }

    private async UniTask InitLocakStr()
    {
        // 确保 LocalizationSettings 初始化完成（这是一个好习惯）
        await LocalizationSettings.InitializationOperation;

        LocalStr.Instance.Base_是 = await GetStringAsync(TableName.BaseString, TableKey.Base_是);
        LocalStr.Instance.Base_否 = await GetStringAsync(TableName.BaseString, TableKey.Base_否);
        LocalStr.Instance.Base_确认 = await GetStringAsync(TableName.BaseString, TableKey.Base_确认);
        LocalStr.Instance.Base_取消 = await GetStringAsync(TableName.BaseString, TableKey.Base_取消);
    }

    public async Task<string> GetStringAsync(TableName tableName , TableKey key)
    {
        var stringOperation = LocalizationSettings.StringDatabase.GetLocalizedStringAsync(tableName.ToString(), key.ToString());
        await stringOperation;
        return stringOperation.Result;
    }


    public void SelectLanguage(AppLanguage tagerLanguage)
    {
        Locale targetLocale = null;
        switch (tagerLanguage)
        {
            case AppLanguage.简体中文:
                targetLocale = Locale.CreateLocale("zh"); // 同步创建Locale
                break;
            case AppLanguage.English:
                targetLocale = Locale.CreateLocale("en");
                break;
        }

        if (targetLocale != null)
        {
            LocalizationSettings.Instance.SetSelectedLocale(targetLocale);
            OnAppLanguageChange?.Invoke();
        }



    }

}
