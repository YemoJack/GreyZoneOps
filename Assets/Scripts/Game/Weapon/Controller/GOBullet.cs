using System;
using UnityEngine;

public class GOBullet
{
    public Vector3 position;
    public Vector3 velocity;

    private float gravity;
    private FirearmWeapon weapon;
    private int hitMask = Physics.DefaultRaycastLayers;

    private float lifeTime;
    private float maxLife;
    private float distance;
    
    public bool active;

    public void Init(Vector3 startPos, Vector3 dir, float gravity, FirearmWeapon weapon, int hitMask)
    {
        this.position = startPos;
        this.gravity = gravity;
        this.weapon = weapon;
        this.hitMask = hitMask;
        SOFirearmConfig config = weapon.Config as SOFirearmConfig;
        this.velocity = dir * config.bulletSpeed;
        this.maxLife = (config.maxRange - config.range)/config.bulletSpeed ;
        this.lifeTime = 0;
        this.active = true;
        this.distance = 0;
    }

    // ⚡ 核心：由 BulletManager 调用
    public void Simulate(float dt)
    {
        if (!active) return;

        lifeTime += dt;
        if (lifeTime > maxLife)
        {
            active = false;
            Debug.Log("Bullet Miss Time Out");
            return;
        }

        Vector3 oldPos = position;

        // 重力
        velocity += Vector3.down * gravity * dt;

        Vector3 newPos = oldPos + velocity * dt;

        distance += Vector3.Distance(oldPos, newPos);

        // ⚡ 分段检测（完美防穿墙）
        if (Physics.Raycast(oldPos, velocity.normalized, out var hit, (newPos - oldPos).magnitude, hitMask, QueryTriggerInteraction.Ignore))
        {
            position = hit.point;
            active = false;
            weapon?.OnHitTarget(hit,(System.Object)(lifeTime,distance));
            return;
        }

        // 没有命中
        position = newPos;
    }
}
