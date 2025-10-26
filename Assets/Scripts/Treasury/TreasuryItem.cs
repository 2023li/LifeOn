using UnityEngine;

[CreateAssetMenu(menuName = "Game/Treasury Item", fileName = "TI_NewItem")]
public class TreasuryItem : ScriptableObject
{
    [SerializeField] private string id;      // 稳定唯一的存档键（发布后尽量不要改）
    [SerializeField] private string display; // 展示名（可作本地化Key）
    [SerializeField] private Sprite icon;    // UI图标（可选）
    [SerializeField] private bool showInTreasuryPanel = true;

    public string Id => id;
    public string Display => display;
    public Sprite Icon => icon;

    public bool ShowInTreasuryPanel => showInTreasuryPanel;

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 没填Id时自动生成一个（只生成一次）
        if (string.IsNullOrWhiteSpace(id))
        {
            id = System.Guid.NewGuid().ToString("N"); // 32位稳定GUID
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }
#endif
}
