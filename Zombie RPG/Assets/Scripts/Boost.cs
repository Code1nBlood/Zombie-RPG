using UnityEngine;

[CreateAssetMenu(fileName = "New Boost", menuName = "Inventory/Boost")]
public class Boost : ScriptableObject
{
    public string boostName;
    public string effect;
    public string duration; 
    public Sprite icon;
    public Color color = Color.white;
}