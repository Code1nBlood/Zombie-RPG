using UnityEngine;

[CreateAssetMenu(fileName = "New Potion", menuName = "Inventory/Potion")]
public class Potion : ScriptableObject
{
    public string potionName;
    public string effect;
    public string duration;
    public Sprite icon;
    public Color color = Color.white;
}