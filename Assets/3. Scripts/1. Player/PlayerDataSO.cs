using UnityEngine;

[CreateAssetMenu(fileName = "New Player Data", menuName = "TurnBased/Player Data")]
public class PlayerDataSO : ScriptableObject
{
    [Header("기본 정보")]
    public string playerName = "Hero";
    public GameObject playerPrefab;
    public GameObject fieldPlayerPrefab;

    [Header("전투 스탯")]
    public int maxHealth = 100;
    public int currentHealth = 100;
    public int baseDamage = 15;

    [Header("필드 이동 및 회전 수치")]
    public float walkSpeed = 4f;
    public float runSpeed = 10f;
    public float jumpHeight = 2f;
    public float gravity = -9.81f;
    public float rotationSpeed = 5f;

    [Header("필드 가감속 설정")]
    public float acceleration = 15f;
    public float deceleration = 2f;

    [Header("필드 전투 설정")]
    public float startBattleTime = 10f;
}