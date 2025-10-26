using UnityEngine;

public class PotionEffectApplier : MonoBehaviour
{
    public void ApplyPotionEffect(Potion potion)
    {
        switch (potion.potionName)
        {
            case "Зеленое зелье":
                HealPlayer(0.5f);
                break;
            case "Красное зелье":
                StartCoroutine(ApplySpeedBoost(12f));
                break;
            case "Синее зелье":
                ApplyPoisonImmunity(1);
                break;
            case "Жёлтое зелье":
                ApplyExperienceBoost(2);
                break;
            case "Зелье Тайфун":
                StartCoroutine(ApplyTyphoonEffect(20f));
                break;
            case "Зелье Непобедимый":
                StartCoroutine(ApplyInvincibility(8f));
                break;
        }
        
        Debug.Log($"Применено зелье: {potion.potionName}");
    }

    private void HealPlayer(float percent)
    {
        Debug.Log($"Восстановлено {percent * 100}% HP");
    }

    private System.Collections.IEnumerator ApplySpeedBoost(float duration)
    {
        Debug.Log($"Скорость увеличена на {duration} секунд");
        yield return new WaitForSeconds(duration);
        Debug.Log("Эффект скорости закончился");
    }

    private void ApplyPoisonImmunity(int waves)
    {
        Debug.Log($"Иммунитет к отравлению на {waves} волн");
    }

    private void ApplyExperienceBoost(int waves)
    {
        Debug.Log($"Буст опыта на {waves} волн");
    }

    private System.Collections.IEnumerator ApplyTyphoonEffect(float duration)
    {
        Debug.Log($"Активирован эффект Тайфун на {duration} секунд");
        yield return new WaitForSeconds(duration);
        Debug.Log("Эффект Тайфун закончился");
    }

    private System.Collections.IEnumerator ApplyInvincibility(float duration)
    {
        Debug.Log($"Активировано бессмертие на {duration} секунд");
        yield return new WaitForSeconds(duration);
        Debug.Log("Эффект бессмертия закончился");
    }
}