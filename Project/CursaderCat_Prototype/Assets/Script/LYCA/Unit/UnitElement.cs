using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Unit
{
    public partial class UnitElement : MonoBehaviour
    {
        [Header("Parameter")]
        public int tacticSpeed;
        public int rangedRadius;
        public int health;
        public int attack;
        public int defend;
        public int speed;

        [Header("Temp Addition")] // 只持续一轮的增益，回合开始时重置
        public int tempShield;
        public int tempHealth;
        public int tempAttack;
        public int tempDefend;
        public int tempSpeed;

        [Header("Traits")]
        public List<Trait> traits;

        [Header("Current Parameter")]
        public int currentSpeed = 0;
        public int currentShield = 0;
        public int currentHealth = 0;
        public int currentAttackTime = 0;
        public int currentTacticSpeed = 0;

        [Header("Component")]
        public Units unit;
        public AudioSource hitAudio;

        public void SetUp()
        {
            currentHealth = health;
            ResetHealthText();
            unit = GetComponent<Units>();
            Central.Instance.UnitNumChangeEvent?.Invoke();
        }

        public bool CheckTraits(string _traitName)
            => traits.Find(t => t.name == _traitName) != null;

        public void ResetMove()
        {
            currentSpeed = speed + tempSpeed;
            //Debug.Log("重置移动力 " + this.ToString());
        }

        /// <summary>
        /// 重置战术调整
        /// 只应该在回合开始时调用
        /// </summary>
        public void ResetTactic() => currentTacticSpeed = tacticSpeed;

        public void ResetAttack() => currentAttackTime = 1;
    }

    public partial class UnitElement
    {
        public void DecreaseCurrentSpeed(int _tmp) => currentSpeed -= _tmp;

        public void DecreaseCurrentTacticSpeed(int _tmp) => currentTacticSpeed -= _tmp;

        public void DecreaseHealth(int _tmp)
        {
            if (currentShield > 0)
            {
                if (currentShield < _tmp)
                {
                    _tmp -= currentShield;
                    currentShield = 0;
                }
                else
                {
                    currentShield -= _tmp;
                    _tmp = 0;
                }
            }
            currentHealth -= _tmp;
            ResetHealthText();
            hitAudio.Play();

            if (currentHealth <= 0)
            {
                ApplyDie();
            }
        }

        public void DecreaseAttackTime(int _tmp) => currentAttackTime -= _tmp;

        public void AddCurrentShield(int _tmp)
        {
            currentShield += _tmp;
            ResetHealthText();
        }

        public void ApplyDie()
        {
            StartCoroutine(Enumerator());
            IEnumerator Enumerator()
            {
                yield return new WaitForEndOfFrame();
                yield return new WaitUntil(() => unit.isActing == false);
                UnitManager.Instance.units.Remove(unit);
                Central.Instance.UnitDieEvent?.Invoke(unit);
                Central.Instance.UnitsNumberChange();
                Destroy(gameObject);
            }
        }
    } // 实时计算部分

    public partial class UnitElement
    {
        private Material hitMaterial;
        private Material normalMaterial;
        private SpriteRenderer sr;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
        }

        private void Start()
        {
            hitMaterial = UnitManager.Instance.hitMaterial;
            normalMaterial = UnitManager.Instance.normalMaterial;
        }

        public void GetHit(int _tmp)
        {
            DecreaseHealth(_tmp);
            StopCoroutine(Enumerator());
            StartCoroutine(Enumerator());

            IEnumerator Enumerator()
            {
                sr.material = hitMaterial;
                yield return new WaitForSeconds(0.1f);
                sr.material = normalMaterial;
            }
        }
    } // 受到攻击

    public partial class UnitElement
    {
        [Header("Health Display")]
        public SpriteRenderer shieldIcon;
        public TextMeshPro healthText;

        private void OnEnable()
        {
            //healthText = transform.Find("Heart Number").GetComponent<TextMeshPro>();
        }

        private void OnValidate()
        {
            healthText = transform.Find("Heart Icon").GetComponentInChildren<TextMeshPro>();
        }

        private void ResetHealthText()
        {
            healthText.text =
                "x" + (currentHealth + currentShield + tempShield).ToString();
            if (currentShield + tempShield > 0) shieldIcon.enabled = true;
            else shieldIcon.enabled = false;
        }
    } // UI显示部分
}