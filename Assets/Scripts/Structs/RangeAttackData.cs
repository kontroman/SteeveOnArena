using MineArena.Controllers;
using MineArena.Interfaces;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MineArena.Structs
{
    public struct RangeAttackData
    {
        public DamageData DamageData;
        public GameObject ProjectilePrefab;
        public Transform Target;
        public Transform FirePoint;

        public RangeAttackData(float damage, GameObject projectilePrefab, Transform target, Transform firePoint)
        {
            ProjectilePrefab = projectilePrefab;
            Target = target;
            FirePoint = firePoint;

            //TODO: ���� ��������� ��������� ����� �� ������ ������, �� ����� ���-�� ��-������� ���������� ���� ��������
            var damageable = Player.Instance.GetComponent<IDamageable>();

            DamageData = new DamageData(damage, damageable);
        }
    }
}
